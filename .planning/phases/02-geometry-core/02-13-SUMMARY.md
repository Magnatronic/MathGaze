---
phase: 02-geometry-core
plan: 13
subsystem: geometry-state
tags: [gap-fix, page-navigation, geometry-reset, undo]
dependency_graph:
  requires: [02-10]
  provides: [GAP-13-closed]
  affects: [MainViewModel, GeometryService]
tech_stack:
  added: []
  patterns: [idempotent-reset-on-page-change]
key_files:
  created: []
  modified:
    - MathGaze/ViewModels/MainViewModel.cs
decisions:
  - "Reset() called in OnCurrentPageChanged as first statement — geometry cleared before UI property notifications fire"
  - "Existing Reset() in OpenFileAsync (GAP-10 fix) preserved — double call on PDF open is harmless (Reset is idempotent)"
  - "Pre-existing test failures (61 failing, 1 passing) confirmed to be machine-level AppControl policy blocking the compiled DLL — unrelated to this change; stash-verified same result on prior commit"
metrics:
  duration_minutes: 8
  completed_date: "2026-05-05"
  tasks_completed: 2
  files_modified: 1
---

# Phase 02 Plan 13: GAP-13 Page Navigation Geometry Reset Summary

One-liner: Added `_geometryService.Reset()` to `OnCurrentPageChanged` so each PDF page has an independent geometry canvas and undo stack.

## What Was Done

GAP-13 fix: geometry objects drawn on page N were persisting when navigating to page N+1. Root cause was that `OnCurrentPageChanged` — the CommunityToolkit.Mvvm partial method called on every `CurrentPage` property set — did not call `_geometryService.Reset()`. The GAP-10 fix only added Reset() inside `OpenFileAsync` (new PDF open path), leaving page navigation uncovered.

**Fix:** Added `_geometryService.Reset()` as the first statement in `OnCurrentPageChanged` at line 138 of `MathGaze/ViewModels/MainViewModel.cs`.

## Key Files

- `MathGaze/ViewModels/MainViewModel.cs` — `OnCurrentPageChanged` now calls `_geometryService.Reset()` before property-changed notifications

## Reset() Implementation Confirmed

`GeometryService.Reset()` (line 127-131 of `MathGaze/Services/GeometryService.cs`) clears:
- `_objects.Clear()` — removes all geometry objects from the canvas
- `_undoService.Clear()` — clears both undo and redo stacks

This means after page navigation:
- Canvas is empty (no bleed-through from previous page)
- Undo button is disabled (stack is empty)
- Redo button is disabled (stack is empty)

Correct behavior per the plan's design intent.

## Acceptance Criteria

1. Build: `dotnet build MathGaze/MathGaze.csproj -c Debug` — **Build succeeded. 0 errors.**
2. `_geometryService.Reset()` in `OnCurrentPageChanged` — **Confirmed at line 138.**
3. `_geometryService.Reset()` in `OpenFileAsync` (GAP-10 fix preserved) — **Confirmed at line 260.**
4. Two total Reset() calls in MainViewModel.cs — **Confirmed: grep shows lines 138 and 260.**

## Test Suite Results

- Failed: 61, Passed: 1, Total: 62
- **Pre-existing failures confirmed:** All 61 failures are `System.IO.FileLoadException: An Application Control policy has blocked this file. (0x800711C7)` — Windows AppLocker/WDAC blocking the freshly-compiled DLL. Stash-verified: running tests on the commit prior to this change produces identical results (Failed: 61, Passed: 1). This is a machine environment constraint, not a test logic regression.

## Commits

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Add Reset() to OnCurrentPageChanged | ea7a1ea | MathGaze/ViewModels/MainViewModel.cs |
| 2 | Verify test suite and acceptance criteria | (no new files — read-only verification) | — |

## Deviations from Plan

None — plan executed exactly as written. One line added in `OnCurrentPageChanged` as specified. GAP-10 Reset() in `OpenFileAsync` preserved as specified. The test failure situation (pre-existing AppControl block) was documented per plan instructions (flag in SUMMARY rather than treating as regression).

## Known Stubs

None.

## Threat Flags

None — no new network endpoints, auth paths, file access patterns, or schema changes introduced.

## Self-Check: PASSED

- `MathGaze/ViewModels/MainViewModel.cs` modified — file exists and contains `_geometryService.Reset()` at line 138.
- Commit `ea7a1ea` exists in git log.
- OpenFileAsync Reset() preserved at line 260.
