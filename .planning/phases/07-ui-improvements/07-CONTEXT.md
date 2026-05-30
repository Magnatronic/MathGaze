# Phase 7: UI improvements - Context

**Gathered:** 2026-05-30
**Status:** Ready for planning

<domain>
## Phase Boundary

Improve the gaze usability of the entire UI shell based on observed interaction gaps:
resize all interactive elements to a consistent ≥56×56px gaze target floor; redesign the
tool rail buttons to be square with icons above labels; widen the scroll rail; add a
contextual object list to the right rail for easier selection; add a drawing guide card
during multi-step operations; enable first-click snapping on lines; wire the dark mode
toggle to the gear button; and provide a "clear current page" action.
PROT-04 (protractor lock) was explicitly excluded by user decision.

The core geometry tools, PDF rendering, session persistence, and export are all complete.
This phase makes the UI genuinely comfortable for daily gaze use.

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

### Top bar button sizing
- **D-12:** All interactive buttons in the top bar become **56×56px minimum**, matching the
  existing PDF export button. This affects: Open, Close, Settings gear, Zoom −/+/Fit,
  Previous/Next page. The top bar height must increase to accommodate (currently 60px;
  will grow to ~72px or as needed).
- **D-13:** The page counter text block and zoom label text block are not buttons and stay
  as-is dimensionally, but their surrounding strip borders must grow in height to match the
  new button heights.

### Tool rail redesign
- **D-14:** Tool buttons become **84×84px square**. Icon is centred above the label (vertical
  stack, not horizontal row). This resolves the "Protrac" truncation — "Protractor" fits
  on one line at the wider button width.
- **D-15:** Tool rail width increases from 104px to accommodate 84px buttons with padding
  (suggest ~100px inner + margins). All 6 tools remain; no scrolling needed.
- **D-16:** "TOOLS" header label stays at top of the rail.

### Scroll rail button sizing
- **D-17:** Scroll rail buttons become **56×56px square** (currently 30px wide × 56px tall).
  Rail width increases from 38px to ~64px (56px button + 4px each side). All 4 buttons
  (Page Up, Scroll Up, Scroll Down, Page Down) resize.

### Object list panel (Select tool, nothing selected)
- **D-18:** When the Select tool is active and no object is selected (`HasSelection = false`),
  the right rail shows an **object list** instead of the "NOTHING SELECTED" dashed box.
  Each row: type icon (small, 16×16px) + auto-generated name ("Line 1", "Circle 2",
  "Protractor 3", "Text 4" — numbered in placement order). Each row is a full-width tap
  target ≥44px tall (gaze-friendly).
- **D-19:** Tapping a row selects that object — equivalent to clicking it on canvas.
  Right rail immediately switches to that object's controls (nudge block, type-specific
  panel). This solves the hit-test precision problem for small or overlapping objects.
- **D-20:** Object list shows only objects on the **current page**. Ordered by placement
  (oldest first). Empty list state: show "No objects on this page" in the dashed box.
- **D-21:** When Select tool is active but an object IS selected, the right rail shows the
  existing selection controls as today (D-18/D-19 only apply to the nothing-selected state).

### First-click snapping
- **D-22:** When drawing a Line, the **first anchor click** (DrawState.Idle → AnchorPlaced)
  runs the same `SnapEngine` logic as the second click — snap to existing endpoints,
  circle centres, and line–line intersections within the snap threshold. Currently the
  first click bypasses snap entirely ("exact placement, no snap" per ToolViewModel.cs).
- **D-23:** The snap ghost/ring indicator already renders during cursor move for the preview;
  committing on first click should snap the anchor to the detected snap point using the
  same `SnapResult` the ghost is already tracking.
- **D-24:** Circle tool first click (centre placement) should also snap — same rules.
  Protractor two-point mode (Phase 5) first click should also snap.

### Protractor lock (PROT-04) — explicitly excluded
- **D-25:** PROT-04 is NOT in Phase 7. User decision: not needed. REQUIREMENTS.md status
  remains "Pending" but is effectively deferred indefinitely.

### Claude's Discretion
- Exact slide-over panel animation (suggest simple opacity/translate, no complex stencil)
- "Clear page" button label and danger styling (suggest same red as Delete, labelled
  "Clear page" to distinguish from Delete)
- Dark theme colour tokens — Claude picks appropriate background, surface, border, ink
  variants for a clean dark theme; cobalt accent unchanged
- Settings panel layout detail (toggle style, close button placement)
- Whether Clear Page button is styled as danger (red) or neutral (the clear action is
  undoable, so danger styling may be over-cautious — Claude's call)
- Object list row height (≥44px suggested; can go to 56px if rail space allows)
- Icon used per object type in the object list (reuse existing SVG paths from ToolRail)

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
- `MathGaze/Views/RightRail.xaml` — add DrawingGuidePanel (HasDrawingInProgress), ObjectListPanel
  (Select + nothing selected), ClearPageButton above undo/redo footer
- `MathGaze/Views/ToolRail.xaml` — redesign all 6 buttons to 84×84px icon-above-label; fix truncation
- `MathGaze/Views/TopBar.xaml` — resize all buttons to 56×56px; wire gear to settings panel
- `MathGaze/Views/ScrollRail.xaml` — resize 4 buttons to 56×56px; widen rail
- `MathGaze/ViewModels/ToolViewModel.cs` — `DrawState`, `ActiveTool`, `AnchorPt`; add
  `HasDrawingInProgress` computed property; first-click snap fix (D-22/D-23/D-24)
- `MathGaze/ViewModels/RightRailViewModel.cs` — add ClearPageCommand, HasDrawingInProgress
  forwarding, ObjectList (ObservableCollection of display items keyed by GeometryObject)
- `MathGaze/Core/SnapEngine.cs` — snap logic already used on second click; wire to first click
- `MathGaze/App.xaml` / `App.xaml.cs` — ResourceDictionary theme swapping entry point
- `MathGaze/Services/IGeometryService.cs` / `GeometryService.cs` — add ClearPage() /
  ClearPageCommand in the existing command pattern

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
- Right rail XAML: three panel states — DrawingGuidePanel (HasDrawingInProgress), ObjectListPanel
  (Select tool + !HasSelection), existing SelectionPanel (HasSelection); ClearPageButton always
  visible above undo/redo row
- `RightRailViewModel`: new `ClearPageCommand`, `HasDrawingInProgress` (forwarded from
  `ToolViewModel`), `ObjectList` ObservableCollection rebuilt on `ObjectsChanged`; each item
  exposes display name + select command
- `ToolViewModel`: fix first-click cases for Line, Circle, and two-point Protractor to pass
  cursor position through `SnapEngine` before committing `AnchorPt`
- `App.xaml`: split current brush definitions into `Themes/Light.xaml` and `Themes/Dark.xaml`;
  `App.xaml.cs` loads saved preference on startup and swaps on toggle
- `MainViewModel` or new `SettingsViewModel`: `ToggleThemeCommand`, `IsDarkMode`; gear button
  in `TopBar.xaml` binds to this; settings panel is a `Grid`/`Border` overlay (no `Popup`)
- `TopBar.xaml` / `ScrollRail.xaml` / `ToolRail.xaml`: all button dimensions updated per
  D-12 through D-17; no new ViewModel wiring needed for resizing

</code_context>

<specifics>
## Specific Ideas

- Screenshot from UAT session shows: "Protrac" label truncated in tool rail; PDF export
  button is the right size but all other top bar buttons are visibly smaller; scroll rail
  buttons are narrow. These are the direct observations that drove D-12 through D-17.
- The object list solves a real daily frustration — when objects overlap or are small,
  the student has to click precisely on the canvas. The list provides a gaze-friendly
  alternative that requires no precision at all.
- First-click snap is a consistency fix. The snap ring already tracks the cursor on first
  click (ghost preview uses it); not committing it felt broken to the user.
- Dark mode is the one setting that materially affects the student's daily experience
  (screen glare in exam rooms). No other v1 settings needed.
- "Clear page" should feel like the Delete button — immediate, undoable — not a ceremony.

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
