# Milestones

## v1.0 MVP (Shipped: 2026-05-31)

**Phases completed:** 7 phases, 35 plans, 40 tasks

**Key accomplishments:**

- WPF + SkiaSharp solution scaffolded on .NET 9 with DI host, self-contained publish flags, and 32-passing CoordinateMapper unit tests across all zoom x DPI combinations
- 3-column WPF shell built with design token ResourceDictionary, MainViewModel observable state skeleton, and 6 UserControls (TopBar, ToolRail, PdfCanvas, ScrollRail, RightRailPlaceholder) — build exits 0, 33 tests pass, human visual checkpoint approved
- PDF rendering pipeline built: IPdfService contract, DocnetPdfService (Docnet.Core/PDFium with SemaphoreSlim thread safety and landscape page guard), PdfCanvasViewModel driving SKElement canvas via CoordinateMapper, PaintSurface handler with Dispatcher.Invoke thread marshaling — build exits 0, 33 tests pass
- All TopBar and ScrollRail interactions wired end-to-end: open PDF, page navigation, 25%-step zoom with fit-page reflow, click-to-commit scroll, and Practice/Exam mode toggle — all verified by human checkpoint (15/15 steps passed)
- Self-contained 149.6 MB MathGaze.exe produced via dotnet publish, bundling .NET 9 runtime + SkiaSharp + PDFium — launches from USB on Windows 10/11 without .NET installed, no admin rights, approved by user
- PDF-coordinate geometry model (PointObject, LineObject, CircleObject) with gaze-accurate hit testing (18/10px tolerances, 28px sub-point radius) and full unit test coverage via GeometryMath and GeometryHitTester
- One-liner:
- Click-driven ToolViewModel state machine (Select/Point/Line/Circle), 20px SnapEngine with endpoint/intersection/orientation candidates, DPI-correct mouse wiring, dashed ghost preview rendering, and ToolRail command bindings
- SkiaSharp geometry rendering layer with 7 cached SKPaint fields drawing Point/Line/Circle in normal and selected states with sub-point tap target indicators
- RightRailViewModel + RightRail.xaml completing the student interaction loop: selection-aware nudge pad (56x56px UDLR, 1/5/20px steps), sub-point labels per D-07, Delete, and always-visible Undo/Redo
- DPI-correct PDF bitmap sizing and Y-axis-corrected nudge direction — three UAT blocking gaps closed with two one-line fixes
- WPF trigger ordering fix for StepButtonStyle (cobalt retained on hover) and DeleteButtonStyle with dark red (#991818) hover to keep white text readable
- IGeometryService injected into MainViewModel; Reset() called on UI thread inside OpenFileAsync before OnDocumentOpenedAsync — geometry objects and undo/redo stack cleared on every PDF session boundary
- DPI call ordering race eliminated and snap ring now tracks cursor continuously during mid-draw, ending intermittent placement inaccuracy and flicker
- SetDpiScale
- 1. [Plan adaptation] SnapEngine.cs changes already applied — no edits made
- One-liner:
- Protractor two-click state machine in ToolViewModel — click line 1 sets AnchorLine, click line 2 computes PDF-space intersection, clamps to 20pt margin, places ProtractorObject; parallel lines shows error and resets; ghost semicircle arc renders at cursor during click-2.
- One-liner:
- SkiaSharp protractor renderer with dual-scale labels, arc-toward-Line-2 orientation, anchor line highlight, aligned ghost preview, and Practice-Mode angle readout — all 5 UAT gaps resolved.
- One-liner:
- One-liner:
- One-liner:
- One-liner:
- One-liner:
- Commit:
- Commit:
- Commit:
- Commit:
- 1. [Rule 1 - Bug] CS0136: local variable name clash — renamed `ds` to `ids` in Idle snap block

---
