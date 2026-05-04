---
status: partial
phase: 02-geometry-core
source: [02-VERIFICATION.md]
started: 2026-05-03T07:09:27Z
updated: 2026-05-04T01:00:00Z
---

## Current Test

Re-UAT 2026-05-04 — 3/5 gap re-tests pass; 2 still failing; 1 new gap found (geometry persists across page navigation).

## Tests

### 1. Geometry objects render correctly on canvas
expected: Point, Line, and Circle objects appear at correct screen positions on top of the PDF bitmap layer with correct visual styling (1A1A2E ink colour)
result: FAIL — First click at a position places dot inaccurately; second click at the exact same position places correctly. Race condition in CoordinateMapper init — something (canvas size, DPI, zoom) isn't ready on first click but gets set by first-click side-effects. See GAP-11.

### 2. Selection highlighting and sub-point tap target indicators
expected: Selected objects render in accent cobalt (#3B6FD4); selected Line shows 8px endpoint dots; selected Circle shows centre + edge dots; active sub-point shows additional 14px ring indicator
result: [pending — not tested separately]

### 3. Ghost preview during mid-draw
expected: After click 1 in Line or Circle mode, a dashed preview line/circle follows the cursor until click 2 commits the object; snap ring indicator appears when within 20px of a snap candidate
result: PARTIAL — Ring is now always visible (GAP-7 fixed). Line preview tracks cursor correctly. Vertical and 45° angle snaps work. Horizontal (0°/180°) snap does not trigger. See GAP-12.

### 4. Nudge step accuracy
expected: Selecting 1/5/20px step and pressing a directional nudge button shifts the selected object by exactly that many PDF-space pixels; Up/Down directions correct
result: PASS — Nudge direction correct (GAP-3 resolved). Step accuracy not separately timed.

### 5. Undo/Redo button state management
expected: Undo button enables after the first geometry command; Redo enables after an undo; both disable at their respective stack limits
result: [pending]

### 6. Gaze target size floor at actual screen DPI
expected: All interactive elements ≥56×56px on target school hardware
result: [pending]

### 7. Right rail visual style
expected: All right rail buttons match app design language — white surface, BrushBorder, CornerRadius=6, no WPF chrome
result: PASS — Delete hover shows dark red; active step retains cobalt on hover (GAP-8, GAP-9 resolved).

### 8. Grid 3 / standard pointer click compatibility
expected: Full interaction loop works from assistive technology device
result: [pending]

### 9. Geometry state cleared on PDF reload
expected: Opening a new PDF resets the geometry canvas — no objects from the previous PDF appear on the new document
result: PASS — Canvas clears correctly when a new PDF is opened (GAP-10 resolved).

### 10. Geometry state cleared on page navigation
expected: Navigating to a different page within the same PDF shows a clean canvas — geometry from page N does not appear on page N+1
result: FAIL — Geometry objects persist when navigating between pages. See GAP-13.

## Summary

total: 10
passed: 3
issues: 3
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

### GAP-11: Point placement inconsistent — first click inaccurate, second click correct
status: failed
description: First click at a position places the dot at the wrong location; second click at the exact same position places correctly. Pattern indicates CoordinateMapper or a dependency (canvas ActualWidth/Height, zoom factor, DPI) is not fully initialised before the first click fires. The SetDpiScale/SetCanvasSize ordering fix was necessary but not sufficient — a lazy-init race remains on first interaction.
severity: blocking

### GAP-12: Horizontal snap direction not triggering
status: failed
description: Vertical (90°) and 45° angle snaps work correctly (confirmed by tooltip labels and ring position). Horizontal (0°/180°) snap does not trigger. Likely an edge case in SnapEngine's angle comparison at the 0°/360° boundary — the comparison probably uses < threshold rather than wrapping the angle range correctly.
severity: high

### GAP-13: Geometry persists when navigating between pages
status: failed
description: Geometry drawn on page N remains visible when navigating to page N+1 within the same PDF. GAP-10 (new PDF open) is fixed, but page navigation does not call Reset(). Each page should have an independent canvas — geometry on an exam question page must not bleed into adjacent pages.
severity: blocking
