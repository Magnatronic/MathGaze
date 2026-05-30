---
phase: 07-ui-improvements
plan: 02
subsystem: ui-shell
tags: [theming, dark-mode, settings-panel, dynamic-resource, persistence, gaze-targets]
dependency_graph:
  requires: [07-01]
  provides: [light-dark-theme-system, settings-panel, theme-persistence]
  affects: [AppStyles, App.xaml, App.xaml.cs, MainViewModel, SettingsViewModel, TopBar, MainWindow]
tech_stack:
  added: [UserPreferences JSON persistence (System.Text.Json, %APPDATA%\MathGaze\preferences.json)]
  patterns:
    - ResourceDictionary swap at MergedDictionaries[0] for live theme toggle
    - UserPreferences static class (JSON) replaces ApplicationSettingsBase (unavailable in .NET 9 SDK-style WPF without extra NuGet)
    - In-window overlay Border (Grid.Column=2, Panel.ZIndex=10) for Grid 3 compatible settings panel
    - StepButtonStyle Tag="active" DataTrigger pattern for theme toggle active state
key_files:
  created:
    - MathGaze/Styles/Themes/Light.xaml
    - MathGaze/Styles/Themes/Dark.xaml
    - MathGaze/Properties/Settings.settings
    - MathGaze/Properties/UserPreferences.cs
    - MathGaze/ViewModels/SettingsViewModel.cs
  modified:
    - MathGaze/Styles/AppStyles.xaml
    - MathGaze/App.xaml
    - MathGaze/App.xaml.cs
    - MathGaze/ViewModels/MainViewModel.cs
    - MathGaze/Views/TopBar.xaml
    - MathGaze/MainWindow.xaml
decisions:
  - "Used UserPreferences.cs (JSON in %APPDATA%\\MathGaze\\) instead of Properties.Settings.Default — ApplicationSettingsBase requires System.Configuration.ConfigurationManager NuGet + MSBuild codegen not wired in this .NET 9 SDK-style project"
  - "Settings.settings file created as schema documentation artifact only; runtime persistence uses UserPreferences.cs"
  - "SettingsViewModel created in Task 1 commit (not Task 2) to unblock App.xaml.cs compilation — cross-task dependency resolved by early creation"
metrics:
  duration_minutes: 20
  completed_date: "2026-05-30"
  tasks_completed: 2
  files_modified: 11
---

# Phase 7 Plan 02: Theme System (Light/Dark) and Settings Panel Summary

ResourceDictionary-based light/dark theme system with runtime swap at MergedDictionaries[0]; gear button opens an in-window settings panel (no Popup, no secondary HWND) with 56px toggle buttons; theme preference persisted to JSON in %APPDATA%\MathGaze\.

## Tasks Completed

### Task 1: Extract brush keys into Light.xaml and Dark.xaml; restructure App.xaml

**Commit:** 4e3016e

**Files:** `MathGaze/Styles/Themes/Light.xaml`, `MathGaze/Styles/Themes/Dark.xaml`, `MathGaze/Styles/AppStyles.xaml`, `MathGaze/App.xaml`, `MathGaze/App.xaml.cs`, `MathGaze/Properties/Settings.settings`, `MathGaze/Properties/UserPreferences.cs`, `MathGaze/ViewModels/SettingsViewModel.cs`

Changes made:
- Created `Styles/Themes/Light.xaml` with all 11 brush keys (BrushBg #F5F3EE through BrushTransparent)
- Created `Styles/Themes/Dark.xaml` with all 11 dark equivalents (BrushBg #1A1C22; cobalt accent unchanged per D-07)
- Removed all 11 `SolidColorBrush` definitions from `AppStyles.xaml` — now 0 occurrences
- Restructured `App.xaml` MergedDictionaries: Light.xaml at index 0 (swappable), AppStyles.xaml at index 1
- Added `ApplyTheme(bool isDark)` to `App` class — swaps `MergedDictionaries[0]` with a new `ResourceDictionary`
- Applied saved theme on startup before host build: `if (savedDark) ApplyTheme(true)`
- Created `Properties/UserPreferences.cs`: static class using `System.Text.Json` to persist `Theme` key to `%APPDATA%\MathGaze\preferences.json`
- Created `Properties/Settings.settings`: schema documentation only (see deviation note)
- Registered `SettingsViewModel` in DI: `services.AddSingleton<SettingsViewModel>()`
- Created `ViewModels/SettingsViewModel.cs` (early, to unblock Task 1 build — see deviation)

### Task 2: Create SettingsViewModel, expose via MainViewModel, wire settings panel

**Commit:** ccf7080

**Files:** `MathGaze/ViewModels/MainViewModel.cs`, `MathGaze/Views/TopBar.xaml`, `MathGaze/MainWindow.xaml`

Changes made:
- Added `SettingsViewModel settingsViewModel` constructor parameter to `MainViewModel`
- Added `private readonly SettingsViewModel _settingsVm` field and `public SettingsViewModel SettingsVm => _settingsVm` property
- Wired gear button in `TopBar.xaml`: `Command="{Binding SettingsVm.OpenSettingsCommand}"`, `ToolTip="Settings"`
- Added `SettingsPanelOverlay` Border to `MainWindow.xaml` Grid.Column=2, Panel.ZIndex=10:
  - Visibility bound to `SettingsVm.IsSettingsPanelOpen` via BoolToVisibilityConverter
  - Header DockPanel: SETTINGS label + 56x56 close (X) button using RailButtonStyle
  - THEME label + UniformGrid with Light (56px) and Dark (56px) buttons
  - Active theme button highlighted via `StepButtonStyle` DataTrigger on `IsDarkMode`
  - No Popup, no secondary HWND — fully Grid 3 compatible

## Build Status

`dotnet build MathGaze/MathGaze.csproj` — **Build succeeded. 0 errors, 9 warnings (all pre-existing NU1701 package compatibility warnings).**

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] SettingsViewModel created in Task 1 commit to unblock compilation**

- **Found during:** Task 1 build verification
- **Issue:** `App.xaml.cs` registers `services.AddSingleton<SettingsViewModel>()` which caused CS0246 ("type not found") since SettingsViewModel didn't exist yet
- **Fix:** Created `SettingsViewModel.cs` as part of the Task 1 commit so the build passes. The file content matches what Task 2 would have created.
- **Files modified:** `MathGaze/ViewModels/SettingsViewModel.cs`
- **Commit:** 4e3016e (included in Task 1 commit)

**2. [Rule 2 - Missing functionality] UserPreferences.cs replaces Settings.settings runtime use**

- **Found during:** Task 1 implementation
- **Issue:** `Properties.Settings.Default` (ApplicationSettingsBase) is not available in .NET 9 SDK-style WPF projects without adding `System.Configuration.ConfigurationManager` NuGet package and MSBuild codegen integration. The project has no Properties folder and no such NuGet reference.
- **Fix:** Created `Properties/UserPreferences.cs` — a static class using `System.Text.Json` (inbox in .NET 9) to persist theme preference to `%APPDATA%\MathGaze\preferences.json`. `Settings.settings` kept as schema documentation artifact. All references in `SettingsViewModel` and `App.xaml.cs` use `UserPreferences` instead of `Properties.Settings.Default`.
- **Files modified:** `MathGaze/Properties/UserPreferences.cs` (new), `MathGaze/Properties/Settings.settings` (doc only)
- **Commit:** 4e3016e

## Known Stubs

None. All theme toggle functionality is fully wired: clicking gear opens panel, clicking Light/Dark swaps theme immediately, preference is saved and restored on next launch.

## Threat Flags

None. The only new data flow is `UserPreferences.Theme` written to `%APPDATA%\MathGaze\preferences.json`. The value is always `"Light"` or `"Dark"` (hardcoded XAML CommandParameter literals). `ApplyTheme` uses `== "Dark"` check so any tampered value defaults to Light. No new network endpoints, auth paths, or trust boundary crossings.

## Self-Check: PASSED

- `MathGaze/Styles/Themes/Light.xaml` — FOUND, BrushBg Color="#F5F3EE" CONFIRMED
- `MathGaze/Styles/Themes/Dark.xaml` — FOUND, BrushBg Color="#1A1C22" CONFIRMED
- `MathGaze/Styles/AppStyles.xaml` — FOUND, 0 SolidColorBrush occurrences CONFIRMED
- `MathGaze/App.xaml` — FOUND, Source="Styles/Themes/Light.xaml" at index 0 CONFIRMED
- `MathGaze/App.xaml.cs` — FOUND, ApplyTheme method CONFIRMED, UserPreferences CONFIRMED, AddSingleton<SettingsViewModel> CONFIRMED
- `MathGaze/Properties/Settings.settings` — FOUND
- `MathGaze/Properties/UserPreferences.cs` — FOUND
- `MathGaze/ViewModels/SettingsViewModel.cs` — FOUND, SetThemeCommand CONFIRMED, UserPreferences.Save() CONFIRMED
- `MathGaze/ViewModels/MainViewModel.cs` — FOUND, public SettingsViewModel SettingsVm CONFIRMED
- `MathGaze/Views/TopBar.xaml` — FOUND, SettingsVm.OpenSettingsCommand binding CONFIRMED
- `MathGaze/MainWindow.xaml` — FOUND, SettingsPanelOverlay CONFIRMED, IsSettingsPanelOpen CONFIRMED, Light/Dark buttons CONFIRMED
- Commit 4e3016e — FOUND
- Commit ccf7080 — FOUND
- Build: 0 errors — CONFIRMED
