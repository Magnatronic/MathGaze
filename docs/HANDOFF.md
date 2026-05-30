# MathGaze — Handoff for AI Agent Build

This document is a brief for an AI coding agent (Claude Code, Cursor, etc.) to begin implementing the MathGaze application from the design exploration in this project.

---

## 1. What this project contains

- `index.html` — design exploration canvas (Split Rails artboards + supporting screens)
- `direction-splitrails.jsx` — the chosen layout direction (Split Rails)
- `additional-screens.jsx` — supporting flow states (snap, reflection draw, MCQ, magnifier, undo history, settings, empty)
- `shared.jsx` — design tokens, icons, protractor SVG, question canvas content
- `uploads/SPEC.md`, `uploads/INITIAL ROADMAP.md`, `uploads/TECH STACK.md` — original product specs

The HTML/JSX is **a visual specification, not the production codebase.** The target stack is .NET + WinUI 3 + Win2D (see `TECH STACK.md`).

---

## 2. Decisions locked in during this design phase

| Decision | Value |
|---|---|
| Layout | **Split Rails** — tools left, selection-aware adjustment right |
| Tool set (MVP) | Select · Point · Line · Circle · Protractor · Text · Mark — **7 tools** |
| Protractor | A tool, but with auto-placement: pick Protractor → click line 1 (defines baseline) → click line 2 (defines other arm). Lands on the intersection, no dragging. |
| Reflection | Verb on selection (line + shape), **not** a tool |
| Label | Folded into Text (auto-anchors when placed on object) |
| Draw | Deferred from MVP |
| Adjustment model | **Pivot Picker** (adaptive) + **Step Size** row (1 / 5 / 20 px) + **Snap Orientation** row (V / H / 45° / Free) |
| Rotate | ±1° / ±5° around active pivot |
| Modes | Exam (no measurements shown) / Practice (live readouts) — togglable, color-coded chip in top bar |
| Theme | Light + Dark, user-toggle |
| Protractor styles | 180° classic / 360° full / minimal (user choice) |
| Accent | Cobalt default; user choice of 5 hues |
| Density | Comfortable / Spacious / XL |
| Interaction rule | **Click-to-commit, never drag.** Two clicks max per primitive. |
| Tool placement | Edge-anchored (left + right rails). Canvas never has tools floating in middle. |

---

## 3. To hand this to an AI agent

### Step 1 — Make the design accessible

Either:
- **Export a PDF** of `index.html` (use the Save as PDF flow in the toolbar, or print to PDF from the browser) and share that, OR
- Share this whole project as a zip (Download project from the toolbar) so the agent can open the HTML directly.

The PDF is usually enough for the agent to understand the visual target.

### Step 2 — Give the agent these documents in this order

1. `uploads/SPEC.md` — what to build (feature list)
2. `uploads/TECH STACK.md` — how to build it (WinUI 3 + Win2D + Windows PDF API)
3. `uploads/INITIAL ROADMAP.md` — week-by-week plan
4. **This file** (`HANDOFF.md`) — design decisions that override the original specs where they conflict
5. The exported PDF / `index.html` — visual reference

### Step 3 — Tell the agent to build in this order

The roadmap in `INITIAL ROADMAP.md` is good. Adjust phase 7 to drop Reflection-as-tool and add Reflection-as-verb instead. The build order:

1. WinUI 3 shell + top bar + left tool rail (7 tools, static for now) + right rail placeholder
2. Win2D canvas integrated, render a PDF page as bitmap background
3. Object model: GeometryObject base + Point + Line + Selection
4. Two-click line creation, click-to-select, hit testing
5. Right rail becomes selection-aware. Render Pivot Picker (adaptive), Step row, Nudge pad, Rotate row
6. Snap system: endpoint snap + intersection snap + orientation snap (V/H/45°)
7. **Protractor tool with two-click placement** — entering Protractor mode arms the canvas to listen for two line picks. After the second pick, place the protractor at the intersection, baseline aligned with the first line. Centre/orientation can then be nudged ±1°/±5° around the active pivot.
8. Reflection-as-verb: select line + shape → contextual "Reflect" button → produce reflected polygon
9. Multiple choice: large hit zones over PDF answer regions, lock-answer button
10. Text tool with auto-anchor heuristic
11. Mark (highlighter) as separate tool
12. Settings panel + persistence + undo/redo + Exam/Practice mode toggle
13. Self-contained EXE packaging

### Step 4 — Acceptance criteria the agent should keep checking

- Every interactive target ≥ 56×56 px at 1× density (gaze accuracy floor)
- No drag gestures anywhere
- No tool action requires more than 2 clicks
- Protractor shows no numeric value in Exam mode
- Mode chip is visible at all times
- Selection state survives undo/redo
- App runs as self-contained EXE, no admin install

---

## 4. Open questions for the agent / next design pass

- Snap-orientation behaviour with non-default pivot — does "Vertical" rotate around pivot, or move pivot to align? (Current design: rotate around pivot.)
- Where does the on-screen math keyboard appear when Text is in math mode? (Not yet designed.)
- Calibration / gaze-software pairing screen — out of scope per user (gaze software handles itself), but a "first run" empty state may help.
- Save / resume format. JSON sidecar next to the PDF is the simplest.

---

That's it. The design is intentionally restrained and the tool count is small, which should make this realistically buildable on the roadmap timeline.
