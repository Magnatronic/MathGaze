---
phase: 02-geometry-core
verified: 2026-05-05T10:00:00Z
status: human_needed
score: 29/29 must-haves verified
re_verification:
  previous_status: human_needed
  previous_score: 26/26
  gaps_closed:
    - "GAP-11: First click placement inaccuracy (race between SetDpiScale/SetCanvasSize and first click) — EnsureCoordinateMapper() now called synchronously inside both SetDpiScale and SetCanvasSize, eliminating all possible ordering races"
    - "GAP-12: Horizontal orientation snap not triggering — SnapEngine section 3 now uses absolute SnapThresholdPx gate on all three orientation guide checks (H/V/45°) instead of bestDist gate, preventing suppression by prior endpoint snaps"
    - "GAP-13: Geometry from page N persists on page N+1 — _geometryService.Reset() added as first statement in OnCurrentPageChanged, giving each page an independent canvas and undo stack"
  gaps_remaining: []
  regressions: []
human_verification:
  - test: "Open a PDF, activate Point tool, click at various positions. Repeat at 100%, 150%, and 200% zoom."
    expected: "Committed point dot appears precisely at the clicked location at all zoom levels and screen DPIs — no visible offset."
    why_human: "DPI call ordering fix (GAP-6/GAP-11) and EnsureCoordinateMapper in SetDpiScale/SetCanvasSize confirmed in code, but coordinate accuracy at all zoom/DPI combinations requires a running app."
  - test: "Activate Line tool, click once (anchor), then move cursor around — near existing endpoints, freely, and past the canvas edge."
    expected: "Dashed ghost line tracks precisely from anchor to cursor. Snap ring is always visible: lighter solid ring in free space, dashed cobalt ring within 20px of a snap candidate. Horizontal snap (0°/180°) fires when cursor aligns with an existing point at same Y."
    why_human: "Ghost rendering and snap label correctness confirmed in code; visual continuity requires a running app."
  - test: "Hover over the Delete button while an object is selected."
    expected: "Delete button hover shows dark red (#991818) background. White text 'Delete' remains readable."
    why_human: "DeleteButtonStyle ControlTemplate confirmed in AppStyles.xaml; rendered hover requires a running app."
  - test: "Click 1px step button; hover over it. Then click 5px; hover. Then 20px; hover."
    expected: "Active step button shows accent cobalt (#3B6FD4) on hover. Non-active buttons show cream on hover."
    why_human: "StepButtonStyle trigger order confirmed in AppStyles.xaml; WPF trigger precedence requires a running app."
  - test: "Place geometry objects on PDF A. Then open a different PDF B."
    expected: "PDF B opens with a completely empty canvas. Undo stack is cleared (Undo button disabled)."
    why_human: "Reset() in OpenFileAsync confirmed in code; session boundary behaviour requires a running app."
  - test: "Open a multi-page PDF. Draw geometry on page 1. Navigate to page 2."
    expected: "Page 2 canvas is empty — no geometry from page 1 bleeds through. Undo button is disabled. Navigate back to page 1 — also empty."
    why_human: "GAP-13 Reset() in OnCurrentPageChanged confirmed in code; visual bleed requires a running app."
  - test: "Select a Line, click near endpoint A — verify right rail label reads 'Move endpoint A'. Nudge Up — verify object moves up on screen."
    expected: "Sub-point selection, correct label, correct nudge direction."
    why_human: "Sub-point hit tolerance (28px) and Y-axis direction require a running app."
  - test: "Place object, nudge it, click Undo — verify object returns to original position. Click Redo."
    expected: "Object position reverts on Undo; Redo re-applies the nudge. Undo/Redo buttons enable/disable correctly."
    why_human: "Button enabled-state and position restoration require a running app."
  - test: "Connect Grid 3 and complete the full interaction loop — place a Point, select it, nudge it, delete it — using only eye-gaze clicks."
    expected: "All actions respond correctly to single WM_LBUTTONDOWN events; no drag required."
    why_human: "Grid 3 compatibility requires the assistive technology device."
---

# Phase 02: Geometry Core Re-Verification Report (3rd pass)

**Phase Goal:** Users can place, select, adjust, and delete geometry objects on top of the PDF using only clicks
**Verified:** 2026-05-05T10:00:00Z
**Status:** human_needed
**Re-verification:** Yes — after closure of GAP-11 (plan 02-11), GAP-12 (plan 02-12), GAP-13 (plan 02-13)

## Re-Verification Summary

Previous verification (2026-05-04T19:00:00Z) found status `human_needed` with 26/26 automated truths passing and 8 human-only UAT items, with no remaining code gaps. Since that verification, plans 02-11, 02-12, and 02-13 were executed to fix three additional bugs discovered during continued human UAT. This re-verification confirms all three new fixes are present in the codebase, all 62 unit tests (up from 55) pass, and the build remains clean.

### New Gap Closure Evidence (Plans 02-11, 02-12, 02-13)

| Gap | Plan | Fix | Code Evidence |
|-----|------|-----|---------------|
| GAP-11: First click placement inaccuracy | 02-11 | EnsureCoordinateMapper() called inside SetDpiScale and SetCanvasSize | `PdfCanvasViewModel.cs` line 101: `EnsureCoordinateMapper()` in SetDpiScale; line 144: `EnsureCoordinateMapper()` in SetCanvasSize (after OnCanvasSizeChanged) |
| GAP-12: Horizontal snap not triggering | 02-12 | SnapEngine section 3 uses absolute SnapThresholdPx gate for H/V/45° | `SnapEngine.cs` lines 80, 88, 98: all three orientation guards use `SnapThresholdPx` instead of `bestDist`; separate `orientBestDist` variable prevents cross-type suppression |
| GAP-13: Geometry bleeds across pages | 02-13 | Reset() called in OnCurrentPageChanged | `MainViewModel.cs` line 138: `_geometryService.Reset()` as first statement in `OnCurrentPageChanged`; line 260: original OpenFileAsync Reset() preserved |

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Clicking near a placed object reliably selects it at gaze tolerances (18px points, 10px lines/circles) | VERIFIED | `GeometryHitTester.cs` — `PointHitRadius=18f`, `LineHitTolerance=10f`, `CircleRingTolerance=10f`; 8 GeometryHitTesterTests confirm |
| 2 | Clicking on a line endpoint sub-selects it correctly, enabling endpoint-only nudge | VERIFIED | `GeometryHitTester.TryHitLineSubPoint` (28px radius); `ToolViewModel.HandleSelectClick` routes to sub-point detection; `RightRailViewModel.NudgeLabel` emits "Move endpoint A/B" |
| 3 | Clicking on the circle center or edge point sub-selects it correctly | VERIFIED | `GeometryHitTester.TryHitCircleSubPoint` (28px); NudgeLabel emits "Move centre"/"Move radius"; DispatchNudge selects NudgeEndpointCommand |
| 4 | Geometry positions stable when zoom changes — stored in PDF points | VERIFIED | All geometry types store double PDF-point coords; NudgeObjectCommand/NudgeEndpointCommand store `_dxPt`/`_dyPt` as PDF points; CoordinateMapper converts at render time only |
| 5 | All hit-test and math unit tests pass | VERIFIED | `dotnet test` — 62 passed, 0 failed (6 GeometryMath + 8 GeometryHitTester + 7 UndoService + 7 SnapEngine + 32 CoordinateMapper + 2 placeholder) |
| 6 | Placing, deleting, and nudging each create one IGeometryCommand pushed to the undo stack | VERIFIED | `GeometryService.ExecuteCommand` routes to `UndoService.Execute`; all four command types implement IGeometryCommand; 7 UndoServiceTests confirm |
| 7 | Undo reverses the last command; Redo re-executes it | VERIFIED | UndoService double-stack confirmed by 3 unit tests |
| 8 | Redoing after a new action is impossible (new action clears redo stack) | VERIFIED | `UndoService.Execute` calls `_redoStack.Clear()`; confirmed by `NewActionAfterUndo_ClearsRedoStack` test |
| 9 | NudgeObjectCommand stores a delta in PDF points so undo is zoom-independent | VERIFIED | `NudgeObjectCommand` has `private readonly double _dxPt` and `_dyPt`; precision confirmed to 6 decimal places by unit test |
| 10 | NudgeEndpointCommand nudges a single endpoint of a LineObject or sub-point of CircleObject | VERIFIED | `NudgeEndpointCommand` stores `_subPointIndex`; explicit `if (subPointIndex == 0) / else` guards in `GeometryService.NudgeSubPoint` |
| 11 | GeometryService.ExecuteCommand is the single mutation entry point | VERIFIED | ToolViewModel.HandleCanvasClick creates PlaceObjectCommand and calls ExecuteCommand; RightRailViewModel.DispatchNudge and Delete both call ExecuteCommand |
| 12 | GeometryService and UndoService registered as singletons in App.xaml.cs DI container | VERIFIED | `App.xaml.cs` — `AddSingleton<IGeometryService, GeometryService>()` and `AddSingleton<UndoService>()` present |
| 13 | Clicking the canvas in Select mode routes to GeometryService.SetSelected / ClearSelection | VERIFIED | `ToolViewModel.HandleSelectClick` calls `SetSelected(hit.Id)` on hit or `ClearSelection()` on miss |
| 14 | In Point/Line/Circle modes, clicks commit geometry via PlaceObjectCommand | VERIFIED | `ToolViewModel.HandleCanvasClick` switch covers all three modes; each commits via `ExecuteCommand(new PlaceObjectCommand(...))` |
| 15 | MouseMove updates GhostCursorPx and triggers canvas repaint | VERIFIED | `ToolViewModel.HandleMouseMove` updates `GhostCursorPx`, fires `GhostChanged`; PdfCanvasViewModel subscribes via `OnGhostChanged` |
| 16 | SnapEngine returns nearest endpoint, intersection, or orientation candidate within 20px | VERIFIED | `SnapEngine.cs` — `SnapThresholdPx = 20f`; 3-priority algorithm; horizontal snap fixed by absolute SnapThresholdPx gate (GAP-12); 7 SnapEngineTests confirm all types including horizontal |
| 17 | DPI fix: SetDpiScale called before SetCanvasSize (GAP-6 fix) | VERIFIED | `PdfCanvas.xaml.cs` line 100: `SetDpiScale` before line 101: `SetCanvasSize` — ordering preserved |
| 18 | All placed geometry objects drawn as vector graphics on the canvas | VERIFIED | `GeometryLayerViewModel.Draw` handles all three cases; called from `PdfCanvasViewModel.Paint()` between DrawBitmap and DrawGhostPreview |
| 19 | SKPaint objects are cached as fields — never allocated per PaintSurface call | VERIFIED | `GeometryLayerViewModel` declares 7 `private readonly SKPaint` fields; no `new SKPaint()` inside `Draw` or `DrawObject` |
| 20 | When nothing is selected, right rail shows placeholder; when selected, shows nudge + delete | VERIFIED | `RightRail.xaml` — BoolToInverseVisibilityConverter on placeholder; BoolToVisibilityConverter on selection panel |
| 21 | All nudge tap targets are >= 56x56px and right rail buttons use app design language | VERIFIED | `RightRail.xaml` — all 4 UDLR + Undo + Redo buttons `Width="56" Height="56"`; RailButtonStyle applied |
| 22 | Delete button hover shows dark red (#991818) — white text remains readable (GAP-8 fix) | VERIFIED | `AppStyles.xaml` — `DeleteButtonStyle` with `#991818` hover; `RightRail.xaml` uses `DeleteButtonStyle` |
| 23 | Active step button retains cobalt background on hover (GAP-9 fix) | VERIFIED | StepButtonStyle: generic IsMouseOver MultiTrigger precedes IsMouseOver+active MultiTrigger in AppStyles.xaml trigger collection |
| 24 | Snap ring always visible during mid-draw — tracks cursor in free space (GAP-7 fix) | VERIFIED | `PdfCanvasViewModel.DrawGhostPreview` — `isSnapped` flag selects `ringCenter`; fallback to `GhostCursorPx` when not snapped |
| 25 | Opening a new PDF clears all geometry objects and undo/redo stacks (GAP-10 fix) | VERIFIED | `MainViewModel.cs` line 260: `_geometryService.Reset()` inside Dispatcher.InvokeAsync in OpenFileAsync |
| 26 | CoordinateMapper always synchronised before first click — no SetDpiScale/SetCanvasSize race (GAP-11 fix) | VERIFIED | `PdfCanvasViewModel.cs` line 101: `EnsureCoordinateMapper()` in `SetDpiScale`; line 144: `EnsureCoordinateMapper()` in `SetCanvasSize` (after `OnCanvasSizeChanged`) |
| 27 | Horizontal orientation snap triggers within 20px of existing points (GAP-12 fix) | VERIFIED | `SnapEngine.cs` lines 80, 88, 98: outer guards use `SnapThresholdPx` not `bestDist`; separate `orientBestDist` prevents cross-type suppression; 2 dedicated unit tests (Snap_HorizontalAlignment_* in SnapEngineTests.cs) confirm |
| 28 | Navigating to a new page clears all geometry — no bleed-through across pages (GAP-13 fix) | VERIFIED | `MainViewModel.cs` line 138: `_geometryService.Reset()` as first statement in `OnCurrentPageChanged`; OpenFileAsync Reset() preserved at line 260 (double call on PDF open is idempotent) |
| 29 | Build is clean and all 62 unit tests pass | VERIFIED | `dotnet build MathGaze/MathGaze.csproj -c Debug` — 0 errors, 6 pre-existing NU1701 warnings; `dotnet test` — Failed: 0, Passed: 62 |

**Score:** 29/29 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `MathGaze/Core/Geometry/GeometryObject.cs` | Abstract base with Id, IsSelected, abstract Draw/HitTest/GetSnapPoints | VERIFIED | Correct abstract contract |
| `MathGaze/Core/Geometry/PointObject.cs` | PointObject with XPt/YPt | VERIFIED | Full HitTest + GetSnapPoints |
| `MathGaze/Core/Geometry/LineObject.cs` | LineObject with X1Pt/Y1Pt/X2Pt/Y2Pt, SelectedEndpoint | VERIFIED | `int? SelectedEndpoint` present |
| `MathGaze/Core/Geometry/CircleObject.cs` | CircleObject with CenterXPt/Y/RadiusPt, SelectedSubPoint | VERIFIED | `int? SelectedSubPoint` present |
| `MathGaze/Core/GeometryMath.cs` | DistancePointToSegment, TryLineIntersect | VERIFIED | Both static methods present |
| `MathGaze/Core/GeometryHitTester.cs` | TryHitObject, TryHitLineSubPoint, TryHitCircleSubPoint, SubPointTapRadius=28f | VERIFIED | All three methods; SubPointTapRadius=28f |
| `MathGaze/Core/Commands/IGeometryCommand.cs` | Execute/Undo taking IGeometryService | VERIFIED | Both methods present |
| `MathGaze/Core/Commands/PlaceObjectCommand.cs` | Adds/removes object | VERIFIED | Execute AddObject; Undo RemoveObject |
| `MathGaze/Core/Commands/DeleteObjectCommand.cs` | Removes/restores object | VERIFIED | Execute RemoveObject; Undo AddObject |
| `MathGaze/Core/Commands/NudgeObjectCommand.cs` | PDF-point deltas for whole-object nudge | VERIFIED | `private readonly double _dxPt` confirmed |
| `MathGaze/Core/Commands/NudgeEndpointCommand.cs` | Sub-point index + PDF-point deltas | VERIFIED | `private readonly int _subPointIndex` present |
| `MathGaze/Core/SnapEngine.cs` | Snap() returning (SKPoint, string?), SnapThresholdPx=20f, absolute orientation gates | VERIFIED | SnapThresholdPx=20f; separate orientBestDist; 4+ occurrences of SnapThresholdPx in orientation section |
| `MathGaze/Services/IGeometryService.cs` | Full service interface with ExecuteCommand, ObjectsChanged, Reset, CanUndo/CanRedo | VERIFIED | All members confirmed present |
| `MathGaze/Services/GeometryService.cs` | Singleton owning object list + UndoService | VERIFIED | `private readonly UndoService _undoService = new()` |
| `MathGaze/Services/UndoService.cs` | Double-stack undo/redo | VERIFIED | `Stack<IGeometryCommand>` _undoStack + _redoStack |
| `MathGaze/ViewModels/ToolViewModel.cs` | State machine: ToolMode enum, DrawState, HandleCanvasClick, HandleMouseMove | VERIFIED | All present |
| `MathGaze/ViewModels/PdfCanvasViewModel.cs` | DPI fix, ghost rendering, geometry layer call, continuous snap ring, EnsureCoordinateMapper in SetDpiScale/SetCanvasSize | VERIFIED | `_dpiScale` used correctly; DrawGhostPreview with isSnapped pattern; `_geometryLayer.Draw()`; EnsureCoordinateMapper at lines 101, 144, 149 |
| `MathGaze/Views/PdfCanvas.xaml.cs` | MouseDown + MouseMove handlers; SetDpiScale before SetCanvasSize | VERIFIED | OnMouseDown + OnMouseMove with `logicalPos.X * dpi.PixelsPerDip`; SetDpiScale at line 100 before SetCanvasSize at line 101 |
| `MathGaze/ViewModels/GeometryLayerViewModel.cs` | Draw(SKCanvas, CoordinateMapper) with 7 cached SKPaint, sub-point rendering | VERIFIED | 7 cached paints; DrawSubPointTargets method; Draw handles all 3 types |
| `MathGaze/ViewModels/RightRailViewModel.cs` | NudgeLabel (D-07), nudge/delete/undo/redo commands, corrected NudgeUp/Down signs | VERIFIED | Pattern-matching NudgeLabel; NudgeUp=+NudgeStepPx, NudgeDown=-NudgeStepPx |
| `MathGaze/Views/RightRail.xaml` | Selection-state panels, RailButtonStyle, StepButtonStyle, DeleteButtonStyle, 56x56px targets | VERIFIED | All command bindings; DeleteButtonStyle on Delete button; 6 x Width="56" Height="56"; 3 DataTriggers on NudgeStepPx |
| `MathGaze/Styles/AppStyles.xaml` | RailButtonStyle, StepButtonStyle, DeleteButtonStyle with corrected trigger order | VERIFIED | All three keys present; #991818 in DeleteButtonStyle; StepButtonStyle trigger order corrected |
| `MathGaze/ViewModels/MainViewModel.cs` | IGeometryService injected; Reset() in OpenFileAsync (GAP-10) AND OnCurrentPageChanged (GAP-13) | VERIFIED | Constructor param; Reset() at lines 138 (OnCurrentPageChanged) and 260 (OpenFileAsync) |
| `MathGaze.Tests/GeometryMathTests.cs` | 6 unit tests | VERIFIED | 6 [Fact] methods; all passing |
| `MathGaze.Tests/GeometryHitTesterTests.cs` | 8+ unit tests | VERIFIED | 8 [Fact] methods; all passing |
| `MathGaze.Tests/UndoServiceTests.cs` | 7 unit tests | VERIFIED | 7 [Fact] methods; all passing |
| `MathGaze.Tests/SnapEngineTests.cs` | 7 unit tests including horizontal snap | VERIFIED | 7 [Fact] methods; includes Snap_HorizontalAlignment_*, Snap_VerticalAlignment_*, Snap_NoNearbyPoints, Snap_CursorOnPoint, Snap_HorizontalAlignment_JustOutsideEndpointThreshold (GAP-12 regression test); all passing |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `GeometryObject.HitTest()` | `GeometryHitTester.TryHitObject()` | static helper using CoordinateMapper | WIRED | TryHitObject calls `obj.HitTest(screenPx, mapper, tolerance)` |
| `LineObject.SelectedEndpoint` | `RightRailViewModel.NudgeLabel` | ObjectsChanged → Refresh() | WIRED | Refresh() switch: `LineObject l when l.SelectedEndpoint == 0 => "Move endpoint A"` |
| `ToolViewModel.HandleCanvasClick` | `GeometryService.ExecuteCommand(new PlaceObjectCommand(...))` | direct call in switch cases | WIRED | All three tool modes commit via ExecuteCommand |
| `RightRailViewModel.NudgeUpCommand` | `IGeometryService.ExecuteCommand(NudgeObjectCommand or NudgeEndpointCommand)` | DispatchNudge() | WIRED | DispatchNudge selects command type; calls `_geometryService.ExecuteCommand(cmd)` |
| `PdfCanvas.xaml.cs OnMouseDown` | `ToolViewModel.HandleCanvasClick(physPx)` | PdfCanvasViewModel bridge | WIRED | OnMouseDown → `_vm.HandleCanvasClick(physPx)` → `_toolVm.HandleCanvasClick` |
| `PdfCanvasViewModel.Paint()` | `GeometryLayerViewModel.Draw(canvas, mapper)` | between DrawBitmap and DrawGhostPreview | WIRED | `_geometryLayer.Draw(canvas, _coordinateMapper)` present in Paint() |
| `RightRailViewModel` | `IGeometryService.ObjectsChanged` | subscribes in constructor | WIRED | `_geometryService.ObjectsChanged += OnObjectsChanged` |
| `MainWindow.xaml` | `RightRail UserControl` | `views:RightRail x:Name="RightRailControl"` | WIRED | RightRailPlaceholder replaced |
| `PdfCanvasViewModel.SetDpiScale` | `EnsureCoordinateMapper()` | direct call after field update | WIRED | Line 101: `EnsureCoordinateMapper()` immediately after `_dpiScale = pixelsPerDip` |
| `PdfCanvasViewModel.SetCanvasSize` | `EnsureCoordinateMapper()` | direct call after OnCanvasSizeChanged | WIRED | Line 144: `EnsureCoordinateMapper()` after `_mainVm.OnCanvasSizeChanged()`; line 149 in else branch |
| `MainViewModel.OnCurrentPageChanged` | `IGeometryService.Reset()` | direct call as first statement | WIRED | `_geometryService.Reset()` at line 138 — first statement in OnCurrentPageChanged |
| `MainViewModel.OpenFileAsync` | `IGeometryService.Reset()` | called inside Dispatcher.InvokeAsync | WIRED | `_geometryService.Reset()` at line 260 — preserved from GAP-10 fix |
| `SnapEngine section 3` | `SnapThresholdPx` (absolute gate) | outer if-guard for H/V/45° | WIRED | Lines 80/88/98 each use `< SnapThresholdPx` not `< bestDist`; separate `orientBestDist` avoids cross-type suppression |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| `RightRailViewModel` | NudgeLabel, HasSelection, SelectedObjectType | `IGeometryService.SelectedObject` (live singleton) | Yes — updated on every ObjectsChanged event | FLOWING |
| `GeometryLayerViewModel` | rendered objects | `IGeometryService.Objects` (live List) | Yes — objects added via PlaceObjectCommand.Execute → GeometryService.AddObject | FLOWING |
| `RightRail.xaml` | NudgeLabel, HasSelection, NudgeStepPx bindings | RightRailViewModel DataContext | Yes — DataContext wired at app startup; DataTriggers reactive to NudgeStepPx | FLOWING |
| `MainViewModel.OnCurrentPageChanged` | _geometryService state | Reset() call clears real data on page change | Yes — clears List and undo stacks before new page renders | FLOWING |
| `MainViewModel.OpenFileAsync` | _geometryService state | Reset() call clears real data on new PDF | Yes — clears List and undo stacks before new document renders | FLOWING |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| All 62 tests pass | `dotnet test MathGaze.Tests/MathGaze.Tests.csproj` | Failed: 0, Passed: 62 | PASS |
| Build succeeds | `dotnet build MathGaze/MathGaze.csproj -c Debug` | 0 errors, 6 pre-existing NU1701 warnings | PASS |
| GAP-11: EnsureCoordinateMapper in SetDpiScale | grep `EnsureCoordinateMapper` in PdfCanvasViewModel.cs | Lines 101, 121, 144, 149, 198 — present in SetDpiScale and SetCanvasSize | PASS |
| GAP-12: Horizontal snap absolute gate | Read SnapEngine.cs lines 79-84 | `if (dH < SnapThresholdPx)` — absolute gate confirmed; separate orientBestDist | PASS |
| GAP-13: Reset in OnCurrentPageChanged | grep `_geometryService.Reset()` in MainViewModel.cs | Lines 138 (OnCurrentPageChanged) and 260 (OpenFileAsync) | PASS |
| Horizontal snap tests pass | `dotnet test` (SnapEngineTests) | 7 SnapEngine tests pass including Snap_HorizontalAlignment_JustOutsideEndpointThreshold | PASS |

### Requirements Coverage

| Requirement | Source Plan(s) | Description | Status | Evidence |
|-------------|----------------|-------------|--------|----------|
| GEOM-01 | 02-01, 02-03 | User can place a Point with one click | SATISFIED | ToolViewModel `case (ToolMode.Point, DrawState.Idle)` → `PlaceObjectCommand(new PointObject(xPt, yPt))` |
| GEOM-02 | 02-01, 02-03 | User can draw a Line with two clicks | SATISFIED | ToolViewModel — Idle stores anchor; AnchorPlaced commits LineObject via PlaceObjectCommand |
| GEOM-03 | 02-01, 02-03 | User can draw a Circle with two clicks | SATISFIED | ToolViewModel — centre + radius-point → CircleObject; radius via Pythagorean distance |
| GEOM-04 | 02-01, 02-04 | User can select any geometry object with one click | SATISFIED | GeometryHitTester.TryHitObject → GeometryService.SetSelected; GeometryLayerViewModel renders accent colour |
| GEOM-05 | 02-02, 02-05, 02-06, 02-07, 02-08 | User can nudge a selected object (step 1/5/20 px) | SATISFIED | RightRailViewModel NudgeStepPx {1,5,20}; DispatchNudge → command → ExecuteCommand; Y-axis corrected; active step highlighted; hover states correct |
| GEOM-06 | 02-02, 02-05 | User can delete a selected object | SATISFIED | RightRailViewModel.Delete → DeleteObjectCommand(obj) → ExecuteCommand; undoable |
| GEOM-07 | 02-03, 02-12 | User can snap to endpoints, intersections, orientation guides | SATISFIED | SnapEngine.Snap() — 3-priority: endpoints → intersections (<=6 lines) → H/V/45°; horizontal gap fixed with absolute SnapThresholdPx gate; 7 SnapEngineTests confirm all three orientation types |
| SYS-01 | 02-02, 02-05 | User can undo/redo any action | SATISFIED | UndoService double-stack; RightRailViewModel Undo/Redo commands; buttons enabled via CanUndo/CanRedo |

**All 8 required requirements (GEOM-01 through GEOM-07, SYS-01) are SATISFIED.**

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `MathGaze/Core/Geometry/LineObject.cs` | Draw() | `throw new NotImplementedException("Draw implemented in GeometryLayerViewModel")` | Info | Intentional stub — GeometryLayerViewModel renders via type switch; Draw() never called. Not a blocker. |
| `MathGaze/Core/Geometry/CircleObject.cs` | Draw() | Same NotImplementedException on Draw | Info | Same as above — intentional; not blocking |
| `MathGaze/Core/Geometry/PointObject.cs` | Draw() | Same NotImplementedException on Draw | Info | Same as above — intentional; not blocking |
| `MathGaze/ViewModels/PdfCanvasViewModel.cs` | DrawGhostPreview | Ghost preview creates `SKPaint` via `using var` on each PaintSurface call | Warning | Per-frame allocation; GC pressure at 60fps. Acceptable at GCSE canvas scale. Not blocking. |

### Human Verification Required

The same human UAT items from the previous verification remain. Three additional items were added for GAP-11, GAP-12, GAP-13 fixes.

**1. Point placement accuracy at all zoom levels (GAP-6/GAP-11 combined re-test)**

**Test:** Open a PDF, activate Point tool. Click at several locations. Repeat at 100%, 150%, and 200% zoom. Test on a monitor with PixelsPerDip > 1.0 if available.
**Expected:** Committed point dot appears precisely at the clicked location at all zoom levels and DPI settings.
**Why human:** DPI ordering and EnsureCoordinateMapper synchronisation confirmed in code; pixel-accurate placement requires a running app.

**2. Snap ring continuous during mid-draw (GAP-7 re-test)**

**Test:** Activate Line tool, click once. Move cursor through free space and near endpoints.
**Expected:** Ring always visible — lighter solid ring in free space, dashed cobalt at snap candidates. No flicker.
**Why human:** GhostCursorPx fallback confirmed in code; no-flicker at 60fps requires a running app.

**3. Horizontal orientation snap in the running app (GAP-12 re-test)**

**Test:** Place a Point. Activate Line tool, click anchor, then move cursor to the same Y level as the placed point but 25px to the right or left.
**Expected:** Snap ring snaps to the horizontal alignment and status shows "snap: horizontal". At less than 20px from the point itself, snap ring shows at the point ("snap: point").
**Why human:** SnapEngine fix confirmed by unit tests; visual snap ring and label in the running app require visual confirmation.

**4. Page navigation clears geometry (GAP-13 re-test)**

**Test:** Open a multi-page PDF. Draw geometry on page 1. Click Next Page.
**Expected:** Page 2 canvas is completely empty. Undo button is disabled. Navigate back — page 1 is also empty.
**Why human:** Reset() in OnCurrentPageChanged confirmed in code; canvas clearing requires a running app.

**5. Delete button hover state (GAP-8 re-test)**

**Test:** Select any geometry object. Hover cursor over the Delete button.
**Expected:** Dark red (#991818) background on hover. White text readable.
**Why human:** ControlTemplate rendering requires a running app.

**6. Active step button hover state (GAP-9 re-test)**

**Test:** Click 5px step button. Hover over it.
**Expected:** Active button retains cobalt on hover. Non-active shows cream.
**Why human:** WPF trigger precedence requires a running app.

**7. Sub-point selection and NudgeLabel update**

**Test:** Place a Line, select it, click near endpoint A.
**Expected:** Right rail shows "Move endpoint A". Nudge Up moves only endpoint A upward.
**Why human:** 28px sub-point tap radius requires a running app.

**8. Undo/Redo button state management**

**Test:** Place object (Undo enables). Undo (Redo enables). Redo (Redo disables).
**Expected:** Correct button enable/disable tracking.
**Why human:** Button enabled-state and repaint timing require a running app.

**9. Grid 3 / gaze input compatibility**

**Test:** Connect Grid 3; complete full interaction loop using only eye-gaze clicks.
**Expected:** All actions on WM_LBUTTONDOWN only. No drag required.
**Why human:** Grid 3 compatibility requires the assistive technology device.

### Gaps Summary

No code gaps found. All 13 UAT-identified gaps (GAP-1 through GAP-13) are now closed:

- GAP-1: Point placement offset — Fixed in 02-06
- GAP-2: Ghost preview misalignment — Fixed in 02-06
- GAP-3: Nudge direction inverted — Fixed in 02-06
- GAP-4: Right rail WPF chrome — Fixed in 02-07
- GAP-5: No step highlight — Fixed in 02-07
- GAP-6: Intermittent placement inaccuracy (DPI ordering) — Fixed in 02-10
- GAP-7: Snap ring flicker — Fixed in 02-10
- GAP-8: Delete button hover unreadable — Fixed in 02-08
- GAP-9: Active step loses cobalt on hover — Fixed in 02-08
- GAP-10: Geometry persists across PDF sessions — Fixed in 02-09
- GAP-11: First click placement inaccuracy (SetCanvasSize race) — Fixed in 02-11
- GAP-12: Horizontal snap not triggering — Fixed in 02-12
- GAP-13: Geometry bleeds across pages — Fixed in 02-13

All 62 unit tests pass. Build clean at 0 errors. All 29 observable truths verified. All 8 requirements satisfied.

Status is `human_needed` because the rendering fixes, interaction fixes, and page-navigation behaviour require visual confirmation on a running app. The automated layer is complete; human re-UAT is the final gate.

---

_Verified: 2026-05-05T10:00:00Z_
_Verifier: Claude (gsd-verifier)_
_Re-verification: Yes — after GAP-11 through GAP-13 closure (plans 02-11, 02-12, 02-13)_
