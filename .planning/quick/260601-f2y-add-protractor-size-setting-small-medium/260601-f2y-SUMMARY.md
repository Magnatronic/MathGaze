---
quick_id: 260601-f2y
status: complete
commit: 0634b90
date: 2026-06-01
---

# Summary: Protractor Size Setting + Rail Overflow Fix

## What was done

7 changes across 7 files, committed atomically as `0634b90`.

### ProtractorObject — per-instance radius
- Added `RadiusPt` property (default `DefaultRadiusPt = 144.0`) so each protractor
  instance carries its own radius. JSON deserialization falls back to 144pt for
  existing saved sessions — no migration needed.
- `HitTest` now uses `this.RadiusPt` instead of the static constant, so hit detection
  matches the visual size of the protractor.

### UserPreferences — size preference
- Added `ProtractorSize` string field (`"Small"` | `"Medium"` | `"Large"`, default `"Medium"`).
- Added `ProtractorSizeRadiusPt` computed property: Small=80pt, Medium=144pt, Large=200pt.
- Persisted alongside Theme in `%APPDATA%\MathGaze\preferences.json`.

### ToolViewModel — stamp size at placement
- Both protractor creation paths (line-intersection and two-point free-placement) now
  set `protractor.RadiusPt = UserPreferences.ProtractorSizeRadiusPt` before
  `ExecuteCommand`. Size preference affects new protractors only.

### GeometryLayerViewModel — renderer uses per-instance radius
- Proxy-point edge calculation changed from `ProtractorObject.DefaultRadiusPt` to
  `obj.RadiusPt`. Renderer now correctly draws each protractor at its stored size.

### SettingsViewModel — size state + command
- Added `IsProtractorSmall`, `IsProtractorMedium`, `IsProtractorLarge` observable booleans.
- Added `SetProtractorSizeCommand(string size)` that syncs the booleans, updates
  `UserPreferences.ProtractorSize`, and calls `Save()`.
- `SyncProtractorSize` helper initialises state from preferences on startup.

### MainWindow.xaml — PROTRACTOR SIZE section in settings panel
- Added below THEME: three buttons (Small / Medium / Large) using `StepButtonStyle`
  with `Tag="active"` DataTriggers bound to the three boolean properties.
- All buttons 56px height (gaze floor).

### RightRail.xaml — ScrollViewer wraps main content
- Main content `StackPanel` wrapped in `<ScrollViewer VerticalScrollBarVisibility="Hidden">`.
- Undo/Redo and Clear page remain bottom-docked (always visible).
- Grid 3 sends scroll-wheel / keyboard scroll events; hidden scrollbar is correct
  for gaze — the student doesn't need to click a scrollbar thumb.
- Delete button now always reachable by scrolling.

## Decisions confirmed
- Size preference is global; changing it affects new protractors only (user decision).
- Existing saved protractors use DefaultRadiusPt (144pt) via JSON default — no migration.
- Small=80pt was chosen as "fits tight angles" based on ~107px screen size at zoom=1.
