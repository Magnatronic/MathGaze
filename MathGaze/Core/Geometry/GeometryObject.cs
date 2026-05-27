using MathGaze.Core;
using SkiaSharp;
using System.Text.Json.Serialization;

namespace MathGaze.Core.Geometry;

[JsonDerivedType(typeof(PointObject),      typeDiscriminator: "point")]
[JsonDerivedType(typeof(LineObject),       typeDiscriminator: "line")]
[JsonDerivedType(typeof(CircleObject),     typeDiscriminator: "circle")]
[JsonDerivedType(typeof(ProtractorObject), typeDiscriminator: "protractor")]
[JsonDerivedType(typeof(TextObject),       typeDiscriminator: "text")]
public abstract class GeometryObject
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public bool IsSelected { get; set; }

    public abstract void Draw(SKCanvas canvas, CoordinateMapper mapper, SKPaint paint);
    public abstract bool HitTest(SKPoint screenPx, CoordinateMapper mapper, float tolerancePx);
    public abstract IEnumerable<(SKPoint ScreenPx, string Label)> GetSnapPoints(CoordinateMapper mapper);
}
