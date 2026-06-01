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

    // Protractor tick mark — minor (1° and 5° ticks)
    private readonly SKPaint _tickMinorPaint = new()
    {
        Style       = SKPaintStyle.Stroke,
        Color       = new SKColor(0x1A, 0x1A, 0x2E, 160),   // BrushInk, 63% alpha
        StrokeWidth = 1f,
        IsAntialias = false,  // minor ticks: performance > quality
    };

    // Protractor tick mark — major (10° ticks)
    private readonly SKPaint _tickMajorPaint = new()
    {
        Style       = SKPaintStyle.Stroke,
        Color       = new SKColor(0x1A, 0x1A, 0x2E, 220),   // BrushInk, near-opaque
        StrokeWidth = 1.5f,
        IsAntialias = true,
    };

    // Protractor numeric labels — outer scale (0→180), 11pt for readability
    private readonly SKPaint _labelPaint = new()
    {
        Style       = SKPaintStyle.Fill,
        Color       = new SKColor(0x1A, 0x1A, 0x2E, 220),
        IsAntialias = true,
    };

    // SKFont for protractor numeric labels (outer) — 16pt for eye-gaze readability
    private readonly SKFont _labelFont = new(SKTypeface.Default, 16f);

    // Protractor numeric labels — inner scale (180→0), slightly smaller and more transparent
    private readonly SKPaint _innerLabelPaint = new()
    {
        Style       = SKPaintStyle.Fill,
        Color       = new SKColor(0x1A, 0x1A, 0x2E, 160),  // more transparent than outer
        IsAntialias = true,
    };

    // SKFont for inner scale labels — 11pt
    private readonly SKFont _innerLabelFont = new(SKTypeface.Default, 11f);

    // Text label paint — T.ink colour (0x1A1A2E), slightly transparent, fill
    private readonly SKPaint _textPaint = new()
    {
        Style       = SKPaintStyle.Fill,
        Color       = new SKColor(0x1A, 0x1A, 0x2E, 220),  // BrushInk
        IsAntialias = true,
    };

    // Text label selection border — accent cobalt stroke, 1.5px
    private readonly SKPaint _textSelectedBorderPaint = new()
    {
        Style       = SKPaintStyle.Stroke,
        Color       = new SKColor(0x3B, 0x6F, 0xD4, 255),  // BrushAccent
        StrokeWidth = 1.5f,
        IsAntialias = true,
    };

    // SKFont for text labels — Consolas (T.mono) 14pt with null fallback to system default
    // Phase 3 established: use SKFont-based API only (SKPaint.TextSize is CS0618 deprecated)
    private readonly SKFont _textFont = new(
        SKTypeface.FromFamilyName("Consolas") ?? SKTypeface.Default, 14f);

    // Scale tracking — updated at the top of Draw() when the combined scale (dpiScale * ZoomFactor) changes.
    // _lastScale = 0 forces a first-run update of all paint/font sizes.
    private double _lastScale = 0.0;

    // Tracks the dpiScale used in the most recent live Draw() call.
    // DrawObjects() saves/restores _lastScale and _currentDpiScaleF around the export draw so
    // the next live Draw() correctly reapplies the screen scale (not the export scale).
    private double _lastScreenDpiScale = 0.0;

    // Cached DPI scale as float for use inside DrawObject / DrawSubPointDot / DrawTextLabel / DrawProtractor.
    // Set at the top of Draw() so all private helpers read a consistent value for this frame.
    private float _currentDpiScaleF = 1.0f;

    public GeometryLayerViewModel(IGeometryService geometryService)
    {
        _geometryService = geometryService;
    }

    /// <summary>
    /// Draw all committed geometry objects. Called from PdfCanvasViewModel.Paint()
    /// after canvas.DrawBitmap() and before DrawGhostPreview().
    /// </summary>
    public void Draw(SKCanvas canvas, CoordinateMapper? mapper, double dpiScale = 1.0)
    {
        if (mapper is null) return;

        // Track screen dpiScale so DrawObjects can restore it after export.
        _lastScreenDpiScale = dpiScale;

        // Update paint/font sizes when combined scale changes (first call forces update via _lastScale=0).
        if (Math.Abs(dpiScale - _lastScale) > 0.001)
        {
            _lastScale = dpiScale;
            float s = (float)dpiScale;
            _normalPaint.StrokeWidth             = 2.5f * s;
            _selectedPaint.StrokeWidth           = 2.5f * s;
            _subRingActivePaint.StrokeWidth      = 2.5f * s;
            _tickMajorPaint.StrokeWidth          = 1.5f * s;
            _tickMinorPaint.StrokeWidth          = 1.0f * s;
            _textSelectedBorderPaint.StrokeWidth = 1.5f * s;
            // SKFont.Size is a read-write property in SkiaSharp 3.x — no reallocation needed.
            _labelFont.Size      = 16f * s;
            _innerLabelFont.Size = 11f * s;
            _textFont.Size       = 14f * s;
        }

        _currentDpiScaleF = (float)dpiScale;

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

    /// <summary>
    /// Export overload: draws an explicit object list (not _geometryService.Objects) at the given
    /// export scale. All objects are drawn in unselected style — selection chrome is UI-only.
    /// Saves and restores _lastScale and _currentDpiScaleF so the next live Draw() correctly
    /// reapplies the screen dpiScale.
    /// </summary>
    public void DrawObjects(
        SKCanvas canvas,
        CoordinateMapper mapper,
        IReadOnlyList<GeometryObject> objects,
        double dpiScale = 1.0)
    {
        // Save screen-render state
        double savedLastScale = _lastScale;
        float  savedDpiScaleF = _currentDpiScaleF;

        // Force paint update for export scale
        _lastScale = 0;  // reset so the update block fires
        if (Math.Abs(dpiScale - _lastScale) > 0.001)
        {
            _lastScale = dpiScale;
            float s = (float)dpiScale;
            _normalPaint.StrokeWidth             = 2.5f * s;
            _selectedPaint.StrokeWidth           = 2.5f * s;
            _subRingActivePaint.StrokeWidth      = 2.5f * s;
            _tickMajorPaint.StrokeWidth          = 1.5f * s;
            _tickMinorPaint.StrokeWidth          = 1.0f * s;
            _textSelectedBorderPaint.StrokeWidth = 1.5f * s;
            _labelFont.Size      = 16f * s;
            _innerLabelFont.Size = 11f * s;
            _textFont.Size       = 14f * s;
        }
        _currentDpiScaleF = (float)dpiScale;

        // Draw all objects as unselected (no selection chrome for export)
        foreach (var obj in objects)
            DrawObject(canvas, obj, mapper, selected: false);

        // Restore screen-render state. Set _lastScale=0 (not savedLastScale) so the guard
        // in Draw() always fires on the next frame and re-applies the screen DPI scale to all
        // paint objects — which are currently at export scale and must be corrected.
        _lastScale        = 0;
        _currentDpiScaleF = savedDpiScaleF;
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
                canvas.DrawCircle(ptPx, 8f * _currentDpiScaleF, strokePaint);
                // Centre dot (fill)
                canvas.DrawCircle(ptPx, 4f * _currentDpiScaleF, dotPaint);
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
                canvas.DrawCircle(centerPx, 4f * _currentDpiScaleF, dotPaint);
                break;

            case ProtractorObject prot:
                DrawProtractor(canvas, prot, mapper, selected);
                break;

            case TextObject text:
                DrawTextLabel(canvas, text, mapper, selected);
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
        // Filled dot: 8px radius (scaled by DPI)
        canvas.DrawCircle(center, 8f * _currentDpiScaleF, isActive ? _subDotPaint : _subDotInactivePaint);

        if (isActive)
        {
            // Active ring: 14px radius around the active sub-point (per design reference)
            canvas.DrawCircle(center, 14f * _currentDpiScaleF, _subRingActivePaint);
        }
    }

    /// <summary>
    /// Render a text label at its PDF-space position.
    /// Uses SKFont-based DrawText overload (Phase 3 pattern — avoids CS0618 on SKPaint.TextSize).
    /// Baseline is placed at PageToScreen(XPt, YPt); selected state draws a cobalt bounding rect.
    /// </summary>
    private void DrawTextLabel(SKCanvas canvas, TextObject text, CoordinateMapper mapper, bool selected)
    {
        if (string.IsNullOrEmpty(text.ContentText)) return;

        var drawPx = mapper.PageToScreen(text.XPt, text.YPt);

        // Render text — baseline at drawPx
        canvas.DrawText(text.ContentText, drawPx.X, drawPx.Y,
            SKTextAlign.Left, _textFont, _textPaint);

        // Selection bounding rect: ink bounds + 4px padding on all sides
        if (selected)
        {
            float advance = _textFont.MeasureText(text.ContentText, out SKRect bounds);
            float pad = 4f * _currentDpiScaleF;
            var selRect = new SKRect(
                drawPx.X + bounds.Left   - pad,
                drawPx.Y + bounds.Top    - pad,
                drawPx.X + bounds.Left   + advance + pad,
                drawPx.Y + bounds.Bottom + pad);
            canvas.DrawRect(selRect, _textSelectedBorderPaint);
        }
    }

    private void DrawProtractor(SKCanvas canvas, ProtractorObject obj,
                                 CoordinateMapper mapper, bool selected)
    {
        var centerPx = mapper.PageToScreen(obj.CenterXPt, obj.CenterYPt);

        // Derive screen radius via proxy-point offset (CoordinateMapper.Scale is private)
        // Same pattern as CircleObject and ProtractorObject.HitTest
        var edgePx  = mapper.PageToScreen(obj.CenterXPt + obj.RadiusPt, obj.CenterYPt);
        float radiusPx = edgePx.X - centerPx.X;
        if (radiusPx < 10f) return;  // too small to render meaningfully

        var bodyPaint = selected ? _selectedPaint : _normalPaint;

        // BaselineAngleDeg is stored in screen-space (CW from right) per RESEARCH.md §Intersection Math
        // Total rotation = baseline (screen-space CW from right) + user rotation offset
        float totalRotDeg = (float)(obj.BaselineAngleDeg + obj.RotationOffsetDeg);

        bool isFull  = obj.Style == ProtractorStyle.Full360;
        int  arcDeg  = isFull ? 360 : 180;

        // For Classic180: startAngle=-180° positions the arc top (9 o'clock = left end of baseline)
        // For Full360: startAngle=0° draws the full circle starting from 3 o'clock
        float startAngle = isFull ? 0f : -180f;

        canvas.Save();
        canvas.Translate(centerPx.X, centerPx.Y);
        canvas.RotateDegrees(totalRotDeg);

        // 1. Arc body
        var oval = new SKRect(-radiusPx, -radiusPx, radiusPx, radiusPx);
        canvas.DrawArc(oval, startAngle, arcDeg, false, bodyPaint);

        // 2. Baseline and arm lines (for Classic180 only — the flat diameter line)
        // The full baseline from -radiusPx to +radiusPx is the primary arm.
        // Additionally draw explicit arm lines from the center crosshair edge out to the arc
        // at both 0° (right) and 180° (left) so the arm visually connects centre to scale arc.
        if (!isFull)
        {
            canvas.DrawLine(-radiusPx, 0, radiusPx, 0, bodyPaint);
        }
        else
        {
            // Full 360°: draw a cross through centre as orientation reference arms
            canvas.DrawLine(-radiusPx, 0, radiusPx, 0, bodyPaint);
            canvas.DrawLine(0, -radiusPx, 0, radiusPx, bodyPaint);
        }

        // 3. Scale tick marks at 1° increments
        for (int a = 0; a <= arcDeg; a++)
        {
            float angleDeg = startAngle + a;
            float angleRad = angleDeg * MathF.PI / 180f;
            float cos = MathF.Cos(angleRad);
            float sin = MathF.Sin(angleRad);

            bool isMajor = (a % 10 == 0);
            bool isMid   = (!isMajor && a % 5 == 0);
            float tickLen = (isMajor ? 24f : isMid ? 13f : 5f) * _currentDpiScaleF;

            float r1 = radiusPx - tickLen;
            float r2 = radiusPx;
            canvas.DrawLine(cos * r1, sin * r1, cos * r2, sin * r2,
                isMajor ? _tickMajorPaint : _tickMinorPaint);
        }

        // 4. Numeric labels every 10° — dual scale (outer 0→180, inner 180→0)
        // Real protractors show both directions so students can read from either end.
        float outerLabelR = radiusPx - 32f * _currentDpiScaleF;   // clear the major tick + gap
        float innerLabelR = radiusPx - 58f * _currentDpiScaleF;   // further inward so dual scales don't overlap
        float labelClamp  = 8f * _currentDpiScaleF;
        if (outerLabelR < labelClamp) outerLabelR = labelClamp;
        if (innerLabelR < labelClamp) innerLabelR = labelClamp;

        for (int a = 0; a <= arcDeg; a += 10)
        {
            float angleDeg = startAngle + a;
            float angleRad = angleDeg * MathF.PI / 180f;
            float cos = MathF.Cos(angleRad);
            float sin = MathF.Sin(angleRad);

            // Outer label: increases left to right (IsFlipped reverses this)
            int outerVal = obj.IsFlipped ? (arcDeg - a) : a;
            // Inner label: decreases left to right (IsFlipped reverses this)
            int innerVal = obj.IsFlipped ? a : (arcDeg - a);

            // SkiaSharp DrawText places baseline at position; offset by 0.35*size for vertical centering
            float outerVert = _labelFont.Size * 0.35f;
            float innerVert = _innerLabelFont.Size * 0.35f;

            canvas.DrawText(outerVal.ToString(),
                cos * outerLabelR, sin * outerLabelR + outerVert,
                SKTextAlign.Center, _labelFont, _labelPaint);

            canvas.DrawText(innerVal.ToString(),
                cos * innerLabelR, sin * innerLabelR + innerVert,
                SKTextAlign.Center, _innerLabelFont, _innerLabelPaint);
        }

        // 5. Center crosshair (small cross at origin)
        float cs = _currentDpiScaleF;
        canvas.DrawCircle(0, 0, 4f * cs, bodyPaint);
        canvas.DrawLine(-8f * cs, 0, -3f * cs, 0, bodyPaint);
        canvas.DrawLine( 3f * cs, 0,  8f * cs, 0, bodyPaint);
        canvas.DrawLine(0, -8f * cs, 0, -3f * cs, bodyPaint);
        canvas.DrawLine(0,  3f * cs, 0,  8f * cs, bodyPaint);

        canvas.Restore();
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
        _tickMinorPaint.Dispose();
        _tickMajorPaint.Dispose();
        _labelPaint.Dispose();
        _labelFont.Dispose();
        _innerLabelPaint.Dispose();
        _innerLabelFont.Dispose();
        _textPaint.Dispose();
        _textSelectedBorderPaint.Dispose();
        _textFont.Dispose();
    }
}
