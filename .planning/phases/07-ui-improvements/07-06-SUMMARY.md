---
phase: 07-ui-improvements
plan: 06
subsystem: ui-styles
tags: [theme-consistency, settings-auto-close, object-list, wpf-mvvm, brush-unification]
dependency_graph:
  requires: [07-05]
  provides: [unified-rail-background, settings-auto-close-on-tool-change, clean-object-list-rows]
  affects: [ToolRail.xaml, MainViewModel.cs, RightRail.xaml]
tech_stack:
  added: []
  patterns: [PropertyChanged-subscription-for-cross-vm-reaction, DynamicResource-BrushSurface-unified]
key_files:
  modified:
    - MathGaze/Views/ToolRail.xaml
    - MathGaze/ViewModels/MainViewModel.cs
    - MathGaze/Views/RightRail.xaml
decisions:
  - "All four panel backgrounds (TopBar, ToolRail, RightRail, ScrollRail) now use BrushSurface; BrushSurface2 retained for inner content chips/file-chip borders only"
  - "Settings auto-close wired via PropertyChanged subscription on ToolViewModel in MainViewModel constructor — direct IsSettingsPanelOpen=false assignment, no generated command invocation needed"
  - "Type chip Border (TypeLabel binding) removed from ObjectList DataTemplate; DisplayName TextBlock gains Margin=4,0 for left padding"
metrics:
  duration: ~8 minutes
  completed: "2026-05-30"
  tasks_completed: 2
  files_modified: 3
---

# Phase 07 Plan 06: Colour Consistency, Settings Auto-Close, Object List Clean Rows Summary

One-liner: Fixed three UAT cosmetic gaps — unified all rail/topbar backgrounds to BrushSurface, wired settings panel to auto-close when a tool is activated, and removed the redundant type chip that caused "Line Line 1" double-labelling in the object list.

## What Was Built

Three targeted UAT enhancements resolving cosmetic and interaction gaps found in Phase 7 testing:

**Fix 1 — Background brush consistency (ToolRail.xaml)**

ToolRail.xaml UserControl `Background` was `{DynamicResource BrushSurface2}` (cream in light mode, slightly darker in dark mode) while TopBar and RightRail used `{DynamicResource BrushSurface}` (pure white / dark panel). Changed ToolRail to `BrushSurface` so all four visible panel backgrounds are identical in both themes.

Audit of all view XAML for hardcoded hex `Background` values:
- `RightRail.xaml` lines 32, 344: `#CC2020` — intentional danger-red on ClearPage and Delete buttons. Kept.
- `PdfCanvas.xaml` line 17: `#CC1A1A2E` — semi-transparent dark toast overlay. Intentional overlay colour, not a surface background. Kept.
- No non-intentional hardcoded hex backgrounds found.

**Fix 2 — Settings auto-close on tool activation (MainViewModel.cs)**

When the settings panel is open and the user clicks a tool button, `ToolViewModel.ActiveTool` changes via `PropertyChanged`. MainViewModel now subscribes to `_toolVm.PropertyChanged` in the constructor and calls `_settingsVm.IsSettingsPanelOpen = false` when `ActiveTool` changes and the settings panel is open. This covers all six tool activations (Select, Point, Line, Circle, Protractor, Text) without any per-command coupling.

The `PropertyChanged` subscription is a singleton-to-singleton subscription for the app lifetime — no unsubscribe needed (no memory leak risk, confirmed in plan threat model T-07-06-01).

**Fix 3 — Object list clean rows (RightRail.xaml)**

The ObjectList `ItemsControl.ItemTemplate` previously contained a type-chip `<Border>` binding to `TypeLabel` (e.g. "Line") followed by a `<TextBlock>` binding to `DisplayName` (e.g. "Line 1"), producing "Line Line 1" in the rendered row. Removed the entire type-chip `<Border>` element. The `<TextBlock Text="{Binding DisplayName}">` is retained with `Margin="4,0"` for left padding. The `TypeLabel` property is not removed from `ObjectListItem` ViewModel (unused properties are harmless and removing them is out of scope).

## Tasks Completed

| Task | Description | Commit |
|------|-------------|--------|
| 1 | Audit and fix background brush consistency across all rail/topbar views | c624332 |
| 2 | Wire settings auto-close on tool activation; remove type chip from object list rows | ccefa8a |

## Verification

- `grep -n "BrushSurface2" MathGaze/Views/ToolRail.xaml` — 0 matches (UserControl Background now BrushSurface)
- `grep -rn "Background=\"#" MathGaze/Views/*.xaml` — 2 matches in RightRail.xaml (#CC2020 danger-red, intentional) + 1 in PdfCanvas.xaml (#CC1A1A2E toast overlay, intentional)
- `grep -n "TypeLabel" MathGaze/Views/RightRail.xaml` — 0 matches
- `grep -n "OnToolPropertyChanged" MathGaze/ViewModels/MainViewModel.cs` — 2 matches (subscription + handler)
- `grep -n "IsSettingsPanelOpen = false" MathGaze/ViewModels/MainViewModel.cs` — 1 match
- Build: 0 errors, 9 NU1701 warnings (expected/known)

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None.

## Threat Flags

No new network endpoints, auth paths, file access patterns, or schema changes introduced. The `PropertyChanged` subscription is an in-process singleton event — no trust boundary surface added (T-07-06-01: accepted, documented in plan threat model).

## Self-Check: PASSED

- `MathGaze/Views/ToolRail.xaml` — modified, BrushSurface confirmed on UserControl Background
- `MathGaze/ViewModels/MainViewModel.cs` — modified, OnToolPropertyChanged handler with IsSettingsPanelOpen=false confirmed
- `MathGaze/Views/RightRail.xaml` — modified, type chip Border removed, DisplayName TextBlock with Margin="4,0" confirmed
- Commit c624332 — verified in git log
- Commit ccefa8a — verified in git log
