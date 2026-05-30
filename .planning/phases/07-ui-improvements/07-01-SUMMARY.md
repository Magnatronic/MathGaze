---
phase: 07-ui-improvements
plan: 01
subsystem: ui-shell
tags: [gaze-targets, accessibility, dimensions, dynamic-resource, theming]
dependency_graph:
  requires: []
  provides: [gaze-compliant-button-sizes, dynamic-resource-brush-keys]
  affects: [TopBar, ToolRail, ScrollRail, RightRail, AppStyles, MainWindow]
tech_stack:
  added: []
  patterns: [DynamicResource for theme-swappable brush keys, icon-above-label StackPanel Vertical orientation]
key_files:
  created: []
  modified:
    - MathGaze/Views/TopBar.xaml
    - MathGaze/Views/ScrollRail.xaml
    - MathGaze/Views/ToolRail.xaml
    - MathGaze/Views/RightRail.xaml
    - MathGaze/Styles/AppStyles.xaml
    - MathGaze/MainWindow.xaml
decisions:
  - "ToolTileStyle style-level setters (BorderBrush, Foreground) intentionally kept as StaticResource per plan — only ControlTemplate trigger brush refs need DynamicResource for theme swap"
  - "IconButtonStyle style-level setters kept as StaticResource for same reason — element-level Width/Height overrides apply at creation time"
metrics:
  duration_minutes: 15
  completed_date: "2026-05-30"
  tasks_completed: 2
  files_modified: 6
---

# Phase 7 Plan 01: Gaze Target Sizing and DynamicResource Brush Conversion Summary

All gaze-interactive buttons in the app shell resized to meet the >=56x56px floor; ToolRail redesigned to 84x84px icon-above-label layout; all theme-sensitive brush references inside ControlTemplate triggers and view XAML converted from StaticResource to DynamicResource.

## Tasks Completed

### Task 1: Resize TopBar buttons to 56x56px and ScrollRail to 56x56px / 64px wide

**Commit:** f816321

**Files:** `MathGaze/Views/TopBar.xaml`, `MathGaze/Views/ScrollRail.xaml`, `MathGaze/MainWindow.xaml`

Changes made:
- `TopBar.xaml` UserControl `Height`: 60 → 72px (56px buttons + 8px top/bottom padding)
- `OpenButton`: 36x36 → 56x56px
- `CloseButton`: 36x36 → 56x56px
- Settings gear `Button`: 40x40 → 56x56px (DockPanel.Dock="Right" preserved)
- Zoom-out, Zoom-in, Fit-page buttons: 32x32 → 56x56px
- Prev-page, Next-page buttons: 36x32 → 56x56px
- PDF Export button: already 56x56 — no change
- All `StaticResource BrushXxx` refs in TopBar.xaml converted to `DynamicResource`
- `ScrollRail.xaml` UserControl `Width`: 38 → 64px
- All 4 scroll buttons `Width`: 30 → 56px (Height was already 56)
- All `StaticResource BrushXxx` refs in ScrollRail.xaml converted to `DynamicResource`
- `MainWindow.xaml` TopBar `RowDefinition Height`: 60 → 72
- `MainWindow.xaml` ToolRail `ColumnDefinition Width`: 104 → 108
- `MainWindow.xaml` `Background` converted to `DynamicResource BrushBg`

### Task 2: Redesign ToolTileStyle and ToolRail to 84x84px icon-above-label; convert brush refs to DynamicResource

**Commit:** 2511029

**Files:** `MathGaze/Styles/AppStyles.xaml`, `MathGaze/Views/ToolRail.xaml`, `MathGaze/Views/RightRail.xaml`

Changes made:

**AppStyles.xaml:**
- `ToolTileStyle` `Height` setter: 56 → 84 (square 84x84 gaze target)
- `ToolTileStyle` `ContentPresenter`: `HorizontalAlignment` Left → Center; `Margin` "12,0,0,0" → "0" (icon-above-label needs centred, no left offset)
- All `ControlTemplate.Triggers` brush refs converted to `DynamicResource`: `BrushSurface2`, `BrushAccentSoft`, `BrushAccent` in `ToolTileStyle`
- `IconButtonStyle` ControlTemplate triggers: `BrushSurface2`, `BrushAccentSoft` → DynamicResource
- `RailButtonStyle` style-level setters: `BrushSurface`, `BrushBorder`, `BrushInk` → DynamicResource; ControlTemplate triggers: `BrushSurface2`, `BrushAccentSoft`, `BrushAccent` → DynamicResource
- `StepButtonStyle` style-level setters: `BrushSurface`, `BrushBorder`, `BrushInk` → DynamicResource; all ControlTemplate trigger brush refs → DynamicResource
- `DeleteButtonStyle` hardcoded hex colours (`#CC2020`, `#991818`, `#660000`) unchanged — not brush keys

**ToolRail.xaml:**
- UserControl `Width`: 104 → 108px
- All 6 tool button `StackPanel Orientation`: Horizontal → Vertical
- `Viewbox` `HorizontalAlignment="Center"` added (was using left-aligned horizontal flow)
- `TextBlock` `VerticalAlignment="Center"` → `HorizontalAlignment="Center"`
- Protractor `TextBlock` `FontSize="11"` override removed (fits at default 12px at 84px width)
- All `StaticResource BrushXxx` refs converted to `DynamicResource`
- Active-state `DataTrigger` setters: `BrushAccentSoft`, `BrushAccent` → DynamicResource
- "TOOLS" header `BrushAccentInk` → DynamicResource

**RightRail.xaml:**
- All `StaticResource BrushXxx` refs converted to `DynamicResource`: `BrushSurface`, `BrushBorder`, `BrushInk`, `BrushInk2`, `BrushInk3`
- `Background` attribute → DynamicResource BrushSurface

## Build Status

`dotnet build MathGaze/MathGaze.csproj` — **Build succeeded. 0 errors, 6 warnings (all pre-existing NU1701 package compatibility warnings unrelated to this plan).**

## DynamicResource Conversion Scope

| File | Refs converted |
|------|---------------|
| AppStyles.xaml | All ControlTemplate trigger brush refs; RailButtonStyle and StepButtonStyle style-level setters |
| ToolRail.xaml | All BrushXxx refs (icons, labels, active DataTriggers, TOOLS header) |
| TopBar.xaml | All BrushXxx refs (backgrounds, borders, icon strokes, text) |
| ScrollRail.xaml | All BrushXxx refs (backgrounds, borders, icon strokes) |
| RightRail.xaml | All BrushXxx refs (backgrounds, borders, labels) |
| MainWindow.xaml | Background BrushBg |

**Intentionally kept as StaticResource:** `ToolTileStyle` and `IconButtonStyle` style-level setters for `BorderBrush` and `Foreground` — per plan note, these are applied once at element creation and DataTrigger overrides handle active state. `BrushAccent` and `BrushTransparent` are identical across themes and can remain StaticResource.

## Deviations from Plan

None — plan executed exactly as written. All acceptance criteria met.

## Known Stubs

None. All changes are dimension and style edits with no data or display content.

## Threat Flags

None. All changes are pure XAML dimension and style edits. No new network endpoints, auth paths, file access patterns, or schema changes introduced.

## Self-Check: PASSED

- MathGaze/Views/TopBar.xaml — FOUND, Height="72", OpenButton/CloseButton/Settings/Zoom/Page buttons all Width="56" Height="56"
- MathGaze/Views/ScrollRail.xaml — FOUND, Width="64", 4x Width="56" buttons
- MathGaze/Views/ToolRail.xaml — FOUND, Width="108", 6x Orientation="Vertical", no FontSize="11"
- MathGaze/Styles/AppStyles.xaml — FOUND, ToolTileStyle Height Value="84", all ControlTemplate triggers DynamicResource
- MathGaze/Views/RightRail.xaml — FOUND, all BrushXxx DynamicResource
- MathGaze/MainWindow.xaml — FOUND, RowDefinition Height="72", ColumnDefinition Width="108"
- Commit f816321 — FOUND
- Commit 2511029 — FOUND
- Build: 0 errors — CONFIRMED
