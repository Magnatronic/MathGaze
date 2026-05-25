# Phase 3: Protractor - Research

**Researched:** 2026-05-25
**Domain:** SkiaSharp arc/tick rendering, line-line intersection math, WPF right-rail extension, Practice/Exam mode chip
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Two-state state machine: `(ToolMode.Protractor, DrawState.Idle)` → click line 1 highlights it → `(ToolMode.Protractor, DrawState.AnchorPlaced)` → click line 2 places protractor at intersection. Status bar: "Click 2nd line" in AnchorPlaced state.
- **D-02:** Parallel lines: error toast "Lines are parallel — pick two non-parallel lines", reset to Idle.
- **D-03:** Off-screen intersection: compute true intersection, clamp center to nearest canvas edge, baseline angle still aligned to line 1. Student nudges into view.
- **D-04:** Default radius in PDF-space points (not screen pixels) — visually ~150px at zoom=1.
- **D-05:** No resize control. Student zooms PDF view instead.
- **D-06:** `ProtractorObject` fields: `CenterXPt`, `CenterYPt` (PDF coords), `BaselineAngleDeg`, `RotationOffsetDeg`, `IsFlipped`, `Style` (Classic180 | Full360).
- **D-07:** No lock state in v1 — PROT-04 deferred.
- **D-08:** Right rail when protractor selected: rotate block (−5°/−1°/+1°/+5°, ≥56×56px), flip button, style toggle (Classic 180° / Full 360°, StepButtonStyle pattern), nudge block (same as other objects), delete button, undo/redo.
- **D-09:** No lock toggle in v1.
- **D-10:** Nudge moves `CenterXPt`/`CenterYPt` in PDF-space; `NudgeObjectCommand` reused.
- **D-11:** Readout = angle a student reads off the physical protractor at current orientation. Computed from `BaselineAngleDeg + RotationOffsetDeg` and angle between source lines.
- **D-12:** Readout renders inside protractor (arc + text, like `shared.jsx` `measuring` prop). NOT in right rail.
- **D-13:** Readout updates live on every rotation button press.
- **D-14:** Exam Mode (`IsPracticeMode = false`) hides readout. Arc and scale marks still drawn. Enforced in `GeometryLayerViewModel` by checking `IsPracticeMode`.
- **D-15:** Mode chip already in `TopBar.xaml` — it is already wired to `IsPracticeMode` and `ToggleModeCommand`. Phase 3 has no new XAML work for the chip itself; just verify binding works with protractor readout.

### Claude's Discretion
- Exact default radius in PDF points (target: visually ~150px at zoom=1.0)
- Intersection math implementation (line-line intersection formula)
- Exact clamping logic for off-screen intersection
- Toast/error display mechanism (reuse StatusMessage or a separate brief notification)
- SkiaSharp rendering details for protractor arc, scale marks, and readout text
- Undo entries: each rotate press = one undo entry (consistent with per-click undo, D-08 Phase 2)

### Deferred Ideas (OUT OF SCOPE)
- **PROT-04 Lock toggle** — deferred from v1 by user decision.
- **Protractor resize control** — deferred from v1 by user decision.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PROT-01 | Activate Protractor mode, click two lines, protractor auto-placed at intersection with baseline aligned to first line | State machine extension in ToolViewModel; line hit-test in placement flow; intersection math via existing `GeometryMath.TryLineIntersect` |
| PROT-02 | Rotate placed protractor ±1° and ±5° via right-rail buttons | New `RotateProtractorCommand` (RotationOffsetDeg += delta); RightRail ProtractorPanel with four StepButtonStyle-pattern buttons |
| PROT-03 | Flip protractor between inner (0→180° L→R) and outer (180→0°) scale | `IsFlipped` bool on ProtractorObject; flip button in right rail; rendering checks `IsFlipped` to reverse scale label order |
| PROT-05 | Choose between 180° classic and 360° full-circle style | `Style` enum (Classic180/Full360); two-button toggle in right rail using StepButtonStyle pattern |
| PROT-06 | Practice Mode shows live angle readout; Exam Mode hides it | `IsPracticeMode` already on `MainViewModel`; `GeometryLayerViewModel.Draw` checks it before rendering readout arc+text |
| SYS-04 | Toggle between Practice Mode and Exam Mode via chip in top bar | Already implemented in `TopBar.xaml` (Mode pill wired to `IsPracticeMode`/`ToggleModeCommand`); Phase 3 verifies protractor readout responds |
| SYS-05 | Mode indicator permanently visible in top bar | Already implemented in `TopBar.xaml`; Phase 3 verifies no regression |
</phase_requirements>

---

## Summary

Phase 3 builds on a fully established WPF + SkiaSharp + CommunityToolkit.Mvvm foundation. The codebase patterns for geometry objects, commands, hit testing, rendering, and right-rail UI are all confirmed working from Phase 2. Phase 3 is an extension phase: add `ProtractorObject` to the object model, extend `ToolViewModel`'s click state machine, add a new rendering branch in `GeometryLayerViewModel`, add a `ProtractorPanel` to `RightRail.xaml`, and wire the mode chip's effect on the readout.

The most technically novel work is the SkiaSharp protractor renderer: drawing 180 or 360 tick marks (1° minor, 5° intermediate, 10° major, 30° labelled), the arc body, center crosshair, and — in Practice Mode only — a small arc and text readout showing the measured angle. All of this is done via `canvas.Save()` → `canvas.Translate(cx, cy)` → `canvas.RotateDegrees(baselineAngle + rotationOffset)` → draw in local space → `canvas.Restore()`. This coordinate transform is the single most important implementation pattern to get right.

The line-line intersection math is already in `GeometryMath.TryLineIntersect` — it operates on `SKPoint` (screen pixels). For protractor placement, the intersection needs to be computed in PDF-point space (convert line endpoints to screen, intersect, convert result back to PDF coords), or the math can be done in PDF-point space directly with the same formula. The placement click handler must also determine which existing lines were clicked: this requires a new "line hit test returning the hit line" path in the click handler (not sub-point selection — the full object, type-checked as `LineObject`).

The ghost preview during protractor placement (before click 2) should render the protractor at 0.5 opacity following the cursor over candidate lines, as described in `docs/additional-screens.jsx` (`ProtractorPlacing`). This extends the existing `DrawGhostPreview` method in `PdfCanvasViewModel`.

**Primary recommendation:** Implement in this order: (1) ProtractorObject model, (2) placement state machine + intersection math, (3) SkiaSharp renderer, (4) right-rail ProtractorPanel, (5) mode chip binding verification. This mirrors the Phase 2 build order that proved reliable.

---

## Standard Stack

### Core (already installed — no new packages required)

| Library | Version | Purpose | Why |
|---------|---------|---------|-----|
| SkiaSharp | 3.119.2 | Hardware-accelerated canvas for protractor rendering | Already in use; `DrawArc`, `RotateDegrees`, `Translate`, `Save`/`Restore` cover all protractor drawing needs |
| SkiaSharp.Views.WPF | 3.119.2 | `SKElement` host control (WPF) | Already wired in `PdfCanvas.xaml` |
| CommunityToolkit.Mvvm | 8.4.2 | `[ObservableProperty]`, `[RelayCommand]` | All new VM properties and commands use this pattern |
| Microsoft.Extensions.DependencyInjection | 10.0.7 | DI container | `RotateProtractorCommand` et al. injected same as Phase 2 |

[VERIFIED: C:\Local Docs\Coding\MathGaze\MathGaze\MathGaze.csproj]

**Installation:** No new packages. All dependencies already present.

---

## Architecture Patterns

### Recommended Project Structure Changes

```
MathGaze/Core/Geometry/
├── GeometryObject.cs           (existing — abstract base)
├── PointObject.cs              (existing)
├── LineObject.cs               (existing)
├── CircleObject.cs             (existing)
└── ProtractorObject.cs         (NEW — extends GeometryObject)

MathGaze/Core/Commands/
├── IGeometryCommand.cs         (existing)
├── PlaceObjectCommand.cs       (existing — reuse for initial placement)
├── DeleteObjectCommand.cs      (existing — reuse)
├── NudgeObjectCommand.cs       (existing — reuse for nudge)
├── RotateProtractorCommand.cs  (NEW — RotationOffsetDeg += delta, undo = -= delta)
├── FlipProtractorCommand.cs    (NEW — IsFlipped toggle, undo = toggle again)
└── StyleProtractorCommand.cs   (NEW — Style enum swap, undo = swap back)

MathGaze/Core/
└── GeometryMath.cs             (existing — TryLineIntersect already present; add PDF-space wrapper)

MathGaze/ViewModels/
├── ToolViewModel.cs            (extend: add ToolMode.Protractor, AnchorLine field, click cases)
├── GeometryLayerViewModel.cs   (extend: add protractor draw case; check IsPracticeMode for readout)
├── RightRailViewModel.cs       (extend: add rotate/flip/style commands; Refresh() switch cases)
└── PdfCanvasViewModel.cs       (extend: extend DrawGhostPreview for Protractor tool)

MathGaze/Views/
├── RightRail.xaml              (extend: add ProtractorPanel StackPanel, visibility bound to SelectedObjectType == "Protractor")
└── ToolRail.xaml               (extend: wire stub Protractor button to ActivateProtractorCommand)
```

### Pattern 1: ProtractorObject Model

**What:** Extends `GeometryObject` with placement state and rendering parameters.
**When to use:** The canonical data model for all protractor instances.

```csharp
// Source: inferred from existing GeometryObject pattern + D-06 from CONTEXT.md
public sealed class ProtractorObject : GeometryObject
{
    public double CenterXPt           { get; set; }   // PDF coords
    public double CenterYPt           { get; set; }   // PDF coords
    public double BaselineAngleDeg    { get; set; }   // angle of line 1, degrees (0° = right, CCW positive per PDF convention)
    public double RotationOffsetDeg   { get; set; }   // user rotation, starts at 0
    public bool   IsFlipped           { get; set; }   // false = inner (0→180 L→R), true = outer (180→0 L→R)
    public ProtractorStyle Style      { get; set; } = ProtractorStyle.Classic180;

    // For computing the readout — stored references to the two source lines
    public Guid Line1Id               { get; init; }
    public Guid Line2Id               { get; init; }

    public ProtractorObject(double centerXPt, double centerYPt,
                            double baselineAngleDeg,
                            Guid line1Id, Guid line2Id)
    {
        CenterXPt       = centerXPt;
        CenterYPt       = centerYPt;
        BaselineAngleDeg = baselineAngleDeg;
        Line1Id         = line1Id;
        Line2Id         = line2Id;
    }
    // HitTest, Draw, GetSnapPoints — same pattern as other objects
}

public enum ProtractorStyle { Classic180, Full360 }
```

### Pattern 2: Placement State Machine Extension

**What:** Extend `ToolViewModel.HandleCanvasClick` switch with `Protractor` cases.
**When to use:** Two-click protractor placement.
**Critical detail:** Click 1 and click 2 must hit-test for `LineObject` specifically (not any geometry object). The existing `GeometryHitTester.TryHitObject` returns `GeometryObject?` — the placement handler type-checks the result as `LineObject`.

```csharp
// Source: existing ToolViewModel.cs pattern, extended per D-01/D-02/D-03
// In ToolViewModel — add AnchorLine field alongside AnchorPt
public LineObject? AnchorLine { get; private set; }  // line 1 selected

// In HandleCanvasClick switch:
case (ToolMode.Protractor, DrawState.Idle):
{
    var hit = GeometryHitTester.TryHitObject(screenPx, _geometryService.Objects, mapper);
    if (hit is LineObject line1)
    {
        AnchorLine = line1;
        DrawState  = DrawState.AnchorPlaced;
        StatusMessage = "Click 2nd line";
        GhostChanged?.Invoke(this, EventArgs.Empty);
    }
    break;
}

case (ToolMode.Protractor, DrawState.AnchorPlaced):
{
    var hit = GeometryHitTester.TryHitObject(screenPx, _geometryService.Objects, mapper);
    if (hit is not LineObject line2 || line2.Id == AnchorLine!.Id) break; // must be different line
    
    // Compute intersection in PDF point space
    if (!GeometryMath.TryLineIntersectPt(AnchorLine, line2, out var interPt))
    {
        StatusMessage = "Lines are parallel — pick two non-parallel lines";
        AnchorLine = null;
        DrawState  = DrawState.Idle;
        GhostChanged?.Invoke(this, EventArgs.Empty);
        break;
    }
    
    // Clamp if off-canvas (D-03) — clamp interPt to visible canvas bounds using mapper
    var clampedPt = ClampToCanvas(interPt, mapper);
    
    // Baseline angle = direction of line 1 in screen space, converted to degrees
    double baselineAngleDeg = ComputeBaselineAngleDeg(AnchorLine, mapper);
    
    var protractor = new ProtractorObject(
        clampedPt.xPt, clampedPt.yPt,
        baselineAngleDeg,
        AnchorLine.Id, line2.Id);
    
    _geometryService.ExecuteCommand(new PlaceObjectCommand(protractor));
    _geometryService.SetSelected(protractor.Id);
    ResetDrawState();
    StatusMessage = "Protractor placed";
    break;
}
```

### Pattern 3: SkiaSharp Protractor Renderer

**What:** Draw the protractor in `GeometryLayerViewModel.DrawObject`, using canvas transform to place and rotate.
**When to use:** Every frame when any `ProtractorObject` is in `_geometryService.Objects`.

**Key SkiaSharp APIs (all VERIFIED against official docs):**
- `canvas.Save()` / `canvas.Restore()` — scope coordinate transforms [VERIFIED: learn.microsoft.com]
- `canvas.Translate(cx, cy)` — move origin to protractor center [VERIFIED: learn.microsoft.com]
- `canvas.RotateDegrees(degrees)` — clockwise rotation (SkiaSharp's Y-axis is screen-down). Baseline angle needs sign adjustment for PDF-space vs screen-space Y flip [VERIFIED: learn.microsoft.com]
- `canvas.DrawArc(SKRect oval, float startAngle, float sweepAngle, bool useCenter, SKPaint)` — startAngle 0° = 3 o'clock, sweepAngle clockwise in degrees [VERIFIED: learn.microsoft.com/en-us/dotnet/api/skiasharp.skcanvas.drawarc]
- `canvas.DrawLine` / `canvas.DrawCircle` / `canvas.DrawText` — all used for ticks, center mark, readout

**Tick rendering approach (from `shared.jsx` reference):**
- 1° minor ticks: length 5px
- 5° intermediate ticks (a % 5 == 0 but % 10 != 0): length 9px
- 10° major ticks (a % 10 == 0): length 18px
- 30° labelled ticks (a % 30 == 0, but in practice 10° labels per design): draw numeric label
- Labels at radius - 32px from center, textAnchor: middle

```csharp
// Source: shared.jsx Protractor component + SkiaSharp docs
// In GeometryLayerViewModel.DrawProtractor(canvas, obj, mapper):
private void DrawProtractor(SKCanvas canvas, ProtractorObject obj, CoordinateMapper mapper, bool selected)
{
    var centerPx = mapper.PageToScreen(obj.CenterXPt, obj.CenterYPt);
    
    // Convert radius from PDF-pt to screen px
    // RadiusPt * Scale = screen radius. Store as const in ProtractorObject or derive from mapper.
    float radiusPx = (float)(ProtractorObject.DefaultRadiusPt * mapper.Scale);
    
    // PDF space Y is flipped vs screen Y. Angle in PDF space (CCW from right) →
    // screen angle = negate (CW from right in screen space = same visual if we handle this correctly).
    // Total rotation = BaselineAngleDeg + RotationOffsetDeg, negated for screen Y-flip.
    float totalRotDeg = -(float)(obj.BaselineAngleDeg + obj.RotationOffsetDeg);
    
    canvas.Save();
    canvas.Translate(centerPx.X, centerPx.Y);
    canvas.RotateDegrees(totalRotDeg);
    
    bool isFull = obj.Style == ProtractorStyle.Full360;
    int arcDeg = isFull ? 360 : 180;
    float startDeg = isFull ? 0f : -180f;  // for DrawArc: -180° = left side (9 o'clock)
    
    // Draw arc body (semicircle or full circle)
    var oval = new SKRect(-radiusPx, -radiusPx, radiusPx, radiusPx);
    canvas.DrawArc(oval, startDeg, arcDeg, false, selected ? _selectedPaint : _normalPaint);
    
    // Draw baseline for Classic180
    if (!isFull)
        canvas.DrawLine(-radiusPx, 0, radiusPx, 0, selected ? _selectedPaint : _normalPaint);
    
    // Draw tick marks (1° increments)
    for (int a = 0; a <= arcDeg; a++)
    {
        float angleDeg = startDeg + a;
        float angleRad = angleDeg * MathF.PI / 180f;
        bool isMajor = (a % 10 == 0);
        bool isMid   = (a % 5  == 0);
        float tickLen = isMajor ? 18f : isMid ? 9f : 5f;
        float r1 = radiusPx - tickLen, r2 = radiusPx;
        float cos = MathF.Cos(angleRad), sin = MathF.Sin(angleRad);
        canvas.DrawLine(cos * r1, sin * r1, cos * r2, sin * r2,
            isMajor ? _tickMajorPaint : _tickMinorPaint);
    }
    
    // Draw numeric labels every 10° (IsFlipped reverses label values)
    for (int a = 0; a <= arcDeg; a += 10)
    {
        float angleDeg = startDeg + a;
        float angleRad = angleDeg * MathF.PI / 180f;
        float lr = radiusPx - 32f;
        int labelValue = obj.IsFlipped ? (arcDeg - a) : a;
        canvas.DrawText(labelValue.ToString(),
            MathF.Cos(angleRad) * lr, MathF.Sin(angleRad) * lr,
            _labelPaint);
    }
    
    // Draw center crosshair
    canvas.DrawCircle(0, 0, 4f, _selectedPaint);
    canvas.DrawLine(-8f, 0, -3f, 0, _selectedPaint);
    canvas.DrawLine(3f, 0, 8f, 0, _selectedPaint);
    canvas.DrawLine(0, -8f, 0, -3f, _selectedPaint);
    canvas.DrawLine(0, 3f, 0, 8f, _selectedPaint);
    
    // Draw readout (Practice Mode only — checked by caller before this method)
    // ... see Pattern 4
    
    canvas.Restore();
}
```

**IMPORTANT label text rendering note:** `canvas.DrawText` in SkiaSharp renders with the text baseline at the given Y coordinate. For centered tick labels, set `_labelPaint.TextAlign = SKTextAlign.Center` and offset Y by half the text height (`paint.TextSize / 2`). [VERIFIED: learn.microsoft.com SKPaint.TextAlign]

### Pattern 4: Practice Mode Readout Rendering

**What:** In Practice Mode, render a small arc from baseline to the measured angle + numeric text.
**Follows:** `shared.jsx` `measuring` prop pattern exactly.

```csharp
// Source: shared.jsx Protractor component, measuring block
// Called inside DrawProtractor when IsPracticeMode == true
// measuredAngle = the angle student would read off the protractor
private void DrawReadout(SKCanvas canvas, float measuredAngleDeg, float radiusPx)
{
    float arcRadius = 40f; // fixed inner arc radius (from shared.jsx)
    var ovalSmall = new SKRect(-arcRadius, -arcRadius, arcRadius, arcRadius);
    
    // Arc from 0 (baseline) to -measuredAngleDeg (negative because SkiaSharp CW,
    // but we want CCW arc from baseline going "up" into the protractor body)
    canvas.DrawArc(ovalSmall, 0f, -measuredAngleDeg, false, _readoutArcPaint);
    
    // Numeric label at midpoint angle of the arc
    float midAngleRad = (-measuredAngleDeg / 2f) * MathF.PI / 180f;
    float textR = 55f;
    canvas.DrawText($"{(int)MathF.Round(measuredAngleDeg)}°",
        MathF.Cos(midAngleRad) * textR,
        MathF.Sin(midAngleRad) * textR,
        _readoutTextPaint);
}
```

### Pattern 5: New Commands (following D-09 command pattern)

```csharp
// Source: existing NudgeObjectCommand.cs pattern
public sealed class RotateProtractorCommand : IGeometryCommand
{
    private readonly Guid  _id;
    private readonly double _deltaDeg;
    public RotateProtractorCommand(Guid id, double deltaDeg) { _id = id; _deltaDeg = deltaDeg; }
    public void Execute(IGeometryService s) => RotateProtractor(s,  _deltaDeg);
    public void Undo   (IGeometryService s) => RotateProtractor(s, -_deltaDeg);
    private static void RotateProtractor(IGeometryService s, double delta)
    {
        if (s.Objects.FirstOrDefault(o => o.Id == _id) is ProtractorObject p)
            p.RotationOffsetDeg += delta;
    }
}
// FlipProtractorCommand and StyleProtractorCommand follow the same template
```

**IGeometryService.NudgeObject** must handle `ProtractorObject` — add a `case ProtractorObject p:` branch to `GeometryService.NudgeObject` that updates `CenterXPt`/`CenterYPt`.

### Pattern 6: Right Rail ProtractorPanel Visibility

**What:** Conditionally show the `ProtractorPanel` in `RightRail.xaml`.
**Uses:** Existing `SelectedObjectType` string property on `RightRailViewModel`. Add `"Protractor"` case.

```xaml
<!-- In RightRail.xaml — new ProtractorPanel, inside the selection panel StackPanel -->
<StackPanel x:Name="ProtractorPanel">
    <StackPanel.Style>
        <Style TargetType="StackPanel">
            <Setter Property="Visibility" Value="Collapsed"/>
            <Style.Triggers>
                <DataTrigger Binding="{Binding SelectedObjectType}" Value="Protractor">
                    <Setter Property="Visibility" Value="Visible"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </StackPanel.Style>
    
    <!-- Rotate block: −5°, −1°, +1°, +5° (each 56x56px, RailButtonStyle) -->
    <UniformGrid Rows="2" Columns="2" Margin="0,0,0,6">
        <Button Width="56" Height="56" Style="{StaticResource RailButtonStyle}"
                Command="{Binding RotateMinus5Command}" Content="−5°"/>
        <Button Width="56" Height="56" Style="{StaticResource RailButtonStyle}"
                Command="{Binding RotateMinus1Command}" Content="−1°"/>
        <Button Width="56" Height="56" Style="{StaticResource RailButtonStyle}"
                Command="{Binding RotatePlus1Command}"  Content="+1°"/>
        <Button Width="56" Height="56" Style="{StaticResource RailButtonStyle}"
                Command="{Binding RotatePlus5Command}"  Content="+5°"/>
    </UniformGrid>
    
    <!-- Flip scale button -->
    <Button Height="56" HorizontalAlignment="Stretch" Margin="0,0,0,6"
            Style="{StaticResource RailButtonStyle}"
            Command="{Binding FlipScaleCommand}" Content="Flip scale"/>
    
    <!-- Style toggle: Classic 180° / Full 360° (StepButtonStyle pattern) -->
    <UniformGrid Rows="1" Columns="2" Margin="0,0,0,10">
        <Button Height="40" Command="{Binding SetStyleClassicCommand}" Content="180°">
            <Button.Style>
                <Style TargetType="Button" BasedOn="{StaticResource StepButtonStyle}">
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding IsStyleClassic}" Value="True">
                            <Setter Property="Tag" Value="active"/>
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </Button.Style>
        </Button>
        <Button Height="40" Command="{Binding SetStyleFullCommand}" Content="360°">
            <Button.Style>
                <Style TargetType="Button" BasedOn="{StaticResource StepButtonStyle}">
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding IsStyleFull}" Value="True">
                            <Setter Property="Tag" Value="active"/>
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </Button.Style>
        </Button>
    </UniformGrid>
    
    <!-- Separator, then nudge block (existing nudge XAML reused) -->
</StackPanel>
```

### Anti-Patterns to Avoid

- **Allocating SKPaint inside DrawProtractor:** Per established pattern (STATE.md: "SKPaint cache: declare readonly SKPaint fields in GeometryLayerViewModel"), never `new SKPaint()` per frame. Declare `_tickMajorPaint`, `_tickMinorPaint`, `_labelPaint`, `_readoutArcPaint`, `_readoutTextPaint` as readonly fields with object initializer syntax. Add them to `Dispose()`.
- **Doing intersection math in screen pixels then storing:** Always compute the final `CenterXPt`/`CenterYPt` in PDF-point space before storing. Screen-space intersection is fine for the math, but convert back to PDF pts before creating `ProtractorObject`.
- **Using `DrawText` with default alignment for tick labels:** SkiaSharp `DrawText` positions text from the baseline at (x, y). For centered-over-tick labels, set `TextAlign = SKTextAlign.Center` and account for vertical centering by adding `paint.TextSize * 0.4f` to Y.
- **Forgetting canvas.Save()/Restore():** The rotation transform must be scoped. Without Save/Restore, subsequent objects render in rotated coordinates.
- **Checking IsPracticeMode inside ProtractorObject:** Mode awareness belongs in the renderer (`GeometryLayerViewModel`), not the model. The model stores data; the renderer decides what to draw.
- **Wiring the Protractor tool button in XAML:** Per established pattern (STATE.md: "Code-behind event wiring: ToolRail Protractor button wired in code-behind"), the Protractor button command binding is done in `ToolRail.xaml.cs`, not XAML `Command={}`. Follow the existing pattern for Select/Point/Line/Circle buttons.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Line-line intersection math | Custom formula | `GeometryMath.TryLineIntersect` (already in codebase) | Already tested and handles parallel/degenerate cases; just add a PDF-point wrapper |
| Undo/redo for rotate/flip/style | Custom stack | `IGeometryService.ExecuteCommand(new RotateProtractorCommand(...))` | Same pattern as all other mutations; free undo/redo |
| Nudge for protractor center | New implementation | `NudgeObjectCommand` + add ProtractorObject case to `GeometryService.NudgeObject` | D-10 says reuse existing nudge command |
| Canvas rotation transform | Manual trig everywhere | `canvas.Save()` → `canvas.RotateDegrees()` → draw in local space → `canvas.Restore()` | This is the standard SkiaSharp transform pattern; avoids per-tick trig for object body orientation |
| Hit testing protractor | Custom arc hit test | Simple distance-to-center + radius check (similar to circle hit test) | Protractor is roughly circular; 10px ring tolerance like CircleObject is sufficient |
| Mode chip UI | New chip widget | `IsPracticeMode` binding in `TopBar.xaml` is already implemented (verified in TopBar.xaml line 88-124) | Nothing new needed; Phase 3 only uses the existing binding to drive readout visibility |

**Key insight:** The protractor renderer is the only genuinely new technical work in this phase. Everything else is an extension of established Phase 2 patterns.

---

## Intersection Math — Implementation Detail

`GeometryMath.TryLineIntersect` takes four `SKPoint` arguments (screen pixels). A PDF-space wrapper is needed:

```csharp
// Add to GeometryMath.cs
/// <summary>
/// Find intersection of two LineObjects in PDF-point space.
/// Returns true and sets pt to intersection (in PDF points). False if parallel.
/// </summary>
public static bool TryLineIntersectPt(LineObject a, LineObject b, out (double xPt, double yPt) pt)
{
    // Work in PDF coords directly — same formula, just double not float
    double dx1 = a.X2Pt - a.X1Pt, dy1 = a.Y2Pt - a.Y1Pt;
    double dx2 = b.X2Pt - b.X1Pt, dy2 = b.Y2Pt - b.Y1Pt;
    double denom = dx1 * dy2 - dy1 * dx2;
    pt = default;
    if (Math.Abs(denom) < 1e-9) return false;
    double t = ((b.X1Pt - a.X1Pt) * dy2 - (b.Y1Pt - a.Y1Pt) * dx2) / denom;
    pt = (a.X1Pt + t * dx1, a.Y1Pt + t * dy1);
    return true;
}
```

**BaselineAngle calculation:** The angle of line 1 in PDF-point space, measured from the positive X axis (east), CCW positive (standard math convention). In screen space, Y is flipped, but because we rotate the canvas with the Y-flip already baked into the mapper, the angle must be computed in PDF space:

```csharp
// Angle of LineObject in PDF-space (degrees, CCW from East = 0)
private static double ComputeBaselineAngleDeg(LineObject line)
{
    double dx = line.X2Pt - line.X1Pt;
    double dy = line.Y2Pt - line.Y1Pt;  // PDF Y: up = positive
    return Math.Atan2(dy, dx) * 180.0 / Math.PI;
}
```

When rendering, screen Y is inverted from PDF Y. The `canvas.RotateDegrees` call is clockwise in screen space. To align the protractor baseline to the visual direction of line 1 on screen:
- Compute the line's angle in screen coordinates: `screenAngle = Math.Atan2(screenDy, screenDx)`  
- Use `canvas.RotateDegrees((float)screenAngleDeg)` after `canvas.Translate(centerPx)`
- Store `BaselineAngleDeg` as the screen-space angle so RotationOffset can be added directly

**Recommendation for Claude's Discretion item:** Store `BaselineAngleDeg` as the screen-space angle (degrees CW from right). This simplifies the renderer since SkiaSharp's angle system is CW from right. Compute it at placement time using the mapper to convert line endpoints to screen pixels, then `Math.Atan2`.

---

## Default Radius — Implementation Detail

**Claude's Discretion: recommended value = 108 PDF points**

Derivation:
- At zoom=1.0, DPI=96: `Scale = (1.0 × 96/72) × 1.0 = 1.333` screen px/pt
- 108 pt × 1.333 = 144 screen px
- At DPI scale 1.5 (144 DPI, typical HiDPI): 108 pt × 2.0 = 216 px — good for HiDPI
- At zoom=0.75: 108 pt × 1.0 = 108 px — still readable
- At zoom=1.5: 108 pt × 2.0 = 216 px — comfortable

Store as `const double DefaultRadiusPt = 108.0` on `ProtractorObject`. [ASSUMED — target is "~150px at zoom=1.0" per D-04; 108pt × 1.333 ≈ 144px is the closest clean integer]

---

## Canvas Clamping — Implementation Detail

**Claude's Discretion: clamp intersection to visible page rect, not canvas rect**

The visible page bounds in PDF-point space are `[0, pageWidthPt] × [0, pageHeightPt]`. The intersection point from `TryLineIntersectPt` is already in PDF-point space. Clamp:

```csharp
private static (double xPt, double yPt) ClampToPageBounds(
    (double xPt, double yPt) pt, double pageWidthPt, double pageHeightPt)
{
    double margin = 20.0; // 20pt margin from page edge so protractor is partially visible
    double x = Math.Clamp(pt.xPt, margin, pageWidthPt - margin);
    double y = Math.Clamp(pt.yPt, margin, pageHeightPt - margin);
    return (x, y);
}
```

The mapper provides page dimensions via `IPdfService.GetPageDimensionsPt(pageIndex)`. The placement handler already has access to the service via `IGeometryService`, but page dimensions require `IPdfService`. **Resolution:** Pass page dimensions to the placement handler, or inject `IPdfService` into `ToolViewModel`. The simpler route is to add `ProtractorPlacementHelper` as a static method taking page dimensions as parameters.

---

## Toast / Error Display — Implementation Detail

**Claude's Discretion: reuse `StatusMessage` property on `ToolViewModel`**

`StatusMessage` is already bound to the status display in `PdfCanvas.xaml.cs` (confirmed in `PdfCanvasViewModel.ToolVmStatusMessage` and wired in code-behind). For the parallel-lines error, set `StatusMessage = "Lines are parallel — pick two non-parallel lines"` and reset draw state. This is visible to the user immediately and is already the established error feedback mechanism. No new toast widget needed.

---

## Common Pitfalls

### Pitfall 1: Screen Y-axis flip in angle computation
**What goes wrong:** Developer computes `BaselineAngleDeg` from PDF-space coordinates using `Math.Atan2(dy, dx)` where `dy = Y2Pt - Y1Pt`. In PDF space Y increases upward, but on screen Y increases downward. The angle appears correct in PDF space but the protractor renders 180° rotated from where the line visually points.
**Why it happens:** PDF Y is opposite to SkiaSharp Y. A line pointing visually "up-right" on screen has a positive screen `dy` (down-right in screen coords) but negative PDF `dy` (because Y2Pt > Y1Pt means higher on page = visually up).
**How to avoid:** Compute the baseline angle using the mapper: convert both line endpoints to screen pixels, then `Math.Atan2(screenDy, screenDx)` where `screenDy = p2.Y - p1.Y` (down = positive). This is the CW-from-right angle that SkiaSharp's `RotateDegrees` expects directly.
**Warning signs:** Protractor baseline is perpendicular or opposite to the drawn line.

### Pitfall 2: SKPaint allocated per frame
**What goes wrong:** `new SKPaint { ... }` inside `DrawProtractor` creates GC pressure at 60 FPS. For 180 ticks × 60 FPS = 10,800 SKPaint allocations/sec.
**Why it happens:** Easy to write; fine for occasional drawing, fatal for per-frame rendering.
**How to avoid:** All `SKPaint` objects as readonly fields in `GeometryLayerViewModel`, initialized once. Existing code enforces this pattern — follow it.
**Warning signs:** Gradual memory growth; GC pauses causing stutter during protractor interaction.

### Pitfall 3: canvas.Save()/Restore() scope error
**What goes wrong:** Forgetting `canvas.Save()` before translate/rotate means the transform accumulates. Second protractor object renders relative to the first one's transform.
**Why it happens:** SkiaSharp canvas transforms are cumulative until restored.
**How to avoid:** Always bracket per-object drawing in `canvas.Save()` / `canvas.Restore()`. Existing `DrawGhostPreview` already uses `using` and paint scoping — apply the same discipline to the transform.
**Warning signs:** Second placed protractor appears at wrong position; rotating one protractor appears to affect another.

### Pitfall 4: Line hit detection during protractor placement overlaps existing selection
**What goes wrong:** In `ToolMode.Protractor, DrawState.AnchorPlaced`, click dispatches to the same `HandleSelectClick` path if not handled before the switch default.
**Why it happens:** The switch statement in `HandleCanvasClick` only handles known `(tool, state)` pairs. Unhandled combinations fall through.
**How to avoid:** Explicitly handle both `Protractor` cases in the switch. They must be before any fallthrough. The click must NOT update selection state during protractor placement — hitting line 1 again accidentally should be ignored (same line check: `if (hit.Id == AnchorLine.Id) break`).
**Warning signs:** Clicking on the first selected line during placement deselects it; protractor placement resets unexpectedly.

### Pitfall 5: DrawText vertical alignment for tick labels
**What goes wrong:** Tick labels appear above or below the expected position. SkiaSharp's `DrawText` places the text baseline (not center) at the given Y coordinate.
**Why it happens:** SVG `dominantBaseline="central"` (used in `shared.jsx`) is not the default SkiaSharp behavior.
**How to avoid:** Use `_labelPaint.TextAlign = SKTextAlign.Center` for horizontal centering. For vertical: add `_labelPaint.TextSize * 0.35f` to the Y coordinate to approximate visual centering.
**Warning signs:** Labels visually displaced upward from expected position.

### Pitfall 6: GeometryService.NudgeObject missing ProtractorObject case
**What goes wrong:** Clicking nudge buttons on a selected protractor does nothing (silent no-op from unmatched switch).
**Why it happens:** `GeometryService.NudgeObject` has explicit cases for `PointObject`, `LineObject`, `CircleObject` only.
**How to avoid:** Add `case ProtractorObject p: p.CenterXPt += dxPt; p.CenterYPt += dyPt; break;` to `GeometryService.NudgeObject` in the same PR as `ProtractorObject`.
**Warning signs:** Nudge buttons appear to work (command fires, undo stack gets entry) but protractor doesn't move.

---

## Code Examples

### SkiaSharp DrawArc signature (verified)
```csharp
// Source: [VERIFIED: learn.microsoft.com/en-us/dotnet/api/skiasharp.skcanvas.drawarc]
// startAngle: degrees, 0° = 3 o'clock (right), positive = clockwise
// sweepAngle: degrees, positive = clockwise
// useCenter: false for open arc, true for pie slice
canvas.DrawArc(
    oval:       new SKRect(-r, -r, r, r),
    startAngle: -180f,   // start at 9 o'clock (left end of protractor baseline)
    sweepAngle: 180f,    // sweep 180° clockwise = top of semicircle
    useCenter:  false,
    paint:      _normalPaint);
```

### Canvas Save/Rotate/Restore pattern (verified)
```csharp
// Source: [VERIFIED: learn.microsoft.com/en-us/dotnet/api/skiasharp.skcanvas]
canvas.Save();
canvas.Translate(centerPx.X, centerPx.Y);
canvas.RotateDegrees(rotationAngleDeg);  // CW positive in SkiaSharp
// ... draw in local space with (0,0) = protractor center ...
canvas.Restore();
```

### IsPracticeMode check in renderer
```csharp
// In GeometryLayerViewModel.DrawObject, after drawing protractor body:
// IsPracticeMode must be injected or passed — GeometryLayerViewModel needs access.
// Solution: inject MainViewModel or expose IsPracticeMode via IGeometryService
// Simplest: add bool IsPracticeMode { get; } to a new interface or pass directly.
// Recommended: constructor-inject MainViewModel into GeometryLayerViewModel.
if (_mainVm.IsPracticeMode && obj is ProtractorObject p)
    DrawReadout(canvas, ComputeMeasuredAngle(p), radiusPx);
```

**Injection change required:** `GeometryLayerViewModel` currently takes only `IGeometryService`. Phase 3 adds a `MainViewModel` constructor parameter to read `IsPracticeMode`. Register the dependency in DI setup (already has `MainViewModel` as singleton).

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `SKXamlCanvas` (WinUI 3) | `SKElement` (WPF) | Phase 1 decision | Use `PaintSurface` event wired in code-behind only (not XAML) |
| Manual DrawArc via path | `canvas.DrawArc(SKRect, startAngle, sweepAngle, ...)` | N/A | Direct API, no path construction needed for simple arcs |

**No deprecated APIs involved.** SkiaSharp 3.119.2 is current as of project start. [VERIFIED: MathGaze.csproj]

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Default radius of 108 PDF points (~144px at zoom=1.0, 96 DPI) is visually comfortable | Default Radius section | Protractor may appear too large or too small; can be adjusted as a constant before Phase 3 ships |
| A2 | BaselineAngle should be computed in screen-space (CW from right) at placement time, not PDF-space | Intersection Math section | Protractor baseline misaligned with line; rendering fix is a one-line sign change |
| A3 | `StatusMessage` is sufficient for the parallel-lines error (no separate toast widget needed) | Toast/Error section | User may not notice the status message if it auto-clears; can upgrade to a visible overlay in Phase 4 if needed |
| A4 | Line1Id/Line2Id stored on ProtractorObject to compute the measured angle at render time | ProtractorObject model | If lines are deleted after protractor is placed, readout would need fallback (show 0° or hide). Acceptable for v1. |
| A5 | GeometryLayerViewModel needs MainViewModel injected to read IsPracticeMode | Code Examples section | Alternative: subscribe to ObjectsChanged on GeometryService and cache IsPracticeMode separately. Either works. |

---

## Open Questions

1. **Does ToolViewModel need IPdfService to clamp the intersection to page bounds?**
   - What we know: `ToolViewModel` is currently injected with only `IGeometryService`. Page dimensions come from `IPdfService`.
   - What's unclear: Whether to inject `IPdfService` into `ToolViewModel` or use a different approach.
   - Recommendation: Pass page dimensions as parameters from `PdfCanvasViewModel.HandleCanvasClick` down to the protractor placement handler. `PdfCanvasViewModel` already holds `IPdfService` via the constructor. Avoids changing `ToolViewModel`'s constructor signature significantly.

2. **How does the ghost protractor look during click 2 (AnchorPlaced state)?**
   - What we know: `docs/additional-screens.jsx` `ProtractorPlacing` shows a ghost protractor at 0.5 opacity at the cursor, with a snap ring at the target vertex (which was the Phase 2 snap-to-vertex pattern, different from Phase 3's click-on-line pattern).
   - What's unclear: Should the ghost track the cursor freely, or should it snap to line intersections as the cursor moves?
   - Recommendation: Ghost tracks the cursor freely (no snap ring during placement). The status message "Click 2nd line" provides guidance. This is simpler and avoids computing tentative intersections on every MouseMove.

3. **What is the exact measured angle the readout should display?**
   - What we know: D-11 says "angle value a student would read off a physical protractor at the current orientation — where the second arm crosses the protractor scale."
   - What's unclear: Which of the two possible intersection angles (supplementary angles) is shown? Physical protractors read the acute angle or the angle as approached from the baseline.
   - Recommendation: Compute the angle between the two source lines using `Math.Atan2` on their direction vectors. If the result > 180°, use 360° - result. Then apply the IsFlipped logic to select inner vs outer reading. Show the positive acute or obtuse angle (0–180°). For 360° style, show the actual bearing.

---

## Environment Availability

Step 2.6: SKIPPED — Phase 3 is a pure code extension with no new external dependencies. All tools, runtimes, and libraries are already present from Phase 2. [VERIFIED: MathGaze.csproj]

---

## Validation Architecture

`workflow.nyquist_validation` is explicitly `false` in `.planning/config.json`. This section is omitted per workflow rules.

---

## Security Domain

Phase 3 introduces no new attack surface: no file I/O, no network, no user-supplied strings executed as code. The angle readout is a computed numeric value rendered via `canvas.DrawText` — no injection risk. ASVS assessment not required for this phase.

---

## Sources

### Primary (HIGH confidence)
- `C:\Local Docs\Coding\MathGaze\MathGaze\MathGaze.csproj` — verified SkiaSharp 3.119.2, CommunityToolkit.Mvvm 8.4.2
- `C:\Local Docs\Coding\MathGaze\MathGaze\Core\GeometryMath.cs` — `TryLineIntersect` already exists
- `C:\Local Docs\Coding\MathGaze\MathGaze\ViewModels\GeometryLayerViewModel.cs` — SKPaint cache pattern confirmed
- `C:\Local Docs\Coding\MathGaze\MathGaze\ViewModels\ToolViewModel.cs` — state machine pattern confirmed
- `C:\Local Docs\Coding\MathGaze\MathGaze\Styles\AppStyles.xaml` — `RailButtonStyle`, `StepButtonStyle`, `DeleteButtonStyle` confirmed
- `C:\Local Docs\Coding\MathGaze\docs\shared.jsx` — `Protractor` SVG component: tick lengths, label positions, measuring readout arc, ModePill
- `C:\Local Docs\Coding\MathGaze\MathGaze\Views\TopBar.xaml` — Mode chip already implemented (lines 83-124)
- [VERIFIED: learn.microsoft.com/en-us/dotnet/api/skiasharp.skcanvas.drawarc] — `DrawArc` signature, degrees CW from 3 o'clock

### Secondary (MEDIUM confidence)
- [learn.microsoft.com/en-us/dotnet/api/skiasharp.skcanvas] — `Save`, `Restore`, `RotateDegrees`, `Translate` confirmed
- `C:\Local Docs\Coding\MathGaze\.planning\STATE.md` — accumulated decisions: SKPaint cache pattern, code-behind wiring pattern, NudgeObject command pattern

### Tertiary (LOW confidence)
- None — all claims either verified in codebase or in official docs.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — packages confirmed in .csproj; no new dependencies
- Architecture: HIGH — all patterns directly verified in existing codebase
- SkiaSharp rendering: HIGH — DrawArc/RotateDegrees/Save/Restore verified in official docs
- Pitfalls: HIGH — derived directly from codebase analysis and established project decisions

**Research date:** 2026-05-25
**Valid until:** 2026-07-25 (stable stack; SkiaSharp 3.x API is stable)
