---
phase: 03-protractor
plan: "04"
subsystem: ui-renderer
tags:
  - protractor
  - skiasharp
  - rendering
  - practice-mode
  - exam-mode
dependency_graph:
  requires:
    - MathGaze/Core/Geometry/ProtractorObject.cs (Plan 01)
    - MathGaze/Core/Commands/RotateProtractorCommand.cs (Plan 01)
    - MathGaze/Core/Commands/FlipProtractorCommand.cs (Plan 01)
    - MathGaze/Core/Commands/StyleProtractorCommand.cs (Plan 01)
    - MathGaze/ViewModels/MainViewModel.cs (IsPracticeMode)
    - MathGaze/Services/IGeometryService.cs (ObjectsChanged_ForceRaise)
  provides:
    - GeometryLayerViewModel protractor rendering branch (DrawProtractor)
    - Practice/Exam mode readout visibility control (DrawReadout / ComputeMeasuredAngle)
    - Mode toggle triggers immediate canvas repaint (OnMainVmPropertyChanged)
  affects:
    - Checkpoint verification — full Phase 3 end-to-end test
tech-stack:
  added: []
  patterns:
    - SKFont fields (readonly) alongside SKPaint fields — SkiaSharp 3.x modern text API
    - Proxy-point radius pattern: edgePx.X - centerPx.X (CoordinateMapper.Scale is private)
    - canvas.Save()/Translate()/RotateDegrees()/Restore() for per-object transform scoping
    - PropertyChanged subscription in GeometryLayerViewModel for cross-VM state changes
key-files:
  created: []
  modified:
    - MathGaze/ViewModels/GeometryLayerViewModel.cs
key-decisions:
  - "CoordinateMapper.Scale is private — radius derived via proxy-point offset (edgePx.X - centerPx.X), same pattern as CircleObject and ProtractorObject.HitTest"
  - "Migrated DrawText calls to SKFont-based API (SKFont + DrawText overload with SKTextAlign) to eliminate CS0618 deprecation warnings — auto-fix Rule 1"
  - "BaselineAngleDeg stored as screen-space CW angle at placement (Plan 02 decision); RotateDegrees applied directly without sign negation"
  - "OnMainVmPropertyChanged fires ObjectsChanged_ForceRaise to trigger canvas repaint on mode toggle — avoids circular dependency (GeometryLayerViewModel does not own InvalidationRequested)"
  - "IsPracticeMode guard in DrawProtractor (not in model) — D-14 enforcement in renderer; model stays mode-agnostic"
  - "DrawReadout arc sweeps CCW (negative sweepAngle) — goes upward into protractor body from baseline"
patterns-established:
  - "SKFont readonly field pattern: private readonly SKFont _labelFont = new(SKTypeface.Default, 9f); — mirrors SKPaint field pattern, disposed in Dispose()"
  - "Cross-VM property subscription: _mainVm.PropertyChanged += OnMainVmPropertyChanged in constructor, unsubscribed in Dispose()"
requirements-completed:
  - PROT-06
  - SYS-04
  - SYS-05
duration: 10min
completed: "2026-05-25"
---

# Phase 03 Plan 04: Protractor Renderer Summary

**SkiaSharp protractor renderer in GeometryLayerViewModel: arc body, 181 graduated tick marks (1°/5°/10°), numeric labels, center crosshair, and Practice-Mode-only angle readout arc + text wired to IsPracticeMode toggle.**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-05-25
- **Completed:** 2026-05-25
- **Tasks:** 1 (Task 2 is a human-verify checkpoint)
- **Files modified:** 1

## Accomplishments

- DrawProtractor renders ProtractorObject with arc body (Classic180 or Full360), baseline line, 181 tick marks at 1° resolution with three sizes (5px minor, 9px intermediate, 10px major), numeric labels every 10° with IsFlipped reversal, and center crosshair
- DrawReadout renders a small inner arc + angle text inside the protractor — gated by `_mainVm.IsPracticeMode` check so it never appears in Exam Mode (D-14/T-03-11)
- ComputeMeasuredAngle derives angle between Line1/Line2 direction vectors using dot product; Full360 shows RotationOffsetDeg bearing, Classic180 applies IsFlipped for inner/outer reading
- Mode toggle fires `ObjectsChanged_ForceRaise` via `OnMainVmPropertyChanged` subscription, causing immediate canvas repaint when student switches Practice/Exam mode
- canvas.Save()/Restore() brackets every DrawProtractor call — no transform leakage between objects (T-03-14)
- All 5 new SKPaint fields + 2 new SKFont fields are readonly (created once, never per-frame); all disposed in Dispose()

## Task Commits

1. **Task 1: Implement SkiaSharp protractor renderer in GeometryLayerViewModel** — `eb06ee3` (feat)

## Files Created/Modified

- `MathGaze/ViewModels/GeometryLayerViewModel.cs` — Added MainViewModel dependency, 5 SKPaint + 2 SKFont fields, OnMainVmPropertyChanged, DrawProtractor, ComputeMeasuredAngle, DrawReadout; added `case ProtractorObject prot:` to DrawObject switch; updated Dispose()

## Decisions Made

- **Proxy-point radius:** CoordinateMapper.Scale is private. Used `edgePx.X - centerPx.X` where `edgePx = mapper.PageToScreen(CenterXPt + DefaultRadiusPt, CenterYPt)`. Identical to CircleObject and ProtractorObject.HitTest patterns — consistent and uses only the public API.
- **No negation of BaselineAngleDeg:** Plan 02 stored BaselineAngleDeg as screen-space CW angle (using `Math.Atan2` on screen-pixel vectors). `canvas.RotateDegrees` is also CW in screen space, so `totalRotDeg = BaselineAngleDeg + RotationOffsetDeg` is correct without negation.
- **SKFont migration:** SkiaSharp 3.119.2 deprecates `SKPaint.TextSize`, `SKPaint.TextAlign`, and `DrawText(string, float, float, SKPaint)`. Migrated to `SKFont` + `DrawText(string, x, y, SKTextAlign, SKFont, SKPaint)` to keep the build clean. Applied as Rule 1 auto-fix.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Migrated deprecated SKPaint text properties to SKFont-based API**
- **Found during:** Task 1 (after first build)
- **Issue:** Plan spec used `TextSize` and `TextAlign` on `SKPaint` and `DrawText(string, float, float, SKPaint)` overload. SkiaSharp 3.119.2 marks these as `[Obsolete]` — generated 8 CS0618 warnings not present before this plan. The build succeeded but introduced new warning noise.
- **Fix:** Removed `TextSize` and `TextAlign` from `_labelPaint` and `_readoutTextPaint`; added `private readonly SKFont _labelFont = new(SKTypeface.Default, 9f)` and `_readoutFont = new(SKTypeface.Default, 14f)`; updated `DrawText` calls to `canvas.DrawText(text, x, y, SKTextAlign.Center, font, paint)`; added both fonts to `Dispose()`.
- **Files modified:** `MathGaze/ViewModels/GeometryLayerViewModel.cs`
- **Verification:** Rebuild shows 0 errors, 9 warnings — all pre-existing NU1701, no new CS0618 warnings.
- **Committed in:** eb06ee3 (Task 1 commit, part of the same fix iteration)

---

**Total deviations:** 1 auto-fixed (Rule 1 — deprecated API warning cleanup)
**Impact on plan:** Purely an API modernisation. No functional change. No scope creep.

## Issues Encountered

None — the proxy-point radius pattern was anticipated by the plan (same as Plans 01 and 02). The only unexpected item was the CS0618 deprecation warnings, resolved immediately.

## Known Stubs

None — this plan delivers a fully functional renderer. DrawProtractor renders all visual elements. ComputeMeasuredAngle computes from real line geometry. DrawReadout is gated by the real IsPracticeMode bool. No placeholder values, no hardcoded empty data.

## Threat Flags

None — no new network endpoints, auth paths, file access patterns, or schema changes at trust boundaries.

Threat model items from the plan implemented:
- T-03-11 (mitigate): `if (_mainVm.IsPracticeMode)` guard — readout code path never reached in Exam Mode
- T-03-12 (accept): 181-tick loop is ~362 cheap float ops per protractor per frame; well within 60 FPS budget; no allocation
- T-03-13 (accept): `FirstOrDefault` returns null when lines deleted; `ComputeMeasuredAngle` returns 0f — readout shows 0°, no crash
- T-03-14 (mitigate): `canvas.Save()` is first call; `canvas.Restore()` is last call; no early returns between them
- T-03-15 (accept): `IsPracticeMode` bool read on render thread — no torn-read risk for aligned bool on modern CPUs

## Next Phase Readiness

Phase 3 code work is complete. Checkpoint Task 2 (human-verify) is the final gate before Phase 3 is declared done. The full protractor feature — placement, right-rail controls, renderer, Practice/Exam mode — is implemented across Plans 01–04.

---
*Phase: 03-protractor*
*Completed: 2026-05-25*
