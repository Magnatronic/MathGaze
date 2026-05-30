---
status: partial
phase: 07-ui-improvements
source: [07-VERIFICATION.md]
started: 2026-05-30T00:00:00Z
updated: 2026-05-30T00:00:00Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. Build passes with 0 errors
expected: `dotnet build MathGaze/MathGaze.csproj` exits 0 errors
result: [pending]

### 2. Theme toggle — visual swap
expected: Click gear button → settings panel opens. Click Dark → entire UI shell changes to dark colours immediately without restart.
result: [pending]

### 3. Theme persistence across restarts
expected: With Dark selected, close and relaunch the app — dark theme applies on startup without clicking anything.
result: [pending]

### 4. Drawing guide panel switching
expected: Activate Line tool, click first point → right rail shows "Line in progress / Click 2nd point" hint card with Cancel button. Clicking Cancel returns to "NOTHING SELECTED" state.
result: [pending]

### 5. Object list population and tap-to-select
expected: With Select tool active and geometry objects on page, right rail shows object list rows (e.g. "Line 1", "Circle 1"). Tapping a row selects that object and right rail switches to selection controls.
result: [pending]

## Summary

total: 5
passed: 0
issues: 0
pending: 5
skipped: 0
blocked: 0

## Gaps
