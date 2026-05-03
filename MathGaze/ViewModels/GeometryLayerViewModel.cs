using MathGaze.Core;
using MathGaze.Core.Geometry;
using MathGaze.Services;
using SkiaSharp;

namespace MathGaze.ViewModels;

/// <summary>
/// Renders all committed geometry objects to the SkiaSharp canvas.
/// Draw() is called from PdfCanvasViewModel.Paint() after the PDF bitmap and before the ghost preview.
///
/// SKPaint objects are cached as fields (created once in constructor) — never allocated per frame
/// to avoid GC pauses (Pitfall 5 in RESEARCH.md).
///
/// Rendering layers within Draw() (bottom to top):
///   1. Unselected objects (normal ink)
///   2. Selected object body (accent colour)
///   3. Sub-point tap targets for selected Line/Circle (endpoint dots + hit zone indicators)
/// </summary>
public sealed class GeometryLayerViewModel : IDisposable
{
    private readonly IGeometryService _geometryService;
    private bool _disposed;

    // ── Cached paints (created once — never per-frame) ───────────────────────

    // Normal (unselected) object paint — ink colour, 2.5px stroke
    private readonly SKPaint _normalPaint = new()
    {
        Style       = SKPaintStyle.Stroke,
        Color       = new SKColor(0x1A, 0x1A, 0x2E, 220),  // BrushInk + slightly transparent
        StrokeWidth = 2.5f,
        IsAntialias = true,
    };

    // Selected object body paint — accent cobalt
    private readonly SKPaint _selectedPaint = new()
    {
        Style       = SKPaintStyle.Stroke,
        Color       = new SKColor(0x3B, 0x6F, 0xD4, 255),  // BrushAccent
        StrokeWidth = 2.5f,
        IsAntialias = true,
    };

    // Point dot fill — normal
    private readonly SKPaint _dotNormalPaint = new()
    {
        Style       = SKPaintStyle.Fill,
        Color       = new SKColor(0x1A, 0x1A, 0x2E, 220),
        IsAntialias = true,
    };

    // Point dot fill — selected/accent
    private readonly SKPaint _dotAccentPaint = new()
    {
        Style       = SKPaintStyle.Fill,
        Color       = new SKColor(0x3B, 0x6F, 0xD4, 255),
        IsAntialias = true,
    };

    // Sub-point endpoint dot (filled, accent — active sub-point)
    private readonly SKPaint _subDotPaint = new()
    {
        Style       = SKPaintStyle.Fill,
        Color       = new SKColor(0x3B, 0x6F, 0xD4, 255),
        IsAntialias = true,
    };

    // Sub-point inactive dot (accent, lower alpha — inactive sub-point when another is active)
    private readonly SKPaint _subDotInactivePaint = new()
    {
        Style       = SKPaintStyle.Fill,
        Color       = new SKColor(0x3B, 0x6F, 0xD4, 140),
        IsAntialias = true,
    };

    // Active sub-point ring indicator — accent cobalt stroke
    private readonly SKPaint _subRingActivePaint = new()
    {
        Style       = SKPaintStyle.Stroke,
        Color       = new SKColor(0x3B, 0x6F, 0xD4, 255),
        StrokeWidth = 2.5f,
        IsAntialias = true,
    };

    public GeometryLayerViewModel(IGeometryService geometryService)
    {
        _geometryService = geometryService;
    }

    /// <summary>
    /// Draw all committed geometry objects. Called from PdfCanvasViewModel.Paint()
    /// after canvas.DrawBitmap() and before DrawGhostPreview().
    /// </summary>
    public void Draw(SKCanvas canvas, CoordinateMapper? mapper)
    {
        if (mapper is null) return;

        var objects = _geometryService.Objects;

        // Pass 1: draw all unselected objects at normal style
        foreach (var obj in objects)
        {
            if (obj.IsSelected) continue;
            DrawObject(canvas, obj, mapper, selected: false);
        }

        // Pass 2: draw selected object on top with accent style + sub-point targets
        foreach (var obj in objects)
        {
            if (!obj.IsSelected) continue;
            DrawObject(canvas, obj, mapper, selected: true);
            DrawSubPointTargets(canvas, obj, mapper);
        }
    }

    private void DrawObject(SKCanvas canvas, GeometryObject obj, CoordinateMapper mapper, bool selected)
    {
        var strokePaint = selected ? _selectedPaint : _normalPaint;
        var dotPaint    = selected ? _dotAccentPaint : _dotNormalPaint;

        switch (obj)
        {
            case PointObject pt:
                var ptPx = mapper.PageToScreen(pt.XPt, pt.YPt);
                // Outer ring (stroke)
                canvas.DrawCircle(ptPx, 8f, strokePaint);
                // Centre dot (fill)
                canvas.DrawCircle(ptPx, 4f, dotPaint);
                break;

            case LineObject line:
                var p1 = mapper.PageToScreen(line.X1Pt, line.Y1Pt);
                var p2 = mapper.PageToScreen(line.X2Pt, line.Y2Pt);
                canvas.DrawLine(p1, p2, strokePaint);
                break;

            case CircleObject circle:
                var centerPx = mapper.PageToScreen(circle.CenterXPt, circle.CenterYPt);
                var edgePx   = mapper.PageToScreen(circle.CenterXPt + circle.RadiusPt, circle.CenterYPt);
                float radiusPx = edgePx.X - centerPx.X;
                if (radiusPx > 0f)
                    canvas.DrawCircle(centerPx, radiusPx, strokePaint);
                // Small center dot
                canvas.DrawCircle(centerPx, 4f, dotPaint);
                break;
        }
    }

    /// <summary>
    /// Draw sub-point tap targets for the selected object.
    /// Each target: a filled dot (8px radius) + optional active ring (14px radius) at the sub-point position.
    /// The invisible hit zone (28px radius per D-04/D-05) is handled by GeometryHitTester — not drawn here.
    /// </summary>
    private void DrawSubPointTargets(SKCanvas canvas, GeometryObject obj, CoordinateMapper mapper)
    {
        switch (obj)
        {
            case LineObject line:
                // D-04: Both endpoints always visible as tap targets when line is selected
                var ep0 = mapper.PageToScreen(line.X1Pt, line.Y1Pt);
                var ep1 = mapper.PageToScreen(line.X2Pt, line.Y2Pt);
                DrawSubPointDot(canvas, ep0, line.SelectedEndpoint == 0);
                DrawSubPointDot(canvas, ep1, line.SelectedEndpoint == 1);
                break;

            case CircleObject circle:
                // D-05: Center and edge point as tap targets when circle is selected
                var cPx = mapper.PageToScreen(circle.CenterXPt, circle.CenterYPt);
                var ePx = mapper.PageToScreen(circle.CenterXPt + circle.RadiusPt, circle.CenterYPt);
                DrawSubPointDot(canvas, cPx, circle.SelectedSubPoint == 0);
                DrawSubPointDot(canvas, ePx, circle.SelectedSubPoint == 1);
                break;
            // D-06: PointObject has no sub-points — nothing to draw
        }
    }

    private void DrawSubPointDot(SKCanvas canvas, SKPoint center, bool isActive)
    {
        // Filled dot: 8px radius
        canvas.DrawCircle(center, 8f, isActive ? _subDotPaint : _subDotInactivePaint);

        if (isActive)
        {
            // Active ring: 14px radius around the active sub-point (per design reference)
            canvas.DrawCircle(center, 14f, _subRingActivePaint);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _normalPaint.Dispose();
        _selectedPaint.Dispose();
        _dotNormalPaint.Dispose();
        _dotAccentPaint.Dispose();
        _subDotPaint.Dispose();
        _subDotInactivePaint.Dispose();
        _subRingActivePaint.Dispose();
    }
}
