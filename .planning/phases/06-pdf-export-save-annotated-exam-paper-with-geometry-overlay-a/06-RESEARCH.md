# Phase 6: PDF Export — Research

**Researched:** 2026-05-29
**Domain:** SkiaSharp PDF generation, WPF command/toast patterns, geometry render reuse
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Render each page at 200 DPI. Fixed — no user-selectable DPI.
- **D-02:** Export all pages in the document, whether annotated or not.
- **D-03:** Auto-save alongside source PDF with suffix `-annotated` before the extension. No file picker.
- **D-04:** Show a confirmation toast: `"Saved: {filename}-annotated.pdf"` on success. Same toast pattern as ToolViewModel.StatusMessage.
- **D-05:** Practice/Exam mode and the live angle readout have been removed (commit 0dc4539). The exported PDF renders exactly what the screen shows — no mode-dependent branching.
- **D-06:** Documentation (REQUIREMENTS.md SYS-04/SYS-05, ROADMAP.md Phase 3 success criteria, 03-CONTEXT.md, 03-PLAN.md) still references Practice/Exam mode. These must be updated as a documentation cleanup task within Phase 6.
- **D-07:** Export button lives in the top bar (TopBar.xaml), always accessible. Target size ≥56×56px. Label: "Export PDF" or PDF-down-arrow icon.
- **D-08:** Use SkiaSharp's built-in `SKDocument.CreatePdf()`. No new NuGet dependency. Output is an image-based PDF (not vector-native).

### Claude's Discretion

- Exact top-bar layout placement of the Export button (within the ≥56×56px constraint)
- Whether to disable the Export button when no PDF is open (recommended: yes, `CanExecute = nameof(IsPdfOpen)`)
- Error handling for write failures — show an error toast
- Whether to open the saved file in the default PDF viewer after export (recommended: no)

### Deferred Ideas (OUT OF SCOPE)

- Vector-native PDF export (PdfSharp + geometry-to-PDF-operators)
- User-selectable DPI (150/200/300)
- Open exported file in PDF viewer after export
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| EXAM-V2-02 | Export annotated PDF for submission | SKDocument.CreatePdf + GeometryLayerViewModel.Draw reuse at 200 DPI |
| DOC-CLEANUP | Update docs still referencing Practice/Exam mode (D-06) | File-by-file search in REQUIREMENTS.md, 03-CONTEXT.md, 03-PLAN.md, ROADMAP.md |
</phase_requirements>

---

## Summary

Phase 6 is a focused export feature with two components: a `PdfExportService` that produces the annotated PDF, and top-bar wiring to trigger it. The technology — `SKDocument.CreatePdf()` — is already in-process (SkiaSharp is a project dependency), the render logic already exists in `GeometryLayerViewModel.Draw()`, and the page-bitmap rendering already exists in `DocnetPdfService.GetPageBitmapAsync()`. The implementation is primarily assembling existing capabilities in a new service.

The key insight is that `GeometryLayerViewModel.Draw()` accepts any `SKCanvas` and a `CoordinateMapper`. For export, we construct a fresh `CoordinateMapper` with `zoomFactor=1.0`, `dpiScale=1.0`, `canvasOriginX=0`, `canvasOriginY=0`, and `exportScale = targetWidthPx / pageWidthPt` encoded by passing the correct `zoomFactor` equivalent. Since `CoordinateMapper.Scale = (dpiScale * 96.0 / 72.0) * zoomFactor`, the export scale formula is `zoomFactor = targetWidthPx / (pageWidthPt * (96.0 / 72.0))` with `dpiScale = 1.0`. This produces a mapper where every PDF point maps to exactly the right number of export pixels.

The second component is documentation cleanup: SYS-04, SYS-05, PROT-06, and several planning files still reference Practice/Exam mode that was removed in commit 0dc4539. These are straightforward text updates — no logic changes.

**Primary recommendation:** Create `IExportService` / `PdfExportService` injected into `MainViewModel`; iterate `SessionService._allPages` via a new read-only accessor on `ISessionService`; for each page call `GetPageBitmapAsync`, create a `SKBitmap` canvas, draw geometry, then write to `SKDocument`; surface success/failure via `ToolViewModel.StatusMessage` toast.

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| SkiaSharp | 3.119.2 (already installed) | `SKDocument.CreatePdf()`, `SKCanvas`, `SKBitmap` | Already a project dependency; no new NuGet required [VERIFIED: codebase] |
| Docnet.Core | (already installed) | `GetPageBitmapAsync()` per-page render at 200 DPI | Already the project PDF engine [VERIFIED: codebase] |
| System.IO | .NET 9 inbox | `FileStream`, `Path.GetFileNameWithoutExtension` for output path | Standard .NET [ASSUMED] |
| Microsoft.Extensions.DependencyInjection | 9.x (already installed) | Register `PdfExportService` as singleton | Established DI pattern in project [VERIFIED: App.xaml.cs] |
| CommunityToolkit.Mvvm | 8.x (already installed) | `AsyncRelayCommand` for `ExportPdfCommand` | Established command pattern [VERIFIED: MainViewModel.cs] |

### No New NuGet Dependencies

This phase requires zero new package installs. All required libraries are already present.

---

## Architecture Patterns

### Recommended Project Structure

```
MathGaze/
├── Services/
│   ├── IExportService.cs        # new — export interface
│   └── PdfExportService.cs      # new — export implementation
├── ViewModels/
│   └── MainViewModel.cs         # modified — add ExportPdfCommand
└── Views/
    └── TopBar.xaml              # modified — add Export PDF button
```

Documentation cleanup (separate plan):
```
.planning/
    REQUIREMENTS.md              # update SYS-04, SYS-05, PROT-06 status/text
.planning/phases/03-protractor/
    03-CONTEXT.md                # update D-11..D-15 references
    03-PLAN.md (plans referencing Practice Mode)
docs/
    HANDOFF.md                   # references Exam/Practice chip
```

### Pattern 1: SKDocument multi-page write loop

**What:** `SKDocument.CreatePdf(Stream)` → `BeginPage(widthPt, heightPt)` → draw → `EndPage()` → `Close()`.

**Critical dimension note:** `BeginPage(width, height)` takes dimensions in **PDF points** (the same unit the source page uses). The canvas returned by `BeginPage` maps point-space to the internal PDF representation. We draw an `SKBitmap` (rendered at pixel resolution) using `canvas.DrawBitmap(bitmap, SKRect)` where the destination rect covers the full page in points. This is how SkiaSharp embeds a raster image into a PDF page.

**Source:** [CITED: learn.microsoft.com/dotnet/api/skiasharp.skdocument.beginpage]

```csharp
// Source: [CITED: learn.microsoft.com/dotnet/api/skiasharp.skdocument.createpdf]
using var fileStream = File.Create(outputPath);
using var pdfDoc    = SKDocument.CreatePdf(fileStream);

for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
{
    var (widthPt, heightPt) = pdfService.GetPageDimensionsPt(pageIndex);

    // 1. Render PDF page bitmap at 200 DPI
    int targetWidthPx  = (int)Math.Round(widthPt  * 200.0 / 72.0);
    int targetHeightPx = (int)Math.Round(heightPt * 200.0 / 72.0);
    using var bitmap = await pdfService.GetPageBitmapAsync(pageIndex, targetWidthPx, targetHeightPx);

    // 2. Create export surface at the same pixel dimensions
    var imageInfo      = new SKImageInfo(targetWidthPx, targetHeightPx);
    using var surface  = SKSurface.Create(imageInfo);
    var exportCanvas   = surface.Canvas;

    // 3. Draw PDF bitmap
    exportCanvas.DrawBitmap(bitmap, new SKRect(0, 0, targetWidthPx, targetHeightPx));

    // 4. Build export CoordinateMapper (zoomFactor derived to map pt→px at 200 DPI)
    //    CoordinateMapper.Scale = (dpiScale * 96.0/72.0) * zoomFactor
    //    We want Scale = 200.0/72.0, so with dpiScale=1.0: zoomFactor = 200.0/96.0
    double exportZoom  = 200.0 / 96.0;
    var mapper         = new CoordinateMapper(exportZoom, 1.0, widthPt, heightPt, 0, 0);

    // 5. Draw geometry for this page
    var pageObjects    = allPages.GetValueOrDefault(pageIndex + 1) ?? new List<GeometryObject>();
    geometryLayer.DrawObjects(exportCanvas, mapper, exportObjects: pageObjects, dpiScale: 200.0 / 96.0);

    // 6. Snapshot and encode
    using var snapshot = surface.Snapshot();
    using var pdfPage  = pdfDoc.BeginPage((float)widthPt, (float)heightPt);
    pdfPage.DrawImage(SKImage.FromBitmap(bitmap), 0, 0, (float)widthPt, (float)heightPt);
    pdfDoc.EndPage();
}
pdfDoc.Close();
```

**Note on geometry drawing:** `GeometryLayerViewModel.Draw()` reads from `IGeometryService.Objects` (the live session). For export, we need to draw the objects from each page's stored list, not just the current page's live objects. This requires either (a) a new overload on `GeometryLayerViewModel` that accepts an explicit object list, or (b) driving draw calls directly in `PdfExportService` using the same private draw logic. Option (a) is cleaner — add a `DrawObjects(SKCanvas, CoordinateMapper, IEnumerable<GeometryObject>, double dpiScale)` overload.

### Pattern 2: Export CoordinateMapper scale derivation

**What:** For screen rendering, `CoordinateMapper.Scale = (dpiScale * 96.0/72.0) * zoomFactor`. For 200 DPI export with the export canvas in pixel space:

```
Target: Scale = 200.0 / 72.0  (pixels per PDF point at 200 DPI)
Setting dpiScale = 1.0, then: zoomFactor = 200.0 / 96.0 ≈ 2.0833
```

The export mapper has `canvasOriginX = 0`, `canvasOriginY = 0` (no centering offset, no scroll). Pages are rendered full-bleed from the top-left corner.

**Source:** [VERIFIED: CoordinateMapper.cs — Scale formula, lines 25-26]

### Pattern 3: ISessionService all-pages accessor

**What:** `SessionService._allPages` is the per-page object store but is private. The export path needs to read it for all pages. Add `IReadOnlyDictionary<int, IReadOnlyList<GeometryObject>> GetAllPages()` to `ISessionService` and implement in `SessionService`.

**Current interface:** `ISessionService` has `SetPdfPath`, `SyncPage`, `TrySaveAsync`, `TryLoadAsync` — no read accessor for the full store. [VERIFIED: ISessionService.cs]

**Important:** Before calling export, `MainViewModel` must call `_sessionService.SyncPage(CurrentPage, _geometryService.Objects.ToList())` to flush the current page's in-memory objects into the session store. The `_pageObjectCache` in `MainViewModel` is separate from `SessionService._allPages` — both must be consulted.

Actually on closer inspection: `SessionService._allPages` is updated by `OnObjectsChanged` (every geometry change) AND by `SyncPage` calls (on page navigation). The export service should call `SyncPage` for the current page before exporting to ensure the currently-viewed page's objects are captured.

### Pattern 4: AsyncRelayCommand wiring in MainViewModel

**What:** Add `ExportPdfCommand` as an `[RelayCommand(CanExecute = nameof(CanExportPdf))]` on `MainViewModel`, following the `OpenFileCommand` / `CloseFileCommand` pattern.

```csharp
// Source: [VERIFIED: MainViewModel.cs — RelayCommand pattern]
[RelayCommand(CanExecute = nameof(CanExportPdf))]
private async Task ExportPdfAsync()
{
    // 1. Sync current page before export
    _sessionService.SyncPage(CurrentPage, _geometryService.Objects.ToList());
    // 2. Delegate to export service
    string? outputPath = BuildOutputPath(_currentPdfPath!);
    bool success = await _exportService.ExportAsync(outputPath, ...).ConfigureAwait(false);
    // 3. Toast on UI thread
    await Application.Current.Dispatcher.InvokeAsync(() =>
        _toolVm.StatusMessage = success
            ? $"Saved: {Path.GetFileName(outputPath)}"
            : "Export failed — check file permissions");
}
private bool CanExportPdf() => IsPdfOpen;
```

`NotifyCanExecuteChangedFor` on `_isPdfOpen` already drives related commands; add `ExportPdfCommand` to that attribute list.

### Pattern 5: Toast for export feedback

**What:** `ToolViewModel.StatusMessage` is the established toast mechanism. The PdfCanvas code-behind calls `UpdateStatusToast(_vm.ToolVmStatusMessage)` from `OnMouseMove`. This means the toast updates on the next mouse move, not instantly.

**For the export confirmation, a more immediate approach is needed.** Inspect the toast pattern: `UpdateStatusToast` in PdfCanvas.xaml.cs is called only from `OnMouseMove`. The export success message should be shown immediately.

**Options:**
1. Call `UpdateStatusToast` directly from the PdfCanvas code-behind by subscribing to a new event/property on `PdfCanvasViewModel` that is raised after export.
2. Add a separate `ExportStatusMessage` property on `MainViewModel` that TopBar.xaml binds to and shows as a WPF overlay — simpler since `MainViewModel` already exposes properties to TopBar.
3. Add a `ShowToast(string)` method to `PdfCanvasViewModel` that the canvas code-behind subscribes to.

**Recommendation (Claude's discretion):** Add `ExportStatusMessage` as an `[ObservableProperty]` on `MainViewModel` bound in TopBar.xaml as a status label, or expose it via `PdfCanvasViewModel.ToolVmStatusMessage`. The cleanest approach that reuses the existing toast infrastructure: set `_toolVm.StatusMessage` (ToolViewModel already has that property) and then call `InvalidationRequested` or fire a dedicated event so the canvas redraws and shows the toast. Since `ToolVmStatusMessage` is already surfaced from `PdfCanvasViewModel`, wiring `ExportPdfAsync` to set `_toolVm.StatusMessage` and then explicitly call `PdfCanvasViewModel` to show it works cleanly.

Alternatively: have `MainViewModel.ExportPdfAsync` call `PdfCanvasViewModel.ShowExportToast(string)` which updates the PdfCanvas toast directly. This avoids coupling to ToolViewModel for a non-tool action.

**Source:** [VERIFIED: PdfCanvas.xaml.cs lines 148-161, PdfCanvas.xaml lines 13-21]

### Pattern 6: Top-bar Export button

**What:** TopBar.xaml uses DockPanel with `LastChildFill="False"`. All right-docked items use `DockPanel.Dock="Right"`. The Export button should be added as `DockPanel.Dock="Right"` before the settings button, or embedded in the file chip area.

The existing top-bar buttons use `IconButtonStyle` at 36×36px. For the ≥56×56px gaze-accuracy floor, the export button needs to be explicitly sized at `Width="56" Height="56"` (or a new chip-style border like the zoom/page-nav strips).

**Recommended approach:** A standalone chip (same border-radius style as the zoom/page-nav strips) docked Right, containing a "Export PDF" text button or icon+label at 56×56px minimum.

**Source:** [VERIFIED: TopBar.xaml — DockPanel structure and button patterns]

### Anti-Patterns to Avoid

- **Drawing geometry from live IGeometryService.Objects during export:** `GeometryService.Objects` only contains the CURRENT page's objects. Export needs all pages. Use `SessionService.GetAllPages()` (new accessor). [VERIFIED: SessionService.cs, MainViewModel.cs]
- **Calling `SKDocument.BeginPage` with pixel dimensions:** `BeginPage(width, height)` takes PDF points, not pixels. Pass `(float)widthPt` and `(float)heightPt`. Confusing this causes pages scaled ~2.77x too large. [CITED: learn.microsoft.com/dotnet/api/skiasharp.skdocument.beginpage]
- **Drawing the bitmap to the PDF canvas at pixel coordinates:** When writing to `SKDocument.BeginPage`, draw the bitmap into a rect in point-space `(0, 0, widthPt, heightPt)`, not pixel-space. The PDF canvas coordinate system is in PDF points.
- **Forgetting `pdfDoc.Close()` or using Dispose without Close:** `SKDocument.Close()` finalizes the PDF stream. `Dispose()` also calls `Close()`, but explicit is clearer. The document is not complete until `Close()` is called. [ASSUMED — standard SkiaSharp pattern]
- **Running the export on the UI thread:** `GetPageBitmapAsync` uses `Task.Run` internally but is async. The overall loop should run on a background thread (`Task.Run`) to avoid freezing the UI. [VERIFIED: DocnetPdfService.cs lines 102-158]
- **Not syncing the current page before export:** `SessionService._allPages` may not have the current page's latest state if the user made changes since the last navigation event. Always call `SyncPage(CurrentPage, objects)` immediately before export. [VERIFIED: SessionService.cs lines 71-77]

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Multi-page PDF creation | Custom PDF byte writer | `SKDocument.CreatePdf()` | PDF is a complex binary format; SkiaSharp wraps Skia's mature PDF backend [CITED: docs] |
| Raster image embedding in PDF | Manual XOBJ stream | `canvas.DrawImage()` in `BeginPage` context | SkiaSharp handles PDF image object creation and compression internally [CITED: docs] |
| Per-page PDF bitmap rendering | Custom PDFium wrapper | `DocnetPdfService.GetPageBitmapAsync()` | Already implemented and battle-tested for all GCSE PDF edge cases [VERIFIED: codebase] |
| Geometry draw logic | Duplicate all draw code | `GeometryLayerViewModel.DrawObjects()` (new overload) | All draw logic is already correct in `GeometryLayerViewModel` including protractor ticks, text labels, etc. [VERIFIED: codebase] |
| Output path derivation | Complex string manipulation | `Path.GetFileNameWithoutExtension` + `.pdf` | Standard .NET [ASSUMED] |

---

## Common Pitfalls

### Pitfall 1: BeginPage dimension units confusion
**What goes wrong:** Calling `BeginPage(targetWidthPx, targetHeightPx)` — using pixel values instead of point values. Result: a PDF with pages ~2.77× too large (200/72 ratio).
**Why it happens:** The PDF canvas lives in point-space, but the bitmap we're drawing was rendered in pixel-space. The two are independent — the canvas size (points) and the bitmap resolution (pixels) do not need to match.
**How to avoid:** Always pass `(float)widthPt, (float)heightPt` to `BeginPage`. The bitmap is then stretched to fill that point-space rect when drawn.
**Warning signs:** Exported PDF opens to ~A2 size instead of A4.

### Pitfall 2: Current page objects not in SessionService._allPages
**What goes wrong:** The current page's objects were placed after the last navigation event. `OnObjectsChanged` updates `_allPages[_getCurrentPage()]` on every geometry change, so this is actually handled automatically. However, if `_getCurrentPage()` returns the wrong value (e.g. during an async race), the export could miss objects.
**Why it happens:** `SessionService` uses a `Func<int>` to lazily read `CurrentPage` from `MainViewModel`. This is safe on the UI thread but could be stale in `Task.Run`.
**How to avoid:** In `MainViewModel.ExportPdfAsync`, snapshot `_sessionService.SyncPage(CurrentPage, _geometryService.Objects.ToList())` synchronously before passing to the background export task.

### Pitfall 3: GeometryLayerViewModel.Draw() reads IGeometryService.Objects — not the per-page store
**What goes wrong:** Calling `_geometryLayer.Draw(exportCanvas, exportMapper, ...)` during export will draw only the objects currently loaded in `IGeometryService` (i.e. the current page). All other pages get no annotations.
**Why it happens:** `GeometryLayerViewModel.Draw()` calls `_geometryService.Objects` directly — it has no knowledge of the per-page store in `SessionService`.
**How to avoid:** Add a new public method `DrawObjects(SKCanvas, CoordinateMapper, IReadOnlyList<GeometryObject>, double dpiScale)` to `GeometryLayerViewModel` that accepts an explicit object list instead of reading from `_geometryService`. The export path calls this. The existing `Draw()` method remains unchanged for live screen rendering.
**Warning signs:** Exported PDF shows annotations only on the last viewed page.

### Pitfall 4: SKDocument canvas coordinate vs bitmap coordinate
**What goes wrong:** Drawing `bitmap` at `canvas.DrawBitmap(bitmap, 0, 0)` (point-space origin, implicit size in pixels) causes the image to be tiny (bitmap pixels interpreted as points = 1/72 inch each).
**How to avoid:** Always use `canvas.DrawBitmap(bitmap, new SKRect(0, 0, (float)widthPt, (float)heightPt))` — stretch the full-resolution bitmap to fill the full point-space page.

### Pitfall 5: Export overwriting source PDF
**What goes wrong:** If the student names their source file something that ends in `-annotated.pdf` and exports it, the next export writes `filename-annotated-annotated.pdf`. Edge case, not a crash, but worth noting.
**How to avoid:** The `-annotated` suffix is appended once; strip any existing `-annotated` suffix before appending, or just document the behavior. Not a blocking concern for v1.

### Pitfall 6: File write to read-only directory
**What goes wrong:** School machines may have read-only PDF directories. `File.Create` throws `UnauthorizedAccessException`.
**How to avoid:** Wrap the export in a try/catch for `IOException` and `UnauthorizedAccessException` (matching the existing `TrySaveAsync` pattern in `SessionService`). Show an error toast if the write fails.

---

## Code Examples

### SKDocument multi-page PDF write (verified API)

```csharp
// Source: [CITED: learn.microsoft.com/dotnet/api/skiasharp.skdocument]
// Page dimensions passed to BeginPage are in PDF points (NOT pixels).
using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
using var pdfDoc = SKDocument.CreatePdf(fileStream);

foreach (var (pageIndex, objects) in allPages)
{
    var (widthPt, heightPt) = pdfService.GetPageDimensionsPt(pageIndex - 1);  // pageIndex is 1-based
    int wPx = (int)Math.Round(widthPt  * 200.0 / 72.0);
    int hPx = (int)Math.Round(heightPt * 200.0 / 72.0);

    // Render source page as bitmap at 200 DPI
    using var bitmap = await pdfService.GetPageBitmapAsync(pageIndex - 1, wPx, hPx);
    if (bitmap is null) continue;

    // Create offscreen surface for compositing geometry on top of PDF bitmap
    using var surface = SKSurface.Create(new SKImageInfo(wPx, hPx));
    var c = surface.Canvas;
    c.DrawBitmap(bitmap, new SKRect(0, 0, wPx, hPx));

    // Build export mapper: Scale = (dpiScale * 96/72) * zoomFactor = 200/72
    // => with dpiScale=1.0: zoomFactor = 200.0/96.0
    var exportMapper = new CoordinateMapper(
        zoomFactor:   200.0 / 96.0,
        dpiScale:     1.0,
        pageWidthPt:  widthPt,
        pageHeightPt: heightPt,
        canvasOriginX: 0,
        canvasOriginY: 0);

    geometryLayer.DrawObjects(c, exportMapper, objects, dpiScale: 200.0 / 96.0);

    // Snapshot and write to PDF page
    using var snapshot = surface.Snapshot();
    using var image = SKImage.FromBitmap(bitmap);  // or from snapshot
    var pageCanvas = pdfDoc.BeginPage((float)widthPt, (float)heightPt);
    pageCanvas.DrawImage(snapshot.ToRasterImage(), new SKRect(0, 0, (float)widthPt, (float)heightPt));
    pdfDoc.EndPage();
}
pdfDoc.Close();
```

### Export CoordinateMapper scale derivation

```csharp
// Source: [VERIFIED: CoordinateMapper.cs line 25]
// CoordinateMapper.Scale = (dpiScale * 96.0 / 72.0) * zoomFactor
// For 200 DPI export: target Scale = 200.0 / 72.0
// With dpiScale = 1.0: zoomFactor = 200.0 / 96.0
// Verify: (1.0 * 96.0/72.0) * (200.0/96.0) = 200.0/72.0  ✓
const double ExportDpi = 200.0;
double exportZoomFactor = ExportDpi / 96.0;  // ≈ 2.0833
var exportMapper = new CoordinateMapper(exportZoomFactor, 1.0, widthPt, heightPt, 0.0, 0.0);
```

### ISessionService — new accessor needed

```csharp
// Source: [VERIFIED: ISessionService.cs, SessionService.cs]
// New method to add to ISessionService:
IReadOnlyDictionary<int, IReadOnlyList<GeometryObject>> GetAllPages();

// Implementation in SessionService:
public IReadOnlyDictionary<int, IReadOnlyList<GeometryObject>> GetAllPages()
    => _allPages.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<GeometryObject>)kvp.Value);
```

### GeometryLayerViewModel — new DrawObjects overload needed

```csharp
// Source: [VERIFIED: GeometryLayerViewModel.cs — Draw() method at line 165]
// New overload that accepts explicit objects instead of reading _geometryService.Objects:
public void DrawObjects(
    SKCanvas canvas,
    CoordinateMapper mapper,
    IReadOnlyList<GeometryObject> objects,
    double dpiScale = 1.0)
{
    // Same paint-update logic as Draw()
    if (Math.Abs(dpiScale - _lastScale) > 0.001) { /* update paints/fonts */ }
    _currentDpiScaleF = (float)dpiScale;

    foreach (var obj in objects)
        if (!obj.IsSelected) DrawObject(canvas, obj, mapper, selected: false);
    // Note: no sub-point targets for export — IsSelected is irrelevant
}
```

### Output path derivation

```csharp
// Source: [ASSUMED — standard .NET path API]
// Input:  "C:\exams\June 2017 QP.pdf"
// Output: "C:\exams\June 2017 QP-annotated.pdf"
private static string BuildAnnotatedPath(string sourcePath)
{
    string dir  = Path.GetDirectoryName(sourcePath) ?? string.Empty;
    string name = Path.GetFileNameWithoutExtension(sourcePath);
    return Path.Combine(dir, name + "-annotated.pdf");
}
```

### MainViewModel ExportPdfCommand wiring

```csharp
// Source: [VERIFIED: MainViewModel.cs — RelayCommand + NotifyCanExecuteChangedFor pattern]
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(CloseFileCommand))]
[NotifyCanExecuteChangedFor(nameof(ExportPdfCommand))]  // add this
private bool _isPdfOpen;

[RelayCommand(CanExecute = nameof(CanExportPdf))]
private async Task ExportPdfAsync()
{
    if (_currentPdfPath is null) return;
    // sync current page objects into session store before export
    _sessionService.SyncPage(CurrentPage, _geometryService.Objects.ToList());
    string outputPath = BuildAnnotatedPath(_currentPdfPath);
    bool ok = await _exportService.ExportAsync(_currentPdfPath, outputPath).ConfigureAwait(false);
    await Application.Current.Dispatcher.InvokeAsync(() =>
    {
        // reuse ToolViewModel StatusMessage for the toast
        _toolVm.StatusMessage = ok
            ? $"Saved: {Path.GetFileName(outputPath)}"
            : "Export failed — check folder permissions";
    });
}
private bool CanExportPdf() => IsPdfOpen;
```

### Top-bar Export button (≥56×56px)

```xml
<!-- Source: [VERIFIED: TopBar.xaml — DockPanel Right-docked chip pattern] -->
<!-- Add after the file chip, before right-docked zoom strip -->
<Border DockPanel.Dock="Left" VerticalAlignment="Center"
        Background="{StaticResource BrushSurface2}"
        BorderBrush="{StaticResource BrushBorder}" BorderThickness="1"
        CornerRadius="8" Margin="0,0,10,0">
    <Button Width="56" Height="56"
            Style="{StaticResource IconButtonStyle}"
            ToolTip="Export annotated PDF"
            Command="{Binding ExportPdfCommand}"
            IsEnabled="{Binding IsPdfOpen}">
        <!-- PDF-down-arrow icon or "Export" label -->
        <Viewbox Width="20" Height="20">
            <Canvas Width="24" Height="24">
                <Path Stroke="{StaticResource BrushInk2}" StrokeThickness="1.8"
                      StrokeStartLineCap="Round" StrokeEndLineCap="Round"
                      Data="M12 3v12M7 11l5 5 5-5M5 19h14"/>
            </Canvas>
        </Viewbox>
    </Button>
</Border>
```

Note: `IconButtonStyle` sets `Width="36"` and `Height="36"` as default setters. Since element-level `Width="56" Height="56"` take WPF precedence over Style setters, this override is safe and established by project precedent (see Phase 2 note: "explicit Width=56 Height=56 used to fit within 148px rail while satisfying gaze floor").

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| WinRT `Windows.Data.Pdf` for rendering | Docnet.Core / PDFium | Phase 1 decision | PDFium used for both view and export; consistent rendering |
| PdfiumViewer (archived) | Docnet.Core | Phase 1 research | Maintained fork, same PDFium engine |
| Practice/Exam mode with live angle readout | Mode removed entirely | Quick task 260528-sj5 | Export has no mode-dependent branching; simplifies export path |

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `Path.Combine(dir, name + "-annotated.pdf")` produces the correct output path on Windows | Code Examples | Low — standard .NET string path API, negligible risk |
| A2 | `SKDocument.Close()` is called implicitly by `Dispose()` so `using` blocks are safe | Code Examples | Low — this is standard SKDocument behavior; if wrong, PDF would be incomplete; mitigate by calling Close() explicitly before Dispose |
| A3 | `SKImage.FromBitmap` creates an `SKImage` backed by the bitmap pixels synchronously with no async required | Code Examples | Low — SkiaSharp static factory; standard behavior |
| A4 | `surface.Snapshot().ToRasterImage()` is the correct approach to get a drawable SKImage from an SKSurface for embedding into PDF canvas | Code Examples | Medium — alternative is `SKImage.FromBitmap`; if `ToRasterImage()` copies GPU texture to CPU incorrectly, the image could be blank; use `SKImage.FromBitmap` as fallback |

---

## Open Questions

1. **GeometryLayerViewModel.DrawObjects overload vs alternative approach**
   - What we know: `Draw()` reads `_geometryService.Objects` directly (line 188). Export needs per-page objects from SessionService.
   - What's unclear: Whether adding a public overload to `GeometryLayerViewModel` is cleaner than passing through `PdfExportService` and manually calling the DrawObject switch directly.
   - Recommendation: Add the public `DrawObjects(SKCanvas, CoordinateMapper, IReadOnlyList<GeometryObject>, double)` overload. It's minimal code (5 lines delegating to the existing private `DrawObject` method) and avoids duplicating the entire draw switch.

2. **Toast display for export result**
   - What we know: The current toast (`StatusToast` in PdfCanvas.xaml) is driven by `ToolVmStatusMessage` which is polled on every `MouseMove`. A successful export toast might not appear until the next mouse move.
   - What's unclear: Whether this latency is acceptable (user clicks the button, no feedback until they move the mouse).
   - Recommendation: Add a method `ShowToastImmediate(string)` to `PdfCanvasViewModel` that both sets `_toolVm.StatusMessage` and fires `InvalidationRequested`, plus directly calls `UpdateStatusToast` via an event the code-behind subscribes to. Alternatively: `PdfCanvasViewModel` can fire a new `ToastRequested` event that PdfCanvas.xaml.cs handles immediately by calling `UpdateStatusToast`.

3. **Selected object state during export**
   - What we know: `GeometryObject.IsSelected` is transient (`[JsonIgnore]`). Objects in `SessionService._allPages` were snapshotted at `SyncPage` time and may have `IsSelected = false` (since selection is cleared on page navigation or they're in-memory snapshots).
   - What's unclear: Should selected objects be drawn in accent color or normal color in the export?
   - Recommendation: Export always draws all objects in "normal" (unselected) style. The selection chrome (cobalt accent, sub-point rings) is UI feedback, not exam annotation. In the `DrawObjects` export overload, skip the selected-object pass entirely and draw all objects as unselected.

---

## Environment Availability

Step 2.6: SKIPPED — this phase is purely in-process code using already-installed NuGet packages. No external tools, services, CLIs, or databases are required beyond what is already present in the project.

---

## Validation Architecture

`workflow.nyquist_validation` is explicitly `false` in `.planning/config.json`. This section is SKIPPED.

---

## Security Domain

`security_enforcement` is not set in config.json (no such key present). However, this phase has a narrow threat surface:

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V5 Input Validation | Minimal | Output path derived from `_currentPdfPath` (set by `OpenFileAsync` from a file dialog) — no user-typed path injection risk |
| V6 Cryptography | No | No cryptographic operations |
| V2 Authentication | No | Desktop app, single user, no auth layer |

The only security-adjacent concern is file write path: the output path is derived programmatically (`sourcePath + "-annotated.pdf"`) with no user-controlled components beyond the source PDF path, which was set by the OS file dialog. No sanitization needed beyond the `IOException/UnauthorizedAccessException` catch.

---

## Sources

### Primary (HIGH confidence)
- `MathGaze/Services/DocnetPdfService.cs` — `GetPageBitmapAsync`, `GetPageDimensionsPt` signatures and behavior verified
- `MathGaze/ViewModels/GeometryLayerViewModel.cs` — `Draw()` method signature, paint cache pattern, all DrawObject logic verified
- `MathGaze/Services/SessionService.cs` — `_allPages` dictionary structure, `SyncPage` and `OnObjectsChanged` verified
- `MathGaze/Services/ISessionService.cs` — interface gap (no GetAllPages) verified
- `MathGaze/ViewModels/MainViewModel.cs` — command pattern, `_currentPdfPath` field, `IsPdfOpen`, `_geometryService.Objects`, `_sessionService.SyncPage` call pattern verified
- `MathGaze/Core/CoordinateMapper.cs` — Scale formula `(dpiScale * 96.0/72.0) * zoomFactor` verified at line 25
- `MathGaze/Views/TopBar.xaml` — DockPanel layout, button style, sizing pattern verified
- `MathGaze/Views/PdfCanvas.xaml` + `PdfCanvas.xaml.cs` — toast mechanism (`StatusToast`, `UpdateStatusToast`) verified
- `MathGaze/Styles/AppStyles.xaml` — `IconButtonStyle` Width/Height setter precedence noted
- `MathGaze/App.xaml.cs` — DI registration pattern verified
- [CITED: learn.microsoft.com/dotnet/api/skiasharp.skdocument.createpdf] — `SKDocument.CreatePdf(Stream)` overloads verified
- [CITED: learn.microsoft.com/dotnet/api/skiasharp.skdocument.beginpage] — `BeginPage(float, float)` dimensions in PDF points verified

### Secondary (MEDIUM confidence)
- [CITED: learn.microsoft.com/dotnet/api/skiasharp.skdocument.createpdf] — `SKDocumentPdfMetadata` overload available for adding PDF title/author metadata if desired

### Tertiary (LOW confidence — see Assumptions Log)
- A2: `SKDocument.Dispose()` calls `Close()` implicitly — training knowledge, not doc-verified this session

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all packages are already in the project; verified in codebase
- Architecture: HIGH — all key APIs verified in source files and official docs
- Pitfalls: HIGH — most verified directly from codebase inspection; A2/A4 flagged LOW in Assumptions Log
- Documentation cleanup: HIGH — files identified and referenced in CONTEXT.md D-06

**Research date:** 2026-05-29
**Valid until:** 2026-07-01 (stable SkiaSharp APIs; no time-pressure dependencies)
