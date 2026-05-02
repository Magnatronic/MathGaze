using MathGaze.Core;
using SkiaSharp;

namespace MathGaze.Core.Geometry;

public sealed class LineObject : GeometryObject
{
    public double X1Pt { get; set; }
    public double Y1Pt { get; set; }
    public double X2Pt { get; set; }
    public double Y2Pt { get; set; }

    /// <summary>null = whole-object selected; 0 = endpoint A sub-selected; 1 = endpoint B sub-selected</summary>
    public int? SelectedEndpoint { get; set; }

    public LineObject(double x1Pt, double y1Pt, double x2Pt, double y2Pt)
    {
        X1Pt = x1Pt; Y1Pt = y1Pt;
        X2Pt = x2Pt; Y2Pt = y2Pt;
    }

    public override void Draw(SKCanvas canvas, CoordinateMapper mapper, SKPaint paint)
        => throw new NotImplementedException("Draw implemented in GeometryLayerViewModel (Plan 04)");

    public override bool HitTest(SKPoint screenPx, CoordinateMapper mapper, float tolerancePx)
    {
        var p1 = mapper.PageToScreen(X1Pt, Y1Pt);
        var p2 = mapper.PageToScreen(X2Pt, Y2Pt);
        return GeometryMath.DistancePointToSegment(screenPx, p1, p2) <= tolerancePx;
    }

    public override IEnumerable<(SKPoint ScreenPx, string Label)> GetSnapPoints(CoordinateMapper mapper)
    {
        yield return (mapper.PageToScreen(X1Pt, Y1Pt), "endpoint A");
        yield return (mapper.PageToScreen(X2Pt, Y2Pt), "endpoint B");
    }
}
