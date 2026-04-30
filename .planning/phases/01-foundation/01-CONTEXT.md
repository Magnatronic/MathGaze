# Phase 1: Foundation - Context

**Gathered:** 2026-04-30
**Status:** Ready for planning

<domain>
## Phase Boundary

Deployment validation confirmation + WPF shell + PDF rendering + CoordinateMapper. Goal: the app loads a GCSE exam PDF, displays it, allows page navigation, zoom, and canvas panning — all running from a self-contained EXE with no install step. No geometry tools yet. Creating posts and interactions are separate phases.

</domain>

<decisions>
## Implementation Decisions

### Deployment spike sequencing
- **D-01:** Deployment validation is a tail confirmation step, not a front-loaded spike gate. Technology quality drives sequencing. Build the full Phase 1 feature set first; confirm self-contained EXE publish works at the end.
- **D-02:** No separate "bare EXE test" plan as plan 1. The WPF shell itself (with PDF rendering wired up) is the first real artifact.

### Shell completeness
- **D-03:** Full 3-column skeleton at the end of Phase 1: TopBar + left tool rail (6 visual stub buttons, no behavior) + PDF canvas + empty right rail placeholder.
- **D-04:** The 6 stub buttons in the left rail represent the MVP tool set (Select, Point, Line, Circle, Protractor, Text) — visually present with icons, not wired to any behavior. Phase 2 fills in behavior without restructuring the window.
- **D-05:** TopBar is fully functional in Phase 1: MathGaze branding, filename display, Open/Close controls, mode toggle chip, zoom (−/+/fit-page), page navigation (prev/counter/next).

### PDF library
- **D-06:** Use Docnet.Core. Wraps PDFium (the same engine as Chrome). Actively maintained, clean async API, bundles the PDFium native DLL via NuGet. No alternate library evaluation needed.

### Scroll interaction
- **D-07:** Wire up the ScrollRail (Up / Down / Page-Up / Page-Down click buttons) in Phase 1 alongside zoom. Panning and zooming belong together — zoom without scroll is unusable when a page is larger than the viewport.
- **D-08:** Scroll buttons use the same click-to-commit model as all other interactions. No drag gestures, no mouse wheel required.

### CoordinateMapper scope
- **D-09:** CoordinateMapper translates between two coordinate spaces: PDF points → screen pixels (for rendering) and screen pixels → PDF points (for hit-testing geometry clicks in Phase 2). Both directions built and unit-tested in Phase 1.
- **D-10:** Unit tests cover zoom 0.5×/1×/1.5×/2× × DPI 96/120/144/192 (100/125/150/200%) — per Phase 1 success criteria.

### Claude's Discretion
- Exact zoom step size (10%, 25%, or other increment) — keep it gaze-friendly (never more than 3 clicks to reach a useful zoom range)
- Default zoom level on open (fit-page is the sensible default)
- Stub button icon set for Phase 1 left rail — use icons from shared.jsx
- Right rail placeholder visual (empty state, no content required)
- How the ScrollRail scroll amount maps to canvas offset (e.g., small = 50px, page = viewport height)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Layout and design
- `docs/direction-splitrails.jsx` — TopBar component (zoom controls, page navigation, mode pill, file controls), ScrollRail component, 3-column window structure, all design tokens
- `docs/shared.jsx` — Design tokens (T object: colors, fonts, spacing), icon set, shared primitives

### Design decisions locked before build
- `docs/HANDOFF.md` — Interaction rules (click-to-commit, ≥56×56px targets, no drag), tool set, mode behavior, theme, density — these override original specs where they conflict

### Requirements and constraints
- `.planning/REQUIREMENTS.md` — CORE-01 (open PDF), CORE-02 (page navigation), CORE-03 (zoom), CORE-04 (self-contained EXE); traceability table maps these to Phase 1
- `.planning/PROJECT.md` — Constraints section: ≥56×56px targets, no drag, Grid 3 pointer events only, no flyouts/popups/secondary HWNDs, self-contained EXE

### Feature specification
- `SPEC.md` — Core object model (Section 4), geometry tool interactions (Section 5) — Phase 1 need not implement these but CoordinateMapper must support them

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- No source code exists yet — this is a greenfield project.
- `docs/direction-splitrails.jsx` and `docs/shared.jsx` are JSX visual specifications, not production code. They are the design reference; the WPF implementation re-creates them in XAML + C#.

### Established Patterns
- None yet — Phase 1 establishes all foundational patterns (MVVM with CommunityToolkit.Mvvm, DI with Microsoft.Extensions.DependencyInjection, SkiaSharp canvas rendering).

### Integration Points
- CoordinateMapper is the single integration point between Phase 1 and all subsequent phases. Every geometry tool (Phase 2+) calls CoordinateMapper.ScreenToPage() to convert click coordinates into PDF-space positions. Build it right in Phase 1.
- Docnet.Core PDF rendering produces a bitmap per page; SkiaSharp paints that bitmap as the canvas background layer. Geometry objects are drawn as a vector layer on top in Phase 2+.

</code_context>

<specifics>
## Specific Ideas

- "I'm not worried about the EXE deployment. Using the best technology for the app is most important." — Deployment confirmation is a last step, not a gate. Don't sacrifice architectural quality for deployment-spike-first sequencing.
- The design TopBar shows the filename in a monospace font ("aqa_paper2_2023.pdf") alongside an Open folder icon (36×36px) and a Close icon — the file controls are part of Phase 1's fully functional TopBar.
- Page counter shows "7 / 22" style — current page and total page count, both from Docnet.Core document metadata.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 01-foundation*
*Context gathered: 2026-04-30*
