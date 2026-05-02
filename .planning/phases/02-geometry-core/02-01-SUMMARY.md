---
phase: 02-geometry-core
plan: 01
subsystem: geometry
tags: [skiasharp, geometry, hit-testing, math, xunit]

# Dependency graph
requires:
  - phase: 01-foundation
    provides: CoordinateMapper (PageToScreen/ScreenToPage API), SkiaSharp 3.119.2, xunit.v3 test project

provides:
  - GeometryObject abstract base class (Id, IsSelected, Draw, HitTest, GetSnapPoints)
  - PointObject, LineObject, CircleObject concrete types in namespace MathGaze.Core.Geometry
  - GeometryMath static helpers (DistancePointToSegment, TryLineIntersect)
  - GeometryHitTester static helpers (TryHitObject, TryHitLineSubPoint, TryHitCircleSubPoint)
  - 14 new unit tests (6 GeometryMath + 8 GeometryHitTester), all passing

affects:
  - 02-02 (tool state machine uses geometry types and hit tester)
  - 02-03 (snap engine uses GeometryMath.TryLineIntersect and GetSnapPoints)
  - 02-04 (geometry layer rendering uses Draw stubs replaced with real implementations)
  - 02-05 (right rail reads SelectedEndpoint/SelectedSubPoint from LineObject/CircleObject)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Geometry objects store positions in PDF point coordinates (D-10) — converted to screen pixels by CoordinateMapper at render/hit-test time"
    - "HitTest stubs call GeometryMath helpers; Draw stubs throw NotImplementedException pending Plan 04"
    - "GeometryHitTester iterates in reverse Z-order (last-placed = topmost) for click disambiguation"
    - "SubPointTapRadius = 28f (56px diameter) satisfies >=56x56px gaze requirement per D-04/D-05"

key-files:
  created:
    - MathGaze/Core/Geometry/GeometryObject.cs
    - MathGaze/Core/Geometry/PointObject.cs
    - MathGaze/Core/Geometry/LineObject.cs
    - MathGaze/Core/Geometry/CircleObject.cs
    - MathGaze/Core/GeometryMath.cs
    - MathGaze/Core/GeometryHitTester.cs
    - MathGaze.Tests/GeometryMathTests.cs
    - MathGaze.Tests/GeometryHitTesterTests.cs
  modified: []

key-decisions:
  - "GeometryMath.cs created in Task 1 (not Task 2) because LineObject.HitTest directly calls DistancePointToSegment — both had to compile together"
  - "PointObject.HitTest implemented directly (not as a stub) since it is a trivial distance check — no architectural concern"
  - "LineObject.HitTest and CircleObject.HitTest fully implemented (not stubbed) since they are required for GeometryHitTester tests to be meaningful"

patterns-established:
  - "Pattern: All geometry positions stored in PDF points (double precision); converted to SKPoint (float) by CoordinateMapper only at hit-test / render time"
  - "Pattern: Sub-point tap radius = 28f screen pixels = 56px diameter = gaze accuracy floor requirement"
  - "Pattern: HitTest tolerance is type-specific (PointHitRadius=18f, LineHitTolerance=10f, CircleRingTolerance=10f)"

requirements-completed:
  - GEOM-01
  - GEOM-02
  - GEOM-03
  - GEOM-04

# Metrics
duration: 3min
completed: 2026-05-02
---

# Phase 2 Plan 01: Geometry Object Model Summary

**PDF-coordinate geometry model (PointObject, LineObject, CircleObject) with gaze-accurate hit testing (18/10px tolerances, 28px sub-point radius) and full unit test coverage via GeometryMath and GeometryHitTester**

## Performance

- **Duration:** 3 min
- **Started:** 2026-05-02T19:57:00Z
- **Completed:** 2026-05-02T20:00:55Z
- **Tasks:** 2 of 2
- **Files modified:** 8 created, 0 modified

## Accomplishments

- Created the full geometry object model: abstract `GeometryObject` base and three concrete types (`PointObject`, `LineObject`, `CircleObject`) with correct PDF-point storage and `GetSnapPoints` for the snap engine
- Implemented `GeometryMath` (segment distance + line-line intersection) and `GeometryHitTester` (Z-order object hit, line endpoint sub-point hit, circle sub-point hit) as pure static helpers with no WPF dependencies
- Added 14 new unit tests (6 GeometryMath + 8 GeometryHitTester); all 48 tests in the suite pass including the 32 pre-existing CoordinateMapper tests

## Task Commits

Each task was committed atomically:

1. **Task 1: Geometry object model — abstract base + three concrete types** - `6661d24` (feat)
2. **Task 2: GeometryMath + GeometryHitTester with unit tests** - `e67d9a0` (feat)

**Plan metadata:** (committed with final docs commit)

## Files Created/Modified

- `MathGaze/Core/Geometry/GeometryObject.cs` - Abstract base: Guid Id, IsSelected, Draw/HitTest/GetSnapPoints abstract members
- `MathGaze/Core/Geometry/PointObject.cs` - Point with XPt/YPt; hit test via center distance; snap yields "point" label
- `MathGaze/Core/Geometry/LineObject.cs` - Line with X1Pt/Y1Pt/X2Pt/Y2Pt, SelectedEndpoint; hit via segment distance; snaps yield "endpoint A"/"endpoint B"
- `MathGaze/Core/Geometry/CircleObject.cs` - Circle with CenterXPt/CenterYPt/RadiusPt, SelectedSubPoint; ring hit + center-dot hit; snap yields "centre"
- `MathGaze/Core/GeometryMath.cs` - DistancePointToSegment (clamped projection), TryLineIntersect (Cramer's rule)
- `MathGaze/Core/GeometryHitTester.cs` - TryHitObject (reverse Z), TryHitLineSubPoint, TryHitCircleSubPoint; SubPointTapRadius=28f
- `MathGaze.Tests/GeometryMathTests.cs` - 6 facts covering midpoint=0, perpendicular=5, beyond-endpoint clamping, degenerate segment, crossing lines, parallel lines
- `MathGaze.Tests/GeometryHitTesterTests.cs` - 8 facts covering all three object types plus line/circle sub-point selection

## Decisions Made

- `GeometryMath.cs` was created in Task 1 (not Task 2 as planned) because `LineObject.HitTest` directly calls `DistancePointToSegment` — both needed to compile together for the Task 1 build verification to pass. This is a sequencing deviation with no architectural impact.
- `PointObject.HitTest`, `LineObject.HitTest`, and `CircleObject.HitTest` were implemented fully rather than stubbed, because the GeometryHitTester tests require working hit-test implementations to be meaningful. The plan's note about stubs applied only to `Draw`, not `HitTest`.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Created GeometryMath.cs in Task 1 instead of Task 2**
- **Found during:** Task 1 (build verification)
- **Issue:** `LineObject.HitTest` calls `GeometryMath.DistancePointToSegment`, which did not exist yet — Task 1 build would fail without it
- **Fix:** Created `GeometryMath.cs` alongside the geometry types in Task 1; committed together as part of the Task 1 commit
- **Files modified:** `MathGaze/Core/GeometryMath.cs`
- **Verification:** `dotnet build MathGaze/MathGaze.csproj` exits 0 with 0 errors after Task 1
- **Committed in:** `6661d24` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (Rule 3 - blocking)
**Impact on plan:** Zero scope creep; GeometryMath is the same code the plan specified, just committed one task earlier than listed.

## Issues Encountered

None — build and tests passed first try.

## Known Stubs

- `GeometryObject.Draw()` is abstract and all three concrete types throw `NotImplementedException` with the message "Draw implemented in GeometryLayerViewModel (Plan 04)". This is intentional per the plan; Plan 04 will replace these stubs with real SkiaSharp draw calls.

## Threat Flags

None — this plan introduces no network endpoints, no file I/O, no auth paths, and no schema changes at trust boundaries. All code is pure in-process arithmetic on SKPoints.

## Next Phase Readiness

- All geometry types ready for Plan 02 (tool state machine) to instantiate via `new PointObject/LineObject/CircleObject`
- `GeometryHitTester.TryHitObject` ready for Plan 02 Select mode click handling
- `GeometryHitTester.TryHitLineSubPoint` / `TryHitCircleSubPoint` ready for Plan 02/05 sub-point selection flow
- `GeometryMath.TryLineIntersect` ready for Plan 03 snap engine
- `GetSnapPoints` on all types ready for Plan 03 snap engine endpoint enumeration
- `Draw` stubs need replacing in Plan 04 before geometry is visible on canvas

## Self-Check: PASSED

All 8 files found on disk. Both commits (6661d24, e67d9a0) present in git log. All 48 tests green.

---
*Phase: 02-geometry-core*
*Completed: 2026-05-02*
