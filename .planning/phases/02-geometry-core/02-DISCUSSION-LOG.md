# Phase 2: Geometry Core - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-02
**Phase:** 02-geometry-core
**Areas discussed:** Ghost preview, Endpoint nudge model, Undo granularity

---

## Ghost Preview (2-click drawing)

| Option | Description | Selected |
|--------|-------------|----------|
| Full ghost + hint | Dashed preview line/arc follows cursor + bottom toast "Click 2nd point". Matches design spec. | ✓ |
| Anchor dot only | Just a filled dot at click 1. No preview follows the cursor. | |
| Ghost no toast | Dashed preview follows cursor, but no bottom status bar. | |

**User's choice:** Full ghost + hint
**Notes:** Best feedback for gaze students — they can see exactly where the line/circle will land before committing click 2. Design spec (`additional-screens.jsx` ReflectionDrawing) already shows this pattern.

---

## Pivot Picker Scope / Endpoint Nudge Model

| Option | Description | Selected |
|--------|-------------|----------|
| Whole-object translate | Nudge moves entire object. No sub-point selection in Phase 2. | |
| Full Pivot Picker | Right rail shows start/mid/end pivot options per the HANDOFF spec. | |
| Endpoint-only nudge | Student picks which endpoint to move via tap targets on canvas. | ✓ (adapted) |

**User's choice:** "Option 1 is good, but it's important that the student can position the start and end of the line. They are unlikely to get it accurate first time. Maybe only 3 is needed for the line as nudging each essentially gives you rotate. Not sure."

**Resolved to:** Two sub-point tap targets on the canvas when a Line is selected (both endpoints become ≥56×56px tap targets). Tapping one sub-selects it; UDLR nudges that endpoint only. Tapping elsewhere keeps line selected but clears sub-selection (whole-object translate). Same pattern for Circle: center + edge point as tap targets.

**Notes:** User correctly identified that endpoint nudge ≈ rotation (one end fixed, other moves). This is intentional for GCSE geometry — students align lines with printed angles by anchoring one end and adjusting the other.

---

## Undo Granularity

| Option | Description | Selected |
|--------|-------------|----------|
| Per-click undo | Each nudge press = 1 undo entry. Simple and predictable. | ✓ |
| Time-window batching | Rapid nudges within ~2 seconds collapse into 1 undo. | |

**User's choice:** Per-click undo
**Notes:** Simple and matches user expectation. A student doing 15 nudges can undo each step. Redo restores forward. No timer complexity.

---

## Claude's Discretion

- Hit-test tolerance around lines (8–10px recommended)
- Snap proximity threshold (20px recommended)
- Exact visual style for ghost preview and committed geometry objects
- Whether orientation snap guides show a faint guide line or just affect snap behaviour

## Deferred Ideas

- Full Pivot Picker UI (start/mid/end adaptive with SVG preview) — practical need met by endpoint tap targets
- Snap Orientation row buttons in right rail — deferred to Phase 3+
- Protractor CTA in right rail when line is selected — Phase 3
