<!-- GSD:project-start source:PROJECT.md -->
## Project

**MathGaze**

A native Windows desktop app that lets eye-gaze students work through GCSE maths exam papers using only gaze-driven clicks — no drag gestures, no mouse, no keyboard. Students load a PDF, annotate it with geometry tools (lines, circles, protractor), select multiple-choice answers, and place text labels — all through Grid 3 / Smartbox driving standard Windows click events. The app starts as a tool built for one specific student and is designed to scale into a general assistive technology product.

**Core Value:** A student can complete a GCSE geometry question — measuring angles, drawing lines of reflection, selecting answers — using only their eyes, without the app ever reducing the cognitive challenge of the maths itself.

### Constraints

- **Interaction**: No drag gestures. Every action is click-to-commit. Maximum 2 clicks per primitive. — Gaze accuracy requirement
- **Target size**: Every interactive element ≥56×56px at 1× density. — Gaze accuracy floor from HANDOFF
- **Deployment**: Self-contained EXE, no admin install, no runtime dependency on pre-installed components. — Runs on school machines
- **Platform**: Windows 10/11 only. No cross-platform scope. — Native stack commitment
- **Input abstraction**: All input treated as standard Windows pointer events. — Grid 3 compatibility
- **Tech stack**: .NET + WinUI 3 + Win2D + Windows PDF API (primary); WPF + SkiaSharp (fallback if WinUI 3 fails Phase 0 validation on exam machine). — Win2D gives hardware-accelerated 2D needed for protractor rendering
- **Rendering**: PDF as bitmap background layer; geometry as vector layer on top; UI overlay above both. — Layer separation is critical for performance and coordinate system correctness
- **Exam integrity**: App assists the student's process, never the answer. No auto-solving, no computed answers displayed in Exam Mode.
<!-- GSD:project-end -->

<!-- GSD:stack-start source:research/STACK.md -->
## Technology Stack

## Recommendation: WPF + SkiaSharp (not WinUI 3)
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
### PDF Rendering
| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| PdfiumViewer | 2.2.0 (last stable) or PDFium.NET | Render PDF pages to bitmaps | `Windows.Data.Pdf` (WinRT) is usable from unpackaged .NET apps but requires `WindowsAppSDK` or specific WinRT interop boilerplate to call from WPF. PdfiumViewer wraps the Chromium PDFium library which is battle-tested, handles all GCSE exam PDF edge cases (mixed colour/mono, embedded fonts, form fields), and renders to `System.Drawing.Bitmap` trivially. |
| PDFium native binary | Bundled via NuGet | PDF engine | Ships as a native DLL inside the NuGet package. x64 EXE only; one DLL, no registration. |
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
## The Deployment Question: WinUI 3 Self-Contained on School Machines
### What "self-contained" actually means for WinUI 3
| Package | Self-contained bundles it? |
|---------|---------------------------|
| Framework package (WinUI 3, DWriteCore, etc.) | YES — DLLs copied to output folder |
| Main package (background tasks, auto-update) | NO — must be registered on machine |
| Singleton package (push notifications, some brokered services) | NO — must be registered on machine |
| DDLM package (prevents OS from servicing framework while app runs) | NO — must be registered on machine |
### What WPF self-contained actually means
### When WinUI 3 makes sense
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
## Installation
# Core app project
# Dev/test only
### Publish command (self-contained single EXE)
### Project file minimum config
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
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->
## Conventions

Conventions not yet established. Will populate as patterns emerge during development.
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->
## Architecture

Architecture not yet mapped. Follow existing patterns found in the codebase.
<!-- GSD:architecture-end -->

<!-- GSD:skills-start source:skills/ -->
## Project Skills

No project skills found. Add skills to any of: `.claude/skills/`, `.agents/skills/`, `.cursor/skills/`, or `.github/skills/` with a `SKILL.md` index file.
<!-- GSD:skills-end -->

<!-- GSD:workflow-start source:GSD defaults -->
## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd-quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd-debug` for investigation and bug fixing
- `/gsd-execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->



<!-- GSD:profile-start -->
## Developer Profile

> Profile not yet configured. Run `/gsd-profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
