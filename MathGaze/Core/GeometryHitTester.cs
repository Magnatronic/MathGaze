using MathGaze.Core.Geometry;
using SkiaSharp;
using System.Collections.Generic;

namespace MathGaze.Core;

/// <summary>
/// Hit testing logic for all geometry object types.
/// All coordinates are in physical screen pixels.
/// Tolerances (in screen pixels):
///   - Point hit radius: 18px (point dot is small; gaze needs a forgiving target)
///   - Line hit corridor: 10px (wide corridor for gaze imprecision)
///   - Circle ring tolerance: 10px
///   - Sub-point tap radius: 28px (= 56px diameter, satisfies >=56x56px gaze requirement)
/// </summary>
public static class GeometryHitTester
{
    public const float PointHitRadius       = 18f;
    public const float LineHitTolerance     = 10f;
    public const float CircleRingTolerance  = 10f;
    public const float SubPointTapRadius    = 28f; // 56px diameter per D-04/D-05

    /// <summary>
    /// Returns the topmost (last-placed = highest Z) object under screenPx, or null if none.
    /// Objects are iterated in reverse (last element first = top of stack).
    /// </summary>
    public static GeometryObject? TryHitObject(
        SKPoint screenPx,
        IReadOnlyList<GeometryObject> objects,
        CoordinateMapper mapper)
    {
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            var obj = objects[i];
            float tolerance = obj switch
            {
                PointObject  => PointHitRadius,
                LineObject   => LineHitTolerance,
                CircleObject => CircleRingTolerance,
                _            => LineHitTolerance,
            };
            if (obj.HitTest(screenPx, mapper, tolerance))
                return obj;
        }
        return null;
    }

    /// <summary>
    /// When a Line is already selected, check if click is near one of its endpoints (sub-point hit).
    /// Returns (line, endpointIndex) where endpointIndex is 0 or 1, or null if no sub-point hit.
    /// Per D-04: two-pass hit: sub-points checked before full re-selection (Pitfall 4 in RESEARCH.md).
    /// </summary>
    public static (LineObject line, int endpointIndex)? TryHitLineSubPoint(
        SKPoint screenPx,
        LineObject line,
        CoordinateMapper mapper)
    {
        var ep0 = mapper.PageToScreen(line.X1Pt, line.Y1Pt);
        var ep1 = mapper.PageToScreen(line.X2Pt, line.Y2Pt);
        if (SKPoint.Distance(screenPx, ep0) <= SubPointTapRadius) return (line, 0);
        if (SKPoint.Distance(screenPx, ep1) <= SubPointTapRadius) return (line, 1);
        return null;
    }

    /// <summary>
    /// When a Circle is already selected, check if click is near center dot (0) or edge point (1).
    /// Returns (circle, subPointIndex) or null.
    /// Per D-05: center = translate whole circle; edge = change radius only.
    /// </summary>
    public static (CircleObject circle, int subPointIndex)? TryHitCircleSubPoint(
        SKPoint screenPx,
        CircleObject circle,
        CoordinateMapper mapper)
    {
        var centerPx = mapper.PageToScreen(circle.CenterXPt, circle.CenterYPt);
        if (SKPoint.Distance(screenPx, centerPx) <= SubPointTapRadius) return (circle, 0);

        // Edge point: rightmost point of the circle at current zoom
        var edgePx = mapper.PageToScreen(circle.CenterXPt + circle.RadiusPt, circle.CenterYPt);
        if (SKPoint.Distance(screenPx, edgePx) <= SubPointTapRadius) return (circle, 1);
        return null;
    }
}
