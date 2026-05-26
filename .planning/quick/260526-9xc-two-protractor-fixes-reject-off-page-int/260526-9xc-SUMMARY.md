---
quick_id: 260526-9xc
status: complete
commit: c2f2c60
date: 2026-05-26
---

# Summary: 260526-9xc

**Two protractor fixes: reject off-page intersection; same-line click places at nearest endpoint**

## What changed

**File:** `MathGaze/ViewModels/ToolViewModel.cs` — `HandleCanvasClick`, protractor AnchorPlaced case

### Fix 1 — Off-page intersection rejected

Replaced `Math.Clamp(interPt.xPt, margin, PageWidthPt - margin)` silent clamping with an explicit
bounds check. If the intersection lies outside page bounds (±20pt margin), shows:

> "Lines don't intersect on this page — extend lines to create a crossing"

State stays at `AnchorPlaced` so the student can pick a different second line without restarting.
The protractor constructor now receives raw `interPt.xPt`/`interPt.yPt` (no clamping).

**Root cause of the UAT failure:** `TryLineIntersectPt` only detected true parallel lines
(denom ≈ 0). Near-parallel lines with off-screen crossings passed the check and were
silently placed at a clamped page-edge position.

### Fix 2 — Same-line second click → nearest endpoint

`if (line2.Id == AnchorLine.Id) break;` replaced with endpoint placement logic:
- Nearest endpoint to click = protractor center
- Baseline aligned to line direction (screen-space atan2)
- Arc forced upward: flip 180° if `cos(baselineAngle) < 0`
- Both `line1Id` and `line2Id` = same line ID; angle readout suppressed (0° < 0.5° guard)

**Use case:** Student draws a line, clicks it twice to place a free-rotation protractor at
one vertex to construct or verify an angle — no second reference line required.

## Decisions

- Parallel lines error resets to Idle (clears AnchorLine); off-page error stays in AnchorPlaced
  (keeps first line highlighted) — different UX since off-page is recoverable by picking a closer line.
- Arc direction for same-line: "faces upward on screen" heuristic via cos check. Student can
  always use the Flip button in the right rail to switch to the other half-plane.
