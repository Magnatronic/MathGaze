using MathGaze.Core;
using SkiaSharp;

namespace MathGaze.Core.Geometry;

/// <summary>
/// A clipboard-pasted text label placed at a PDF-space coordinate (D-04).
/// ContentText is immutable after construction (D-03).
/// XPt/YPt are mutable — NudgeObjectCommand adjusts them (D-05/TEXT-02).
/// </summary>
public sealed class TextObject : GeometryObject
{
    /// <summary>Text content from clipboard at placement time. Immutable (D-03). Max 500 chars (security: DoS protection — T-04-01).</summary>
    public string ContentText { get; init; } = string.Empty;

    /// <summary>Horizontal position in PDF-point coordinates (D-04/D-10).</summary>
    public double XPt { get; set; }

    /// <summary>Vertical position in PDF-point coordinates (D-04/D-10).</summary>
    public double YPt { get; set; }

    /// <summary>Parameterless constructor required for System.Text.Json deserialization.</summary>
    public TextObject() { }

    public TextObject(string contentText, double xPt, double yPt)
    {
        // Truncate at 500 chars (DoS protection — school machine safety per RESEARCH.md §Security, T-04-01)
        ContentText = contentText.Length > 500 ? contentText[..500] : contentText;
        XPt = xPt;
        YPt = yPt;
    }

    /// <summary>
    /// Hit-test against the rendered text bounding rect expanded by tolerancePx on all sides.
    /// Uses SKFont.MeasureText for accurate ink bounds including kerning (Pitfall 5 mitigation:
    /// 'using var' ensures SKFont is disposed after each hit-test call — one per click, not per frame).
    /// Font size 14f matches the GeometryLayerViewModel render font.
    /// </summary>
    public override bool HitTest(SKPoint screenPx, CoordinateMapper mapper, float tolerancePx)
    {
        var drawPx = mapper.PageToScreen(XPt, YPt);
        using var font = new SKFont(SKTypeface.FromFamilyName("Consolas") ?? SKTypeface.Default, 14f);
        float advance = font.MeasureText(ContentText, out SKRect bounds);
        var hitRect = new SKRect(
            drawPx.X + bounds.Left  - tolerancePx,
            drawPx.Y + bounds.Top   - tolerancePx,
            drawPx.X + bounds.Left  + advance + tolerancePx,
            drawPx.Y + bounds.Bottom + tolerancePx);
        return hitRect.Contains(screenPx);
    }

    /// <summary>Text objects have no snap points (D-04: only XPt/YPt for nudge).</summary>
    public override IEnumerable<(SKPoint ScreenPx, string Label)> GetSnapPoints(CoordinateMapper mapper)
        => Enumerable.Empty<(SKPoint, string)>();

    /// <summary>
    /// Draw is not implemented on the model — rendering is handled in GeometryLayerViewModel
    /// (established pattern: all concrete draw logic lives in the renderer, not the model).
    /// </summary>
    public override void Draw(SKCanvas canvas, CoordinateMapper mapper, SKPaint paint)
        => throw new NotSupportedException("TextObject rendering is handled by GeometryLayerViewModel, not the model.");
}
