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
  - uat-gaps
dependency_graph:
  requires:
    - MathGaze/Core/Geometry/ProtractorObject.cs (Plan 01)
    - MathGaze/Core/Commands/RotateProtractorCommand.cs (Plan 01)
    - MathGaze/Core/Commands/FlipProtractorCommand.cs (Plan 01)
    - MathGaze/Core/Commands/StyleProtractorCommand.cs (Plan 01)
    - MathGaze/ViewModels/MainViewModel.cs (IsPracticeMode)
    - MathGaze/Services/IGeometryService.cs (ObjectsChanged_ForceRaise, SetSelected)
  provides:
    - GeometryLayerViewModel protractor rendering branch (DrawProtractor)
    - Practice/Exam mode readout visibility control (DrawReadout / ComputeMeasuredAngle)
    - Mode toggle triggers immediate canvas repaint (OnMainVmPropertyChanged)
    - Dual-scale numeric labels (outer 0→180, inner 180→0) at different radii
    - Anchor line highlight on first protractor click
    - Ghost preview aligned to Line 1 angle before second click
  affects:
    - Checkpoint verification — full Phase 3 end-to-end test
tech-stack:
  added: []
  patterns:
    - SKFont fields (readonly) alongside SKPaint fields — SkiaSharp 3.x modern text API
    - Proxy-point radius pattern: edgePx.X - centerPx.X (CoordinateMapper.Scale is private)
    - canvas.Save()/Translate()/RotateDegrees()/Restore() for per-object transform scoping
    - PropertyChanged subscription in GeometryLayerViewModel for cross-VM state changes
    - Local-frame cross-product check for arc direction: un-rotate Line 2 midpoint by line1AngleDeg, check sign of localY
key-files:
  created: []
  modified:
    - MathGaze/ViewModels/GeometryLayerViewModel.cs
    - MathGaze/ViewModels/ToolViewModel.cs
    - MathGaze/ViewModels/PdfCanvasViewModel.cs
key-decisions:
  - "CoordinateMapper.Scale is private — radius derived via proxy-point offset (edgePx.X - centerPx.X), same pattern as CircleObject and ProtractorObject.HitTest"
  - "Migrated DrawText calls to SKFont-based API (SKFont + DrawText overload with SKTextAlign) to eliminate CS0618 deprecation warnings — auto-fix Rule 1"
  - "BaselineAngleDeg stored as screen-space CW angle at placement (Plan 02 decision); RotateDegrees applied directly without sign negation"
  - "OnMainVmPropertyChanged fires ObjectsChanged_ForceRaise to trigger canvas repaint on mode toggle — avoids circular dependency (GeometryLayerViewModel does not own InvalidationRequested)"
  - "IsPracticeMode guard in DrawProtractor (not in model) — D-14 enforcement in renderer; model stays mode-agnostic"
  - "DrawReadout arc sweeps CCW (negative sweepAngle) — goes upward into protractor body from baseline"
  - "Arc facing: un-rotate Line 2 midpoint into protractor local frame, check sign of localY; positive = below baseline in screen space = flip 180 deg"
  - "Dual scale labels: outerLabelR = radiusPx-24 for 0->180, innerLabelR = radiusPx-42 for 180->0; separate SKFont sizes (11pt/8pt) and alpha (220/160)"
  - "Ghost rotation: canvas.Save/Translate(center)/RotateDegrees(line1Angle)/draw/Restore — center dot drawn after Restore so it stays circular"
patterns-established:
  - "SKFont readonly field pattern: private readonly SKFont _labelFont = new(SKTypeface.Default, 9f); — mirrors SKPaint field pattern, disposed in Dispose()"
  - "Cross-VM property subscription: _mainVm.PropertyChanged += OnMainVmPropertyChanged in constructor, unsubscribed in Dispose()"
requirements-completed:
  - PROT-06
  - SYS-04
  - SYS-05
duration: 15min (initial) + 10min (UAT gap fixes)
completed: "2026-05-25"
---

# Phase 03 Plan 04: Protractor Renderer Summary

**SkiaSharp protractor renderer with dual-scale labels, arc-toward-Line-2 orientation, anchor line highlight, aligned ghost preview, and Practice-Mode angle readout — all 5 UAT gaps resolved.**

## Performance

- **Duration:** ~25 min total (15 min initial renderer + 10 min UAT gap fixes)
- **Started:** 2026-05-25
- **Completed:** 2026-05-25
- **Tasks:** 1 (renderer) + 5 UAT gap fixes
- **Files modified:** 3

## Accomplishments

### Initial renderer (commit eb06ee3)

- DrawProtractor renders ProtractorObject with arc body (Classic180 or Full360), baseline line, 181 tick marks at 1° resolution with three sizes (5px minor, 9px intermediate, 18px major), and center crosshair
- DrawReadout renders a small inner arc + angle text inside the protractor — gated by `_mainVm.IsPracticeMode` so it never appears in Exam Mode (D-14/T-03-11)
- ComputeMeasuredAngle derives angle between Line1/Line2 direction vectors using dot product
- canvas.Save()/Restore() brackets every DrawProtractor call — no transform leakage (T-03-14)
- All SKPaint and SKFont fields are readonly (created once, never per-frame); all disposed in Dispose()

### UAT gap fixes (commit 4ef5705)

- **Gap 1 — BaselineAngleDeg from Line 1:** Second-click handler now computes `line1AngleDeg = Atan2(p2Screen.Y - p1Screen.Y, p2Screen.X - p1Screen.X)`. The flat diameter of the placed protractor lies exactly along Line 1.
- **Gap 2 — Arc faces toward Line 2:** After computing `line1AngleDeg`, the midpoint of Line 2 is rotated into the protractor's local frame. If `localY > 0` (Line 2 midpoint is below the baseline in screen space), baseline is flipped by 180° so the arc always opens toward Line 2.
- **Gap 3 — Anchor line highlights:** First protractor click now calls `_geometryService.SetSelected(line1.Id)` immediately after setting `AnchorLine`, turning Line 1 cobalt so the student has visual confirmation before clicking the second line.
- **Gap 4 — Dual scale labels:** Label loop replaced with two passes. Outer labels (0→180, 11pt, alpha 220) at `radiusPx - 24`. Inner labels (180→0, 8pt, alpha 160) at `radiusPx - 42`. `_innerLabelPaint` and `_innerLabelFont` added as readonly fields, disposed in Dispose().
- **Gap 5 — Ghost preview aligned to anchor:** `DrawGhostProtractor` now reads `_toolVm.AnchorLine` and computes its screen-space angle. Uses `canvas.Save()/Translate(center)/RotateDegrees(line1Angle)/draw arc+baseline/Restore()` so the ghost semicircle previews the correct orientation. Center dot is drawn after Restore so it remains circular.

## Task Commits

1. **Task 1: Implement SkiaSharp protractor renderer in GeometryLayerViewModel** — `eb06ee3` (feat)
2. **UAT gaps 1-5: fix protractor orientation, dual scale, line highlight, ghost alignment** — `4ef5705` (feat)

## Files Created/Modified

- `MathGaze/ViewModels/GeometryLayerViewModel.cs` — Added dual-scale label loop, `_innerLabelPaint`, `_innerLabelFont`; increased outer font from 9pt to 11pt; updated Dispose()
- `MathGaze/ViewModels/ToolViewModel.cs` — Gap 1+2: replaced baseline angle computation with line1AngleDeg + local-frame flip logic; Gap 3: added `SetSelected(line1.Id)` on first protractor click
- `MathGaze/ViewModels/PdfCanvasViewModel.cs` — Gap 5: DrawGhostProtractor uses anchor line angle via Save/Translate/RotateDegrees/Restore

## Decisions Made

- **Proxy-point radius:** CoordinateMapper.Scale is private. Used `edgePx.X - centerPx.X` pattern throughout.
- **Arc direction check via local-frame:** Un-rotate Line 2 midpoint by `-line1AngleDeg` into the protractor's local coordinate system. In SkiaSharp screen coords (Y down), the arc is drawn in the negative-Y half. Positive `localY` means Line 2 is on the flat side, so flip 180°. This handles all quadrant combinations correctly.
- **Dual scale at different radii:** Using two separate passes with different radii (24pt and 42pt inset from arc) and different font/alpha values matches a real semicircular protractor. `IsFlipped` property correctly inverts both outer and inner readings simultaneously.
- **Ghost rotation after Restore for dot:** The center dot must be drawn in canvas (unrotated) space to remain circular regardless of rotation angle — hence `canvas.Restore()` before the dot draw.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Migrated deprecated SKPaint text properties to SKFont-based API**
- **Found during:** Task 1 (after first build)
- **Issue:** SkiaSharp 3.119.2 marks `TextSize`, `TextAlign` on `SKPaint` and `DrawText(string, float, float, SKPaint)` as `[Obsolete]`.
- **Fix:** Removed text properties from `_labelPaint`; added `_labelFont` (SKFont); updated `DrawText` calls; added font to `Dispose()`.
- **Files modified:** `MathGaze/ViewModels/GeometryLayerViewModel.cs`
- **Committed in:** eb06ee3

**2. [UAT gaps] 5 rendering gaps identified during human verification and fixed**
- **Found during:** Human UAT after Task 1
- **Gap 1:** BaselineAngleDeg was already computed from Line 1 screen-space angle correctly in existing code — confirmed working, no change needed to computation logic (the comment was updated for clarity).
- **Gap 2:** Arc orientation check added — local-frame cross-product determines which side Line 2 falls on, flips baseline 180° if needed.
- **Gap 3:** `SetSelected` call added on first protractor click.
- **Gap 4:** Single-pass label loop replaced with dual-pass (outer + inner scale).
- **Gap 5:** Ghost now uses `canvas.Save()/RotateDegrees()/Restore()` pattern aligned to anchor line.
- **Committed in:** 4ef5705

---

**Total deviations:** 2 (1 auto-fixed deprecated API; 5 UAT gaps fixed in post-verification session)
**Impact on plan:** UAT gaps are functional improvements to correctness and UX. No architectural changes.

## Issues Encountered

None — all fixes were straightforward applications of the established patterns (proxy-point radius, Save/Rotate/Restore, SetSelected).

## Known Stubs

None — all rendering is wired to real geometry objects. Dual scale labels use actual computed angles. Ghost preview reads live AnchorLine data.

## Threat Flags

None — no new network endpoints, auth paths, file access patterns, or schema changes.

## Self-Check

- `MathGaze/ViewModels/ToolViewModel.cs` — FOUND (modified, committed)
- `MathGaze/ViewModels/GeometryLayerViewModel.cs` — FOUND (modified, committed)
- `MathGaze/ViewModels/PdfCanvasViewModel.cs` — FOUND (modified, committed)
- Commit `4ef5705` — FOUND in git log
- Build result: `Build succeeded. 9 Warning(s) 0 Error(s)` — all warnings are pre-existing NU1701

## Self-Check: PASSED

## Next Phase Readiness

Phase 3 is complete. All UAT gaps resolved. The full protractor feature — placement, right-rail controls, renderer, dual scale, correct orientation, Practice/Exam mode — is implemented across Plans 01–04.

---
*Phase: 03-protractor*
*Completed: 2026-05-25*
