using SkiaSharp;

namespace MathGaze.Services;

/// <summary>
/// Contract for PDF loading and page rendering.
/// All methods are safe to call from any thread.
/// </summary>
public interface IPdfService : IDisposable
{
    /// <summary>Number of pages in the loaded document, or 0 if no document is open.</summary>
    int PageCount { get; }

    /// <summary>True if a document is currently open.</summary>
    bool IsOpen { get; }

    /// <summary>
    /// Open a PDF file. Closes any previously open document first.
    /// Returns true on success, false on failure (corrupt file, wrong format).
    /// </summary>
    Task<bool> OpenDocumentAsync(string filePath);

    /// <summary>
    /// Close the currently open document and release resources.
    /// Safe to call when no document is open.
    /// </summary>
    void CloseDocument();

    /// <summary>
    /// Get the width and height of the specified page in PDF points (1/72 inch).
    /// Returns (595, 842) as a safe default for A4 if no document is open.
    /// </summary>
    (double widthPt, double heightPt) GetPageDimensionsPt(int pageIndex);

    /// <summary>
    /// Render the specified page to an SKBitmap.
    /// targetWidthPx and targetHeightPx are the physical pixel dimensions to render at.
    /// Caller is responsible for disposing the returned bitmap.
    /// Returns null if no document is open or pageIndex is out of range.
    /// </summary>
    Task<SKBitmap?> GetPageBitmapAsync(int pageIndex, int targetWidthPx, int targetHeightPx);
}
