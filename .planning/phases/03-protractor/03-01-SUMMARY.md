---
phase: 03-protractor
plan: "01"
subsystem: geometry-model
tags:
  - protractor
  - data-model
  - commands
  - geometry-math
dependency_graph:
  requires:
    - MathGaze/Core/Geometry/GeometryObject.cs
    - MathGaze/Core/Commands/IGeometryCommand.cs
    - MathGaze/Services/IGeometryService.cs
    - MathGaze/Core/Geometry/LineObject.cs
  provides:
    - ProtractorObject data model (all D-06 fields + DefaultRadiusPt constant)
    - ProtractorStyle enum (Classic180, Full360)
    - RotateProtractorCommand (undoable rotation)
    - FlipProtractorCommand (undoable flip)
    - StyleProtractorCommand (undoable style swap)
    - GeometryMath.TryLineIntersectPt (PDF-space line intersection)
  affects:
    - Plans 02, 03, 04 — all read ProtractorObject; Plan 02 calls TryLineIntersectPt for placement
tech_stack:
  added: []
  patterns:
    - ProtractorObject.HitTest uses proxy-point radius pattern (same as CircleObject) — CoordinateMapper.Scale is private; derive screen radius via mapper.PageToScreen(CenterXPt + DefaultRadiusPt, CenterYPt)
    - Commands use LINQ lookup on IGeometryService.Objects (same as NudgeObjectCommand pattern)
    - TryLineIntersectPt uses double precision with 1e-9 parallel guard vs 1e-6f in float TryLineIntersect
key_files:
  created:
    - MathGaze/Core/Geometry/ProtractorObject.cs
    - MathGaze/Core/Commands/RotateProtractorCommand.cs
    - MathGaze/Core/Commands/FlipProtractorCommand.cs
    - MathGaze/Core/Commands/StyleProtractorCommand.cs
  modified:
    - MathGaze/Core/GeometryMath.cs
decisions:
  - "CoordinateMapper.Scale is private — ProtractorObject.HitTest derives screen radius using proxy-point offset (PageToScreen(CenterXPt + DefaultRadiusPt, CenterYPt).X - center.X), identical to CircleObject pattern"
  - "BaselineAngleDeg stored as screen-space CW angle at placement time (per RESEARCH.md recommendation) so RotateDegrees can be applied directly in SkiaSharp renderer without Y-flip correction"
  - "DefaultRadiusPt = 108.0 (108pt × 1.333 ≈ 144px at zoom=1, 96 DPI) — closest clean integer to target ~150px from D-04"
  - "TryLineIntersectPt uses 1e-9 threshold (double precision) vs existing TryLineIntersect 1e-6f (float) to match precision of PDF-space double coordinates"
metrics:
  duration_minutes: 2
  completed_date: "2026-05-25"
  tasks_completed: 2
  files_created_or_modified: 5
---

# Phase 03 Plan 01: Protractor Model and Commands Summary

**One-liner:** ProtractorObject data model with five D-06 fields plus ProtractorStyle enum, three undoable commands (Rotate/Flip/Style), and double-precision PDF-space line intersection math helper.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Create ProtractorObject model and ProtractorStyle enum | 632df9f | MathGaze/Core/Geometry/ProtractorObject.cs (created) |
| 2 | Add three protractor commands and extend GeometryMath | 7f85378 | RotateProtractorCommand.cs, FlipProtractorCommand.cs, StyleProtractorCommand.cs (created); GeometryMath.cs (modified) |

## Verification

Final build: `dotnet build MathGaze/MathGaze.csproj` — **0 errors, 9 warnings** (all pre-existing NU1701 package target framework warnings, not introduced by this plan).

All five acceptance criteria checks confirmed:
- `ProtractorObject.cs` contains `public sealed class ProtractorObject : GeometryObject`
- `ProtractorObject.cs` contains `public enum ProtractorStyle { Classic180, Full360 }`
- `ProtractorObject.cs` contains `public const double DefaultRadiusPt = 108.0`
- `ProtractorObject.cs` contains all D-06 fields: CenterXPt, CenterYPt, BaselineAngleDeg, RotationOffsetDeg, IsFlipped, Style, Line1Id, Line2Id
- `GeometryMath.cs` contains both `TryLineIntersect` (float/SKPoint) and `TryLineIntersectPt` (double/LineObject)
- `GeometryMath.cs` contains `Math.Abs(denom) < 1e-9`
- `RotateProtractorCommand.cs` contains `p.RotationOffsetDeg += delta`
- `FlipProtractorCommand.cs` contains `p.IsFlipped = !p.IsFlipped`
- `StyleProtractorCommand.cs` contains `p.Style = style`

## Deviations from Plan

### Auto-fixed Issues

None — plan executed exactly as written.

**One deviation from the plan spec (not a bug fix):** The plan's `HitTest` action specifies `float radiusPx = (float)(DefaultRadiusPt * mapper.Scale)`, but `CoordinateMapper.Scale` is a private property. Applied the same proxy-point pattern used by `CircleObject.HitTest` instead: `edgeScreen.X - centerScreen.X` where `edgeScreen = mapper.PageToScreen(CenterXPt + DefaultRadiusPt, CenterYPt)`. This is mathematically equivalent and follows the established codebase pattern. Documented as a decision above.

## Known Stubs

None — this plan is a pure model/math layer. No UI, no rendering, no data source wiring.

## Threat Flags

None — no new network endpoints, auth paths, file access patterns, or schema changes at trust boundaries.

## Self-Check: PASSED

- `MathGaze/Core/Geometry/ProtractorObject.cs` — FOUND
- `MathGaze/Core/Commands/RotateProtractorCommand.cs` — FOUND
- `MathGaze/Core/Commands/FlipProtractorCommand.cs` — FOUND
- `MathGaze/Core/Commands/StyleProtractorCommand.cs` — FOUND
- `MathGaze/Core/GeometryMath.cs` (modified) — FOUND
- Commit 632df9f — FOUND
- Commit 7f85378 — FOUND
