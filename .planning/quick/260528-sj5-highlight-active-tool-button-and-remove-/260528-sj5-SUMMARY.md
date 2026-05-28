---
id: 260528-sj5
type: quick
date: 2026-05-28
duration_mins: 25
tasks_completed: 2
files_modified: 5
commits:
  - 58b2f71
  - 0dc4539
tags: [ui, tool-rail, cleanup, practice-mode]
---

# Quick Task 260528-sj5: Highlight Active Tool Button and Remove Practice Mode

**One-liner:** Active tool button now shows BrushAccentSoft fill + BrushAccent border via per-button DataTrigger on ToolViewModel.ActiveTool; IsPracticeMode, ToggleModeCommand, the mode pill, ComputeMeasuredAngle, DrawReadout, and three readout SKPaint/SKFont fields are fully removed.

## Tasks Completed

| # | Name | Commit | Files |
|---|------|--------|-------|
| 1 | Add active-tool highlight to all 6 ToolRail buttons | 58b2f71 | MathGaze/Views/ToolRail.xaml |
| 2 | Remove Practice Mode scaffolding | 0dc4539 | MathGaze/ViewModels/MainViewModel.cs, MathGaze/Views/TopBar.xaml, MathGaze/Styles/AppStyles.xaml, MathGaze/ViewModels/GeometryLayerViewModel.cs |

## What Changed

### Task 1 — Active tool highlight

Each of the 6 tool buttons (Select, Point, Line, Circle, Protractor, Text) in `ToolRail.xaml` now uses an inline `<Button.Style>` block instead of `Style="{StaticResource ToolTileStyle}"`. Each inline style uses `BasedOn="{StaticResource ToolTileStyle}"` and adds one `DataTrigger` on `{Binding ActiveTool}` (resolves to `ToolViewModel.ActiveTool` via the UserControl's DataContext). When the binding value matches the button's tool mode name string (e.g. `Value="Select"`), WPF's enum TypeConverter matches correctly and applies:

- `Background = {StaticResource BrushAccentSoft}` (#EEF2FB)
- `BorderBrush = {StaticResource BrushAccent}` (#3B6FD4)

No changes to AppStyles.xaml or ToolViewModel were needed.

### Task 2 — Remove Practice Mode

**MainViewModel.cs:** Removed `[ObservableProperty] private bool _isPracticeMode = true` and `[RelayCommand] private void ToggleMode()`.

**TopBar.xaml:** Removed the entire mode pill `<Button>` block (lines 82–124 in the original) including its custom `ControlTemplate`, the coloured dot `Ellipse` with `DataTrigger` on `IsPracticeMode`, and the `TextBlock` showing "Practice Mode"/"Exam Mode".

**AppStyles.xaml:** Removed `BrushExam` (#D94F2A) and `BrushPractice` (#2CA870) — confirmed no other callers in the codebase.

**GeometryLayerViewModel.cs:** Removed in full:
- `_mainVm` field and `MainViewModel mainViewModel` constructor parameter
- `_mainVm.PropertyChanged += OnMainVmPropertyChanged` subscription
- `OnMainVmPropertyChanged` handler method
- Practice Mode readout block inside `DrawProtractor` (the `if (_mainVm.IsPracticeMode && ...)` guard)
- `ComputeMeasuredAngle(ProtractorObject)` private method
- `DrawReadout(SKCanvas, float, float, bool)` private method
- Three SKPaint/SKFont fields: `_readoutArcPaint`, `_readoutTextPaint`, `_readoutFont`
- Three corresponding `Dispose()` calls

## Build Result

Compilation: zero CS errors, zero new CS warnings (pre-existing NU1701 package compatibility warnings unchanged). The only build failure was MSB3027 (file lock) because the MathGaze app was running during the build — this is a deployment step failure, not a compilation failure.

## Deviations from Plan

None. Plan executed exactly as written.

The plan noted: "If `_mainVm` has NO remaining usages after the removals above, remove the field, the constructor parameter..." — confirmed no remaining usages; both removed as instructed.

## Known Stubs

None introduced by this task.

## Threat Flags

None — no new network endpoints, auth paths, or schema changes.

## Self-Check

- [x] `MathGaze/Views/ToolRail.xaml` — modified, 6 inline styles added
- [x] `MathGaze/ViewModels/MainViewModel.cs` — IsPracticeMode and ToggleModeCommand absent
- [x] `MathGaze/Views/TopBar.xaml` — mode pill absent
- [x] `MathGaze/Styles/AppStyles.xaml` — BrushExam and BrushPractice absent
- [x] `MathGaze/ViewModels/GeometryLayerViewModel.cs` — all practice mode code absent
- [x] Commit 58b2f71 exists
- [x] Commit 0dc4539 exists

## Self-Check: PASSED
