using SkiaSharp;

namespace MathGaze.Core;

/// <summary>
/// Translates between PDF point-space and screen pixel-space.
///
/// PDF coordinate system: origin BOTTOM-LEFT, Y increases UPWARD, units = points (1/72 inch).
/// Screen coordinate system: origin TOP-LEFT, Y increases DOWNWARD, units = physical pixels.
///
/// Update via the Update() method when zoom, DPI, page, or scroll offset changes.
/// </summary>
public sealed class CoordinateMapper
{
    private double _zoomFactor;
    private double _dpiScale;      // PixelsPerDip from VisualTreeHelper (1.0 at 96 DPI, 1.5 at 144 DPI)
    private double _pageWidthPt;
    private double _pageHeightPt;
    private double _canvasOriginX; // Physical pixels: X of page top-left corner in canvas space
    private double _canvasOriginY; // Physical pixels: Y of page top-left corner in canvas space

    // Scale factor: PDF points → physical screen pixels
    // 1 point = 1/72 inch. At 96 DPI baseline, 1 point = 96/72 px. dpiScale adjusts for actual DPI.
    // Multiply by zoomFactor for the current zoom level.
    private double Scale => (_dpiScale * 96.0 / 72.0) * _zoomFactor;

    public CoordinateMapper(
        double zoomFactor,
        double dpiScale,
        double pageWidthPt,
        double pageHeightPt,
        double canvasOriginX = 0,
        double canvasOriginY = 0)
    {
        _zoomFactor    = zoomFactor;
        _dpiScale      = dpiScale;
        _pageWidthPt   = pageWidthPt;
        _pageHeightPt  = pageHeightPt;
        _canvasOriginX = canvasOriginX;
        _canvasOriginY = canvasOriginY;
    }

    /// <summary>
    /// Update all transform parameters in one call (avoids creating a new instance on every zoom/scroll).
    /// </summary>
    public void Update(
        double zoomFactor,
        double dpiScale,
        double pageWidthPt,
        double pageHeightPt,
        double canvasOriginX,
        double canvasOriginY)
    {
        _zoomFactor    = zoomFactor;
        _dpiScale      = dpiScale;
        _pageWidthPt   = pageWidthPt;
        _pageHeightPt  = pageHeightPt;
        _canvasOriginX = canvasOriginX;
        _canvasOriginY = canvasOriginY;
    }

    /// <summary>
    /// Convert a point from PDF space (points, origin bottom-left) to screen space (physical pixels, origin top-left).
    /// </summary>
    public SKPoint PageToScreen(double xPt, double yPt)
    {
        // Flip Y: PDF Y=0 is at the bottom; screen Y=0 is at the top.
        // Distance from PDF bottom to the point = yPt.
        // Distance from PDF top  to the point = pageHeightPt - yPt.
        double screenX = xPt * Scale + _canvasOriginX;
        double screenY = (_pageHeightPt - yPt) * Scale + _canvasOriginY;
        return new SKPoint((float)screenX, (float)screenY);
    }

    /// <summary>
    /// Convert a point from screen space (physical pixels, origin top-left) to PDF space (points, origin bottom-left).
    /// </summary>
    public (double xPt, double yPt) ScreenToPage(SKPoint screenPx)
    {
        double xPt = (screenPx.X - _canvasOriginX) / Scale;
        // Invert the Y flip
        double yPt = _pageHeightPt - (screenPx.Y - _canvasOriginY) / Scale;
        return (xPt, yPt);
    }

    /// <summary>
    /// Returns the destination rectangle (in physical canvas pixels) to draw the page bitmap into.
    /// The page is centred horizontally; canvasOriginY sets the vertical offset (scroll position).
    /// canvasWidthPx and canvasHeightPx are the physical dimensions of the SKXamlCanvas.
    /// </summary>
    public SKRect GetPageDestRect(int canvasWidthPx, int canvasHeightPx)
    {
        double pageWidthPx  = _pageWidthPt  * Scale;
        double pageHeightPx = _pageHeightPt * Scale;

        // Centre horizontally; use canvasOriginX/Y for position
        float left   = (float)_canvasOriginX;
        float top    = (float)_canvasOriginY;
        float right  = (float)(_canvasOriginX + pageWidthPx);
        float bottom = (float)(_canvasOriginY + pageHeightPx);

        return new SKRect(left, top, right, bottom);
    }

    /// <summary>
    /// The page width in physical screen pixels at the current zoom and DPI.
    /// </summary>
    public double PageWidthPx => _pageWidthPt * Scale;

    /// <summary>
    /// The page height in physical screen pixels at the current zoom and DPI.
    /// </summary>
    public double PageHeightPx => _pageHeightPt * Scale;

    /// <summary>
    /// The page width in PDF points (coordinate-space units). Used for clamping intersection points to page bounds.
    /// </summary>
    public double PageWidthPt => _pageWidthPt;

    /// <summary>
    /// The page height in PDF points (coordinate-space units). Used for clamping intersection points to page bounds.
    /// </summary>
    public double PageHeightPt => _pageHeightPt;
}
