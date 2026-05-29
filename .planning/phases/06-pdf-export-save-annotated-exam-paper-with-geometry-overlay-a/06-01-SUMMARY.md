---
phase: 06-pdf-export-save-annotated-exam-paper-with-geometry-overlay-a
plan: "01"
subsystem: export
tags: [pdf-export, skiaSharp, skdocument, geometry-render, toast]
dependency_graph:
  requires:
    - MathGaze/Services/IPdfService.cs
    - MathGaze/Services/ISessionService.cs
    - MathGaze/ViewModels/GeometryLayerViewModel.cs
    - MathGaze/Core/CoordinateMapper.cs
  provides:
    - MathGaze/Services/IExportService.cs
    - MathGaze/Services/PdfExportService.cs
    - ISessionService.GetAllPages()
    - GeometryLayerViewModel.DrawObjects()
    - MainViewModel.ExportPdfCommand
    - PdfCanvasViewModel.ToastRequested
  affects:
    - MathGaze/Views/TopBar.xaml
    - MathGaze/App.xaml.cs
tech_stack:
  added: []
  patterns:
    - SKDocument.CreatePdf() for image-based PDF output at 200 DPI
    - CoordinateMapper constructed at export zoom (200/96) with dpiScale=1.0
    - Save/restore _lastScale and _currentDpiScaleF around DrawObjects export call
    - ToastRequested event for immediate toast without waiting for MouseMove
    - ToolViewModel injected into MainViewModel for StatusMessage access
key_files:
  created:
    - MathGaze/Services/IExportService.cs
    - MathGaze/Services/PdfExportService.cs
  modified:
    - MathGaze/Services/ISessionService.cs
    - MathGaze/Services/SessionService.cs
    - MathGaze/ViewModels/GeometryLayerViewModel.cs
    - MathGaze/ViewModels/MainViewModel.cs
    - MathGaze/ViewModels/PdfCanvasViewModel.cs
    - MathGaze/Views/PdfCanvas.xaml.cs
    - MathGaze/Views/TopBar.xaml
    - MathGaze/App.xaml.cs
decisions:
  - ToolViewModel injected into MainViewModel (not accessed via PdfCanvasViewModel) to set StatusMessage directly — cleaner than adding a forwarding method to PdfCanvasViewModel
  - Export DPI scale passed as ExportDpi/96.0 (not ExportDpi/72.0) so stroke widths scale consistently with CoordinateMapper's dpiScale parameter semantics
  - DrawObjects saves/restores _lastScale and _currentDpiScaleF to prevent export scale from corrupting next live Draw() call
metrics:
  duration_minutes: 25
  completed_date: "2026-05-29"
  tasks_completed: 2
  tasks_total: 3
  files_created: 2
  files_modified: 8
---

# Phase 06 Plan 01: PDF Export Pipeline Summary

**One-liner:** Image-based annotated PDF export at 200 DPI using SKDocument.CreatePdf with per-page geometry compositing via a dedicated DrawObjects overload.

## What Was Built

### Task 1 — Export service layer + geometry draw overload

- `IExportService.cs` — interface with `Task<bool> ExportAsync(string sourcePdfPath, string outputPath)`
- `PdfExportService.cs` — renders all pages at 200 DPI: bitmap per page from IPdfService, geometry composited via GeometryLayerViewModel.DrawObjects, output written via SKDocument.CreatePdf. `BuildAnnotatedPath()` strips any existing `-annotated` suffix before appending.
- `ISessionService.GetAllPages()` — returns snapshot dictionary of all pages keyed by 1-based page number; used by PdfExportService to draw annotations on every page
- `SessionService.GetAllPages()` — implemented via `_allPages.ToDictionary(... AsReadOnly())`
- `GeometryLayerViewModel.DrawObjects()` — export-path overload that draws an explicit object list at a specified dpiScale; saves/restores `_lastScale` and `_currentDpiScaleF` around the export to prevent the export scale from corrupting the next live Draw() call; all objects rendered as unselected (no selection chrome)

### Task 2 — UI wiring and command plumbing

- `PdfCanvasViewModel.ToastRequested` event + `RequestToastUpdate()` — raised by MainViewModel after setting StatusMessage to force an immediate toast without waiting for the next MouseMove
- `PdfCanvas.xaml.cs` — subscribes to `ToastRequested` via `OnToastRequested` handler; calls `UpdateStatusToast` directly
- `MainViewModel.ExportPdfCommand` — async relay command with `CanExecute = IsPdfOpen`; syncs current page objects into SessionService before calling ExportAsync; shows success/failure toast via ToolViewModel.StatusMessage + RequestToastUpdate()
- `MainViewModel` — `ToolViewModel` injected into constructor to provide direct access to `StatusMessage`; `[NotifyCanExecuteChangedFor(nameof(ExportPdfCommand))]` added to `_isPdfOpen`
- `App.xaml.cs` — `services.AddSingleton<IExportService, PdfExportService>()` registered before ViewModel registrations
- `TopBar.xaml` — Export PDF button at Width=56 Height=56 (satisfies >=56px gaze floor), `Command="{Binding ExportPdfCommand}"`, download-arrow icon with "PDF" label

## Decisions Made

| Decision | Rationale |
|----------|-----------|
| Inject ToolViewModel into MainViewModel | Needed to set StatusMessage for export toast; cleaner than adding forwarding method to PdfCanvasViewModel |
| Export dpiScale = ExportDpi/96.0 | Matches CoordinateMapper's dpiScale parameter semantics (pixels per DIP unit); keeps stroke width scaling consistent |
| Save/restore _lastScale in DrawObjects | Prevents 200 DPI export scale from sticking and corrupting the next 96 DPI screen render |
| SKDocument PDF page size in points (not pixels) | Per BeginPage API contract: page canvas operates in PDF point units; geometry drawn at point-scale coordinates |

## Deviations from Plan

### Auto-added Functionality

**[Rule 2 - Missing] ToolViewModel injected into MainViewModel**
- **Found during:** Task 2
- **Issue:** Plan's ExportPdfAsync code referenced `_toolVm.StatusMessage` but MainViewModel had no `_toolVm` field. Plan noted: "If `_toolVm` is not in MainViewModel, inject `ToolViewModel` into MainViewModel."
- **Fix:** Added `ToolViewModel toolViewModel` parameter to MainViewModel constructor, stored as `_toolVm`. ToolViewModel is already registered as singleton in DI — no new registration needed.
- **Files modified:** `MathGaze/ViewModels/MainViewModel.cs`
- **Commit:** 80a79d7

**[Rule 2 - Missing] using System.Linq added to MainViewModel**
- **Found during:** Task 2 (ExportPdfAsync uses `.ToList()`)
- **Fix:** Added `using System.Linq;` to MainViewModel.cs
- **Files modified:** `MathGaze/ViewModels/MainViewModel.cs`
- **Commit:** 80a79d7

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| Task 1 | a2f6a02 | feat(06-01): add IExportService, PdfExportService, GetAllPages, DrawObjects |
| Task 2 | 80a79d7 | feat(06-01): wire ExportPdfCommand, ToastRequested event, DI, Export PDF button |

## Known Stubs

None — all export functionality is fully wired. The feature requires human verification (Task 3 checkpoint) to confirm end-to-end export produces a valid annotated PDF.

## Threat Flags

No new threat surface beyond what is documented in the plan's threat model (T-06-01 through T-06-04). IOException and UnauthorizedAccessException are caught in PdfExportService.ExportAsync.

## Self-Check

**Files created:**
- MathGaze/Services/IExportService.cs — FOUND
- MathGaze/Services/PdfExportService.cs — FOUND

**Key modifications verified in build:**
- Build succeeded: 0 errors, 9 warnings (all pre-existing NU1701 package compat warnings)

**Commits:**
- a2f6a02 — FOUND (git log confirmed)
- 80a79d7 — FOUND (git log confirmed)

## Self-Check: PASSED
