using MathGaze.Core;
using SkiaSharp;

namespace MathGaze.Core.Geometry;

public enum ProtractorStyle { Classic180, Full360 }

/// <summary>
/// Data model for a protractor instance placed at the intersection of two lines.
/// Per D-06 from Phase 3 CONTEXT.md: stores center in PDF coordinates, baseline angle
/// (screen-space CW from right at placement time), rotation offset (user-applied),
/// flip state (inner vs outer scale), and style (Classic180 vs Full360).
/// </summary>
public sealed class ProtractorObject : GeometryObject
{
    // ── PDF-space position ──────────────────────────────────────────────────
    /// <summary>Protractor center X in PDF point coordinates (nudge-able).</summary>
    public double CenterXPt { get; set; }

    /// <summary>Protractor center Y in PDF point coordinates (nudge-able).</summary>
    public double CenterYPt { get; set; }

    // ── Rotation ────────────────────────────────────────────────────────────
    /// <summary>
    /// Screen-space clockwise angle from right (0° = east, positive CW) at placement time.
    /// Stored in screen-space so RotateDegrees can be applied directly in SkiaSharp renderer.
    /// Per RESEARCH.md §Intersection Math: recommended to store screen-space CW angle.
    /// </summary>
    public double BaselineAngleDeg { get; set; }

    /// <summary>
    /// User-applied rotation offset in degrees. Starts at 0.
    /// Accumulated by RotateProtractorCommand (±1° and ±5° button presses).
    /// </summary>
    public double RotationOffsetDeg { get; set; }

    // ── Scale orientation ────────────────────────────────────────────────────
    /// <summary>
    /// false = inner scale (0→180° left to right); true = outer scale (180→0° left to right).
    /// Per D-03 from Phase 3 CONTEXT.md.
    /// </summary>
    public bool IsFlipped { get; set; }

    // ── Appearance ───────────────────────────────────────────────────────────
    /// <summary>Classic180 = semicircle (default); Full360 = full circle. Per D-05.</summary>
    public ProtractorStyle Style { get; set; } = ProtractorStyle.Classic180;

    // ── Source line references (for angle readout) ──────────────────────────
    /// <summary>
    /// GUID of the first line clicked (defines the baseline direction).
    /// Used at render time to compute the measured angle readout (D-11, A-4).
    /// </summary>
    public Guid Line1Id { get; init; }

    /// <summary>
    /// GUID of the second line clicked.
    /// Used at render time to compute the measured angle readout (D-11, A-4).
    /// </summary>
    public Guid Line2Id { get; init; }

    // ── Constants ────────────────────────────────────────────────────────────
    /// <summary>
    /// Default protractor radius in PDF points.
    /// 144 pt × 1.333 (Scale at zoom=1, 96 DPI) ≈ 192 screen px — large enough
    /// for 16pt labels at 10° intervals (~34px arc length each) to be readable
    /// for eye-gaze students without zooming in.
    /// </summary>
    public const double DefaultRadiusPt = 144.0;

    // ── Constructor ──────────────────────────────────────────────────────────
    public ProtractorObject(double centerXPt, double centerYPt,
                            double baselineAngleDeg,
                            Guid line1Id, Guid line2Id)
    {
        CenterXPt        = centerXPt;
        CenterYPt        = centerYPt;
        BaselineAngleDeg = baselineAngleDeg;
        Line1Id          = line1Id;
        Line2Id          = line2Id;
    }

    // ── GeometryObject overrides ─────────────────────────────────────────────
    public override void Draw(SKCanvas canvas, CoordinateMapper mapper, SKPaint paint)
        => throw new NotImplementedException("Draw implemented in GeometryLayerViewModel (Plan 04)");

    /// <summary>
    /// Hit test for the protractor. Checks:
    ///   1. A small center zone (≤24px) for nudge/whole-object selection.
    ///   2. The arc band (within tolerancePx of the outer radius arc).
    /// Radius conversion uses the same proxy-point approach as CircleObject.
    /// </summary>
    public override bool HitTest(SKPoint screenPx, CoordinateMapper mapper, float tolerancePx)
    {
        var centerScreen = mapper.PageToScreen(CenterXPt, CenterYPt);
        // Derive screen radius using a proxy offset in PDF-point space (same as CircleObject pattern)
        var edgeScreen = mapper.PageToScreen(CenterXPt + DefaultRadiusPt, CenterYPt);
        float radiusPx = edgeScreen.X - centerScreen.X;
        float dist     = SKPoint.Distance(screenPx, centerScreen);
        // Center zone hit (for nudge/drag selection)
        if (dist <= 24f) return true;
        // Arc band hit (within tolerancePx of circumference)
        return dist >= radiusPx - tolerancePx && dist <= radiusPx + tolerancePx;
    }

    /// <summary>
    /// Protractors do not contribute snap points — no snapping to the protractor body.
    /// Per plan: protractors are positioned via line intersection, not snap.
    /// </summary>
    public override IEnumerable<(SKPoint ScreenPx, string Label)> GetSnapPoints(CoordinateMapper mapper)
        => Enumerable.Empty<(SKPoint, string)>();
}
