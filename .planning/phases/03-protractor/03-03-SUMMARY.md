---
phase: 03-protractor
plan: "03"
subsystem: ui-right-rail
tags:
  - protractor
  - right-rail
  - commands
  - xaml
  - viewmodel
dependency_graph:
  requires:
    - MathGaze/Core/Commands/RotateProtractorCommand.cs (Plan 01)
    - MathGaze/Core/Commands/FlipProtractorCommand.cs (Plan 01)
    - MathGaze/Core/Commands/StyleProtractorCommand.cs (Plan 01)
    - MathGaze/Core/Geometry/ProtractorObject.cs (Plan 01)
    - MathGaze/ViewModels/RightRailViewModel.cs (existing)
    - MathGaze/Views/RightRail.xaml (existing)
    - MathGaze/Styles/AppStyles.xaml (RailButtonStyle, StepButtonStyle — existing)
  provides:
    - RightRailViewModel protractor command set (RotateMinus5/1, RotatePlus1/5, FlipScale, SetStyleClassic, SetStyleFull)
    - RightRailViewModel.SelectedObjectType "Protractor" case
    - RightRailViewModel.IsStyleClassic / IsStyleFull observable props
    - ProtractorPanel StackPanel in RightRail.xaml (collapsed by default, visible for "Protractor")
  affects:
    - Plan 04 — protractor renderer reads SelectedObjectType; right-rail controls drive RotationOffsetDeg, IsFlipped, Style
tech_stack:
  added: []
  patterns:
    - ProtractorPanel uses DataTrigger on SelectedObjectType="Protractor" — same visibility-toggle pattern as HasSelection converters
    - Style toggle uses StepButtonStyle with Tag="active" DataTrigger (BasedOn) — same pattern as nudge step selector
    - CanProtractor() guard on all 7 protractor commands — WPF disables button when CanExecute=false (T-03-10 mitigation)
key_files:
  created: []
  modified:
    - MathGaze/ViewModels/RightRailViewModel.cs
    - MathGaze/Views/RightRail.xaml
decisions:
  - "ProtractorPanel inserted after selection type label, before nudge block — protractor-specific controls appear above the shared nudge/delete controls that all object types share"
  - "No second delete button inside ProtractorPanel — the existing shared Delete button at the bottom of the selection panel handles deletion of all selected objects including protractors"
  - "SetStyleClassic/SetStyleFull guard p.Style != newStyle before dispatching — avoids creating a no-op undo entry when re-clicking the already-active style"
metrics:
  duration_minutes: 2
  completed_date: "2026-05-25"
  tasks_completed: 2
  files_created_or_modified: 2
---

# Phase 03 Plan 03: Protractor Right-Rail Controls Summary

**One-liner:** ProtractorPanel wired to RightRail with four rotate buttons (56x56px gaze floor), flip scale, and 180°/360° style toggle — all dispatching through IGeometryService.ExecuteCommand for undo/redo; panel collapses automatically when no protractor is selected.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Add protractor commands to RightRailViewModel | 06a075e | MathGaze/ViewModels/RightRailViewModel.cs (modified) |
| 2 | Add ProtractorPanel to RightRail.xaml | e8ce645 | MathGaze/Views/RightRail.xaml (modified) |

## Verification

Final build: `dotnet build MathGaze/MathGaze.csproj` — **0 errors, 9 warnings** (all pre-existing NU1701 package target framework warnings, not introduced by this plan).

All acceptance criteria confirmed:

**RightRailViewModel.cs:**
- Contains `ProtractorObject => "Protractor"` in SelectedObjectType switch (line 52)
- Contains `RotateMinus5Command` (line 75)
- Contains `RotatePlus5Command` (line 78)
- Contains `FlipScaleCommand` (line 79)
- Contains `SetStyleClassicCommand` (line 80)
- Contains `SetStyleFullCommand` (line 81)
- Contains `IsStyleClassic` and `IsStyleFull` observable properties (lines 28-29)
- Contains `new RotateProtractorCommand` (lines 161, 168, 175, 182)
- Contains `new FlipProtractorCommand` (line 189)
- Contains `new StyleProtractorCommand` (lines 198, 207)

**RightRail.xaml:**
- Contains `x:Name="ProtractorPanel"` (line 55)
- Contains `DataTrigger Binding="{Binding SelectedObjectType}" Value="Protractor"` (line 60)
- Contains `Command="{Binding RotateMinus5Command}"` (line 78)
- Contains `Command="{Binding RotateMinus1Command}"` (line 83)
- Contains `Command="{Binding RotatePlus1Command}"` (line 88)
- Contains `Command="{Binding RotatePlus5Command}"` (line 93)
- Contains `Command="{Binding FlipScaleCommand}"` (line 102)
- Contains `Command="{Binding SetStyleClassicCommand}"` (line 115)
- Contains `Command="{Binding SetStyleFullCommand}"` (line 129)
- Contains `DataTrigger Binding="{Binding IsStyleClassic}" Value="True"` (line 121)
- Contains `DataTrigger Binding="{Binding IsStyleFull}" Value="True"` (line 135)
- All four rotate buttons have `Width="56" Height="56"` (lines 76-95)
- Flip button has `Height="56"` (line 99)

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None — this plan wires real commands to real UI. All seven commands dispatch through IGeometryService.ExecuteCommand. No placeholder values, no hardcoded empty data flowing to UI.

## Threat Flags

None — no new network endpoints, auth paths, file access patterns, or schema changes at trust boundaries.

All threat model items from the plan's `<threat_model>` are implemented:
- T-03-08: RotateProtractorCommand stores Guid; type-checked `is ProtractorObject p` lookup — wrong ID silently no-ops
- T-03-09: No DoS risk — each command is O(n) LINQ; undo stack bounded by UndoService
- T-03-10 (mitigate): `CanProtractor()` returns false when no protractor selected; WPF disables all 7 buttons when CanExecute=false

## Self-Check: PASSED

- `MathGaze/ViewModels/RightRailViewModel.cs` — FOUND
- `MathGaze/Views/RightRail.xaml` — FOUND
- Commit 06a075e — FOUND
- Commit e8ce645 — FOUND
- Build: 0 errors, 9 warnings (all pre-existing) — CONFIRMED
