# Phase 2: Geometry Core - Research

**Researched:** 2026-05-02
**Domain:** WPF + SkiaSharp 2D geometry object model, hit testing, snap system, undo/redo command pattern
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Ghost preview for 2-click drawing: filled accent dot + outer ring at anchor, dashed preview line/arc from anchor to cursor. Bottom toast: "Click 2nd point" (with snap context if snap candidate active).
- **D-02:** Circle ghost: after click 1 (center), ghost circle whose radius equals distance from center to cursor. Updates live as cursor moves.
- **D-03:** Whole-object translate when no sub-point selected.
- **D-04:** Line endpoint sub-selection: both endpoints render as large tap targets (≥56×56px hit area). Tapping endpoint sub-selects it; UDLR nudges that endpoint only. Tapping elsewhere on canvas clears sub-selection.
- **D-05:** Circle sub-selection: center dot and edge point as tap targets. Center = translate whole circle. Edge point = change radius only.
- **D-06:** Point object: no sub-points. Nudge always moves it.
- **D-07:** Right rail reflects sub-selection state: "Move endpoint A" / "Move endpoint B" label in nudge block when sub-point active.
- **D-08:** Per-click undo. Every discrete action is one undo entry. No time-window batching.
- **D-09:** Command pattern: `IGeometryCommand` with Execute/Undo. All geometry mutations go through the command stack.
- **D-10:** Geometry positions stored in PDF point coordinates. CoordinateMapper converts to screen pixels for rendering and hit-testing.
- **D-11:** Fix `dpiScale = 1.0` hardcode in `PdfCanvasViewModel.EnsureCoordinateMapper()`. Wire real `PixelsPerDip` from `VisualTreeHelper.GetDpi()`.

### Claude's Discretion

- Hit-test tolerance buffer around lines (recommend 8–10px radius for gaze accuracy)
- Snap proximity threshold (recommend 20px screen pixels)
- Exact accent dot size for anchor and sub-selected endpoint indicators
- Visual style (stroke width, opacity) for ghost preview and committed geometry objects
- Whether orientation guide snaps (V/H/45°) show a faint guide line across the canvas or just affect snap behaviour

### Deferred Ideas (OUT OF SCOPE)

- Full Pivot Picker from HANDOFF (start/mid/end adaptive pivot with SVG preview)
- Snap Orientation row (V/H/45°/Free buttons in right rail)
- Protractor CTA in right rail when line is selected
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| GEOM-01 | User can place a Point with one click | GeometryObject model + single-click commit pattern in ToolStateMachine |
| GEOM-02 | User can draw a Line with two clicks (click start → click end) | Two-click state machine with ghost preview (D-01) |
| GEOM-03 | User can draw a Circle with two clicks (click centre → click radius point) | Two-click state machine with live ghost circle (D-02) |
| GEOM-04 | User can select any geometry object with one click | Hit-test pass in Select mode; object ownership by GeometryService |
| GEOM-05 | User can nudge a selected object using step controls (1/5/20 px) | NudgeCommand via IGeometryCommand; CoordinateMapper for unit conversion |
| GEOM-06 | User can delete a selected object via a right-rail action | DeleteCommand via IGeometryCommand; right-rail delete button |
| GEOM-07 | User can snap new points to endpoints, line-line intersections, orientation guides | SnapEngine computing candidates from geometry list; visual snap ring |
| SYS-01 | User can undo any action and redo previously undone actions | UndoService with two Stack<IGeometryCommand>; per-click entry (D-08/D-09) |
</phase_requirements>

---

## Summary

Phase 2 builds a complete geometry editing layer on top of the Phase 1 PDF canvas. The foundation is already correct — `CoordinateMapper` handles PDF↔screen transformation, `PdfCanvasViewModel.Paint()` is the single draw call, and all WPF pointer events flow through code-behind to avoid SkiaSharp XAML binding issues. The geometry layer slots in as a new `GeometryService` singleton + `GeometryLayerViewModel` that draws after the PDF bitmap in `Paint()`.

The two main technical domains are: (1) the object model and tool state machine — which is straightforward given the locked decisions, and (2) the snap engine — which requires careful attention to coordinate space (always work in screen pixels for snap proximity, but store in PDF points). Hit-testing lines requires point-to-segment distance math; hit-testing circles requires distance-from-center vs. radius math. Neither needs an external library.

The command pattern for undo/redo is a standard double-stack implementation with `IGeometryCommand.Execute()` / `IGeometryCommand.Undo()`. The main non-obvious detail is that nudge commands receive the step in screen pixels but must convert to PDF points before storing the delta, so the object moves the correct physical distance regardless of zoom at undo time.

**Primary recommendation:** Implement `GeometryService` as a singleton that owns the `List<GeometryObject>` and the `UndoService`. All mutations flow through `ExecuteCommand(IGeometryCommand)`. The `GeometryLayerViewModel` is a thin rendering adapter that reads from `GeometryService` and receives the `SKCanvas` from `PdfCanvasViewModel.Paint()`.

---

## Project Constraints (from CLAUDE.md)

| Directive | How it affects Phase 2 |
|-----------|----------------------|
| No drag gestures. Every action click-to-commit. Max 2 clicks per primitive. | Tool state machine: max states = 0 (idle) → 1 (anchor placed) → commit. No intermediate drag states. |
| Every interactive element ≥56×56px at 1× density | Sub-point tap targets (D-04/D-05) must have 56px hit radius in logical WPF units, not screen px |
| All input treated as standard Windows pointer events | Use `MouseDown`/`MouseMove` on `PdfCanvas.xaml.cs`; no touch-specific APIs |
| WPF + SkiaSharp + .NET 9 | `SKCanvas.DrawLine`, `DrawCircle`, `DrawOval`, `DrawPath` for all geometry; `SKPathEffect.CreateDash` for ghost previews |
| PDF as bitmap background; geometry as vector layer on top; UI overlay above both | Geometry draw calls happen after `canvas.DrawBitmap(_pageBitmap, destRect)` and before `canvas.Flush()` |
| No flyouts, no popups, no secondary HWNDs — all UI in-window panels | Right rail is a WPF `UserControl` in the existing column; no popups |
| GSD Workflow Enforcement | All file edits through GSD execute-phase |

---

## Standard Stack

### Core (already installed — no new packages needed for geometry model)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| SkiaSharp | 3.119.2 (installed) | Geometry rendering on SKCanvas | Already in project; `DrawLine`, `DrawCircle`, `DrawOval`, `DrawPath`, `SKPathEffect.CreateDash` cover all Phase 2 drawing needs |
| SkiaSharp.Views.WPF | 3.119.2 (installed) | SKElement host in WPF | Already in project; `MouseDown`/`MouseMove` events on `SKElement` available via standard WPF routed events |
| CommunityToolkit.Mvvm | 8.4.2 (installed) | `[ObservableProperty]`, `[RelayCommand]` for right-rail VM | Already in project |
| System.Text.Json | Inbox .NET 9 | Not needed Phase 2 (serialisation deferred to Phase 4) | — |

**No new NuGet packages required for Phase 2.** [VERIFIED: MathGaze.csproj]

### Supporting (Claude's discretion — no new installs)

All geometry math (point-to-line distance, line-line intersection, circle hit test) is pure arithmetic implementable in ~30 lines each. These go in `MathGaze.Core` alongside `CoordinateMapper`.

---

## Architecture Patterns

### Recommended Project Structure (additions to existing)

```
MathGaze/
├── Core/
│   ├── CoordinateMapper.cs            [EXISTS — extend with DPI fix]
│   ├── Geometry/
│   │   ├── GeometryObject.cs          [abstract base]
│   │   ├── PointObject.cs
│   │   ├── LineObject.cs
│   │   └── CircleObject.cs
│   ├── Commands/
│   │   ├── IGeometryCommand.cs
│   │   ├── PlaceObjectCommand.cs
│   │   ├── DeleteObjectCommand.cs
│   │   ├── NudgeObjectCommand.cs
│   │   └── NudgeEndpointCommand.cs
│   ├── GeometryHitTester.cs           [point-to-object distance math]
│   ├── SnapEngine.cs                   [snap candidate computation]
│   └── GeometryMath.cs                [line intersection, distance]
├── Services/
│   ├── IGeometryService.cs
│   ├── GeometryService.cs             [owns List<GeometryObject> + UndoService]
│   └── UndoService.cs                 [two Stack<IGeometryCommand>]
├── ViewModels/
│   ├── GeometryLayerViewModel.cs      [renders objects to SKCanvas]
│   ├── RightRailViewModel.cs          [selection state, nudge step, sub-point]
│   └── ToolViewModel.cs               [active tool, in-progress state]
├── Views/
│   ├── RightRail.xaml + .cs           [replaces RightRailPlaceholder]
│   └── PdfCanvas.xaml.cs              [add MouseDown/MouseMove wiring]
```

### Pattern 1: Tool State Machine (ToolViewModel)

**What:** A simple enum-driven state machine tracking the current tool and how many clicks have been committed for the in-progress object.

**When to use:** Every canvas click routes through here before being dispatched.

**States:**
- `Idle` — no tool active, canvas in Select mode
- `PointReady` — Point tool active, waiting for click 1
- `LineAnchorPlaced(SKPoint anchor)` — Line tool, click 1 done; ghost preview active
- `CircleAnchorPlaced(SKPoint center)` — Circle tool, click 1 done; ghost circle active

**Example:**
```csharp
// Source: phase design — this is the recommended implementation pattern [ASSUMED]
public enum ToolMode { Select, Point, Line, Circle }
public enum DrawState { Idle, AnchorPlaced }

public partial class ToolViewModel : ObservableObject
{
    [ObservableProperty] private ToolMode _activeTool = ToolMode.Select;
    [ObservableProperty] private DrawState _drawState = DrawState.Idle;

    // In-progress anchor stored in PDF point coordinates (D-10)
    public (double xPt, double yPt)? AnchorPt { get; private set; }

    // Ghost cursor position in screen pixels — updated on every MouseMove
    public SKPoint GhostCursorPx { get; set; }

    public void HandleCanvasClick(SKPoint screenPx, CoordinateMapper mapper,
                                  IGeometryService geometryService, SnapEngine snap)
    {
        var snapped = snap.Snap(screenPx); // returns screenPx or snapped position
        var (xPt, yPt) = mapper.ScreenToPage(snapped);

        switch (ActiveTool, DrawState)
        {
            case (ToolMode.Point, DrawState.Idle):
                geometryService.ExecuteCommand(new PlaceObjectCommand(
                    new PointObject(xPt, yPt)));
                break;

            case (ToolMode.Line, DrawState.Idle):
                AnchorPt = (xPt, yPt);
                DrawState = DrawState.AnchorPlaced;
                break;

            case (ToolMode.Line, DrawState.AnchorPlaced):
                var anchor = AnchorPt!.Value;
                geometryService.ExecuteCommand(new PlaceObjectCommand(
                    new LineObject(anchor.xPt, anchor.yPt, xPt, yPt)));
                AnchorPt = null;
                DrawState = DrawState.Idle;
                break;

            case (ToolMode.Circle, DrawState.Idle):
                AnchorPt = (xPt, yPt);
                DrawState = DrawState.AnchorPlaced;
                break;

            case (ToolMode.Circle, DrawState.AnchorPlaced):
                var ctr = AnchorPt!.Value;
                // Radius in PDF points: compute from screen distance → page distance
                double dx = xPt - ctr.xPt, dy = yPt - ctr.yPt;
                double radiusPt = Math.Sqrt(dx * dx + dy * dy);
                geometryService.ExecuteCommand(new PlaceObjectCommand(
                    new CircleObject(ctr.xPt, ctr.yPt, radiusPt)));
                AnchorPt = null;
                DrawState = DrawState.Idle;
                break;

            case (ToolMode.Select, _):
                geometryService.TrySelectAt(screenPx, mapper);
                break;
        }
    }
}
```

### Pattern 2: Geometry Object Model

**What:** Abstract base class with PDF-space coordinates. Derived types add shape-specific fields.

**When to use:** All geometry stored and serialised from these types.

```csharp
// Source: design decisions D-10 [ASSUMED — implementation pattern]
public abstract class GeometryObject
{
    public Guid Id { get; } = Guid.NewGuid();
    public bool IsSelected { get; set; }
    public abstract void Draw(SKCanvas canvas, CoordinateMapper mapper, SKPaint paint);
    public abstract bool HitTest(SKPoint screenPx, CoordinateMapper mapper, float tolerancePx);
}

public sealed class PointObject : GeometryObject
{
    public double XPt { get; set; }
    public double YPt { get; set; }
    // ... Draw: circle at PageToScreen(XPt, YPt)
    // ... HitTest: distance from screenPx to PageToScreen < tolerancePx
}

public sealed class LineObject : GeometryObject
{
    public double X1Pt { get; set; }  public double Y1Pt { get; set; }
    public double X2Pt { get; set; }  public double Y2Pt { get; set; }
    // Endpoint sub-selection (D-04)
    public int? SelectedEndpoint { get; set; } // null, 0, or 1
    // ... Draw: DrawLine(PageToScreen(p1), PageToScreen(p2))
    // ... HitTest: point-to-segment distance; also check endpoint tap targets
}

public sealed class CircleObject : GeometryObject
{
    public double CenterXPt { get; set; }
    public double CenterYPt { get; set; }
    public double RadiusPt { get; set; }
    // Sub-selection (D-05): 0 = center, 1 = edge point
    public int? SelectedSubPoint { get; set; }
    // ... HitTest: |distance_from_center - radiusPx| < tolerancePx (ring test)
    //              OR distance_from_center < dotRadiusPx (center dot test)
}
```

### Pattern 3: IGeometryCommand (Undo/Redo)

**What:** Per-action command objects stored in two stacks. Execute/Undo are inverses. [VERIFIED: standard command pattern — multiple sources]

```csharp
// Source: D-08, D-09 from CONTEXT.md
public interface IGeometryCommand
{
    void Execute(IGeometryService service);
    void Undo(IGeometryService service);
}

// UndoService — owns both stacks
public sealed class UndoService
{
    private readonly Stack<IGeometryCommand> _undoStack = new();
    private readonly Stack<IGeometryCommand> _redoStack = new();

    public void Execute(IGeometryCommand cmd, IGeometryService service)
    {
        cmd.Execute(service);
        _undoStack.Push(cmd);
        _redoStack.Clear();       // new action kills redo history
    }

    public void Undo(IGeometryService service)
    {
        if (_undoStack.TryPop(out var cmd))
        {
            cmd.Undo(service);
            _redoStack.Push(cmd);
        }
    }

    public void Redo(IGeometryService service)
    {
        if (_redoStack.TryPop(out var cmd))
        {
            cmd.Execute(service);
            _undoStack.Push(cmd);
        }
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
}
```

### Pattern 4: Ghost Preview Rendering

**What:** Between click 1 and click 2, render a dashed preview updated on every `MouseMove`. The `GhostCursorPx` in `ToolViewModel` is set on every `MouseMove` and triggers `InvalidationRequested`.

**SkiaSharp dashed line pattern:**
```csharp
// Source: SKPathEffect.CreateDash official docs [CITED: learn.microsoft.com/en-us/dotnet/api/skiasharp.skpatheffect.createdash]
using var ghostPaint = new SKPaint
{
    Style        = SKPaintStyle.Stroke,
    Color        = SKColor.Parse("#3B6FD4").WithAlpha(180),   // BrushAccent + partial alpha
    StrokeWidth  = 2f,
    IsAntialias  = true,
    PathEffect   = SKPathEffect.CreateDash(new float[] { 8f, 5f }, 0f),
};

// Anchor dot (filled accent circle)
using var anchorPaint = new SKPaint
{
    Style       = SKPaintStyle.Fill,
    Color       = SKColor.Parse("#3B6FD4"),
    IsAntialias = true,
};
// Anchor ring (outer unfilled ring)
using var ringPaint = new SKPaint
{
    Style       = SKPaintStyle.Stroke,
    Color       = SKColor.Parse("#3B6FD4"),
    StrokeWidth = 2f,
    IsAntialias = true,
};

// Draw anchor dot + ring
var anchorPx = mapper.PageToScreen(anchorXPt, anchorYPt);
canvas.DrawCircle(anchorPx, 5f, anchorPaint);   // filled dot
canvas.DrawCircle(anchorPx, 12f, ringPaint);     // outer ring

// Draw dashed ghost line (Line tool) or arc (Circle tool)
canvas.DrawLine(anchorPx, ghostCursorPx, ghostPaint);

// Circle ghost: draw full circle whose radius = distance(center, cursor)
float ghostRadius = SKPoint.Distance(anchorPx, ghostCursorPx);
canvas.DrawCircle(anchorPx, ghostRadius, ghostPaint);
```

**MouseMove → InvalidateVisual performance note:** `SKElement.InvalidateVisual()` is safe to call on every `MouseMove` frame; SkiaSharp redraws only the dirty region. At 60Hz input from Grid 3, this is well within render budget. [ASSUMED — based on general WPF rendering knowledge, not benchmarked in this project]

### Pattern 5: Snap Engine

**What:** On each `MouseMove` (and before each click commit), computes the nearest snap candidate within the proximity threshold. Returns the snapped screen position and the snap type for status toast.

**Snap priority order (Claude's discretion — recommended):**
1. Existing object endpoints (highest priority for gaze students: they need to connect lines)
2. Line-line intersections (computed live — only if ≤5 lines; scale concern otherwise)
3. Orientation guides: V/H/45° from current cursor position

**Recommended thresholds (Claude's discretion):**
- Snap proximity: 20px screen pixels (large enough for ±10px gaze imprecision)
- Hit-test tolerance for lines: 10px screen pixels (wide finger/gaze target)
- Point hit radius: 18px screen pixels (point dot is small, needs forgiving target)
- Sub-point tap target (D-04/D-05): 28px screen pixel radius (= 56px diameter = ≥56px requirement)

```csharp
// Source: design decision + recommended algorithm [ASSUMED — implementation pattern]
public sealed class SnapEngine
{
    private const float SnapThresholdPx = 20f;

    // Returns: snapped position (screen px) + snap type string for toast
    public (SKPoint position, string? snapLabel) Snap(
        SKPoint cursorPx,
        IReadOnlyList<GeometryObject> objects,
        CoordinateMapper mapper)
    {
        float bestDist = SnapThresholdPx;
        SKPoint best   = cursorPx;
        string? label  = null;

        // 1. Endpoints
        foreach (var obj in objects)
        {
            foreach (var ep in obj.GetSnapPoints(mapper))
            {
                float d = SKPoint.Distance(cursorPx, ep.ScreenPx);
                if (d < bestDist) { bestDist = d; best = ep.ScreenPx; label = ep.Label; }
            }
        }

        // 2. Line-line intersections (only compute if few lines for performance)
        var lines = objects.OfType<LineObject>().ToList();
        if (lines.Count <= 6)
        {
            for (int i = 0; i < lines.Count; i++)
            for (int j = i + 1; j < lines.Count; j++)
            {
                if (GeometryMath.TryLineIntersect(lines[i], lines[j], mapper, out var pt))
                {
                    float d = SKPoint.Distance(cursorPx, pt);
                    if (d < bestDist) { bestDist = d; best = pt; label = "intersection"; }
                }
            }
        }

        // 3. Orientation guides (vertical, horizontal, 45° from cursor)
        // ... compute snapped-to-V, snapped-to-H, snapped-to-45 candidates
        // pick whichever is closest within threshold

        return (best, label);
    }
}
```

### Pattern 6: Hit Testing

**What:** On a canvas click in Select mode, iterate geometry objects in reverse Z-order (last placed = top), returning the first object within tolerance.

**Line hit test — point-to-segment distance:**
```csharp
// Source: standard 2D geometry algorithm [CITED: csharphelper.com/howtos/howto_point_segment_distance.html]
public static float DistancePointToSegment(SKPoint p, SKPoint a, SKPoint b)
{
    var ab = b - a;
    var ap = p - a;
    float t = SKPoint.DotProduct(ap, ab) / SKPoint.DotProduct(ab, ab);
    t = Math.Clamp(t, 0f, 1f);
    var closest = new SKPoint(a.X + t * ab.X, a.Y + t * ab.Y);
    return SKPoint.Distance(p, closest);
}
```

**Circle hit test — ring proximity:**
```csharp
// Source: standard 2D geometry [ASSUMED]
public static bool HitTestCircle(SKPoint click, SKPoint centerPx, float radiusPx, float tolerancePx)
{
    float dist = SKPoint.Distance(click, centerPx);
    // Hit the ring:
    if (Math.Abs(dist - radiusPx) <= tolerancePx) return true;
    // Hit the center dot (sub-select target):
    if (dist <= 18f) return true;
    return false;
}
```

**Line-line intersection (for snap engine):**
```csharp
// Source: standard algorithm — Cramer's rule [CITED: csharphelper.com/howtos/howto_segment_intersection.html]
public static bool TryLineIntersect(SKPoint a1, SKPoint a2, SKPoint b1, SKPoint b2, out SKPoint pt)
{
    float dx1 = a2.X - a1.X, dy1 = a2.Y - a1.Y;
    float dx2 = b2.X - b1.X, dy2 = b2.Y - b1.Y;
    float denom = dx1 * dy2 - dy1 * dx2;
    pt = SKPoint.Empty;
    if (Math.Abs(denom) < 1e-6f) return false;   // parallel
    float t = ((b1.X - a1.X) * dy2 - (b1.Y - a1.Y) * dx2) / denom;
    pt = new SKPoint(a1.X + t * dx1, a1.Y + t * dy1);
    return true;  // infinite lines; caller decides if within segment range
}
```

### Pattern 7: DPI Fix (D-11)

**What:** `PdfCanvasViewModel.EnsureCoordinateMapper()` currently hardcodes `dpiScale = 1.0` (line 150). Phase 2 must wire the real DPI.

**How:** `VisualTreeHelper.GetDpi(this)` is already called in `PdfCanvas.xaml.cs` (`ReportCanvasSize` method). The `dpiInfo.PixelsPerDip` value needs to be forwarded to `PdfCanvasViewModel` and from there to `CoordinateMapper`.

**Recommended approach:**
```csharp
// PdfCanvas.xaml.cs — existing ReportCanvasSize, extend to also report DPI
var dpiInfo = VisualTreeHelper.GetDpi(this);
_vm.SetDpiScale(dpiInfo.PixelsPerDip);   // new method on PdfCanvasViewModel

// PdfCanvasViewModel.cs — store and use in EnsureCoordinateMapper
private double _dpiScale = 1.0;

public void SetDpiScale(double pixelsPerDip)
{
    _dpiScale = pixelsPerDip;
}

// EnsureCoordinateMapper: replace hardcoded 1.0 with _dpiScale
_coordinateMapper = new CoordinateMapper(
    zoomFactor:    _mainVm.ZoomFactor,
    dpiScale:      _dpiScale,           // was: 1.0
    ...
```

**Note:** `SetDpiScale` should be called from `ReportCanvasSize` and also when the window moves to a different monitor (WPF fires `DpiChanged` on the `Window`). The DPI fix must come before geometry rendering or sub-pixel geometry will misalign with the bitmap.

### Pattern 8: Right Rail — Selection-Aware UserControl

**What:** Replace `RightRailPlaceholder.xaml` with a `RightRail.xaml` that observes `RightRailViewModel` and shows different content based on selection state.

**States:**
1. Nothing selected → "NOTHING SELECTED" dashed box + undo/redo buttons
2. Point selected → SelectionCard + NudgeBlock (label: "Move") + Delete button
3. Line selected, no sub-point → SelectionCard + sub-point tap targets + NudgeBlock (label: "Move") + Delete
4. Line selected, endpoint A → SelectionCard + NudgeBlock (label: "Move endpoint A") + Delete
5. Line selected, endpoint B → SelectionCard + NudgeBlock (label: "Move endpoint B") + Delete
6. Circle selected, no sub-point → SelectionCard + NudgeBlock (label: "Move") + Delete
7. Circle selected, center → SelectionCard + NudgeBlock (label: "Move centre") + Delete
8. Circle selected, edge → SelectionCard + NudgeBlock (label: "Move radius") + Delete

**Undo/Redo buttons appear in all states** (visible at bottom of right rail per design reference `RightRailEmpty`).

**Sub-point tap targets on canvas:** The ≥56×56px hit area for endpoints is implemented as a transparent hit zone drawn in the SkiaSharp layer — a circle with 28px radius (= 56px diameter) centred on the endpoint. The visual dot is smaller (8–10px). The hit zone is invisible but captured by `GeometryHitTester.TryHitSubPoint()`.

### Anti-Patterns to Avoid

- **Storing coordinates in screen pixels:** If screen-space coords are stored and zoom changes, geometry shifts. Always store in PDF points (D-10).
- **Triggering `InvalidateVisual` from background threads:** `SKElement.InvalidateVisual()` is a WPF UI thread operation. Route through `Dispatcher.Invoke` as established in Phase 1 pattern.
- **Recomputing all line-line intersections on every `MouseMove`:** For large geometry sets, O(n²) intersection computation can lag. Gate on `lines.Count <= 6` or similar for Phase 2; optimise if needed in v2.
- **Creating new `SKPaint` objects per-frame:** `SKPaint` is disposable and allocating per-frame causes GC pressure. Cache paint objects in the ViewModel or use a paint pool.
- **Binding commands in XAML for SKElement interactions:** Established Phase 1 pattern: wire `MouseDown`/`MouseMove` in code-behind (`PdfCanvas.xaml.cs`), call ViewModel methods directly. No XAML command binding on the SKElement.
- **Using WPF logical pixels for hit-test tolerance:** The `SKElement` reports events in logical pixels; `SkCanvas` draws in physical pixels. Convert using `PixelsPerDip`. Failing to do so means hit targets are the wrong size on high-DPI screens.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Dashed preview lines | Custom stipple loop | `SKPathEffect.CreateDash(float[], float)` | Built into SkiaSharp; handles phase, variable dash spacing, performance [CITED: learn.microsoft.com docs] |
| Dependency injection | Manual service locator | `Microsoft.Extensions.DependencyInjection` (already installed) | Already wired in `App.xaml.cs`; add new services as `AddSingleton<>` |
| Observable properties | Manual `INotifyPropertyChanged` | `[ObservableProperty]` from CommunityToolkit.Mvvm (already installed) | Source-generator-based; zero boilerplate |
| Float comparisons for parallel lines | Custom epsilon logic | `Math.Abs(denom) < 1e-6f` guard in intersection test | Standard approach; sufficient for GCSE-scale geometry |

**Key insight:** Phase 2 requires no new NuGet packages. Everything is arithmetic on SKPoints using APIs already in the project.

---

## Common Pitfalls

### Pitfall 1: Coordinate Space Confusion in Hit Testing

**What goes wrong:** Mouse events arrive in WPF logical pixels. SkiaSharp draws in physical pixels. The `SKElement` PaintSurface gives physical pixel dimensions but `MouseEventArgs.GetPosition` gives logical pixels. Hit testing in the wrong space produces targets that are 1.5× or 2× off on high-DPI screens.

**Why it happens:** WPF and SkiaSharp have different coordinate units. `VisualTreeHelper.GetDpi` gives the `PixelsPerDip` ratio needed to convert.

**How to avoid:** In `PdfCanvas.xaml.cs`, convert the mouse position to physical pixels before passing to the ViewModel:
```csharp
private void OnMouseDown(object sender, MouseButtonEventArgs e)
{
    var logicalPos = e.GetPosition(SkCanvas);
    var dpi = VisualTreeHelper.GetDpi(this);
    var physPx = new SKPoint(
        (float)(logicalPos.X * dpi.PixelsPerDip),
        (float)(logicalPos.Y * dpi.PixelsPerDip));
    _vm?.HandleCanvasClick(physPx);
}
```

**Warning signs:** Click targets that feel shifted; hit tests that work at 100% DPI but miss at 125% or 150%.

### Pitfall 2: Nudge Step Interpretation

**What goes wrong:** The right rail nudge step (1/5/20 px) is labelled in screen pixels. But nudge commands store a delta in PDF points. If you convert step size to PDF points using the *current* zoom, and then zoom changes before undo, the undo applies the same PDF-point delta but the screen-pixel displacement feels different from the original.

**Why it happens:** PDF point deltas are zoom-independent; screen pixel deltas are zoom-dependent.

**How to avoid:** This is correct behaviour — nudge in PDF points is stable across zoom. The right rail label "1/5/20 px" means "approximately this many pixels at the current view". Document this in the NudgeCommand implementation comment. The user decision (D-05, D-08) is per-click undo, which works correctly with PDF-point deltas.

**Warning signs:** Undo of a nudge command moves the object a different distance at different zoom levels — this is correct and intentional, not a bug.

### Pitfall 3: MouseMove Ghost Preview Performance

**What goes wrong:** `MouseMove` fires at hundreds of events per second on a desktop machine. If each move calls `InvalidateVisual()`, the canvas redraws at the mouse polling rate, which causes CPU usage to spike and may stutter on the school machine.

**Why it happens:** WPF does not automatically coalesce `MouseMove` events before layout.

**How to avoid:** Throttle redraws using a flag: set a `_ghostDirty = true` flag on `MouseMove`, and call `InvalidateVisual()` only from a `CompositionTarget.Rendering` handler (fires once per frame at the WPF render rate, ~60Hz). Alternative: accept the event rate and rely on SkiaSharp's hardware-accelerated rendering — on modern hardware (even school machines), 60 Hz redraws for a simple geometry layer is well within budget. Start without throttling; add if needed. [ASSUMED — based on general WPF knowledge; no benchmark for target school hardware]

**Warning signs:** Fan spin, high CPU in Task Manager while moving mouse with a Line tool active.

### Pitfall 4: Sub-Point Hit Targets Overlapping Object Hit Targets

**What goes wrong:** When a Line is selected, clicking near an endpoint should sub-select the endpoint. But the endpoint tap target (28px radius) and the line hit target (10px corridor) overlap. If the selection logic hits the line first, it deselects and re-selects, preventing endpoint sub-selection.

**Why it happens:** Hit test priority is undefined without explicit ordering.

**How to avoid:** Implement a two-pass hit test:
1. When a line is selected, check endpoint sub-points first. If a sub-point is hit, update sub-selection and return — do not re-select.
2. Only fall through to the full object hit test if no sub-point was hit.
3. Clicking anywhere else deselects the sub-point but keeps the parent object selected.

**Warning signs:** Tapping near an endpoint selects it but immediately loses sub-selection focus.

### Pitfall 5: SKPaint Disposal Causing Frame Drops

**What goes wrong:** Creating and disposing `SKPaint` objects on every `PaintSurface` call (60 times/sec) triggers garbage collection pauses visible as frame drops.

**Why it happens:** `SKPaint` is an `IDisposable` wrapping a native Skia object.

**How to avoid:** Cache paints as private fields in `GeometryLayerViewModel`, create them once in the constructor. Recreate only when colour/style changes (e.g., selection state changes). [CITED: standard SkiaSharp performance guidance]

---

## Code Examples

### Drawing a committed Line with selection highlight

```csharp
// Source: SkiaSharp API + project design tokens [ASSUMED — implementation pattern]
private void DrawLine(SKCanvas canvas, LineObject line, CoordinateMapper mapper,
                      SKPaint normalPaint, SKPaint selectedPaint)
{
    var p1 = mapper.PageToScreen(line.X1Pt, line.Y1Pt);
    var p2 = mapper.PageToScreen(line.X2Pt, line.Y2Pt);

    var paint = line.IsSelected ? selectedPaint : normalPaint;
    canvas.DrawLine(p1, p2, paint);

    if (line.IsSelected)
    {
        // Endpoint tap targets (D-04): visual dot + invisible hit zone
        DrawEndpointTarget(canvas, p1, line.SelectedEndpoint == 0);
        DrawEndpointTarget(canvas, p2, line.SelectedEndpoint == 1);
    }
}

private void DrawEndpointTarget(SKCanvas canvas, SKPoint center, bool isActive)
{
    // Visual dot: 8px radius
    using var dotPaint = new SKPaint
    {
        Style = SKPaintStyle.Fill,
        Color = isActive ? SKColor.Parse("#3B6FD4") : SKColor.Parse("#3B6FD4").WithAlpha(160),
        IsAntialias = true,
    };
    canvas.DrawCircle(center, 8f, dotPaint);

    if (isActive)
    {
        // Active sub-point ring indicator
        using var ringPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColor.Parse("#3B6FD4"),
            StrokeWidth = 2.5f,
            IsAntialias = true,
        };
        canvas.DrawCircle(center, 14f, ringPaint);
    }
}
```

### Snap ring visual

```csharp
// Source: additional-screens.jsx ProtractorPlacing pattern — dashed ring + filled dot [CITED: docs/additional-screens.jsx]
private void DrawSnapIndicator(SKCanvas canvas, SKPoint snappedPx)
{
    using var ringPaint = new SKPaint
    {
        Style       = SKPaintStyle.Stroke,
        Color       = SKColor.Parse("#3B6FD4"),
        StrokeWidth = 2f,
        IsAntialias = true,
        PathEffect  = SKPathEffect.CreateDash(new float[] { 3f, 3f }, 0f),
    };
    using var dotPaint = new SKPaint
    {
        Style       = SKPaintStyle.Fill,
        Color       = SKColor.Parse("#3B6FD4"),
        IsAntialias = true,
    };
    canvas.DrawCircle(snappedPx, 18f, ringPaint);   // outer dashed ring
    canvas.DrawCircle(snappedPx, 5f, dotPaint);      // filled snap dot
}
```

### Status toast rendering (bottom of canvas)

```csharp
// Source: direction-splitrails.jsx status bar pattern [CITED: docs/direction-splitrails.jsx]
// Implemented as a WPF TextBlock overlay (not drawn in SkiaSharp) — avoids text
// rendering complexity in Skia; a WPF TextBlock in the canvas Grid handles it.
// Bind to ToolViewModel.StatusMessage (observable string).
// Example values: "Click 2nd point", "Click 2nd point · snap: endpoint A"
```

**Recommendation:** Render the status toast as a WPF overlay element (TextBlock in the canvas Grid, positioned at bottom), not as SkiaSharp text. This gives free WPF text rendering with no Skia font loading complexity.

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `dpiScale = 1.0` hardcode (Phase 1) | Wire `VisualTreeHelper.GetDpi().PixelsPerDip` (Phase 2, D-11) | Phase 1 → Phase 2 | High-DPI geometry renders correctly |
| `RightRailPlaceholder.xaml` (stub) | `RightRail.xaml` selection-aware control (Phase 2) | Phase 1 → Phase 2 | Full editing UI |
| ToolRail buttons with no commands (Phase 1) | ToolRail buttons bound to `ToolViewModel` commands | Phase 1 → Phase 2 | Tools become functional |

**No deprecated APIs being introduced in Phase 2.** All patterns build directly on established Phase 1 foundations.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | MouseMove at 60Hz with SKElement.InvalidateVisual() per frame is within render budget on school hardware | Common Pitfalls #3 | Ghost preview may stutter; throttling via CompositionTarget.Rendering mitigates |
| A2 | SKPaint object creation per-frame causes measurable GC pressure | Common Pitfalls #5 | If wrong, the caching complexity is unnecessary but harmless |
| A3 | Gating intersection snap on `lines.Count <= 6` prevents lag in typical Phase 2 usage | Snap Engine pattern | GCSE papers rarely have >6 drawn lines; if wrong, performance degrades gracefully |
| A4 | Status toast implemented as WPF TextBlock overlay (not Skia text) is simpler | Code Examples | Skia text rendering with font loading would work but is more code; WPF text is sufficient |

---

## Open Questions

1. **DpiChanged event on window move between monitors**
   - What we know: `VisualTreeHelper.GetDpi` gives current DPI; WPF fires `DpiChanged` on `Window` when moved to a different monitor
   - What's unclear: Does the existing Phase 1 code handle per-monitor DPI changes during a session? The manifest has `PerMonitorV2` but no `DpiChanged` handler was added in Phase 1.
   - Recommendation: Add a `DpiChanged` handler on `MainWindow` that calls `SetDpiScale` and triggers canvas reload. Include in Phase 2 scope as part of D-11.

2. **Orientation snap visual: guide line or snap-only?**
   - What we know: Claude's discretion — can show faint guide line across canvas or just affect snap
   - What's unclear: A full-width guide line adds visual noise; snap-only is invisible until snapped
   - Recommendation: Show a short guide line segment (50px in the snap direction) centred on the snapped point only when snap is active. Disappears when cursor moves away. This gives feedback without permanent visual clutter.

3. **Undo/Redo buttons placement**
   - What we know: Design shows Undo/Redo at bottom of right rail in all states (`RightRailEmpty` in direction-splitrails.jsx)
   - What's unclear: Should Undo/Redo also be accessible from somewhere when no tool is active (canvas-only focus)?
   - Recommendation: Place Undo/Redo at bottom of right rail per design. Grid 3 users will always use the rail buttons; keyboard shortcuts are irrelevant for the target user.

---

## Environment Availability

Step 2.6: SKIPPED — Phase 2 adds no external dependencies. All required libraries (SkiaSharp, CommunityToolkit.Mvvm, DI, .NET 9) are already installed and verified in Phase 1.

---

## Validation Architecture

`nyquist_validation: false` in `.planning/config.json` — this section is omitted per configuration.

---

## Security Domain

Phase 2 introduces no network calls, no file I/O, no authentication, and no user-supplied data that executes. Geometry coordinates are numeric floats; snap engine inputs are bounded screen coordinates. ASVS review is not applicable for this phase.

---

## Sources

### Primary (HIGH confidence)

- [VERIFIED: MathGaze.csproj] — confirmed installed packages (SkiaSharp 3.119.2, CommunityToolkit.Mvvm 8.4.2, no new packages needed)
- [VERIFIED: MathGaze/Core/CoordinateMapper.cs] — confirmed `PageToScreen`/`ScreenToPage` API signatures; `dpiScale` hardcode location
- [VERIFIED: MathGaze/Views/PdfCanvas.xaml.cs] — confirmed `VisualTreeHelper.GetDpi` already called; `ReportCanvasSize` method is the DPI fix entry point
- [VERIFIED: MathGaze/ViewModels/PdfCanvasViewModel.cs] — confirmed `Paint()` method structure; geometry layer insertion point after line 138
- [VERIFIED: MathGaze/App.xaml.cs] — confirmed DI singleton registration pattern
- [VERIFIED: .planning/phases/02-geometry-core/02-CONTEXT.md] — all locked decisions D-01 through D-11

### Secondary (MEDIUM confidence)

- [CITED: learn.microsoft.com/en-us/dotnet/api/skiasharp.skpatheffect.createdash] — `SKPathEffect.CreateDash(float[], float)` API, dash interval format
- [CITED: csharphelper.com/howtos/howto_point_segment_distance.html] — point-to-segment distance algorithm
- [CITED: csharphelper.com/howtos/howto_segment_intersection.html] — line-line intersection via Cramer's rule
- [CITED: docs/additional-screens.jsx] — ghost preview visual pattern (dashed ring + filled dot at snap target)
- [CITED: docs/direction-splitrails.jsx] — NudgeBlock, SelectionCard, UtilRow component structures; status bar pattern

### Tertiary (LOW confidence)

- [ASSUMED] — MouseMove render throttling needed at 60Hz+ on school hardware — not benchmarked
- [ASSUMED] — SKPaint caching recommendation — standard practice, not project-specific benchmark

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages; all tools verified in csproj
- Architecture patterns: HIGH — locked decisions from CONTEXT.md + verified existing Phase 1 patterns
- Geometry math algorithms: HIGH — standard algorithms with multiple cited sources
- Ghost preview timing/performance: LOW — assumed, not measured on target hardware
- Sub-point tap target overlap handling: MEDIUM — recommended pattern, needs integration testing

**Research date:** 2026-05-02
**Valid until:** 2026-06-02 (stable stack — SkiaSharp 3.x API is stable)
