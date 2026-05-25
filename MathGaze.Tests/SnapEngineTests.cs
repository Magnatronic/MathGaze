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
    /// GAP-14b: orientation guides removed. Cursor at same Y as a PointObject, 25px to the right
    /// (outside 20px endpoint threshold). No snap fires — label is null, position is unchanged.
    /// </summary>
    [Fact]
    public void Snap_HorizontalAlignment_ReturnsNullLabel()
    {
        var engine = new SnapEngine();
        var mapper = MakeMapper();
        var point = new PointObject(100, 400);
        var objects = new List<GeometryObject> { point };
        var snapPt = mapper.PageToScreen(100, 400); // screen position of point
        // 25px right — outside 20px endpoint threshold; orientation guide removed → null
        var cursor = new SKPoint(snapPt.X + 25f, snapPt.Y); // same Y, 25px right

        var (pos, label) = engine.Snap(cursor, objects, mapper);

        Assert.Null(label);
        Assert.InRange(pos.X, cursor.X - 0.5f, cursor.X + 0.5f);
        Assert.InRange(pos.Y, cursor.Y - 0.5f, cursor.Y + 0.5f);
    }

    /// <summary>
    /// GAP-14b: orientation guides removed. Cursor at same Y as a PointObject, 25px to the left.
    /// No snap fires — label is null.
    /// </summary>
    [Fact]
    public void Snap_HorizontalAlignment_LeftOfPoint_ReturnsNullLabel()
    {
        var engine = new SnapEngine();
        var mapper = MakeMapper();
        var point = new PointObject(100, 400);
        var objects = new List<GeometryObject> { point };
        var snapPt = mapper.PageToScreen(100, 400);
        // 25px left — outside 20px endpoint threshold; orientation guide removed → null
        var cursor = new SKPoint(snapPt.X - 25f, snapPt.Y);

        var (pos, label) = engine.Snap(cursor, objects, mapper);

        Assert.Null(label);
    }

    /// <summary>
    /// GAP-14b: orientation guides removed. Cursor at same X as a PointObject, 25px below
    /// (outside 20px endpoint threshold). No snap fires — label is null.
    /// </summary>
    [Fact]
    public void Snap_VerticalAlignment_ReturnsNullLabel()
    {
        var engine = new SnapEngine();
        var mapper = MakeMapper();
        var point = new PointObject(100, 400);
        var objects = new List<GeometryObject> { point };
        var snapPt = mapper.PageToScreen(100, 400);
        // 25px below — outside 20px endpoint threshold; orientation guide removed → null
        var cursor = new SKPoint(snapPt.X, snapPt.Y + 25f); // same X, 25px below

        var (pos, label) = engine.Snap(cursor, objects, mapper);

        Assert.Null(label);
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

    /// <summary>
    /// GAP-12 regression: endpoint priority — when cursor is within 20px of endpoint A,
    /// endpoint snap wins. This test documents the priority architecture from 02-11/02-12.
    /// Two points: pointA (near cursor) and pointB (horizontally aligned with cursor but further
    /// away). Cursor snaps to pointA, not pointB.
    /// </summary>
    [Fact]
    public void Snap_HorizontalAlignment_WhenEndpointAlreadySnapped_StillReturnsPoint()
    {
        var engine = new SnapEngine();
        var mapper = MakeMapper();
        // pointA: cursor will be within ~4.24px (3+3 diagonal) — endpoint snap fires
        // pointB: same screen Y as pointA, 100px away
        var pointA = new PointObject(100, 400);
        var pointB = new PointObject(200, 400); // same PDF Y → same screen Y as pointA
        var objects = new List<GeometryObject> { pointA, pointB };
        var snapPtA = mapper.PageToScreen(100, 400);
        // Cursor: 3px right and 3px below pointA (distance ~4.24px — endpoint snap fires)
        var cursor = new SKPoint(snapPtA.X + 3f, snapPtA.Y + 3f);

        var (pos, label) = engine.Snap(cursor, objects, mapper);

        Assert.Equal("point", label);
    }

    /// <summary>
    /// GAP-14b: orientation guides removed. Cursor 21px to the right of a PointObject, same Y:
    /// distance = 21px (outside 20px endpoint threshold → no endpoint snap).
    /// Horizontal guide also removed → no snap at all, label is null.
    /// </summary>
    [Fact]
    public void Snap_HorizontalAlignment_JustOutsideEndpointThreshold_ReturnsNull()
    {
        var engine = new SnapEngine();
        var mapper = MakeMapper();
        var point = new PointObject(100, 400);
        var objects = new List<GeometryObject> { point };
        var snapPt = mapper.PageToScreen(100, 400);
        // 21px to the right, same Y: distance = 21 > 20 → endpoint snap does NOT fire.
        // Orientation guide removed → no snap at all.
        var cursor = new SKPoint(snapPt.X + 21f, snapPt.Y);

        var (pos, label) = engine.Snap(cursor, objects, mapper);

        Assert.Null(label);
        Assert.InRange(pos.X, cursor.X - 0.5f, cursor.X + 0.5f);
        Assert.InRange(pos.Y, cursor.Y - 0.5f, cursor.Y + 0.5f);
    }

    /// <summary>
    /// GAP-14/GAP-14b regression: orientation guides entirely removed (GAP-14b).
    /// Cursor at 25px horizontal + 12px vertical offset: distance ~27.7px exceeds endpoint
    /// threshold, and orientation guides are gone. Expects null label (no snap of any kind).
    /// </summary>
    [Fact]
    public void Snap_OrientationSnap_OutsideNewThreshold_ReturnsNull()
    {
        var engine = new SnapEngine();
        var mapper = MakeMapper();
        var point = new PointObject(100, 400);
        var objects = new List<GeometryObject> { point };
        var snapPt = mapper.PageToScreen(100, 400);
        // dH = 12px (> new 10px orient threshold), dV = 25px (> 10px),
        // distance to endpoint ≈ 27.7px (> 20px endpoint threshold) → no snap at all
        var cursor = new SKPoint(snapPt.X + 25f, snapPt.Y + 12f);

        var (pos, label) = engine.Snap(cursor, objects, mapper);

        Assert.Null(label);
        Assert.InRange(pos.X, cursor.X - 0.5f, cursor.X + 0.5f);
        Assert.InRange(pos.Y, cursor.Y - 0.5f, cursor.Y + 0.5f);
    }
}
