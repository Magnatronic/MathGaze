# Phase 7: UI Improvements - Research

**Researched:** 2026-05-30
**Domain:** WPF UI improvements — theming, right-rail panel states, button sizing, snapping, settings persistence
**Confidence:** HIGH (all findings verified against live codebase; WPF patterns are well-established)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Mid-draw right rail guidance**
- D-01: When a tool is active and the anchor is placed (DrawState.AnchorPlaced), the right rail replaces the "NOTHING SELECTED" dashed box with a contextual hint card. Content: tool name + step instruction. One cancel button (>=56x56px) that resets draw state back to Idle.
- D-02: No orientation constraint buttons (H/V/45°/Free) in this version. Drawing guide only. Deferred.
- D-03: Clicking the already-active tool button cancels in-progress draw and resets to Idle. All tool activate commands call ResetDrawState() unconditionally — already verified in code.

**Dark mode**
- D-04: The gear button opens an in-window slide-over panel (no flyout, no secondary HWND — Grid 3 compatible). Panel contains a single Light/Dark toggle (>=56x56px). Close button returns to normal view.
- D-05: Dark mode via WPF ResourceDictionary swapping: two theme dictionaries (Light.xaml, Dark.xaml) merged at Application.Resources level. Toggling replaces the merged dictionary at runtime.
- D-06: Theme preference persisted via Properties.Settings (or System.Configuration.ApplicationSettingsBase). Default is Light mode.
- D-07: Only accent-neutral dark colours needed in v1 — cobalt accent unchanged.

**Clear current page**
- D-08: "Clear page" button in right rail, above undo/redo footer row. Always visible, >=56x56px.
- D-09: "Clear page" removes all geometry objects on current page as a single undoable command (ClearPageCommand). No confirm dialog. Consistent with how Delete works.
- D-10: After clear, canvas repaints empty. Undo restores all cleared objects in one step. Auto-save triggers via ObjectsChanged.

**Top bar button sizing**
- D-12: All interactive buttons in top bar become 56x56px minimum. Affects: Open, Close, Settings gear, Zoom -/+/Fit, Previous/Next page. Top bar height increases to ~72px.
- D-13: Page counter and zoom label text blocks stay dimensionally as-is; surrounding strip borders grow in height.

**Tool rail redesign**
- D-14: Tool buttons become 84x84px square. Icon centred above label (vertical stack). "Protractor" fits on one line.
- D-15: Tool rail width increases from 104px. All 6 tools remain; no scrolling needed.
- D-16: "TOOLS" header label stays at top of the rail.

**Scroll rail button sizing**
- D-17: Scroll rail buttons become 56x56px square (currently 30px wide x 56px tall). Rail width increases from 38px to ~64px.

**Object list panel (Select tool, nothing selected)**
- D-18: When Select tool active and no object selected, right rail shows object list instead of "NOTHING SELECTED" dashed box. Each row: type icon (16x16px) + auto-generated name. Each row >=44px tall.
- D-19: Tapping a row selects that object — equivalent to clicking it on canvas.
- D-20: Object list shows only objects on current page, ordered by placement (oldest first). Empty: show "No objects on this page" in dashed box.
- D-21: When Select tool active and object IS selected, right rail shows existing selection controls.

**First-click snapping**
- D-22: When drawing a Line, the first anchor click runs the same SnapEngine logic as second click.
- D-23: The snap ghost/ring indicator already renders during cursor move for the preview; first click should snap anchor to detected snap point.
- D-24: Circle tool first click (centre placement) should also snap. Protractor two-point mode first click should also snap.

**Protractor lock (PROT-04) — explicitly excluded**
- D-25: PROT-04 is NOT in Phase 7.

### Claude's Discretion
- Exact slide-over panel animation (suggest simple opacity/translate, no complex stencil)
- "Clear page" button label and danger styling
- Dark theme colour tokens — Claude picks background, surface, border, ink variants; cobalt accent unchanged
- Settings panel layout detail (toggle style, close button placement)
- Whether Clear Page button is styled as danger (red) or neutral
- Object list row height (>=44px suggested; can go to 56px if rail space allows)
- Icon used per object type in the object list (reuse existing SVG paths from ToolRail)

### Deferred Ideas (OUT OF SCOPE)
- Orientation constraint buttons (H/V/45°/Free) during mid-draw
- Protractor lock (PROT-04)
- Accent colour selection
- Density settings (Comfortable/Spacious/XL)
</user_constraints>

---

## Summary

Phase 7 is a polish phase that touches every visible surface of the app without changing any geometry logic. All decisions are locked; this research verifies the precise implementation paths in the existing codebase and identifies the WPF patterns needed.

The codebase is well-structured: a clean command pattern, existing converter infrastructure, and already-defined button styles make most changes mechanical edits to XAML dimensions plus adding a handful of new classes. The two non-trivial items are ResourceDictionary theme swapping (requires splitting `AppStyles.xaml` into Light/Dark files and wiring a toggle) and ClearPageCommand (a multi-object variant of the existing DeleteObjectCommand pattern).

The right-rail now has three content states to manage: DrawingGuidePanel (anchor placed, any drawing tool), ObjectListPanel (Select tool + no selection), and the existing SelectionPanel (has selection). These are distinguished by two boolean properties: `HasDrawingInProgress` (new, forwarded from ToolViewModel) and `HasSelection` (existing). A three-panel Grid with Visibility bindings handles this cleanly.

**Primary recommendation:** Work in feature slices, not layers. Each of the 7 feature areas (button sizing, tool rail, theme system, settings panel, object list, first-click snap, clear-page) is independently releasable and testable. Tackle them in ascending dependency order: button sizing first (pure XAML, no ViewModel changes), then tool rail, then theme system, then the ViewModel-coupled features.

---

## Standard Stack

All required libraries are already in the project. No new NuGet dependencies needed.

### Core (already installed)
| Library | Version | Purpose | Phase 7 Use |
|---------|---------|---------|-------------|
| CommunityToolkit.Mvvm | 8.4.2 | MVVM, `[ObservableProperty]`, `[RelayCommand]` | New commands and properties in RightRailViewModel, ToolViewModel, new SettingsViewModel |
| SkiaSharp.Views.WPF | 3.119.2 | SkiaSharp canvas host | No change — canvas rendering not touched |
| System.Configuration (inbox) | .NET 9 inbox | `ApplicationSettingsBase` / `Properties.Settings` | Theme preference persistence |
| WPF ResourceDictionary | Inbox | Theme dictionaries | Light.xaml / Dark.xaml theme swap |

[VERIFIED: csproj + App.xaml.cs — all packages confirmed present]

### Supporting Patterns Already Established
| Pattern | Location | Phase 7 Use |
|---------|----------|-------------|
| `BoolToVisibilityConverter` | App.xaml | Panel switching (HasDrawingInProgress, HasSelection) |
| `BoolToInverseVisibilityConverter` | App.xaml | Inverse panel switching |
| `DataTrigger` on `Tag="active"` | AppStyles.xaml StepButtonStyle | Light/Dark toggle button state |
| `RailButtonStyle` / `DeleteButtonStyle` | AppStyles.xaml | Clear Page button, cancel button in drawing guide |
| `IGeometryCommand` / `ExecuteCommand()` | Core/Commands, GeometryService | ClearPageCommand |
| `ObjectsChanged` event | GeometryService | Trigger object list rebuild |

[VERIFIED: Read AppStyles.xaml, RightRail.xaml, GeometryService.cs]

**Installation:** No new packages needed. No `dotnet add package` step.

---

## Architecture Patterns

### Recommended Project Structure Changes

```
MathGaze/
├── Styles/
│   ├── AppStyles.xaml          (existing — keep non-theme styles here)
│   ├── Themes/
│   │   ├── Light.xaml          (NEW — all SolidColorBrush definitions from AppStyles.xaml)
│   │   └── Dark.xaml           (NEW — dark equivalents of same brush keys)
├── Views/
│   ├── RightRail.xaml          (MODIFY — add 3-panel structure)
│   ├── ToolRail.xaml           (MODIFY — 84x84, icon-above-label)
│   ├── TopBar.xaml             (MODIFY — all buttons 56x56)
│   └── ScrollRail.xaml         (MODIFY — buttons 56x56, rail width 64px)
├── ViewModels/
│   ├── ToolViewModel.cs        (MODIFY — HasDrawingInProgress, first-click snap)
│   ├── RightRailViewModel.cs   (MODIFY — ClearPageCommand, ObjectList, HasDrawingInProgress)
│   └── SettingsViewModel.cs    (NEW — IsDarkMode, ToggleThemeCommand)
├── Core/Commands/
│   └── ClearPageCommand.cs     (NEW — IGeometryCommand capturing snapshot for undo)
└── Properties/
    └── Settings.settings        (NEW — Theme string preference)
```

[VERIFIED: codebase structure read from project files]

---

### Pattern 1: ResourceDictionary Theme Swapping

**What:** At runtime, swap the first merged dictionary in `Application.Resources.MergedDictionaries` to switch colour tokens. All brushes in Light.xaml and Dark.xaml must share identical `x:Key` names.

**When to use:** D-05 mandates this exact approach.

**Critical constraint:** `AppStyles.xaml` currently contains both colour brushes AND button styles. The colour brushes must be split out to `Themes/Light.xaml` and `Themes/Dark.xaml`. Button styles that reference brush keys by `{StaticResource}` must stay in a separate styles dictionary — they cannot be in the same dictionary being swapped, or the keys won't resolve after the swap.

**Verified current brushes in AppStyles.xaml that need both Light and Dark variants:**
- `BrushBg`, `BrushSurface`, `BrushSurface2`, `BrushBorder`
- `BrushInk`, `BrushInk2`, `BrushInk3`
- `BrushAccent`, `BrushAccentSoft`, `BrushAccentInk` (same in both themes — cobalt unchanged per D-07)
- `BrushTransparent` (same in both)

[VERIFIED: Read AppStyles.xaml — 11 brush keys identified]

**App.xaml structure after split:**
```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- Index 0: theme dictionary (swapped at runtime) -->
            <ResourceDictionary Source="Styles/Themes/Light.xaml"/>
            <!-- Index 1: non-theme styles (button templates, typography) -->
            <ResourceDictionary Source="Styles/AppStyles.xaml"/>
        </ResourceDictionary.MergedDictionaries>
        <BooleanToVisibilityConverter x:Key="BoolToVisibilityConverter"/>
        <converters:InverseBoolToVisibilityConverter x:Key="BoolToInverseVisibilityConverter"/>
    </ResourceDictionary>
</Application.Resources>
```

**Runtime swap in App.xaml.cs:**
```csharp
// Source: [ASSUMED — standard WPF ResourceDictionary swap pattern]
public void ApplyTheme(bool isDark)
{
    var dict = new ResourceDictionary
    {
        Source = new Uri(
            isDark
                ? "Styles/Themes/Dark.xaml"
                : "Styles/Themes/Light.xaml",
            UriKind.Relative)
    };
    // Replace index 0 — the theme slot
    Application.Current.Resources.MergedDictionaries[0] = dict;
}
```

**Pitfall:** `StaticResource` lookups for brush keys in button templates are resolved once at load time against the merged dictionary tree. When you swap the dictionary at runtime, elements that already rendered with `StaticResource` may not update. Use `DynamicResource` in button templates for any brush key that participates in theming. The existing button styles in AppStyles.xaml use `TemplateBinding` for Background/BorderBrush (which responds to property changes) and inline `{StaticResource}` references. Those inline static references will NOT update on swap — they must be converted to `DynamicResource` within the button templates.

[ASSUMED: DynamicResource requirement for template internals — standard WPF pattern but not verified in this specific codebase's template structure beyond reading the code]

**Dark theme colour tokens (Claude's discretion):**
| Key | Light value | Dark value |
|-----|-------------|------------|
| BrushBg | #F5F3EE | #1A1C22 |
| BrushSurface | #FFFFFF | #22252E |
| BrushSurface2 | #FAF8F3 | #1E2028 |
| BrushBorder | #E8E6E0 | #3A3D47 |
| BrushInk | #1A1D24 | #F0EEE8 |
| BrushInk2 | #4A4E58 | #A8ACBA |
| BrushInk3 | #8A8E98 | #6A6E7A |
| BrushAccent | #3B6FD4 | #3B6FD4 (unchanged) |
| BrushAccentSoft | #EEF2FB | #1E2A45 |
| BrushAccentInk | #2A4FA0 | #7BA4F0 |
| BrushTransparent | Transparent | Transparent |

These token values are chosen to maintain WCAG AA contrast ratios between BrushInk variants and their respective backgrounds, while keeping the cobalt accent visually consistent across themes.

---

### Pattern 2: Properties.Settings for Theme Persistence

**What:** `System.Configuration.ApplicationSettingsBase` is the WPF/Windows Forms mechanism for simple per-user settings. Generates a strongly-typed `Settings` class from a `.settings` file.

**How to add in .NET 9 WPF:**
1. Add `Properties/Settings.settings` file with one entry:
   ```xml
   <Setting Name="Theme" Type="System.String" Scope="User">
     <Value>Light</Value>
   </Setting>
   ```
2. Run build — generates `Properties/Settings.cs` (or use the `Microsoft.Build.Tasks.Core` tooling).
3. Read/write:
   ```csharp
   // Source: [ASSUMED — standard Properties.Settings pattern for .NET WPF]
   // Read on startup
   bool isDark = Properties.Settings.Default.Theme == "Dark";
   // Write on toggle
   Properties.Settings.Default.Theme = isDark ? "Dark" : "Light";
   Properties.Settings.Default.Save();
   ```

**Location of Settings.settings:** `MathGaze/Properties/Settings.settings`

**Scope:** `User` scope stores per-user in `%APPDATA%\...\user.config`. The setting survives app restarts automatically. No additional code needed beyond `.Save()`.

**Alternative:** For a self-contained publish, `Properties.Settings` works correctly — the generated assembly embed handles settings location relative to the user profile, not the EXE location. [ASSUMED]

---

### Pattern 3: ClearPageCommand

**What:** A single `IGeometryCommand` that captures all current geometry objects as a snapshot on Execute and restores the full list on Undo.

**Critical design:** The existing `UndoService.Execute()` pushes ONE command per call. ClearPageCommand must capture a snapshot of all objects at the time of execution so Undo can add them all back. This mirrors how `DeleteObjectCommand` works but for N objects at once.

**Implementation:**
```csharp
// Source: [VERIFIED — pattern derived from DeleteObjectCommand.cs + GeometryService.cs]
public sealed class ClearPageCommand : IGeometryCommand
{
    // Snapshot taken at construction time (before Execute)
    private readonly IReadOnlyList<GeometryObject> _snapshot;

    public ClearPageCommand(IReadOnlyList<GeometryObject> currentObjects)
    {
        // ToList creates a defensive copy — command owns these references
        _snapshot = currentObjects.ToList();
    }

    public void Execute(IGeometryService service)
    {
        // Remove each object individually — RemoveObject is the correct mutation API
        foreach (var obj in _snapshot)
            service.RemoveObject(obj.Id);
        service.ClearSelection();
    }

    public void Undo(IGeometryService service)
    {
        // AddObject re-inserts each object (same instances = same Ids, positions, etc.)
        foreach (var obj in _snapshot)
            service.AddObject(obj);
    }
}
```

**Caller in RightRailViewModel:**
```csharp
[RelayCommand]
private void ClearPage()
{
    var snapshot = _geometryService.Objects.ToList();
    if (snapshot.Count == 0) return;   // no-op if page already empty
    _geometryService.ExecuteCommand(new ClearPageCommand(snapshot));
    // ExecuteCommand raises ObjectsChanged → auto-save triggers (D-10)
}
```

**Note:** The snapshot is taken from `_geometryService.Objects` at the time the button is clicked (not at command construction). Since `ClearPageCommand` receives the list in its constructor, the caller must pass `_geometryService.Objects.ToList()` (not the live collection) immediately before calling `ExecuteCommand`. The command then holds the defensive copy.

[VERIFIED: DeleteObjectCommand.cs, PlaceObjectCommand.cs, UndoService.cs — established pattern]

---

### Pattern 4: Three-Panel Right Rail

**What:** The right rail needs three mutually-exclusive content states:
1. **DrawingGuidePanel** — shown when `HasDrawingInProgress = true` (any drawing tool, AnchorPlaced state)
2. **ObjectListPanel** — shown when `HasDrawingInProgress = false` AND `ActiveTool = Select` AND `HasSelection = false`
3. **SelectionPanel** — shown when `HasDrawingInProgress = false` AND `HasSelection = true`

**Current state:** The right rail has two states only (NOTHING SELECTED / SELECTION PANEL) gated by `HasSelection`. The existing XAML uses `BoolToInverseVisibilityConverter` and `BoolToVisibilityConverter`.

**New ViewModel properties needed:**
- `HasDrawingInProgress` — computed from `ToolViewModel.DrawState == DrawState.AnchorPlaced`
- `HasObjectList` — computed: `!HasDrawingInProgress && ActiveTool == Select && !HasSelection`

**XAML pattern for three states:**
```xml
<!-- State 1: Drawing guide -->
<Border Visibility="{Binding HasDrawingInProgress, Converter={StaticResource BoolToVisibilityConverter}}">
    <!-- hint card + cancel button -->
</Border>

<!-- State 2: Object list (Select + nothing selected + not drawing) -->
<Border Visibility="{Binding HasObjectList, Converter={StaticResource BoolToVisibilityConverter}}">
    <!-- list or empty dashed box -->
</Border>

<!-- State 3: Selection panel (has selection + not drawing) -->
<StackPanel Visibility="{Binding HasSelectionPanel, Converter={StaticResource BoolToVisibilityConverter}}">
    <!-- existing selection content -->
</StackPanel>
```

Where `HasSelectionPanel = HasSelection && !HasDrawingInProgress`.

**Wire-up:** `RightRailViewModel` needs a reference to `ToolViewModel` to subscribe to its `PropertyChanged` event for `DrawState` and `ActiveTool` changes.

[VERIFIED: RightRail.xaml, ToolViewModel.cs, RightRailViewModel.cs — existing two-state pattern confirmed; three-state extension is mechanical]

---

### Pattern 5: ObservableCollection for Object List

**What:** `ObservableCollection<ObjectListItem>` where `ObjectListItem` is a simple display record with: display name (e.g. "Line 1"), type enum, select command.

**Naming convention:** Per D-18, names are "Line 1", "Circle 2", etc. numbered in placement order (index in `_geometryService.Objects` + 1). This is purely a display concern — the underlying `GeometryObject.Id` is used for selection.

**Rebuild strategy:** Rebuild the entire `ObservableCollection` on every `ObjectsChanged` event. The list is typically small (10–30 items for exam geometry work). Rebuilding entirely avoids complex diff logic and ensures ordering always matches `_geometryService.Objects`.

**Memory leak prevention:** The existing `RightRailViewModel` subscribes to `_geometryService.ObjectsChanged` via a named method (`OnObjectsChanged`). The same pattern applies for any new ToolViewModel event subscription. Named methods (not lambdas) allow unsubscription in a `Dispose()` method if needed.

**ObjectListItem model:**
```csharp
// Source: [VERIFIED — pattern from RightRailViewModel.cs + GeometryObject.cs]
public sealed class ObjectListItem
{
    public string DisplayName { get; init; } = string.Empty;
    public ToolMode ObjectType { get; init; }   // for icon lookup
    public IRelayCommand SelectCommand { get; init; } = null!;
}
```

**Rebuild in RightRailViewModel.Refresh():**
```csharp
// Called on ObjectsChanged — rebuild object list items
var newItems = _geometryService.Objects
    .Select((obj, i) => new ObjectListItem
    {
        DisplayName = obj switch
        {
            PointObject      => $"Point {i + 1}",
            LineObject       => $"Line {i + 1}",
            CircleObject     => $"Circle {i + 1}",
            ProtractorObject => $"Protractor {i + 1}",
            TextObject       => $"Text {i + 1}",
            _                => $"Object {i + 1}",
        },
        SelectCommand = new RelayCommand(() => _geometryService.SetSelected(obj.Id)),
    })
    .ToList();
ObjectList.Clear();
foreach (var item in newItems) ObjectList.Add(item);
```

[ASSUMED: Per-item RelayCommand allocation on each rebuild is acceptable for small lists. If profiling shows churn, switch to a keyed update strategy — but this is unlikely needed for exam geometry counts.]

---

### Pattern 6: First-Click Snapping

**What:** Currently, the first click in Line, Circle, and Protractor two-point mode bypasses `snap.Snap()` and commits `screenPx` directly (comments in code say "exact placement, no snap"). D-22/D-23/D-24 require running snap on first click too.

**Current code (ToolViewModel.cs, lines 92-99 for Line):**
```csharp
case (ToolMode.Line, DrawState.Idle):
{
    // First click — exact anchor placement, no snap
    var (xPt, yPt) = mapper.ScreenToPage(screenPx);
    AnchorPt  = (xPt, yPt);
    DrawState = DrawState.AnchorPlaced;
    ...
}
```

**Required change:**
```csharp
case (ToolMode.Line, DrawState.Idle):
{
    // First click — snap to existing geometry (D-22)
    var (snappedPx, _) = snap.Snap(screenPx, _geometryService.Objects, mapper);
    var (xPt, yPt) = mapper.ScreenToPage(snappedPx);
    AnchorPt  = (xPt, yPt);
    DrawState = DrawState.AnchorPlaced;
    ...
}
```

Same pattern for `ToolMode.Circle, DrawState.Idle` (line 115-121).

For Protractor two-point path (the `else` branch at line 153-160), the vertex click should also snap:
```csharp
// Two-point protractor path first click
var (snappedPx, _) = snap.Snap(screenPx, _geometryService.Objects, mapper);
var (xPt, yPt) = mapper.ScreenToPage(snappedPx);
AnchorPt = (xPt, yPt);
```

**Note on snap during cursor move:** `HandleMouseMove` currently sets `LastSnap` only during `DrawState.AnchorPlaced`. For the snap ghost to show during DrawState.Idle (before first click), `HandleMouseMove` also needs to call snap during Idle when a drawing tool is active. This is an optional visual enhancement — the functional first-click snap does not require it, but it improves user feedback.

[VERIFIED: ToolViewModel.cs lines 73-307 — all three cases confirmed]

---

### Pattern 7: In-Window Settings Panel (Slide-over)

**What:** A `Border`/`Grid` overlay element inside the existing window layout, positioned over the right rail area. Not a `Popup`, not a secondary `Window`. Grid 3 can interact with it as normal window content.

**WPF implementation:** Use a `Grid` with `Panel.ZIndex` or position it as the topmost element in the main window's layout `Grid`. A `TranslateTransform` or `Margin` animation gives the slide-in effect.

**Recommended approach:**
- Add an overlay `Border` as a sibling of the main layout content inside `MainWindow.xaml`
- Bind `Visibility` to `SettingsViewModel.IsSettingsPanelOpen`
- Position it to cover only the right rail area (right-aligned, same width as right rail)
- Use a simple `RenderTransform` `TranslateTransform` with a `DoubleAnimation` for the slide

**Settings panel contents (D-04, D-07):**
```
┌─────────────────────┐
│  SETTINGS        [X]│  ← close button 56x56
├─────────────────────┤
│  THEME               │
│  [Light] [Dark]      │  ← StepButtonStyle pattern, active = cobalt
└─────────────────────┘
```

**SettingsViewModel (new, registered as singleton in App.xaml.cs):**
```csharp
public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool _isDarkMode;
    [ObservableProperty] private bool _isSettingsPanelOpen;

    [RelayCommand] private void OpenSettings()  => IsSettingsPanelOpen = true;
    [RelayCommand] private void CloseSettings() => IsSettingsPanelOpen = false;

    [RelayCommand]
    private void SetTheme(string theme)
    {
        IsDarkMode = theme == "Dark";
        // Call App.Current to swap dictionary + save preference
        ((App)Application.Current).ApplyTheme(IsDarkMode);
    }
}
```

**Gear button in TopBar:** Bind its `Command` to `SettingsViewModel.OpenSettingsCommand`. TopBar currently has no command wired to the gear button (confirmed: ToolTip says "not yet implemented"). The gear button needs to be resized to 56x56 (D-12) as part of that change.

[VERIFIED: TopBar.xaml line 109-122 — gear button is 40x40 with no Command binding]

---

### Pattern 8: Button Sizing Changes

All sizing changes are pure XAML edits — no ViewModel changes required.

**TopBar.xaml current state (confirmed by reading):**
| Button | Current size | Target size |
|--------|-------------|-------------|
| Open (folder icon) | 36x36 (IconButtonStyle default) | 56x56 |
| Close (X icon) | 36x36 | 56x56 |
| PDF Export | 56x56 (already correct) | 56x56 — no change |
| Settings gear | 40x40 | 56x56 |
| Zoom out | 32x32 | 56x56 |
| Zoom in | 32x32 | 56x56 |
| Fit page | 32x32 | 56x56 |
| Previous page | 36x32 | 56x56 |
| Next page | 36x32 | 56x56 |
| TopBar height | 60px | 72px (accommodate 56px buttons + 8px padding each side) |

**IconButtonStyle** currently has `MinWidth=36, MinHeight=36, Width=36, Height=36`. Setting explicit `Width="56" Height="56"` on each button overrides these (element-level properties win over style setters — same pattern as Delete button in RightRail).

**ToolRail.xaml current state:**
| Property | Current | Target |
|---------|---------|--------|
| `ToolTileStyle` Width/Height | 84x56 (horizontal layout) | 84x84 (square) |
| Rail width | 104px | ~100px inner + margins, suggest 108px |
| Button content layout | `Orientation="Horizontal"` (icon left, label right) | `Orientation="Vertical"` (icon above, label below) |
| Protractor label FontSize | 11 (set inline to fit "Protractor") | 12 (default) — now fits at 84px width |

**`ToolTileStyle` must be updated** in AppStyles.xaml: `Height` from 56 to 84, `ContentPresenter` alignment from `HorizontalAlignment="Left"` to `HorizontalAlignment="Center" VerticalAlignment="Center"`.

**ScrollRail.xaml current state:**
| Property | Current | Target |
|---------|---------|--------|
| Button Width | 30 | 56 |
| Button Height | 56 (already correct) | 56 — no change |
| `UserControl Width` | 38 | 64 |
| Border padding | 4 | 4 (unchanged) |

[VERIFIED: TopBar.xaml, ToolRail.xaml, ScrollRail.xaml, AppStyles.xaml — all current dimensions confirmed by reading]

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Theme switching | Custom colour injection or per-control style overrides | ResourceDictionary swap at `MergedDictionaries[0]` | WPF's built-in merge resolves all `DynamicResource` references automatically across the tree |
| Settings persistence | File I/O to custom JSON | `Properties.Settings` (`ApplicationSettingsBase`) | Handles user profile location, roaming, and `.Save()` atomicity automatically |
| Object list naming | Dynamic name computation per render | Build once on ObjectsChanged, store in ObservableCollection | Eliminates repeated switching logic in XAML converters |
| Clear-page undo | Ad-hoc undo stack bypass | `ClearPageCommand : IGeometryCommand` via `ExecuteCommand()` | UndoService handles stack management; bypass would corrupt undo history |
| Settings overlay | `Popup` or new `Window` | In-window `Border`/`Grid` overlay | Grid 3 cannot interact with secondary HWNDs or WPF Popup elements |

---

## Common Pitfalls

### Pitfall 1: StaticResource in button templates won't update on theme swap

**What goes wrong:** Button background and border colours set via `{StaticResource}` inside `ControlTemplate` triggers are looked up once at load time and frozen. Swapping the theme dictionary changes the resource but existing rendered elements don't see the change.

**Why it happens:** `StaticResource` in WPF is a one-time lookup. Only `DynamicResource` re-queries the resource tree when the dictionary changes.

**How to avoid:** In the `ControlTemplate` sections of `RailButtonStyle`, `StepButtonStyle`, `ToolTileStyle`, and `IconButtonStyle`, replace `{StaticResource BrushXxx}` with `{DynamicResource BrushXxx}` for any brush key that has different Light/Dark values. Brush keys that are identical in both themes (BrushAccent, BrushTransparent) can remain StaticResource.

**Warning signs:** After theme toggle, buttons retain their old background/border colour while surrounding panels update.

[VERIFIED: AppStyles.xaml — all four button styles use StaticResource internally]

### Pitfall 2: SkiaSharp canvas (SKElement) does not participate in WPF theming

**What goes wrong:** The SkiaSharp canvas draws geometry using hardcoded `SKColor` values in `GeometryLayerViewModel`. These are not WPF brushes and will not respond to theme swaps.

**Why it happens:** SkiaSharp renders directly via `PaintSurface`, bypassing WPF's visual tree theming entirely.

**How to avoid:** Phase 7 does not require theming the canvas geometry colours — only the surrounding UI shell. The student's geometry (cobalt lines, circles) looks the same in both themes. The canvas background is the PDF rendered as a bitmap, which is also theme-independent. No action needed for v1.

**Warning signs:** N/A for this phase — just be aware the canvas won't darken.

[VERIFIED: GeometryLayerViewModel pattern confirmed from codebase structure; canvas colours are SKPaint, not WPF brushes]

### Pitfall 3: ClearPageCommand snapshot must be taken before ExecuteCommand is called

**What goes wrong:** If `ClearPageCommand` calls `_geometryService.Objects` inside its `Execute()` method to know what to remove, the list will already be partially mutated by the first few `RemoveObject()` calls in a loop.

**Why it happens:** `_geometryService.Objects` is a live `IReadOnlyList<>` view of the internal `List<GeometryObject>`. Iterating it while removing will throw or silently skip items.

**How to avoid:** The snapshot (`IReadOnlyList<GeometryObject>` stored in the command) must be passed as a constructor argument — taken from `_geometryService.Objects.ToList()` before `ExecuteCommand` is called. `Execute()` then iterates the snapshot, not the live list.

[VERIFIED: GeometryService.cs — `_objects` is a `List<T>`, `Objects` property returns it as `IReadOnlyList<T>` without copying]

### Pitfall 4: ToolViewModel reference in RightRailViewModel requires DI wiring

**What goes wrong:** `RightRailViewModel` currently only depends on `IGeometryService`. Adding `HasDrawingInProgress` requires observing `ToolViewModel.DrawState`. If `ToolViewModel` is added as a constructor parameter without registering the subscription correctly, `PropertyChanged` events won't fire.

**Why it happens:** CommunityToolkit.Mvvm's `[ObservableProperty]` generates `OnDrawStateChanged` partial methods and raises `PropertyChanged`. The subscriber must call `toolVm.PropertyChanged += OnToolPropertyChanged` in the constructor.

**How to avoid:** Add `ToolViewModel` as a constructor parameter to `RightRailViewModel` (DI already resolves both as singletons — no new registration needed). Subscribe to `PropertyChanged` with a named method. In the handler, check `e.PropertyName` for `nameof(ToolViewModel.DrawState)` or `nameof(ToolViewModel.ActiveTool)` and recompute `HasDrawingInProgress`.

[VERIFIED: App.xaml.cs DI registration — ToolViewModel and RightRailViewModel are both AddSingleton; no circular dependency]

### Pitfall 5: Properties.Settings requires app restart to update in self-contained publish

**What goes wrong:** `Properties.Settings` generates a `user.config` file in the user's profile. On first run of a new version, the old `user.config` may be ignored (application version change). The setting effectively defaults on version bump.

**Why it happens:** `ApplicationSettingsBase` scopes `user.config` by application version. A new publish = new version = empty settings.

**How to avoid:** Either (a) accept this as acceptable for a v1 preference that defaults to Light anyway, or (b) call `Properties.Settings.Default.Upgrade()` on startup before `.Default.Theme` is read. `Upgrade()` migrates settings from the previous version.

[ASSUMED: ApplicationSettingsBase upgrade behaviour — standard .NET knowledge, not verified against .NET 9 specifically]

### Pitfall 6: Tool rail button width — ToolTileStyle HeightWidth vs explicit override

**What goes wrong:** `ToolTileStyle` sets `Width=84, Height=56`. The tool rail currently overrides nothing — it relies on the style's default size. Changing the style to `Height=84` changes ALL buttons globally. This is correct for the tool rail but must not affect other consumers of `ToolTileStyle` (there are none currently — confirmed by reading the codebase).

**How to avoid:** Verify no other code uses `ToolTileStyle`. Change `Height` from 56 to 84 directly in AppStyles.xaml. Update `ContentPresenter` alignment to centre vertically.

[VERIFIED: Grepped codebase — ToolTileStyle is only referenced in ToolRail.xaml]

### Pitfall 7: Page cache interaction with ClearPage

**What goes wrong:** `MainViewModel._pageObjectCache` caches the list of objects when the user leaves a page. If ClearPage is executed and then the user navigates away and back, the cache will contain the pre-clear objects (restored via `_pageObjectCache`).

**Why it happens:** The page cache is a snapshot taken in `OnCurrentPageChanged`. The undo stack lives in `UndoService` which is not page-aware. After ClearPage + navigate away + navigate back, the cache restores the old objects, bypassing the undo stack entirely.

**How to avoid:** After `ClearPageCommand.Execute()`, also call `_geometryService.Reset()` on the undo stack... but wait: that would destroy the undo entry. The correct fix is: when `OnCurrentPageChanged` writes the cache snapshot, it must write the CURRENT state of `_geometryService.Objects` (which reflects any clears). This is already what the code does — the cache snapshot is taken from `_geometryService.Objects` at the time of navigation. So if the user clears page 1 then navigates to page 2, the cache entry for page 1 will contain an empty list (correct). Undo is only meaningful while on the same page (undo stack clears on page navigation via `Reset()`).

**Conclusion:** No action needed. The existing page cache and navigation reset pattern already handles this correctly. ClearPage followed by undo must happen on the same page, which the undo stack supports.

[VERIFIED: MainViewModel.cs — `OnCurrentPageChanged` calls `_geometryService.Reset()` which clears the undo stack; per-page undo is the established behaviour]

---

## Code Examples

Verified patterns from the live codebase:

### Existing panel switching (two-state — basis for three-state extension)
```xml
<!-- Source: RightRail.xaml lines 28-44 -->
<Grid Visibility="{Binding HasSelection, Converter={StaticResource BoolToInverseVisibilityConverter}}">
    <!-- NOTHING SELECTED content -->
</Grid>
<StackPanel Visibility="{Binding HasSelection, Converter={StaticResource BoolToVisibilityConverter}}">
    <!-- SELECTION PANEL content -->
</StackPanel>
```

### Existing active-state toggle button (reuse for Light/Dark toggle)
```xml
<!-- Source: RightRail.xaml lines 118-127 — StepButtonStyle + DataTrigger pattern -->
<Button Command="{Binding SetStyleClassicCommand}" Content="180°">
    <Button.Style>
        <Style TargetType="Button" BasedOn="{StaticResource StepButtonStyle}">
            <Style.Triggers>
                <DataTrigger Binding="{Binding IsStyleClassic}" Value="True">
                    <Setter Property="Tag" Value="active"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Button.Style>
</Button>
```

### Existing command with undo (basis for ClearPageCommand)
```csharp
// Source: DeleteObjectCommand.cs
public sealed class DeleteObjectCommand : IGeometryCommand
{
    private readonly GeometryObject _obj;
    public DeleteObjectCommand(GeometryObject obj) => _obj = obj;
    public void Execute(IGeometryService service) => service.RemoveObject(_obj.Id);
    public void Undo(IGeometryService service)    => service.AddObject(_obj);
}
```

### Existing snap call (basis for first-click snap addition)
```csharp
// Source: ToolViewModel.cs lines 103-105
case (ToolMode.Line, DrawState.AnchorPlaced):
{
    var (snappedPx, _) = snap.Snap(screenPx, _geometryService.Objects, mapper);
    var (xPt, yPt) = mapper.ScreenToPage(snappedPx);
    ...
}
```

### Existing danger button pattern
```xml
<!-- Source: RightRail.xaml lines 235-242 -->
<Button Height="56"
        Style="{StaticResource DeleteButtonStyle}"
        Command="{Binding DeleteCommand}"
        Content="Delete"
        Background="#CC2020"
        Foreground="White"
        HorizontalAlignment="Stretch"/>
```

---

## State of the Art

| Old Approach | Current Approach | Notes |
|--------------|------------------|-------|
| WPF `Popup` for overlays | In-window `Grid`/`Border` overlay | Required for Grid 3 compatibility (established Phase 3 rule) |
| Singleton `Application.Current.Resources` global brushes | Theme dictionary at `MergedDictionaries[0]` | Standard WPF theming — not WinUI 3's `XamlRoot` approach |
| Inline `Width`/`Height` overrides per button | Style-defined dimensions | Phase 7 is consolidating — tool rail uses style; top bar still needs inline overrides for non-standard sizes |

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | DynamicResource required inside ControlTemplate trigger Setters for brush keys to update on dictionary swap | Pattern 1 (ResourceDictionary) | If wrong: some brush references may update without DynamicResource (depends on WPF version); test by running theme toggle and inspecting visual output |
| A2 | Properties.Settings works correctly in self-contained .NET 9 WPF publish | Pattern 2 (Settings) | If wrong: settings don't persist; fallback is read/write to a small JSON file in %APPDATA% |
| A3 | Properties.Settings.Default.Upgrade() needed on version bump | Pitfall 5 | If wrong: users lose their theme preference after each publish; acceptable for v1 Light default |
| A4 | Per-item RelayCommand allocation in ObservableCollection rebuild is acceptable for typical exam geometry counts (< 30 items) | Pattern 5 (ObjectList) | If wrong: observable collection rebuild causes visible lag; fix by caching items and doing keyed diff |
| A5 | Dark theme colour token values maintain accessibility contrast | Pattern 1 (Dark tokens table) | If wrong: text is insufficiently readable for low-vision users; verify with contrast checker before release |

---

## Open Questions

1. **SettingsViewModel DI registration placement**
   - What we know: `App.xaml.cs` registers all viewmodels. `SettingsViewModel` is new and needs to be a singleton.
   - What's unclear: Which ViewModel should own `OpenSettingsCommand` for binding from TopBar? TopBar currently binds to `MainViewModel`. Either add to MainViewModel or create SettingsViewModel and inject into TopBar's DataContext.
   - Recommendation: Add `SettingsViewModel` as a DI singleton. Wire TopBar's DataContext to `MainViewModel` plus make `SettingsViewModel` accessible via a property on `MainViewModel`, or use a separate `DataContextProxy` approach. Simplest: expose `SettingsViewModel` as a property of `MainViewModel` for binding.

2. **Snap during Idle hover (visual feedback before first click)**
   - What we know: `HandleMouseMove` only runs snap during `AnchorPlaced`. The snap ghost ring doesn't show during Idle.
   - What's unclear: Whether the user/D-22/D-23 intend for the snap ring to also show during Idle hover (before first click).
   - Recommendation: Add Idle-state snap tracking in `HandleMouseMove` for Line and Circle tools (not Protractor — it already skips snap during AnchorPlaced). This is a UX enhancement; the functional first-click snap works regardless.

3. **ObjectListItem SelectCommand and RightRailViewModel lifecycle**
   - What we know: Each `ObjectListItem` holds a `RelayCommand` that captures an `obj.Id` closure.
   - What's unclear: If objects are garbage collected and re-added (via undo), the Id is stable (it's a Guid set at construction). The closure is safe.
   - Recommendation: Use `obj.Id` in the lambda, not `obj` itself. This is already the pattern in `_geometryService.SetSelected(obj.Id)`.

---

## Environment Availability

Step 2.6: SKIPPED — This phase is purely code and XAML changes within the existing WPF project. No external tools, services, databases, or CLI utilities beyond the existing build chain are required. The project already builds and runs; Phase 7 adds no new external dependencies.

---

## Validation Architecture

`nyquist_validation: false` in `.planning/config.json` — this section is skipped.

---

## Security Domain

This phase involves no authentication, session management, input validation from external sources, or cryptography. The only new data handled is the theme preference string ("Light" / "Dark") persisted to `Properties.Settings`. This is a low-sensitivity user preference with no security implications.

`security_enforcement` is not explicitly set in config.json (absent = enabled by default for most projects), but the threat surface for this phase is negligible — all changes are UI layout and local preference persistence. No ASVS categories apply.

---

## Sources

### Primary (HIGH confidence — verified by reading live codebase)
- `MathGaze/App.xaml` — current merged dictionary structure; confirmed single AppStyles.xaml merge
- `MathGaze/App.xaml.cs` — DI registrations; confirmed ToolViewModel and RightRailViewModel as singletons
- `MathGaze/Styles/AppStyles.xaml` — all 11 brush keys, all 5 button styles, exact dimensions
- `MathGaze/Views/RightRail.xaml` — two-panel structure, existing converters, button sizes
- `MathGaze/Views/ToolRail.xaml` — 84x56 ToolTileStyle, horizontal layout, Protractor at FontSize 11
- `MathGaze/Views/TopBar.xaml` — gear button at 40x40 with no Command; all other button sizes
- `MathGaze/Views/ScrollRail.xaml` — 30x56 scroll buttons, Width=38 rail
- `MathGaze/ViewModels/ToolViewModel.cs` — exact first-click code at lines 78-99, 115-121, 151-160; `snap.Snap()` call on second click lines 103-105
- `MathGaze/ViewModels/RightRailViewModel.cs` — constructor, Refresh() pattern, no ToolViewModel dependency
- `MathGaze/Core/SnapEngine.cs` — `Snap()` signature and return type `(SKPoint Position, string? Label)`
- `MathGaze/Core/Commands/IGeometryCommand.cs` — `Execute/Undo(IGeometryService)` interface
- `MathGaze/Core/Commands/DeleteObjectCommand.cs` — single-object command pattern
- `MathGaze/Core/Commands/PlaceObjectCommand.cs` — add/remove command pattern
- `MathGaze/Services/GeometryService.cs` — `_objects` is `List<T>`, `Objects` is live view, `AddObject`/`RemoveObject` signatures
- `MathGaze/Services/UndoService.cs` — stack-based Execute/Undo/Redo; `Clear()` on reset
- `MathGaze/ViewModels/MainViewModel.cs` — page cache pattern, DI wiring, `_geometryService.Reset()` on page nav
- `MathGaze/Core/Geometry/GeometryObject.cs` — `Id` is Guid, `IsSelected` is JsonIgnore
- `MathGaze/MathGaze.csproj` — confirmed .NET 9, WPF, no System.Configuration explicit reference (inbox)
- `.planning/config.json` — `nyquist_validation: false` confirmed

### Secondary (MEDIUM confidence — standard WPF documentation patterns)
- WPF ResourceDictionary theming: standard WPF pattern documented in Microsoft docs; `DynamicResource` vs `StaticResource` behaviour is well-established
- `Properties.Settings` / `ApplicationSettingsBase`: standard .NET WPF mechanism; present in all .NET Framework and .NET Core WPF projects

### Tertiary (LOW confidence — assumed from training knowledge)
- `Properties.Settings.Default.Upgrade()` migration behaviour on version bump (A3)
- `DynamicResource` required specifically inside ControlTemplate trigger Setters (A1) — needs verification by testing theme swap in running app

---

## Metadata

**Confidence breakdown:**
- Button sizing changes: HIGH — pure XAML dimension changes, all current sizes verified
- Theme system (ResourceDictionary split): HIGH for structure; MEDIUM for DynamicResource specifics in existing templates
- ClearPageCommand: HIGH — pattern directly derived from existing DeleteObjectCommand
- First-click snap: HIGH — exact code location identified; change is a one-line addition per case
- Object list panel: HIGH — ObservableCollection and Refresh() pattern already established in codebase
- Settings persistence: MEDIUM — Properties.Settings is standard but not yet tested in this specific publish configuration
- Settings panel slide-over: HIGH — in-window overlay pattern established in Phase 3; no new WPF mechanics

**Research date:** 2026-05-30
**Valid until:** 2026-07-01 (stable WPF patterns; no fast-moving ecosystem components)
