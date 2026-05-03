---
phase: 02-geometry-core
plan: 04
subsystem: ui
tags: [skia, wpf, geometry, rendering, canvas, viewmodel]

# Dependency graph
requires:
  - phase: 02-geometry-core/02-01
    provides: GeometryObject model (PointObject, LineObject, CircleObject) with IsSelected, SelectedEndpoint, SelectedSubPoint
  - phase: 02-geometry-core/02-02
    provides: IGeometryService with Objects list, ObjectsChanged event
  - phase: 02-geometry-core/02-03
    provides: PdfCanvasViewModel.Paint() with DrawBitmap + DrawGhostPreview + CoordinateMapper
provides:
  - GeometryLayerViewModel with Draw(SKCanvas, CoordinateMapper?) rendering all committed geometry objects
  - Visual rendering for Point (outer ring + centre dot), Line (stroke), Circle (ring + centre dot)
  - Selection highlighting in accent cobalt (#3B6FD4) for selected objects
  - Sub-point tap target indicators (8px dot + 14px active ring) for selected Line/Circle
  - Geometry vector layer inserted between PDF bitmap and ghost preview in Paint()
  - Proper Dispose() pattern with named event handler unsubscription
affects: [02-05, 03-protractor, ui-rendering]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - SKPaint cached as readonly fields on ViewModel (never allocated per PaintSurface call)
    - Two-pass draw: unselected objects first, selected objects on top
    - Named event handler methods for proper unsubscription in Dispose()

key-files:
  created:
    - MathGaze/ViewModels/GeometryLayerViewModel.cs
  modified:
    - MathGaze/ViewModels/PdfCanvasViewModel.cs
    - MathGaze/App.xaml.cs

key-decisions:
  - "GeometryLayerViewModel uses non-nullable _geometryLayer field in PdfCanvasViewModel (injected via DI, always present)"
  - "Lambda event subscriptions converted to named methods (OnGhostChanged, OnObjectsChanged) so Dispose() can unsubscribe"
  - "Sub-point dot uses 8px radius (visual) with active ring at 14px — hit zone (28px) handled by GeometryHitTester, not drawn"

patterns-established:
  - "SKPaint cache pattern: all paints declared as readonly fields with object initializer syntax, never new() per frame"
  - "Two-pass render: unselected → selected (selected always drawn on top of unselected)"
  - "Layer insertion order: DrawBitmap → geometry layer → ghost preview → Flush()"

requirements-completed:
  - GEOM-04

# Metrics
duration: 8min
completed: 2026-05-03
---

# Phase 02 Plan 04: GeometryLayerViewModel — Geometry Rendering Summary

**SkiaSharp geometry rendering layer with 7 cached SKPaint fields drawing Point/Line/Circle in normal and selected states with sub-point tap target indicators**

## Performance

- **Duration:** 8 min
- **Started:** 2026-05-03T07:00:00Z
- **Completed:** 2026-05-03T07:08:00Z
- **Tasks:** 1
- **Files modified:** 3 (1 created, 2 modified)

## Accomplishments

- Created GeometryLayerViewModel with all 7 SKPaint objects cached as readonly fields (zero per-frame GC allocation)
- All three geometry types render correctly: Point (outer ring + centre dot), Line (stroke between endpoints), Circle (circumference ring + centre dot)
- Selected objects draw in accent cobalt (#3B6FD4); unselected in normal ink (#1A1A2E at 220 alpha)
- Sub-point tap targets rendered for selected Line (both endpoints) and Circle (centre + edge point): 8px filled dot, active sub-point adds 14px ring indicator
- Geometry layer inserted at correct position in PdfCanvasViewModel.Paint(): after DrawBitmap, before DrawGhostPreview
- Lambda event subscriptions (OnGhostChanged, OnObjectsChanged) converted to named methods and unsubscribed in Dispose() to prevent memory leaks

## Task Commits

1. **Task 1: GeometryLayerViewModel — full rendering for all object types and selection states** - `149cab0` (feat)

## Files Created/Modified

- `MathGaze/ViewModels/GeometryLayerViewModel.cs` — New class: Draw() renders all geometry objects with cached paints, two-pass selection rendering, and sub-point tap target indicators
- `MathGaze/ViewModels/PdfCanvasViewModel.cs` — Added GeometryLayerViewModel field + constructor injection; inserted `_geometryLayer.Draw()` between DrawBitmap and DrawGhostPreview; converted lambda subscriptions to named methods; added unsubscription in Dispose()
- `MathGaze/App.xaml.cs` — Registered `GeometryLayerViewModel` as singleton before PdfCanvasViewModel

## Decisions Made

- `_geometryLayer` is a non-nullable readonly field in PdfCanvasViewModel (injected via DI) — no `?.` null-check needed in Paint(), which is correct since it's always present
- Lambda event handlers `(_, _) => ...` from Plan 03 constructor converted to named private methods so they can be properly unsubscribed (Rule 2: missing critical functionality — memory leak prevention)
- Sub-point visual dot radius is 8px; active ring is 14px — the 28px invisible hit zone is owned by GeometryHitTester, not drawn here

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Converted lambda event subscriptions to named methods for proper Dispose() unsubscription**
- **Found during:** Task 1 (reading PdfCanvasViewModel.cs before implementing)
- **Issue:** Plan 03 had wired `GhostChanged` and `ObjectsChanged` as anonymous lambdas `(_, _) => ...` — these cannot be unsubscribed in Dispose(), creating a memory leak
- **Fix:** Replaced lambdas with named private methods `OnGhostChanged` and `OnObjectsChanged`; unsubscribed both in Dispose() before `_geometryLayer.Dispose()`
- **Files modified:** MathGaze/ViewModels/PdfCanvasViewModel.cs
- **Verification:** Build succeeds; unsubscription lines confirmed by grep in Dispose()
- **Committed in:** 149cab0 (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 missing critical — memory leak prevention)
**Impact on plan:** Auto-fix was essential for correct Dispose() behaviour as specified in the plan's acceptance criteria. No scope creep.

## Issues Encountered

None — build succeeded first attempt (0 errors, expected NU1701 warnings only).

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Geometry objects are now visible on-screen with correct colours and selection highlighting
- Plan 02-05 (nudge/delete right rail) can proceed: the visual proof that placed objects appear is now in place
- Sub-point tap targets are rendered correctly; GeometryHitTester sub-point detection (from Plan 02-02) pairs with these visuals

## Known Stubs

None — all rendering logic is fully implemented.

---

## Self-Check

- [x] `MathGaze/ViewModels/GeometryLayerViewModel.cs` exists with `private readonly SKPaint _normalPaint = new()`
- [x] `MathGaze/ViewModels/PdfCanvasViewModel.cs` contains `_geometryLayer.Draw(canvas, _coordinateMapper)`
- [x] `MathGaze/App.xaml.cs` contains `services.AddSingleton<GeometryLayerViewModel>()`
- [x] Commit `149cab0` exists
- [x] `dotnet build` exits 0

## Self-Check: PASSED

---
*Phase: 02-geometry-core*
*Completed: 2026-05-03*
