# Technology Stack: MathGaze

**Project:** MathGaze — native Windows eye-gaze geometry assistant
**Researched:** 2026-04-29
**Research mode:** Ecosystem + Feasibility (deployment question)

---

## Recommendation: WPF + SkiaSharp (not WinUI 3)

The primary candidate (WinUI 3 + Win2D) has a hard deployment blocker for the target environment. The self-contained deployment mode in Windows App SDK does **not** eliminate the need for MSIX infrastructure — it only bundles the Framework package DLLs, while the Main and Singleton packages still require the Windows App SDK MSIX runtime to be registered on the machine. On school/exam machines where MSIX sideloading is disabled by Group Policy, this fails silently or with `0x80070005`. WPF with SkiaSharp has no such dependency: it deploys as a plain folder of DLLs with zero OS-level registration.

This recommendation reverses the stack order in PROJECT.md. WinUI 3 should be treated as a future upgrade path, not the starting point, until Phase 0 validates it on the actual target machine.

---

## Recommended Stack

### Core Framework

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| .NET 9 | 9.x (STS, supported to May 2026) | Runtime | Self-contained .NET publish bundles the runtime into the output folder. No pre-installed .NET required on target machine. Use `--self-contained true` at publish time. |
| WPF | Inbox with .NET 9 | UI shell, layout, XAML controls | Zero external runtime dependencies beyond .NET. Ships since Windows XP era; present on every Windows 10/11 machine but irrelevant for self-contained publish. Well-understood deployment story. Excellent UIA accessibility support for eye-gaze. |

### Rendering Layer

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| SkiaSharp | 3.x (latest stable) | Hardware-accelerated 2D canvas | GPU-accelerated via OpenGL/Vulkan/Metal backends. Draws lines, circles, arcs, and bitmaps with sub-pixel antialiasing. Pure NuGet, no OS-level registration. `SKXamlCanvas` (from `SkiaSharp.Views.WPF`) drops into a WPF layout as a standard XAML element. |
| SkiaSharp.Views.WPF | 3.x (ships with SkiaSharp) | WPF host control for SkiaSharp | `SKXamlCanvas` fires `PaintSurface` on each frame; you draw into an `SKCanvas`. Handles DPI scaling. No interop ceremony. |

**Why not Win2D:** Win2D requires `CanvasControl` inside a WinUI 3 XAML tree. It is not usable from WPF. SkiaSharp is the direct WPF equivalent and is used in production by apps like JetBrains Rider and VS Code's extension host.

**Why not raw Direct2D from WPF:** Possible via D3DImage, but requires unsafe C# interop, complicates the build, and provides no advantage over SkiaSharp for this use case.

### PDF Rendering

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| PdfiumViewer | 2.2.0 (last stable) or PDFium.NET | Render PDF pages to bitmaps | `Windows.Data.Pdf` (WinRT) is usable from unpackaged .NET apps but requires `WindowsAppSDK` or specific WinRT interop boilerplate to call from WPF. PdfiumViewer wraps the Chromium PDFium library which is battle-tested, handles all GCSE exam PDF edge cases (mixed colour/mono, embedded fonts, form fields), and renders to `System.Drawing.Bitmap` trivially. |
| PDFium native binary | Bundled via NuGet | PDF engine | Ships as a native DLL inside the NuGet package. x64 EXE only; one DLL, no registration. |

**Alternative considered — `Windows.Data.Pdf`:** This WinRT API works from unpackaged .NET apps on Windows 10/11 via `Windows.Win32` or direct COM activation. It is lighter than PDFium and avoids native dependencies. However, it renders to an `IRandomAccessStream` (PNG/BMP bytes) which adds an extra decode step, and its DPI-scaling behaviour for high-resolution exam PDFs has known quality issues at non-standard zoom levels. PDFium gives direct bitmap access at any DPI. **Use `Windows.Data.Pdf` only if native PDFium DLL deployment is blocked by school device policies.**

### State and Architecture

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| CommunityToolkit.Mvvm | 8.x | MVVM pattern, INotifyPropertyChanged, RelayCommand, messaging | Source-generator-based; zero reflection overhead. `[ObservableProperty]` and `[RelayCommand]` attributes eliminate boilerplate. Used in the Microsoft Store itself. Platform-agnostic; works identically on WPF and WinUI 3, so a future migration to WinUI 3 requires no ViewModel rewrites. |
| Microsoft.Extensions.DependencyInjection | 9.x | DI container | Lightweight, standard in all .NET 6+ apps. Enables testable ViewModels and injectable services (PDF loader, geometry engine, session persistence). |

### Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Text.Json | Inbox (.NET 9) | JSON serialisation for session sidecar file | Save/load geometry state as JSON next to the PDF. No external dependency. |
| Microsoft.Extensions.Logging | 9.x | Structured logging | Debug builds only; helps diagnose gaze input timing issues and geometry engine errors. |

---

## The Deployment Question: WinUI 3 Self-Contained on School Machines

**Verdict: NOT reliably xcopy-deployable on managed school machines without prior IT setup. Confidence: HIGH (official docs verified).**

### What "self-contained" actually means for WinUI 3

Setting `<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>` in the project file causes the Windows App SDK **Framework package** DLLs to be copied into the build output folder. The app no longer depends on the shared framework package being installed from the Microsoft Store.

However, the Windows App SDK runtime is composed of **four** MSIX packages:

| Package | Self-contained bundles it? |
|---------|---------------------------|
| Framework package (WinUI 3, DWriteCore, etc.) | YES — DLLs copied to output folder |
| Main package (background tasks, auto-update) | NO — must be registered on machine |
| Singleton package (push notifications, some brokered services) | NO — must be registered on machine |
| DDLM package (prevents OS from servicing framework while app runs) | NO — must be registered on machine |

The Main, Singleton, and DDLM packages require MSIX registration. On a school machine with Group Policy set to block MSIX sideloading (`AllowDevelopmentWithoutDevLicense = 0`, `AllowAllTrustedApps = 0`), the Windows App SDK installer returns `0x80070005` (access denied) when it tries to call `PackageManager.AddPackageAsync`. The app then fails to start.

From official docs: *"If the computer is managed in an enterprise environment, there might be a policy preventing these settings from being changed. In that case if you get an error when you or your app tries to install the Windows App SDK runtime, contact your IT Professional to enable sideloading or Developer mode."*

### What WPF self-contained actually means

`dotnet publish -r win-x64 --self-contained true -p:PublishSingleFile=true`

Output: one `.exe` file (~60-100 MB). Contains the entire .NET runtime, all app DLLs, SkiaSharp native binaries, and PDFium. No MSIX. No registry writes. No admin rights. Runs from a USB stick, a shared network folder, or the student's Desktop. Zero OS-level registration.

The only caveat is the Visual C++ Redistributable, which is pre-installed on every school Windows 10/11 image that has run any Windows Update. This is not a practical blocker.

### When WinUI 3 makes sense

WinUI 3 is the right long-term target for this app: Fluent design, better accessibility APIs (UI Automation updates), and Win2D for hardware-accelerated canvas. It becomes viable for this deployment scenario when:

1. The school machines have the Windows App SDK runtime pre-provisioned by IT (one-time admin action), OR
2. Microsoft ships true zero-MSIX self-contained deployment (as of Windows App SDK 1.8, still not fully resolved for the Main/Singleton packages), OR
3. The app is distributed via the Microsoft Store (handles runtime deployment automatically).

**Phase 0 validation task:** Test WinUI 3 + Win2D on the actual target school machine before any WinUI 3 code is written. If it runs, the stack can be revisited. If it fails, WPF is confirmed.

---

## What NOT to Use

| Technology | Verdict | Reason |
|------------|---------|--------|
| WinUI 3 + Win2D (primary candidate) | Deferred to post-validation | MSIX deployment blocker on managed school machines. Win2D is the right canvas library but is WinUI 3-only. |
| Electron / web stack | Hard no | 200+ MB runtime, input latency, no reliable offline file access, exam environment hostile. |
| WinForms | No | GDI+ rendering only; cannot draw antialiased geometry or hardware-accelerated canvas. Would require D3DImage hacks. |
| MAUI | No | Cross-platform abstraction adds complexity with no benefit (Windows-only). Custom drawing is harder than WPF + SkiaSharp. |
| C++ / DirectX | No | Maximum performance ceiling not needed. Accessibility APIs (UIA) are harder to expose from C++. Development speed too slow. |
| WPF + WriteableBitmap (no SkiaSharp) | No | WriteableBitmap is CPU-rendered. No hardware acceleration. Protractor rotation at 60 FPS will stutter. |
| PdfiumViewer (old, archived) | Use with caution | The original PdfiumViewer NuGet by pvginkel is archived. Use `PDFiumSharp` or `Docnet.Core` as maintained alternatives. Verify at dependency time. |

---

## Installation

```bash
# Core app project
dotnet add package SkiaSharp
dotnet add package SkiaSharp.Views.WPF
dotnet add package PDFiumSharp           # or Docnet.Core — verify current maintained status
dotnet add package CommunityToolkit.Mvvm
dotnet add package Microsoft.Extensions.DependencyInjection

# Dev/test only
dotnet add package Microsoft.Extensions.Logging.Debug
```

### Publish command (self-contained single EXE)

```bash
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishReadyToRun=true \
  -o ./dist
```

Output: `MathGaze.exe` (~80-100 MB), no installer, no admin rights, runs from any folder.

### Project file minimum config

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net9.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

---

## Alternatives Considered

| Category | Recommended | Alternative | Why Not |
|----------|-------------|-------------|---------|
| UI framework | WPF | WinUI 3 | MSIX deployment blocker on school machines |
| 2D canvas | SkiaSharp | Win2D | Win2D is WinUI 3-only; not usable from WPF |
| 2D canvas | SkiaSharp | Direct2D via D3DImage | Requires C++ interop, complex build, no DX advantage for this use case |
| PDF rendering | PDFiumSharp | Windows.Data.Pdf | WinRT API requires extra interop boilerplate from WPF; DPI scaling quality issues |
| PDF rendering | PDFiumSharp | MuPDF | GPL licence incompatible with potential commercial/school licensing |
| MVVM | CommunityToolkit.Mvvm | Prism | Prism is heavier and more opinionated; overkill for a single-window app |
| Runtime | .NET 9 self-contained | .NET 8 LTS | .NET 9 is current; .NET 8 LTS is acceptable if .NET 9 causes issues, but no known reason to downgrade |

---

## Confidence Assessment

| Area | Confidence | Evidence |
|------|------------|---------|
| WinUI 3 deployment blocker | HIGH | Official Microsoft docs: `0x80070005` error code documented for managed enterprise environments; self-contained deployment docs confirm Main/Singleton packages are NOT bundled |
| WPF self-contained xcopy works | HIGH | .NET publish self-contained is a first-class dotnet CLI feature since .NET Core 3.0; no MSIX involved |
| SkiaSharp GPU acceleration in WPF | HIGH | SkiaSharp.Views.WPF ships `SKXamlCanvas` which uses OpenGL backend by default; documented and widely used in production |
| PDFium for PDF rendering quality | HIGH | PDFium is the engine in Chrome; handles all PDF features including those common in GCSE exam papers |
| CommunityToolkit.Mvvm for WPF | HIGH | Explicitly documented as platform-agnostic; targets .NET Standard 2.0 |
| Win2D NuGet package name | MEDIUM | Package is `Microsoft.Graphics.Win2D` for WinUI 3; docs page confirmed Win2D integrates with WinUI/Windows App SDK XAML, not WPF XAML |
| .NET 9 support lifecycle | HIGH | .NET 9 is STS, supported until May 2026. .NET 10 (LTS) releases November 2025. Migration to .NET 10 LTS recommended at first opportunity. |

---

## Sources

- Windows App SDK deployment architecture: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/deployment-architecture
- Windows App SDK unpackaged deployment guide: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/deploy-unpackaged-apps
- Windows App SDK self-contained deployment: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/self-contained-deploy/deploy-self-contained-apps
- Windows App SDK deployment overview: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/deploy-overview
- Windows App SDK release channels (1.8.7 confirmed latest stable, April 2026): https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-channels
- Win2D overview: https://learn.microsoft.com/en-us/windows/apps/develop/win2d/
- Windows.Data.Pdf.PdfPage.RenderToStreamAsync: https://learn.microsoft.com/en-us/uwp/api/windows.data.pdf.pdfpage.rendertostreamasync
- CommunityToolkit.Mvvm: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/
- .NET 9 overview: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview
