---
plan: 05-01
phase: 05-angle-measurement
status: complete
started: 2026-05-28
completed: 2026-05-28
commits:
  - 8af53d6
  - 2e1f48e
---

## What Was Built

Two-point protractor placement: students can now place a protractor by clicking a canvas vertex then a direction point, without needing pre-drawn LineObjects. The existing two-line path (click Line 1 → click Line 2) is completely unchanged.

## Key Changes

### ToolViewModel.cs
- `ActivateProtractor`: status message updated to "Click vertex (or a line)"
- Protractor Idle case: branched — line hit → two-line path (existing), empty canvas → two-point path (new). `AnchorPt` stores vertex; `AnchorLine` stays null as discriminator
- Protractor AnchorPlaced case: wrapped existing two-line block in `if (AnchorLine is not null)`, added `else` block for two-point path — places `ProtractorObject` with `Guid.Empty, Guid.Empty` line IDs
- HandleMouseMove: status discriminated — "Click to set baseline direction" vs "Click 2nd line"

### PdfCanvasViewModel.cs
- `DrawGhostProtractor`: two-point branch added — ghost arc anchored at vertex (`ghostCenterPx` from `AnchorPt`), rotates toward cursor; dashed arm line from vertex to cursor drawn after arc

### GeometryLayerViewModel.cs
- `DrawProtractor` Practice Mode readout: guarded with `obj.Line1Id != Guid.Empty` to suppress misleading "0°" for two-point protractors

## Self-Check

| Criterion | Status |
|-----------|--------|
| `AnchorLine is null` discriminator in AnchorPlaced case | ✓ |
| `"Click vertex (or a line)"` in ActivateProtractor | ✓ |
| `"Click to set baseline direction"` in Idle branch + HandleMouseMove | ✓ |
| `Guid.Empty, Guid.Empty` in two-point ProtractorObject construction | ✓ |
| `_toolVm.AnchorPt.HasValue` in DrawGhostProtractor | ✓ |
| `ghostCenterPx` computed variable in DrawGhostProtractor | ✓ |
| `obj.Line1Id != Guid.Empty` readout guard | ✓ |
| Build: 0 errors, no new warnings | ✓ |
| Out-of-scope files modified | ✗ None |

## Self-Check: PASSED

## key-files

### created
(none — all files modified, not created)

### modified
- MathGaze/ViewModels/ToolViewModel.cs
- MathGaze/ViewModels/PdfCanvasViewModel.cs
- MathGaze/ViewModels/GeometryLayerViewModel.cs

## Deviations

None. Implementation matches plan exactly. ~40 lines of new/changed code as estimated.
