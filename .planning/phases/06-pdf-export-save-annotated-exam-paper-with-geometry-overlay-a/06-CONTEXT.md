# Phase 6: PDF Export - Context

**Gathered:** 2026-05-29
**Status:** Ready for planning

<domain>
## Phase Boundary

Produce a new PDF file from the currently open exam paper with all geometry annotations
(Points, Lines, Circles, Protractors, TextObjects) baked in as a rendered image overlay.
The student triggers the export with a single click; the file is saved automatically
alongside the source PDF. No viewer, no live editing — this is a one-shot "save annotated
copy" action for printing or submission.

</domain>

<decisions>
## Implementation Decisions

### Export resolution
- **D-01:** Render each page at **200 DPI**. A4 at 200 DPI yields ~1654×2339px — good quality
  on school printers, crisp enough for GCSE question text and geometry lines, and a
  manageable file size (~1–3 MB per page). No user-selectable DPI; 200 DPI is fixed.

### Page scope
- **D-02:** Export **all pages** in the document, whether annotated or not. The student
  submits the complete exam paper. This is the safest option — no risk of omitting blank
  answer pages that a teacher may expect to see.

### Output location and naming
- **D-03:** Auto-save alongside the source PDF with the suffix `-annotated` before the
  extension. Example: `June 2017 QP.pdf` → `June 2017 QP-annotated.pdf` in the same
  directory. No file picker, no typing — one click, one file.
- **D-04:** After export completes, show a brief confirmation toast: `"Saved: {filename}-annotated.pdf"`.
  Same toast pattern used for clipboard errors and parallel-lines protractor error. Reassures
  the student the export succeeded.

### Angle readout — moot (mode removed)
- **D-05:** Practice/Exam mode and the live angle readout have been **removed** from the
  codebase (quick task 260528-sj5, commit 0dc4539). The protractor renders its arc and
  scale marks only — no numeric readout. The exported PDF renders exactly what the screen
  shows: no mode-dependent branching needed in the export path.
- **D-06:** Documentation (REQUIREMENTS.md SYS-04/SYS-05, ROADMAP.md Phase 3 success
  criteria, 03-CONTEXT.md D-11/D-12/D-13/D-14/D-15, 03-PLAN.md references) still
  references Practice/Exam mode and the angle readout. These should be updated to reflect
  the removal. **This is a documentation cleanup task within Phase 6, not a new feature.**

### Export trigger placement
- **D-07:** Export button lives in the **top bar** (same level as Open File and mode chip
  area), so it is always accessible regardless of which tool or object is selected. Target
  size ≥56×56px. Label: "Export PDF" or a PDF-down-arrow icon. Exact placement is
  Claude's discretion within the top bar.

### PDF generation approach
- **D-08:** Use **SkiaSharp's built-in `SKDocument.CreatePdf()`** — no new NuGet dependency.
  For each page:
  1. Render the PDF page bitmap at 200 DPI via `DocnetPdfService.GetPageBitmapAsync()`.
  2. Draw geometry objects for that page using the same SkiaSharp draw calls as
     `GeometryLayerViewModel.Draw()`, scaled to the 200 DPI canvas.
  3. Encode the combined result as an image-embedded PDF page.
  Output is an image-based PDF (not vector-native), which is appropriate for
  print/submission and requires no additional library.

### Claude's Discretion
- Exact top-bar layout placement of the Export button (within the ≥56×56px constraint)
- Whether to disable the Export button when no PDF is open (sensible default: yes)
- Error handling for write failures (directory read-only, disk full) — show an error toast
- Whether to open the saved file in the default PDF viewer after export (recommend: no —
  keep it simple; the toast confirmation is sufficient)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Design system and interaction rules
- `docs/direction-splitrails.jsx` — Split Rails layout, top bar area, design tokens, ToolTileStyle (≥56×56px)
- `docs/shared.jsx` — Design tokens (T object: colours, fonts, spacing), icon set
- `docs/HANDOFF.md` — Locked interaction rules: ≥56×56px targets, click-to-commit, no drag, Grid 3 pointer events only

### Key source files
- `MathGaze/Services/DocnetPdfService.cs` — `GetPageBitmapAsync(pageIndex, widthPx, heightPx)` for per-page rendering; `GetPageDimensionsPt()` for page size in points
- `MathGaze/ViewModels/GeometryLayerViewModel.cs` — `Draw(SKCanvas, ...)` method and all cached `SKPaint` fields; reuse draw logic for export canvas
- `MathGaze/Services/SessionService.cs` — per-page object cache; must iterate all pages to collect geometry for export
- `MathGaze/Services/ISessionService.cs` — `SyncPage()` and `TryLoadAsync()` interface
- `MathGaze/ViewModels/MainViewModel.cs` — top bar command wiring pattern; add `ExportPdfCommand`
- `MathGaze/Views/MainWindow.xaml` (or TopBar.xaml) — top bar layout; add Export PDF button

### Requirements
- `.planning/REQUIREMENTS.md` — EXAM-V2-02 (originally v2; moved to Phase 6 by roadmap update)

### Prior phase context
- `.planning/phases/02-geometry-core/02-CONTEXT.md` — D-10 (PDF-space coordinate storage); coordinate → screen mapping patterns
- `.planning/phases/04-answer-layer/04-CONTEXT.md` — D-09/D-10/D-11 (sidecar structure, page-keyed object storage)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `DocnetPdfService.GetPageBitmapAsync(pageIndex, widthPx, heightPx)`: already renders any page at any pixel resolution — use with 200 DPI dimensions computed from `GetPageDimensionsPt()`
- `GeometryLayerViewModel.Draw(SKCanvas canvas, float combinedScale, ...)`: all geometry draw logic is already here; can be called with an export canvas at the appropriate scale
- `SessionService` per-page object cache: stores geometry objects keyed by page number — iterate to collect all pages' objects for multi-page export
- `ToolViewModel.StatusMessage`: existing toast/status pattern — reuse for "Saved: filename-annotated.pdf" confirmation
- `MainViewModel.OpenPdfCommand` / `CloseFileCommand`: existing top-bar command pattern to follow for `ExportPdfCommand`

### Established Patterns
- **Coordinate system (D-10)**: geometry stored in PDF points; `CoordinateMapper.PageToScreen()` maps to screen; for export, derive `exportScale = (targetWidthPx / pageWidthPt)` and apply the same mapping at export resolution
- **SKPaint cache pattern**: `GeometryLayerViewModel` already caches all paints as `readonly` fields — the export path calls `Draw()` directly and benefits from the same caches
- **Command pattern**: `RelayCommand` / `AsyncRelayCommand` on `MainViewModel`; wire `ExportPdfCommand` there
- **Toast pattern**: `ToolViewModel.StatusMessage` with a brief display duration — use same mechanism for export success/failure

### Integration Points
- Top bar XAML: add Export PDF button alongside Open File button
- `MainViewModel`: add `ExportPdfCommand` (AsyncRelayCommand), `CanExportPdf` predicate (returns `IsFileOpen`)
- New `IExportService` / `PdfExportService`: handles the multi-page render loop, SKDocument creation, file write
- `SessionService`: must expose or allow iteration of all-pages object cache for the export path

</code_context>

<specifics>
## Specific Ideas

- The export is a single-click action — designed for a gaze user who cannot easily navigate a file picker or type a filename. Auto-naming is non-negotiable for this user.
- The toast confirmation ("Saved: June 2017 QP-annotated.pdf") gives the student clear evidence the export succeeded without requiring them to switch to a file manager.
- Practice/Exam mode was removed (quick task 260528-sj5); several docs still reference it and need cleanup as part of this phase.

</specifics>

<deferred>
## Deferred Ideas

- **Vector-native PDF export** — SkiaSharp PDF produces image-embedded pages (not searchable text). A vector-native export using the original PDF's text layer plus drawn geometry as vector shapes would require a much more complex approach (PdfSharp + direct geometry-to-PDF-operator mapping). Deferred — image-based is sufficient for GCSE submission.
- **User-selectable DPI** — 200 DPI is fixed. A settings option to choose 150/200/300 DPI is deferred; school use cases don't require it.
- **Open exported file after save** — Not opening the file in the PDF viewer after export. Deferred to user feedback if needed.

</deferred>

---

*Phase: 06-pdf-export-save-annotated-exam-paper-with-geometry-overlay-a*
*Context gathered: 2026-05-29*
