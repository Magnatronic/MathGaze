using MathGaze.Core;
using SkiaSharp;

namespace MathGaze.Core.Geometry;

public abstract class GeometryObject
{
    public Guid Id { get; } = Guid.NewGuid();
    public bool IsSelected { get; set; }

    public abstract void Draw(SKCanvas canvas, CoordinateMapper mapper, SKPaint paint);
    public abstract bool HitTest(SKPoint screenPx, CoordinateMapper mapper, float tolerancePx);
    public abstract IEnumerable<(SKPoint ScreenPx, string Label)> GetSnapPoints(CoordinateMapper mapper);
}
