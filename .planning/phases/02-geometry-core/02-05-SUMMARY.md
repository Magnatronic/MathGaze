---
phase: 02-geometry-core
plan: 05
subsystem: ui
tags: [wpf, xaml, mvvm, communitytoolkit, geometry, undo-redo, right-rail, nudge]

# Dependency graph
requires:
  - phase: 02-geometry-core/02-04
    provides: GeometryLayerViewModel, IGeometryService, NudgeObjectCommand, NudgeEndpointCommand, DeleteObjectCommand, PointObject/LineObject/CircleObject with sub-point selection
provides:
  - RightRailViewModel — selection-aware ViewModel observing IGeometryService.ObjectsChanged; exposes NudgeLabel (D-07 sub-point labels), NudgeUp/Down/Left/Right, Delete, Undo, Redo commands
  - RightRail.xaml — 148px WPF UserControl with NOTHING SELECTED state, contextual nudge block (1/5/20px steps + 56x56px UDLR pad), Delete button, always-visible Undo/Redo footer
  - InverseBoolToVisibilityConverter in MathGaze.Converters — collapses when bool is true, visible when false
  - BoolToVisibilityConverter and BoolToInverseVisibilityConverter registered in App.xaml application resources
  - RightRailPlaceholder replaced by live RightRail in MainWindow.xaml
affects:
  - 03-protractor (right rail pattern established for contextual verb controls)
  - 04-text-answers (same rail pattern for text/answer selection state)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - RightRailViewModel injects IGeometryService, subscribes to ObjectsChanged, calls Refresh() on every event to update all observable state in one pass
    - NudgeLabel uses C# pattern matching switch expression with sub-point guards (D-07)
    - All geometry mutations from UI flow through IGeometryService.ExecuteCommand() — no direct list access from ViewModel
    - DispatchNudge() selects NudgeEndpointCommand vs NudgeObjectCommand based on SelectedEndpoint/SelectedSubPoint — same pattern used by GeometryLayerViewModel hit testing

key-files:
  created:
    - MathGaze/ViewModels/RightRailViewModel.cs
    - MathGaze/Views/RightRail.xaml
    - MathGaze/Views/RightRail.xaml.cs
    - MathGaze/Converters/InverseBoolToVisibilityConverter.cs
  modified:
    - MathGaze/App.xaml.cs (AddSingleton<RightRailViewModel>)
    - MathGaze/App.xaml (xmlns:converters, BoolToVisibilityConverter, BoolToInverseVisibilityConverter resources)
    - MathGaze/MainWindow.xaml (RightRailPlaceholder → RightRail x:Name="RightRailControl")
    - MathGaze/MainWindow.xaml.cs (added RightRailViewModel constructor parameter, DataContext wiring)

key-decisions:
  - "RightRailViewModel.Refresh() called unconditionally on every ObjectsChanged event — O(1) switch on SelectedObject type, no performance concern at GCSE exam scale (T-02-15)"
  - "Nudge delta passed as PDF points directly (1 screen px = 1 PDF pt at zoom=1) — zoom-independence is a property of the command pattern, not the ViewModel (D-10 + Pitfall 2)"
  - "ToolTileStyle not applied to nudge directional buttons — ToolTileStyle sets Width=84 which would exceed the 56px cell size in the 3x3 UniformGrid; buttons use Width=56 Height=56 inline"
  - "Step selector buttons omit ToolTileStyle for same reason — Height=40 fits within the rail width"

patterns-established:
  - "Pattern: Selection-aware right rail — RightRailViewModel.HasSelection drives Visibility of NOTHING SELECTED vs selection panel via BoolToInverseVisibilityConverter / BoolToVisibilityConverter"
  - "Pattern: Sub-point label via pattern matching — NudgeLabel switch expression with type + property guards (LineObject l when l.SelectedEndpoint == 0) for clean D-07 implementation"

requirements-completed: [GEOM-05, GEOM-06, SYS-01]

# Metrics
duration: 8min
completed: 2026-05-03
---

# Phase 02 Plan 05: Right Rail — Nudge, Delete, Undo/Redo Summary

**RightRailViewModel + RightRail.xaml completing the student interaction loop: selection-aware nudge pad (56x56px UDLR, 1/5/20px steps), sub-point labels per D-07, Delete, and always-visible Undo/Redo**

## Performance

- **Duration:** ~8 min
- **Started:** 2026-05-03T06:59:41Z
- **Completed:** 2026-05-03T07:03:11Z
- **Tasks:** 2
- **Files modified:** 8

## Accomplishments
- RightRailViewModel fully wired to IGeometryService: 4 nudge commands, Delete, Undo, Redo, SetStep — all mutations through ExecuteCommand()
- NudgeLabel implements D-07 sub-point labelling ("Move endpoint A/B", "Move centre/radius") via pattern-matching switch expression
- RightRail.xaml replaces RightRailPlaceholder with live contextual panel — NOTHING SELECTED dashed box when empty, nudge block + delete when selected
- All 4 directional nudge buttons are exactly 56x56px satisfying the ≥56px gaze accuracy floor from CLAUDE.md
- Undo/Redo footer always visible at bottom of rail; enabled/disabled via CanUndo/CanRedo

## Task Commits

1. **Task 1: RightRailViewModel — nudge, delete, undo/redo commands** - `3776b1d` (feat)
2. **Task 2: RightRail.xaml + wire into MainWindow** - `c2a07ea` (feat)

**Plan metadata:** _(docs commit follows)_

## Files Created/Modified
- `MathGaze/ViewModels/RightRailViewModel.cs` — Selection-aware ViewModel: NudgeLabel, 4 nudge commands, Delete, Undo, Redo, SetStep
- `MathGaze/Views/RightRail.xaml` — 148px UserControl with NOTHING SELECTED state and contextual nudge block
- `MathGaze/Views/RightRail.xaml.cs` — Minimal code-behind; DataContext set by MainWindow
- `MathGaze/Converters/InverseBoolToVisibilityConverter.cs` — IValueConverter: true→Collapsed, false→Visible
- `MathGaze/App.xaml.cs` — Added `services.AddSingleton<RightRailViewModel>()`
- `MathGaze/App.xaml` — Added xmlns:converters, BoolToVisibilityConverter, BoolToInverseVisibilityConverter resources
- `MathGaze/MainWindow.xaml` — Replaced `views:RightRailPlaceholder` with `views:RightRail x:Name="RightRailControl"`
- `MathGaze/MainWindow.xaml.cs` — Added RightRailViewModel constructor parameter; wires `RightRailControl.DataContext = rightRailViewModel`

## Decisions Made
- ToolTileStyle (84x56px default width) was not applied to the 56x56px directional pad buttons or step selector buttons — applying it would set Width=84, breaking the 3x3 UniformGrid layout. Buttons use explicit Width="56" Height="56" inline to satisfy the gaze target floor while fitting within the 148px rail.
- NudgeLabel switch expression uses C# positional-when pattern guards rather than nested if/else for readability and exhaustiveness coverage.

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered
None.

## Known Stubs
None — all bindings wire to live ViewModel data. NudgeLabel, HasSelection, SelectedObjectType, and all commands update from real IGeometryService state on every ObjectsChanged event.

## Threat Flags
No new trust boundaries introduced beyond those documented in the plan's threat model (T-02-14 through T-02-17). All mitigations applied as specified.

## User Setup Required
None — no external service configuration required.

## Next Phase Readiness
- Full student interaction loop complete for geometry objects: place → select → nudge → delete → undo/redo
- GEOM-05, GEOM-06, and SYS-01 requirements satisfied
- Right rail pattern established for Phase 03 (protractor contextual controls) and Phase 04 (text/answer selection verbs)
- Ready for Phase 03: Protractor tool implementation

---
*Phase: 02-geometry-core*
*Completed: 2026-05-03*
