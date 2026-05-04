---
phase: 02-geometry-core
plan: 08
subsystem: ui
tags: [wpf, xaml, styles, button, hover, right-rail, gaze-ux]

# Dependency graph
requires:
  - phase: 02-geometry-core
    provides: RightRail.xaml with nudge controls, Delete button, StepButtonStyle for 1/5/20px step selector

provides:
  - DeleteButtonStyle in AppStyles.xaml — danger-red button with dark red (#991818) hover, white text readable at all times
  - Corrected StepButtonStyle trigger order — active step button retains cobalt on hover

affects:
  - 02-geometry-core (any plan that adds new right-rail buttons using RailButtonStyle or StepButtonStyle)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "WPF trigger precedence: last trigger in ControlTemplate.Triggers collection wins on same property — use ordering to express priority"
    - "DeleteButtonStyle: BasedOn RailButtonStyle, Template override — inherits all property setters, replaces ControlTemplate only"
    - "Danger-red button pattern: inline Background/Foreground on Button element feed into TemplateBinding; style-derived ControlTemplate uses those values"

key-files:
  created: []
  modified:
    - MathGaze/Styles/AppStyles.xaml
    - MathGaze/Views/RightRail.xaml

key-decisions:
  - "DeleteButtonStyle uses full ControlTemplate override (not Trigger override) because the base RailButtonStyle IsMouseOver trigger targets TargetName=Bd directly, bypassing TemplateBinding — only a new Template can intercept this at the correct layer"
  - "StepButtonStyle trigger order fix: generic IsMouseOver MultiTrigger moved to position 2, IsMouseOver+active MultiTrigger moved to position 3 (last); no Setter values changed, only collection order"

patterns-established:
  - "Pattern: When a ControlTemplate trigger targets a named element (TargetName=Bd) and must behave differently for a subclass, derive the style and override the full Template rather than adding a Trigger to the base template"

requirements-completed: [GEOM-05, GEOM-06]

# Metrics
duration: 8min
completed: 2026-05-04
---

# Phase 02 Plan 08: Hover-State Visibility Bug Fixes Summary

**WPF trigger ordering fix for StepButtonStyle (cobalt retained on hover) and DeleteButtonStyle with dark red (#991818) hover to keep white text readable**

## Performance

- **Duration:** ~8 min
- **Started:** 2026-05-04T18:20:00Z
- **Completed:** 2026-05-04T18:28:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- GAP-8 closed: Delete button hover now shows #991818 (dark red) instead of cream — white text remains readable when gaze dwells on the button
- GAP-9 closed: StepButtonStyle trigger order corrected — generic IsMouseOver MultiTrigger now at position 2, IsMouseOver+active MultiTrigger at position 3 (last = highest WPF priority); active step button retains cobalt background on hover
- Build: 0 errors; Tests: 55 passed, 0 failed

## Task Commits

Each task was committed atomically:

1. **Task 1: Add DeleteButtonStyle and fix StepButtonStyle trigger order** — `b97e869` (feat)
2. **Task 2: Apply DeleteButtonStyle to Delete button in RightRail.xaml** — `b86e0dc` (feat)

## Files Created/Modified

- `MathGaze/Styles/AppStyles.xaml` — Added DeleteButtonStyle (BasedOn RailButtonStyle, custom ControlTemplate with #991818 hover); reordered StepButtonStyle triggers so active+hover MultiTrigger is last
- `MathGaze/Views/RightRail.xaml` — Delete button Style changed from RailButtonStyle to DeleteButtonStyle

## Decisions Made

- DeleteButtonStyle uses a full ControlTemplate override rather than an additional Trigger. Rationale: the base RailButtonStyle IsMouseOver trigger uses `TargetName="Bd"` to set the Border Background directly, bypassing `TemplateBinding Background`. The only way to intercept this for a derived style is to replace the Template entirely. BasedOn still inherits all property Setters (Background, Foreground, FontFamily, Cursor, FocusVisualStyle, etc.) so no values are duplicated.
- StepButtonStyle fix is purely an ordering change — no Setter values were modified. WPF resolves conflicting triggers on the same property by last-wins within the collection, so moving the `IsMouseOver+active` MultiTrigger to position 3 (after generic hover at position 2) gives it higher priority.

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- GAP-8 and GAP-9 are fully resolved; right rail hover states are correct for all button types
- RailButtonStyle remains unchanged — nudge pad and undo/redo buttons retain existing cream hover behaviour
- Ready to proceed with remaining geometry-core plans

---
*Phase: 02-geometry-core*
*Completed: 2026-05-04*
