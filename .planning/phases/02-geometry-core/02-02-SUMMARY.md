---
phase: 02-geometry-core
plan: "02"
subsystem: geometry-commands
tags: [command-pattern, undo-redo, services, di, unit-tests]
dependency_graph:
  requires: [02-01]
  provides: [IGeometryCommand, GeometryService, UndoService]
  affects: [02-03, 02-04, 02-05]
tech_stack:
  added: []
  patterns: [command-pattern, double-stack-undo, singleton-service]
key_files:
  created:
    - MathGaze/Core/Commands/IGeometryCommand.cs
    - MathGaze/Core/Commands/PlaceObjectCommand.cs
    - MathGaze/Core/Commands/DeleteObjectCommand.cs
    - MathGaze/Core/Commands/NudgeObjectCommand.cs
    - MathGaze/Core/Commands/NudgeEndpointCommand.cs
    - MathGaze/Services/IGeometryService.cs
    - MathGaze/Services/GeometryService.cs
    - MathGaze/Services/UndoService.cs
    - MathGaze.Tests/UndoServiceTests.cs
  modified:
    - MathGaze/App.xaml.cs
decisions:
  - "NudgeSubPoint silently no-ops on out-of-range subPointIndex (T-02-06 mitigation — no exception, no crash)"
  - "GeometryService.AddObject does not raise ObjectsChanged; only ExecuteCommand raises it after the full command completes"
  - "UndoService registered as singleton in DI even though GeometryService owns its own private instance — kept for future direct injection if needed"
metrics:
  duration_seconds: 151
  completed_date: "2026-05-02"
  tasks_completed: 2
  files_created: 9
  files_modified: 1
---

# Phase 02 Plan 02: Command Pattern and Services Layer Summary

**One-liner:** Double-stack undo/redo via IGeometryCommand + GeometryService singleton; all geometry mutations now flow through ExecuteCommand() and are automatically undoable.

## What Was Built

The immutable data-flow spine for all geometry editing. Every geometry mutation in MathGaze now flows through a single entry point — `GeometryService.ExecuteCommand()` — which executes the command, pushes it to the undo stack, and fires `ObjectsChanged`. This means every subsequent plan that places, deletes, or nudges objects gets undo/redo for free.

### IGeometryCommand Interface

`MathGaze/Core/Commands/IGeometryCommand.cs` — two-method contract: `Execute(IGeometryService)` and `Undo(IGeometryService)`. Commands are self-contained and symmetric.

### Four Concrete Commands

- **PlaceObjectCommand** — `AddObject` on execute, `RemoveObject` on undo
- **DeleteObjectCommand** — `RemoveObject` on execute, `AddObject` on undo (captures object reference so undo can restore)
- **NudgeObjectCommand** — stores `(objectId, dxPt, dyPt)` in PDF points; execute adds delta, undo subtracts. Zoom-independent per D-10.
- **NudgeEndpointCommand** — per-sub-point nudge; `subPointIndex` 0/1 routes to the correct endpoint of `LineObject` or to center/radius of `CircleObject` per D-04/D-05.

### IGeometryService Contract

`MathGaze/Services/IGeometryService.cs` — full service contract: `Objects` (read-only list), `SelectedObject`, `ObjectsChanged` event, direct mutation methods (used only by commands), selection methods, `ExecuteCommand`, `Undo`/`Redo`, `CanUndo`/`CanRedo`, `Reset`.

### UndoService

`MathGaze/Services/UndoService.cs` — two `Stack<IGeometryCommand>` (`_undoStack`, `_redoStack`). Execute: runs command, pushes to undo stack, clears redo stack. Undo: pops undo, runs Undo(), pushes to redo. Redo: pops redo, runs Execute(), pushes to undo.

### GeometryService

`MathGaze/Services/GeometryService.cs` — singleton owning `List<GeometryObject>` and a private `UndoService`. All coordinate manipulation dispatched via switch on concrete type. `NudgeSubPoint` uses explicit `if (index == 0) / else if (index == 1)` guard — out-of-range index is a silent no-op (T-02-06 mitigation). `ObjectsChanged` fires on `ExecuteCommand`, `Undo`, `Redo`, `SetSelected`, `ClearSelection`, and `Reset`.

### DI Registration

`MathGaze/App.xaml.cs` — added `AddSingleton<IGeometryService, GeometryService>()` and `AddSingleton<UndoService>()` to the existing service registration block.

### Unit Tests

`MathGaze.Tests/UndoServiceTests.cs` — 7 `[Fact]` tests:
1. Execute pushes to undo stack, clears redo stack
2. Undo reverses change, moves to redo stack
3. Redo re-applies change, moves back to undo stack
4. New action after undo clears redo stack
5. Three nudges then undo×3 restores original position exactly (floating-point precision: 6 decimal places)
6. DeleteCommand execute removes object; undo restores it
7. NudgeEndpointCommand index 0 nudges only endpoint A, endpoint B unchanged

All 55 tests pass (32 CoordinateMapper + 7 UndoService + 16 GeometryHitTester).

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| Task 1 | 19c2e11 | feat(02-02): IGeometryCommand interface + four concrete command implementations |
| Task 2 | 2f30982 | feat(02-02): UndoService + GeometryService + DI registration + 7 unit tests |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical Functionality] IGeometryService.cs created in Task 1**
- **Found during:** Task 1
- **Issue:** `IGeometryCommand.cs` references `IGeometryService` as a parameter type. The plan staged `IGeometryService.cs` under Task 2, but the build would fail at Task 1 verification without it.
- **Fix:** Created `IGeometryService.cs` alongside the Task 1 command files so the compiler could resolve the type reference. The interface content is identical to what was specified in Task 2.
- **Files modified:** `MathGaze/Services/IGeometryService.cs` (created early)
- **Commit:** 19c2e11

**2. [Rule 2 - Security] NudgeSubPoint out-of-range index made explicit**
- **Found during:** Task 2 — threat model review (T-02-06)
- **Issue:** The plan's `GeometryService.NudgeSubPoint` used bare `else` for the second branch, meaning any index >= 2 would fall into the `else` and be treated as index 1 (wrong behaviour).
- **Fix:** Changed to `if (index == 0) / else if (index == 1) / // else: no-op` pattern. Out-of-range indices now silently no-op as specified in T-02-06 mitigation.
- **Files modified:** `MathGaze/Services/GeometryService.cs`
- **Commit:** 2f30982

## Known Stubs

None — this is a pure service/command layer with no UI rendering. No stubs detected.

## Threat Flags

No new threat surface introduced. All files are in-process, no file I/O, no network, no user-supplied data execution. Threat register (T-02-04, T-02-05, T-02-06) addressed within implementation:
- T-02-04: Objects exposed only as `IReadOnlyList<GeometryObject>` — callers cannot mutate without going through `ExecuteCommand`
- T-02-05: Undo stack unbounded growth accepted (GCSE-scale usage)
- T-02-06: Out-of-range subPointIndex silently no-ops (explicit if/else if guards in `NudgeSubPoint`)

## Self-Check: PASSED

Verified:
- `MathGaze/Core/Commands/IGeometryCommand.cs` — EXISTS
- `MathGaze/Core/Commands/PlaceObjectCommand.cs` — EXISTS
- `MathGaze/Core/Commands/DeleteObjectCommand.cs` — EXISTS
- `MathGaze/Core/Commands/NudgeObjectCommand.cs` — EXISTS
- `MathGaze/Core/Commands/NudgeEndpointCommand.cs` — EXISTS
- `MathGaze/Services/IGeometryService.cs` — EXISTS
- `MathGaze/Services/GeometryService.cs` — EXISTS
- `MathGaze/Services/UndoService.cs` — EXISTS
- `MathGaze.Tests/UndoServiceTests.cs` — EXISTS
- Commits 19c2e11 and 2f30982 — CONFIRMED in git log
- All 55 tests pass — CONFIRMED (`dotnet test` exit 0)
- 0 build errors — CONFIRMED
