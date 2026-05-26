# Phase 4: Answer Layer - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-26
**Phase:** 04-answer-layer
**Areas discussed:** Text box input model, MCQ answer selection interaction, Session persistence scope, Text box editing lifecycle

---

## Text box input model

| Option | Description | Selected |
|--------|-------------|----------|
| WPF TextBox overlaid on canvas | Grid 3 keyboard events go to WPF control; text editing happens natively | |
| SkiaSharp text with manual key capture | Canvas-consistent but requires reimplementing caret/backspace/etc. | |
| Grid 3 types into WPF TextBox | Grid 3 generates key events into focused WPF TextBox | |
| Clipboard-paste-on-placement | Compose in Grid 3, copy, click canvas to place | ✓ |

**User's choice:** Clipboard-paste-on-placement  
**Notes:** "I envisaged just being able to paste text onto the canvas. Keep it simple." Screen space is limited when Grid 3 is running simultaneously — composing in Grid 3 and pasting keeps MathGaze focused on placement, not editing. No WPF TextBox needed, no keyboard focus juggling.

### Empty clipboard behaviour

| Option | Description | Selected |
|--------|-------------|----------|
| Show toast and do nothing | "Copy text first, then click to place" | ✓ |
| Place empty placeholder box | Creates [text] placeholder | |

**User's choice:** Toast and do nothing — consistent with protractor parallel-lines error pattern.

---

## MCQ answer selection interaction

| Option | Description | Selected |
|--------|-------------|----------|
| Click anywhere — tick appears there | Freeform tick marker at click position | |
| Student pre-defines answer zones | 2-click zone definition before selecting | |
| Scrap MCQ / use Circle tool | Students circle answers with existing Circle tool | ✓ |
| Keep MCQ but simplify to tick drop | Click to drop a ✓ glyph wherever student clicks | |

**User's choice:** Scrap MCQ entirely — "I think the circle will work if something needs to be selected. Scrap it completely."  
**Notes:** ANS-01, ANS-02, ANS-03 deferred to v2. Phase 4 scope reduced to Text tool + save/restore. The `MCQScreen` design in `docs/additional-screens.jsx` remains the v2 implementation reference.

---

## Session persistence scope

### What to save

| Option | Description | Selected |
|--------|-------------|----------|
| Geometry + page number | Saves objects and last-visited page; zoom/scroll reset to defaults | ✓ |
| Geometry + page + zoom + scroll | Full view state restore | |

**User's choice:** Geometry + page number — simpler, avoids potentially disorienting zoom state on restore.

### Save trigger

| Option | Description | Selected |
|--------|-------------|----------|
| After every ObjectsChanged event | Per-command save, no debounce | ✓ |
| Debounced 300ms after last change | Batches rapid nudge presses | |

**User's choice:** Every ObjectsChanged — consistent with per-click undo model; geometry files are small.

### Session restore

| Option | Description | Selected |
|--------|-------------|----------|
| Auto-detect and silently restore | Check for sidecar on PDF open, load automatically | ✓ |
| Ask the student whether to restore | Show a dialog | |

**User's choice:** Silent auto-restore — "1 seems sensible."

### PDF export clarification

**User's question:** Can work be saved as a PDF or printed?  
**Decision:** PDF export (EXAM-V2-02) is v2 scope. JSON sidecar + screenshot is sufficient for v1. User confirmed: "Fine for v1 — JSON sidecar + screenshot is enough."

---

## Text box editing lifecycle

This area was largely determined by the clipboard-paste model decided in area 1.

| Option | Description | Selected |
|--------|-------------|----------|
| Full undo/redo participation | Place, nudge, delete each = one undo entry | ✓ |
| No undo for text boxes | Commit-only; delete and re-place | |

**User's choice:** Full undo/redo — consistent with D-08 (Phase 2 per-click undo model).  
**Notes:** Lifecycle is: Place (clipboard read) → Select → Nudge/Delete. No editing state. Text is immutable once placed.

---

## Claude's Discretion

- TextObject visual rendering (font, size, colour, selected state styling)
- SkiaSharp `SKFont`-based DrawText API (following Phase 3 pattern)
- Hit-test tolerance around text bounding box
- JSON schema structure for polymorphic geometry object serialization
- Sidecar filename convention confirmed: `{pdf-filename}.mathgaze.json`

## Deferred Ideas

- ANS-01/02/03 (MCQ answer selection) — deferred to v2 by explicit user decision
- EXAM-V2-02 (PDF export) — deferred to v2
- PROT-04 (protractor lock) — carried forward from Phase 3 deferral
