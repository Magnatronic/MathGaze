# Phase 1: Foundation - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-30
**Phase:** 01-foundation
**Areas discussed:** Deployment spike sequencing, Shell completeness, PDF library selection, Scroll interaction model

---

## Deployment spike sequencing

| Option | Description | Selected |
|--------|-------------|----------|
| Spike first | Minimal WPF EXE (empty window), test USB boot on school machine before writing features | |
| Features first, deploy at end | Build all Phase 1 features, validate self-contained EXE at the end | ✓ |
| Parallel | Spike + features simultaneously on two machines | |

**User's choice:** "I'm not worried about the exe deployment. Using the best technology for the app is most important."
**Notes:** Deployment validation is a tail confirmation, not a front-loaded gate.

---

## Shell completeness

| Option | Description | Selected |
|--------|-------------|----------|
| Full 3-column skeleton | TopBar + left rail (6 stub buttons) + PDF canvas + empty right rail placeholder | ✓ |
| Minimal — canvas + TopBar only | No side rails in Phase 1; Phase 2 adds 3-column structure | |

**User's choice:** Full 3-column skeleton.
**Notes:** 6 stub buttons in left rail (Select, Point, Line, Circle, Protractor, Text) — visual only, no behavior. Phase 2 fills in without restructuring.

---

## PDF library selection

| Option | Description | Selected |
|--------|-------------|----------|
| Docnet.Core | Actively maintained, clean async API, PDFium native DLL via NuGet | ✓ |
| PDFiumSharp | Also maintained, older API style | |
| Research spike | Compare both with evidence before committing | |

**User's choice:** Docnet.Core.
**Notes:** No research spike needed — Docnet.Core selected with confidence.

---

## Scroll interaction model

| Option | Description | Selected |
|--------|-------------|----------|
| Wire up ScrollRail in Phase 1 | Up/Down/PageUp/PageDown click buttons wired alongside zoom | ✓ |
| Defer scroll to Phase 2 | Phase 1 proves zoom only; panning added with geometry canvas | |

**User's choice:** Wire up ScrollRail in Phase 1.
**Notes:** Zoom and panning belong together — zoomed-in PDFs must be navigable.

---

## Claude's Discretion

- Zoom step size and default zoom level
- Stub button icon choice
- Right rail placeholder visual
- ScrollRail scroll distance mapping to canvas offset
