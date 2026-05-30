---
phase: 07-ui-improvements
plan: 07
subsystem: ui-polish
tags: [right-rail-width, icon-scale, snap-ring, skiaSharp-canvas, wpf-layout]
dependency_graph:
  requires: [07-05, 07-06]
  provides: [right-rail-180px, tool-icons-32px, idle-snap-ring]
  affects: [MainWindow.xaml, RightRail.xaml, ToolRail.xaml, PdfCanvasViewModel.cs]
tech_stack:
  added: []
  patterns: [SKPaint-idle-snap-block, WPF-ColumnDefinition-width, Viewbox-scale]
key_files:
  modified:
    - MathGaze/MainWindow.xaml
    - MathGaze/Views/RightRail.xaml
    - MathGaze/Views/ToolRail.xaml
    - MathGaze/ViewModels/PdfCanvasViewModel.cs
decisions:
  - "Idle snap ring uses variable name 'ids' (not 'ds') to avoid CS0136 name clash with the existing 'ds' float in the outer AnchorPlaced render block — both locals are in the same method scope"
  - "Idle snap ring block added BEFORE the AnchorPlaced early-return gate so it executes independently; existing AnchorPlaced path is completely unchanged"
metrics:
  duration: ~15 minutes
  completed: "2026-05-30"
  tasks_completed: 2
  files_modified: 4
---

# Phase 07 Plan 07: Visual Polish — Rail Width, Icon Scale, Idle Snap Ring Summary

One-liner: Widened right rail to 180px, scaled all six tool icons from 24x24 to 32x32 Viewbox with 13pt labels, and fixed Gap 8 by adding an Idle-state snap ring render path in DrawGhostPreview so cobalt snap feedback is visible before the first click on Line/Circle tools.

## What Was Built

Three visual polish gaps resolved:

**Fix 1 — Right rail width 148px → 180px (MainWindow.xaml, RightRail.xaml)**

The right-rail `ColumnDefinition` in `MainWindow.xaml` was changed from `Width="148"` to `Width="180"`. The `RightRail.xaml` `UserControl` width was updated to match. The `SettingsPanelOverlay` in `MainWindow.xaml` has no explicit width and fills `Grid.Column="2"` automatically — no change needed. Content inside RightRail (nudge pad, protractor rotate grid, object list) all fit comfortably at 180px.

Five content `TextBlock` elements in `RightRail.xaml` had their font sizes increased:
- `DrawingInstructionText`: 11 → 14pt
- `SelectedObjectType`: 11 → 14pt
- `NudgeLabel`: 11 → 14pt
- Object list `DisplayName`: 11 → 13pt
- "No objects on this page." description: 11 → 13pt

Section header chrome labels (`FontSize="9"` — "ROTATE", "STYLE", "NUDGE", "NO OBJECTS") were left at 9pt as intended UI chrome.

**Fix 2 — Tool rail icon scale and label font size (ToolRail.xaml)**

All 6 `<Viewbox>` elements (Select, Point, Line, Circle, Protractor, Text) changed from `Width="24" Height="24"` to `Width="32" Height="32"`. The inner `<Canvas Width="24" Height="24">` elements were not changed — those are viewport coordinate spaces for the SVG path data, not rendered sizes. All 6 tool button `<TextBlock>` labels received `FontSize="13"`.

**Fix 3 — Idle-state snap ring in DrawGhostPreview (PdfCanvasViewModel.cs) — Gap 8**

Root cause: `DrawGhostPreview` began with `if (_toolVm.DrawState != DrawState.AnchorPlaced) return;` which blocked ALL rendering including snap ring, even when `ToolViewModel.HandleMouseMove` had correctly computed `LastSnap` during `Idle` hover for Line/Circle tools.

Fix: Added a new block immediately after the null-coordinator guard and before the `AnchorPlaced` gate. When `DrawState == Idle` AND `ActiveTool` is `Line` or `Circle` AND `LastSnap.HasValue`:
- If snapped (`Label` is not null): draws a dashed cobalt ring (alpha=220) + filled dot (alpha=200) at the snap point — clear "will snap here" signal
- If near but not snapped (`Label` is null): draws a faint ring (alpha=80) at the cursor — soft "you're close" indicator
- If `LastSnap` has no value (cursor far from any snap points): block is skipped entirely — no visual noise

The block returns early after drawing so the `AnchorPlaced` path (anchor dot, ghost line/arc, mid-draw snap ring) never runs for `Idle` state. The existing `AnchorPlaced` render block is completely unchanged.

A deviation fix was required: the local variable was named `ids` (not `ds`) to avoid CS0136 — C# disallows two locals with the same name in the same method scope even when in separate `if` blocks. The outer `AnchorPlaced` block already declares `float ds`; the new Idle block uses `float ids` (idle dpi-scale).

## Tasks Completed

| Task | Description | Commit |
|------|-------------|--------|
| 1 | Widen right rail to 180px; scale tool icons 24→32px; increase font sizes | e80a12c |
| 2 | Fix snap ring not rendering in Idle state before first click (Gap 8) | 3bf4b26 |

## Verification

- `grep Width="148" MainWindow.xaml RightRail.xaml` — 0 matches
- `grep -c Width="180" MainWindow.xaml` — 1 match (ColumnDefinition)
- `grep -c Width="180" RightRail.xaml` — 1 match (UserControl)
- `grep -c Width="32" Height="32" ToolRail.xaml` — 6 matches (one per tool button Viewbox)
- `grep -c FontSize="13" ToolRail.xaml` — 6 matches (one per tool TextBlock)
- `grep -n DrawState.Idle PdfCanvasViewModel.cs` — 1 match inside DrawGhostPreview (new Idle-snap block)
- `grep -n idleSnapPaint PdfCanvasViewModel.cs` — 2 matches (declaration + DrawCircle call)
- Build: 0 errors, 9 NU1701 warnings (expected/known)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] CS0136: local variable name clash — renamed `ds` to `ids` in Idle snap block**
- **Found during:** Task 2 — first build attempt
- **Issue:** C# CS0136 — `float ds` declared in the new Idle block and `float ds` in the existing AnchorPlaced block are both in the scope of `DrawGhostPreview`. C# does not allow two locals with the same name in overlapping scopes even when in separate `if` branches at the same level.
- **Fix:** Renamed `ds` to `ids` (idle dpi-scale) throughout the new Idle snap block. Semantically identical — same computation `(float)(_dpiScale * _mainVm.ZoomFactor)`.
- **Files modified:** `MathGaze/ViewModels/PdfCanvasViewModel.cs`
- **Commit:** 3bf4b26 (included in Task 2 commit after fix)

## Known Stubs

None.

## Threat Flags

No new network endpoints, auth paths, file access patterns, or schema changes introduced. All changes are XAML layout and an in-process SkiaSharp render path — no trust boundary surface added. T-07-07-01 (Idle snap block O(1) cost, no allocation when LastSnap is null) as documented in plan threat model — accepted.

## Self-Check: PASSED

- `MathGaze/MainWindow.xaml` — modified, Width="180" on ColumnDefinition confirmed
- `MathGaze/Views/RightRail.xaml` — modified, Width="180" on UserControl confirmed; 5 TextBlock font sizes increased
- `MathGaze/Views/ToolRail.xaml` — modified, 6 Viewboxes at 32x32 and 6 TextBlocks at FontSize="13" confirmed
- `MathGaze/ViewModels/PdfCanvasViewModel.cs` — modified, Idle snap ring block with DrawState.Idle and idleSnapPaint confirmed
- Commit e80a12c — verified in git log
- Commit 3bf4b26 — verified in git log
