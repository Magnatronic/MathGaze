using MathGaze.Core;
using MathGaze.Core.Geometry;
using SkiaSharp;
using Xunit;
using System.Collections.Generic;

namespace MathGaze.Tests;

public class SnapEngineTests
{
    // Shared mapper: zoom=1.0, dpi=1.0, 595x842 A4 page, no offset
    // scale = (1.0 * 96.0/72.0) * 1.0 = 96/72 ≈ 1.333
    // PointObject at PDF (100, 400) maps to screen:
    //   screenX = 100 * 1.333 = 133.33px
    //   screenY = (842 - 400) * 1.333 = 442 * 1.333 = 589.33px
    private static CoordinateMapper MakeMapper() =>
        new CoordinateMapper(zoomFactor: 1.0, dpiScale: 1.0, pageWidthPt: 595, pageHeightPt: 842);

    /// <summary>
    /// Horizontal snap: cursor at same Y as a PointObject's screen position, 25px to the right.
    /// Must be >20px from the endpoint so priority-1 endpoint snap does not fire first.
    /// </summary>
    [Fact]
    public void Snap_HorizontalAlignment_ReturnsHorizontalLabel()
    {
        var engine = new SnapEngine();
        var mapper = MakeMapper();
        var point = new PointObject(100, 400);
        var objects = new List<GeometryObject> { point };
        var snapPt = mapper.PageToScreen(100, 400); // screen position of point
        // 25px right — outside the 20px endpoint threshold so horizontal guide fires, not "point"
        var cursor = new SKPoint(snapPt.X + 25f, snapPt.Y); // same Y, 25px right

        var (pos, label) = engine.Snap(cursor, objects, mapper);

        Assert.Equal("horizontal", label);
        Assert.InRange(pos.X, cursor.X - 0.5f, cursor.X + 0.5f); // X stays at cursor
        Assert.InRange(pos.Y, snapPt.Y - 0.5f, snapPt.Y + 0.5f); // Y snapped to point
    }

    /// <summary>
    /// Horizontal snap: cursor at same Y as a PointObject's screen position, 25px to the left.
    /// </summary>
    [Fact]
    public void Snap_HorizontalAlignment_LeftOfPoint_ReturnsHorizontalLabel()
    {
        var engine = new SnapEngine();
        var mapper = MakeMapper();
        var point = new PointObject(100, 400);
        var objects = new List<GeometryObject> { point };
        var snapPt = mapper.PageToScreen(100, 400);
        // 25px left — outside the 20px endpoint threshold so horizontal guide fires, not "point"
        var cursor = new SKPoint(snapPt.X - 25f, snapPt.Y);

        var (pos, label) = engine.Snap(cursor, objects, mapper);

        Assert.Equal("horizontal", label);
    }

    /// <summary>
    /// Vertical snap: cursor at same X as a PointObject, 25px below.
    /// Must be >20px from the endpoint so priority-1 endpoint snap does not fire first.
    /// </summary>
    [Fact]
    public void Snap_VerticalAlignment_ReturnsVerticalLabel()
    {
        var engine = new SnapEngine();
        var mapper = MakeMapper();
        var point = new PointObject(100, 400);
        var objects = new List<GeometryObject> { point };
        var snapPt = mapper.PageToScreen(100, 400);
        // 25px below — outside the 20px endpoint threshold so vertical guide fires, not "point"
        var cursor = new SKPoint(snapPt.X, snapPt.Y + 25f); // same X, 25px below

        var (pos, label) = engine.Snap(cursor, objects, mapper);

        Assert.Equal("vertical", label);
    }

    /// <summary>
    /// No snap: cursor 30px diagonally away — outside threshold in all directions.
    /// </summary>
    [Fact]
    public void Snap_NoNearbyPoints_ReturnsNullLabel()
    {
        var engine = new SnapEngine();
        var mapper = MakeMapper();
        var point = new PointObject(100, 400);
        var objects = new List<GeometryObject> { point };
        var snapPt = mapper.PageToScreen(100, 400);
        var cursor = new SKPoint(snapPt.X + 25f, snapPt.Y + 25f); // outside threshold for all snaps

        var (pos, label) = engine.Snap(cursor, objects, mapper);

        Assert.Null(label);
        Assert.InRange(pos.X, cursor.X - 0.5f, cursor.X + 0.5f);
        Assert.InRange(pos.Y, cursor.Y - 0.5f, cursor.Y + 0.5f);
    }

    /// <summary>
    /// Endpoint priority: cursor within 15px of a PointObject's exact position →
    /// label "point" (priority 1 beats orientation guides).
    /// </summary>
    [Fact]
    public void Snap_CursorOnPoint_ReturnsPointLabel()
    {
        var engine = new SnapEngine();
        var mapper = MakeMapper();
        var point = new PointObject(100, 400);
        var objects = new List<GeometryObject> { point };
        var snapPt = mapper.PageToScreen(100, 400);
        var cursor = new SKPoint(snapPt.X + 5f, snapPt.Y + 5f); // within 20px of endpoint

        var (pos, label) = engine.Snap(cursor, objects, mapper);

        Assert.Equal("point", label);
    }
}
