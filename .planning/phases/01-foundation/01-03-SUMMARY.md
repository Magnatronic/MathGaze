---
phase: 01-foundation
plan: 03
subsystem: pdf-rendering
tags: [wpf, skiasharp, docnet-core, pdf, mvvm, services, thread-safety]

# Dependency graph
requires:
  - 01-01 (CoordinateMapper, WPF project, SkiaSharp, DI host)
  - 01-02 (MainViewModel, PdfCanvas UserControl skeleton, SKElement, DI wiring)
provides:
  - IPdfService contract: OpenDocumentAsync, CloseDocument, GetPageDimensionsPt, GetPageBitmapAsync
  - DocnetPdfService: SemaphoreSlim-gated Docnet.Core PDFium rendering to SKBitmap
  - IFileDialogService + FileDialogService: OS open-file dialog abstraction
  - PdfCanvasViewModel: drives SKElement canvas, owns SKBitmap and CoordinateMapper
  - PdfCanvas.xaml.cs: PaintSurface delegates to PdfCanvasViewModel.Paint; Dispatcher.Invoke for thread safety
affects: [01-04, 01-05]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - SemaphoreSlim(1,1) gates all DocLib.Instance calls — PDFium C API is not thread-safe
    - DocLib.Instance singleton lives for app lifetime (disposing while IDocReader open crashes process)
    - Docnet.Core PageDimensions landscape guard: dimOne <= dimTwo constraint requires swap + RotateBitmap90 for landscape pages
    - SKColorType.Bgra8888 matches Docnet.Core GetImage() byte order (not RGBA)
    - Interlocked.Exchange for atomic bitmap swap — disposes old bitmap before installing new one
    - Dispatcher.Invoke marshals InvalidationRequested from background render thread to UI thread
    - VisualTreeHelper.GetDpi(this).PixelsPerDip converts WPF logical units to physical pixels
    - SizeChanged wired in code-behind (not XAML) — same compat shim pattern as PaintSurface from Plan 02
    - System.IO and System.Windows.Media explicit usings required in files touched by XAML temp-project
    - PdfCanvasViewModel.OnDocumentOpenedAsync: public entry point for Plan 04 to call after PDF open

key-files:
  created:
    - MathGaze/Services/IPdfService.cs
    - MathGaze/Services/DocnetPdfService.cs
    - MathGaze/Services/IFileDialogService.cs
    - MathGaze/Services/FileDialogService.cs
    - MathGaze/ViewModels/PdfCanvasViewModel.cs
  modified:
    - MathGaze/Views/PdfCanvas.xaml (added Loaded="OnLoaded" event)
    - MathGaze/Views/PdfCanvas.xaml.cs (full rendering pipeline: PaintSurface, SizeChanged, Dispatcher.Invoke)
    - MathGaze/App.xaml.cs (registered IPdfService, IFileDialogService, PdfCanvasViewModel in DI)
    - MathGaze/MainWindow.xaml.cs (PdfCanvasViewModel injection; PdfCanvasView.DataContext wiring)

key-decisions:
  - "SemaphoreSlim(1,1) chosen over lock() for async compatibility in GetPageBitmapAsync (Task.Run path)"
  - "DocLib.Instance is a process-lifetime singleton — never dispose it while IDocReader instances are open"
  - "dpiScale hardcoded to 1.0 in PdfCanvasViewModel.EnsureCoordinateMapper for Phase 1; full high-DPI support deferred to Phase 2"
  - "SizeChanged wired in code-behind (not XAML) to avoid XAML temp-project compat shim type resolution failures"
  - "System.IO using added explicitly to DocnetPdfService for XAML temp-project build compatibility"

requirements-completed: [CORE-01, CORE-02]

# Metrics
duration: ~25min
completed: 2026-04-30
---

# Phase 01 Plan 03: PDF Rendering Pipeline Summary

**PDF rendering pipeline built: IPdfService contract, DocnetPdfService (Docnet.Core/PDFium with SemaphoreSlim thread safety and landscape page guard), PdfCanvasViewModel driving SKElement canvas via CoordinateMapper, PaintSurface handler with Dispatcher.Invoke thread marshaling — build exits 0, 33 tests pass**

## Performance

- **Duration:** ~25 min
- **Completed:** 2026-04-30
- **Tasks:** 2 of 2 (complete)
- **Files:** 5 created, 4 modified

## Accomplishments

- Created `IPdfService` contract: `OpenDocumentAsync`, `CloseDocument`, `GetPageDimensionsPt`, `GetPageBitmapAsync` — fully async, nullable-safe, IDisposable
- Created `DocnetPdfService`: SemaphoreSlim(1,1) gates all `DocLib.Instance` calls; `Task.Run` offloads rendering from UI thread; landscape page guard swaps `dimOne`/`dimTwo` and applies `RotateBitmap90`; `SKColorType.Bgra8888` matches PDFium byte order; `Interlocked.Exchange` for atomic bitmap disposal
- Created `IFileDialogService` + `FileDialogService`: OS `OpenFileDialog` abstraction with PDF filter, decoupling ViewModels from WPF `Window` references
- Created `PdfCanvasViewModel`: observes `MainViewModel.PropertyChanged` for page/zoom changes; `LoadCurrentPageAsync` renders at computed physical pixel dimensions; `Paint()` draws via `CoordinateMapper.GetPageDestRect`; `InvalidationRequested` event for UI thread notification; `OnDocumentOpenedAsync` public entry point for Plan 04
- Updated `PdfCanvas.xaml.cs`: `OnPaintSurface` delegates to `PdfCanvasViewModel.Paint`; `OnCanvasSizeChanged` calls `ReportCanvasSize` (physical pixel conversion via `VisualTreeHelper.GetDpi`); `Dispatcher.Invoke` marshals canvas invalidation from background threads to UI thread; `Loaded` event wires `_vm` from `DataContext`
- Registered `IPdfService`, `IFileDialogService`, and `PdfCanvasViewModel` as DI singletons in `App.xaml.cs`
- Updated `MainWindow.xaml.cs` to inject `PdfCanvasViewModel` and set `PdfCanvasView.DataContext`

## Task Commits

1. **Task 1: IPdfService interface + DocnetPdfService implementation** — `f6272f7`
2. **Task 2: PdfCanvasViewModel + PaintSurface rendering pipeline** — `af1259e`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] SKXamlCanvas does not exist — adapted to SKElement (carried forward from Plan 02)**
- **Found during:** Task 2 planning (pre-empted via Plan 02 SUMMARY knowledge)
- **Issue:** Plan 03 references `SKXamlCanvas` in the PdfCanvas.xaml update snippet. Per Plan 02 deviation, the actual WPF control is `SKElement`. The XAML file already used `SKElement` from Plan 02.
- **Fix:** Used `SKElement` in all code-behind references; no XAML change needed beyond adding `Loaded` attribute.
- **Files modified:** `MathGaze/Views/PdfCanvas.xaml`, `MathGaze/Views/PdfCanvas.xaml.cs`
- **Commit:** `af1259e`

**2. [Rule 1 - Bug] Missing System.IO using in DocnetPdfService for XAML temp-project compatibility**
- **Found during:** Task 1 build verification
- **Issue:** The XAML temp-project (generated by WPF XAML compiler) does not receive implicit `using System.IO` from the main project's global usings. `File.Exists()` in `DocnetPdfService.cs` failed with CS0103.
- **Fix:** Added `using System.IO;` explicitly to `DocnetPdfService.cs`.
- **Files modified:** `MathGaze/Services/DocnetPdfService.cs`
- **Commit:** `f6272f7`

**3. [Rule 1 - Bug] Missing System.Windows.Media using in PdfCanvas.xaml.cs for XAML temp-project compatibility**
- **Found during:** Task 2 build verification
- **Issue:** `VisualTreeHelper` lives in `System.Windows.Media`. The XAML temp-project does not receive implicit usings, so CS0103 was raised.
- **Fix:** Added `using System.Windows.Media;` explicitly to `PdfCanvas.xaml.cs`.
- **Files modified:** `MathGaze/Views/PdfCanvas.xaml.cs`
- **Commit:** `af1259e`

**4. [Rule 1 - Bug] SizeChanged wired in code-behind (not XAML)**
- **Found during:** Task 2 design (pre-empted from Plan 02 SUMMARY pattern)
- **Issue:** Plan 03 snippet adds `SizeChanged="OnCanvasSizeChanged"` as a XAML attribute on `SKElement`. Per Plan 02 deviation, SkiaSharp event handlers on `SKElement` must be wired in code-behind to avoid XAML temp-project resolution failures.
- **Fix:** Added `SkCanvas.SizeChanged += OnCanvasSizeChanged;` in constructor; removed from XAML.
- **Files modified:** `MathGaze/Views/PdfCanvas.xaml`, `MathGaze/Views/PdfCanvas.xaml.cs`
- **Commit:** `af1259e`

---

**Total deviations:** 4 auto-fixed (all bugs from XAML compat shim pattern, consistent with Plan 02 findings)

## Known Stubs

- `MathGaze/ViewModels/PdfCanvasViewModel.cs` — `OnDocumentOpenedAsync` is a public entry point but is not yet called by anything. Intentional; Plan 04 calls this from `OpenFileCommand` after `IPdfService.OpenDocumentAsync` succeeds.
- `MathGaze/ViewModels/PdfCanvasViewModel.cs` — `dpiScale` hardcoded to 1.0 in `EnsureCoordinateMapper`. Intentional; full high-DPI support wired in Phase 2 once `VisualTreeHelper.GetDpi` result is passed from `PdfCanvas.xaml.cs` into the ViewModel.
- Canvas renders grey background until a PDF is opened via Plan 04's `OpenFileCommand`. This is the correct state — the rendering pipeline is complete but not yet triggered.

## Threat Flags

None — this plan creates no network endpoints, auth paths, or schema changes. All new surfaces are:
- Local file path validated by OS `OpenFileDialog` (no path concatenation)
- `File.Exists()` guard before `DocLib.Instance.GetDocReader` call
- PDFium CVE risk accepted per T-03-01 (NuGet update path documented in threat model)

## Self-Check: PASSED

- `MathGaze/Services/IPdfService.cs` exists and contains `GetPageBitmapAsync` — FOUND
- `MathGaze/Services/DocnetPdfService.cs` contains `SemaphoreSlim` — FOUND
- `MathGaze/Services/DocnetPdfService.cs` contains `DocLib.Instance` — FOUND
- `MathGaze/Services/DocnetPdfService.cs` contains `Bgra8888` — FOUND
- `MathGaze/Services/DocnetPdfService.cs` contains `needsSwap` — FOUND
- `MathGaze/Services/IFileDialogService.cs` contains `ShowOpenPdfDialog` — FOUND
- `MathGaze/App.xaml.cs` contains `IPdfService` — FOUND
- `MathGaze/ViewModels/PdfCanvasViewModel.cs` contains `InvalidationRequested` — FOUND
- `MathGaze/ViewModels/PdfCanvasViewModel.cs` contains `GetPageBitmapAsync` — FOUND
- `MathGaze/ViewModels/PdfCanvasViewModel.cs` contains `CoordinateMapper` — FOUND
- `MathGaze/Views/PdfCanvas.xaml.cs` contains `OnPaintSurface` — FOUND
- `MathGaze/Views/PdfCanvas.xaml.cs` contains `Dispatcher.Invoke` — FOUND
- `MathGaze/Views/PdfCanvas.xaml.cs` contains `VisualTreeHelper.GetDpi` — FOUND
- `MathGaze/App.xaml.cs` contains `PdfCanvasViewModel` — FOUND
- Commit `f6272f7` exists — FOUND
- Commit `af1259e` exists — FOUND
- Build exits 0 — CONFIRMED
- 33 tests pass — CONFIRMED

---
*Phase: 01-foundation*
*Completed: 2026-04-30*
