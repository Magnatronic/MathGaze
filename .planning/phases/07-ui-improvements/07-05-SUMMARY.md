---
phase: 07-ui-improvements
plan: 05
subsystem: ui-styles
tags: [theme-switching, dynamic-resource, scrollbar, wpf-styles, dark-mode]
dependency_graph:
  requires: []
  provides: [theme-switching-border-foreground, no-native-scrollbar-ellipse]
  affects: [AppStyles.xaml, RightRail.xaml]
tech_stack:
  added: []
  patterns: [DynamicResource-in-style-setter, ScrollViewer-with-hidden-bars]
key_files:
  modified:
    - MathGaze/Styles/AppStyles.xaml
    - MathGaze/Views/RightRail.xaml
decisions:
  - "ToolTileStyle and IconButtonStyle style-level BorderBrush/Foreground setters must use DynamicResource so WPF re-resolves brush on ResourceDictionary swap (ApplyTheme)"
  - "ObjectList ItemsControl wrapped in ScrollViewer with VerticalScrollBarVisibility=Hidden; MaxHeight=260 prevents layout overflow; native scrollbar chrome eliminated"
  - "PdfCanvas.xaml has no ScrollViewer — no change needed; SKElement fills the grid directly"
metrics:
  duration: ~10 minutes
  completed: "2026-05-30"
  tasks_completed: 2
  files_modified: 2
---

# Phase 07 Plan 05: Theme DynamicResource and Scrollbar Fix Summary

One-liner: Fixed theme-swap broken button borders/foreground by converting 4 StaticResource setters to DynamicResource, and eliminated native WPF scrollbar ellipse thumb from object list by wrapping ItemsControl in a hidden-bar ScrollViewer.

## What Was Built

Two blocking UAT failures for Phase 7 resolved:

**Fix 1 — DynamicResource in style-level setters (AppStyles.xaml)**

WPF style-level `<Setter>` blocks that use `StaticResource` bind the brush object at element creation time. When `ApplyTheme` swaps the `ResourceDictionary`, elements with `StaticResource` keep the old brush and never re-resolve. Only `DynamicResource` creates a live binding that re-resolves on dictionary changes.

Four setters changed in AppStyles.xaml:
- `ToolTileStyle` BorderBrush: `{StaticResource BrushBorder}` → `{DynamicResource BrushBorder}`
- `ToolTileStyle` Foreground: `{StaticResource BrushInk}` → `{DynamicResource BrushInk}`
- `IconButtonStyle` BorderBrush: `{StaticResource BrushBorder}` → `{DynamicResource BrushBorder}`
- `IconButtonStyle` Foreground: `{StaticResource BrushInk2}` → `{DynamicResource BrushInk2}`

Not changed: `RailButtonStyle`, `StepButtonStyle` (already DynamicResource), `BrushAccent`, `BrushTransparent` (identical in both themes — StaticResource fine), hardcoded hex colours in `DeleteButtonStyle`.

**Fix 2 — Suppress native scrollbar ellipse (RightRail.xaml)**

WPF's default `ItemsControl` template embeds a `ScrollViewer` whose default `ScrollBar` ControlTemplate renders the thumb as an `Ellipse`. In dark mode this appears as a large dark blob. The fix wraps the ObjectList `ItemsControl` in an explicit `ScrollViewer` with:
- `VerticalScrollBarVisibility="Hidden"` — hides native scrollbar chrome entirely
- `HorizontalScrollBarVisibility="Disabled"` — no horizontal overflow expected
- `MaxHeight="260"` — prevents object list from pushing ClearPage/Undo buttons off-screen (fits ~5 objects at 48px each + margins)
- `CanContentScroll="False"` — smooth pixel-based mouse-wheel scrolling

`PdfCanvas.xaml` confirmed to have no `ScrollViewer` element — the UAT-reported ellipse originated from the object list panel only.

## Tasks Completed

| Task | Description | Commit |
|------|-------------|--------|
| 1 | Convert ToolTileStyle and IconButtonStyle style-level setters to DynamicResource | 65d4d17 |
| 2 | Suppress native scrollbar in object list by wrapping ItemsControl in ScrollViewer | ac3ffc2 |

## Verification

- `grep "StaticResource Brush" MathGaze/Styles/AppStyles.xaml` — 0 matches (no remaining non-accent/transparent StaticResource brush refs in style setters)
- `grep "DynamicResource BrushBorder" MathGaze/Styles/AppStyles.xaml` — 4 matches (2 new + 2 existing in RailButtonStyle/StepButtonStyle)
- `grep "VerticalScrollBarVisibility" MathGaze/Views/RightRail.xaml` — 1 match ("Hidden")
- `grep "MaxHeight" MathGaze/Views/RightRail.xaml` — 1 match ("260")
- Build: 0 errors, 9 NU1701 warnings (expected/known)

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None.

## Threat Flags

No new network endpoints, auth paths, file access patterns, or schema changes introduced. Changes are purely XAML style and layout — no trust boundary surface added.

## Self-Check: PASSED

- `MathGaze/Styles/AppStyles.xaml` — modified, confirmed DynamicResource on all 4 target setters
- `MathGaze/Views/RightRail.xaml` — modified, ScrollViewer wrapper with Hidden bars confirmed
- Commit 65d4d17 — verified in git log
- Commit ac3ffc2 — verified in git log
