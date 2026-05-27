using MathGaze.Core;
using MathGaze.Core.Geometry;
using MathGaze.Services;
using SkiaSharp;
using System.Text.Json;
using Xunit;

namespace MathGaze.Tests;

/// <summary>
/// Tests for 04-01: TextObject model and GeometryObject serialization foundation.
/// </summary>
public class TextObjectTests
{
    private static CoordinateMapper MakeMapper() =>
        new CoordinateMapper(zoomFactor: 1.0, dpiScale: 1.0, pageWidthPt: 595, pageHeightPt: 842);

    // ── TextObject construction ──────────────────────────────────────────────

    [Fact]
    public void TextObject_Constructor_StoresContentTextAndPosition()
    {
        var obj = new TextObject("5 cm", 100.0, 200.0);
        Assert.Equal("5 cm", obj.ContentText);
        Assert.Equal(100.0, obj.XPt);
        Assert.Equal(200.0, obj.YPt);
    }

    [Fact]
    public void TextObject_ParameterlessConstructor_ExistsAndDoesNotThrow()
    {
        // Required for System.Text.Json deserialization
        var obj = new TextObject();
        Assert.NotNull(obj);
        Assert.Equal(string.Empty, obj.ContentText);
    }

    [Fact]
    public void TextObject_ContentText_TruncatedAt500Chars_WhenLonger()
    {
        // DoS protection: T-04-01 mitigation
        string longText = new string('x', 600);
        var obj = new TextObject(longText, 0, 0);
        Assert.Equal(500, obj.ContentText.Length);
        Assert.Equal(new string('x', 500), obj.ContentText);
    }

    [Fact]
    public void TextObject_ContentText_NotTruncated_WhenExactly500Chars()
    {
        string text = new string('a', 500);
        var obj = new TextObject(text, 0, 0);
        Assert.Equal(500, obj.ContentText.Length);
    }

    // ── HitTest ─────────────────────────────────────────────────────────────

    [Fact]
    public void TextObject_HitTest_ReturnsTrue_WhenPointIsInsideBoundingBox()
    {
        var mapper = MakeMapper();
        // Place text at a known PDF position
        var obj = new TextObject("5 cm", 100.0, 421.0);
        // Screen position at the exact draw point (baseline anchor)
        var drawPx = mapper.PageToScreen(100.0, 421.0);
        // A click right at the draw point should be within the bounds
        bool hit = obj.HitTest(drawPx, mapper, tolerancePx: 8f);
        Assert.True(hit);
    }

    [Fact]
    public void TextObject_HitTest_ReturnsFalse_WhenPointIsFarOutsideBoundingBox()
    {
        var mapper = MakeMapper();
        var obj = new TextObject("5 cm", 100.0, 421.0);
        var drawPx = mapper.PageToScreen(100.0, 421.0);
        // 20px below the bounding box — well outside text bounds + 8px tolerance
        var farPt = new SKPoint(drawPx.X, drawPx.Y + 40f);
        bool hit = obj.HitTest(farPt, mapper, tolerancePx: 8f);
        Assert.False(hit);
    }

    // ── GetSnapPoints ────────────────────────────────────────────────────────

    [Fact]
    public void TextObject_GetSnapPoints_ReturnsEmpty()
    {
        var mapper = MakeMapper();
        var obj = new TextObject("5 cm", 100.0, 421.0);
        var snapPts = obj.GetSnapPoints(mapper);
        Assert.Empty(snapPts);
    }

    // ── GeometryObject.Id serialization (Pitfall 1 fix) ─────────────────────

    [Fact]
    public void GeometryObject_Id_SurvivesJsonRoundTrip_WithSameGuidValue()
    {
        // Uses TextObject (a concrete subclass) to test GeometryObject.Id round-trip
        var original = new TextObject("test", 10.0, 20.0);
        Guid originalId = original.Id;

        string json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<TextObject>(json);

        Assert.NotNull(restored);
        Assert.Equal(originalId, restored!.Id);
    }

    [Fact]
    public void GeometryObject_PolymorphicList_SurvivesJsonRoundTrip()
    {
        // Critical: verifies [JsonDerivedType] attributes allow polymorphic serialization
        var objects = new List<GeometryObject>
        {
            new TextObject("5 cm", 100.0, 200.0),
            new LineObject(0, 0, 100, 100),
        };

        string json = JsonSerializer.Serialize(objects);
        var restored = JsonSerializer.Deserialize<List<GeometryObject>>(json);

        Assert.NotNull(restored);
        Assert.Equal(2, restored!.Count);
        var restoredText = Assert.IsType<TextObject>(restored[0]);
        Assert.Equal("5 cm", restoredText.ContentText);
    }

    [Fact]
    public void ProtractorObject_Line1Id_Line2Id_SurviveJsonRoundTrip()
    {
        // Pitfall 2: Line1Id/Line2Id must not become Guid.Empty after restore
        var line1Id = Guid.NewGuid();
        var line2Id = Guid.NewGuid();
        var protractor = new ProtractorObject(100, 200, 45.0, line1Id, line2Id);
        Guid originalId = protractor.Id;

        string json = JsonSerializer.Serialize<GeometryObject>(protractor);
        var restored = JsonSerializer.Deserialize<GeometryObject>(json);

        var restoredProtr = Assert.IsType<ProtractorObject>(restored);
        Assert.Equal(originalId, restoredProtr.Id);
        Assert.Equal(line1Id, restoredProtr.Line1Id);
        Assert.Equal(line2Id, restoredProtr.Line2Id);
    }

    // ── GeometryService.NudgeObject TextObject case ──────────────────────────

    [Fact]
    public void GeometryService_NudgeObject_AppliesDeltaToTextObject()
    {
        var service = new GeometryService();
        var obj = new TextObject("5 cm", 100.0, 200.0);
        service.AddObject(obj);

        service.NudgeObject(obj.Id, dxPt: 5.0, dyPt: -3.0);

        Assert.Equal(105.0, obj.XPt, precision: 6);
        Assert.Equal(197.0, obj.YPt, precision: 6);
    }

    [Fact]
    public void GeometryService_NudgeObject_DoesNotThrow_ForTextObject()
    {
        var service = new GeometryService();
        var obj = new TextObject("hello", 50.0, 50.0);
        service.AddObject(obj);

        var ex = Record.Exception(() => service.NudgeObject(obj.Id, 1.0, 1.0));
        Assert.Null(ex);
    }
}
