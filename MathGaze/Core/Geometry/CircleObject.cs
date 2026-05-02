using MathGaze.Core;
using SkiaSharp;

namespace MathGaze.Core.Geometry;

public sealed class CircleObject : GeometryObject
{
    public double CenterXPt { get; set; }
    public double CenterYPt { get; set; }
    public double RadiusPt  { get; set; }

    /// <summary>null = whole-object selected; 0 = center sub-selected (translates); 1 = edge sub-selected (changes radius)</summary>
    public int? SelectedSubPoint { get; set; }

    public CircleObject(double centerXPt, double centerYPt, double radiusPt)
    {
        CenterXPt = centerXPt;
        CenterYPt = centerYPt;
        RadiusPt  = radiusPt;
    }

    public override void Draw(SKCanvas canvas, CoordinateMapper mapper, SKPaint paint)
        => throw new NotImplementedException("Draw implemented in GeometryLayerViewModel (Plan 04)");

    public override bool HitTest(SKPoint screenPx, CoordinateMapper mapper, float tolerancePx)
    {
        var centerPx = mapper.PageToScreen(CenterXPt, CenterYPt);
        // RadiusPt is in PDF points; convert to screen pixels via a proxy point offset
        var edgePx = mapper.PageToScreen(CenterXPt + RadiusPt, CenterYPt);
        float radiusPx = edgePx.X - centerPx.X;
        float dist = SKPoint.Distance(screenPx, centerPx);
        // Ring hit: within tolerance of the circumference
        if (Math.Abs(dist - radiusPx) <= tolerancePx) return true;
        // Center dot hit: within 18px of center (sub-point tap target)
        if (dist <= 18f) return true;
        return false;
    }

    public override IEnumerable<(SKPoint ScreenPx, string Label)> GetSnapPoints(CoordinateMapper mapper)
    {
        yield return (mapper.PageToScreen(CenterXPt, CenterYPt), "centre");
    }
}
