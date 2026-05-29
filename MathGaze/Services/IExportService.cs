namespace MathGaze.Services;

/// <summary>
/// Produces an annotated PDF file from the current session.
/// The output is an image-based PDF (one raster image per page) at 200 DPI. (D-08)
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Export all pages of sourcePdfPath with geometry annotations to outputPath.
    /// Returns true on success, false if the write failed (read-only dir, disk full, etc.).
    /// </summary>
    Task<bool> ExportAsync(string sourcePdfPath, string outputPath);
}
