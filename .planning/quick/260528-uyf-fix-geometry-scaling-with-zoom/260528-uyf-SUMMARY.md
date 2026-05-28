---
phase: quick-260528-uyf
plan: 01
subsystem: rendering
tags: [zoom, geometry, scaling, skia, paint-cache]
dependency_graph:
  requires: [quick-260528-u67]
  provides: [geometry-zoom-proportional-scaling]
  affects: [PdfCanvasViewModel, GeometryLayerViewModel]
tech_stack:
  added: []
  patterns: [combinedScale-passthrough, paint-cache-invalidation-on-zoom]
key_files:
  modified:
    - MathGaze/ViewModels/PdfCanvasViewModel.cs
    - MathGaze/ViewModels/GeometryLayerViewModel.cs
decisions:
  - "Pass _dpiScale * ZoomFactor as combined scale to geometry Draw and ghost methods (not _dpiScale alone)"
  - "Rename _lastDpiScale to _lastScale in GeometryLayerViewModel so cache guard invalidates on zoom changes, not just DPI changes"
metrics:
  duration_minutes: 8
  completed_date: "2026-05-28"
  tasks_completed: 2
  files_modified: 2
---

# Quick Task 260528-uyf: Fix Geometry Scaling With Zoom — Summary

**One-liner:** Geometry stroke widths, dot radii, font sizes, tick lengths, and ghost previews now scale by `_dpiScale * ZoomFactor` so geometry stays proportional to PDF content at all zoom levels.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Pass combinedScale to geometry Draw and ghost methods | 22ee552 | PdfCanvasViewModel.cs |
| 2 | Rename _lastDpiScale to _lastScale; build verified | 5e60dba | GeometryLayerViewModel.cs |

## Changes Made

### Task 1 — PdfCanvasViewModel.cs (3 substitutions)

1. `_geometryLayer.Draw(canvas, _coordinateMapper, _dpiScale)` → `_geometryLayer.Draw(canvas, _coordinateMapper, _dpiScale * _mainVm.ZoomFactor)`
2. `float ds = (float)_dpiScale;` → `float ds = (float)(_dpiScale * _mainVm.ZoomFactor);` (in `DrawGhostPreview`)
3. `float dps = (float)_dpiScale;` → `float dps = (float)(_dpiScale * _mainVm.ZoomFactor);` (in `DrawGhostProtractor`)

### Task 2 — GeometryLayerViewModel.cs (field rename + comment updates)

- `private double _lastDpiScale = 0.0;` → `private double _lastScale = 0.0;`
- Cache guard comparison: `_lastDpiScale` → `_lastScale`
- Cache guard assignment: `_lastDpiScale` → `_lastScale`
- Inline comments updated to reference "combined scale (dpiScale * ZoomFactor)"

## Verification

- `_lastDpiScale` count in GeometryLayerViewModel.cs: 0
- `_dpiScale * _mainVm.ZoomFactor` count in PdfCanvasViewModel.cs: 3 (lines 216, 241, 367)
- Build: succeeded, 0 errors, 9 pre-existing NU1701 warnings (unchanged)

## Deviations from Plan

**1. [Rule 2 - Missing] Updated stale inline comment inside Draw()**
- **Found during:** Task 2
- **Issue:** After renaming `_lastDpiScale` to `_lastScale`, the existing inline comment `// Update paint/font sizes when DPI changes (first call forces update via _lastDpiScale=0)` still referenced the old field name and "DPI changes" only.
- **Fix:** Updated comment to `// Update paint/font sizes when combined scale changes (first call forces update via _lastScale=0)`.
- **Files modified:** MathGaze/ViewModels/GeometryLayerViewModel.cs
- **Commit:** 5e60dba

## Known Stubs

None.

## Threat Flags

None — changes are internal ViewModel arithmetic only; no new network endpoints, auth paths, file access patterns, or schema changes.

## Self-Check: PASSED

- `22ee552` found in git log: FOUND
- `5e60dba` found in git log: FOUND
- `MathGaze/ViewModels/PdfCanvasViewModel.cs` modified: FOUND
- `MathGaze/ViewModels/GeometryLayerViewModel.cs` modified: FOUND
- `_lastDpiScale` in GeometryLayerViewModel.cs: 0 occurrences (PASS)
- `_dpiScale * _mainVm.ZoomFactor` in PdfCanvasViewModel.cs: 3 occurrences (PASS)
- Build: succeeded, 0 errors (PASS)
