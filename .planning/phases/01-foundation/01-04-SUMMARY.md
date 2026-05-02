---
phase: 01-foundation
plan: 04
subsystem: ui
tags: [wpf, skiasharp, mvvm, community-toolkit, pdf, docnet, commands, relay-command]

# Dependency graph
requires:
  - phase: 01-03
    provides: IPdfService, DocnetPdfService, PdfCanvasViewModel, IFileDialogService

provides:
  - All MainViewModel commands fully wired: OpenFileAsync, CloseFileCommand, ZoomIn/ZoomOut/FitPage, PreviousPage/NextPage, ScrollUp/Down/PageUp/PageDown, ToggleMode
  - Fit-page zoom mode with resize reflow (_isFitPageMode flag)
  - Scroll clamping against page height at current zoom
  - CanvasHeightPx public property on PdfCanvasViewModel for cross-ViewModel queries
  - ClearCanvas() on document close
  - Scroll indicator thumb position wired to ScrollThumbTopRatio
  - Mode pill toggle (Practice / Exam) as clickable Button with ControlTemplate

affects: [01-05, geometry-tools, annotation-layer]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Post-construction property injection (SetPdfCanvasViewModel) to break circular DI dependency"
    - "Fit-page mode flag (_isFitPageMode) re-applied on canvas resize for correct reflow"
    - "SizeChanged on UserControl (not SKElement) as the reliable canvas-resize trigger"
    - "Marshal.Copy for safe pixel transfer into SkiaSharp bitmap; InstallPixels+GCHandle causes dangling pointer"
    - "PageDimensions(1.0) scaling-factor overload — not PageDimensions(1,1) viewport overload"

key-files:
  created: []
  modified:
    - MathGaze/ViewModels/MainViewModel.cs
    - MathGaze/ViewModels/PdfCanvasViewModel.cs
    - MathGaze/Services/DocnetPdfService.cs
    - MathGaze/Views/TopBar.xaml
    - MathGaze/Views/ScrollRail.xaml
    - MathGaze/Views/ScrollRail.xaml.cs
    - MathGaze/Views/PdfCanvas.xaml.cs
    - MathGaze/MainWindow.xaml
    - MathGaze/App.xaml.cs

key-decisions:
  - "Zoom steps: 25% increments from 25% to 400% (16 steps). Fit-page resets to exact computed zoom outside the step grid."
  - "Scroll amounts: small = 120 physical px; page = 85% of canvas viewport height"
  - "Post-construction injection (SetPdfCanvasViewModel) chosen over constructor injection to break circular DI; wired in App.xaml.cs"
  - "Marshal.Copy replaces InstallPixels+GCHandle — bitmap owns pixel data, no dangling pointer risk"
  - "PageDimensions(1.0) scaling-factor overload gives correct page-point dimensions; PageDimensions(1,1) is the viewport constructor and returns 1x1 pixels"
  - "SizeChanged moved to UserControl level (not SKElement) for reliable resize events on column width changes"
  - "_isFitPageMode flag maintained in MainViewModel; cleared on manual zoom, set on FitPage and document open; re-applied on canvas resize"

patterns-established:
  - "Post-construction DI injection via Set{Name}() method for circular ViewModel dependencies"
  - "Fit-page zoom as default on document open; flag preserved across resize events"
  - "CanvasHeightPx queried from PdfCanvasViewModel by MainViewModel for geometry calculations"
  - "Marshal.Copy pattern for Docnet raw byte → SKBitmap pixel transfer"

requirements-completed: [CORE-01, CORE-02, CORE-03]

# Metrics
duration: ~4h (multi-session with iterative bug-fixing)
completed: 2026-05-02
---

# Phase 01 Plan 04: MainViewModel Command Wiring Summary

**All TopBar and ScrollRail interactions wired end-to-end: open PDF, page navigation, 25%-step zoom with fit-page reflow, click-to-commit scroll, and Practice/Exam mode toggle — all verified by human checkpoint (15/15 steps passed)**

## Performance

- **Duration:** ~4 hours (multi-session: initial wiring + 5 iterative bug-fix passes)
- **Started:** 2026-04-30T22:31:45Z
- **Completed:** 2026-05-02T09:45:19Z
- **Tasks:** 2 (Task 1: command wiring; Task 2: human verification checkpoint — approved)
- **Files modified:** 9

## Accomplishments

- Replaced all stub commands in MainViewModel with full implementations: OpenFileAsync (async file open + initial render), CloseFileCommand, ZoomIn/ZoomOut (25% steps, 25%–400%), FitPage (canvas-height-fitted, reflows on resize), PreviousPage/NextPage (CanExecute guards), ScrollUp/Down/PageUp/PageDown (120px and 85%-viewport steps), ToggleMode
- Resolved circular DI dependency between MainViewModel and PdfCanvasViewModel via post-construction SetPdfCanvasViewModel() injection wired in App.xaml.cs
- Fixed DocnetPdfService: PageDimensions(1.0) scaling-factor overload instead of PageDimensions(1,1) — corrected page dimensions cascaded into correct FitPage zoom and correct bitmap request sizes; also fixed SKAlphaType from Premul to Unpremul
- Fixed pixel transfer: Marshal.Copy replaces InstallPixels+GCHandle, eliminating dangling-pointer blank-canvas bug
- Fixed PdfCanvas.xaml.cs DataContext/SizeChanged race: DataContextChanged handler wires ViewModel immediately regardless of layout order; SizeChanged moved to UserControl for reliable resize events
- Added _isFitPageMode flag: FitPage zoom re-applied on window resize/maximize without manual re-click
- Fixed mode pill: replaced plain Border with Button using custom ControlTemplate bound to ToggleModeCommand — pill is now clickable
- Fixed scroll indicator: ScrollRail.xaml.cs subscribes to ScrollThumbTopRatio and positions thumb rectangle via UpdateThumbPosition()
- Fixed CanExecute for PreviousPage/NextPage: OnCurrentPageChanged/OnTotalPagesChanged now call NotifyCanExecuteChanged on both commands

## Task Commits

Each task was committed atomically (Task 1 required multiple fix passes):

1. **Task 1 (initial wiring):** `377a22b` — feat(01-04): wire all MainViewModel commands — open, zoom, page nav, scroll, mode
2. **Task 1 (fix pass 1):** `f54cfb3` — fix(01-04): replace InstallPixels+GCHandle with Marshal.Copy for safe pixel transfer
3. **Task 1 (fix pass 2):** `617ca50` — fix(01-04): wrong PageDimensions constructor caused 1x1 pixel page dimensions
4. **Task 1 (fix pass 3):** `227f984` — fix(01-04): wire canvas ViewModel defensively; drive page load from SetCanvasSize
5. **Task 1 (fix pass 4):** `fc5b8c4` — fix(01-04): prev-page, mode-toggle, close-canvas, scroll-indicator, resize-reflow
6. **Task 1 (fix pass 5):** `681cc13` — fix(01-04): reflow PDF on window resize/maximize
7. **Task 2:** Human checkpoint approved by user — no commit

## Files Created/Modified

- `MathGaze/ViewModels/MainViewModel.cs` — Full command wiring: OpenFileAsync, CloseFile, ZoomIn/ZoomOut/FitPage, PreviousPage/NextPage, ScrollUp/Down/PageUp/PageDown, ToggleMode; _isFitPageMode flag and OnCanvasSizeChanged reflow
- `MathGaze/ViewModels/PdfCanvasViewModel.cs` — CanvasHeightPx public property; ClearCanvas() method; ScrollOffsetY reaction in property-changed handler; SizeChanged → LoadCurrentPageAsync when IsOpen; InvalidationRequested fired from ClearCanvas
- `MathGaze/Services/DocnetPdfService.cs` — PageDimensions(1.0) fix; SKAlphaType.Unpremul fix; Marshal.Copy pixel transfer replacing InstallPixels+GCHandle
- `MathGaze/Views/TopBar.xaml` — Mode pill converted from Border to Button with custom ControlTemplate bound to ToggleModeCommand
- `MathGaze/Views/ScrollRail.xaml` — Layout adjustments for scroll thumb track
- `MathGaze/Views/ScrollRail.xaml.cs` — DependencyProperty for ScrollThumbTopRatio; UpdateThumbPosition() subscribing to property changes and track SizeChanged
- `MathGaze/Views/PdfCanvas.xaml.cs` — DataContextChanged handler (WireViewModel); SizeChanged on UserControl (not SKElement); PixelsPerDip scaling for correct physical pixel dimensions
- `MathGaze/MainWindow.xaml` — Removed competing DataContext="{Binding}" on PdfCanvas element
- `MathGaze/App.xaml.cs` — SetPdfCanvasViewModel() post-construction injection call

## Decisions Made

- **Zoom steps:** 25% increments, 25%–400% range (16 discrete steps). FitPage sets an exact computed zoom outside the step grid; manual zoom after fit-page snaps to nearest step from the fitted value.
- **Scroll amounts:** 120 physical pixels for small scroll; 85% of canvas viewport height for page scroll. Both chosen from RESEARCH.md recommendations.
- **Post-construction injection:** SetPdfCanvasViewModel() property setter on PdfCanvasViewModel, called in App.xaml.cs after both ViewModels are constructed. Avoids constructor cycle; keeps DI container simple.
- **Fit-page as default on open:** FitPage() called automatically after document opens. _isFitPageMode=true persists so resize events re-apply it.
- **Marshal.Copy over InstallPixels:** InstallPixels stores a raw pointer; freeing the GCHandle in finally leaves a dangling pointer. Marshal.Copy transfers bytes into the bitmap's own pixel buffer — data lifetime equals bitmap lifetime.
- **PageDimensions(1.0):** The scaling-factor overload returns real page dimensions in points. PageDimensions(1,1) is the viewport constructor and returns 1×1 no matter the page.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] InstallPixels+GCHandle dangling pointer caused blank canvas**
- **Found during:** Task 1 (initial wiring run)
- **Issue:** SKBitmap.InstallPixels stores only a raw pointer; GCHandle freed in finally block left bitmap with garbage pixel data. Canvas rendered blank/white instead of PDF content.
- **Fix:** Replaced with Marshal.Copy to transfer rawBytes into bitmap's own allocated pixel buffer.
- **Files modified:** MathGaze/Services/DocnetPdfService.cs
- **Verification:** PDF page rendered visibly after fix.
- **Committed in:** f54cfb3

**2. [Rule 1 - Bug] PageDimensions(1,1) viewport constructor returned 1×1 pixel dimensions**
- **Found during:** Task 1 (post-pixel-fix run)
- **Issue:** GetPageDimensionsPt called PageDimensions(1, 1) — the viewport constructor — causing GetPageWidth/Height to return 1. FitPage computed zoom > 4.0 (clamped to 400%), and LoadCurrentPageAsync requested a 5×5 pixel bitmap.
- **Fix:** Changed to PageDimensions(1.0) — the scaling-factor constructor where 1.0 pixel/point returns actual page dimensions in points. Also corrected SKAlphaType from Premul to Unpremul to match Docnet's un-premultiplied BGRA output.
- **Files modified:** MathGaze/Services/DocnetPdfService.cs
- **Verification:** FitPage now computes ~0.85× zoom on a standard A4 PDF at 1080p canvas height.
- **Committed in:** 617ca50

**3. [Rule 1 - Bug] PdfCanvas DataContext/SizeChanged race caused CanvasHeightPx=0 at FitPage time**
- **Found during:** Task 1 (post-dimensions-fix run)
- **Issue:** Three interacting bugs: (1) MainWindow.xaml had DataContext="{Binding}" on PdfCanvas competing with code-behind assignment; (2) PdfCanvas.xaml.cs only captured _vm in Loaded handler — SizeChanged fired before Loaded so CanvasHeightPx was never set; (3) SetCanvasSize only fired InvalidationRequested without reloading the bitmap when a document was already open.
- **Fix:** Removed competing DataContext binding from XAML; added DataContextChanged handler (WireViewModel) that captures ViewModel and pushes current canvas size immediately; SetCanvasSize now calls LoadCurrentPageAsync() when _pdfService.IsOpen.
- **Files modified:** MathGaze/MainWindow.xaml, MathGaze/ViewModels/PdfCanvasViewModel.cs, MathGaze/Views/PdfCanvas.xaml.cs
- **Verification:** CanvasHeightPx correctly set before FitPage runs; fit-page zoom ~0.85× confirmed.
- **Committed in:** 227f984

**4. [Rule 1 - Bug] Four UI interaction bugs found during interactive testing**
- **Found during:** Task 1 (interactive verification pre-checkpoint)
- **Issue:** (a) PreviousPage/NextPage buttons stayed disabled after page 1 because OnCurrentPageChanged/OnTotalPagesChanged did not call NotifyCanExecuteChanged; (b) mode pill was a Border with no click handler; (c) CloseFile did not clear canvas — stale bitmap remained; (d) scroll indicator thumb did not move.
- **Fix:** (a) Added NotifyCanExecuteChanged calls in both partial methods; (b) replaced Border with Button using custom ControlTemplate bound to ToggleModeCommand; (c) added ClearCanvas() method to PdfCanvasViewModel, called from CloseFile; (d) added DependencyProperty ScrollThumbTopRatio in ScrollRail.xaml.cs with UpdateThumbPosition() subscription.
- **Files modified:** MathGaze/ViewModels/MainViewModel.cs, MathGaze/ViewModels/PdfCanvasViewModel.cs, MathGaze/Views/TopBar.xaml, MathGaze/Views/ScrollRail.xaml, MathGaze/Views/ScrollRail.xaml.cs
- **Verification:** All four behaviours corrected; confirmed by subsequent interactive run.
- **Committed in:** fc5b8c4

**5. [Rule 1 - Bug] PDF did not reflow on window resize/maximize**
- **Found during:** Task 1 (post-four-bug-fix run)
- **Issue:** SizeChanged was attached to SKElement; column resize events did not propagate reliably to the element. After maximize, PDF remained at original zoom/dimensions. _isFitPageMode not tracked so FitPage was not re-applied after resize.
- **Fix:** Moved SizeChanged to UserControl (this); used e.NewSize * PixelsPerDip for definitive physical dimensions; stopped overwriting _canvasWidthPx/_canvasHeightPx from stale SKCanvas surface info in Paint(); added _isFitPageMode flag with OnCanvasSizeChanged() re-applying fit-page on resize when flag is true; CloseFile resets flag.
- **Files modified:** MathGaze/ViewModels/MainViewModel.cs, MathGaze/ViewModels/PdfCanvasViewModel.cs, MathGaze/Views/PdfCanvas.xaml.cs
- **Verification:** Window maximize and restore both reflow the PDF correctly at fit-page zoom.
- **Committed in:** 681cc13

---

**Total deviations:** 5 auto-fixed (5× Rule 1 Bug)
**Impact on plan:** All fixes necessary for correct interactive behaviour. No new features or scope creep. The plan's task description assumed a cleaner Docnet API surface and a simpler WPF/SkiaSharp wiring path than reality; all corrections address implementation-level correctness, not design changes.

## Issues Encountered

- **Docnet.Core API ambiguity:** PageDimensions has two overloads with similar signatures — (int width, int height) viewport constructor and (double scaleFactor) point-dimensions constructor. Both compile without error; only the wrong-result at runtime reveals the mistake. Established pattern: always use PageDimensions(1.0) for point dimensions.
- **SkiaSharp pixel ownership:** InstallPixels is a zero-copy pinning API; it does not own the data. Documented in SkiaSharp source but not prominently in tutorials. Marshal.Copy is the safe path for managed-byte-array sources.
- **WPF DataContext race:** SizeChanged can fire during initial layout measure pass, before Loaded and before DataContext propagation. DataContextChanged handler is the correct subscription point, not Loaded, for components that need ViewModel access at layout time.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- All CORE-01 (open PDF), CORE-02 (page navigation), CORE-03 (zoom) requirements satisfied and verified by human checkpoint
- All interactive elements in TopBar and ScrollRail respond correctly
- Ready for Plan 05: gaze target size audit (formal 56px minimum check) and any remaining accessibility polish before geometry tool work begins
- No blockers

---
*Phase: 01-foundation*
*Completed: 2026-05-02*
