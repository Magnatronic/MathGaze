using Docnet.Core;
using Docnet.Core.Models;
using SkiaSharp;
using System.IO;
using System.Runtime.InteropServices;

namespace MathGaze.Services;

/// <summary>
/// PDF service backed by Docnet.Core (PDFium engine).
///
/// Thread safety: PDFium's C API is not thread-safe. All DocLib calls are
/// gated by a SemaphoreSlim(1,1). Rendering is offloaded to Task.Run.
///
/// DocLib.Instance is a singleton — created once and never disposed.
/// (Disposing it while IDocReader instances are open crashes the process.)
/// </summary>
public sealed class DocnetPdfService : IPdfService
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _currentFilePath;
    private int _pageCount;
    private bool _disposed;

    public int PageCount => _pageCount;
    public bool IsOpen => _currentFilePath is not null;

    public async Task<bool> OpenDocumentAsync(string filePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Verify file exists before attempting PDFium open
            if (!File.Exists(filePath))
                return false;

            // Open briefly to read page count — DocReader is opened per-render
            using var docReader = DocLib.Instance.GetDocReader(filePath, new PageDimensions(1, 1));
            _pageCount       = docReader.GetPageCount();
            _currentFilePath = filePath;
            return true;
        }
        catch (Exception)
        {
            _currentFilePath = null;
            _pageCount = 0;
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void CloseDocument()
    {
        _lock.Wait();
        try
        {
            _currentFilePath = null;
            _pageCount = 0;
        }
        finally
        {
            _lock.Release();
        }
    }

    public (double widthPt, double heightPt) GetPageDimensionsPt(int pageIndex)
    {
        if (_currentFilePath is null) return (595, 842);

        _lock.Wait();
        try
        {
            // PageDimensions(double scalingFactor) with 1.0 means 1 pixel per PDF point.
            // GetPageWidth()/GetPageHeight() then return the page size in points directly.
            // DO NOT use PageDimensions(int, int) here — that constructor takes a pixel-space
            // viewport and GetPageWidth/Height returns scaled pixel dimensions, not points.
            using var docReader = DocLib.Instance.GetDocReader(_currentFilePath, new PageDimensions(1.0));
            if (pageIndex < 0 || pageIndex >= docReader.GetPageCount()) return (595, 842);

            using var pageReader = docReader.GetPageReader(pageIndex);
            return (pageReader.GetPageWidth(), pageReader.GetPageHeight());
        }
        catch
        {
            return (595, 842);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<SKBitmap?> GetPageBitmapAsync(int pageIndex, int targetWidthPx, int targetHeightPx)
    {
        if (_currentFilePath is null) return null;

        return await Task.Run(async () =>
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_currentFilePath is null) return null;

                // Docnet.Core 2.6.0 constraint: PageDimensions requires dimOne <= dimTwo.
                // If the page is landscape (width > height), swap and we handle rotation in
                // the bitmap creation step. For GCSE papers (always portrait A4), this
                // path is rarely triggered — but must be handled for exam booklets.
                bool needsSwap = targetWidthPx > targetHeightPx;
                int dimOne = needsSwap ? targetHeightPx : targetWidthPx;
                int dimTwo = needsSwap ? targetWidthPx  : targetHeightPx;

                using var docReader = DocLib.Instance.GetDocReader(
                    _currentFilePath,
                    new PageDimensions(dimOne, dimTwo));

                if (pageIndex < 0 || pageIndex >= docReader.GetPageCount()) return null;

                using var pageReader = docReader.GetPageReader(pageIndex);

                // Docnet.Core returns BGRA bytes
                var rawBytes = pageReader.GetImage();
                int width    = pageReader.GetPageWidth();
                int height   = pageReader.GetPageHeight();

                // Docnet returns BGRA bytes with straight (un-premultiplied) alpha.
                var imageInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
                var bitmap    = new SKBitmap(imageInfo);

                // Copy pixel data directly into the bitmap's own managed pixel buffer.
                // InstallPixels only stores a pointer — using it with a freed GCHandle
                // leaves a dangling pointer and produces a blank or corrupted image.
                Marshal.Copy(rawBytes, 0, bitmap.GetPixels(), rawBytes.Length);

                // If we swapped dimensions for landscape, rotate the bitmap 90°
                if (needsSwap)
                {
                    var rotated = RotateBitmap90(bitmap);
                    bitmap.Dispose();
                    return rotated;
                }

                return bitmap;
            }
            catch
            {
                return null;
            }
            finally
            {
                _lock.Release();
            }
        }).ConfigureAwait(false);
    }

    private static SKBitmap RotateBitmap90(SKBitmap source)
    {
        var rotated = new SKBitmap(source.Height, source.Width);
        using var canvas = new SKCanvas(rotated);
        canvas.Translate(source.Height, 0);
        canvas.RotateDegrees(90);
        canvas.DrawBitmap(source, 0, 0);
        return rotated;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lock.Dispose();
    }
}
