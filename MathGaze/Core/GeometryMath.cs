using MathGaze.Core.Geometry;
using SkiaSharp;

namespace MathGaze.Core;

/// <summary>Static 2D geometry math used by hit testing and snap engine.</summary>
public static class GeometryMath
{
    /// <summary>
    /// Minimum distance from point p to the line segment [a, b] in screen pixels.
    /// Clamps the projection to [0, 1] so points beyond segment endpoints return
    /// distance to the nearest endpoint, not the infinite-line distance.
    /// </summary>
    public static float DistancePointToSegment(SKPoint p, SKPoint a, SKPoint b)
    {
        var ab = b - a;
        var ap = p - a;
        float lenSq = ab.X * ab.X + ab.Y * ab.Y;
        if (lenSq < 1e-6f) return SKPoint.Distance(p, a); // degenerate segment
        float t = (ap.X * ab.X + ap.Y * ab.Y) / lenSq;
        t = Math.Clamp(t, 0f, 1f);
        var closest = new SKPoint(a.X + t * ab.X, a.Y + t * ab.Y);
        return SKPoint.Distance(p, closest);
    }

    /// <summary>
    /// Attempt to find the intersection of two infinite lines defined by segments (a1->a2) and (b1->b2).
    /// Returns true and sets pt to the intersection point. Returns false for parallel/coincident lines.
    /// Note: intersection is for infinite lines — callers decide whether the point is within segment bounds.
    /// </summary>
    public static bool TryLineIntersect(SKPoint a1, SKPoint a2, SKPoint b1, SKPoint b2, out SKPoint pt)
    {
        float dx1 = a2.X - a1.X, dy1 = a2.Y - a1.Y;
        float dx2 = b2.X - b1.X, dy2 = b2.Y - b1.Y;
        float denom = dx1 * dy2 - dy1 * dx2;
        pt = SKPoint.Empty;
        if (Math.Abs(denom) < 1e-6f) return false; // parallel
        float t = ((b1.X - a1.X) * dy2 - (b1.Y - a1.Y) * dx2) / denom;
        pt = new SKPoint(a1.X + t * dx1, a1.Y + t * dy1);
        return true;
    }

    /// <summary>
    /// Find the intersection of two LineObjects in PDF-point space (double precision).
    /// Returns true and sets pt to the intersection point (in PDF points).
    /// Returns false if lines are parallel or near-parallel (|denom| &lt; 1e-9).
    /// This is the PDF-space equivalent of TryLineIntersect (which works in screen pixels).
    /// Called by ToolViewModel protractor placement handler (D-01/D-03 from Phase 3 CONTEXT.md).
    /// Per T-03-02: the parallel check prevents division by zero.
    /// </summary>
    public static bool TryLineIntersectPt(LineObject a, LineObject b, out (double xPt, double yPt) pt)
    {
        double dx1 = a.X2Pt - a.X1Pt;
        double dy1 = a.Y2Pt - a.Y1Pt;
        double dx2 = b.X2Pt - b.X1Pt;
        double dy2 = b.Y2Pt - b.Y1Pt;
        double denom = dx1 * dy2 - dy1 * dx2;
        pt = default;
        if (Math.Abs(denom) < 1e-9) return false; // parallel or coincident
        double t = ((b.X1Pt - a.X1Pt) * dy2 - (b.Y1Pt - a.Y1Pt) * dx2) / denom;
        pt = (a.X1Pt + t * dx1, a.Y1Pt + t * dy1);
        return true;
    }
}
