---
phase: 03-protractor
plan: "02"
subsystem: geometry-interaction
tags:
  - protractor
  - state-machine
  - ghost-preview
  - tool-viewmodel
  - geometry-service
dependency_graph:
  requires:
    - MathGaze/Core/Geometry/ProtractorObject.cs
    - MathGaze/Core/GeometryMath.cs (TryLineIntersectPt)
    - MathGaze/Core/Commands/PlaceObjectCommand.cs
    - MathGaze/Core/CoordinateMapper.cs
  provides:
    - ToolMode.Protractor enum value
    - ToolViewModel Protractor two-click state machine (Idle → AnchorPlaced → placed)
    - Parallel-lines error handling and Idle reset
    - Off-screen intersection clamping (20pt margin)
    - BaselineAngleDeg computed from screen-space Atan2
    - ActivateProtractorCommand (RelayCommand, bound in ToolRail.xaml)
    - GeometryService.NudgeObject ProtractorObject case
    - DrawGhostProtractor (50% alpha semicircle arc at cursor during click-2)
    - CoordinateMapper.PageWidthPt / PageHeightPt public properties
  affects:
    - Plans 03, 04 — protractor rendering and right-rail controls consume ToolViewModel.ActiveTool == Protractor
tech_stack:
  added: []
  patterns:
    - Protractor ghost radius derived via proxy-point offset (PageToScreen(DefaultRadiusPt, 0) - PageToScreen(0, 0)) because CoordinateMapper.Scale is private
    - DrawGhostPreview early-returns for Protractor before AnchorPt null-check — Protractor uses AnchorLine not AnchorPt
    - ToolRail Protractor button uses XAML Command binding (consistent with all other tool buttons)
key_files:
  created: []
  modified:
    - MathGaze/ViewModels/ToolViewModel.cs
    - MathGaze/Services/GeometryService.cs
    - MathGaze/ViewModels/PdfCanvasViewModel.cs
    - MathGaze/Core/CoordinateMapper.cs
    - MathGaze/Views/ToolRail.xaml
key_decisions:
  - "Ghost radius uses proxy-point pattern (PageToScreen(DefaultRadiusPt,0).X - PageToScreen(0,0).X) since CoordinateMapper.Scale is private — same pattern as ProtractorObject.HitTest in Plan 01"
  - "ToolRail Protractor button uses XAML Command binding (Command={Binding ActivateProtractorCommand}) — consistent with all other tool buttons; plan mentioned code-behind as an option but XAML binding is the established pattern"
  - "HandleMouseMove sets LastSnap=null and StatusMessage='Click 2nd line' for Protractor in AnchorPlaced state — no snap ring shown during protractor placement (cursor tracks freely)"
requirements_completed:
  - PROT-01
  - PROT-02
duration: 10min
completed: "2026-05-25"
---

# Phase 03 Plan 02: Protractor Interaction Layer Summary

**Protractor two-click state machine in ToolViewModel — click line 1 sets AnchorLine, click line 2 computes PDF-space intersection, clamps to 20pt margin, places ProtractorObject; parallel lines shows error and resets; ghost semicircle arc renders at cursor during click-2.**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-05-25
- **Completed:** 2026-05-25
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments

- Full Protractor two-click placement flow: click line 1 → "Click 2nd line", click line 2 → ProtractorObject placed and selected at intersection
- Parallel lines handled cleanly: error message "Lines are parallel — pick two non-parallel lines" shown, state resets to Idle
- Off-screen intersections clamped to 20pt margin using `Math.Clamp` — no out-of-bounds protractors
- BaselineAngleDeg stored as screen-space Atan2 angle so SkiaSharp RotateDegrees applies directly at render time
- GeometryService.NudgeObject now handles ProtractorObject (CenterXPt/CenterYPt) — nudge works immediately after placement
- Ghost protractor (50% alpha semicircle + baseline + center dot) renders at cursor during placement click-2
- CoordinateMapper gains public PageWidthPt/PageHeightPt for clamping; proxy-point radius pattern used in ghost renderer

## Task Commits

1. **Task 1: Extend ToolViewModel with Protractor state machine + wire ToolRail button** — `cd493ff` (feat)
2. **Task 2: Extend GeometryService.NudgeObject + add ghost protractor preview** — `bdb4b7b` (feat)

## Files Created/Modified

- `MathGaze/ViewModels/ToolViewModel.cs` — Added ToolMode.Protractor, AnchorLine field, ActivateProtractorCommand, two Protractor switch cases, HandleMouseMove Protractor branch, ResetDrawState clears AnchorLine
- `MathGaze/Services/GeometryService.cs` — Added ProtractorObject case to NudgeObject
- `MathGaze/ViewModels/PdfCanvasViewModel.cs` — DrawGhostPreview intercepts Protractor AnchorPlaced; DrawGhostProtractor method added
- `MathGaze/Core/CoordinateMapper.cs` — Added public PageWidthPt and PageHeightPt properties
- `MathGaze/Views/ToolRail.xaml` — Added x:Name="ProtractorButton", Command binding, removed "(Phase 3)" from tooltip

## Decisions Made

- **Ghost radius via proxy-point pattern:** `CoordinateMapper.Scale` is private. Used `PageToScreen(DefaultRadiusPt, 0).X - PageToScreen(0, 0).X` to derive the screen radius in pixels. Same approach as `ProtractorObject.HitTest` from Plan 01 — consistent with established codebase pattern.
- **XAML Command binding for ProtractorButton:** The plan mentioned code-behind wiring as an option, but all existing tool buttons use `Command="{Binding ...}"` XAML binding. Followed the established pattern for consistency.
- **HandleMouseMove Protractor branch:** During AnchorPlaced state with Protractor active, snap is disabled (`LastSnap = null`) and status shows "Click 2nd line". Ghost tracks cursor freely as specified in RESEARCH.md.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Added PageWidthPt/PageHeightPt to CoordinateMapper**
- **Found during:** Task 1 (Protractor AnchorPlaced case)
- **Issue:** Plan spec referenced `mapper.PageWidthPt` and `mapper.PageHeightPt` for clamping, but these properties did not exist on `CoordinateMapper` — only the private `_pageWidthPt`/`_pageHeightPt` fields existed.
- **Fix:** Added `public double PageWidthPt => _pageWidthPt;` and `public double PageHeightPt => _pageHeightPt;` to CoordinateMapper.cs. The plan's action block explicitly stated to add these if missing.
- **Files modified:** `MathGaze/Core/CoordinateMapper.cs`
- **Committed in:** cd493ff (Task 1 commit)

**2. [Rule 1 - Deviation] Ghost radius computed via proxy-point offset, not `_coordinateMapper.Scale`**
- **Found during:** Task 2 (DrawGhostProtractor implementation)
- **Issue:** Plan spec used `ProtractorObject.DefaultRadiusPt * _coordinateMapper.Scale` but `Scale` is a private property on `CoordinateMapper`. The plan's action block anticipated this and specified the proxy-point fallback.
- **Fix:** Used `PageToScreen(DefaultRadiusPt, 0).X - PageToScreen(0, 0).X` — mathematically equivalent, uses public API only.
- **Files modified:** `MathGaze/ViewModels/PdfCanvasViewModel.cs`
- **Committed in:** bdb4b7b (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (1 missing property, 1 private API workaround)
**Impact on plan:** Both adjustments were explicitly anticipated in the plan's action block instructions. No scope creep.

## Issues Encountered

None — build succeeded with 0 errors on both tasks. All 9 warnings are pre-existing NU1701 package target framework warnings, not introduced by this plan.

## Known Stubs

None — this plan wires interaction logic. No data source stubs, no placeholder UI, no hardcoded empty values flowing to rendering.

## Threat Flags

None — no new network endpoints, auth paths, file access patterns, or schema changes at trust boundaries. All threat model items from the plan's `<threat_model>` (T-03-04 through T-03-07) are implemented as specified.

## Next Phase Readiness

- Plan 03 (protractor rendering) can now read `ToolMode.Protractor`, `ProtractorObject` fields, and `ActiveTool` from ToolViewModel
- Plan 03 needs to implement the actual SkiaSharp draw of a placed ProtractorObject in GeometryLayerViewModel
- Plan 04 (right-rail controls) can bind ActivateProtractorCommand and read the placed protractor's state for rotate/flip buttons
- No blockers for Plan 03

## Self-Check: PASSED

- `MathGaze/ViewModels/ToolViewModel.cs` contains `ToolMode.Protractor` — CONFIRMED (line 11)
- `MathGaze/ViewModels/ToolViewModel.cs` contains `AnchorLine` field — CONFIRMED (line 31)
- `MathGaze/ViewModels/ToolViewModel.cs` contains `ActivateProtractor` RelayCommand — CONFIRMED (line 53)
- `MathGaze/ViewModels/ToolViewModel.cs` contains `case (ToolMode.Protractor, DrawState.Idle):` — CONFIRMED (line 140)
- `MathGaze/ViewModels/ToolViewModel.cs` contains `case (ToolMode.Protractor, DrawState.AnchorPlaced):` — CONFIRMED (line 153)
- `MathGaze/ViewModels/ToolViewModel.cs` contains `"Lines are parallel — pick two non-parallel lines"` — CONFIRMED (line 164)
- `MathGaze/ViewModels/ToolViewModel.cs` contains `Math.Clamp(interPt.xPt, margin,` — CONFIRMED (line 173)
- `MathGaze/Views/ToolRail.xaml` contains `x:Name="ProtractorButton"` — CONFIRMED
- `MathGaze/Services/GeometryService.cs` NudgeObject contains `case ProtractorObject p:` — CONFIRMED (line 55)
- `MathGaze/Services/GeometryService.cs` contains `p.CenterXPt += dxPt` — CONFIRMED
- `MathGaze/ViewModels/PdfCanvasViewModel.cs` contains `DrawGhostProtractor` — CONFIRMED (line 321)
- `MathGaze/ViewModels/PdfCanvasViewModel.cs` contains `ToolMode.Protractor` check in DrawGhostPreview — CONFIRMED (line 224)
- `MathGaze/Core/CoordinateMapper.cs` contains `PageWidthPt` and `PageHeightPt` — CONFIRMED
- Commit cd493ff — CONFIRMED
- Commit bdb4b7b — CONFIRMED
- Build: 0 errors, 9 warnings (all pre-existing) — CONFIRMED

---
*Phase: 03-protractor*
*Completed: 2026-05-25*
