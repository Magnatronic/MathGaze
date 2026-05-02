# Phase 2: Geometry Core - Context

**Gathered:** 2026-05-02
**Status:** Ready for planning

<domain>
## Phase Boundary

All geometry creation and editing tools: Point (1 click), Line (2 clicks), Circle (2 clicks), Select, Nudge (with sub-point targeting), Delete, Snap-to-endpoint/intersection/orientation, Undo/Redo. Students can place and adjust geometry objects precisely on top of the PDF using only gaze clicks. No protractor (Phase 3), no text/MCQ (Phase 4).

</domain>

<decisions>
## Implementation Decisions

### Ghost preview for 2-click drawing
- **D-01:** Full ghost + status hint. Between click 1 and click 2 for Line and Circle, a dashed preview follows the cursor: filled accent dot + outer ring at the anchor point, dashed preview line/arc from anchor to current cursor position. Bottom toast: "Click 2nd point" (with snap context if a snap candidate is active).
- **D-02:** For Circle specifically: after click 1 (center), a ghost circle appears whose radius equals the distance from center to cursor. The edge ghost updates live as the cursor moves.

### Sub-point nudge model (endpoint targeting)
- **D-03:** Whole-object translate when no sub-point is selected. If a line or circle is selected and no endpoint is focused, UDLR nudge moves the entire object by the step size.
- **D-04:** When a Line is selected, both endpoints render as large tap targets (≥56×56px hit area around a visual dot). Tapping an endpoint "sub-selects" it; UDLR then nudges only that endpoint. Tapping anywhere else on canvas (not an endpoint) keeps the line selected but clears the endpoint sub-selection.
- **D-05:** When a Circle is selected, the center dot and the edge point both render as tap targets. Tapping the center sub-selects it (nudge translates the whole circle). Tapping the edge point sub-selects it (nudge changes the radius only).
- **D-06:** A Point object has no sub-points — nudge always moves it.
- **D-07:** Right rail reflects sub-selection state: shows "Move endpoint A" or "Move endpoint B" label in the nudge block when a sub-point is active.

### Undo/Redo
- **D-08:** Per-click undo. Every discrete action (place object, delete object, each nudge button press) is one undo entry. No time-window batching. Students doing 15 nudges can undo each step individually; redo restores them forward.
- **D-09:** Undo stack uses a command pattern (IGeometryCommand with Execute/Undo). All geometry mutations go through the command stack.

### Coordinate storage
- **D-10:** Geometry object positions stored in PDF point coordinates (not screen pixels). CoordinateMapper converts to screen pixels for rendering and hit-testing. This keeps geometry stable across zoom and scroll changes.

### DPI correction (carry-forward from Phase 1 VERIFICATION.md)
- **D-11:** Fix the `dpiScale = 1.0` hardcode in `PdfCanvasViewModel.EnsureCoordinateMapper()`. Phase 2 should wire the real `PixelsPerDip` from `VisualTreeHelper.GetDpi()` so high-DPI screens render geometry correctly.

### Claude's Discretion
- Hit-test tolerance buffer around lines (recommend 8–10px radius for gaze accuracy)
- Snap proximity threshold (recommend 20px screen pixels — large enough for gaze imprecision)
- Exact accent dot size for anchor and sub-selected endpoint indicators
- Visual style (stroke width, opacity) for ghost preview and committed geometry objects
- Whether orientation guide snaps (V/H/45°) show a faint guide line across the canvas or just affect snap behaviour

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Layout and visual design
- `docs/direction-splitrails.jsx` — NudgeBlock component (step 1/5/20 + UDLR pad), SelectionCard, UtilRow, RightRailLineOne, PivotPicker visual (for reference even though Pivot Picker is deferred)
- `docs/shared.jsx` — Design tokens (T object: accent cobalt, surface, border, ink levels, mono font), icon set, ToolTileStyle

### Interaction rules and feature spec
- `docs/HANDOFF.md` — Interaction rules (click-to-commit, ≥56×56px, no drag), adjustment model description, snap behaviour, tool set MVP
- `SPEC.md` §4 (Core Object Model), §5.1 (Basic Construction tools) — Point, Line, Circle interaction specs

### In-progress / ghost state design
- `docs/additional-screens.jsx` — `ReflectionDrawing` function: shows dashed ghost line + anchor dot + cursor ring pattern for 2-click drawing; `ProtractorPlacing`: shows snap ring + filled snap dot + hint line pattern

### Phase 1 foundation
- `.planning/phases/01-foundation/01-CONTEXT.md` — Established decisions: CoordinateMapper contract, Docnet PDF service, AppStyles design tokens, WPF + SkiaSharp stack
- `.planning/phases/01-foundation/01-VERIFICATION.md` — Anti-pattern noted: dpiScale=1.0 hardcode (fix in Phase 2, D-11)
- `MathGaze/Core/CoordinateMapper.cs` — Both directions (PageToScreen, ScreenToPage, GetPageDestRect); Update() for efficient reuse
- `MathGaze/ViewModels/PdfCanvasViewModel.cs` — Paint() method to extend with geometry layer; EnsureCoordinateMapper() dpiScale fix target
- `MathGaze/Views/PdfCanvas.xaml.cs` — Canvas click events must be added here; MouseDown (pointer events) routed to ViewModel

### Requirements
- `.planning/REQUIREMENTS.md` — GEOM-01 through GEOM-07, SYS-01 (all Phase 2 scope)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `MathGaze/Core/CoordinateMapper.cs`: `ScreenToPage(SKPoint)` converts canvas click → PDF coordinates for object storage; `PageToScreen(x, y)` converts stored PDF coords → canvas pixels for rendering. Update() is cheap — call on every zoom/scroll/page change.
- `MathGaze/ViewModels/MainViewModel.cs`: `IsPracticeMode` observable — geometry layer can observe this to show/hide measurement readouts in Phase 3. `ZoomFactor` and `ScrollOffsetY` property change notifications already trigger canvas repaint via PdfCanvasViewModel.
- `MathGaze/Styles/AppStyles.xaml`: `ToolTileStyle` (84×56 WPF units), all colour brushes (`BrushAccent`, `BrushSurface`, `BrushBorder`, `BrushInk`, `BrushInk2`, `BrushInk3`) — use these for right rail geometry controls.

### Established Patterns
- **MVVM with CommunityToolkit.Mvvm**: `[ObservableProperty]` and `[RelayCommand]` for all ViewModel properties and commands.
- **DI singleton**: All services and ViewModels registered as singletons in `App.xaml.cs`. New services (e.g., `IGeometryService`, `IUndoService`) follow the same pattern.
- **Canvas invalidation**: Raise `InvalidationRequested` event → `PdfCanvas.xaml.cs` calls `SkCanvas.InvalidateVisual()` on UI thread. All geometry rendering goes through `PdfCanvasViewModel.Paint()` — add geometry draw calls after the PDF bitmap draw.
- **Code-behind event wiring**: PaintSurface and SizeChanged events wired in code-behind (not XAML) to avoid SkiaSharp XAML temp-project resolution issues. Same pattern must apply to MouseDown/MouseMove event wiring for tool interactions.
- **Command pattern location**: `PdfCanvas.xaml.cs` receives WPF pointer events and should forward to `PdfCanvasViewModel` via method calls (not direct command binding, to avoid XAML SKElement binding issues).

### Integration Points
- `PdfCanvasViewModel.Paint(SKCanvas, int, int)`: Add geometry layer draw after line 138 (`canvas.DrawBitmap(_pageBitmap, destRect)`). A `GeometryLayerViewModel` (or similar) takes `SKCanvas` and renders all placed objects.
- `MathGaze/Views/ToolRail.xaml`: 6 stub buttons have no Command bindings. Phase 2 wires these to tool-activation commands in MainViewModel (or a new ToolViewModel).
- `MathGaze/Views/RightRailPlaceholder.xaml`: Replace with a selection-aware right rail control. The right rail must observe the currently selected geometry object and render the appropriate controls (SelectionCard + NudgeBlock + sub-point indicator + Delete).
- `MathGaze/Views/PdfCanvas.xaml.cs`: Add `MouseDown` and `MouseMove` event handlers in code-behind. Forward to PdfCanvasViewModel with `ScreenToPage`-converted coordinates.

</code_context>

<specifics>
## Specific Ideas

- "It's important that the student can position the start and end of the line — they are unlikely to get it accurate first time." — Endpoint sub-selection tap targets (D-04) exist specifically for this. Endpoint nudge is as important as initial placement.
- "Nudging each endpoint essentially gives you rotate." — Correct: nudging one endpoint of a line while the other stays fixed produces rotation. This is the intended behaviour for lining up geometry with printed angles/lines on the PDF.

</specifics>

<deferred>
## Deferred Ideas

- Full Pivot Picker from HANDOFF (start/mid/end adaptive pivot with SVG preview) — the sub-point tap-target model in D-04 covers the practical need; the full Pivot Picker UI can be added post-MVP if needed.
- Snap Orientation row (V/H/45°/Free buttons in right rail) — snap orientation guides are in GEOM-07 scope (snap to orientation), but the explicit snap-orientation toggle buttons from HANDOFF are deferred to Phase 3+ when the right rail has more room.
- Protractor CTA in right rail when line is selected ("Pick 2nd line" button) — part of Phase 3; right rail in Phase 2 shows only geometry editing controls.

</deferred>

---

*Phase: 02-geometry-core*
*Context gathered: 2026-05-02*
