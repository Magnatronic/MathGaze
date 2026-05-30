---
phase: 07-ui-improvements
plan: 04
subsystem: right-rail-ux
tags: [object-list, snap, first-click-snap, gaze-targets, right-rail]
dependency_graph:
  requires: [07-01, 07-02, 07-03]
  provides: [object-list-panel, first-click-snap]
  affects: [RightRailViewModel, RightRail.xaml, ToolViewModel]
tech_stack:
  added: []
  patterns:
    - ObjectListItem as file-level class in RightRailViewModel.cs namespace (not nested — avoids CS0136 scope issues)
    - ObservableCollection<ObjectListItem> rebuilt on every ObjectsChanged — simple and correct for small page object counts
    - RelayCommand closure captures capturedId (Guid) not obj reference — correct capture pattern for loop closures (D-19)
    - HasObjectList computed in UpdateDrawingState (called by both Refresh and OnToolPropertyChanged) — single computation point
    - Idle snap for Line/Circle in HandleMouseMove — snap ring visible before first click (D-23 enhancement)
key_files:
  created: []
  modified:
    - MathGaze/ViewModels/RightRailViewModel.cs
    - MathGaze/Views/RightRail.xaml
    - MathGaze/ViewModels/ToolViewModel.cs
decisions:
  - "ObjectListItem defined as file-level class (not nested) inside RightRailViewModel.cs namespace — avoids CS0136 name conflict with 'obj' variable in Refresh(); cleaner scoping"
  - "foreach loop variable renamed geoObj (not obj) to avoid CS0136 conflict with outer 'var obj = SelectedObject' in Refresh()"
  - "HasObjectList recomputed in UpdateDrawingState() which is called by both Refresh() and OnToolPropertyChanged() — ensures HasObjectList stays accurate when ActiveTool, HasSelection, or HasDrawingInProgress changes"
metrics:
  duration_minutes: 2
  completed_date: "2026-05-30"
  tasks_completed: 2
  files_modified: 3
---

# Phase 7 Plan 04: Object List Panel and First-Click Snap Summary

ObjectListPanel added to right rail (Select tool + nothing selected state) showing per-type named rows (Line 1, Circle 2, etc.) as 48px gaze-friendly buttons; first-click snap enabled for Line, Circle, and Protractor two-point mode; snap ring shows during Idle hover for Line/Circle tools.

## Tasks Completed

### Task 1: Add ObjectListItem, ObjectList, HasObjectList to RightRailViewModel and update RightRail.xaml with ObjectListPanel

**Commit:** 6adde58

**Files:** `MathGaze/ViewModels/RightRailViewModel.cs`, `MathGaze/Views/RightRail.xaml`

Changes made:
- Added `using System.Collections.ObjectModel` to RightRailViewModel.cs
- Added `[ObservableProperty] private bool _hasObjectList` to RightRailViewModel
- Added `public ObservableCollection<ObjectListItem> ObjectList { get; } = new()` to RightRailViewModel
- Updated `UpdateDrawingState()` to compute `HasObjectList = ActiveTool == Select && !HasSelection && !HasDrawingInProgress`
- Updated `Refresh()` to rebuild ObjectList: iterates `_geometryService.Objects`, assigns per-type counters (lineCount, circleCount, etc.), creates `ObjectListItem` with `DisplayName = "{typeName} {idx}"`, `TypeLabel`, and `RelayCommand` closing over `capturedId`
- Added `ObjectListItem` as file-level class with `DisplayName`, `TypeLabel`, `SelectCommand` (IRelayCommand) properties
- RightRail.xaml: replaced "NOTHING SELECTED" Grid with ObjectListPanel `Border` bound to `HasObjectList`
- ObjectListPanel: `ItemsControl` with DataTemplate — 48px `Button` per item, type chip `Border` (BrushAccentSoft/BrushAccent) + `TextBlock` display name
- Empty state: dashed `Rectangle` box with "No objects on this page" shown when `ObjectList.Count == 0`

### Task 2: Add first-click snapping for Line, Circle, and Protractor two-point mode in ToolViewModel

**Commit:** 7beb67b

**Files:** `MathGaze/ViewModels/ToolViewModel.cs`

Changes made:
- Line Idle case: replaced `mapper.ScreenToPage(screenPx)` with `snap.Snap(screenPx, ...)` → `mapper.ScreenToPage(snappedPx)` — anchor now snaps to existing geometry (D-22)
- Circle Idle case: same pattern — centre snaps to existing geometry on first click (D-24)
- Protractor Idle else branch: added `snap.Snap` call before `ScreenToPage` so vertex snaps to existing geometry (D-24)
- HandleMouseMove: added `else if (DrawState == DrawState.Idle && ActiveTool is ToolMode.Line or ToolMode.Circle)` branch that calls `snap.Snap` and sets `LastSnap` — snap ring visible before first click (D-23)
- Second-click snap cases (Line AnchorPlaced, Circle AnchorPlaced) unchanged

## Build Status

`dotnet build MathGaze/MathGaze.csproj` — **Build succeeded. 0 errors, 9 warnings (all pre-existing NU1701 package compatibility warnings).**

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Renamed foreach loop variable obj → geoObj to resolve CS0136 scope conflict**

- **Found during:** Task 1 build verification
- **Issue:** `Refresh()` already uses `var obj = _geometryService.SelectedObject` at the top of the method. Adding `foreach (var obj in objects)` inside the same method body caused CS0136 ("A local or parameter named 'obj' cannot be declared in this scope because that name is used in an enclosing local scope"). C# does not allow shadowing a local variable with another local in a nested scope.
- **Fix:** Renamed the foreach loop variable to `geoObj` (and updated all references within the loop body). Also renamed `var objects` to `var pageObjects` for clarity.
- **Files modified:** `MathGaze/ViewModels/RightRailViewModel.cs`
- **Commit:** 6adde58 (included in Task 1 commit)

**2. [Rule 1 - Bug] ObjectListItem defined as file-level class (not nested) to avoid CS0136**

- **Found during:** Task 1 — anticipating scope issues from plan's suggested nested class placement
- **Issue:** The plan suggested adding `ObjectListItem` as a nested class inside `RightRailViewModel`. However, nesting it would require placing it inside the `RightRailViewModel` class body which uses `partial class` with generated code from CommunityToolkit.Mvvm — the generated partial class already defines the class boundary. File-level placement in the same namespace is cleaner and avoids any partial-class ordering issues.
- **Fix:** Placed `ObjectListItem` as a file-level `public sealed class` after the closing brace of `RightRailViewModel`, within the same `MathGaze.ViewModels` namespace. No separate file needed.
- **Files modified:** `MathGaze/ViewModels/RightRailViewModel.cs`
- **Commit:** 6adde58 (included in Task 1 commit)

## Known Stubs

None. All bindings fully wired:
- ObjectListPanel appears when Select tool active + no selection + no drawing in progress
- Each row's SelectCommand calls `_geometryService.SetSelected(capturedId)` — selects the object and ObjectsChanged fires, triggering Refresh() which recomputes HasObjectList=false (selection now exists) causing the panel to collapse and SelectionPanel to appear
- Empty state shows "No objects on this page" when ObjectList.Count == 0
- First-click snap active for Line, Circle, Protractor two-point mode
- Snap ring shows during Idle hover for Line and Circle tools

## Threat Flags

None. All mutations are local in-process operations. ObjectListItem.SelectCommand closes over a `Guid` from in-memory geometry state. SnapEngine returns bounded screen-pixel coordinates from existing in-process geometry data — no external input.

## Self-Check: PASSED

- `MathGaze/ViewModels/RightRailViewModel.cs` — FOUND
  - `ObservableCollection<ObjectListItem> ObjectList` — CONFIRMED (line 47)
  - `[ObservableProperty] private bool _hasObjectList` — CONFIRMED (line 41)
  - `class ObjectListItem` — CONFIRMED (line 338)
  - `capturedId = geoObj.Id` closure pattern — CONFIRMED (line 173)
  - `HasObjectList` in `UpdateDrawingState` — CONFIRMED
- `MathGaze/Views/RightRail.xaml` — FOUND
  - `HasObjectList` BoolToVisibilityConverter binding — CONFIRMED (line 66)
  - `ItemsSource="{Binding ObjectList}"` — CONFIRMED (line 97)
  - `Command="{Binding SelectCommand}"` — CONFIRMED (line 115)
- `MathGaze/ViewModels/ToolViewModel.cs` — FOUND
  - `snap.Snap(screenPx, _geometryService.Objects, mapper)` in Line Idle — CONFIRMED (line 114)
  - `snap.Snap(screenPx, _geometryService.Objects, mapper)` in Circle Idle — CONFIRMED (line 126)
  - `snap.Snap(screenPx, _geometryService.Objects, mapper)` in Protractor Idle else — CONFIRMED (line 139)
  - `DrawState == DrawState.Idle && ActiveTool is ToolMode.Line or ToolMode.Circle` branch — CONFIRMED (line 362)
- Commit 6adde58 — FOUND (Task 1)
- Commit 7beb67b — FOUND (Task 2)
- Build: 0 errors — CONFIRMED
