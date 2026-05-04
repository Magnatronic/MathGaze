---
phase: 02-geometry-core
plan: 09
subsystem: ui
tags: [wpf, mvvm, geometry, dependency-injection, communitytoolkit]

# Dependency graph
requires:
  - phase: 02-geometry-core
    provides: GeometryService.Reset() implementation and IGeometryService interface
provides:
  - MainViewModel injects IGeometryService and calls Reset() on every PDF open
  - Geometry objects and undo/redo stacks are cleared before first page renders on new document
affects: [session-persistence, geometry-rendering]

# Tech tracking
tech-stack:
  added: []
  patterns: [Constructor injection of IGeometryService into MainViewModel follows same pattern as ToolViewModel]

key-files:
  created: []
  modified:
    - MathGaze/ViewModels/MainViewModel.cs

key-decisions:
  - "Reset() placed inside the first Dispatcher.InvokeAsync block in OpenFileAsync so it executes on the UI thread, matching all other geometry mutations and preventing cross-thread ObjectsChanged events"
  - "Reset() called after ScrollOffsetY = 0 and before OnDocumentOpenedAsync — geometry cleared before first canvas paint of new document"

patterns-established:
  - "PDF session boundary pattern: OpenFileAsync calls _geometryService.Reset() inside UI-thread InvokeAsync before triggering canvas render"

requirements-completed: [GEOM-01, GEOM-02, GEOM-03, GEOM-04, GEOM-05, GEOM-06, SYS-01]

# Metrics
duration: 5min
completed: 2026-05-04
---

# Phase 02 Plan 09: Geometry State Cleared on PDF Open Summary

**IGeometryService injected into MainViewModel; Reset() called on UI thread inside OpenFileAsync before OnDocumentOpenedAsync — geometry objects and undo/redo stack cleared on every PDF session boundary**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-05-04T00:00:00Z
- **Completed:** 2026-05-04T00:05:00Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments
- Added `IGeometryService` constructor parameter and private field to `MainViewModel`
- Called `_geometryService.Reset()` inside the UI-thread `Dispatcher.InvokeAsync` block in `OpenFileAsync`, after `ScrollOffsetY = 0` and before `OnDocumentOpenedAsync`
- DI automatically resolves `IGeometryService` (already registered as singleton in `App.xaml.cs`) — no changes needed to App.xaml.cs
- GAP-10 closed: opening a second PDF no longer renders ghost geometry objects from the previous session

## Task Commits

Each task was committed atomically:

1. **Task 1: Inject IGeometryService into MainViewModel and call Reset() on PDF open** - `de6b209` (fix)

## Files Created/Modified
- `MathGaze/ViewModels/MainViewModel.cs` - Added `IGeometryService` field + constructor parameter; added `_geometryService.Reset()` inside `OpenFileAsync` `Dispatcher.InvokeAsync` lambda

## Decisions Made
- Reset() placed inside the first `Dispatcher.InvokeAsync` block so it executes on the UI thread — consistent with all geometry mutations (T-02-09-02 mitigation)
- Reset() called before `OnDocumentOpenedAsync` to ensure geometry is cleared before the first canvas paint of the new document (T-02-09-01 mitigation)
- No changes to App.xaml.cs — `IGeometryService` singleton was already registered; DI resolves the new constructor parameter automatically

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- GAP-10 resolved: gaze students who open a second exam paper will not see geometry from a previous session
- Exam integrity maintained: no ghost objects can overlay printed exam text
- All 55 existing tests pass with no regressions

---
*Phase: 02-geometry-core*
*Completed: 2026-05-04*
