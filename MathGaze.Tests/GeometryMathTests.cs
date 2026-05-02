using MathGaze.Core;
using SkiaSharp;
using Xunit;

namespace MathGaze.Tests;

public class GeometryMathTests
{
    // DistancePointToSegment: point exactly at midpoint of horizontal segment
    [Fact]
    public void DistancePointToSegment_PointAtMidpoint_ReturnsZero()
    {
        var a = new SKPoint(0, 0);
        var b = new SKPoint(100, 0);
        var p = new SKPoint(50, 0);
        Assert.Equal(0f, GeometryMath.DistancePointToSegment(p, a, b), precision: 3);
    }

    // DistancePointToSegment: point perpendicular to midpoint, 5px above
    [Fact]
    public void DistancePointToSegment_PerpendicularOffset_ReturnsOffset()
    {
        var a = new SKPoint(0, 0);
        var b = new SKPoint(100, 0);
        var p = new SKPoint(50, 5);
        Assert.Equal(5f, GeometryMath.DistancePointToSegment(p, a, b), precision: 3);
    }

    // DistancePointToSegment: point beyond endpoint is clamped (not negative/infinite-line distance)
    [Fact]
    public void DistancePointToSegment_BeyondEndpoint_ReturnsDistanceToEndpoint()
    {
        var a = new SKPoint(0, 0);
        var b = new SKPoint(100, 0);
        var p = new SKPoint(110, 0); // 10px beyond b
        Assert.Equal(10f, GeometryMath.DistancePointToSegment(p, a, b), precision: 3);
    }

    // DistancePointToSegment: degenerate segment (a == b) returns point distance
    [Fact]
    public void DistancePointToSegment_DegenerateSegment_ReturnsPointDistance()
    {
        var a = new SKPoint(50, 50);
        var b = new SKPoint(50, 50);
        var p = new SKPoint(58, 50);
        Assert.Equal(8f, GeometryMath.DistancePointToSegment(p, a, b), precision: 2);
    }

    // TryLineIntersect: two crossing diagonals intersect at (50,50)
    [Fact]
    public void TryLineIntersect_CrossingLines_ReturnsIntersection()
    {
        var a1 = new SKPoint(0, 0);
        var a2 = new SKPoint(100, 100);
        var b1 = new SKPoint(100, 0);
        var b2 = new SKPoint(0, 100);
        bool hit = GeometryMath.TryLineIntersect(a1, a2, b1, b2, out var pt);
        Assert.True(hit);
        Assert.Equal(50f, pt.X, precision: 1);
        Assert.Equal(50f, pt.Y, precision: 1);
    }

    // TryLineIntersect: parallel horizontal lines — no intersection
    [Fact]
    public void TryLineIntersect_ParallelLines_ReturnsFalse()
    {
        var a1 = new SKPoint(0, 0);
        var a2 = new SKPoint(100, 0);
        var b1 = new SKPoint(0, 10);
        var b2 = new SKPoint(100, 10);
        bool hit = GeometryMath.TryLineIntersect(a1, a2, b1, b2, out _);
        Assert.False(hit);
    }
}
