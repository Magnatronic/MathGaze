---
status: partial
phase: 07-ui-improvements
source: [07-VERIFICATION.md]
started: 2026-05-30T00:00:00Z
updated: 2026-05-30T15:37:18Z
---

## Current Test

[awaiting human re-verification after gap closure — plans 07-05, 07-06, 07-07 executed]

## Tests

### 1. Build passes with 0 errors
expected: `dotnet build MathGaze/MathGaze.csproj` exits 0 errors
result: passed — app launched successfully (original UAT); all gap closure plans confirm 0 errors

### 2. Theme toggle — visual swap (originally passed)
expected: Click gear button → settings panel opens. Click Dark → entire UI shell changes to dark colours immediately without restart.
result: passed (original UAT)

### 3. Theme persistence across restarts (originally passed)
expected: With Dark selected, close and relaunch the app — dark theme applies on startup.
result: passed (original UAT)

### 4. Drawing guide panel switching (originally passed)
expected: Activate Line tool, click first point → right rail shows hint card with Cancel button.
result: passed (original UAT)

### 5. Object list population and tap-to-select (originally passed)
expected: Right rail shows object list rows; tapping selects.
result: passed (original UAT — display names still need re-check after Gap 6 fix)

### 6. Gap 1 re-check — Button borders update on theme switch
expected: After switching Dark → Light and Light → Dark, all tool rail button borders and foreground text update to the correct theme colour — no stuck white/black borders (4 style-level setters in AppStyles.xaml converted to DynamicResource)
result: [pending]

### 7. Gap 2 re-check — No scrollbar ellipse in dark mode
expected: No WPF native scrollbar ellipse thumb visible anywhere in the app in dark mode with object list populated
result: [pending]

### 8. Gap 3 re-check — TopBar and ToolRail same shade
expected: TopBar and ToolRail appear the same background colour in both themes (both now BrushSurface)
result: [pending]

### 9. Gap 4 re-check — Settings auto-closes on tool activation
expected: With settings panel open, clicking any tool button closes the settings panel automatically
result: [pending]

### 10. Gap 5 re-check — Right rail 180px
expected: Right rail visibly wider; protractor controls and object list have adequate breathing room
result: [pending]

### 11. Gap 6 re-check — Object list shows clean single names
expected: Object list rows show "Line 1" only — no "Line Line 1" duplication
result: [pending]

### 12. Gap 7 re-check — Tool icons 32x32, labels 13pt
expected: All 6 tool icons visibly larger; tool labels clearly legible
result: [pending]

### 13. Gap 8 re-check — Idle snap ring before first click
expected: With Line or Circle tool active, hover near an existing geometry endpoint → dashed cobalt ring + dot appears at snap point BEFORE first click; hover over empty canvas → faint ring follows cursor
result: [pending]

## Summary

total: 13
passed: 5
issues: 0
pending: 8
skipped: 0
blocked: 0

## Gaps

### Gap 1 — Button border colours don't update on theme switch (blocking)
status: resolved
description: ToolTileStyle, IconButtonStyle style-level setters for BorderBrush and Foreground use StaticResource (locked to light-theme value). In dark mode buttons show white/light borders. After switching back to light, buttons show black borders. Fix: convert ALL style-level setters in ALL button styles to DynamicResource.
debug_session:

### Gap 2 — WPF ScrollBar thumb renders as ellipse (blocking)
status: resolved
description: The PDF canvas ScrollViewer uses WPF's default ScrollBar template which has an Ellipse-shaped thumb. In dark mode this renders as a large dark oval/teardrop dominating the canvas. Fix: apply a custom ScrollBar template with a rectangular thumb, or hide the native scrollbar and use only the custom scroll rail buttons.
debug_session:

### Gap 3 — Colour inconsistency between TopBar and rails (minor)
status: resolved
description: TopBar background appears a different shade from ToolRail/ScrollRail in some configurations. Some Background attributes may be using different brush keys or hardcoded values rather than consistent BrushSurface/BrushBg DynamicResource references.
debug_session:

### Gap 4 — Settings panel doesn't close when tool is clicked (enhancement)
status: resolved
description: When the settings panel is open and the user activates a tool, the settings panel should auto-close. Currently it stays open. Fix: observe ActiveTool changes in SettingsViewModel (or MainViewModel) and call CloseSettings() when ActiveTool changes while IsSettingsPanelOpen=true.
debug_session:

### Gap 5 — Right rail feels cramped (enhancement)
status: resolved
description: Right rail (currently 148px wide) feels cramped especially with the protractor controls and object list. User would prefer wider rail (~180px) with correspondingly larger content.
debug_session:

### Gap 6 — Object list shows redundant text (enhancement)
status: resolved
description: Each object list row shows a text chip ("Line") + display name ("Line 1"), resulting in "Line Line 1" which is visually redundant. Either remove the chip entirely and show just "Line 1", or replace the text chip with a small canvas-drawn icon.
debug_session:

### Gap 7 — Icons/fonts could be larger (enhancement)
status: resolved
description: The tool rail buttons and right rail panels have available whitespace that could be used for larger icons and text. Student would benefit from bolder, more visible UI elements.
debug_session:

### Gap 8 — Snap ring not visible before first click (enhancement)
status: resolved
description: The plan added HandleMouseMove Idle-state snap ring for Line/Circle, but snap visual feedback before the first click is either not rendering or not prominent enough. Should show the same snap ring as during second-click hover.
