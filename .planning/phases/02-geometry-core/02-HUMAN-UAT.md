---
status: diagnosed
phase: 02-geometry-core
source: [02-VERIFICATION.md]
started: 2026-05-03T07:09:27Z
updated: 2026-05-04T00:00:00Z
---

## Current Test

Re-verification 2026-05-04 — 3 of 5 gaps confirmed closed; 2 partially fixed; 5 new issues found.

## Tests

### 1. Geometry objects render correctly on canvas
expected: Point, Line, and Circle objects appear at correct screen positions on top of the PDF bitmap layer with correct visual styling (1A1A2E ink colour)
result: PARTIAL — Placement is accurate sometimes but not always. DPI fix reduced the offset but intermittent inaccuracy remains (see GAP-6).

### 2. Selection highlighting and sub-point tap target indicators
expected: Selected objects render in accent cobalt (#3B6FD4); selected Line shows 8px endpoint dots; selected Circle shows centre + edge dots; active sub-point shows additional 14px ring indicator
result: [pending — not tested separately]

### 3. Ghost preview during mid-draw
expected: After click 1 in Line or Circle mode, a dashed preview line/circle follows the cursor until click 2 commits the object; snap ring indicator appears when within 20px of a snap candidate
result: PARTIAL — Ghost line tracks correctly but snap ring flickers: disappears and reappears during movement (see GAP-7).

### 4. Nudge step accuracy
expected: Selecting 1/5/20px step and pressing a directional nudge button shifts the selected object by exactly that many PDF-space pixels; Up/Down directions correct
result: PASS — Nudge direction now correct (GAP-3 resolved). Step accuracy not separately timed.

### 5. Undo/Redo button state management
expected: Undo button enables after the first geometry command; Redo enables after an undo; both disable at their respective stack limits
result: [pending]

### 6. Gaze target size floor at actual screen DPI
expected: All interactive elements ≥56×56px on target school hardware
result: [pending]

### 7. Right rail visual style
expected: All right rail buttons match app design language — white surface, BrushBorder, CornerRadius=6, no WPF chrome
result: PARTIAL — Normal state correct (GAP-4 resolved). Hover state on Delete button is unreadable: ControlTemplate hover trigger overrides local red background with BrushSurface2 cream, white text becomes invisible (see GAP-8). Step button active state loses cobalt on hover (see GAP-9).

### 8. Grid 3 / standard pointer click compatibility
expected: Full interaction loop works from assistive technology device
result: [pending]

### 9. Geometry state cleared on PDF reload
expected: Opening a new PDF resets the geometry canvas — no objects from the previous PDF appear on the new document
result: FAIL — Objects drawn on a previous PDF appear on the first page of the next PDF opened (see GAP-10).

## Summary

total: 9
passed: 1
issues: 6
pending: 4
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

### GAP-6: Point placement intermittently inaccurate
status: failed
description: After the DPI fix, placement is correct sometimes but not consistently. Likely a remaining coordinate conversion race or a separate code path (e.g. zoom != 1.0, or first-render timing) that still misaligns bitmap vs CoordinateMapper. Needs diagnostic logging of scale values at click time.
severity: blocking

### GAP-7: Snap ring flickers during mid-draw
status: failed
description: During a draw operation (after click 1), the snap ring indicator disappears and reappears as the cursor moves. Root cause likely: snap ring render is only invalidated on certain MouseMove events, or a condition in ToolViewModel clears the ghost state incorrectly between frames.
severity: high

### GAP-8: Delete button hover state unreadable
status: failed
description: RailButtonStyle ControlTemplate hover trigger sets the Border's Background to BrushSurface2 (cream) via TargetName="Bd", overriding the local Background="#CC2020" TemplateBinding. Foreground="White" stays white → white text on cream = invisible. Fix: Delete button needs a dedicated style or the hover trigger must preserve a custom background.
severity: high

### GAP-9: Active step button loses cobalt highlight on hover
status: failed
description: StepButtonStyle has two MultiTriggers for IsMouseOver. The generic IsMouseOver trigger (BrushSurface2) is listed after the IsMouseOver+Tag="active" trigger, so it wins in WPF trigger precedence (later in collection = higher priority). Active cobalt background is overridden by cream on hover. Fix: swap the order so active+hover trigger is last, or use a single MultiTrigger with an else-if pattern.
severity: high

### GAP-10: Geometry objects persist when opening a new PDF
status: failed
description: When a new PDF is loaded, the geometry object collection is not cleared. Objects drawn on the previous document appear on the first page of the new one. Root cause: `GeometryService` (or `PdfCanvasViewModel`) does not call a clear/reset when the PDF session changes.
severity: blocking
