---
status: diagnosed
phase: 07-ui-improvements
source: [07-VERIFICATION.md]
started: 2026-05-30T00:00:00Z
updated: 2026-05-30T00:00:00Z
---

## Current Test

Human UAT completed 2026-05-30.

## Tests

### 1. Build passes with 0 errors
expected: `dotnet build MathGaze/MathGaze.csproj` exits 0 errors
result: passed — app launched successfully

### 2. Theme toggle — visual swap
expected: Click gear button → settings panel opens. Click Dark → entire UI shell changes to dark colours immediately without restart.
result: failed — app does switch to dark mode, but buttons show thick white borders in dark mode (StaticResource BrushBorder/BrushInk in ToolTileStyle style-level setters not updating). Switching back to light mode leaves solid black borders on buttons.

### 3. Theme persistence across restarts
expected: With Dark selected, close and relaunch the app — dark theme applies on startup.
result: passed

### 4. Drawing guide panel switching
expected: Activate Line tool, click first point → right rail shows hint card with Cancel button.
result: passed

### 5. Object list population and tap-to-select
expected: Right rail shows object list rows; tapping selects.
result: passed — but list rows show redundant text: type chip says "Line" and display name says "Line 1", giving "Line Line 1" appearance. User prefers icon-only chips or just the display name.

## Summary

total: 5
passed: 3
issues: 2
pending: 0
skipped: 0
blocked: 0

## Gaps

### Gap 1 — Button border colours don't update on theme switch (blocking)
status: failed
description: ToolTileStyle, IconButtonStyle style-level setters for BorderBrush and Foreground use StaticResource (locked to light-theme value). In dark mode buttons show white/light borders. After switching back to light, buttons show black borders. Fix: convert ALL style-level setters in ALL button styles to DynamicResource.
debug_session:

### Gap 2 — WPF ScrollBar thumb renders as ellipse (blocking)
status: failed
description: The PDF canvas ScrollViewer uses WPF's default ScrollBar template which has an Ellipse-shaped thumb. In dark mode this renders as a large dark oval/teardrop dominating the canvas. Fix: apply a custom ScrollBar template with a rectangular thumb, or hide the native scrollbar and use only the custom scroll rail buttons.
debug_session:

### Gap 3 — Colour inconsistency between TopBar and rails (minor)
status: failed
description: TopBar background appears a different shade from ToolRail/ScrollRail in some configurations. Some Background attributes may be using different brush keys or hardcoded values rather than consistent BrushSurface/BrushBg DynamicResource references.
debug_session:

### Gap 4 — Settings panel doesn't close when tool is clicked (enhancement)
status: failed
description: When the settings panel is open and the user activates a tool, the settings panel should auto-close. Currently it stays open. Fix: observe ActiveTool changes in SettingsViewModel (or MainViewModel) and call CloseSettings() when ActiveTool changes while IsSettingsPanelOpen=true.
debug_session:

### Gap 5 — Right rail feels cramped (enhancement)
status: failed
description: Right rail (currently 148px wide) feels cramped especially with the protractor controls and object list. User would prefer wider rail (~180px) with correspondingly larger content.
debug_session:

### Gap 6 — Object list shows redundant text (enhancement)
status: failed
description: Each object list row shows a text chip ("Line") + display name ("Line 1"), resulting in "Line Line 1" which is visually redundant. Either remove the chip entirely and show just "Line 1", or replace the text chip with a small canvas-drawn icon.
debug_session:

### Gap 7 — Icons/fonts could be larger (enhancement)
status: failed
description: The tool rail buttons and right rail panels have available whitespace that could be used for larger icons and text. Student would benefit from bolder, more visible UI elements.
debug_session:

### Gap 8 — Snap ring not visible before first click (enhancement)
status: failed
description: The plan added HandleMouseMove Idle-state snap ring for Line/Circle, but snap visual feedback before the first click is either not rendering or not prominent enough. Should show the same snap ring as during second-click hover.
