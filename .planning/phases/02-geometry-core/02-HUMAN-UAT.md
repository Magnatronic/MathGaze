---
status: diagnosed
phase: 02-geometry-core
source: [02-VERIFICATION.md]
started: 2026-05-03T07:09:27Z
updated: 2026-05-03T07:17:00Z
---

## Current Test

Human testing complete — 5 issues found.

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
status: failed
description: Committed point objects do not appear at the gaze cursor position. Likely a mismatch between WPF logical coordinates (MouseDown position) and the physical-pixel PDF coordinate space used by the rendering layer.
severity: blocking

### GAP-2: Ghost preview / snap ring cursor misalignment
status: failed
description: During mid-draw (after click 1), the dashed ghost line and snap ring indicator do not track accurately to the cursor position. Same root cause as GAP-1 — DPI/coordinate conversion in HandleMouseMove path.
severity: blocking

### GAP-3: Up/Down nudge directions inverted
status: failed
description: Pressing the Up nudge button moves the object downward; Down moves it upward. The Y-axis delta sign is reversed in NudgeObjectCommand or NudgeEndpointCommand.
severity: blocking

### GAP-4: Right rail visual style does not match app UI
status: failed
description: Right rail uses default WPF gray button styling. Needs to match the clean minimal white style of the rest of the app (toolbar, tool rail). Buttons should have consistent border radius, background, font, and spacing matching the established design language.
severity: high

### GAP-5: Step size selection has no visual highlight
status: failed
description: When selecting 1px, 5px, or 20px nudge step, the selected option is not visually distinguished from the others. User cannot tell which step size is currently active.
severity: high
