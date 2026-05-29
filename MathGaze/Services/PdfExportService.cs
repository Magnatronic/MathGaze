using MathGaze.Core;
using MathGaze.Core.Geometry;
using MathGaze.ViewModels;
using SkiaSharp;
using System.IO;

namespace MathGaze.Services;

/// <summary>
/// Renders all PDF pages at 200 DPI with geometry annotations baked in as a raster overlay.
/// Uses SKDocument.CreatePdf() — no new NuGet dependency. (D-08)
///
/// Page rendering:
///   1. Render source PDF page bitmap at 200 DPI via IPdfService.GetPageBitmapAsync
///   2. Composite geometry on top via GeometryLayerViewModel.DrawObjects
///   3. Embed the composited image into an SKDocument PDF page (page size in PDF points)
///
/// Export scale derivation (RESEARCH.md Pattern 2):
///   CoordinateMapper.Scale = (dpiScale * 96.0/72.0) * zoomFactor
///   Target scale = 200.0/72.0 (pixels per PDF point at 200 DPI)
///   With dpiScale=1.0: zoomFactor = 200.0/96.0
/// </summary>
public sealed class PdfExportService : IExportService
{
    private const double ExportDpi = 200.0;

    private readonly IPdfService             _pdfService;
    private readonly ISessionService         _sessionService;
    private readonly GeometryLayerViewModel  _geometryLayer;

    public PdfExportService(
        IPdfService pdfService,
        ISessionService sessionService,
        GeometryLayerViewModel geometryLayer)
    {
        _pdfService      = pdfService;
        _sessionService  = sessionService;
        _geometryLayer   = geometryLayer;
    }

    /// <summary>
    /// Derive the output path: strip any existing "-annotated" suffix then append "-annotated.pdf".
    /// Input:  "C:\exams\June 2017 QP.pdf"
    /// Output: "C:\exams\June 2017 QP-annotated.pdf"
    /// </summary>
    public static string BuildAnnotatedPath(string sourcePath)
    {
        string dir  = Path.GetDirectoryName(sourcePath) ?? string.Empty;
        string name = Path.GetFileNameWithoutExtension(sourcePath);
        // Strip any existing "-annotated" suffix to avoid double-appending
        if (name.EndsWith("-annotated", StringComparison.OrdinalIgnoreCase))
            name = name[..^"-annotated".Length];
        return Path.Combine(dir, name + "-annotated.pdf");
    }

    public async Task<bool> ExportAsync(string sourcePdfPath, string outputPath)
    {
        try
        {
            int totalPages = _pdfService.PageCount;
            if (totalPages <= 0) return false;

            // Collect all pages' geometry objects from the session store (D-02)
            var allPages = _sessionService.GetAllPages();

            // Run the entire PDF-write loop on a background thread to avoid freezing the UI.
            // GetPageBitmapAsync is already async internally but the SKDocument write loop
            // is CPU-bound — keep it off the UI thread.
            return await Task.Run(async () =>
            {
                try
                {
                    using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    using var pdfDoc     = SKDocument.CreatePdf(fileStream);

                    for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
                    {
                        var (widthPt, heightPt) = _pdfService.GetPageDimensionsPt(pageIndex);

                        // Compute pixel dimensions at 200 DPI (D-01)
                        int wPx = (int)Math.Round(widthPt  * ExportDpi / 72.0);
                        int hPx = (int)Math.Round(heightPt * ExportDpi / 72.0);

                        // 1. Render source PDF page as bitmap at 200 DPI
                        using var bitmap = await _pdfService.GetPageBitmapAsync(pageIndex, wPx, hPx).ConfigureAwait(false);
                        if (bitmap is null) continue;

                        // 2. Create offscreen surface for compositing geometry on top of PDF bitmap
                        using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
                        var c = surface.Canvas;

                        // Draw the PDF page bitmap (stretch to fill pixel dimensions)
                        c.DrawBitmap(bitmap, new SKRect(0, 0, bitmap.Width, bitmap.Height));

                        // 3. Build export CoordinateMapper at 200 DPI (RESEARCH.md Pattern 2)
                        //    Scale = (dpiScale * 96/72) * zoomFactor = 200/72
                        //    => with dpiScale=1.0: zoomFactor = 200.0/96.0
                        double exportZoom = ExportDpi / 96.0;
                        var exportMapper  = new CoordinateMapper(
                            zoomFactor:    exportZoom,
                            dpiScale:      1.0,
                            pageWidthPt:   widthPt,
                            pageHeightPt:  heightPt,
                            canvasOriginX: 0,
                            canvasOriginY: 0);

                        // 4. Draw geometry for this page (D-05: all objects as unselected)
                        // Session store uses 1-based page numbers; pageIndex is 0-based
                        if (allPages.TryGetValue(pageIndex + 1, out var pageObjects) && pageObjects.Count > 0)
                        {
                            double exportDpiScale = ExportDpi / 96.0;  // scale factor for stroke widths
                            _geometryLayer.DrawObjects(c, exportMapper, pageObjects, dpiScale: exportDpiScale);
                        }

                        // 5. Snapshot composited surface and write to PDF page
                        //    CRITICAL: BeginPage takes PDF POINTS not pixels (Pitfall 1)
                        //    CRITICAL: Draw bitmap into point-space rect (Pitfall 4)
                        using var snapshot    = surface.Snapshot();
                        var pageCanvas        = pdfDoc.BeginPage((float)widthPt, (float)heightPt);
                        pageCanvas.DrawImage(snapshot.ToRasterImage(), new SKRect(0, 0, (float)widthPt, (float)heightPt));
                        pdfDoc.EndPage();
                    }

                    pdfDoc.Close();  // Finalize PDF stream (A2: explicit Close before Dispose)
                    return true;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Read-only directory or disk full (Pitfall 6 / RESEARCH.md security note)
                    return false;
                }
            }).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }
}
