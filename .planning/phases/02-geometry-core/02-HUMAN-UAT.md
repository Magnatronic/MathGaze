---
status: partial
phase: 02-geometry-core
source: [02-VERIFICATION.md]
started: 2026-05-03T07:09:27Z
updated: 2026-05-03T11:00:00Z
---

## Current Test

5 gaps fixed — awaiting human re-verification.

## Tests

### 1. Geometry objects render correctly on canvas
expected: Point, Line, and Circle objects appear at correct screen positions on top of the PDF bitmap layer with correct visual styling (1A1A2E ink colour)
result: FAIL — Point placement is inconsistent; the committed point object does not always appear under the gaze cursor at the click position. Appears to be a coordinate conversion offset.

### 2. Selection highlighting and sub-point tap target indicators
expected: Selected objects render in accent cobalt (#3B6FD4); selected Line shows 8px endpoint dots; selected Circle shows centre + edge dots; active sub-point shows additional 14px ring indicator
result: [pending — not tested separately]

### 3. Ghost preview during mid-draw
expected: After click 1 in Line or Circle mode, a dashed preview line/circle follows the cursor until click 2 commits the object; snap ring indicator appears when within 20px of a snap candidate
result: FAIL — Ghost preview line and snap indicator circle are misaligned with the actual cursor/click position during mid-draw. The snap ring does not track precisely to where the cursor is.

### 4. Nudge step accuracy
expected: Selecting 1/5/20px step and pressing a directional nudge button shifts the selected object (or its active endpoint) by exactly that many PDF-space pixels; visual result matches expectation
result: FAIL — Up and down nudge directions are inverted: pressing Up moves the object down, pressing Down moves it up.

### 5. Undo/Redo button state management
expected: Undo button enables after the first geometry command; Redo enables after an undo; both disable at their respective stack limits
result: [pending]

### 6. Gaze target size floor at actual screen DPI
expected: All interactive elements ≥56×56px on target school hardware
result: [pending]

### 7. Snap engine accuracy with gaze cursor
expected: Snap ring appears within 20px of endpoint/intersection/orientation candidates
result: FAIL — Snap ring position is misaligned (linked to same coordinate issue as tests 1 and 3)

### 8. Grid 3 / standard pointer click compatibility
expected: Full interaction loop works from assistive technology device
result: [pending]

## Summary

total: 8
passed: 0
issues: 5
pending: 3
skipped: 0
blocked: 0

## Gaps

### GAP-1: Point placement coordinate offset
status: resolved
description: Fixed in 02-06 — `_dpiScale` added to `LoadCurrentPageAsync` scale formula so bitmap dimensions match CoordinateMapper's physical-pixel space.
severity: blocking

### GAP-2: Ghost preview / snap ring cursor misalignment
status: resolved
description: Fixed in 02-06 — same root cause and fix as GAP-1.
severity: blocking

### GAP-3: Up/Down nudge directions inverted
status: resolved
description: Fixed in 02-06 — NudgeUp now passes `+NudgeStepPx`, NudgeDown passes `-NudgeStepPx`, matching PDF Y-axis convention.
severity: blocking

### GAP-4: Right rail visual style does not match app UI
status: resolved
description: Fixed in 02-07 — `RailButtonStyle` added to AppStyles.xaml and applied to all right rail action buttons.
severity: high

### GAP-5: Step size selection has no visual highlight
status: resolved
description: Fixed in 02-07 — `StepButtonStyle` with DataTrigger on `NudgeStepPx` gives active step cobalt highlight.
severity: high
