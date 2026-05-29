# Phase 6: PDF Export - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-29
**Phase:** 06-pdf-export-save-annotated-exam-paper-with-geometry-overlay-a
**Areas discussed:** Export resolution, Page scope, Output location, Angle readout in export

---

## Export resolution

| Option | Description | Selected |
|--------|-------------|----------|
| 150 DPI | Smallest files; geometry visible but fine print may look soft | |
| 200 DPI | A4 at ~1654×2339px; good school-printer quality, reasonable file size | ✓ |
| 300 DPI | Full print quality; ~2480×3508px per page; larger files | |

**User's choice:** 200 DPI
**Notes:** None — recommended option accepted.

---

## Page scope

| Option | Description | Selected |
|--------|-------------|----------|
| All pages | Export every page, annotated or not — complete exam paper | ✓ |
| Annotated pages only | Only pages with at least one geometry object | |

**User's choice:** All pages
**Notes:** Safest for submission — no risk of missing blank answer pages.

---

## Output location

| Option | Description | Selected |
|--------|-------------|----------|
| Auto-save alongside source PDF | Derives filename (e.g., exam.pdf → exam-annotated.pdf); no file picker | ✓ |
| Save As dialog | Windows file picker — more flexible but requires typing/mouse navigation | |

**User's choice:** Auto-save alongside source PDF
**Notes:** Required for gaze users — no typing, single click.

**Follow-up — toast confirmation:**

| Option | Description | Selected |
|--------|-------------|----------|
| Show toast ("Saved: filename-annotated.pdf") | Brief status message confirming save location | ✓ |
| No toast — silent save | Student checks folder themselves | |

**User's choice:** Show toast
**Notes:** Same toast pattern as clipboard error and protractor parallel-lines error.

---

## Angle readout in export

| Option | Description | Selected |
|--------|-------------|----------|
| Always show readout | Show angle value regardless of mode | |
| Follow current mode | Export reflects current Practice/Exam mode state | |
| Always hide readout | Protractor arc shown; numeric value always hidden | |

**User's choice:** N/A — user reported that Practice/Exam mode and the angle readout have been
removed from the codebase entirely (quick task 260528-sj5, commit 0dc4539). The protractor now
renders arc and scale marks only; no numeric readout in any mode.
**Notes:** Documentation (REQUIREMENTS.md, ROADMAP.md Phase 3, 03-CONTEXT.md, etc.) still
references Practice/Exam mode and the readout. These should be cleaned up as part of Phase 6.

---

## Claude's Discretion

- Exact top-bar layout placement of the Export button
- Error handling for write failures (toast recommended)
- Whether to disable Export button when no PDF is open (yes, recommended)
- Whether to open the exported file in a viewer after save (no, recommended)

## Deferred Ideas

- Vector-native PDF export (requires separate library and complex geometry-to-PDF-operator mapping)
- User-selectable DPI (200 DPI fixed for v1)
- Open file in PDF viewer after export
