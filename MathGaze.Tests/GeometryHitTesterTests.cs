using MathGaze.Core;
using MathGaze.Core.Geometry;
using SkiaSharp;
using Xunit;
using System.Collections.Generic;

namespace MathGaze.Tests;

public class GeometryHitTesterTests
{
    // Shared mapper: zoom=1.0, dpi=1.0, 595x842 A4 page, no offset
    private static CoordinateMapper MakeMapper() =>
        new CoordinateMapper(zoomFactor: 1.0, dpiScale: 1.0, pageWidthPt: 595, pageHeightPt: 842);

    // --- PointObject hit tests ---

    [Fact]
    public void TryHitObject_ClickOnPoint_ReturnsPointObject()
    {
        var mapper = MakeMapper();
        var pt = new PointObject(100, 421); // centre-left of A4
        var screenPt = mapper.PageToScreen(100, 421);
        var result = GeometryHitTester.TryHitObject(screenPt, new List<GeometryObject> { pt }, mapper);
        Assert.Same(pt, result);
    }

    [Fact]
    public void TryHitObject_ClickFarFromPoint_ReturnsNull()
    {
        var mapper = MakeMapper();
        var pt = new PointObject(100, 421);
        var screenPt = mapper.PageToScreen(100, 421);
        var farPt = new SKPoint(screenPt.X + 30, screenPt.Y); // 30px away > 18px tolerance
        var result = GeometryHitTester.TryHitObject(farPt, new List<GeometryObject> { pt }, mapper);
        Assert.Null(result);
    }

    // --- LineObject hit tests ---

    [Fact]
    public void TryHitObject_ClickOnLineMidpoint_ReturnsLineObject()
    {
        var mapper = MakeMapper();
        // Horizontal line at y=421 from x=100 to x=200 in PDF points
        var line = new LineObject(100, 421, 200, 421);
        var p1 = mapper.PageToScreen(100, 421);
        var p2 = mapper.PageToScreen(200, 421);
        var mid = new SKPoint((p1.X + p2.X) / 2f, p1.Y); // midpoint on the line
        var result = GeometryHitTester.TryHitObject(mid, new List<GeometryObject> { line }, mapper);
        Assert.Same(line, result);
    }

    [Fact]
    public void TryHitObject_ClickFarFromLine_ReturnsNull()
    {
        var mapper = MakeMapper();
        var line = new LineObject(100, 421, 200, 421);
        var p1 = mapper.PageToScreen(100, 421);
        var p2 = mapper.PageToScreen(200, 421);
        var mid = new SKPoint((p1.X + p2.X) / 2f, p1.Y + 20f); // 20px below line > 10px tolerance
        var result = GeometryHitTester.TryHitObject(mid, new List<GeometryObject> { line }, mapper);
        Assert.Null(result);
    }

    // --- LineObject sub-point hit tests (D-04) ---

    [Fact]
    public void TryHitLineSubPoint_ClickOnEndpointA_ReturnsIndex0()
    {
        var mapper = MakeMapper();
        var line = new LineObject(100, 421, 200, 421);
        var ep0 = mapper.PageToScreen(100, 421);
        var result = GeometryHitTester.TryHitLineSubPoint(ep0, line, mapper);
        Assert.NotNull(result);
        Assert.Equal(0, result!.Value.endpointIndex);
    }

    [Fact]
    public void TryHitLineSubPoint_ClickOnEndpointB_ReturnsIndex1()
    {
        var mapper = MakeMapper();
        var line = new LineObject(100, 421, 200, 421);
        var ep1 = mapper.PageToScreen(200, 421);
        var result = GeometryHitTester.TryHitLineSubPoint(ep1, line, mapper);
        Assert.NotNull(result);
        Assert.Equal(1, result!.Value.endpointIndex);
    }

    [Fact]
    public void TryHitLineSubPoint_ClickFarFromEndpoints_ReturnsNull()
    {
        var mapper = MakeMapper();
        var line = new LineObject(100, 421, 200, 421);
        var midPx = mapper.PageToScreen(150, 421); // midpoint — not near either endpoint
        var result = GeometryHitTester.TryHitLineSubPoint(midPx, line, mapper);
        Assert.Null(result);
    }

    // --- CircleObject hit tests ---

    [Fact]
    public void TryHitObject_ClickOnCircleRing_ReturnsCircleObject()
    {
        var mapper = MakeMapper();
        // Circle at centre (150, 421) with radius 30pt
        var circle = new CircleObject(150, 421, 30);
        var edgePx = mapper.PageToScreen(180, 421); // right-edge of circle
        var ringPt = new SKPoint(edgePx.X, edgePx.Y); // exactly on ring
        var result = GeometryHitTester.TryHitObject(ringPt, new List<GeometryObject> { circle }, mapper);
        Assert.Same(circle, result);
    }

    [Fact]
    public void TryHitCircleSubPoint_ClickOnCenter_ReturnsIndex0()
    {
        var mapper = MakeMapper();
        var circle = new CircleObject(150, 421, 30);
        var centerPx = mapper.PageToScreen(150, 421);
        var result = GeometryHitTester.TryHitCircleSubPoint(centerPx, circle, mapper);
        Assert.NotNull(result);
        Assert.Equal(0, result!.Value.subPointIndex);
    }
}
