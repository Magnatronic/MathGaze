---
status: partial
phase: 02-geometry-core
source: [02-VERIFICATION.md]
started: 2026-05-03T07:09:27Z
updated: 2026-05-04T00:00:00Z
---

## Current Test

Re-verification 2026-05-04 — GAP-6 through GAP-10 closed by plans 02-08/09/10. Awaiting human re-UAT to confirm fixes in running app.

## Tests

### 1. Geometry objects render correctly on canvas
expected: Point, Line, and Circle objects appear at correct screen positions on top of the PDF bitmap layer with correct visual styling (1A1A2E ink colour)
result: [pending re-test — GAP-6 DPI ordering fix applied, verify placement accurate at all zoom levels]

### 2. Selection highlighting and sub-point tap target indicators
expected: Selected objects render in accent cobalt (#3B6FD4); selected Line shows 8px endpoint dots; selected Circle shows centre + edge dots; active sub-point shows additional 14px ring indicator
result: [pending — not tested separately]

### 3. Ghost preview during mid-draw
expected: After click 1 in Line or Circle mode, a dashed preview line/circle follows the cursor until click 2 commits the object; snap ring indicator appears when within 20px of a snap candidate
result: [pending re-test — GAP-7 snap ring flicker fix applied, verify ring is always visible during draw]

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
result: [pending re-test — GAP-8 Delete hover + GAP-9 active step hover fixed; verify all button states correct]

### 8. Grid 3 / standard pointer click compatibility
expected: Full interaction loop works from assistive technology device
result: [pending]

### 9. Geometry state cleared on PDF reload
expected: Opening a new PDF resets the geometry canvas — no objects from the previous PDF appear on the new document
result: [pending re-test — GAP-10 Reset() call added to OpenFileAsync; verify canvas clears on every new PDF]

## Summary

total: 9
passed: 1
issues: 0
pending: 8
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
status: resolved
description: Fixed in 02-10 — SetDpiScale now called before SetCanvasSize in ReportCanvasSize; CoordinateMapper always created with correct _dpiScale.
severity: blocking

### GAP-7: Snap ring flickers during mid-draw
status: resolved
description: Fixed in 02-10 — DrawGhostPreview now always renders a ring during AnchorPlaced state: lighter solid ring at GhostCursorPx when free, dashed cobalt at LastSnap.Position when snapped.
severity: high

### GAP-8: Delete button hover state unreadable
status: resolved
description: Fixed in 02-08 — DeleteButtonStyle added to AppStyles.xaml with IsMouseOver trigger setting #991818 (dark red) on Border. Delete button in RightRail.xaml uses DeleteButtonStyle.
severity: high

### GAP-9: Active step button loses cobalt highlight on hover
status: resolved
description: Fixed in 02-08 — StepButtonStyle trigger order corrected: generic IsMouseOver MultiTrigger now at position 2, IsMouseOver+Tag=active MultiTrigger at position 3 (last = wins).
severity: high

### GAP-10: Geometry objects persist when opening a new PDF
status: resolved
description: Fixed in 02-09 — IGeometryService injected into MainViewModel; _geometryService.Reset() called inside Dispatcher.InvokeAsync in OpenFileAsync before OnDocumentOpenedAsync.
severity: blocking
