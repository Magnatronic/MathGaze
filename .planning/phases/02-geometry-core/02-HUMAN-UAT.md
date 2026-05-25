---
status: partial
phase: 02-geometry-core
source: [02-VERIFICATION.md]
started: 2026-05-03T07:09:27Z
updated: 2026-05-25T00:00:00Z
---

## Current Test

UAT session 2026-05-25 — all pending tests run in app. Test 1 FAIL: placement still inconsistent. New GAP-14 raised. Tests 2, 3, 5, 6, 10 passed. Test 8 deferred (no Grid 3 hardware).

## Tests

### 1. Geometry objects render correctly on canvas
expected: Point, Line, and Circle objects appear at correct screen positions on top of the PDF bitmap layer with correct visual styling (1A1A2E ink colour)
result: FAIL — Placement is inconsistent: sometimes lands exactly where clicked, sometimes offset. Consistent across all tested zoom levels. Clicking near an existing point sometimes places the new object at the existing point instead of cursor position. Same behaviour on Line and Circle. → GAP-14

### 2. Selection highlighting and sub-point tap target indicators
expected: Selected objects render in accent cobalt (#3B6FD4); selected Line shows 8px endpoint dots; selected Circle shows centre + edge dots; active sub-point shows additional 14px ring indicator
result: PASS — Selected objects turn cobalt. (2026-05-25)

### 3. Ghost preview during mid-draw
expected: After click 1 in Line or Circle mode, a dashed preview line/circle follows the cursor until click 2 commits the object; snap ring indicator appears when within 20px of a snap candidate
result: PASS — Snap ring always present during mid-draw; snaps to nearby endpoints correctly. (2026-05-25)

### 4. Nudge step accuracy
expected: Selecting 1/5/20px step and pressing a directional nudge button shifts the selected object by exactly that many PDF-space pixels; Up/Down directions correct
result: PASS — Nudge direction correct (GAP-3 resolved). Step accuracy not separately timed.

### 5. Undo/Redo button state management
expected: Undo button enables after the first geometry command; Redo enables after an undo; both disable at their respective stack limits
result: PASS — Undo and redo work correctly. (2026-05-25)

### 6. Gaze target size floor at actual screen DPI
expected: All interactive elements ≥56×56px on target school hardware
result: PASS — Targets visually meet size floor. (2026-05-25)

### 7. Right rail visual style
expected: All right rail buttons match app design language — white surface, BrushBorder, CornerRadius=6, no WPF chrome
result: PASS — Delete hover shows dark red; active step retains cobalt on hover (GAP-8, GAP-9 resolved).

### 8. Grid 3 / standard pointer click compatibility
expected: Full interaction loop works from assistive technology device
result: DEFERRED — No Grid 3 hardware available for testing. (2026-05-25)

### 9. Geometry state cleared on PDF reload
expected: Opening a new PDF resets the geometry canvas — no objects from the previous PDF appear on the new document
result: PASS — Canvas clears correctly when a new PDF is opened (GAP-10 resolved).

### 10. Geometry state cleared on page navigation
expected: Navigating to a different page within the same PDF shows a clean canvas — geometry from page N does not appear on page N+1
result: PASS — Canvas clears correctly on page navigation. (2026-05-25)

## Summary

total: 10
passed: 8
issues: 1
pending: 0
skipped: 0
blocked: 0
deferred: 1 (Test 8 — Grid 3 hardware unavailable)

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
status: resolved
description: Fixed in 02-11 — EnsureCoordinateMapper() now called synchronously in both SetDpiScale and SetCanvasSize (after OnCanvasSizeChanged, before LoadCurrentPageAsync). All ordering races between mapper init and first user click are eliminated. Confirmed by build + 62 unit tests. Pending human re-test at multiple zoom/DPI levels.
severity: blocking

### GAP-12: Horizontal snap direction not triggering
status: resolved
description: Fixed in 02-11/02-12 — SnapEngine section 3 orientation guides now wrapped in `if (label is null)` and use absolute SnapThresholdPx gate (not bestDist). A separate orientBestDist variable prevents cross-type suppression. 7 SnapEngineTests confirm horizontal, vertical, and 45° snap all work. Pending human re-test in running app.
severity: high

### GAP-13: Geometry persists when navigating between pages
status: resolved
description: Fixed in 02-13 — _geometryService.Reset() added as first statement in OnCurrentPageChanged. Reset() clears both object list and undo/redo stacks. OpenFileAsync Reset() (GAP-10) preserved. Confirmed in code. Pending human re-test in running app.
severity: blocking

### GAP-14: Placement intermittently offset — snap engaging unexpectedly
status: open
description: Object placement (Point, Line, Circle) is inconsistent — sometimes lands exactly at cursor, sometimes offset. Consistent across zoom levels. Clicking near an existing point sometimes places the new object at the existing point rather than the cursor. Snap threshold (20px) may be too aggressive, causing unintended snaps. Or a residual CoordinateMapper timing issue. Dashed ghost preview tracks cursor correctly during mid-draw, but committed position diverges. Needs code investigation.
severity: blocking
