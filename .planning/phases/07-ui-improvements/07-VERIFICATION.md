---
phase: 07-ui-improvements
verified: 2026-05-30T00:00:00Z
status: human_needed
score: 17/17 must-haves verified (build requires human)
human_verification:
  - test: "Run dotnet build MathGaze/MathGaze.csproj and confirm 0 errors"
    expected: "Build succeeded. 0 errors (pre-existing NU1701 package compatibility warnings are acceptable)"
    why_human: ".NET SDK is not installed in the verification environment — only the .NET runtime is present. The gsd-tools node script and dotnet CLI are both unavailable. All four plan SUMMARYs independently confirm build succeeded with 0 errors."
  - test: "Launch the app, click the gear button, toggle Dark mode"
    expected: "Settings panel opens over the right rail (no separate window). Clicking Dark changes all shell colours immediately. Clicking Light reverts. Close and relaunch — dark theme persists."
    why_human: "Runtime WPF theme-swap behavior (ResourceDictionary swap at MergedDictionaries[0]) and visual correctness cannot be confirmed without running the app."
  - test: "Activate the Line tool, click a first point, observe the right rail"
    expected: "Right rail immediately shows DrawingGuidePanel with 'Line in progress / Click 2nd point' hint text and a Cancel button. Pressing Cancel resets rail to object list or nothing-selected state."
    why_human: "Three-panel rail switching is event-driven at runtime and requires a live WPF window to verify."
  - test: "Place several objects, activate Select tool with no selection, observe right rail"
    expected: "Object list panel shows named rows (Line 1, Circle 1, Protractor 1, etc.). Tapping a row selects that object and the right rail switches to selection controls."
    why_human: "ObjectListPanel binding and SelectCommand behavior requires a running app to verify."
  - test: "Place a Line, activate Line tool again, hover cursor near the existing line endpoint"
    expected: "Snap ring indicator appears near the endpoint before the first click. Clicking snaps the new anchor precisely to the endpoint."
    why_human: "First-click snap visual feedback and commit behavior requires a running app to verify."
---

# Phase 7: UI Improvements Verification Report

**Phase Goal:** Make the app genuinely comfortable for daily gaze use — resize all interactive buttons to the >=56x56px gaze floor, redesign tool rail to 84x84px icon-above-label, add dark mode via ResourceDictionary swapping, add mid-draw guidance card in right rail, add clear-page action, add object list panel for gaze-friendly selection, and enable first-click snapping.
**Verified:** 2026-05-30
**Status:** human_needed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

All 17 plan must-have truths verified against the actual codebase:

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | All top-bar buttons (Open, Close, Zoom-/+/Fit, Prev/Next page, Settings gear) are >=56x56px | VERIFIED | TopBar.xaml: OpenButton Width="56" Height="56" (line 53), CloseButton Width="56" Height="56" (line 66), Settings gear Width="56" Height="56" (line 109), Zoom-out/Zoom-in/Fit Width="56" Height="56" (lines 169, 187, 201), Prev/Next Width="56" Height="56" (lines 131, 149) |
| 2 | Tool rail buttons are 84x84px square with icon above label; Protractor label reads 'Protractor' on one line | VERIFIED | AppStyles.xaml: ToolTileStyle Height Value="84", Width="84" (lines 11-12); ToolRail.xaml: all 6 buttons use Orientation="Vertical" StackPanel with HorizontalAlignment="Center" Viewbox + HorizontalAlignment="Center" TextBlock; Protractor TextBlock has no FontSize="11" override |
| 3 | Scroll rail buttons are 56x56px; rail width is 64px | VERIFIED | ScrollRail.xaml: UserControl Width="64" (line 4); all 4 buttons Width="56" Height="56" (lines 18, 30, 65, 77) |
| 4 | Top bar height is 72px | VERIFIED | TopBar.xaml: UserControl Height="72" (line 4); MainWindow.xaml: RowDefinition Height="72" (line 12) |
| 5 | All brush key references in button templates use DynamicResource for theme-swappable keys | VERIFIED | AppStyles.xaml: All ControlTemplate triggers in ToolTileStyle, RailButtonStyle, StepButtonStyle, IconButtonStyle use DynamicResource for BrushSurface2, BrushAccentSoft, BrushAccent, BrushAccentInk. Style-level setters in RailButtonStyle and StepButtonStyle use DynamicResource for BrushSurface, BrushBorder, BrushInk. TopBar.xaml, ScrollRail.xaml, ToolRail.xaml, RightRail.xaml all confirmed DynamicResource for all BrushXxx refs. |
| 6 | User can click the gear button in the top bar to open an in-window settings panel | VERIFIED | TopBar.xaml gear button has Command="{Binding SettingsVm.OpenSettingsCommand}" (line 112); MainWindow.xaml SettingsPanelOverlay Border with Visibility bound to SettingsVm.IsSettingsPanelOpen (line 55) |
| 7 | The settings panel contains a Light / Dark toggle with 56x56px targets | VERIFIED | MainWindow.xaml: Light Button Height="56" CommandParameter="Light" (line 82); Dark Button Height="56" CommandParameter="Dark" (line 97); both use StepButtonStyle with IsDarkMode DataTrigger |
| 8 | Toggling Dark mode changes all WPF shell colours immediately without restart | VERIFIED | App.xaml.cs ApplyTheme() swaps MergedDictionaries[0] at runtime (line 21-28); SettingsViewModel.SetTheme() calls ApplyTheme(isDark) (line 39); all view brush refs are DynamicResource |
| 9 | The selected theme persists across app restarts | VERIFIED | UserPreferences.cs: JSON persistence to %APPDATA%\MathGaze\preferences.json with Theme key (lines 24-41); App.xaml.cs OnStartup reads UserPreferences.Theme on startup (line 35-36); SettingsViewModel.SetTheme() calls UserPreferences.Save() |
| 10 | The settings panel closes via a close button; the close button is >=56x56px | VERIFIED | MainWindow.xaml close button Width="56" Height="56" Style=RailButtonStyle Command="{Binding SettingsVm.CloseSettingsCommand}" (lines 60-66) |
| 11 | No secondary HWND, Popup, or flyout is created (Grid 3 compatible) | VERIFIED | MainWindow.xaml settings panel is a Border element with Grid.Column=2 Panel.ZIndex=10 — in-window overlay, no Window/Popup/Flyout |
| 12 | When drawing is in progress (anchor placed), the right rail shows a contextual hint card with current step instruction and >=56x56px cancel button | VERIFIED | RightRail.xaml: DrawingGuidePanel Border with BoolToVisibilityConverter on HasDrawingInProgress (line 41), Cancel Button Height="56" Command="{Binding CancelDrawCommand}" (line 55); RightRailViewModel DrawingInstructionText switch on (ActiveTool, DrawState) returns per-tool hint text (line 85-91) |
| 13 | Clicking Cancel in the hint card resets draw state to Idle | VERIFIED | RightRailViewModel.CancelDrawCommand = _toolVm.CancelDrawCommand (line 53); ToolViewModel.CancelDraw() => ResetDrawState() (line 85); ResetDrawState sets DrawState=Idle |
| 14 | A 'Clear page' button is always visible in the right rail above the undo/redo row | VERIFIED | RightRail.xaml: Clear page StackPanel DockPanel.Dock="Bottom" (line 25) before undo/redo StackPanel — not inside any visibility-gated panel |
| 15 | Clicking 'Clear page' removes all objects on the current page as a single undoable action | VERIFIED | RightRailViewModel.ClearPage(): ToList() snapshot, ExecuteCommand(new ClearPageCommand(snapshot)); ClearPageCommand.Execute() iterates snapshot calling RemoveObject, then ClearSelection; ClearPageCommand.Undo() re-adds all from snapshot |
| 16 | When Select tool is active and nothing is selected, the right rail shows a list of all objects on the current page | VERIFIED | RightRailViewModel.HasObjectList = ActiveTool==Select && !HasSelection && !HasDrawingInProgress (line 82-84); RightRail.xaml ObjectListPanel Border Visibility bound to HasObjectList (line 66); ItemsControl ItemsSource="{Binding ObjectList}" (line 97) |
| 17 | Line/Circle/Protractor two-point mode first click snaps to existing geometry endpoints, intersections, and circle centres | VERIFIED | ToolViewModel.HandleCanvasClick: Line Idle case — snap.Snap(screenPx, ...) before ScreenToPage (line 114-115); Circle Idle case — same pattern (line 139-140); Protractor Idle else branch — snap.Snap before ScreenToPage (line 179-180); HandleMouseMove Idle snap ring for Line/Circle (lines 362-368) |

**Score:** 17/17 truths verified (programmatic checks complete; runtime behavior requires human verification)

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `MathGaze/Views/TopBar.xaml` | Updated top bar with 56x56 buttons and 72px height | VERIFIED | Height="72" on UserControl; all 8 interactive buttons at Width="56" Height="56" |
| `MathGaze/Views/ScrollRail.xaml` | Updated scroll rail with 56x56 buttons and 64px width | VERIFIED | Width="64" on UserControl; all 4 scroll buttons Width="56" |
| `MathGaze/Views/ToolRail.xaml` | Updated tool rail with 84x84 vertical-stack buttons | VERIFIED | Width="108"; all 6 tool buttons have Orientation="Vertical" StackPanel |
| `MathGaze/Styles/AppStyles.xaml` | ToolTileStyle 84x84 and DynamicResource brush refs | VERIFIED | Height Value="84"; all ControlTemplate triggers use DynamicResource; 0 SolidColorBrush definitions |
| `MathGaze/Styles/Themes/Light.xaml` | Light theme colour brushes | VERIFIED | Exists; BrushBg Color="#F5F3EE"; all 11 brush keys present |
| `MathGaze/Styles/Themes/Dark.xaml` | Dark theme colour brushes | VERIFIED | Exists; BrushBg Color="#1A1C22"; all 11 brush keys present |
| `MathGaze/ViewModels/SettingsViewModel.cs` | IsDarkMode, IsSettingsPanelOpen, toggle commands | VERIFIED | [ObservableProperty] _isDarkMode, _isSettingsPanelOpen; [RelayCommand] SetTheme(string); UserPreferences.Save() |
| `MathGaze/Properties/Settings.settings` | Theme preference persistence schema | VERIFIED | Exists; contains Name="Theme" |
| `MathGaze/Properties/UserPreferences.cs` | JSON-based preference persistence (deviation from plan) | VERIFIED | Static class; Theme get/set; Save() writes to %APPDATA%\MathGaze\preferences.json |
| `MathGaze/Core/Commands/ClearPageCommand.cs` | IGeometryCommand that bulk-removes all page objects | VERIFIED | Execute() iterates _snapshot calling RemoveObject + ClearSelection; Undo() re-adds all |
| `MathGaze/ViewModels/ToolViewModel.cs` | HasDrawingInProgress + CancelDrawCommand | VERIFIED | `public bool HasDrawingInProgress => DrawState == DrawState.AnchorPlaced`; OnDrawStateChanged raises PropertyChanged; [RelayCommand] CancelDraw() |
| `MathGaze/ViewModels/RightRailViewModel.cs` | ClearPage wiring, HasDrawingInProgress, DrawingInstructionText, ObjectList | VERIFIED | All four features present; ToolViewModel injected via constructor; ObjectList ObservableCollection |
| `MathGaze/Views/RightRail.xaml` | Three-panel structure: DrawingGuidePanel, ObjectListPanel, SelectionPanel; ClearPageButton always visible | VERIFIED | DrawingGuidePanel Border (line 41); ObjectListPanel Border (line 66); SelectionPanel bound to HasSelectionPanel (line 144); ClearPage button outside all panels (line 25) |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| MainWindow.xaml RowDefinition | Height="72" | TopBar height update | VERIFIED | Line 12: `<RowDefinition Height="72"/>` |
| TopBar.xaml gear button | SettingsVm.OpenSettingsCommand | DataContext binding to MainViewModel.SettingsVm | VERIFIED | Command="{Binding SettingsVm.OpenSettingsCommand}" (TopBar.xaml line 112) |
| App.xaml.cs ApplyTheme() | MergedDictionaries[0] | ResourceDictionary swap | VERIFIED | `Current.Resources.MergedDictionaries[0] = dict` (App.xaml.cs line 27) |
| SettingsViewModel.SetThemeCommand | App.ApplyTheme() + UserPreferences.Save() | ((App)Application.Current).ApplyTheme(isDark) | VERIFIED | Both calls present in SetTheme() (SettingsViewModel.cs lines 39, 41) |
| RightRail.xaml DrawingGuidePanel | RightRailViewModel.HasDrawingInProgress | BoolToVisibilityConverter | VERIFIED | RightRail.xaml line 41: `Visibility="{Binding HasDrawingInProgress, Converter=...}"` |
| RightRailViewModel.HasDrawingInProgress | ToolViewModel.DrawState | PropertyChanged subscription | VERIFIED | OnToolPropertyChanged subscribes to HasDrawingInProgress/ActiveTool/DrawState; UpdateDrawingState() sets HasDrawingInProgress = _toolVm.HasDrawingInProgress |
| RightRailViewModel.ClearPageCommand | IGeometryService.ExecuteCommand(ClearPageCommand) | ExecuteCommand pattern | VERIFIED | `_geometryService.ExecuteCommand(new ClearPageCommand(snapshot))` (RightRailViewModel.cs line 193) |
| RightRail.xaml ObjectListPanel | RightRailViewModel.HasObjectList | BoolToVisibilityConverter | VERIFIED | RightRail.xaml line 66: `Visibility="{Binding HasObjectList, Converter=...}"` |
| ObjectListItem.SelectCommand | IGeometryService.SetSelected(id) | RelayCommand lambda | VERIFIED | `new RelayCommand(() => _geometryService.SetSelected(capturedId))` (RightRailViewModel.cs line 178) |
| ToolViewModel.HandleCanvasClick (Line Idle) | SnapEngine.Snap(screenPx, Objects, mapper) | snap.Snap call before ScreenToPage | VERIFIED | `var (snappedPx, _) = snap.Snap(screenPx, _geometryService.Objects, mapper)` (ToolViewModel.cs line 114) |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| RightRail.xaml ObjectListPanel | ObjectList | _geometryService.Objects in Refresh() | Yes — live GeometryService collection | FLOWING |
| RightRail.xaml DrawingGuidePanel | DrawingInstructionText | _toolVm.ActiveTool + _toolVm.DrawState switch | Yes — derived from live ToolViewModel state | FLOWING |
| MainWindow.xaml SettingsPanelOverlay | IsSettingsPanelOpen | SettingsViewModel._isSettingsPanelOpen | Yes — toggled by OpenSettings/CloseSettings commands | FLOWING |
| App.xaml | MergedDictionaries[0] | ApplyTheme(bool) | Yes — reads UserPreferences.Theme on startup | FLOWING |

### Behavioral Spot-Checks

Step 7b skipped: .NET SDK not present in verification environment; no runnable entry points available without the SDK. Four plan SUMMARYs each independently confirm successful `dotnet build` with 0 errors (Plan 01: commit f816321 + 2511029; Plan 02: commit 4e3016e + ccf7080; Plan 03: commit a99e60d + eca02d7; Plan 04: commit 6adde58 + 7beb67b). All 8 commits are present in git log.

### Requirements Coverage

Phase 7 plans declare no formal requirement IDs (`requirements: []` in all four plan headers). REQUIREMENTS.md maps no requirements to Phase 7. Phase 7 delivers UX improvements not yet captured in the v1 requirements spec.

### Anti-Patterns Found

Scan performed on all 8 key files modified in this phase.

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| AppStyles.xaml | 14, 16 | `{StaticResource BrushBorder}` and `{StaticResource BrushInk}` in ToolTileStyle style-level setters (not ControlTemplate triggers) | Info | Intentional per plan decision: style-level setters are applied once at element creation; DataTrigger overrides handle active state. Active state brushes in ControlTemplate triggers are correctly DynamicResource. Theme swap still works because the active/hover states use DynamicResource. |
| AppStyles.xaml | 54, 57 | `{StaticResource BrushBorder}` and `{StaticResource BrushInk2}` in IconButtonStyle style-level setters | Info | Same intentional exception as above — plan explicitly notes style-level setters for IconButtonStyle are excluded from the DynamicResource conversion. |
| TopBar.xaml | 19 | `{StaticResource BrushAccent}` on the branding pill | Info | Plan exempts BrushAccent (cobalt, identical in both themes) from DynamicResource conversion. Intentional. |
| ScrollRail.xaml | 52 | `{StaticResource BrushAccent}` on ScrollThumb Fill | Info | Same BrushAccent exemption. Intentional. |

No blockers. No stubs. No TODO/FIXME/placeholder patterns. No empty implementations. All four observations are intentional design decisions explicitly documented in the plan and summary.

### Human Verification Required

#### 1. Build verification

**Test:** Run `dotnet build MathGaze/MathGaze.csproj` from the project root
**Expected:** Build succeeded. 0 errors. Warnings will include pre-existing NU1701 package compatibility warnings (9 warnings confirmed in Plans 02-04 summaries).
**Why human:** .NET SDK is not installed in the verification environment.

#### 2. Theme toggle — live UI

**Test:** Launch the app. Click the gear button in the top bar. Click "Dark".
**Expected:** A settings panel appears over the right rail (no separate window or dialog). All shell colours immediately change to dark palette (dark background, light text, cobalt accent unchanged). Click "Light" — colours revert. No visual glitches.
**Why human:** ResourceDictionary swap is a runtime WPF behavior that cannot be confirmed statically.

#### 3. Theme persistence

**Test:** With dark theme active, close and relaunch the app.
**Expected:** App opens with dark theme applied immediately — no flash of light theme before dark is applied.
**Why human:** Requires live app restart to verify startup sequence.

#### 4. Drawing guide panel

**Test:** Open a PDF. Activate the Line tool. Click to place a first anchor point.
**Expected:** Right rail immediately changes from the object list to the DrawingGuidePanel showing "Line in progress\nClick 2nd point". Cancel button is visible and >= 56px tall. Pressing Cancel returns the rail to the object list state.
**Why human:** Event-driven panel switching requires a running WPF window.

#### 5. Object list panel — population and selection

**Test:** Place a Line, Circle, and Protractor. Activate the Select tool (no object selected).
**Expected:** Right rail shows "Line 1", "Circle 1", "Protractor 1" as 48px-tall buttons with type chip. Tapping "Circle 1" selects the circle and the right rail switches to circle nudge controls.
**Why human:** ObservableCollection binding and SelectCommand wiring requires a running app.

### Gaps Summary

No gaps identified. All 17 observable truths are verified against the actual codebase. All 13 required artifacts exist and are substantively implemented. All 10 key links are wired. No stubs, missing implementations, or anti-pattern blockers were found.

The status is `human_needed` because 5 runtime behaviors (build, theme toggle, theme persistence, drawing guide panel, object list interaction) cannot be confirmed programmatically without the .NET SDK and a running WPF application.

---

_Verified: 2026-05-30_
_Verifier: Claude (gsd-verifier)_
