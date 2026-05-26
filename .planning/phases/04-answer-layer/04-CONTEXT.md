# Phase 4: Answer Layer - Context

**Gathered:** 2026-05-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Text label placement (click + clipboard paste, nudge, delete, undo) and JSON sidecar auto-save/restore. Students can place text labels on the PDF canvas and resume their work across sessions automatically.

**Scope reduction from original roadmap:** ANS-01, ANS-02, ANS-03 (MCQ answer selection) are deferred to v2. Students use the existing Circle tool to mark answer options in v1. Phase 4 requirements in scope: TEXT-01, TEXT-02, SYS-02, SYS-03.

</domain>

<decisions>
## Implementation Decisions

### Text tool — input model
- **D-01:** Clipboard-paste-on-placement. Student composes text in Grid 3, copies it, activates Text tool, clicks canvas → the clipboard content becomes a `TextObject` at that PDF-space position. No in-app text editing state. No WPF TextBox needed.
- **D-02:** If clipboard is empty or contains non-text when the student clicks, show a brief status toast: "Copy text first, then click to place." No TextObject is created. Same toast pattern as the protractor parallel-lines error.
- **D-03:** Text is rendered by SkiaSharp alongside other geometry objects. Once placed, text is immutable — to change it, student deletes the TextObject and re-places with corrected clipboard content.

### Text tool — object behaviour
- **D-04:** `TextObject` stores: `ContentText` (string from clipboard), `XPt` / `YPt` (PDF-space coordinates — same D-10 pattern as all geometry objects). No lock state for text boxes.
- **D-05:** Placing a text box = one undo entry. Each nudge press = one undo entry. Deleting = one undo entry. Full undo/redo participation consistent with D-08 (Phase 2).
- **D-06:** When a TextObject is selected, the right rail shows the standard Nudge block (1/5/20px UDLR) + Delete button — same controls as other objects. No text-specific right-rail controls needed.
- **D-07:** `TextObject` extends `GeometryObject` and is stored in `IGeometryService.Objects` alongside Point/Line/Circle/Protractor. Placed via `PlaceObjectCommand`.

### MCQ answer selection — deferred
- **D-08:** ANS-01 (click to select), ANS-02 (toggle), ANS-03 (lock answer) are deferred to v2. In v1, students circle answer options using the existing Circle tool. No `AnswerObject` or `AnswerMode` in Phase 4.

### Session persistence — what gets saved
- **D-09:** The JSON sidecar saves: all geometry objects (Point, Line, Circle, Protractor, TextObject — full state required to reconstruct each), and the current page number. Zoom level and scroll position are NOT saved; they reset to defaults on reopen.
- **D-10:** Sidecar filename convention: `{pdf-filename}.mathgaze.json` placed in the same directory as the PDF. Example: `aqa_paper2_2023.pdf` → `aqa_paper2_2023.pdf.mathgaze.json`.
- **D-11:** Use `System.Text.Json` (inbox, no NuGet dependency). Each geometry object type serializes its discriminated type name plus all fields needed to reconstruct it.

### Session persistence — trigger and restore
- **D-12:** Auto-save triggers on every `IGeometryService.ObjectsChanged` event — the same event that triggers canvas repaint. No debounce. Geometry files are small; save is fast.
- **D-13:** On PDF open: check for the sidecar file at `{pdf-path}.mathgaze.json`. If found, silently deserialize and load all geometry objects + navigate to the saved page number. No prompt. If sidecar is missing or corrupt, open clean.
- **D-14:** Page navigation (user changes page) also triggers a sidecar save so the last-visited page is always persisted.

### PDF export
- **D-15:** PDF export (annotations baked into a new PDF) is v2 scope (`EXAM-V2-02`). In v1, the JSON sidecar is the persistence mechanism. Students can screen-capture the annotated view as a workaround.

### Claude's Discretion
- TextObject rendering: font family (T.mono from design tokens), font size, text colour (T.ink or accent), background/border treatment while selected
- SkiaSharp text rendering API: use `SKFont`-based `DrawText` overload (not deprecated `SKPaint.TextSize` API — per Phase 3 pattern)
- Hit-test tolerance for TextObject (recommend 8px around the rendered text bounding box)
- Exact JSON schema structure for sidecar (polymorphic type discriminator strategy for System.Text.Json)
- Error handling for corrupt/unreadable sidecar (log and open clean)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Design system and interaction rules
- `docs/direction-splitrails.jsx` — Split Rails layout, right rail controls, NudgeBlock component, tool rail stub buttons, design tokens reference
- `docs/shared.jsx` — Design tokens (T object: colours, fonts including T.mono, spacing), icon set, ToolTileStyle
- `docs/HANDOFF.md` — Locked interaction rules: ≥56×56px targets, click-to-commit, no drag, Grid 3 pointer events only, no flyouts

### MCQ design reference (v2 planning only)
- `docs/additional-screens.jsx` — `MCQScreen` function: shows the MCQ UI pattern for when ANS-01/02/03 are implemented in v2. Do not implement in Phase 4.

### Phase foundation
- `.planning/phases/02-geometry-core/02-CONTEXT.md` — D-08 (per-click undo), D-09 (command pattern via ExecuteCommand), D-10 (PDF-space coordinate storage), established RailButtonStyle/StepButtonStyle patterns
- `.planning/phases/03-protractor/03-CONTEXT.md` — SKFont-based DrawText pattern (Phase 3 migrated from deprecated API), SKPaint cache pattern (readonly fields, no per-frame allocation)

### Requirements
- `.planning/REQUIREMENTS.md` — TEXT-01, TEXT-02 (text tool), SYS-02 (auto-save), SYS-03 (session restore). Note: ANS-01/02/03 explicitly deferred to v2 by user decision in this discuss-phase session.

### Key source files for Phase 4 implementation
- `MathGaze/Services/IGeometryService.cs` — service interface; `ObjectsChanged` event hooks for auto-save trigger
- `MathGaze/Core/Geometry/GeometryObject.cs` — abstract base; TextObject extends this
- `MathGaze/Core/Commands/PlaceObjectCommand.cs` — use for TextObject placement
- `MathGaze/Core/Commands/NudgeObjectCommand.cs` — use for TextObject nudge (same as other objects)
- `MathGaze/ViewModels/ToolViewModel.cs` — add `ToolMode.Text` to enum; add `ActivateText` command and click handler
- `MathGaze/ViewModels/GeometryLayerViewModel.cs` — add TextObject draw case

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `IGeometryService.ExecuteCommand()`: TextObject placement and nudge go through this — undo/redo automatic
- `IGeometryService.ObjectsChanged` event: subscribe in a new `SessionService` to trigger JSON save
- `PlaceObjectCommand` / `NudgeObjectCommand` / `DeleteObjectCommand`: all reusable for TextObject with no changes
- `GeometryHitTester`: extend with `TryHitText` for selection (text bounding box hit test)
- `GeometryLayerViewModel`: add TextObject draw case alongside existing Point/Line/Circle/Protractor draw cases
- `ToolViewModel.HandleCanvasClick`: add `(ToolMode.Text, DrawState.Idle)` case — read clipboard, place or toast
- `RailButtonStyle`, `ToolTileStyle`, `NudgeBlock` (right rail): reuse for Text tool button and right-rail selection panel
- `StatusMessage` in `ToolViewModel`: reuse for "Copy text first, then click to place" toast

### Established Patterns
- **PDF-space coordinates (D-10)**: `TextObject.XPt`, `TextObject.YPt` stored in PDF points; `CoordinateMapper.PageToScreen()` used at render time
- **Command pattern (D-09)**: all TextObject mutations through `IGeometryService.ExecuteCommand()`
- **SKFont-based text rendering**: use `SKFont` + `DrawText` overload (Phase 3 established this; avoids CS0618 warning from deprecated `SKPaint.TextSize` API)
- **SKPaint cache**: declare readonly `SKPaint` fields in `GeometryLayerViewModel` for text rendering; no per-frame allocation

### Integration Points
- `ToolRail.xaml`: wire existing Text stub button to `ActivateTextCommand`
- `GeometryLayerViewModel.Paint()`: add TextObject draw case after existing object draw calls
- `IGeometryService.ObjectsChanged` → new `ISessionService` (or `SessionService` singleton) serializes all objects + page number to JSON sidecar
- `MainViewModel.OpenPdfCommand` (or `DocnetPdfService.LoadAsync`): after PDF opens, check for sidecar and restore if found
- `MainViewModel.CurrentPage`: persisted to sidecar; restored on load

</code_context>

<specifics>
## Specific Ideas

- "I envisaged just being able to paste text onto the canvas. Keep it simple." — The clipboard-paste model (D-01) directly implements this intent. No WPF TextBox, no keyboard focus management.
- "Screen space is limited to have Grid 3 running at the same time and thought this might be easier." — Confirmed rationale for the paste model: Grid 3 and MathGaze coexist on screen; Grid 3 handles all text composition, MathGaze just receives the clipboard result.
- The MCQ design in `docs/additional-screens.jsx` (MCQScreen) shows the target v2 experience with a prominent "Lock answer B" button. Keep this design as the reference for when ANS-01/02/03 are implemented in v2.

</specifics>

<deferred>
## Deferred Ideas

- **ANS-01: MCQ click-to-select** — user decision: defer to v2. Students use Circle tool in v1. Design reference: `MCQScreen` in `docs/additional-screens.jsx`.
- **ANS-02: MCQ toggle selection** — deferred with ANS-01.
- **ANS-03: MCQ lock answer** — deferred with ANS-01.
- **EXAM-V2-02: PDF export** — annotated PDF export deferred to v2. JSON sidecar is v1 persistence; screenshot is the v1 workaround for a paper record.
- **PROT-04: Protractor lock toggle** — carried forward from Phase 3 deferred list. Still v2.

</deferred>

---

*Phase: 04-answer-layer*
*Context gathered: 2026-05-26*
