---
phase: 02-geometry-core
verified: 2026-05-04T19:00:00Z
status: human_needed
score: 26/26 must-haves verified
re_verification:
  previous_status: human_needed
  previous_score: 21/21
  gaps_closed:
    - "GAP-6: Intermittent point placement inaccuracy — SetDpiScale now called BEFORE SetCanvasSize in ReportCanvasSize (DPI ordering race eliminated)"
    - "GAP-7: Snap ring flickers during mid-draw — DrawGhostPreview now always renders a ring during AnchorPlaced state using GhostCursorPx as fallback"
    - "GAP-8: Delete button hover state unreadable — DeleteButtonStyle added with dark red (#991818) hover; white text stays readable"
    - "GAP-9: Active step button loses cobalt on hover — StepButtonStyle trigger order corrected; IsMouseOver+active MultiTrigger is now last (highest WPF priority)"
    - "GAP-10: Geometry persists across PDF sessions — MainViewModel now injects IGeometryService and calls Reset() inside OpenFileAsync"
  gaps_remaining: []
  regressions: []
human_verification:
  - test: "Open a PDF, activate Point tool, click on the canvas at various positions including near the edges. Repeat at 100%, 150%, and 200% zoom."
    expected: "Committed point dot appears precisely at the clicked location at all zoom levels and screen DPIs — no visible offset"
    why_human: "DPI call ordering fix (GAP-6) confirmed in code (SetDpiScale before SetCanvasSize) but coordinate accuracy at all zoom levels requires a running app."
  - test: "Activate Line tool, click once (anchor), then move cursor around — near existing object endpoints, freely, and past the canvas edge."
    expected: "Dashed ghost line tracks precisely from anchor to cursor. Snap ring is always visible: lighter solid ring in free space, dashed cobalt ring when within 20px of a snap candidate."
    why_human: "GAP-7 fix (continuous snap ring using GhostCursorPx fallback) is confirmed in code but visual continuity and no-flicker requires a running app."
  - test: "Hover over the Delete button while an object is selected."
    expected: "Delete button hover shows dark red (#991818) background — white text 'Delete' remains readable. No cream background appears on hover."
    why_human: "GAP-8 DeleteButtonStyle hover fix confirmed in AppStyles.xaml but ControlTemplate rendering requires a running app."
  - test: "Click 1px step button; hover over it. Then click 5px; hover. Then 20px; hover."
    expected: "Active step button shows accent cobalt (#3B6FD4) background with white text and retains that cobalt colour when hovered. Non-active step buttons show cream on hover."
    why_human: "GAP-9 StepButtonStyle trigger reorder confirmed in AppStyles.xaml but WPF trigger precedence result requires a running app."
  - test: "Place geometry objects on PDF A. Then open a different PDF B. Inspect the canvas."
    expected: "PDF B opens with a completely empty canvas — no geometry objects from PDF A visible. Undo stack is also cleared (Undo button is disabled)."
    why_human: "GAP-10 Reset() call in MainViewModel confirmed in code; the correct session boundary behaviour requires a running app."
  - test: "Select a Line, click near endpoint A — verify right rail label reads 'Move endpoint A'. Nudge Up — verify object moves up on screen."
    expected: "Sub-point selection, correct label, correct nudge direction."
    why_human: "Sub-point hit tolerance (28px) and Y-axis direction require a running app to confirm."
  - test: "Place object, nudge it, click Undo — verify object returns to original position. Click Redo."
    expected: "Object position reverts on Undo; Redo re-applies the nudge. Undo/Redo buttons enable/disable correctly."
    why_human: "Button enabled-state and position restoration require a running app."
  - test: "Connect Grid 3 and complete the full interaction loop — place a Point, select it, nudge it, delete it — using only eye-gaze clicks."
    expected: "All actions respond correctly to single WM_LBUTTONDOWN events; no drag required."
    why_human: "Grid 3 compatibility requires the assistive technology device."
---

# Phase 02: Geometry Core Re-Verification Report

**Phase Goal:** Implement the full geometry layer — objects, commands, services, tool interaction, rendering, right rail UI — so a student can place, nudge, delete, and undo geometry primitives on the exam PDF using only gaze clicks.
**Verified:** 2026-05-04T19:00:00Z
**Status:** human_needed
**Re-verification:** Yes — after closure of GAP-6 through GAP-10 (plans 02-08, 02-09, 02-10)

## Re-Verification Summary

Previous verification (2026-05-03T13:00:00Z) found status `human_needed` with 21/21 automated truths passing and 5 human-only UAT items. Human UAT subsequently identified 5 new bugs (GAP-6 through GAP-10). Plans 02-08, 02-09, and 02-10 were executed to close those gaps. This re-verification confirms all 5 fixes are present in the codebase, all 55 unit tests still pass, and the build is clean. Human re-UAT is required to confirm the visual and interaction fixes.

### Gap Closure Evidence

| Gap | Fix | Code Evidence |
|-----|-----|---------------|
| GAP-6: Intermittent point placement inaccuracy | SetDpiScale called before SetCanvasSize in ReportCanvasSize | `PdfCanvas.xaml.cs` lines 100-101: `_vm.SetDpiScale(dpiInfo.PixelsPerDip)` before `_vm.SetCanvasSize(widthPx, heightPx)` |
| GAP-7: Snap ring flickers | DrawGhostPreview always draws ring during AnchorPlaced | `PdfCanvasViewModel.cs` lines 270-291: `isSnapped` flag; `ringCenter = isSnapped ? LastSnap.Position : GhostCursorPx` |
| GAP-8: Delete button hover unreadable | DeleteButtonStyle with #991818 hover | `AppStyles.xaml` line 194: `x:Key="DeleteButtonStyle"`; line 207: `#991818`; `RightRail.xaml` line 141: `DeleteButtonStyle` |
| GAP-9: Active step loses cobalt on hover | StepButtonStyle trigger order corrected | Generic IsMouseOver MultiTrigger now before IsMouseOver+active MultiTrigger in `AppStyles.xaml` |
| GAP-10: Geometry persists across PDF sessions | Reset() called in OpenFileAsync | `MainViewModel.cs` line 25: `IGeometryService geometryService` constructor param; line 256: `_geometryService.Reset()` |

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Clicking near a placed object reliably selects it at gaze tolerances (18px points, 10px lines/circles) | VERIFIED | `GeometryHitTester.cs` — `PointHitRadius=18f`, `LineHitTolerance=10f`, `CircleRingTolerance=10f`; 8 GeometryHitTesterTests confirm |
| 2 | Clicking on a line endpoint sub-selects it correctly, enabling endpoint-only nudge | VERIFIED | `GeometryHitTester.TryHitLineSubPoint` (28px radius); `ToolViewModel.HandleSelectClick` routes to sub-point detection; `RightRailViewModel.NudgeLabel` emits "Move endpoint A/B" |
| 3 | Clicking on the circle center or edge point sub-selects it correctly | VERIFIED | `GeometryHitTester.TryHitCircleSubPoint` (28px); NudgeLabel emits "Move centre"/"Move radius"; DispatchNudge selects NudgeEndpointCommand |
| 4 | Geometry positions stable when zoom changes — stored in PDF points | VERIFIED | All geometry types store double PDF-point coords; NudgeObjectCommand/NudgeEndpointCommand store `_dxPt`/`_dyPt` as PDF points; CoordinateMapper converts at render time only |
| 5 | All hit-test and math unit tests pass | VERIFIED | `dotnet test` — 55 passed, 0 failed (6 GeometryMath + 8 GeometryHitTester + 7 UndoService + 32 CoordinateMapper + 2 placeholder) |
| 6 | Placing, deleting, and nudging each create one IGeometryCommand pushed to the undo stack | VERIFIED | `GeometryService.ExecuteCommand` routes to `UndoService.Execute`; all four command types implement IGeometryCommand; 7 UndoServiceTests confirm |
| 7 | Undo reverses the last command; Redo re-executes it | VERIFIED | UndoService double-stack confirmed by 3 unit tests |
| 8 | Redoing after a new action is impossible (new action clears redo stack) | VERIFIED | `UndoService.Execute` calls `_redoStack.Clear()`; confirmed by `NewActionAfterUndo_ClearsRedoStack` test |
| 9 | NudgeObjectCommand stores a delta in PDF points so undo is zoom-independent | VERIFIED | `NudgeObjectCommand` has `private readonly double _dxPt` and `_dyPt`; precision confirmed to 6 decimal places by unit test |
| 10 | NudgeEndpointCommand nudges a single endpoint of a LineObject or sub-point of CircleObject | VERIFIED | `NudgeEndpointCommand` stores `_subPointIndex`; explicit `if (subPointIndex == 0) / else if (index == 1)` guards in `GeometryService.NudgeSubPoint` |
| 11 | GeometryService.ExecuteCommand is the single mutation entry point | VERIFIED | ToolViewModel.HandleCanvasClick creates PlaceObjectCommand and calls ExecuteCommand; RightRailViewModel.DispatchNudge and Delete both call ExecuteCommand |
| 12 | GeometryService and UndoService registered as singletons in App.xaml.cs DI container | VERIFIED | `App.xaml.cs` line 24: `AddSingleton<IGeometryService, GeometryService>()`; line 25: `AddSingleton<UndoService>()` |
| 13 | Clicking the canvas in Select mode routes to GeometryService.SetSelected / ClearSelection | VERIFIED | `ToolViewModel.HandleSelectClick` calls `SetSelected(hit.Id)` on hit or `ClearSelection()` on miss |
| 14 | In Point/Line/Circle modes, clicks commit geometry via PlaceObjectCommand | VERIFIED | `ToolViewModel.HandleCanvasClick` switch covers all three modes; each commits via `ExecuteCommand(new PlaceObjectCommand(...))` |
| 15 | MouseMove updates GhostCursorPx and triggers canvas repaint | VERIFIED | `ToolViewModel.HandleMouseMove` updates `GhostCursorPx`, fires `GhostChanged`; PdfCanvasViewModel subscribes via `OnGhostChanged` |
| 16 | SnapEngine returns nearest endpoint, intersection, or orientation candidate within 20px | VERIFIED | `SnapEngine.cs` — `SnapThresholdPx = 20f`; 3-priority algorithm |
| 17 | DPI fix: SetDpiScale called before SetCanvasSize (GAP-6 fix) | VERIFIED | `PdfCanvas.xaml.cs` lines 100-101: `SetDpiScale` appears at line 100 before `SetCanvasSize` at line 101 — ordering enforced by code comment |
| 18 | All placed geometry objects drawn as vector graphics on the canvas | VERIFIED | `GeometryLayerViewModel.Draw` handles all three cases; called from `PdfCanvasViewModel.Paint()` between DrawBitmap and DrawGhostPreview |
| 19 | SKPaint objects are cached as fields — never allocated per PaintSurface call | VERIFIED | `GeometryLayerViewModel` declares 7 `private readonly SKPaint` fields; no `new SKPaint()` inside `Draw` or `DrawObject` |
| 20 | When nothing is selected, right rail shows placeholder; when selected, shows nudge + delete | VERIFIED | `RightRail.xaml` — BoolToInverseVisibilityConverter on placeholder; BoolToVisibilityConverter on selection panel |
| 21 | All nudge tap targets are >= 56x56px and right rail buttons use app design language | VERIFIED (partial) | `RightRail.xaml` — all 4 UDLR buttons `Width="56" Height="56"` (6 total); RailButtonStyle applied; visual confirmation requires human |
| 22 | Delete button hover shows dark red (#991818) — white text remains readable (GAP-8 fix) | VERIFIED | `AppStyles.xaml` line 194: `DeleteButtonStyle`; line 207: `#991818` hover; `RightRail.xaml` line 141: `DeleteButtonStyle` applied |
| 23 | Active step button retains cobalt background on hover (GAP-9 fix) | VERIFIED | StepButtonStyle: generic IsMouseOver MultiTrigger precedes IsMouseOver+active MultiTrigger in AppStyles.xaml trigger collection |
| 24 | Snap ring always visible during mid-draw — tracks cursor in free space, snaps when within 20px (GAP-7 fix) | VERIFIED | `PdfCanvasViewModel.DrawGhostPreview` lines 270-291: `isSnapped` flag selects ringCenter; no longer gated on `LastSnap?.Label is not null` |
| 25 | Opening a new PDF clears all geometry objects and undo/redo stacks (GAP-10 fix) | VERIFIED | `MainViewModel.cs` lines 25 and 256: IGeometryService injected; `_geometryService.Reset()` called in Dispatcher.InvokeAsync inside OpenFileAsync |
| 26 | DPI call ordering: SetDpiScale precedes SetCanvasSize so CoordinateMapper never uses stale _dpiScale (GAP-6 fix) | VERIFIED | `PdfCanvas.xaml.cs` lines 98-101: comment and code confirm SetDpiScale called first |

**Score:** 26/26 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `MathGaze/Core/Geometry/GeometryObject.cs` | Abstract base with Id, IsSelected, abstract Draw/HitTest/GetSnapPoints | VERIFIED | Correct abstract contract at all 5 required members |
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
| `MathGaze/Core/SnapEngine.cs` | Snap() returning (SKPoint, string?), SnapThresholdPx=20f | VERIFIED | SnapThresholdPx=20f; 3-priority algorithm |
| `MathGaze/Services/IGeometryService.cs` | Full service interface with ExecuteCommand, ObjectsChanged, Reset, CanUndo/CanRedo | VERIFIED | All members confirmed present |
| `MathGaze/Services/GeometryService.cs` | Singleton owning object list + UndoService | VERIFIED | `private readonly UndoService _undoService = new()` |
| `MathGaze/Services/UndoService.cs` | Double-stack undo/redo | VERIFIED | `Stack<IGeometryCommand>` _undoStack + _redoStack |
| `MathGaze/ViewModels/ToolViewModel.cs` | State machine: ToolMode enum, DrawState, HandleCanvasClick, HandleMouseMove | VERIFIED | All present |
| `MathGaze/ViewModels/PdfCanvasViewModel.cs` | DPI fix, ghost rendering, geometry layer call, continuous snap ring | VERIFIED | `_dpiScale` used correctly; DrawGhostPreview with isSnapped pattern; `_geometryLayer.Draw()` |
| `MathGaze/Views/PdfCanvas.xaml.cs` | MouseDown + MouseMove handlers with DPI-correct conversion; SetDpiScale before SetCanvasSize | VERIFIED | OnMouseDown + OnMouseMove with `logicalPos.X * dpi.PixelsPerDip`; SetDpiScale at line 100 before SetCanvasSize at line 101 |
| `MathGaze/ViewModels/GeometryLayerViewModel.cs` | Draw(SKCanvas, CoordinateMapper) with 7 cached SKPaint, sub-point rendering | VERIFIED | 7 cached paints; DrawSubPointTargets method; Draw handles all 3 types |
| `MathGaze/ViewModels/RightRailViewModel.cs` | NudgeLabel (D-07), nudge/delete/undo/redo commands, corrected NudgeUp/Down signs | VERIFIED | Pattern-matching NudgeLabel; NudgeUp=+NudgeStepPx, NudgeDown=-NudgeStepPx |
| `MathGaze/Views/RightRail.xaml` | Selection-state panels, RailButtonStyle, StepButtonStyle, DeleteButtonStyle, 56x56px targets | VERIFIED | All command bindings; DeleteButtonStyle on Delete button; 6 x Width="56" Height="56"; 3 DataTriggers on NudgeStepPx |
| `MathGaze/Styles/AppStyles.xaml` | RailButtonStyle, StepButtonStyle, DeleteButtonStyle with corrected trigger order | VERIFIED | All three keys present; #991818 in DeleteButtonStyle |
| `MathGaze/ViewModels/MainViewModel.cs` | IGeometryService injected; Reset() called in OpenFileAsync | VERIFIED | Constructor param on line 25; Reset() on line 256 inside Dispatcher.InvokeAsync |
| `MathGaze.Tests/GeometryMathTests.cs` | 6 unit tests | VERIFIED | 6 [Fact] methods; all passing |
| `MathGaze.Tests/GeometryHitTesterTests.cs` | 8+ unit tests | VERIFIED | 8 [Fact] methods; all passing |
| `MathGaze.Tests/UndoServiceTests.cs` | 7 unit tests | VERIFIED | 7 [Fact] methods; all passing |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `GeometryObject.HitTest()` | `GeometryHitTester.TryHitObject()` | static helper using CoordinateMapper | WIRED | TryHitObject calls `obj.HitTest(screenPx, mapper, tolerance)` |
| `LineObject.SelectedEndpoint` | `RightRailViewModel.NudgeLabel` | ObjectsChanged → Refresh() | WIRED | Refresh() switch: `LineObject l when l.SelectedEndpoint == 0 => "Move endpoint A"` |
| `ToolViewModel.HandleCanvasClick` | `GeometryService.ExecuteCommand(new PlaceObjectCommand(...))` | direct call in switch cases | WIRED | All three tool modes commit via ExecuteCommand |
| `RightRailViewModel.NudgeUpCommand` | `IGeometryService.ExecuteCommand(NudgeObjectCommand or NudgeEndpointCommand)` | DispatchNudge() | WIRED | DispatchNudge selects command type; calls `_geometryService.ExecuteCommand(cmd)` |
| `PdfCanvas.xaml.cs OnMouseDown` | `ToolViewModel.HandleCanvasClick(physPx)` | PdfCanvasViewModel bridge | WIRED | OnMouseDown → `_vm.HandleCanvasClick(physPx)` → `_toolVm.HandleCanvasClick` |
| `PdfCanvasViewModel.Paint()` | `GeometryLayerViewModel.Draw(canvas, mapper)` | between DrawBitmap and DrawGhostPreview | WIRED | `_geometryLayer.Draw(canvas, _coordinateMapper)` at line 207 |
| `RightRailViewModel` | `IGeometryService.ObjectsChanged` | subscribes in constructor | WIRED | `_geometryService.ObjectsChanged += OnObjectsChanged` |
| `MainWindow.xaml` | `RightRail UserControl` | `views:RightRail x:Name="RightRailControl"` | WIRED | RightRailPlaceholder replaced |
| `MainWindow.xaml.cs` | `RightRailControl.DataContext = rightRailViewModel` | constructor injection | WIRED | Present at line 21 of MainWindow.xaml.cs |
| `PdfCanvasViewModel.LoadCurrentPageAsync` | `PdfCanvasViewModel.EnsureCoordinateMapper` | same `_dpiScale * ZoomFactor * 96/72` formula | WIRED | Line 310: `(_dpiScale * 96.0 / 72.0) * _mainVm.ZoomFactor`; line 347: `(_dpiScale * _mainVm.ZoomFactor * 96.0 / 72.0)` |
| `PdfCanvas.xaml.cs ReportCanvasSize` | `PdfCanvasViewModel.SetDpiScale` (before SetCanvasSize) | call ordering fix | WIRED | Line 100: SetDpiScale; line 101: SetCanvasSize — ordering enforced |
| `DrawGhostPreview` | `ToolViewModel.GhostCursorPx` | fallback ring center when not snapped | WIRED | `ringCenter = isSnapped ? LastSnap.Position : GhostCursorPx` |
| `MainViewModel.OpenFileAsync` | `IGeometryService.Reset()` | called inside Dispatcher.InvokeAsync | WIRED | `_geometryService.Reset()` at line 256 after IsPdfOpen = true |
| `RightRail.xaml Delete button` | `AppStyles.xaml DeleteButtonStyle` | `Style={StaticResource DeleteButtonStyle}` | WIRED | Line 141 of RightRail.xaml: DeleteButtonStyle |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| `RightRailViewModel` | NudgeLabel, HasSelection, SelectedObjectType | `IGeometryService.SelectedObject` (live singleton) | Yes — updated on every ObjectsChanged event | FLOWING |
| `GeometryLayerViewModel` | rendered objects | `IGeometryService.Objects` (live List) | Yes — objects added via PlaceObjectCommand.Execute → GeometryService.AddObject | FLOWING |
| `RightRail.xaml` | NudgeLabel, HasSelection, NudgeStepPx bindings | RightRailViewModel DataContext | Yes — DataContext wired at app startup; DataTriggers reactive to NudgeStepPx | FLOWING |
| `MainViewModel.OpenFileAsync` | _geometryService state | Reset() call clears real data | Yes — clears List and undo stacks before new PDF renders | FLOWING |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| All 55 tests pass | `dotnet test MathGaze.Tests/MathGaze.Tests.csproj` | Failed: 0, Passed: 55 | PASS |
| Build succeeds | `dotnet build MathGaze/MathGaze.csproj -c Debug` | 0 errors, 6 pre-existing NU1701 warnings | PASS |
| GAP-6: SetDpiScale before SetCanvasSize | Read PdfCanvas.xaml.cs ReportCanvasSize lines 98-101 | SetDpiScale at line 100 before SetCanvasSize at line 101 | PASS |
| GAP-7: Continuous snap ring using GhostCursorPx | grep `isSnapped` in PdfCanvasViewModel.cs | Lines 270-291: isSnapped flag; ringCenter fallback to GhostCursorPx | PASS |
| GAP-8: DeleteButtonStyle with #991818 | grep `DeleteButtonStyle` in AppStyles.xaml and RightRail.xaml | AppStyles line 194 (definition) + line 207 (#991818); RightRail line 141 (usage) | PASS |
| GAP-9: StepButtonStyle trigger order | Inspect AppStyles.xaml StepButtonStyle trigger sequence | Generic IsMouseOver MultiTrigger precedes IsMouseOver+active MultiTrigger | PASS |
| GAP-10: Reset() in OpenFileAsync | grep `_geometryService.Reset()` in MainViewModel.cs | Line 256: inside Dispatcher.InvokeAsync after IsPdfOpen = true | PASS |
| 56x56px gaze targets preserved | grep `Width="56" Height="56"` in RightRail.xaml | 6 matches (4 UDLR + Undo + Redo) | PASS |

### Requirements Coverage

| Requirement | Source Plan(s) | Description | Status | Evidence |
|-------------|----------------|-------------|--------|----------|
| GEOM-01 | 02-01, 02-03 | User can place a Point with one click | SATISFIED | ToolViewModel `case (ToolMode.Point, DrawState.Idle)` → `PlaceObjectCommand(new PointObject(xPt, yPt))` |
| GEOM-02 | 02-01, 02-03 | User can draw a Line with two clicks | SATISFIED | ToolViewModel — Idle stores anchor; AnchorPlaced commits LineObject via PlaceObjectCommand |
| GEOM-03 | 02-01, 02-03 | User can draw a Circle with two clicks | SATISFIED | ToolViewModel — centre + radius-point → CircleObject; radius computed via Pythagorean distance |
| GEOM-04 | 02-01, 02-04 | User can select any geometry object with one click | SATISFIED | GeometryHitTester.TryHitObject → GeometryService.SetSelected; GeometryLayerViewModel renders accent colour |
| GEOM-05 | 02-02, 02-05, 02-06, 02-07, 02-08 | User can nudge a selected object (step 1/5/20 px) | SATISFIED | RightRailViewModel NudgeStepPx {1,5,20}; DispatchNudge → command → ExecuteCommand; Y-axis corrected (GAP-3); active step highlighted (GAP-5); hover states correct (GAP-8, GAP-9) |
| GEOM-06 | 02-02, 02-05 | User can delete a selected object | SATISFIED | RightRailViewModel.Delete → DeleteObjectCommand(obj) → ExecuteCommand; undoable |
| GEOM-07 | 02-03 | User can snap to endpoints, intersections, orientation guides | SATISFIED | SnapEngine.Snap() — 3-priority: endpoints → intersections (<=6 lines) → H/V/45°; 20px threshold |
| SYS-01 | 02-02, 02-05 | User can undo/redo any action | SATISFIED | UndoService double-stack; RightRailViewModel Undo/Redo commands; buttons enabled via CanUndo/CanRedo |

**All 8 required requirements (GEOM-01 through GEOM-07, SYS-01) are SATISFIED.**

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `MathGaze/Core/Geometry/LineObject.cs` | 22-23 | `throw new NotImplementedException("Draw implemented in GeometryLayerViewModel")` | Info | Intentional stub on abstract Draw(). GeometryLayerViewModel renders via type switch — this overridden method is never called. Not a blocker. |
| `MathGaze/Core/Geometry/CircleObject.cs` | similar | Same NotImplementedException on Draw | Info | Same as above — intentional; not blocking |
| `MathGaze/Core/Geometry/PointObject.cs` | similar | Same NotImplementedException on Draw | Info | Same as above — intentional; not blocking |
| `MathGaze/ViewModels/PdfCanvasViewModel.cs` | 225-232 | Ghost preview creates `SKPaint` objects via `using var` on each PaintSurface call | Warning | Ghost paints allocated per-frame; GC pressure at 60fps. Acceptable at GCSE canvas scale per T-02-07. Not blocking. |

### Human Verification Required

Five new bugs (GAP-6 through GAP-10) were fixed in plans 02-08, 02-09, and 02-10. All fixes are confirmed in code. Human re-UAT is required to confirm the fixes produce correct visual and interaction behaviour on a running app.

**1. Point placement accuracy at all zoom levels (GAP-6 re-test)**

**Test:** Open a PDF, activate Point tool. Click at several locations. Repeat at 100%, 150%, and 200% zoom. Test on a monitor with PixelsPerDip > 1.0 if available.
**Expected:** Committed point dot appears precisely at the clicked location at all zoom levels and DPI settings — no visible offset.
**Why human:** DPI ordering fix (SetDpiScale before SetCanvasSize) confirmed in code but coordinate accuracy at all zoom/DPI combinations requires a running app.

**2. Snap ring continuous during mid-draw (GAP-7 re-test)**

**Test:** Activate Line tool, click once (anchor). Move cursor slowly across the canvas — through free space, near existing endpoints, through intersection zones.
**Expected:** A ring indicator is always visible: lighter solid ring tracks the cursor in free space; switches to dashed cobalt ring when within 20px of a snap candidate. No disappear/reappear flicker.
**Why human:** Continuous ring rendering using GhostCursorPx fallback is confirmed in code; no-flicker at 60fps requires a running app.

**3. Delete button hover state (GAP-8 re-test)**

**Test:** Select any geometry object. Hover the cursor over the Delete button in the right rail.
**Expected:** Delete button shows a dark red background (#991818) on hover. White text "Delete" remains clearly readable — no cream/white background appears.
**Why human:** DeleteButtonStyle ControlTemplate override is confirmed in AppStyles.xaml; rendered hover requires a running app.

**4. Active step button hover state (GAP-9 re-test)**

**Test:** Click 5px step button (it should highlight cobalt). Then hover over it without clicking. Then hover over the 1px button.
**Expected:** Active (5px) button retains cobalt (#3B6FD4) background on hover. Non-active (1px) button shows cream on hover.
**Why human:** StepButtonStyle trigger reorder is confirmed in AppStyles.xaml; WPF trigger precedence rendering requires a running app.

**5. Geometry state cleared on new PDF (GAP-10 re-test)**

**Test:** Place several geometry objects on PDF A. Then File > Open a different PDF B.
**Expected:** PDF B opens with a completely empty canvas. No geometry objects from PDF A visible. Undo button is disabled (stack cleared).
**Why human:** Reset() call in MainViewModel.OpenFileAsync is confirmed in code; the session-boundary clearing requires a running app to verify no objects bleed through.

**6. Sub-point selection and NudgeLabel update**

**Test:** Place a Line, click to select it (turns cobalt). Then click near endpoint A.
**Expected:** Sub-selection activates; right rail label changes to "Move endpoint A". Nudge Up moves only endpoint A upward.
**Why human:** 28px sub-point tap radius and label update require a running app.

**7. Undo/Redo button state management**

**Test:** Place an object (Undo should enable). Click Undo (object disappears, Redo should enable). Click Redo (object reappears, Redo should disable).
**Expected:** Button enabled-state tracks undo/redo stack state correctly throughout.
**Why human:** Button enabled-state and canvas repaint timing require a running app.

**8. Grid 3 / gaze input compatibility**

**Test:** Connect Grid 3; complete the full interaction loop (place, select, nudge, delete) using only eye-gaze clicks.
**Expected:** All actions respond to single WM_LBUTTONDOWN events. No drag required. All tap targets are large enough to activate via gaze dwell.
**Why human:** Grid 3 compatibility requires the assistive technology device.

### Gaps Summary

No code gaps found. All 10 UAT-identified gaps (GAP-1 through GAP-10) are closed:

- GAP-1: Point placement offset — Fixed in 02-06 (`_dpiScale` in LoadCurrentPageAsync)
- GAP-2: Ghost preview misalignment — Fixed in 02-06 (same root cause as GAP-1)
- GAP-3: Nudge direction inverted — Fixed in 02-06 (NudgeUp = +NudgeStepPx)
- GAP-4: Right rail WPF chrome — Fixed in 02-07 (RailButtonStyle)
- GAP-5: No step highlight — Fixed in 02-07 (StepButtonStyle + DataTrigger)
- GAP-6: Intermittent placement inaccuracy — Fixed in 02-10 (SetDpiScale before SetCanvasSize)
- GAP-7: Snap ring flicker — Fixed in 02-10 (continuous ring using GhostCursorPx fallback)
- GAP-8: Delete button hover unreadable — Fixed in 02-08 (DeleteButtonStyle with #991818)
- GAP-9: Active step loses cobalt on hover — Fixed in 02-08 (StepButtonStyle trigger reorder)
- GAP-10: Geometry persists across PDF sessions — Fixed in 02-09 (Reset() in OpenFileAsync)

All 55 unit tests pass. Build is clean at 0 errors. All 26 observable truths verified. All 8 requirements satisfied.

Status is `human_needed` because all 10 bug fixes require visual and interaction confirmation on a running app at the target school hardware DPI. This is the expected and appropriate state — the automated layer is complete; human re-UAT is the final gate.

---

_Verified: 2026-05-04T19:00:00Z_
_Verifier: Claude (gsd-verifier)_
_Re-verification: Yes — after GAP-6 through GAP-10 closure (plans 02-08, 02-09, 02-10)_
