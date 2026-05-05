---
phase: 02-geometry-core
plan: 11
subsystem: coordinate-mapping, snap-engine
tags: [bug-fix, coordinate-mapper, snap-engine, tdd, unit-tests]
dependency_graph:
  requires: [02-07]
  provides: [GAP-11-fix, snap-engine-tests]
  affects: [PdfCanvasViewModel, SnapEngine]
tech_stack:
  added: []
  patterns: [defensive-sync-update, priority-guard-orientation-snaps]
key_files:
  created:
    - MathGaze.Tests/SnapEngineTests.cs
  modified:
    - MathGaze/ViewModels/PdfCanvasViewModel.cs
    - MathGaze/Core/SnapEngine.cs
decisions:
  - "EnsureCoordinateMapper() called synchronously in SetCanvasSize (after OnCanvasSizeChanged) and SetDpiScale to eliminate all ordering races before first user click"
  - "Orientation guide snaps (section 3) only run when no higher-priority snap found in sections 1+2 — prevents endpoint snaps being overridden"
  - "45 degree guide adds d>0 guard to skip cursors already on the diagonal line (d=0 means no actual snap movement)"
  - "Horizontal/vertical snap tests use 25px offset (not 15px) to stay outside 20px endpoint threshold so orientation guides can fire"
metrics:
  duration_seconds: 289
  completed_date: "2026-05-05"
  tasks_completed: 2
  files_modified: 3
requirements: [GEOM-01, GEOM-02, GEOM-03]
gaps_addressed: [GAP-11]
---

# Phase 02 Plan 11: CoordinateMapper Race Fix and SnapEngine Tests Summary

Force synchronous CoordinateMapper update in SetCanvasSize/SetDpiScale to eliminate GAP-11 first-click placement race; add 5 SnapEngine unit tests and fix orientation guide priority bug so endpoint snaps cannot be overridden by lower-priority guides.

## What Was Changed

### Task 1: Force synchronous CoordinateMapper update (PdfCanvasViewModel.cs)

**SetDpiScale** — added `EnsureCoordinateMapper()` call after updating `_dpiScale`:
```csharp
public void SetDpiScale(double pixelsPerDip)
{
    _dpiScale = pixelsPerDip;
    EnsureCoordinateMapper(); // synchronously update mapper before any click can fire
}
```

**SetCanvasSize** — added `EnsureCoordinateMapper()` call in both branches, in the PDF-open branch after `OnCanvasSizeChanged()` (so it reads the updated ZoomFactor from ApplyFitPage):
```csharp
_mainVm.OnCanvasSizeChanged();
EnsureCoordinateMapper(); // synchronously bring mapper up to date
_ = LoadCurrentPageAsync();
```
And in the no-PDF branch:
```csharp
EnsureCoordinateMapper(); // guard returns early if !IsOpen — harmless
InvalidationRequested?.Invoke(this, EventArgs.Empty);
```

**Call order enforced:** `OnCanvasSizeChanged` → `EnsureCoordinateMapper` → `LoadCurrentPageAsync`. This ensures the mapper always reflects the current ZoomFactor (which ApplyFitPage may have just changed) before any click can fire.

**Total `EnsureCoordinateMapper()` call sites:** 6 (2 new in SetDpiScale + SetCanvasSize PDF path + SetCanvasSize else path; 3 existing in HandleCanvasClick, HandleMouseMove, Paint).

### Task 2: SnapEngine unit tests + orientation guide priority fix

**MathGaze.Tests/SnapEngineTests.cs** — 5 new `[Fact]` tests:
- `Snap_HorizontalAlignment_ReturnsHorizontalLabel` — cursor 25px right, same Y → "horizontal"
- `Snap_HorizontalAlignment_LeftOfPoint_ReturnsHorizontalLabel` — cursor 25px left, same Y → "horizontal"
- `Snap_VerticalAlignment_ReturnsVerticalLabel` — cursor 25px below, same X → "vertical"
- `Snap_NoNearbyPoints_ReturnsNullLabel` — cursor 25px diagonally → null (no snap)
- `Snap_CursorOnPoint_ReturnsPointLabel` — cursor 5px from endpoint → "point" (priority 1)

Note: tests use 25px offset (not 15px as suggested in plan) to stay outside the 20px endpoint snap threshold. This is required because endpoint snap (priority 1) at 15px would override the orientation guide, causing false "point" label instead of "horizontal"/"vertical".

**MathGaze/Core/SnapEngine.cs** — two bugs fixed (Rule 1 auto-fixes):

**Bug 1:** Orientation guide section (section 3) did not enforce priority over sections 1+2. When cursor was `(snapPt.X+5, snapPt.Y+5)`, endpoint snap correctly set `bestDist=7.07, label="point"`, but then orientation guide 45° computed `d=0` (cursor exactly on 45° diagonal) which beat bestDist=7.07, overwriting "point" with "45°".

**Fix:** Wrap section 3 in `if (label is null)` — orientation guides only run when no higher-priority snap was found. Use a separate `orientBestDist` starting at `SnapThresholdPx` for section 3 competition.

**Bug 2:** The 45° guide fired when cursor was exactly on the 45° diagonal from a snap point (d=0), even when the cursor was 35px away (beyond the 20px threshold). `deviation=0 < 20` triggered the check; candidate = cursor; `d=0 < orientBestDist` → returned "45°" for a cursor nowhere near snapping.

**Fix:** Added `d > 0f` guard to the 45° check: if the cursor IS the candidate (d=0), skip — no actual snap movement would occur and labelling "45°" is misleading.

Horizontal and vertical guides do NOT have this d>0 guard because `d=0` means the cursor is perfectly aligned (e.g., exact same Y), which IS a meaningful label even though no position change occurs.

## Root Cause Assessment

The plan's root cause analysis (REVISED ROOT CAUSE section) identified a DPI race scenario where `VisualTreeHelper.GetDpi(this)` might return 1.0 before the control is in the visual tree. The defensive fix (call `EnsureCoordinateMapper()` in `SetCanvasSize` and `SetDpiScale`) eliminates all possible ordering races without requiring an exact reproduction path.

The confirmed UAT symptom ("first click wrong, second click correct at same position") is consistent with a stale mapper at click time. The fix ensures the mapper is always synchronously current after any state change.

## Test Results

| Metric | Value |
|--------|-------|
| Pre-fix test count | 55 |
| New tests added | 5 |
| Post-fix test count | 60 |
| Failed | 0 |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] SnapEngine orientation guides could override endpoint snaps**
- **Found during:** Task 2 (TDD RED → tests failed with "45°" where "point" expected)
- **Issue:** Section 3 (orientation guides) ran unconditionally and used the already-reduced `bestDist` as their competition threshold. When cursor was on the 45° diagonal from a snap point, `d=0` beat any prior endpoint snap.
- **Fix:** Wrap section 3 in `if (label is null)` so orientation guides only compete when no higher-priority snap was found. Use a separate `orientBestDist` for section 3.
- **Files modified:** `MathGaze/Core/SnapEngine.cs`
- **Commit:** 54bcbfc

**2. [Rule 1 - Bug] 45° orientation guide fired spuriously when cursor was far away on the exact diagonal**
- **Found during:** Task 2 (`Snap_NoNearbyPoints_ReturnsNullLabel` returned "45°" instead of null)
- **Issue:** `deviation = |dx|-|dy| = 0` when dx=dy=25 (cursor on exact 45° from point). Candidate = cursor, `d=0 < threshold` → 45° snap fired even though cursor was 35px from snap point.
- **Fix:** Added `d > 0f` guard to 45° inner check — skip when cursor IS the candidate.
- **Files modified:** `MathGaze/Core/SnapEngine.cs`
- **Commit:** 54bcbfc

**3. [Plan adjustment] Test offset changed from 15px to 25px for horizontal/vertical tests**
- **Found during:** Task 2 design review
- **Issue:** The plan suggested 15px horizontal offset, but 15 < 20px endpoint threshold — endpoint snap (priority 1) would return "point" not "horizontal". The test at 15px would test the wrong behaviour.
- **Fix:** Used 25px offset (outside the 20px threshold) so orientation guide fires correctly. Added comment explaining this design constraint.
- **Files modified:** `MathGaze.Tests/SnapEngineTests.cs`

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| Task 1 | e51a4f5 | fix(02-11): force synchronous CoordinateMapper update in SetCanvasSize and SetDpiScale |
| Task 2 | 54bcbfc | test(02-11): add SnapEngine unit tests; fix orientation guide priority bug |

## Known Stubs

None — no stub patterns introduced in this plan.

## Threat Flags

None — no new network endpoints, auth paths, file access patterns, or schema changes introduced.

## Self-Check: PASSED

- `MathGaze/ViewModels/PdfCanvasViewModel.cs` — exists, contains `EnsureCoordinateMapper();` at lines 101, 110, 121, 144, 149, 198
- `MathGaze.Tests/SnapEngineTests.cs` — exists with 5 `[Fact]` tests
- `MathGaze/Core/SnapEngine.cs` — exists, orientation priority bug fixed
- Commit e51a4f5 — verified in git log
- Commit 54bcbfc — verified in git log
- Build: 0 errors
- Tests: Failed: 0, Passed: 60
