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

    /// <summary>
    /// Transient UI state — never persisted to the sidecar.
    /// [JsonIgnore] prevents IsSelected from being written to or read from the JSON sidecar,
    /// which means TrySaveAsync does NOT need to clear it on the live objects before serialising.
    /// Without this attribute, the save loop mutated the same object references that live in
    /// GeometryService._objects, silently clearing selection on every ObjectsChanged event.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsSelected { get; set; }

    public abstract void Draw(SKCanvas canvas, CoordinateMapper mapper, SKPaint paint);
    public abstract bool HitTest(SKPoint screenPx, CoordinateMapper mapper, float tolerancePx);
    public abstract IEnumerable<(SKPoint ScreenPx, string Label)> GetSnapPoints(CoordinateMapper mapper);
}
