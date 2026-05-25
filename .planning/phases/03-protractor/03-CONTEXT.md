# Phase 3: Protractor - Context

**Gathered:** 2026-05-25
**Status:** Ready for planning

<domain>
## Phase Boundary

Place a protractor on the PDF canvas by clicking two existing lines — the protractor auto-positions at their intersection, with baseline aligned to the first line clicked. After placement, the student can rotate it (±1°/±5°), flip between inner/outer scale, switch between 180° and 360° style, and move it via nudge. In Practice Mode a live angle readout renders inside the protractor; toggling to Exam Mode hides it. The mode toggle chip is always visible in the top bar.

No reflection, no text boxes, no MCQ, no auto-save in this phase (Phase 4).

</domain>

<decisions>
## Implementation Decisions

### Line selection interaction
- **D-01:** Protractor tool uses a two-state state machine: click line 1 (sets baseline) → click line 2 (places protractor). On click 1, the selected line highlights (cobalt accent, same style as regular selection) and status shows "Click 2nd line". ToolMode adds `Protractor`; DrawState reuses `Idle` / `AnchorPlaced` (AnchorPlaced = line 1 selected).

### Parallel lines edge case
- **D-02:** If the two selected lines are parallel (no intersection), show an error toast: "Lines are parallel — pick two non-parallel lines" and reset to Idle (click 1 cleared). Student must start again.

### Off-screen intersection
- **D-03:** If the mathematical intersection exists but falls outside the visible canvas area, calculate the true intersection in PDF coordinates and clamp the protractor center to the nearest visible canvas edge. The baseline angle is still aligned to line 1. The student can then nudge it into full view.

### Protractor size
- **D-04:** Default radius stored in PDF-space points (not screen pixels), so the protractor scales visually with zoom — same as all other geometry objects (D-10 from Phase 2). Planner to determine the default PDF-space radius (aim for a visually comfortable size at 1× zoom, roughly equivalent to 150px at zoom=1).
- **D-05:** No resize control. The student zooms the PDF view if they need the protractor to appear larger. No resize button in the right rail.

### ProtractorObject model
- **D-06:** `ProtractorObject` extends `GeometryObject`. Fields: `CenterXPt`, `CenterYPt` (PDF coords, nudge-able), `BaselineAngleDeg` (angle from line 1, degrees), `RotationOffsetDeg` (user rotation, starts at 0), `IsFlipped` (bool, false = inner scale), `Style` (enum: Classic180 | Full360).
- **D-07:** No lock state in v1 — PROT-04 (lock toggle) deferred by user decision.

### Right rail controls for protractor
- **D-08:** When a protractor is selected, the right rail shows:
  1. Rotate block: four buttons — `−5°`, `−1°`, `+1°`, `+5°` (≥56×56px each, standard RailButtonStyle)
  2. Flip button: "Flip scale" — toggles `IsFlipped` between inner (0→180° L→R) and outer (180→0° L→R)
  3. Style toggle: "Classic 180° / Full 360°" — two-button group, active style highlighted cobalt (StepButtonStyle pattern)
  4. Nudge block: same step sizes (1/5/20px) + UDLR pad as other objects — moves `CenterXPt`/`CenterYPt`
  5. Delete button (DangerButtonStyle, same as other objects)
  6. Undo/Redo (existing, always present)
- **D-09:** No lock toggle in v1 (PROT-04 deferred).

### Nudge behaviour
- **D-10:** UDLR nudge moves the protractor center in PDF-space (same as NudgeObjectCommand for PointObject). Step sizes 1/5/20px. Rotation does not change on nudge.

### Angle readout (Practice Mode)
- **D-11:** The readout shows the angle value a student would read off a physical protractor at the current orientation — i.e., where the second arm crosses the protractor scale. This is computed from `BaselineAngleDeg + RotationOffsetDeg` and the angle between the two source lines.
- **D-12:** Readout renders inside the protractor (arc + text, same as `shared.jsx` `measuring` prop pattern). It is NOT in the right rail.
- **D-13:** Readout updates live on every rotation button press (each ±1°/±5° updates the value immediately).
- **D-14:** In Exam Mode (`IsPracticeMode = false`), the readout is not rendered. The protractor arc and scale marks are still drawn; only the numeric value and arc indicator are hidden. This is enforced in `GeometryLayerViewModel` by checking `IsPracticeMode` before rendering the readout.

### Practice/Exam mode chip
- **D-15:** `IsPracticeMode` already exists in `MainViewModel`. The mode chip in the top bar uses the `ModePill` design from `docs/additional-screens.jsx` (color-coded: cobalt for Practice, neutral for Exam). `ToggleModeCommand` is already wired. Phase 3 adds the visual chip to `TopBar.xaml` and wires `IsPracticeMode` binding.

### Claude's Discretion
- Exact default radius in PDF points (target: visually ~150px at zoom=1.0)
- Intersection math implementation (line-line intersection formula)
- Exact "clamp to canvas edge" clamping logic for off-screen intersection case
- Toast/error display mechanism (reuse StatusMessage or a separate brief notification)
- SkiaSharp rendering details for protractor arc, scale marks, and readout text
- Undo entries: each rotate press = one undo entry (consistent with per-click undo, D-08 Phase 2)

### Deferred from v1
- **PROT-04 Lock toggle** — user decision: defer. The spec requirement exists but will not be implemented in Phase 3. Note for Phase 4 or v2.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Protractor design
- `docs/shared.jsx` — `Protractor` SVG component (arc rendering, scale marks, tick lengths at 1°/5°/10°/30°, label positions, `measuring` readout prop), `ModePill` component (Practice/Exam chip)
- `docs/additional-screens.jsx` — `ProtractorPlacing` function: ghost protractor during placement, snap ring + hint line pattern, hint bubble text
- `docs/HANDOFF.md` §2 — Locked design decisions: auto-placement at intersection, rotate ±1°/±5°, protractor styles (180°/360°/minimal), mode chip always visible

### Requirements
- `.planning/REQUIREMENTS.md` — PROT-01 through PROT-06, SYS-04, SYS-05 (all Phase 3 scope). Note: PROT-04 (lock) deferred to v2 by explicit user decision.

### Phase 2 foundation
- `.planning/phases/02-geometry-core/02-CONTEXT.md` — D-08 (per-click undo), D-09 (command pattern), D-10 (PDF-space coords), established RailButtonStyle/StepButtonStyle patterns
- `MathGaze/Core/Geometry/GeometryObject.cs` — abstract base class to extend for ProtractorObject
- `MathGaze/ViewModels/ToolViewModel.cs` — ToolMode enum (add `Protractor`), DrawState machine (reuse `AnchorPlaced`)
- `MathGaze/ViewModels/MainViewModel.cs` — `IsPracticeMode` property and `ToggleModeCommand` already present
- `MathGaze/ViewModels/GeometryLayerViewModel.cs` — rendering layer to extend with protractor draw
- `MathGaze/Core/SnapEngine.cs` — existing snap infrastructure; protractor placement uses line hit-test, not snap

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ToolViewModel.HandleCanvasClick`: extend `switch (ActiveTool, DrawState)` with `(ToolMode.Protractor, DrawState.Idle)` and `(ToolMode.Protractor, DrawState.AnchorPlaced)` cases — same pattern as Line/Circle
- `IGeometryService.Objects`: protractor objects stored alongside Point/Line/Circle — no architecture change
- `GeometryHitTester`: extend with `TryHitProtractor` for selection click testing
- `GeometryLayerViewModel`: add protractor draw case; check `IsPracticeMode` for readout rendering
- `RailButtonStyle`, `StepButtonStyle`, `DeleteButtonStyle`, `RailButtonDangerStyle` in `AppStyles.xaml` — reuse for all protractor right-rail controls
- `IsPracticeMode` in `MainViewModel`: already data-bindable; observe in `GeometryLayerViewModel` to toggle readout

### Established Patterns
- **PDF-space coordinate storage (D-10):** ProtractorObject stores `CenterXPt`, `CenterYPt` in PDF points
- **Command pattern (D-09):** `RotateProtractorCommand`, `FlipProtractorCommand`, `StyleProtractorCommand`, `NudgeObjectCommand` (reuse existing) go through `IGeometryService.ExecuteCommand()`
- **Code-behind event wiring:** ToolRail Protractor button wired in code-behind (not XAML), same as existing tool buttons
- **SKPaint cache:** declare readonly SKPaint fields in GeometryLayerViewModel, never allocate per-frame

### Integration Points
- `TopBar.xaml`: add `ModePill` equivalent (WPF toggle chip bound to `IsPracticeMode`)
- `ToolRail.xaml`: wire existing Protractor stub button to `ActivateProtractorCommand`
- `MathGaze/Core/GeometryMath.cs`: add `LineLineIntersection(LineObject, LineObject)` static method
- `RightRail.xaml`: add a `ProtractorPanel` section (visible when `SelectedObject is ProtractorObject`)

</code_context>

<specifics>
## Specific Ideas

- "The protractor is not a thing you place — it is the consequence of picking two lines." (PROJECT.md) — this mental model must be preserved in the UX flow. No separate placement step.
- The ghost protractor during placement (from `ProtractorPlacing` in additional-screens.jsx) shows the protractor at 0.5 opacity at the cursor position while the student is selecting the second line — this ghost should update live as the cursor moves over the second line before committing.
- The `measuring` prop in the `Protractor` SVG renders as: small arc from baseline to the measured angle + numeric text at the midpoint of that arc. The SkiaSharp equivalent is an arc + `DrawText` call.

</specifics>

<deferred>
## Deferred Ideas

- **PROT-04 Lock toggle** — "User can lock the protractor position to prevent accidental nudge." Explicitly deferred from v1 by user decision. Add to Phase 4 or v2 backlog.
- **Protractor resize control** — User decided no resize in v1; student zooms PDF view instead.

</deferred>

---

*Phase: 03-protractor*
*Context gathered: 2026-05-25*
