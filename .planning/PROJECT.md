# MathGaze

## What This Is

A native Windows desktop app that lets eye-gaze students work through GCSE maths exam papers using only gaze-driven clicks — no drag gestures, no mouse, no keyboard. Students load a PDF, annotate it with geometry tools (lines, circles, protractor), select multiple-choice answers, and place text labels — all through Grid 3 / Smartbox driving standard Windows click events. The app starts as a tool built for one specific student and is designed to scale into a general assistive technology product.

## Core Value

A student can complete a GCSE geometry question — measuring angles, drawing lines of reflection, selecting answers — using only their eyes, without the app ever reducing the cognitive challenge of the maths itself.

## Requirements

### Validated

#### Core (Validated in Phase 1: Foundation)
- [x] User can load a PDF and navigate between pages
- [x] User can zoom in and out of the PDF
- [x] App runs as a self-contained EXE with no admin install required

#### Geometry Tools (Validated in Phase 2: Geometry Core — pending human re-test of GAP-11/12/13 fixes)
- [x] User can place a Point with one click
- [x] User can draw a Line (segment) with two clicks
- [x] User can draw a Circle (center → radius point) with two clicks
- [x] User can select any geometry object with one click
- [x] User can nudge a selected object using step controls (1px / 5px / 20px)
- [x] User can delete a selected object
- [x] User can undo and redo any action (Validated in Phase 2: Geometry Core)

### Active

#### Protractor
- [ ] User can activate Protractor mode, click two lines, and have the protractor auto-placed at their intersection with baseline aligned to the first line
- [ ] User can rotate the placed protractor ±1° and ±5° via right-rail controls
- [ ] User can flip the protractor between inner and outer scale
- [ ] User can lock the protractor position to prevent accidental movement
- [ ] Protractor displays a live angle readout in Practice Mode

#### Transformations
- [ ] User can select a line + shape and apply Reflection via a contextual button (verb, not a tool)

#### Text & Answers
- [ ] User can place a text box and type via Grid 3 (no in-app keyboard)
- [ ] User can click to select a multiple-choice answer (highlighted with visual tick)
- [ ] User can toggle or change their answer selection
- [ ] User can lock an answer to prevent accidental change

#### System
- [ ] Work is auto-saved to a JSON sidecar file alongside the PDF
- [ ] User can resume a previously saved session
- [ ] Practice Mode shows live measurements; an Exam Mode toggle hides them (top bar, always visible)

### Out of Scope

- **In-app keyboard** — Grid 3 handles all text input; MathGaze does not need one
- **Mark / Highlight tool** — useful but different from geometry tools; deferred to v2
- **Cross-platform (iPad, Mac)** — Windows-only by design; no architecture compromise for portability
- **Drag gestures** — gaze-incompatible; everything is click-to-commit
- **Auto-solving / AI assistance** — exam integrity; the app assists the student's thinking, not the answer
- **Real-time collaboration** — single-user, offline tool
- **Full math keyboard** — Grid 3 handles it
- **JCQ-compliant Exam Mode lockdown** — practice use drives v1; formal exam compliance is a v2 goal
- **Advanced constructions** (bisectors, loci, angle bisector) — Tier 3, post-MVP
- **Full transformation suite** (rotation, enlargement) — Tier 2, post-reflection MVP

## Context

The student uses Grid 3 (Smartbox) as their AAC / eye-gaze platform. Grid 3 drives all Windows input as standard pointer and keyboard events, which means MathGaze needs zero gaze-SDK integration — it just handles click events. Grid 3 also handles the on-screen keyboard, so MathGaze's text tool is a text box receiver only.

The design has been prototyped in JSX (`docs/direction-splitrails.jsx`) with a full Split Rails layout (tool nouns left, selection-aware verbs right, PDF canvas centre). Design tokens, icons, protractor SVG, and example question canvas are all in `docs/shared.jsx`. Screenshots are in `docs/screenshots/`. These are the visual source of truth for implementation.

The protractor insight from the design phase is critical and must be preserved: the protractor is not a thing you place — it is the consequence of picking two lines. The student selects two lines; the system infers and places the protractor. This collapses three actions (pick tool → place anchor → align) into two clicks on things they were already looking at.

## Constraints

- **Interaction**: No drag gestures. Every action is click-to-commit. Maximum 2 clicks per primitive. — Gaze accuracy requirement
- **Target size**: Every interactive element ≥56×56px at 1× density. — Gaze accuracy floor from HANDOFF
- **Deployment**: Self-contained EXE, no admin install, no runtime dependency on pre-installed components. — Runs on school machines
- **Platform**: Windows 10/11 only. No cross-platform scope. — Native stack commitment
- **Input abstraction**: All input treated as standard Windows pointer events. — Grid 3 compatibility
- **Tech stack**: .NET + WinUI 3 + Win2D + Windows PDF API (primary); WPF + SkiaSharp (fallback if WinUI 3 fails Phase 0 validation on exam machine). — Win2D gives hardware-accelerated 2D needed for protractor rendering
- **Rendering**: PDF as bitmap background layer; geometry as vector layer on top; UI overlay above both. — Layer separation is critical for performance and coordinate system correctness
- **Exam integrity**: App assists the student's process, never the answer. No auto-solving, no computed answers displayed in Exam Mode.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Split Rails layout (tools left, verbs right, canvas centre) | Tools stay stable; right rail adapts to what's selected; canvas has full focus | — Pending |
| Protractor as consequence of two line picks, not a standalone tool | Collapses pick→place→align into 2 clicks on existing objects; reduces cognitive load | — Pending |
| Reflection as verb on selection, not a tool | Reflection requires a line + a shape to be meaningful; making it contextual prevents misuse | — Pending |
| Grid 3 handles all text input — no in-app keyboard | Avoids duplicating what Grid 3 does well; keeps MathGaze focused on geometry | — Pending |
| Mark/Highlight deferred to v2 | Different interaction model from geometry tools; not blocking core geometry use | — Pending |
| Windows-only, native stack | Exam environments are Windows; native gives best latency and accessibility API access | — Pending |
| WinUI 3 validation as Phase 0 before any app code | WinUI 3 self-contained EXE on school machine is unvalidated; fallback to WPF + SkiaSharp if it fails | — Pending |
| 6 MVP tools: Select · Point · Line · Circle · Protractor · Text | Mark dropped from 7→6 for v1; these 6 cover all geometry question types in the spec | — Pending |
| Practice Mode drives v1; Exam Mode (JCQ-compliant) is v2 | No exam deadline driving v1; building it right matters more than building it locked down | — Pending |
| Auto-save to JSON sidecar next to PDF | Simplest possible persistence; no database, no cloud, works offline, recoverable | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-05-05 — Phase 02 complete: all geometry tools (Point, Line, Circle, Snap, Select, Nudge, Delete, Undo) shipped across 13 plans including 8 UAT gap-closure plans. 29/29 automated truths verified; 7 items pending human re-test. Phase 03 (Protractor) is next.*
