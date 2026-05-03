---
phase: 02-geometry-core
plan: 07
subsystem: ui-styles
tags: [gap-closure, right-rail, styles, gaze-ux]
one_liner: "RailButtonStyle and StepButtonStyle close GAP-4/GAP-5: right rail now matches app design language with cobalt active-step highlight"

dependency_graph:
  requires:
    - 02-06 (RightRailViewModel with NudgeStepPx, SetStepCommand, nudge commands)
  provides:
    - RailButtonStyle (white surface, BrushBorder, CornerRadius=6, no WPF gray chrome)
    - StepButtonStyle (base=RailButtonStyle, Tag=active triggers BrushAccent background)
  affects:
    - MathGaze/Styles/AppStyles.xaml
    - MathGaze/Views/RightRail.xaml

tech_stack:
  added: []
  patterns:
    - "WPF style BasedOn with inline Button.Style for per-element DataTrigger bindings"
    - "Tag='active' pattern for style-driven active state without ViewModel coupling in style"
    - "WPF property value precedence: inline Background/Foreground on Delete button overrides RailButtonStyle defaults"

key_files:
  created: []
  modified:
    - MathGaze/Styles/AppStyles.xaml
    - MathGaze/Views/RightRail.xaml

decisions:
  - "StepButtonStyle uses Tag='active' (not a direct DataTrigger in the style) because styles cannot bind to ViewModel properties directly — DataTriggers live on each Button element in RightRail.xaml using BasedOn"
  - "Delete button uses RailButtonStyle + inline property overrides (Background=#CC2020, Foreground=White) — WPF property value precedence means element-level values win over style setters, so danger-red coloring is preserved"
  - "MultiTrigger in StepButtonStyle handles hover-while-active (BrushAccentInk) separately from hover-while-inactive (BrushSurface2) to prevent hover from washing out the active cobalt"

metrics:
  duration_minutes: 8
  completed_date: "2026-05-03"
  tasks_completed: 2
  files_modified: 2
---

# Phase 02 Plan 07: Right Rail Style Polish Summary

RailButtonStyle and StepButtonStyle close GAP-4/GAP-5: right rail now matches app design language with cobalt active-step highlight.

## What Was Built

Two new reusable button styles added to `AppStyles.xaml` and applied throughout `RightRail.xaml`:

**RailButtonStyle** — for all right rail action buttons (nudge pad UDLR, Undo, Redo, Delete):
- White surface (BrushSurface), BrushBorder outline, CornerRadius=6
- Hover: BrushSurface2 background
- Pressed: BrushAccentSoft background + BrushAccent border highlight
- Disabled: 0.4 opacity
- FocusVisualStyle=Null (no keyboard focus chrome interfering with gaze display)

**StepButtonStyle** — for the 1px / 5px / 20px step selector buttons:
- Base appearance matches RailButtonStyle
- Active state: `Tag="active"` triggers BrushAccent (#3B6FD4) background + white text
- Hover-while-active: BrushAccentInk (#2A4FA0) for darker feedback without washing out selection
- Tag is set externally via DataTrigger on each Button element in RightRail.xaml

**RightRail.xaml changes:**
- Undo and Redo buttons: `Style="{StaticResource RailButtonStyle}"`
- 4 UDLR directional buttons: `Style="{StaticResource RailButtonStyle}"`, 56×56px preserved
- Delete button: `Style="{StaticResource RailButtonStyle}"` with inline `Background="#CC2020"` and `Foreground="White"` overrides
- Step selector row: each button uses `BasedOn="{StaticResource StepButtonStyle}"` with a `DataTrigger` on `NudgeStepPx` to set `Tag="active"` when its step value matches

## Gaps Closed

| Gap | Description | Status |
|-----|-------------|--------|
| GAP-4 | Right rail buttons used default WPF gray chrome | Closed — all buttons now use white surface + BrushBorder + CornerRadius=6 |
| GAP-5 | Active step size (1px/5px/20px) had no visual distinction | Closed — active step shows BrushAccent cobalt background with white text |

## Deviations from Plan

None — plan executed exactly as written.

## Verification Results

| Check | Result |
|-------|--------|
| `dotnet build` → 0 errors | PASS |
| `grep "x:Key=\"RailButtonStyle\""` → 1 match in AppStyles.xaml | PASS |
| `grep "x:Key=\"StepButtonStyle\""` → 1 match in AppStyles.xaml | PASS |
| `grep -c "CornerRadius=\"6\""` → 3 matches in AppStyles.xaml | PASS (IconButtonStyle + RailButtonStyle + StepButtonStyle) |
| `grep "BrushAccent"` in StepButtonStyle active trigger | PASS |
| `grep -c "RailButtonStyle"` in RightRail.xaml → 7 matches | PASS (≥5 required: Undo + Redo + 4 UDLR + Delete = 7) |
| `grep -c "StepButtonStyle"` in RightRail.xaml → 3 matches | PASS |
| `grep -c "DataTrigger Binding"` → 3 opening tags | PASS |
| `grep -c "NudgeStepPx"` → 3 matches | PASS |
| `grep -c 'Width="56" Height="56"'` → 6 matches | PASS (4 UDLR + Undo + Redo — gaze targets preserved) |

## Known Stubs

None.

## Threat Flags

None — no new network endpoints, auth paths, file access patterns, or schema changes introduced. XAML DataTrigger reads `NudgeStepPx` (int, always 1/5/20) from in-process ViewModel; no security boundary crossed (T-02-07-01 accepted, T-02-07-02 mitigated by build check).

## Self-Check: PASSED

- `MathGaze/Styles/AppStyles.xaml` — FOUND
- `MathGaze/Views/RightRail.xaml` — FOUND
- commit `d377a64` (Task 1: AppStyles.xaml) — FOUND
- commit `d5bec4b` (Task 2: RightRail.xaml) — FOUND
