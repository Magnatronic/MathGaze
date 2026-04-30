# Phase 1: Foundation - Research

**Researched:** 2026-04-30
**Domain:** WPF + SkiaSharp + Docnet.Core — self-contained desktop app, PDF rendering, coordinate mapping
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Deployment validation is a tail confirmation step, not a front-loaded spike gate. Build the full Phase 1 feature set first; confirm self-contained EXE publish works at the end.
- **D-02:** No separate "bare EXE test" plan as plan 1. The WPF shell itself (with PDF rendering wired up) is the first real artifact.
- **D-03:** Full 3-column skeleton at the end of Phase 1: TopBar + left tool rail (6 visual stub buttons, no behavior) + PDF canvas + empty right rail placeholder.
- **D-04:** The 6 stub buttons in the left rail represent: Select, Point, Line, Circle, Protractor, Text — visually present with icons, not wired to behavior.
- **D-05:** TopBar is fully functional in Phase 1: MathGaze branding, filename display, Open/Close controls, mode toggle chip, zoom (−/+/fit-page), page navigation (prev/counter/next).
- **D-06:** Use Docnet.Core as the PDF library. Wraps PDFium. No alternate library evaluation needed.
- **D-07:** Wire up the ScrollRail (Up / Down / Page-Up / Page-Down click buttons) in Phase 1 alongside zoom.
- **D-08:** Scroll buttons use the same click-to-commit model as all other interactions. No drag gestures, no mouse wheel required.
- **D-09:** CoordinateMapper translates PDF points → screen pixels (rendering) and screen pixels → PDF points (hit-testing). Both directions built and unit-tested in Phase 1.
- **D-10:** Unit tests cover zoom 0.5×/1×/1.5×/2× × DPI 96/120/144/192 (100/125/150/200%).

### Claude's Discretion

- Exact zoom step size (10%, 25%, or other increment) — keep it gaze-friendly (never more than 3 clicks to reach a useful zoom range)
- Default zoom level on open (fit-page is the sensible default)
- Stub button icon set for Phase 1 left rail
- Right rail placeholder visual (empty state, no content required)
- How the ScrollRail scroll amount maps to canvas offset (e.g., small = 50px, page = viewport height)

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.

</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| CORE-01 | User can open a PDF file from local disk | OpenFileDialog in ViewModel via service abstraction; Docnet.Core loads file path |
| CORE-02 | User can navigate to any page in the loaded PDF | Docnet.Core IDocReader exposes page count; PageReader per page index |
| CORE-03 | User can zoom in and out of the PDF view | SKXamlCanvas ScaleTransform + re-render at zoom; CoordinateMapper tracks zoom factor |
| CORE-04 | App runs as a self-contained EXE with no admin install, no pre-installed runtime | `PublishSingleFile + SelfContained + RuntimeIdentifier=win-x64 + IncludeNativeLibrariesForSelfExtract=true` |

</phase_requirements>

---

## Summary

Phase 1 establishes the entire application skeleton: WPF shell with 3-column layout, fully functional TopBar, PDF rendering via Docnet.Core into a SkiaSharp canvas, page navigation, zoom, scroll, and the CoordinateMapper class that every subsequent phase depends on. It is a greenfield project with no prior code.

The stack is firmly decided in CLAUDE.md: .NET 9, WPF, SkiaSharp 3.x (via SkiaSharp.Views.WPF), Docnet.Core 2.6.0, CommunityToolkit.Mvvm 8.4.2, and Microsoft.Extensions.DependencyInjection. All libraries are verified against NuGet as of 2026-04-30.

The primary technical risk in Phase 1 is the self-contained single-EXE publish with a native PDFium DLL bundled inside. The `IncludeNativeLibrariesForSelfExtract` flag causes native DLLs to be extracted to `%TEMP%/.net` at first run — acceptable for school machines, but must be tested on a real school machine at the end of the phase. The **local development machine has .NET 8.0 SDK only** — installing .NET 9 SDK is a Wave 0 prerequisite before any code can be written.

**Primary recommendation:** Build in this sequence — (1) install .NET 9 SDK, (2) scaffold WPF project with DI wiring, (3) get a PDF page rendering on screen via Docnet.Core + SkiaSharp, (4) add TopBar + rails, (5) wire CoordinateMapper with unit tests, (6) verify self-contained publish.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET 9 | 9.0.x (STS) | Runtime | Self-contained publish bundles runtime; `--self-contained true` on win-x64 |
| WPF | Inbox with .NET 9 | UI shell, XAML layout | Zero external OS dependencies; full UIA accessibility for Grid 3; xaml-native |
| SkiaSharp | 3.119.2 | GPU-accelerated 2D canvas | Draws PDF bitmap + geometry vector layer; hardware-accelerated via OpenGL |
| SkiaSharp.Views.WPF | 3.119.2 | WPF host for SkiaSharp | Provides `SKXamlCanvas` — a WPF element that fires `PaintSurface` per frame |
| Docnet.Core | 2.6.0 | PDF rendering to pixel arrays | Wraps PDFium (Chrome's engine); MIT licence; bundles PDFium native DLL via NuGet |
| CommunityToolkit.Mvvm | 8.4.2 | MVVM boilerplate reduction | Source-generator-based `[ObservableProperty]`/`[RelayCommand]`; platform-agnostic |
| Microsoft.Extensions.DependencyInjection | 10.0.7 | DI container | Lightweight; standard across all modern .NET; enables testable ViewModels |

[VERIFIED: nuget.org — all versions confirmed 2026-04-30]

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Text.Json | Inbox (.NET 9) | JSON for session sidecar | Used in Phase 4; no additional NuGet required |
| xunit.v3 | 3.2.2 | Unit test framework | CoordinateMapper tests; xunit v2 is now deprecated/legacy |
| Microsoft.Extensions.Logging | 10.0.x | Structured logging | Debug builds only; diagnose gaze timing and render issues |

[VERIFIED: nuget.org — xunit.v3 3.2.2 published 2026-01-14; xunit v2 is deprecated as of 2025]

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Docnet.Core 2.6.0 | PDFiumSharp, PDFiumCore | Both are maintained; Docnet.Core chosen per D-06 locked decision |
| SkiaSharp.Views.WPF | WriteableBitmap + SKSurface.Create | WriteableBitmap is CPU-only; SKXamlCanvas has hardware-accelerated path |
| xunit.v3 | NUnit, MSTest | xunit.v3 is the modern replacement for deprecated xunit v2; works with .NET 9 |

### Installation

```bash
# Core app packages
dotnet add package SkiaSharp --version 3.119.2
dotnet add package SkiaSharp.Views.WPF --version 3.119.2
dotnet add package Docnet.Core --version 2.6.0
dotnet add package CommunityToolkit.Mvvm --version 8.4.2
dotnet add package Microsoft.Extensions.DependencyInjection --version 10.0.7

# Test project
dotnet add package xunit.v3 --version 3.2.2
dotnet add package Microsoft.NET.Test.Sdk
```

**Version verification:** All versions confirmed against NuGet registry on 2026-04-30.
[VERIFIED: nuget.org registry]

---

## Architecture Patterns

### Recommended Project Structure

```
MathGaze/
├── MathGaze.csproj               # WPF app, net9.0-windows, PublishSingleFile
├── App.xaml / App.xaml.cs        # DI host setup, no StartupUri
├── MainWindow.xaml / .cs         # Shell — 3-column grid, DI-constructed
├── app.manifest                  # PerMonitorV2 DPI awareness
│
├── ViewModels/
│   ├── MainViewModel.cs          # Top-level: open/close PDF, mode, page nav, zoom
│   └── PdfCanvasViewModel.cs     # Canvas state: current page bitmap, scroll offset
│
├── Views/
│   ├── TopBar.xaml / .cs         # Extracted UserControl: branding, file, zoom, page nav
│   ├── ToolRail.xaml / .cs       # Left column: 6 stub ToolTile buttons
│   ├── PdfCanvas.xaml / .cs      # Centre: SKXamlCanvas host
│   ├── ScrollRail.xaml / .cs     # Scroll overlay: Up/Down/PageUp/PageDown buttons
│   └── RightRailPlaceholder.xaml # Empty state placeholder
│
├── Services/
│   ├── IPdfService.cs            # Interface: OpenDocument, GetPage, PageCount
│   ├── DocnetPdfService.cs       # Implementation: Docnet.Core wrapper
│   └── IFileDialogService.cs     # Interface: ShowOpenFileDialog -> string?
│
├── Core/
│   └── CoordinateMapper.cs       # PDF-space <-> screen-space transforms (see below)
│
└── MathGaze.Tests/
    └── CoordinateMapperTests.cs  # xunit.v3 — zoom × DPI matrix
```

### Pattern 1: DI Wiring in App.xaml.cs

**What:** Remove `StartupUri` from App.xaml; build the DI container in `OnStartup`; resolve `MainWindow` from the container.

**When to use:** Always in MVVM WPF — prevents ViewModels being newed directly in code-behind.

```csharp
// Source: [ASSUMED] — standard DI-in-WPF pattern, widely documented
protected override void OnStartup(StartupEventArgs e)
{
    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddSingleton<IPdfService, DocnetPdfService>();
            services.AddSingleton<IFileDialogService, FileDialogService>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
        })
        .Build();

    var window = host.Services.GetRequiredService<MainWindow>();
    window.Show();
}
```

### Pattern 2: SKXamlCanvas PaintSurface

**What:** `SKXamlCanvas` (from `SkiaSharp.Views.WPF`) fires `PaintSurface` each render frame. Access `e.Surface.Canvas` to draw. `e.Info.Width/Height` gives pixel dimensions already scaled for DPI.

**When to use:** All canvas drawing — PDF bitmap layer in Phase 1, geometry vector layer in Phase 2+.

```csharp
// Source: [CITED: learn.microsoft.com/dotnet/api/skiasharp.views.windows.skxamlcanvas.paintsurface]
// Namespace is SkiaSharp.Views.Windows even in the WPF package
private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
{
    var canvas = e.Surface.Canvas;
    canvas.Clear(SKColors.LightGray);  // canvas background

    if (_pageBitmap != null)
    {
        // Draw PDF page bitmap, scaled/translated by CoordinateMapper
        var destRect = _coordinateMapper.GetPageDestRect(e.Info.Width, e.Info.Height);
        canvas.DrawBitmap(_pageBitmap, destRect);
    }

    canvas.Flush();
}
```

**DPI note:** `SKXamlCanvas` with `IgnorePixelScaling = false` (default) scales the canvas surface to physical pixels. `e.Info.Width` is in physical pixels. Always use `VisualTreeHelper.GetDpi(this).PixelsPerDip` to convert WPF logical units to physical pixels in the ViewModel layer.
[VERIFIED: learn.microsoft.com SkiaSharp docs, 2026-01-27]

### Pattern 3: Docnet.Core PDF Rendering

**What:** Docnet.Core renders a PDF page to a raw BGRA byte array. You specify target pixel dimensions (which control the render scale). `GetPageWidth()` and `GetPageHeight()` return the actual rendered pixel dimensions.

**When to use:** Loading a new PDF, changing page, changing zoom level — any time `_pageBitmap` needs rebuilding.

```csharp
// Source: [CITED: github.com/GowenGit/docnet/examples/nuget-usage]
// DocLib.Instance is a singleton that lives for app lifetime
using var docReader = DocLib.Instance.GetDocReader(filePath, new PageDimensions(targetWidthPx, targetHeightPx));
using var pageReader = docReader.GetPageReader(pageIndex);

var rawBytes = pageReader.GetImage();     // BGRA byte array
var width    = pageReader.GetPageWidth();
var height   = pageReader.GetPageHeight();

// Create SKBitmap from raw bytes
var imageInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
var bitmap = new SKBitmap(imageInfo);
var gcHandle = GCHandle.Alloc(rawBytes, GCHandleType.Pinned);
try
{
    bitmap.InstallPixels(imageInfo, gcHandle.AddrOfPinnedObject());
}
finally
{
    gcHandle.Free();
}
```

**Pixel target formula:** PDF pages use points (1 point = 1/72 inch). To render at a target screen DPI:
```
targetWidthPx  = (pdfWidthPt  / 72.0) * targetDpi * zoomFactor
targetHeightPx = (pdfHeightPt / 72.0) * targetDpi * zoomFactor
```
A standard A4 exam paper is 595 × 842 points. At 96 DPI × 1× zoom: 794 × 1123 px.
[VERIFIED: pdfium.patagames.com coordinate system docs; Docnet.Core GitHub examples]

**Critical — PageDimensions landscape constraint:** Docnet.Core 2.6.0 requires `dimOne <= dimTwo` in `PageDimensions`. If the PDF page is landscape (width > height), swap the values and apply a rotation. Tracked in Docnet.Core issue #72.
[CITED: github.com/GowenGit/docnet/issues/72]

### Pattern 4: CoordinateMapper

**What:** A pure C# class (no WPF dependencies) that encodes the affine transform between two coordinate spaces. Dependency-injectable; unit-testable in isolation.

**When to use:** Every place that converts a click position on the screen to a position on the PDF page (or vice versa).

```csharp
// Source: [ASSUMED] — derived from PDF coordinate conventions + WPF DPI model
public sealed class CoordinateMapper
{
    // PDF coordinate space: origin bottom-left, Y-up, units = points (1/72 inch)
    // Screen coordinate space: origin top-left, Y-down, units = physical pixels
    //
    // Inputs at construction time (updated on zoom/scroll/DPI change):
    //   zoomFactor     — current zoom (1.0 = 100%)
    //   dpiScale       — PixelsPerDip from VisualTreeHelper (e.g. 1.5 at 144dpi)
    //   pageWidthPt    — PDF page width in points
    //   pageHeightPt   — PDF page height in points
    //   canvasOriginX  — X offset of page's top-left corner in canvas pixels (for panning)
    //   canvasOriginY  — Y offset of page's top-left corner in canvas pixels

    public SKPoint PageToScreen(double xPt, double yPt) { ... }
    public (double xPt, double yPt) ScreenToPage(SKPoint screenPx) { ... }
    public SKRect GetPageDestRect(int canvasWidthPx, int canvasHeightPx) { ... }
}
```

**Unit test matrix (D-10):** Zoom {0.5, 1.0, 1.5, 2.0} × DPI {96, 120, 144, 192} = 16 combinations. Each test asserts:
1. Round-trip: `ScreenToPage(PageToScreen(pt)) ≈ pt` within floating-point epsilon
2. Page boundary: top-left PDF point maps to canvas origin; bottom-right maps to `(pageWidthPx, pageHeightPx)`

### Pattern 5: MVVM Observable Properties

**What:** `[ObservableProperty]` attribute on private fields in `partial` classes that inherit from `ObservableObject` generates the full property + `PropertyChanged` notification.

**When to use:** All ViewModel bindable state.

```csharp
// Source: [CITED: learn.microsoft.com/dotnet/communitytoolkit/mvvm/generators/observableproperty]
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private int    _currentPage = 1;
    [ObservableProperty] private int    _totalPages = 0;
    [ObservableProperty] private double _zoomFactor = 1.0;
    [ObservableProperty] private bool   _isPracticeMode = true;

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var path = _fileDialogService.ShowOpenFileDialog("PDF files|*.pdf");
        if (path is null) return;
        // load PDF...
    }
}
```

### Pattern 6: PerMonitorV2 DPI Manifest

**What:** An `app.manifest` file that declares `PerMonitorV2` DPI awareness ensures WPF scales correctly on high-DPI school displays (125%, 150%, 200% are common).

**When to use:** Required for this project — must be in place before testing DPI scaling.

```xml
<!-- app.manifest — add to project with <ApplicationManifest>app.manifest</ApplicationManifest> -->
<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
  <application xmlns="urn:schemas.microsoft.com/SMI/2005/WindowsSettings">
    <windowsSettings>
      <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true</dpiAware>
      <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
    </windowsSettings>
  </application>
</assembly>
```
[CITED: learn.microsoft.com/windows/win32/hidpi/declaring-managed-apps-dpi-aware]

### Anti-Patterns to Avoid

- **Recreating `DocLib.Instance` per render:** DocLib is a singleton that must live for the entire app lifetime. One instance per app; open/close IDocReader per document.
- **Mixing WPF logical pixels with Skia physical pixels:** WPF controls measure in DIUs (logical pixels). SKXamlCanvas `e.Info.Width` is in physical pixels. Never pass one where the other is expected — CoordinateMapper must track `dpiScale` explicitly.
- **Using `Assembly.Location` in single-file publish:** Returns empty string. Always use `AppContext.BaseDirectory` to find files next to the EXE.
  [CITED: learn.microsoft.com/dotnet/core/deploying/single-file/overview]
- **Calling `GetImage()` on the UI thread:** Docnet.Core renders synchronously; call it on a background thread (`Task.Run`) and marshal the SKBitmap back to invalidate the canvas.
- **Treating BGRA bytes as RGBA:** Docnet.Core `GetImage()` returns BGRA (Blue-Green-Red-Alpha). Use `SKColorType.Bgra8888` when creating `SKImageInfo`. Using RGBA will produce colour-shifted images.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| PDF to pixel rendering | Custom PDFium P/Invoke | Docnet.Core | PDFium has 400+ edge-case behaviours; mixed colour/mono PDFs, embedded fonts, form fields |
| DPI-aware canvas | Custom WriteableBitmap + D3DImage | SKXamlCanvas | Hardware backend, correct DPI handling, no interop ceremony |
| Property-change notification | Manual INotifyPropertyChanged | CommunityToolkit.Mvvm `[ObservableProperty]` | Eliminates boilerplate; source-gen = no reflection |
| Command binding | Manual ICommand | `[RelayCommand]` | Handles async, cancellation tokens, CanExecute automatically |
| PDF coordinate maths | Custom PDF spec parsing | PDFium (via Docnet.Core) | PDF coordinate system is non-trivial (Y-axis inverted, multiple coordinate spaces per page) |

**Key insight:** PDFium handles GCSE exam PDFs correctly because it is Chrome's production PDF engine. Any custom parser will fail on exam paper edge cases (mixed-resolution scans, form fields, annotation layers).

---

## Common Pitfalls

### Pitfall 1: Docnet.Core PageDimensions landscape constraint

**What goes wrong:** `PageDimensions(widthPx, heightPx)` throws or produces corrupt output when `widthPx > heightPx` (landscape PDF page).
**Why it happens:** The library enforces `dimOne <= dimTwo`. GCSE papers are typically A4 portrait, but answer booklets can be landscape.
**How to avoid:** Check page orientation from a first-pass `GetPageWidth()`/`GetPageHeight()` call at natural dimensions. If landscape, swap dimensions and apply a 90° rotation to the rendered SKBitmap.
**Warning signs:** `ArgumentException` at document open time, or pages that appear rotated 90°.
[CITED: github.com/GowenGit/docnet/issues/72]

### Pitfall 2: Native DLL extraction in single-file publish

**What goes wrong:** The PDFium native DLL cannot be bundled inside the managed single-file EXE — it must be extracted to disk before the app starts. On first launch, .NET extracts it to `%TEMP%/.net/[app-name]/[hash]/`. Subsequent runs use the cache.
**Why it happens:** `IncludeNativeLibrariesForSelfExtract=true` enables extraction-based bundling; the DLL is not truly embedded in the EXE like managed code.
**How to avoid:** Set `IncludeNativeLibrariesForSelfExtract=true` in the project file. Verify the school machine's `%TEMP%` is writable (it always is for the logged-in user). Do not delete `%TEMP%/.net` between sessions — re-extraction is slow.
**Warning signs:** `DllNotFoundException` for `pdfium.dll` on first run on a clean machine.
[CITED: learn.microsoft.com/dotnet/core/deploying/single-file/overview]

### Pitfall 3: WPF DIU vs. Skia physical pixel confusion

**What goes wrong:** CoordinateMapper uses the wrong pixel scale, causing geometry clicks in Phase 2 to land at the wrong PDF coordinates on high-DPI displays.
**Why it happens:** WPF measures in Device-Independent Units (1 DIU = 1 pixel at 96 DPI). At 150% scaling, 1 DIU = 1.5 physical pixels. SKXamlCanvas reports dimensions in physical pixels. Mixing the two silently produces wrong coordinates.
**How to avoid:** Always obtain `dpiScale = VisualTreeHelper.GetDpi(skCanvas).PixelsPerDip` and pass it into `CoordinateMapper`. Unit-test at 96/120/144/192 DPI (D-10).
**Warning signs:** Geometry tools appearing offset from where the user clicked, but only on high-DPI machines.

### Pitfall 4: Docnet.Core thread safety

**What goes wrong:** Calling `DocLib.Instance` or `IDocReader` methods from multiple threads simultaneously can corrupt native state or crash.
**Why it happens:** PDFium's C API is not thread-safe by default.
**How to avoid:** Use a single `SemaphoreSlim(1,1)` around all Docnet.Core calls in `DocnetPdfService`. Render on a `Task.Run` thread but gate with the semaphore.
**Warning signs:** `AccessViolationException` or intermittent crashes during rapid page navigation.
[ASSUMED — based on general PDFium thread safety knowledge; verify against Docnet.Core issues if crashes occur]

### Pitfall 5: Missing .NET 9 SDK on development machine

**What goes wrong:** The development machine has .NET 8.0 only. `dotnet new` will target .NET 8 by default; WPF source generators may behave differently. The project file needs `<TargetFramework>net9.0-windows</TargetFramework>`.
**Why it happens:** .NET 9 SDK must be installed separately; runtime and SDK are distinct.
**How to avoid:** Install .NET 9 SDK (9.0.x) as Wave 0 before any code is written. Verify with `dotnet --list-sdks`.
**Warning signs:** Build error `The current .NET SDK does not support targeting .NET 9.0`.
[VERIFIED: local environment audit — only .NET 8.0.13 runtime found in `C:/Program Files/dotnet/shared/`]

### Pitfall 6: .NET 9 WPF single-file publish regression (RC1 issue)

**What goes wrong:** In .NET 9.0 RC1, a regression caused `Presentation*.dll` to be missing from the published output. This was tracked and resolved before the final release.
**Why it happens:** Known SDK regression.
**How to avoid:** Use .NET 9.0 final SDK (9.0.1 or later, not RC builds). Verify publish output includes `PresentationFramework.dll` (or its absence if fully bundled).
**Warning signs:** `FileNotFoundException` for `PresentationFramework.dll` on the target machine.
[CITED: github.com/dotnet/sdk/issues/43461 — RC1 regression, resolved in final release]

---

## Code Examples

### Project File (minimum config for self-contained single EXE)

```xml
<!-- Source: [CITED: learn.microsoft.com/dotnet/core/deploying/single-file/overview] -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net9.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
    <PublishSingleFile>true</PublishSingleFile>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    <!-- Force Docnet.Core to use x64 PDFium binary -->
    <DocnetRuntime>win-x64</DocnetRuntime>
  </PropertyGroup>
</Project>
```

### Publish Command

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

### SKXamlCanvas XAML Placement

```xml
<!-- Source: [CITED: learn.microsoft.com SkiaSharp API docs] -->
<xmlns:skia="clr-namespace:SkiaSharp.Views.WPF;assembly=SkiaSharp.Views.WPF"/>
...
<skia:SKXamlCanvas x:Name="PdfCanvas"
    HorizontalAlignment="Stretch"
    VerticalAlignment="Stretch"
    PaintSurface="OnPaintSurface"
    IgnorePixelScaling="False"/>
```

### ToolTile Button (≥56×56px gaze target)

```xml
<!-- Source: [ASSUMED] — derived from docs/shared.jsx ToolTile spec -->
<!-- Width=84 Height=56 matches shared.jsx: T.s(84) × T.s(56) at comfortable density -->
<Button Width="84" Height="56" Style="{StaticResource ToolTileStyle}">
    <StackPanel Orientation="Horizontal">
        <Path Data="{StaticResource CursorIcon}" Width="24" Height="24"/>
        <TextBlock Text="Select" FontSize="12" FontWeight="SemiBold" Margin="8,0,0,0"/>
    </StackPanel>
</Button>
```

### CoordinateMapper Unit Test Structure

```csharp
// Source: [ASSUMED] — derived from D-10 requirements
[Theory]
[InlineData(0.5, 96)]  [InlineData(0.5, 120)]  [InlineData(0.5, 144)]  [InlineData(0.5, 192)]
[InlineData(1.0, 96)]  [InlineData(1.0, 120)]  [InlineData(1.0, 144)]  [InlineData(1.0, 192)]
[InlineData(1.5, 96)]  [InlineData(1.5, 120)]  [InlineData(1.5, 144)]  [InlineData(1.5, 192)]
[InlineData(2.0, 96)]  [InlineData(2.0, 120)]  [InlineData(2.0, 144)]  [InlineData(2.0, 192)]
public void RoundTrip_PageToScreenToPage_PreservesCoordinates(double zoom, double dpi)
{
    var mapper = new CoordinateMapper(
        zoomFactor: zoom,
        dpiScale: dpi / 96.0,
        pageWidthPt: 595,   // A4 width
        pageHeightPt: 842); // A4 height

    var original = new SKPoint(297.5f, 421f);  // centre of page in PDF points
    var screen   = mapper.PageToScreen(original.X, original.Y);
    var roundTrip = mapper.ScreenToPage(screen);

    Assert.Equal(original.X, roundTrip.xPt, precision: 3);
    Assert.Equal(original.Y, roundTrip.yPt, precision: 3);
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| xunit v2 (`xunit` package) | xunit.v3 (`xunit.v3` package) | xunit v2 deprecated Jan 2025 | Use `xunit.v3` 3.2.2 for new projects |
| PdfiumViewer (pvginkel) | Docnet.Core or PDFiumSharp | 2021 (archived) | PdfiumViewer is archived; use Docnet.Core per D-06 |
| SkiaSharp 2.88.x | SkiaSharp 3.119.x | Feb 2026 | v3 has breaking changes in API; use 3.x for new projects |
| `<SingleFilePublish>` without `IncludeNativeLibrariesForSelfExtract` | `IncludeNativeLibrariesForSelfExtract=true` for native DLLs | .NET 6+ | Native DLLs must be extracted to disk; managed DLLs are embedded in memory |

**Deprecated/outdated:**
- `xunit` 2.9.3: deprecated as of Jan 2025; use `xunit.v3`
- `PdfiumViewer` (pvginkel/PdfiumViewer): archived; never use for new projects
- WinUI 3 + Win2D: deployment-blocked on school machines (MSIX requirement). Not relevant to this phase.

---

## Runtime State Inventory

Step 2.5: SKIPPED — this is a greenfield phase. No existing runtime state to inventory; there are no databases, services, OS registrations, or build artifacts to migrate.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 9 SDK | All compilation and publish | **NOT FOUND** | — | Install .NET 9 SDK (9.0.x) — blocking |
| .NET 8.0 runtime | Existing machine | Found | 8.0.13 | N/A (wrong version for this project) |
| Windows Desktop App Runtime 8.0 | Existing machine | Found | 8.0.13 | N/A (wrong version for this project) |
| NuGet (nuget.org) | Package restore | Assumed available | — | Offline pack for air-gapped school machines |
| Visual Studio or VS Code | Development IDE | Unknown | — | Notepad++ + `dotnet` CLI (not recommended) |

**Missing dependencies with no fallback:**
- **.NET 9 SDK** — must be installed before any `dotnet build` or `dotnet publish` command will work. Download from https://dotnet.microsoft.com/en-us/download/dotnet/9.0 — the Win x64 SDK installer.

**Missing dependencies with fallback:**
- Visual Studio — `dotnet` CLI alone is sufficient to build and publish, but VS 2022 (17.x) provides XAML designer. Not required.

---

## Validation Architecture

`workflow.nyquist_validation` is explicitly `false` in `.planning/config.json` — this section is skipped.

---

## Security Domain

`security_enforcement` is not set in `.planning/config.json` — treat as enabled. However, this phase has a narrow security surface:

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | No | Single-user local app; no accounts |
| V3 Session Management | No | No network sessions |
| V4 Access Control | No | Single-user; no multi-user permissions |
| V5 Input Validation | Yes (limited) | File path from OpenFileDialog (OS-validated); PDF bytes from local disk |
| V6 Cryptography | No | No cryptographic operations in Phase 1 |

### Known Threat Patterns for Stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Malicious PDF triggering PDFium exploit | Tampering | PDFium is kept current (NuGet update path); Docnet.Core 2.6.0 uses PDFium from the NuGet binary package |
| File path injection via OpenFileDialog | Tampering | `OpenFileDialog` returns OS-validated paths; no path concatenation; `File.Exists()` check before loading |
| Temp directory DLL hijacking | Elevation of Privilege | PDFium DLL extracted to `%TEMP%/.net/[app]/[content-hash]/` — content-addressed, tamper-resistant |

**Overall assessment:** Phase 1 has a low security surface. The primary risk is PDFium CVEs — mitigate by keeping Docnet.Core on the latest stable version. No network, no auth, no user-supplied code execution paths.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Docnet.Core thread safety requires a mutex around all calls | Common Pitfalls #4 | If wrong, no crashes, but code has unnecessary locking overhead |
| A2 | ToolTile sizing (84×56 WPF DIUs) maps correctly from shared.jsx T.s(84)×T.s(56) | Code Examples | If wrong, stub buttons below 56px floor — fix before Phase 2 |
| A3 | DI host setup via `Host.CreateDefaultBuilder()` is the standard pattern in WPF .NET 9 | Architecture Pattern 1 | If wrong, DI wiring still works but startup code differs |
| A4 | Docnet.Core is safe to call from a `Task.Run` background thread with a lock | DocnetPdfService design | If the library has thread-local state, a lock is insufficient; switch to a dedicated render thread |

---

## Open Questions

1. **Docnet.Core and AnyCPU builds**
   - What we know: Docnet.Core uses a `.targets` file to copy the correct PDFium native DLL. Setting `<DocnetRuntime>win-x64</DocnetRuntime>` in the project file forces x64.
   - What's unclear: Whether `PublishSingleFile` with `IncludeNativeLibrariesForSelfExtract` correctly bundles the `win-x64` PDFium DLL when the project is compiled as `win-x64`.
   - Recommendation: Verify during the deployment confirmation task by running `dotnet publish -r win-x64 -c Release` and checking that `pdfium.dll` is NOT loose in the output folder (it should be bundled). If it is loose, add `<ExcludeFromSingleFile>false</ExcludeFromSingleFile>` metadata to the DLL.

2. **Zoom step size for gaze usability**
   - What we know: Discretionary per CONTEXT.md. Design shows "110%" in the TopBar. Need ≤ 3 clicks to reach any useful zoom range.
   - Recommendation: Use 25% increments (50%, 75%, 100%, 125%, 150%, 175%, 200%). A student needs ≤ 3 clicks to go from fit-page (~85% for A4 on 1080p) to 150%. This is within the 3-click rule.

3. **Scroll amount calibration**
   - What we know: Discretionary per CONTEXT.md. Small = some pixels, Page = viewport height.
   - Recommendation: Small scroll = 120px (physical), Page scroll = `canvasHeight * 0.85`. The 0.85 multiplier preserves context overlap so the student can see where they were.

---

## Sources

### Primary (HIGH confidence)
- [CITED: nuget.org/packages/Docnet.Core] — version 2.6.0, published 2023-09-04, 4.4M downloads
- [CITED: nuget.org/packages/SkiaSharp] — version 3.119.2, published 2026-02-07
- [CITED: nuget.org/packages/SkiaSharp.Views.WPF] — version 3.119.2, published 2026-02-07
- [CITED: nuget.org/packages/CommunityToolkit.Mvvm] — version 8.4.2, published 2026-03-25
- [CITED: nuget.org/packages/Microsoft.Extensions.DependencyInjection] — version 10.0.7, published 2026-04-21
- [CITED: nuget.org/packages/xunit.v3] — version 3.2.2, published 2026-01-14
- [CITED: learn.microsoft.com/dotnet/api/skiasharp.views.windows.skxamlcanvas.paintsurface] — PaintSurface API
- [CITED: learn.microsoft.com/dotnet/core/deploying/single-file/overview] — single-file publish, native DLLs
- [CITED: github.com/GowenGit/docnet/examples/nuget-usage/NugetUsageX64/PdfToImageExamples.cs] — rendering pattern
- [CITED: pdfium.patagames.com/help/html/PdfViewer_CoordinateSystems.htm] — PDF coordinate system (points, origin)
- [CITED: learn.microsoft.com/dotnet/communitytoolkit/mvvm/generators/observableproperty] — ObservableProperty API
- [CITED: learn.microsoft.com/windows/win32/hidpi/declaring-managed-apps-dpi-aware] — PerMonitorV2 manifest

### Secondary (MEDIUM confidence)
- [github.com/GowenGit/docnet/issues/72] — PageDimensions landscape constraint
- [github.com/GowenGit/docnet/issues/13] — PageDimensions scaling approach
- [github.com/dotnet/sdk/issues/43461] — .NET 9 RC1 WPF publish regression (confirmed resolved)

### Tertiary (LOW confidence)
- General WPF DI pattern (multiple community sources, cross-verified; no single canonical Microsoft doc)

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all versions verified on NuGet 2026-04-30
- Architecture: HIGH — patterns derived from official SkiaSharp and Docnet.Core docs and examples
- CoordinateMapper design: MEDIUM — math is correct; exact class interface is [ASSUMED] and will be refined during implementation
- Pitfalls: HIGH — sourced from official docs and GitHub issues for each library
- Deployment: HIGH — single-file publish flags from official .NET docs; native DLL behaviour well-documented

**Research date:** 2026-04-30
**Valid until:** 2026-07-30 (90 days; SkiaSharp and CommunityToolkit are actively developed — re-verify versions at that date)
