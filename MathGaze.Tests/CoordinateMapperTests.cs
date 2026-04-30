using MathGaze.Core;
using SkiaSharp;
using Xunit;

namespace MathGaze.Tests;

public class CoordinateMapperTests
{
    // ── Round-trip: PageToScreen → ScreenToPage preserves coordinates ──────────
    // zoom × dpi: 0.5/1.0/1.5/2.0 × 96/120/144/192 = 16 combinations
    [Theory]
    [InlineData(0.5,  96)]  [InlineData(0.5,  120)]  [InlineData(0.5,  144)]  [InlineData(0.5,  192)]
    [InlineData(1.0,  96)]  [InlineData(1.0,  120)]  [InlineData(1.0,  144)]  [InlineData(1.0,  192)]
    [InlineData(1.5,  96)]  [InlineData(1.5,  120)]  [InlineData(1.5,  144)]  [InlineData(1.5,  192)]
    [InlineData(2.0,  96)]  [InlineData(2.0,  120)]  [InlineData(2.0,  144)]  [InlineData(2.0,  192)]
    public void RoundTrip_PageToScreenToPage_PreservesCoordinates(double zoom, double dpi)
    {
        var mapper = new CoordinateMapper(
            zoomFactor: zoom,
            dpiScale: dpi / 96.0,
            pageWidthPt: 595,
            pageHeightPt: 842);

        // Centre of an A4 page in PDF points
        const double xPt = 297.5;
        const double yPt = 421.0;

        var screen    = mapper.PageToScreen(xPt, yPt);
        var roundTrip = mapper.ScreenToPage(screen);

        Assert.Equal(xPt, roundTrip.xPt, precision: 3);
        Assert.Equal(yPt, roundTrip.yPt, precision: 3);
    }

    // ── Boundary: PDF top-left corner maps to canvasOrigin ─────────────────────
    [Theory]
    [InlineData(0.5,  96)]  [InlineData(0.5,  120)]  [InlineData(0.5,  144)]  [InlineData(0.5,  192)]
    [InlineData(1.0,  96)]  [InlineData(1.0,  120)]  [InlineData(1.0,  144)]  [InlineData(1.0,  192)]
    [InlineData(1.5,  96)]  [InlineData(1.5,  120)]  [InlineData(1.5,  144)]  [InlineData(1.5,  192)]
    [InlineData(2.0,  96)]  [InlineData(2.0,  120)]  [InlineData(2.0,  144)]  [InlineData(2.0,  192)]
    public void Boundary_PdfTopLeft_MapsToCanvasOrigin(double zoom, double dpi)
    {
        // canvasOriginX/Y default to 0 — page top-left should be at (0,0) in screen space
        var mapper = new CoordinateMapper(
            zoomFactor: zoom,
            dpiScale: dpi / 96.0,
            pageWidthPt: 595,
            pageHeightPt: 842,
            canvasOriginX: 0,
            canvasOriginY: 0);

        // PDF coordinate system: origin is BOTTOM-LEFT, Y-up.
        // Top-left of page in PDF space is (0, pageHeightPt).
        var screen = mapper.PageToScreen(0, 842);

        Assert.Equal(0f, screen.X, precision: 3);
        Assert.Equal(0f, screen.Y, precision: 3);
    }
}
