---
phase: quick-260525-vf7
plan: 01
subsystem: protractor-renderer
tags: [bug-fix, protractor, angle-readout, IsFlipped]
dependency_graph:
  requires: []
  provides: [correct-protractor-arc-orientation, correct-flipped-readout-arc]
  affects: [ToolViewModel, GeometryLayerViewModel]
tech_stack:
  added: []
  patterns: [screenPx-direction-for-flip-check, isFlipped-arc-branch]
key_files:
  modified:
    - MathGaze/ViewModels/ToolViewModel.cs
    - MathGaze/ViewModels/GeometryLayerViewModel.cs
decisions:
  - "Use student click point (screenPx) not segment midpoint as flip direction vector — click location reliably indicates which side of the intersection the student intends"
  - "Degenerate guard threshold 5px (Euclidean) — consistent with existing 5px proximity tolerances in the codebase"
  - "isFlipped parameter defaults false so all non-flipped call paths are unchanged"
metrics:
  duration_minutes: 12
  completed: 2026-05-25
  tasks_completed: 2
  files_modified: 2
---

# Quick Task 260525-vf7: Fix Two Protractor Angle Bugs Summary

**One-liner:** Fix protractor flip check to use student click direction and fix flipped readout arc to start from -180° sweeping CW.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Fix flip check — use student click position | 450c5d5 | MathGaze/ViewModels/ToolViewModel.cs |
| 2 | Fix DrawReadout arc direction when IsFlipped=true | d1ae3eb | MathGaze/ViewModels/GeometryLayerViewModel.cs |

## What Was Built

### Task 1 — ToolViewModel.cs (~line 187)

The protractor placement flip check previously used the midpoint of Line 2 as the direction vector. When the student clicked near the intersection, the midpoint could be on the opposite side, causing the arc to face away from where the student clicked.

**Fix:** Replace midpoint calculation with `screenPx` (the student's actual click position). Add a degenerate guard: if the click is within 5px (Euclidean) of the intersection, fall back to the P1→P2 direction of Line 2.

Before:
```csharp
double mx2 = (line2.X1Pt + line2.X2Pt) / 2.0;
double my2 = (line2.Y1Pt + line2.Y2Pt) / 2.0;
var midScreen2 = mapper.PageToScreen(mx2, my2);
var intPtScreen = mapper.PageToScreen(interPt.xPt, interPt.yPt);
double dxScreen = midScreen2.X - intPtScreen.X;
double dyScreen = midScreen2.Y - intPtScreen.Y;
```

After:
```csharp
var intPtScreen = mapper.PageToScreen(interPt.xPt, interPt.yPt);
double dxScreen = screenPx.X - intPtScreen.X;
double dyScreen = screenPx.Y - intPtScreen.Y;
if (Math.Sqrt(dxScreen * dxScreen + dyScreen * dyScreen) < 5.0)
{
    var p1s = mapper.PageToScreen(line2.X1Pt, line2.Y1Pt);
    var p2s = mapper.PageToScreen(line2.X2Pt, line2.Y2Pt);
    dxScreen = p2s.X - p1s.X;
    dyScreen = p2s.Y - p1s.Y;
}
```

### Task 2 — GeometryLayerViewModel.cs (~lines 361, 416)

`DrawReadout` always started the arc at 0° (right end of baseline) sweeping CCW. When `IsFlipped=true` the outer scale's 0° is at the left end of the baseline (-180° in local canvas space), so the arc was drawn in the wrong quadrant.

**Fix:** Add `bool isFlipped = false` parameter. When `isFlipped=true`, start arc at -180° and sweep CW by `measuredAngleDeg`. Mid-angle for text label recalculated accordingly. Non-flipped path is unchanged.

Call site updated: `DrawReadout(canvas, measuredAngleDeg, radiusPx, obj.IsFlipped);`

## Verification

Build: zero CS compiler errors. The only build errors are MSB3027/MSB3021 file-lock warnings (the app was running during build — no compilation issue).

Manual smoke test (to be performed by user):
- Two lines at ~45°, click Line 2 close to intersection: protractor orients correctly (degenerate guard fires).
- Two lines at ~144°, click Line 2 on the obtuse side: readout shows 36° with arc in upper-LEFT quadrant.
- Two lines at ~40° (standard acute): readout shows 40° with arc in upper-RIGHT quadrant (unchanged behaviour).

## Deviations from Plan

None — plan executed exactly as written. Both code changes match the additional_context spec verbatim.

## Known Stubs

None.

## Threat Flags

None — changes are pure rendering/placement logic with no new network, auth, file, or schema surface.

## Self-Check: PASSED

- MathGaze/ViewModels/ToolViewModel.cs — modified, exists
- MathGaze/ViewModels/GeometryLayerViewModel.cs — modified, exists
- Commit 450c5d5 — exists (Task 1)
- Commit d1ae3eb — exists (Task 2)
