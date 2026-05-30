---
phase: 07-ui-improvements
plan: 03
subsystem: right-rail-ux
tags: [drawing-guide, clear-page, three-panel, undo, gaze-targets]
dependency_graph:
  requires: [07-01, 07-02]
  provides: [drawing-guide-panel, clear-page-command, three-panel-rail]
  affects: [RightRailViewModel, ToolViewModel, RightRail.xaml, ClearPageCommand]
tech_stack:
  added: []
  patterns:
    - ToolViewModel.PropertyChanged subscription in RightRailViewModel for DrawState reactivity
    - HasSelectionPanel computed bool on RightRailViewModel (HasSelection && !HasDrawingInProgress) for XAML exclusion
    - NothingSelected panel uses BoolToInverseVisibilityConverter + DataTrigger to achieve three-state exclusion without MultiValueConverter
    - ClearPageCommand snapshot pattern (ToList() before ExecuteCommand) prevents live-collection mutation during iteration
    - IRelayCommand forwarding property (CancelDrawCommand => _toolVm.CancelDrawCommand) exposes ToolViewModel command through RightRail DataContext
key_files:
  created:
    - MathGaze/Core/Commands/ClearPageCommand.cs
  modified:
    - MathGaze/ViewModels/ToolViewModel.cs
    - MathGaze/ViewModels/RightRailViewModel.cs
    - MathGaze/Views/RightRail.xaml
decisions:
  - "NothingSelected panel visibility uses BoolToInverseVisibilityConverter (HasSelection=false) + DataTrigger (HasDrawingInProgress=true -> Collapsed) rather than HasNothingSelectedPanel computed property — avoids adding a fourth computed bool and keeps the three-state logic visible in XAML"
  - "CancelDrawCommand exposed as forwarding property on RightRailViewModel rather than changing XAML DataContext — consistent with existing pattern where all right rail bindings resolve against RightRailViewModel"
  - "HasSelectionPanel computed in RightRailViewModel (not XAML MultiValueConverter) — simpler, testable, and consistent with other bool properties on the ViewModel"
  - "DI constructor injection change (IGeometryService, ToolViewModel) requires no App.xaml.cs change — both are already AddSingleton and the container auto-resolves the new parameter"
metrics:
  duration_minutes: 3
  completed_date: "2026-05-30"
  tasks_completed: 2
  files_modified: 4
---

# Phase 7 Plan 03: Drawing Guide Panel, ClearPage, and Three-Panel Rail Summary

Snapshot-based ClearPageCommand for single-step undoable page clear; HasDrawingInProgress computed property on ToolViewModel; RightRailViewModel subscribes to ToolViewModel.PropertyChanged to drive three-panel switching (DrawingGuidePanel / NothingSelected / SelectionPanel); Clear page button always visible above undo/redo footer.

## Tasks Completed

### Task 1: Create ClearPageCommand and add HasDrawingInProgress + CancelDrawCommand to ToolViewModel

**Commit:** a99e60d

**Files:** `MathGaze/Core/Commands/ClearPageCommand.cs`, `MathGaze/ViewModels/ToolViewModel.cs`

Changes made:
- Created `ClearPageCommand.cs`: takes defensive `IReadOnlyList<GeometryObject>` snapshot in constructor; `Execute()` iterates snapshot calling `service.RemoveObject` then `service.ClearSelection()`; `Undo()` iterates snapshot calling `service.AddObject` — follows DeleteObjectCommand pattern
- Added `public bool HasDrawingInProgress => DrawState == DrawState.AnchorPlaced` to ToolViewModel
- Added `partial void OnDrawStateChanged(DrawState value)` calling `OnPropertyChanged(nameof(HasDrawingInProgress))` — CommunityToolkit.Mvvm partial method fires on every DrawState change
- Added `[RelayCommand] private void CancelDraw() => ResetDrawState()` — generates `CancelDrawCommand` for XAML binding

### Task 2: Wire RightRailViewModel to ToolViewModel and update RightRail.xaml to three-panel structure with ClearPage button

**Commit:** eca02d7

**Files:** `MathGaze/ViewModels/RightRailViewModel.cs`, `MathGaze/Views/RightRail.xaml`

Changes made:
- Added `private readonly ToolViewModel _toolVm` field
- Added `[ObservableProperty] private bool _hasDrawingInProgress`, `_hasSelectionPanel`, `_drawingInstructionText`
- Added `public IRelayCommand CancelDrawCommand => _toolVm.CancelDrawCommand` forwarding property
- Updated constructor to `(IGeometryService geometryService, ToolViewModel toolVm)` — DI auto-resolves, no App.xaml.cs change needed
- Added `OnToolPropertyChanged` handler subscribed to `_toolVm.PropertyChanged` — reacts to `HasDrawingInProgress`, `ActiveTool`, `DrawState`
- Added `UpdateDrawingState()`: sets HasDrawingInProgress, HasSelectionPanel, DrawingInstructionText (switch on ActiveTool+DrawState for per-tool hint text)
- Added `[RelayCommand] private void ClearPage()`: ToList() snapshot, early return if empty, ExecuteCommand(new ClearPageCommand(snapshot))
- Added `ClearPageCommand.NotifyCanExecuteChanged()` to Refresh()
- Called `UpdateDrawingState()` at end of `Refresh()` to keep panel state consistent after ObjectsChanged
- Added `using System.Linq` for `.ToList()`
- RightRail.xaml: replaced two-panel structure with three-panel structure:
  - DrawingGuidePanel: `Border` with `CornerRadius="10"`, accent border, `DrawingInstructionText` TextBlock, 56px Cancel button bound to `CancelDrawCommand` — visible via `BoolToVisibilityConverter` on `HasDrawingInProgress`
  - NothingSelected: existing dashed Rectangle + text; `BoolToInverseVisibilityConverter` on `HasSelection` + DataTrigger collapses it when `HasDrawingInProgress=True`
  - SelectionPanel: unchanged content; visibility now bound to `HasSelectionPanel` (was `HasSelection`)
  - ClearPage button (Height=56, DeleteButtonStyle, danger red): added as `DockPanel.Dock="Bottom"` above undo/redo row — always visible

## Build Status

`dotnet build MathGaze/MathGaze.csproj` — **Build succeeded. 0 errors, 9 warnings (all pre-existing NU1701 package compatibility warnings).**

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Border.RadiusX/RadiusY replaced with CornerRadius**

- **Found during:** Task 2 build verification
- **Issue:** `Border` in WPF uses `CornerRadius` attribute, not `RadiusX`/`RadiusY` (which are properties of `Rectangle`). The plan's XAML snippet used `RadiusX="10" RadiusY="10"` on the DrawingGuidePanel `Border`, causing MC3072 build error.
- **Fix:** Replaced with `CornerRadius="10"` — correct WPF property for Border rounded corners.
- **Files modified:** `MathGaze/Views/RightRail.xaml`
- **Commit:** eca02d7 (included in Task 2 commit)

## Known Stubs

None. All bindings are fully wired:
- DrawingGuidePanel appears when any drawing tool has AnchorPlaced state
- DrawingInstructionText shows correct per-tool hint (Line / Circle / Protractor)
- Cancel button resets draw state via CancelDrawCommand -> ToolViewModel.ResetDrawState()
- ClearPage removes all objects as single undoable action via ClearPageCommand snapshot pattern
- Clear page button is always visible regardless of selection or draw state

## Threat Flags

None. All mutations are local in-process operations. ClearPageCommand only mutates in-memory GeometryService state via the established IGeometryCommand interface. CancelDraw only clears in-memory draw state — no file I/O or network.

## Self-Check: PASSED

- `MathGaze/Core/Commands/ClearPageCommand.cs` — FOUND, service.RemoveObject CONFIRMED, service.AddObject CONFIRMED, service.ClearSelection CONFIRMED
- `MathGaze/ViewModels/ToolViewModel.cs` — FOUND, HasDrawingInProgress CONFIRMED, OnDrawStateChanged CONFIRMED, CancelDraw CONFIRMED
- `MathGaze/ViewModels/RightRailViewModel.cs` — FOUND, ToolViewModel _toolVm CONFIRMED, ToolViewModel toolVm constructor param CONFIRMED, HasDrawingInProgress CONFIRMED, HasSelectionPanel CONFIRMED, _drawingInstructionText CONFIRMED, ClearPageCommand CONFIRMED, CancelDrawCommand CONFIRMED
- `MathGaze/Views/RightRail.xaml` — FOUND, HasDrawingInProgress binding CONFIRMED, CancelDrawCommand binding CONFIRMED, ClearPageCommand binding CONFIRMED, HasSelectionPanel binding CONFIRMED
- Commit a99e60d — FOUND (Task 1)
- Commit eca02d7 — FOUND (Task 2)
- Build: 0 errors — CONFIRMED
