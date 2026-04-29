# Architecture Patterns

**Domain:** Native Windows geometry annotation app over PDF background
**Project:** MathGaze
**Researched:** 2026-04-29
**Confidence:** MEDIUM-HIGH (training data on WinUI 3 / Win2D / .NET MVVM patterns; WebSearch unavailable for verification; core algorithms are classical and stable)

---

## Recommended Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        UI Layer (WinUI 3 XAML)              │
│   TopBar · LeftToolRail · RightRailPanel · StatusBar        │
│            (bound to ViewModels via x:Bind)                 │
└──────────────────────┬──────────────────────────────────────┘
                       │ Commands / property bindings
┌──────────────────────▼──────────────────────────────────────┐
│                    ViewModel Layer                          │
│  MainViewModel · ToolViewModel · SelectionViewModel         │
│  GeometryViewModel · ProtractorViewModel · CanvasViewModel  │
└────────┬───────────────────────────┬────────────────────────┘
         │ reads/writes              │ raises events
┌────────▼──────────┐    ┌──────────▼──────────────────────┐
│   Geometry Store  │    │     Input Controller            │
│  (observable      │    │  (click→tool state machine)     │
│   object graph)   │◄───│   ToolMode FSM · HitTester      │
│   undo/redo log   │    │   SnapEngine                    │
└────────┬──────────┘    └─────────────────────────────────┘
         │ notifies
┌────────▼──────────────────────────────────────────────────┐
│                  Render Pipeline (Win2D)                   │
│  CanvasAnimatedControl.Draw event                         │
│  Layer 1: PDF bitmap (CanvasBitmap, cached per page/zoom) │
│  Layer 2: Geometry vector (DrawingSession calls)          │
│  Layer 3: Interaction feedback (snap ring, ghost object)  │
└───────────────────────────────────────────────────────────┘
         │
┌────────▼──────────────────────────────────────────────────┐
│                  PDF Service                              │
│  Windows.Data.Pdf (primary) or PDFium (fallback)         │
│  Renders page → CanvasBitmap at current DPI/zoom         │
└───────────────────────────────────────────────────────────┘
```

---

## Component Boundaries

| Component | Responsibility | Communicates With |
|-----------|---------------|-------------------|
| **MainWindow (XAML)** | Shell layout — top bar, rail containers, Win2D canvas host | MainViewModel (bindings), CanvasControl (events) |
| **LeftToolRail** | Tool selection buttons, fixed 7 tools | ToolViewModel (selected tool binding) |
| **RightRailPanel** | Selection-aware verb panel; renders NudgeBlock, PivotPicker, ProtractorControls, SnapOrientationRow, ReflectVerb depending on SelectionViewModel state | SelectionViewModel, GeometryViewModel |
| **CanvasAnimatedControl** | Win2D surface — receives Draw callbacks and pointer events | RenderService (draw calls), InputController (pointer events) |
| **InputController** | Translates pointer events into geometry operations; runs tool FSM; calls SnapEngine before committing clicks | ToolViewModel (current tool), SnapEngine, GeometryStore, UndoStack |
| **ToolStateMachine** | Tracks multi-click tool state (e.g., Line awaiting second click); produces CommitAction when sequence completes | InputController, GeometryStore |
| **SnapEngine** | Given a raw canvas point, returns a snapped point + snap type | GeometryStore (all current geometry), CoordinateMapper |
| **HitTester** | Given a canvas point + tolerance, returns the nearest geometry object(s) | GeometryStore, CoordinateMapper |
| **GeometryStore** | Observable collection of GeometryObject instances; source of truth for all drawn objects; fires change events for re-render | All consumers |
| **UndoStack** | Maintains Command list; Execute/Undo/Redo; publishes CanUndo/CanRedo | InputController (push), MainViewModel (undo/redo bindings) |
| **CoordinateMapper** | Single place for all space transforms: PDF space ↔ canvas space ↔ screen space | PDF rendering, InputController, SnapEngine, HitTester, RenderService |
| **PDFService** | Loads PDF, renders page to CanvasBitmap at requested scale; caches bitmaps | CoordinateMapper (knows page dimensions), RenderService |
| **RenderService** | Stateless draw helper called from CanvasAnimatedControl.Draw; draws each layer in order | GeometryStore, PDFService (bitmap), CoordinateMapper |
| **PersistenceService** | Serialize/deserialize GeometryStore to JSON sidecar; auto-save on mutation | GeometryStore, SessionModel |
| **SessionModel** | Value object: PDF path, current page, zoom, geometry list | PersistenceService |

---

## Data Flow

### Normal click → geometry creation (Line tool, 2-click)

```
User click (Grid 3 / mouse)
  → CanvasAnimatedControl.PointerPressed
  → InputController.OnPointerPressed(screenPt)
      → CoordinateMapper.ScreenToCanvas(screenPt) → canvasPt
      → SnapEngine.Snap(canvasPt) → (snappedPt, SnapType)
      → ToolStateMachine.Feed(snappedPt, SnapType)
          [First click]: store pending start point, enter AwaitingSecondPoint state
          [Second click]: produce CreateLineCommand(start, snappedPt)
              → UndoStack.Execute(CreateLineCommand)
                  → GeometryStore.Add(new LineObject(start, end))
                      → GeometryStore.ObjectsChanged event
                          → CanvasAnimatedControl.Invalidate() [triggers Draw]
                          → SelectionViewModel.Refresh()
                          → PersistenceService.AutoSave()
```

### Render loop

```
CanvasAnimatedControl.Draw(sender, args)
  → RenderService.Draw(args.DrawingSession, currentState)
      → Layer 1: ds.DrawImage(cachedPdfBitmap)
      → Layer 2: foreach obj in GeometryStore: obj.Draw(ds, CoordinateMapper)
      → Layer 3: InputController.DrawFeedback(ds)  ← ghost line, snap ring
```

### Selection → right-rail update

```
User click in Select tool
  → HitTester.HitTest(canvasPt, tolerance=GazeTolerance) → GeometryObject
  → GeometryStore.SetSelection(obj)
      → SelectionViewModel.SelectedObject changed
          → RightRailPanel re-renders via x:Bind
```

---

## Coordinate System Strategy

Three spaces must be managed carefully. One class owns all transforms.

### The Three Spaces

| Space | Origin | Units | Notes |
|-------|--------|-------|-------|
| **PDF space** | Top-left of page | PDF points (1/72 inch) | Authoritative geometry storage space |
| **Canvas space** | Top-left of Win2D CanvasControl | Device-independent pixels (DIPs) | Working space for rendering and input |
| **Screen space** | Top-left of screen | Physical pixels | PointerPressed events arrive here |

### Why store geometry in PDF space

Geometry coordinates must survive zoom and pan. If you store in canvas space, every nudge, zoom-level change, or page scroll requires recalculating all object positions. Storing in PDF space means objects "live" on the page — zoom/pan only affects the display transform, not the data.

### CoordinateMapper state

```csharp
class CoordinateMapper
{
    // Set once when PDF page loads
    SizeF PdfPageSizePt;        // PDF page dimensions in points

    // Updated on zoom/pan
    double ZoomFactor;          // e.g. 1.5 = 150%
    Vector2 PanOffsetDips;      // canvas-space translation

    // Derived: PDF points → canvas DIPs
    // canvasPt = (pdfPt * (CanvasPhysicalWidth / PdfPageWidthPt) * ZoomFactor) + PanOffset
    Vector2 PdfToCanvas(Vector2 pdfPt);
    Vector2 CanvasToPdf(Vector2 canvasPt);

    // Screen → canvas (handles DPI scaling)
    Vector2 ScreenToCanvas(Point screenPt);
}
```

### DPI awareness

Win2D CanvasControl works in DIPs automatically. PointerPressed gives logical (DIP) coordinates on WinUI 3 if the app is DPI-aware (it must be). The CoordinateMapper's ScreenToCanvas is essentially a no-op at 96 DPI, but must account for 150% / 200% displays common on school laptops.

### Concrete transform chain for a click

```
PointerPressed (logical px, relative to CanvasControl)
  → no transform needed (WinUI 3 gives DIP coordinates)
  → subtract PanOffsetDips
  → divide by ZoomFactor
  → divide by (CanvasDipsPerPdfPoint)   [= CanvasWidth / PdfPageWidthPt]
  → result: PDF-space point  ← store this
```

---

## Geometry Object Model

### Base class

```csharp
abstract class GeometryObject
{
    Guid Id { get; } = Guid.NewGuid();
    bool IsSelected { get; set; }
    bool IsLocked { get; set; }
    string? Label { get; set; }

    // All positions in PDF space
    abstract void Draw(CanvasDrawingSession ds, CoordinateMapper mapper,
                       DrawStyle style);
    abstract bool HitTest(Vector2 pdfPt, float tolerancePdf);
    abstract Rect BoundsPdf { get; }         // for spatial index
    abstract void Translate(Vector2 deltaPdf);
    abstract GeometryObject DeepClone();     // for undo snapshot
}
```

### Concrete types

```csharp
class PointObject : GeometryObject
{
    Vector2 PositionPdf;
}

class LineObject : GeometryObject
{
    Vector2 StartPdf;
    Vector2 EndPdf;
    // No child PointObject references — endpoints stored directly.
    // Snap engine finds endpoints by querying lines, not a separate point list.
}

class CircleObject : GeometryObject
{
    Vector2 CenterPdf;
    float RadiusPdf;         // derived from center→edge click distance
}

class ProtractorObject : GeometryObject
{
    Vector2 CenterPdf;
    float RotationDeg;       // angle of the baseline arm
    ProtractorStyle Style;   // Classic180 | Full360
    bool IsFlipped;
    // NOT derived from line references at draw time — copied at placement.
    // Lines may be deleted; protractor persists.
}

class TextBoxObject : GeometryObject
{
    Vector2 PositionPdf;
    string Text;
    float FontSizePt;
}

class MCQAnnotation : GeometryObject   // not really geometry, but same store
{
    Rect RegionPdf;         // bounding box of answer choice on PDF
    string ChoiceLabel;     // "A", "B", "C", "D"
    bool IsSelected;
    bool IsLocked;
}
```

### Why no shared PointObject references between lines

GeoGebra-style shared vertex references create complex dependency graphs: move the shared point and every dependent object must update. For MathGaze, the interaction model is nudge-by-step, not drag — so there is no "I grabbed point B which is shared by line 1 and line 2." Independent endpoints kept per-object is simpler and correct for click-to-commit. Snap visually suggests coincidence without enforcing structural coupling.

---

## Layer Rendering Approach

Use `CanvasAnimatedControl` (not `CanvasControl`) because it fires a steady draw tick independent of invalidation — important for smooth snap feedback animation while the user hovers.

### Draw order (each frame)

```
1. DrawImage(pdfBitmap) at transform(PanOffset, ZoomFactor)
2. For each obj in GeometryStore (back-to-front by creation order):
       obj.Draw(ds, mapper, normal or selected style)
3. InputController.DrawLiveFeedback(ds, mapper):
       — If awaiting second click: ghost line from first point to cursor
       — Snap ring: circle around snapped candidate point (animated radius)
       — Orientation guide: faint V/H/45° line through cursor when snap orientation active
4. SelectionOverlay: selection highlight / pivot indicator on selected object
```

### PDF bitmap caching

```
Cache key: (pageIndex, zoomLevel rounded to nearest 0.1)
On zoom change: kick off async RenderPageAsync at new zoom → swap bitmap when ready
While loading: display previous zoom level's bitmap scaled in software (acceptable blur)
Max cache: 3 pages (current ± 1) to limit memory
```

The Windows.Data.Pdf API renders to a Windows.Storage.Streams.IRandomAccessStream, which you convert to a CanvasBitmap via CanvasBitmap.LoadAsync. This is the correct path — confirmed stable in .NET / WinUI 3 context (MEDIUM confidence — approach is documented but Phase 0 validation on exam machine is required as per PROJECT.md).

---

## Snap System

The snap system runs before every committed click (except Select tool). It also runs continuously during pointer-move to drive visual feedback.

### Snap types (priority order)

| Priority | Type | Description | Trigger radius |
|----------|------|-------------|----------------|
| 1 | EndpointSnap | Nearest line/circle endpoint | 28 px canvas |
| 2 | IntersectionSnap | Nearest geometric intersection of any two objects | 28 px canvas |
| 3 | CenterSnap | Nearest circle center | 24 px canvas |
| 4 | OrientationSnap | Force cursor onto V/H/45° rays from previous point | Active when snap orientation mode is set |
| 5 | GridSnap | Nearest grid point (optional, off by default) | 16 px canvas |
| — | FreeSnap | Raw cursor position | Fallback |

Convert tolerance to PDF space before comparison: `tolerancePdf = toleranceCanvas / ZoomFactor`.

### Endpoint snap algorithm

```
candidates = []
foreach obj in GeometryStore:
    foreach endpoint in obj.GetSnapPoints():   // each line returns [start, end]; circle returns [center]
        d = distance(cursorPdf, endpointPdf)
        if d < tolerancePdf:
            candidates.append((d, endpoint, EndpointSnap))

return candidates.minBy(d)  // closest wins
```

`GetSnapPoints()` is a virtual method on GeometryObject. Cheap — O(n) over objects, each returning 1–3 points.

### Intersection snap algorithm

Compute all pairwise intersections lazily (only if no endpoint snap found in the priority chain):

```
for each pair (A, B) in GeometryStore combinations:
    pts = ComputeIntersections(A, B)
    foreach pt in pts:
        d = distance(cursorPdf, pt)
        if d < tolerancePdf:
            candidates.append((d, pt, IntersectionSnap))
```

**Line-line intersection:** Standard parameterised form. Solve for t and s in [0,1] for segment intersection; no bounds check for infinite-line intersection (for protractor placement).

**Line-circle intersection:** Quadratic formula giving 0, 1, or 2 points.

**Circle-circle intersection:** Radical axis method.

For MVP with O(n²) pairwise: at realistic scene sizes (< 30 objects) this is fast enough. Do not premature-optimise. A quadtree / spatial hash is a future optimisation if scenes grow past ~100 objects.

### Orientation snap algorithm

When the user has activated a snap orientation (V, H, or 45°) from the right rail:

```
// prevPt = last committed point (e.g. first click of a line)
// cursorPdf = raw cursor
// snapAngle = 0° (H), 90° (V), or 45°

axis = unit vector at snapAngle from prevPt
projection = dot(cursorPdf - prevPt, axis)
snappedPt = prevPt + axis * projection
```

This forces the second click to land exactly on the chosen angle ray from the first point, regardless of where the gaze cursor actually is. Ideal for drawing horizontal/vertical lines without drag.

---

## Hit Testing

Hit testing uses gaze-friendly tolerances — significantly larger than mouse targets.

### Tolerance hierarchy

| Scenario | Tolerance (canvas px) | Notes |
|----------|----------------------|-------|
| Select tool click | 32 px | Generous for gaze |
| Snap search | 28 px | Slightly tighter than select |
| Protractor two-line picks | 36 px | Protractor mode needs the largest tolerance |

### Line hit test (distance point to segment)

```csharp
bool HitTest(Vector2 pdfPt, float tolerancePdf)
{
    // Project pdfPt onto the line segment
    Vector2 ab = EndPdf - StartPdf;
    float t = Vector2.Dot(pdfPt - StartPdf, ab) / ab.LengthSquared();
    t = Math.Clamp(t, 0f, 1f);
    Vector2 nearest = StartPdf + t * ab;
    return Vector2.Distance(pdfPt, nearest) <= tolerancePdf;
}
```

### Circle hit test (distance to circumference, not area)

```csharp
bool HitTest(Vector2 pdfPt, float tolerancePdf)
{
    float d = Vector2.Distance(pdfPt, CenterPdf);
    // Hit if near the rim OR near the center (for selection)
    return Math.Abs(d - RadiusPdf) <= tolerancePdf
        || d <= tolerancePdf;  // center point
}
```

### Hit test ordering (what gets selected when objects overlap)

Evaluate in this priority order, return first match:
1. Selected object re-checked (sticky selection helps gaze users maintain selection)
2. Most recently created objects first (painter's order — topmost drawn = topmost selectable)
3. Point objects before line objects before circles (smaller objects win ties)

---

## Undo / Redo — Command Pattern

### Command interface

```csharp
interface IGeometryCommand
{
    void Execute(GeometryStore store);
    void Undo(GeometryStore store);
    string Description { get; }    // for undo history display
}
```

### Concrete commands

| Command | Execute | Undo |
|---------|---------|------|
| `CreateObjectCommand` | store.Add(obj) | store.Remove(obj.Id) |
| `DeleteObjectCommand` | store.Remove(obj.Id) | store.Add(obj) |
| `MoveObjectCommand` | obj.Translate(delta) | obj.Translate(-delta) |
| `NudgeCommand` | obj.Translate(step * direction) | obj.Translate(-step * direction) |
| `RotateProtractorCommand` | protractor.RotationDeg += delta | protractor.RotationDeg -= delta |
| `FlipProtractorCommand` | protractor.IsFlipped = !p.IsFlipped | protractor.IsFlipped = !p.IsFlipped |
| `SetTextCommand` | box.Text = newText | box.Text = oldText |
| `SetMCQSelectionCommand` | annotation.IsSelected = val | annotation.IsSelected = !val |
| `CompositeCommand` | execute all sub-commands | undo all sub-commands in reverse |

`CompositeCommand` is used for operations that affect multiple objects atomically (e.g., Reflection produces the original plus a new mirrored polygon — both are in one undo step).

### UndoStack implementation

```csharp
class UndoStack
{
    private Stack<IGeometryCommand> _undoStack = new();
    private Stack<IGeometryCommand> _redoStack = new();
    private const int MaxDepth = 100;

    void Execute(IGeometryCommand cmd)
    {
        cmd.Execute(store);
        _undoStack.Push(cmd);
        _redoStack.Clear();   // new action kills redo history
        TrimToMaxDepth();
    }

    void Undo() { if (_undoStack.TryPop(out var cmd)) { cmd.Undo(store); _redoStack.Push(cmd); } }
    void Redo() { if (_redoStack.TryPop(out var cmd)) { cmd.Execute(store); _undoStack.Push(cmd); } }

    bool CanUndo => _undoStack.Count > 0;
    bool CanRedo => _redoStack.Count > 0;
}
```

### What does NOT go through UndoStack

- Selection changes (selecting is not a destructive action)
- Mode toggle (Exam/Practice)
- Zoom / pan (view state, not content)
- Protractor-tool "first line pick" before the protractor is committed (intermediate input state, not a stored mutation)

---

## MVVM Wiring

### ViewModel responsibilities

```
MainViewModel
  ├── CurrentPage (int)               → page nav buttons
  ├── ZoomFactor (double)             → zoom controls
  ├── AppMode (Exam | Practice)       → mode chip color
  ├── CanUndo / CanRedo (bool)        → undo/redo button enabled
  └── Commands: UndoCommand, RedoCommand, OpenFileCommand, SaveCommand

ToolViewModel
  ├── ActiveTool (ToolType enum)      → left rail selection state
  └── ToolSelectedCommand

SelectionViewModel
  ├── SelectedObject (GeometryObject?) → right rail visibility
  ├── SelectionType (enum)             → which right-rail panel to show
  ├── NudgeCommand(direction, stepPx)
  ├── RotateCommand(deltaDeg)
  ├── DeleteSelectedCommand
  └── LockSelectedCommand

CanvasViewModel
  ├── PdfBitmap (CanvasBitmap?)       → render layer 1
  ├── Objects (IReadOnlyList<GeometryObject>) → render layer 2
  ├── LiveFeedback (SnapFeedback?)    → render layer 3
  └── CoordinateMapper                → used by RenderService
```

### Binding approach

WinUI 3 strongly prefers `x:Bind` over `{Binding}` for performance. Use compiled bindings throughout. ViewModels implement `INotifyPropertyChanged` via `CommunityToolkit.Mvvm`'s `[ObservableProperty]` source generator.

The Win2D CanvasAnimatedControl does NOT bind to ViewModel properties directly. Instead:
- ViewModels mutate the GeometryStore and CoordinateMapper (plain CLR objects, not DependencyObjects)
- GeometryStore fires a simple C# event (`ObjectsChanged`)
- RenderService (called from the Draw callback) reads directly from GeometryStore/CoordinateMapper — no binding involved
- This avoids marshalling geometry state through the WinUI 3 binding system, which would add latency

### Tool state machine state (NOT in ViewModel)

The multi-click state machine for tools (e.g., "have first click, awaiting second for Line") lives in InputController, not in ViewModel. It is transient interaction state. ViewModels only reflect committed state.

---

## Protractor Placement Algorithm

This is the most complex single operation. It must be correct.

### Inputs
- LineObject A (baseline — defines 0° reference)
- LineObject B (second arm)

### Steps

```
1. Compute intersection point of infinite lines A and B
       (use standard 2D line intersection formula in PDF space)
       If lines are parallel: no intersection → abort, show "lines do not intersect" hint

2. Compute baseline angle of line A from positive-X axis
       baselineAngleDeg = atan2(A.End.Y - A.Start.Y, A.End.X - A.Start.X) * 180/π

3. Create ProtractorObject:
       Center = intersection point
       RotationDeg = baselineAngleDeg
       Style = user's last chosen style (default Classic180)
       IsFlipped = false

4. Push CreateObjectCommand(protractorObj) to UndoStack

5. Transition tool state back to Select, auto-select the new protractor
```

### Live angle readout (Practice Mode only)

```
// Compute angle between the two arms at the intersection
arm1Dir = normalize(A.End - intersection)   // or A.Start, whichever is away from intersection
arm2Dir = normalize(B.End - intersection)
angleDeg = acos(dot(arm1Dir, arm2Dir)) * 180/π
// Ensure 0–180° range; handle ambiguity via cross product sign
```

---

## Suggested Build Order (Dependency Graph)

Dependencies flow downward. Each item can only be built after everything it depends on.

```
1. CoordinateMapper (pure math, no dependencies)
        ↓
2. GeometryObject base class + PointObject + LineObject (depend on CoordinateMapper types)
        ↓
3. GeometryStore (depends on GeometryObject)
   UndoStack (depends on GeometryStore)
        ↓
4. PDFService + CanvasBitmap rendering (depends on CoordinateMapper for page size)
        ↓
5. RenderService / Draw loop on CanvasAnimatedControl (depends on GeometryStore, PDFService, CoordinateMapper)
        ↓
6. InputController + ToolStateMachine — Select + Line tool only (depends on GeometryStore, UndoStack, CoordinateMapper)
   HitTester (depends on GeometryStore, CoordinateMapper)
        ↓
7. MVVM ViewModels wired to GeometryStore, UndoStack, ToolViewModel
   WinUI 3 shell: MainWindow + LeftRail + RightRail (depends on ViewModels)
        ↓
8. SnapEngine (depends on GeometryStore, CoordinateMapper, HitTester)
   Integrate SnapEngine into InputController
        ↓
9. CircleObject + Circle tool (same stack as Line tool, lower priority)
        ↓
10. ProtractorObject + Protractor two-line pick flow (depends on SnapEngine, LineObject, intersection math)
         ↓
11. Right-rail NudgeBlock wired to NudgeCommand + RotateCommand
    Orientation snap (V/H/45°) wired to SnapEngine
         ↓
12. ReflectVerb: select Line + shape → CompositeCommand producing mirrored polygon
         ↓
13. TextBoxObject + TextTool (simple text box, no in-app keyboard)
    MCQAnnotation (click-to-select regions on PDF)
         ↓
14. PersistenceService (JSON sidecar, depends on all object types being defined)
    Auto-save, session resume
         ↓
15. Practice/Exam mode toggle, live angle readout
    Settings panel, density/theme/accent
    Self-contained EXE packaging
```

### Critical path

Items 1–8 are the critical path. Nothing works visually until step 5 (PDF + draw loop), and nothing is interactive until step 6 (input controller with hit testing). Protractor (step 10) depends on having a working snap engine (step 8), not just the draw loop.

---

## Patterns to Follow

### Pattern 1: Retained-mode geometry, immediate-mode drawing

The geometry store is retained (objects persist between frames). The draw loop is immediate (re-draws everything each frame from the store). This matches Win2D's design and GeoGebra's approach. Do NOT attempt an invalidation / dirty-rect scheme in v1 — redrawing all objects each frame at 60 FPS is negligible cost for a scene with < 100 objects.

### Pattern 2: CoordinateMapper as single source of truth for transforms

Never compute zoom/pan transforms inline in a Draw method or a HitTest method. All coordinate conversion goes through one class. This was the key insight from Inkscape's architecture — coordinate bugs are almost always caused by equivalent-but-slightly-different transform expressions scattered across the codebase.

### Pattern 3: Snap runs in PDF space, feedback drawn in canvas space

Do snap arithmetic in PDF space (tolerances scale correctly). Convert the snapped point to canvas space only at the last moment for rendering feedback (snap ring, ghost line). This eliminates a class of bugs where zoom changes make snapping feel inconsistent.

### Pattern 4: Commands own the geometry mutation

No GeometryObject modifies itself from outside a Command. `GeometryStore.Add/Remove/Modify` are only called from `IGeometryCommand.Execute/Undo`. The Input Controller creates commands; it does not call store methods directly. This is the Paint.NET approach and it makes undo correct by construction.

### Pattern 5: Tool FSM state is private to InputController

The "awaiting second click for Line" state is never exposed to ViewModels or UI. The only thing the UI needs to know is "Line tool is active" (ToolViewModel) and what snap feedback to show (LiveFeedback property on CanvasViewModel). Internal tool sequence state is InputController's private business.

---

## Anti-Patterns to Avoid

### Anti-Pattern 1: Storing canvas-space coordinates in geometry objects

**What:** Saving `StartPdf` as a canvas pixel position that "happens to be correct at the current zoom."
**Why bad:** Every zoom change requires re-computing all stored positions. Pan changes require adding offsets to all objects. Object positions drift with floating-point accumulation.
**Instead:** All geometry stored in PDF space. CoordinateMapper converts at render time only.

### Anti-Pattern 2: Shared PointObject references between lines

**What:** `LineObject.Start` is a `PointObject` shared with another `LineObject.End`, so moving the point moves both lines.
**Why bad:** Creates a dependency graph. Undo becomes non-trivial (undo a move on the shared point must know which line it belonged to). Deleting a line must decide whether to delete the shared point. Intersection detection becomes ambiguous.
**Instead:** Lines store independent endpoint coordinates. Snap visually suggests coincidence; it does not enforce structural coupling.

### Anti-Pattern 3: Binding geometry state to WinUI 3 DependencyProperties

**What:** Making `GeometryObject` a `DependencyObject` and binding its properties into XAML.
**Why bad:** XAML binding is designed for UI state, not hundreds of small geometry objects. Overhead of dependency property change propagation will cause jank on large scenes. Win2D draw sessions are not part of the XAML visual tree.
**Instead:** GeometryStore is a plain C# observable collection. RenderService reads it directly in the Draw callback. ViewModels observe the store for aggregate UI state (count of objects, selection state) but not per-object geometry.

### Anti-Pattern 4: Running intersection computation every pointer-move tick across all object pairs

**What:** Recomputing all O(n²) intersections on every PointerMoved event for snap feedback.
**Why bad:** At 60 Hz with many objects, this burns CPU needlessly.
**Instead:** Endpoint snap runs every tick (cheap, O(n)). Intersection snap only runs when no endpoint snap found AND cursor is near an existing object (two-phase: coarse proximity check first, then precise intersection math). At MVP scene sizes this is still fast enough without a spatial index.

### Anti-Pattern 5: Tool logic in ViewModels

**What:** Putting line-creation state machine in ToolViewModel or MainViewModel.
**Why bad:** MVVM ViewModels are for UI state. Multi-click tool sequences are transient interaction state that has no business being in a ViewModel. Results in awkward "first click flag" properties on the ViewModel.
**Instead:** Tool FSM lives in InputController, a plain C# class that knows nothing about XAML. ViewModels only reflect committed results.

### Anti-Pattern 6: Blocking the UI thread for PDF rendering

**What:** Calling `PdfPage.RenderToStreamAsync(...).GetAwaiter().GetResult()` on the UI thread.
**Why bad:** PDF rendering, especially at high zoom, can take 200–500ms. This freezes the window and is unacceptable for a gaze app where visual responsiveness is critical.
**Instead:** Render PDF pages on a background task. Show previous-zoom bitmap (scaled) while new bitmap loads. Swap in new bitmap atomically in the Draw callback.

---

## Scalability Considerations

| Concern | At MVP (< 30 objects) | Future (100+ objects) | Notes |
|---------|----------------------|----------------------|-------|
| Hit testing | O(n) linear scan | Add quadtree if > 100 objects | Not needed for v1 |
| Intersection snap | O(n²) all pairs | Prune by proximity first | Already the plan |
| PDF rendering | One page at a time, cached | LRU cache, 3-page window | Sufficient for exam use |
| Geometry serialization | Full JSON rewrite on each save | Delta / append log | Not needed for single-exam sessions |
| Undo history | In-memory stack, 100 steps | No change needed | Geometry objects are small |

---

## Sources

- WinUI 3 + Win2D architecture: Training data (MEDIUM confidence). Phase 0 validation required on exam machine per PROJECT.md decisions.
- CoordinateMapper pattern: Derived from Inkscape's coordinate system design (well-documented in Inkscape developer docs, training data).
- Command pattern for undo: Standard GoF pattern; applied as in Paint.NET architecture (Paint.NET author's blog posts, training data).
- Snap algorithm structure: Classical computational geometry; tolerance-based approach matches GeoGebra's documented interaction design.
- GeoGebra independent endpoint approach: Inferred from GeoGebra's "free" vs "dependent" object model; MathGaze uses only free objects for simplicity.
- PDF-space coordinate storage: Standard practice in vector editors (Inkscape, PDF.js, PDFium all store in document space).
- WebSearch unavailable during research session: Some implementation details flagged MEDIUM confidence; Phase 0 implementation spike will validate WinUI 3 + Win2D packaging on target machine.
