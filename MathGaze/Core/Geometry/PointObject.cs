using MathGaze.Core;
using SkiaSharp;

namespace MathGaze.Core.Geometry;

public sealed class PointObject : GeometryObject
{
    public double XPt { get; set; }
    public double YPt { get; set; }

    public PointObject(double xPt, double yPt)
    {
        XPt = xPt;
        YPt = yPt;
    }

    public override void Draw(SKCanvas canvas, CoordinateMapper mapper, SKPaint paint)
        => throw new NotImplementedException("Draw implemented in GeometryLayerViewModel (Plan 04)");

    public override bool HitTest(SKPoint screenPx, CoordinateMapper mapper, float tolerancePx)
    {
        var center = mapper.PageToScreen(XPt, YPt);
        return SKPoint.Distance(screenPx, center) <= tolerancePx;
    }

    public override IEnumerable<(SKPoint ScreenPx, string Label)> GetSnapPoints(CoordinateMapper mapper)
    {
        yield return (mapper.PageToScreen(XPt, YPt), "point");
    }
}
