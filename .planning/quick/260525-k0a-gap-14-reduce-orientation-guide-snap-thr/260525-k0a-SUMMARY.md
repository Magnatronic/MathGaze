---
phase: quick
plan: 260525-k0a
subsystem: snap-engine
tags: [snap, geometry, gaze-accuracy, gap-14]
dependency_graph:
  requires: []
  provides: [OrientThresholdPx constant, GAP-14 fix]
  affects: [SnapEngine section-3, orientation guide snap behaviour]
tech_stack:
  added: []
  patterns: [dual-threshold snap (20px endpoints / 10px orientation guides)]
key_files:
  modified:
    - MathGaze/Core/SnapEngine.cs
    - MathGaze.Tests/SnapEngineTests.cs
decisions:
  - OrientThresholdPx = 10f introduced as a separate constant from SnapThresholdPx = 20f so sections 1+2 thresholds are independently tunable
metrics:
  duration_min: 5
  completed_date: "2026-05-25"
  tasks_completed: 1
  files_modified: 2
---

# Phase quick Plan 260525-k0a: GAP-14 Reduce Orientation Guide Snap Threshold Summary

**One-liner:** Halved orientation guide snap threshold from 20px to 10px via dedicated `OrientThresholdPx` constant, eliminating silent position drift when 3+ geometry objects create overlapping H/V/45° snap bands.

## What Was Done

Introduced `OrientThresholdPx = 10f` as a separate compile-time constant in `SnapEngine.cs` and replaced all 4 section-3 threshold comparisons (`orientBestDist` init, `dH`, `dV`, `deviation`) with it. Sections 1 and 2 (endpoint and intersection snap) continue to use `SnapThresholdPx = 20f` unchanged.

Added regression test `Snap_OrientationSnap_OutsideNewThreshold_ReturnsNull` that places the cursor at `snapPt + (25, 12)` — outside the new 10px orient threshold in every direction but inside the old 20px threshold — and asserts `label is null`.

## Tasks

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Introduce OrientThresholdPx and add regression test | 086e1b1 | MathGaze/Core/SnapEngine.cs, MathGaze.Tests/SnapEngineTests.cs |

## Verification

- `dotnet test`: 63/63 passed (62 pre-existing + 1 new)
- `OrientThresholdPx` appears in exactly 5 lines of SnapEngine.cs (1 declaration + 4 section-3 uses)
- `SnapThresholdPx` appears in exactly 3 lines of SnapEngine.cs (1 declaration + 2 uses in sections 1+2)
- Build: zero errors, only pre-existing NU1701 warnings (expected, documented in STATE.md)

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None.

## Threat Flags

None — no new network endpoints, auth paths, or schema changes introduced.

## Self-Check: PASSED

- [x] MathGaze/Core/SnapEngine.cs modified and committed (086e1b1)
- [x] MathGaze.Tests/SnapEngineTests.cs modified and committed (086e1b1)
- [x] All 63 tests pass
- [x] Commit 086e1b1 exists in git log
