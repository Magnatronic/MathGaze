---
phase: 02-geometry-core
plan: "06"
subsystem: ui
tags: [wpf, skiasharp, coordinate-mapper, dpi, nudge, pdf-rendering]

# Dependency graph
requires:
  - phase: 02-geometry-core/02-05
    provides: PdfCanvasViewModel, RightRailViewModel, CoordinateMapper, GeometryService, SnapEngine

provides:
  - DPI-correct bitmap rendering in LoadCurrentPageAsync (GAP-1/GAP-2 closed)
  - Correct NudgeUp/NudgeDown Y-axis direction in RightRailViewModel (GAP-3 closed)

affects: [02-geometry-core, human-UAT]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Scale formula must include _dpiScale in both LoadCurrentPageAsync and EnsureCoordinateMapper — they must be identical"
    - "PDF Y-axis is bottom-origin (Y increases upward); NudgeUp increases YPt (+), NudgeDown decreases YPt (-)"

key-files:
  created: []
  modified:
    - MathGaze/ViewModels/PdfCanvasViewModel.cs
    - MathGaze/ViewModels/RightRailViewModel.cs

key-decisions:
  - "GAP-1/GAP-2: LoadCurrentPageAsync must multiply by _dpiScale to produce physical-pixel bitmap dimensions matching EnsureCoordinateMapper's coordinate space"
  - "GAP-3: PDF Y-axis is 0=bottom, increasing upward; NudgeUp must pass +NudgeStepPx to increase YPt and move object up on screen"

patterns-established:
  - "Scale formula invariant: (_dpiScale * zoomFactor * 96.0 / 72.0) must be used consistently across both bitmap sizing and coordinate mapping"

requirements-completed: [GEOM-01, GEOM-02, GEOM-03, GEOM-05, GEOM-07]

# Metrics
duration: 10min
completed: 2026-05-03
---

# Phase 02 Plan 06: UAT Gap Fix Summary

**DPI-correct PDF bitmap sizing and Y-axis-corrected nudge direction — three UAT blocking gaps closed with two one-line fixes**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-05-03T11:56:11Z
- **Completed:** 2026-05-03T12:06:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- GAP-1 closed: committed geometry objects now appear at the exact gaze cursor position (no systematic DPI offset)
- GAP-2 closed: ghost preview line/arc and snap ring track the actual cursor position during mid-draw
- GAP-3 closed: Up nudge moves object upward on screen; Down nudge moves object downward

## Task Commits

Each task was committed atomically:

1. **Task 1: Fix LoadCurrentPageAsync bitmap scale to include _dpiScale** - `9afb495` (fix)
2. **Task 2: Fix NudgeUp/NudgeDown Y-axis sign inversion** - `67bc460` (fix)

**Plan metadata:** (docs commit follows)

## Files Created/Modified

- `MathGaze/ViewModels/PdfCanvasViewModel.cs` - Added `_dpiScale` multiplier to `LoadCurrentPageAsync` scale formula so bitmap pixel dimensions match the physical-pixel coordinate space used by `EnsureCoordinateMapper`
- `MathGaze/ViewModels/RightRailViewModel.cs` - Swapped signs in `NudgeUp` (+NudgeStepPx) and `NudgeDown` (-NudgeStepPx) to match PDF Y-axis convention (0=bottom, positive=upward)

## Decisions Made

- The scale formula `(_dpiScale * ZoomFactor * 96.0 / 72.0)` is now established as the canonical form used in both `LoadCurrentPageAsync` and `EnsureCoordinateMapper`. They must remain identical.
- PDF Y-axis sign convention documented in inline comments on both NudgeUp and NudgeDown for future maintainers.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None. Both bugs had a single-line root cause exactly as described in the plan's `<interfaces>` section. Build passed clean on first attempt; 55/55 tests passed with no regressions.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- All three UAT gaps (GAP-1, GAP-2, GAP-3) from human verification are now resolved.
- The geometry layer is ready for human re-UAT: place a Point, verify it appears at click position; nudge Up, verify object moves upward.
- Remaining UAT gaps (GAP-4, GAP-5) are tracked in earlier plans if applicable.
- Phase 02 geometry core is complete pending re-UAT sign-off.

---
*Phase: 02-geometry-core*
*Completed: 2026-05-03*

## Self-Check: PASSED

- FOUND: MathGaze/ViewModels/PdfCanvasViewModel.cs
- FOUND: MathGaze/ViewModels/RightRailViewModel.cs
- FOUND: .planning/phases/02-geometry-core/02-06-SUMMARY.md
- FOUND: commit 9afb495 (fix LoadCurrentPageAsync bitmap scale)
- FOUND: commit 67bc460 (fix NudgeUp/NudgeDown sign inversion)
