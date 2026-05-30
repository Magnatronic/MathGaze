# Phase 7: UI improvements - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-30
**Phase:** 07-ui-improvements
**Areas discussed:** Mid-draw right rail, Settings panel, Protractor lock (PROT-04), Clear annotations

---

## Mid-draw right rail

| Option | Description | Selected |
|--------|-------------|----------|
| Drawing guide only | Show contextual hint card: "Line in progress — click 2nd point" with cancel button. No constraint controls. | ✓ |
| Guide + orientation constraints | H/V/45°/Free buttons in right rail during mid-draw, restoring removed snap functionality. | |

**User's choice:** Drawing guide only

---

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, clicking active tool cancels | Clicking the already-active tool resets draw state. | ✓ |
| Cancel button in right rail only | Only the right-rail card has a cancel. | |

**User's choice:** Yes, clicking active tool cancels

---

## Settings panel

| Option | Description | Selected |
|--------|-------------|----------|
| Dark mode toggle only | Single Light/Dark toggle. Small scope, high value for gaze student. | ✓ |
| Dark mode + accent colour | Dark mode plus 5 accent hue choices. | |
| Full preferences panel | Dark mode, accent colour, density, other settings. | |

**User's choice:** Dark mode toggle only

---

| Option | Description | Selected |
|--------|-------------|----------|
| In-window slide-over panel | Panel slides in from right edge, covers right rail. Grid 3 compatible. | ✓ |
| Toast-style inline toggle | Gear button simply toggles immediately — no panel needed. | |

**User's choice:** In-window slide-over panel

---

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, persist it | Save preference to app config; survives restarts. | ✓ |
| Session only | Resets to light mode on restart. | |

**User's choice:** Persist to app settings

---

## Protractor lock (PROT-04)

| Option | Description | Selected |
|--------|-------------|----------|
| Lock button — disables nudge only | Lock toggle in ProtractorPanel; nudge disabled, rotate/flip still work. | |
| Lock button — disables all | Full freeze: no nudge AND no rotate/flip. | |
| Not needed | — | ✓ |

**User's choice:** Not needed — PROT-04 excluded from Phase 7. Remains pending in REQUIREMENTS.md.

---

## Clear annotations

| Option | Description | Selected |
|--------|-------------|----------|
| Yes — clear current page only | Remove all geometry from current page. Undoable. | ✓ |
| Yes — clear all pages | Remove all geometry from all pages. | |
| Not needed | Undo handles this. | |

**User's choice:** Clear current page only

---

| Option | Description | Selected |
|--------|-------------|----------|
| Right rail bottom, above undo/redo | Always accessible from right rail. | ✓ |
| Top bar | Lives next to Open File / Export PDF. | |

**User's choice:** Right rail, above undo/redo footer

---

| Option | Description | Selected |
|--------|-------------|----------|
| Execute immediately, undoable | No confirm dialog. Fires as single undo entry. | ✓ |
| Two-click confirm in-rail | First click shows confirmation in right rail. | |

**User's choice:** Execute immediately, undoable

---

## Claude's Discretion

- Dark theme colour tokens
- Settings panel animation detail
- Whether "Clear page" uses danger styling
- Exact drawing guide card layout
- Fix for "Protrac" truncation in tool rail

## Deferred Ideas

- Orientation constraint buttons (H/V/45°/Free) during mid-draw
- Protractor lock (PROT-04)
- Accent colour selection
- Density settings (Comfortable/Spacious/XL)
