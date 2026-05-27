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
    private readonly MainViewModel    _mainVm;
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

    // Practice Mode readout arc paint
    private readonly SKPaint _readoutArcPaint = new()
    {
        Style       = SKPaintStyle.Stroke,
        Color       = new SKColor(0x3B, 0x6F, 0xD4, 200),   // BrushAccent cobalt
        StrokeWidth = 2f,
        IsAntialias = true,
    };

    // Practice Mode readout text paint
    private readonly SKPaint _readoutTextPaint = new()
    {
        Style       = SKPaintStyle.Fill,
        Color       = new SKColor(0x3B, 0x6F, 0xD4, 255),
        IsAntialias = true,
    };

    // SKFont for Practice Mode readout text — 14pt, modern SkiaSharp 3.x API
    private readonly SKFont _readoutFont = new(SKTypeface.Default, 14f);

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

    public GeometryLayerViewModel(IGeometryService geometryService, MainViewModel mainViewModel)
    {
        _geometryService = geometryService;
        _mainVm          = mainViewModel;
        // Subscribe to IsPracticeMode changes so canvas repaints when mode toggles
        _mainVm.PropertyChanged += OnMainVmPropertyChanged;
    }

    private void OnMainVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsPracticeMode))
            _geometryService.ObjectsChanged_ForceRaise();
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
        // Filled dot: 8px radius
        canvas.DrawCircle(center, 8f, isActive ? _subDotPaint : _subDotInactivePaint);

        if (isActive)
        {
            // Active ring: 14px radius around the active sub-point (per design reference)
            canvas.DrawCircle(center, 14f, _subRingActivePaint);
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
            var selRect = new SKRect(
                drawPx.X + bounds.Left   - 4f,
                drawPx.Y + bounds.Top    - 4f,
                drawPx.X + bounds.Left   + advance + 4f,
                drawPx.Y + bounds.Bottom + 4f);
            canvas.DrawRect(selRect, _textSelectedBorderPaint);
        }
    }

    private void DrawProtractor(SKCanvas canvas, ProtractorObject obj,
                                 CoordinateMapper mapper, bool selected)
    {
        var centerPx = mapper.PageToScreen(obj.CenterXPt, obj.CenterYPt);

        // Derive screen radius via proxy-point offset (CoordinateMapper.Scale is private)
        // Same pattern as CircleObject and ProtractorObject.HitTest
        var edgePx  = mapper.PageToScreen(obj.CenterXPt + ProtractorObject.DefaultRadiusPt, obj.CenterYPt);
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

        // 2. Baseline (for Classic180 only — the flat diameter line)
        if (!isFull)
            canvas.DrawLine(-radiusPx, 0, radiusPx, 0, bodyPaint);

        // 3. Scale tick marks at 1° increments
        for (int a = 0; a <= arcDeg; a++)
        {
            float angleDeg = startAngle + a;
            float angleRad = angleDeg * MathF.PI / 180f;
            float cos = MathF.Cos(angleRad);
            float sin = MathF.Sin(angleRad);

            bool isMajor = (a % 10 == 0);
            bool isMid   = (!isMajor && a % 5 == 0);
            float tickLen = isMajor ? 24f : isMid ? 13f : 5f;

            float r1 = radiusPx - tickLen;
            float r2 = radiusPx;
            canvas.DrawLine(cos * r1, sin * r1, cos * r2, sin * r2,
                isMajor ? _tickMajorPaint : _tickMinorPaint);
        }

        // 4. Numeric labels every 10° — dual scale (outer 0→180, inner 180→0)
        // Real protractors show both directions so students can read from either end.
        float outerLabelR = radiusPx - 32f;   // clear the 24px major tick + 8px gap
        float innerLabelR = radiusPx - 58f;   // further inward so dual scales don't overlap at 16pt
        if (outerLabelR < 8f) outerLabelR = 8f;
        if (innerLabelR < 8f) innerLabelR = 8f;

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
        canvas.DrawCircle(0, 0, 4f, bodyPaint);
        canvas.DrawLine(-8f, 0, -3f, 0, bodyPaint);
        canvas.DrawLine( 3f, 0,  8f, 0, bodyPaint);
        canvas.DrawLine(0, -8f, 0, -3f, bodyPaint);
        canvas.DrawLine(0,  3f, 0,  8f, bodyPaint);

        // 6. Practice Mode readout (D-14: only when IsPracticeMode = true)
        if (_mainVm.IsPracticeMode)
        {
            float measuredAngleDeg = ComputeMeasuredAngle(obj);
            DrawReadout(canvas, measuredAngleDeg, radiusPx, obj.IsFlipped);
        }

        canvas.Restore();
    }

    /// <summary>
    /// Computes the angle the student would read off the protractor at its current orientation.
    /// This is the angle between line 1 (baseline) and line 2, as seen from the protractor's perspective.
    ///
    /// Per D-11: the readout = angle where the second arm crosses the protractor scale.
    /// Per RESEARCH.md §Open Questions Q3: show 0–180° (acute/obtuse); use IsFlipped for inner/outer reading.
    /// </summary>
    private float ComputeMeasuredAngle(ProtractorObject obj)
    {
        // Find the two source lines by ID
        var line1 = _geometryService.Objects.FirstOrDefault(o => o.Id == obj.Line1Id) as LineObject;
        var line2 = _geometryService.Objects.FirstOrDefault(o => o.Id == obj.Line2Id) as LineObject;

        if (line1 is null || line2 is null) return 0f;

        if (obj.Style == ProtractorStyle.Full360)
        {
            double bearing = ((obj.RotationOffsetDeg % 360.0) + 360.0) % 360.0;
            return (float)bearing;
        }

        // Use screen-space direction so the result is independent of draw direction.
        // PDF Y increases upward; screen Y increases downward — flip dy.
        double dx2_s = line2.X2Pt - line2.X1Pt;
        double dy2_s = -(line2.Y2Pt - line2.Y1Pt);

        // Rotate Line 2 into the protractor's local frame.
        double bRad  = obj.BaselineAngleDeg * Math.PI / 180.0;
        double cosB  = Math.Cos(-bRad), sinB = Math.Sin(-bRad);
        double localDx = dx2_s * cosB - dy2_s * sinB;
        double localDy = dx2_s * sinB + dy2_s * cosB;

        // Arc occupies negative-Y of local space. If Line 2 points the other way, flip it.
        if (localDy > 0) { localDx = -localDx; localDy = -localDy; }

        // Natural reading = angle from right-end baseline (+X) going CCW into arc.
        double localAngle   = Math.Atan2(localDy, localDx) * 180.0 / Math.PI;
        double naturalAngle = -localAngle;   // [-180,0] → [0,180]

        if (obj.IsFlipped) naturalAngle = 180.0 - naturalAngle;

        return (float)Math.Clamp(naturalAngle, 0.0, 180.0);
    }

    /// <summary>
    /// Renders the angle readout inside the protractor (Practice Mode only).
    /// Called inside canvas.Save()/Restore() scope from DrawProtractor — origin is at protractor center.
    /// Per RESEARCH.md Pattern 4 (shared.jsx measuring prop).
    /// </summary>
    private void DrawReadout(SKCanvas canvas, float measuredAngleDeg, float radiusPx, bool isFlipped = false)
    {
        if (measuredAngleDeg < 0.5f) return;  // nothing meaningful to show

        float arcRadius = Math.Max(30f, radiusPx * 0.25f);  // inner arc at 25% of outer radius
        var ovalSmall   = new SKRect(-arcRadius, -arcRadius, arcRadius, arcRadius);

        // When IsFlipped, the outer scale's 0° is at the left end of the baseline (-180° in local space).
        // Draw arc starting from -180°, sweeping CW (positive in Skia) by measuredAngleDeg.
        // When not flipped, start from 0° sweeping CCW (negative in Skia) as before.
        float arcStartDeg, arcSweepDeg, midAngleRad;
        if (isFlipped)
        {
            arcStartDeg  = -180f;
            arcSweepDeg  = measuredAngleDeg;          // positive = CW in Skia
            midAngleRad  = (-180f + measuredAngleDeg / 2f) * MathF.PI / 180f;
        }
        else
        {
            arcStartDeg  = 0f;
            arcSweepDeg  = -measuredAngleDeg;         // negative = CCW in Skia
            midAngleRad  = (-measuredAngleDeg / 2f) * MathF.PI / 180f;
        }

        canvas.DrawArc(ovalSmall, arcStartDeg, arcSweepDeg, false, _readoutArcPaint);

        // Label at midpoint of the arc
        float textR = arcRadius + 14f;
        float tx    = MathF.Cos(midAngleRad) * textR;
        float ty    = MathF.Sin(midAngleRad) * textR + _readoutFont.Size * 0.35f;

        canvas.DrawText($"{(int)MathF.Round(measuredAngleDeg)}°", tx, ty, SKTextAlign.Center, _readoutFont, _readoutTextPaint);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _mainVm.PropertyChanged -= OnMainVmPropertyChanged;
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
        _readoutArcPaint.Dispose();
        _readoutTextPaint.Dispose();
        _readoutFont.Dispose();
        _textPaint.Dispose();
        _textSelectedBorderPaint.Dispose();
        _textFont.Dispose();
    }
}
