---
phase: 02-geometry-core
plan: 10
subsystem: ui
tags: [skiasharp, coordinate-mapper, dpi, snap-ring, ghost-preview, rendering]

# Dependency graph
requires:
  - phase: 02-geometry-core
    provides: ToolViewModel with GhostCursorPx and LastSnap properties; PdfCanvasViewModel with DrawGhostPreview; CoordinateMapper with _dpiScale field; ReportCanvasSize DPI wiring
provides:
  - GAP-6 closed: SetDpiScale always called before SetCanvasSize in ReportCanvasSize — _dpiScale never stale when CoordinateMapper is created or updated
  - GAP-7 closed: Snap ring renders continuously during mid-draw — free-cursor solid ring at GhostCursorPx; dashed cobalt ring at snap candidate when within 20px threshold
affects: [03-protractor, 04-session, phase-verification]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "DPI-before-canvas ordering: SetDpiScale must always precede SetCanvasSize to avoid CoordinateMapper construction with stale _dpiScale"
    - "Continuous ghost feedback: always render cursor ring during AnchorPlaced state; style changes distinguish snapped vs free states"

key-files:
  created: []
  modified:
    - MathGaze/Views/PdfCanvas.xaml.cs
    - MathGaze/ViewModels/PdfCanvasViewModel.cs

key-decisions:
  - "SetDpiScale called before SetCanvasSize in ReportCanvasSize (GAP-6 fix) — ordering enforced by code comment explaining the requirement"
  - "Snap ring always drawn when DrawState == AnchorPlaced, using GhostCursorPx as fallback center; isSnapped flag selects style (full vs lighter opacity, dashed vs solid)"

patterns-established:
  - "Call ordering guard: DPI scale must be forwarded to ViewModel before triggering any async bitmap or mapper work"
  - "Continuous ring pattern: ghost feedback is always present during draw, not gated on snap-candidate existence"

requirements-completed: [GEOM-01, GEOM-02, GEOM-03, GEOM-07]

# Metrics
duration: 12min
completed: 2026-05-04
---

# Phase 02 Plan 10: Rendering Correctness Fixes (GAP-6, GAP-7) Summary

**DPI call ordering race eliminated and snap ring now tracks cursor continuously during mid-draw, ending intermittent placement inaccuracy and flicker**

## Performance

- **Duration:** 12 min
- **Started:** 2026-05-04T00:00:00Z
- **Completed:** 2026-05-04T00:12:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Fixed GAP-6: `SetDpiScale` now called before `SetCanvasSize` in `ReportCanvasSize`, guaranteeing `_dpiScale` is always correct before `LoadCurrentPageAsync` or `CoordinateMapper` construction fires — eliminates placement inaccuracy at non-1.0 DPI monitors
- Fixed GAP-7: `DrawGhostPreview` snap ring block replaced with unconditional ring rendering during `AnchorPlaced` state — solid lighter ring tracks cursor in free space, switches to full dashed cobalt ring at snap candidates — no more disappear/reappear flicker

## Task Commits

Each task was committed atomically:

1. **Task 1: Fix DPI call order in PdfCanvas.xaml.cs (GAP-6)** - `39a8417` (fix)
2. **Task 2: Fix snap ring flicker in PdfCanvasViewModel.cs (GAP-7)** - `df3763e` (fix)

**Plan metadata:** (docs commit follows)

## Files Created/Modified

- `MathGaze/Views/PdfCanvas.xaml.cs` - Swapped `SetDpiScale`/`SetCanvasSize` call order in `ReportCanvasSize`; updated comment to explain ordering requirement
- `MathGaze/ViewModels/PdfCanvasViewModel.cs` - Replaced snap ring block in `DrawGhostPreview` with continuous ring logic using `GhostCursorPx` fallback and `isSnapped` flag for style selection

## Decisions Made

- Snap ring always draws during `AnchorPlaced` state regardless of snap candidate presence — the free-cursor ring uses 100-alpha solid stroke vs 200-alpha dashed stroke for snapped state; this distinction is clear to users and avoids the flicker entirely
- `isSnapped` local variable pattern used to avoid multiple null-checks on `LastSnap` and to make the two rendering paths explicit
- Snap dot (filled 5px circle) only drawn at snapped position, not at free-cursor position — prevents the dot from chasing the cursor and looking like a placed point

## Deviations from Plan

None — plan executed exactly as written. Both fixes implemented exactly as specified in the plan's interface pseudocode.

## Issues Encountered

None. Both changes were minimal and targeted. Build remained at 0 errors and 55 tests passed throughout.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- GAP-6 and GAP-7 are closed; the coordinate pipeline is now correct at all DPI scales
- Phase 02 geometry-core verification (`02-VERIFICATION.md`) can now be run — all known rendering gaps (GAP-1 through GAP-9) have been addressed
- No outstanding rendering correctness issues for Phase 03 protractor work

---
*Phase: 02-geometry-core*
*Completed: 2026-05-04*

## Self-Check: PASSED

- FOUND: `MathGaze/Views/PdfCanvas.xaml.cs`
- FOUND: `MathGaze/ViewModels/PdfCanvasViewModel.cs`
- FOUND: `.planning/phases/02-geometry-core/02-10-SUMMARY.md`
- FOUND commit `39a8417`: fix(02-10): swap SetDpiScale/SetCanvasSize call order in ReportCanvasSize (GAP-6)
- FOUND commit `df3763e`: fix(02-10): render snap ring continuously during mid-draw operations (GAP-7)
