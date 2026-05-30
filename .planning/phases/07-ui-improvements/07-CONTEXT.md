# Phase 7: UI improvements - Context

**Gathered:** 2026-05-30
**Status:** Ready for planning

<domain>
## Phase Boundary

Polish four specific interaction gaps for the gaze user: contextual right-rail guidance
during multi-click drawing operations; a dark mode toggle wired to the stub gear button;
and a single-click "clear current page" annotation reset action in the right rail.
PROT-04 (protractor lock) was explicitly excluded by user decision.

The core geometry tools, PDF rendering, session persistence, and export are all complete.
This phase tightens the feel for daily use.

</domain>

<decisions>
## Implementation Decisions

### Mid-draw right rail guidance
- **D-01:** When a tool is active and the anchor is placed (DrawState.AnchorPlaced), the right
  rail replaces the "NOTHING SELECTED" dashed box with a contextual hint card. Content:
  tool name + step instruction (e.g. "Line in progress — click 2nd point" or
  "Circle in progress — click radius point"). One cancel button (≥56×56px) that resets
  draw state back to Idle.
- **D-02:** No orientation constraint buttons (H/V/45°/Free) in this version. Drawing guide
  only. The orientation constraint idea from the 2026-05-06 design note is deferred.
- **D-03:** Clicking the already-active tool button also cancels in-progress draw and resets
  to Idle. This means all tool activate commands call `ResetDrawState()` regardless of
  current state — the student can always bail via the tool rail without hunting for a
  right-rail cancel button.

### Dark mode
- **D-04:** The gear button in the top bar opens an in-window slide-over panel (no flyout,
  no secondary HWND — Grid 3 compatible). The panel contains a single Light / Dark
  mode toggle (≥56×56px target). Close button returns to normal view.
- **D-05:** Dark mode is implemented via WPF `ResourceDictionary` swapping: two theme
  dictionaries (Light.xaml, Dark.xaml) merged at the `Application.Resources` level.
  Toggling replaces the merged dictionary at runtime.
- **D-06:** The current theme preference is persisted via `Properties.Settings` (or
  `System.Configuration.ApplicationSettingsBase`) so it survives app restarts. Default
  is Light mode.
- **D-07:** Only accent-neutral dark colours needed in v1 — no accent colour variant.
  The cobalt accent remains the same in both themes.

### Clear current page
- **D-08:** A "Clear page" button lives in the right rail, above the undo/redo footer row.
  It is always visible (not hidden behind a selection state), ≥56×56px.
- **D-09:** "Clear page" removes all geometry objects on the current page as a single
  undoable command (`ClearPageCommand`). Executes immediately — no confirm dialog
  (Grid 3 cannot use popups; undo provides the safety net). Consistent with how
  Delete works.
- **D-10:** After clear, the canvas repaints empty. Undo restores all cleared objects in
  one step. Auto-save triggers as normal via `ObjectsChanged`.

### Protractor lock (PROT-04) — explicitly excluded
- **D-11:** PROT-04 is NOT in Phase 7. User decision: not needed. REQUIREMENTS.md status
  remains "Pending" but is effectively deferred indefinitely.

### Claude's Discretion
- Exact slide-over panel animation (suggest simple opacity/translate, no complex stencil)
- "Clear page" button label and danger styling (suggest same red as Delete, labelled
  "Clear page" to distinguish from Delete)
- Fix the "Protrac" truncation bug in the tool rail — the ProtractorButton label is cut off
  at the rail width; adjust font size or abbreviate to "Protract." (visible in UAT screenshot)
- Dark theme colour tokens — Claude picks appropriate background, surface, border, ink
  variants for a clean dark theme; cobalt accent unchanged
- Settings panel layout detail (toggle style, close button placement)
- Whether Clear Page button is styled as danger (red) or neutral (the clear action is
  undoable, so danger styling may be over-cautious — Claude's call)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Design system and interaction rules
- `docs/direction-splitrails.jsx` — Split Rails layout, design tokens, RightRailShell pattern,
  SelectionCard component (adapt for mid-draw hint card), NudgeBlock, ToolTileStyle (≥56×56px)
- `docs/shared.jsx` — Design tokens (T object: colours, fonts, spacing), icon set
- `docs/HANDOFF.md` — Locked interaction rules: ≥56×56px targets, click-to-commit, no drag,
  no flyouts/secondary HWNDs, Grid 3 pointer events only

### Key source files
- `MathGaze/Views/RightRail.xaml` — existing right rail structure; add ClearPageButton above
  undo/redo footer; add DrawingGuidePanel that shows when HasDrawingInProgress is true
- `MathGaze/Views/ToolRail.xaml` — all 6 tool buttons; fix Protractor label truncation
- `MathGaze/Views/TopBar.xaml` — gear button is stub; wire to open settings panel
- `MathGaze/ViewModels/ToolViewModel.cs` — `DrawState`, `ActiveTool`, `AnchorPt`; all
  ActivateX commands should call ResetDrawState() (D-03); add HasDrawingInProgress property
- `MathGaze/ViewModels/RightRailViewModel.cs` — add ClearPageCommand; add
  HasDrawingInProgress forwarding from ToolViewModel for right-rail panel switching
- `MathGaze/App.xaml` / `App.xaml.cs` — ResourceDictionary theme swapping entry point
- `MathGaze/Services/IGeometryService.cs` / `GeometryService.cs` — add ClearPage() method
  or handle via a new ClearPageCommand in the command pattern

### Prior phase context
- `.planning/phases/02-geometry-core/02-CONTEXT.md` — D-08 (per-click undo), D-09
  (command pattern via ExecuteCommand), established RailButtonStyle/StepButtonStyle patterns
- `.planning/phases/03-protractor/03-CONTEXT.md` — Grid 3 constraints recap; no flyouts,
  no secondary HWNDs; all UI within the main window
- `.planning/notes/2026-05-06-phase-3-orientation-constraint.md` — design note for
  orientation constraints; noted here for completeness but deferred per D-02

### Requirements
- `.planning/REQUIREMENTS.md` — PROT-04 remains "Pending" but excluded from Phase 7 (D-11)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ToolViewModel.DrawState` / `AnchorPt`: already tracks whether a draw is in progress;
  expose as `HasDrawingInProgress` computed property for right-rail panel switching
- `ToolViewModel.ActivateXCommand` methods: all currently call `ResetDrawState()` —
  D-03 requires verifying they do so unconditionally (already the case per existing code)
- `RailButtonStyle` / `DeleteButtonStyle`: existing button styles satisfy ≥56×56px gaze floor;
  reuse for Clear Page button and cancel button in drawing guide
- `StepButtonStyle` with Tag="active" pattern: established for toggle-style buttons;
  reuse for Light/Dark toggle in settings panel
- `BoolToVisibilityConverter` / `BoolToInverseVisibilityConverter`: already in App.xaml
  resources; use for HasDrawingInProgress panel switching in right rail
- `ToolViewModel.StatusMessage` / toast pattern: already shows in-progress guidance as
  canvas overlay — the right-rail guide card complements this (toast disappears, card persists)

### Established Patterns
- **Command pattern**: all geometry mutations go through `IGeometryService.ExecuteCommand()`
  with undo/redo support; `ClearPageCommand` must follow the same pattern
- **ResourceDictionary theming**: standard WPF approach; `Application.Resources.MergedDictionaries`
  swap at runtime; all existing brushes must be defined in both Light.xaml and Dark.xaml
- **No flyouts / no secondary HWNDs**: Grid 3 constraint; settings panel must be an
  in-window element (e.g. a `Grid` overlay or `Border` covering the right rail area),
  not a `Popup`, `ContextMenu`, or separate `Window`
- **Properties.Settings**: standard .NET mechanism for persisting simple app preferences;
  no external dependency

### Integration Points
- Right rail XAML: add `DrawingGuidePanel` (DataTrigger on `HasDrawingInProgress`) and
  `ClearPageButton` (always visible, above undo/redo row)
- `RightRailViewModel`: new `ClearPageCommand`, `HasDrawingInProgress` property (forwarded
  from `ToolViewModel` via event subscription or observable binding)
- `App.xaml`: split current brush definitions into `Themes/Light.xaml` and `Themes/Dark.xaml`;
  `App.xaml.cs` loads the saved preference on startup and swaps on toggle
- `MainViewModel` or new `SettingsViewModel`: `ToggleThemeCommand`, `IsDarkMode` property;
  gear button in `TopBar.xaml` binds to this command
- Settings panel: a `Grid` or `Border` overlay in `MainWindow.xaml` (or a separate
  UserControl) that slides in when `IsSettingsOpen` is true

</code_context>

<specifics>
## Specific Ideas

- The screenshot shared during discussion shows the "Protrac" truncation clearly — tool rail
  ProtractorButton label is cut off. Fix this as part of the tool rail pass.
- Dark mode is the one setting that materially affects the student's daily experience
  (screen glare in exam rooms). No other v1 settings needed.
- "Clear page" should feel like the Delete button — immediate, undoable — not a ceremony.
  The undo safety net is sufficient; a confirmation dialog would be an extra click the
  student doesn't need.
- Clicking the active tool to cancel draw is the most natural escape hatch for a gaze user
  who is already looking at the tool rail.

</specifics>

<deferred>
## Deferred Ideas

- **Orientation constraint buttons (H/V/45°/Free)** during mid-draw — noted in planning
  notes 2026-05-06; deferred from Phase 7 by user decision (D-02). Good candidate for v2.
- **Protractor lock (PROT-04)** — explicitly excluded from Phase 7 (D-11). Still pending
  in REQUIREMENTS.md.
- **Accent colour selection** — the design envisions 5 accent hues; user chose dark mode
  only for v1. Deferred.
- **Density settings (Comfortable/Spacious/XL)** — design intent; deferred.

</deferred>

---

*Phase: 07-ui-improvements*
*Context gathered: 2026-05-30*
