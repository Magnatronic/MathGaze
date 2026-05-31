# MathGaze

## What This Is

A native Windows desktop app that lets eye-gaze students work through GCSE maths exam papers using only gaze-driven clicks — no drag gestures, no mouse, no keyboard. Students load a PDF, annotate it with geometry tools (lines, circles, protractor), measure pre-drawn angles, export annotated PDFs for submission, and place text labels — all through Grid 3 / Smartbox driving standard Windows click events. Built for one specific student; designed to scale into a general assistive technology product.

## Core Value

A student can complete a GCSE geometry question — measuring angles, drawing lines of reflection, selecting answers — using only their eyes, without the app ever reducing the cognitive challenge of the maths itself.

## Requirements

### Validated

#### Core (v1.0)
- ✓ User can load a PDF and navigate between pages — v1.0
- ✓ User can zoom in and out of the PDF — v1.0
- ✓ App runs as a self-contained EXE with no admin install required — v1.0

#### Geometry Tools (v1.0)
- ✓ User can place a Point with one click — v1.0
- ✓ User can draw a Line (segment) with two clicks — v1.0
- ✓ User can draw a Circle (center → radius point) with two clicks — v1.0
- ✓ User can select any geometry object with one click — v1.0
- ✓ User can nudge a selected object using step controls (1px / 5px / 20px) — v1.0
- ✓ User can delete a selected object — v1.0
- ✓ User can undo and redo any action — v1.0
- ✓ Snap feedback appears and commits to endpoints, intersections, and orientation guides — v1.0

#### Protractor (v1.0)
- ✓ User can activate Protractor mode, click two lines, and have the protractor auto-placed at their intersection — v1.0
- ✓ User can place a protractor via two-point click (vertex + arm direction) for pre-drawn angles — v1.0
- ✓ User can rotate the placed protractor ±1° and ±5° via right-rail controls — v1.0
- ✓ User can flip the protractor between inner and outer scale — v1.0
- ✓ 180° classic style and 360° full-circle style both available — v1.0

#### Text & Session (v1.0)
- ✓ User can place a clipboard-pasted text label on the canvas using the Text tool — v1.0
- ✓ A selected text label responds to nudge controls for repositioning — v1.0
- ✓ Work is auto-saved to a JSON sidecar file alongside the PDF after every change — v1.0
- ✓ User can resume a previous session by opening the same PDF — all geometry restores silently — v1.0

#### Export (v1.0)
- ✓ User can export an annotated PDF (200 DPI, geometry baked in) for printing or submission — v1.0

#### UI (v1.0)
- ✓ All interactive elements ≥56×56px gaze floor — v1.0
- ✓ Dark mode and light mode via ResourceDictionary theme swapping — v1.0
- ✓ Object list panel for gaze-friendly selection of placed objects — v1.0
- ✓ Mid-draw guidance card in right rail — v1.0
- ✓ Proportional scroll indicator that hides when nothing to scroll — v1.0

### Active (v2 scope)

#### Multiple Choice Answers (Deferred — D-08)
- [ ] User can click to select a multiple-choice answer (highlighted with visual tick)
- [ ] User can toggle or change their answer selection
- [ ] User can lock a selected answer to prevent accidental change

#### Transformations
- [ ] User can select a line + shape and apply Reflection via a contextual button (verb, not a tool)

#### Advanced Geometry
- [ ] Geometry notation marks — right-angle square, angle arc marker, equal-length tick marks, parallel arrow markers
- [ ] Mark / Highlight tool for marking questions on the PDF

#### Exam Compliance
- [ ] Full JCQ-compliant Exam Mode lockdown (AARA guidance)

### Out of Scope

- **In-app keyboard** — Grid 3 handles all text input; MathGaze does not need one
- **Cross-platform (iPad, Mac)** — Windows-only by design; no architecture compromise for portability
- **Drag gestures** — gaze-incompatible; everything is click-to-commit
- **Auto-solving / AI assistance** — exam integrity; the app assists the student's thinking, not the answer
- **Real-time collaboration** — single-user, offline tool
- **Protractor position lock** — removed; unnecessary in practice (nudge step controls provide sufficient precision)
- **Advanced constructions** (bisectors, loci, angle bisector) — Tier 3, post-MVP
- **Full transformation suite** (rotation, enlargement) — Tier 2, post-reflection MVP
- **PDF shape/line detection (computer vision)** — high complexity, unreliable for exam use

## Context

v1.0 shipped 2026-05-31. 5,814 lines of C# + XAML across 7 phases, 35 plans, 196 commits over 32 days.

Tech stack confirmed: WPF + SkiaSharp + .NET 9 self-contained. WinUI 3 was the original primary candidate but its MSIX/framework package deployment model is incompatible with managed school machines — WPF is the right permanent choice, not a fallback.

The student uses Grid 3 (Smartbox) as their AAC / eye-gaze platform. Grid 3 drives all Windows input as standard pointer and keyboard events — MathGaze needs zero gaze-SDK integration. Grid 3 handles the on-screen keyboard; MathGaze's text tool is a receiver only.

The split-rails layout (tool nouns left, selection-aware verbs right, canvas centre) proved correct in practice. The right rail's three-panel design (drawing guide during draw, object controls when selected, object list otherwise) handles all states cleanly.

The protractor insight held up: the protractor is not a thing you place — it is the consequence of picking two lines. Two-point free placement (Phase 5) extended this to pre-drawn PDF angles, completing the protractor's interaction model.

Practice/Exam mode was removed mid-milestone (commit 0dc4539) — it added complexity without value for the current student's use case. The protractor renders arc and scale marks only; no numeric readout.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Split Rails layout (tools left, verbs right, canvas centre) | Tools stay stable; right rail adapts to selection; canvas has full focus | ✓ Good — three-panel right rail (draw/select/list) handled all states cleanly |
| Protractor as consequence of two line picks, not a standalone tool | Collapses pick→place→align into 2 clicks on existing objects | ✓ Good — extended to two-point free placement in Phase 5 for pre-drawn angles |
| WPF + SkiaSharp (not WinUI 3) | WinUI 3 MSIX deployment blocked on managed school machines | ✓ Good — confirmed permanent choice; SKElement GPU canvas handled all rendering needs |
| Grid 3 handles all text input — no in-app keyboard | Avoids duplicating what Grid 3 does well | ✓ Good — text tool is a clipboard receiver; works seamlessly |
| Auto-save to JSON sidecar next to PDF | Simplest possible persistence; no database, no cloud, works offline | ✓ Good — round-trip save/restore works reliably |
| 6 MVP tools: Select · Point · Line · Circle · Protractor · Text | Mark dropped from 7→6 for v1; 6 tools cover all geometry question types | ✓ Good — complete for GCSE geometry |
| Practice/Exam mode removed (D-commit 0dc4539) | Added complexity without value for current student; protractor renders scale marks only | ✓ Good — simplifies the UI significantly |
| MCQ answer selection deferred to v2 (D-08) | No exam deadline; building geometry right matters more than building answer layer | ✓ Good — geometry is solid; MCQ can follow |
| PDF export promoted from v2 to Phase 6 | Student needs to submit annotated work; JSON sidecar alone insufficient for submission | ✓ Good — 200 DPI export works; submission-ready |
| Reflection as verb on selection, not a tool | Reflection requires a line + a shape to be meaningful; contextual prevents misuse | — Pending (v2) |

## Constraints

- **Interaction**: No drag gestures. Every action is click-to-commit. Maximum 2 clicks per primitive.
- **Target size**: Every interactive element ≥56×56px at 1× density.
- **Deployment**: Self-contained EXE, no admin install, no runtime dependency on pre-installed components.
- **Platform**: Windows 10/11 only. No cross-platform scope.
- **Input abstraction**: All input treated as standard Windows pointer events. Grid 3 compatible.
- **Tech stack**: WPF + SkiaSharp + .NET 9 self-contained (confirmed permanent — WinUI 3 deployment blocked on school machines).
- **Rendering**: PDF as bitmap background layer; geometry as vector layer on top; UI overlay above both.
- **Exam integrity**: App assists the student's process, never the answer. No auto-solving.

---
*Last updated: 2026-05-31 after v1.0 MVP milestone — 7 phases, 35 plans, 5,814 LOC shipped. PROT-04 (protractor lock) removed as unnecessary. Practice/Exam mode removed mid-milestone. Tech stack confirmed: WPF + SkiaSharp is the permanent choice.*
