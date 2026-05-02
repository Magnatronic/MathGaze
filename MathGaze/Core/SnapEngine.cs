using MathGaze.Core.Geometry;
using SkiaSharp;

namespace MathGaze.Core;

/// <summary>
/// Computes the nearest snap candidate for a given cursor position.
/// Priority: 1. Existing object endpoints, 2. Line-line intersections (≤6 lines), 3. Orientation guides (V/H/45°).
/// Snap threshold: 20px screen pixels (large enough for ±10px gaze imprecision).
/// </summary>
public sealed class SnapEngine
{
    private const float SnapThresholdPx = 20f;

    /// <summary>
    /// Returns the best snapped position (screen px) and an optional label for the status toast.
    /// If no snap candidate is within threshold, returns the original cursorPx with null label.
    /// </summary>
    public (SKPoint Position, string? Label) Snap(
        SKPoint cursorPx,
        IReadOnlyList<GeometryObject> objects,
        CoordinateMapper mapper)
    {
        float bestDist = SnapThresholdPx;
        SKPoint best   = cursorPx;
        string? label  = null;

        // ── 1. Existing object snap points (endpoints, centres) ───────────────
        foreach (var obj in objects)
        {
            foreach (var (snapPx, snapLabel) in obj.GetSnapPoints(mapper))
            {
                float d = SKPoint.Distance(cursorPx, snapPx);
                if (d < bestDist)
                {
                    bestDist = d;
                    best     = snapPx;
                    label    = snapLabel;
                }
            }
        }

        // ── 2. Line-line intersections (only for small sets — O(n²) perf guard) ──
        var lines = objects.OfType<LineObject>().ToList();
        if (lines.Count is >= 2 and <= 6)
        {
            for (int i = 0; i < lines.Count; i++)
            for (int j = i + 1; j < lines.Count; j++)
            {
                var p1 = mapper.PageToScreen(lines[i].X1Pt, lines[i].Y1Pt);
                var p2 = mapper.PageToScreen(lines[i].X2Pt, lines[i].Y2Pt);
                var q1 = mapper.PageToScreen(lines[j].X1Pt, lines[j].Y1Pt);
                var q2 = mapper.PageToScreen(lines[j].X2Pt, lines[j].Y2Pt);

                if (GeometryMath.TryLineIntersect(p1, p2, q1, q2, out var pt))
                {
                    float d = SKPoint.Distance(cursorPx, pt);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best     = pt;
                        label    = "intersection";
                    }
                }
            }
        }

        // ── 3. Orientation guides: vertical, horizontal, 45° from existing endpoints ──
        // Snap cursor's Y to horizontal alignment or X to vertical alignment with nearby snap points.
        foreach (var obj in objects)
        {
            foreach (var (snapPx, _) in obj.GetSnapPoints(mapper))
            {
                // Horizontal alignment: same Y as a snap point
                float dH = Math.Abs(cursorPx.Y - snapPx.Y);
                if (dH < bestDist)
                {
                    var candidate = new SKPoint(cursorPx.X, snapPx.Y);
                    float d = SKPoint.Distance(cursorPx, candidate);
                    if (d < bestDist) { bestDist = d; best = candidate; label = "horizontal"; }
                }
                // Vertical alignment: same X as a snap point
                float dV = Math.Abs(cursorPx.X - snapPx.X);
                if (dV < bestDist)
                {
                    var candidate = new SKPoint(snapPx.X, cursorPx.Y);
                    float d = SKPoint.Distance(cursorPx, candidate);
                    if (d < bestDist) { bestDist = d; best = candidate; label = "vertical"; }
                }
                // 45° alignment: |dx| ≈ |dy| from a snap point
                float dx = cursorPx.X - snapPx.X;
                float dy = cursorPx.Y - snapPx.Y;
                if (Math.Abs(Math.Abs(dx) - Math.Abs(dy)) < bestDist)
                {
                    float sign = dy >= 0 ? 1f : -1f;
                    float len  = (Math.Abs(dx) + Math.Abs(dy)) / 2f;
                    var candidate = new SKPoint(snapPx.X + Math.Sign(dx) * len,
                                               snapPx.Y + sign * len);
                    float d = SKPoint.Distance(cursorPx, candidate);
                    if (d < bestDist) { bestDist = d; best = candidate; label = "45°"; }
                }
            }
        }

        return (best, label);
    }
}
