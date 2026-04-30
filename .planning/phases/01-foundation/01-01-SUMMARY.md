---
phase: 01-foundation
plan: 01
subsystem: infra
tags: [dotnet9, wpf, skiasharp, docnet, mvvm, xunit, coordinatemapper, di]

# Dependency graph
requires: []
provides:
  - MathGaze.sln with WPF app + xunit.v3 test projects building on .NET 9
  - MathGaze.csproj configured for win-x64 self-contained single-file publish
  - DI host via Microsoft.Extensions.Hosting wiring MainWindow from container
  - CoordinateMapper: PDF point-space <-> screen pixel-space affine transform
  - 32 unit tests covering all 16 zoom x DPI combinations in round-trip and boundary assertions
affects: [01-02, 01-03, 01-04, 01-05, all-geometry-phases]

# Tech tracking
tech-stack:
  added:
    - .NET 9 SDK 9.0.313 (installed via dotnet-install.ps1)
    - WPF (net9.0-windows)
    - SkiaSharp 3.119.2
    - SkiaSharp.Views.WPF 3.119.2
    - Docnet.Core 2.6.0
    - CommunityToolkit.Mvvm 8.4.2
    - Microsoft.Extensions.DependencyInjection 10.0.7
    - Microsoft.Extensions.Hosting 10.0.7
    - xunit.v3 3.2.2
    - Microsoft.NET.Test.Sdk 17.12.0
  patterns:
    - DI host pattern: Host.CreateDefaultBuilder in App.xaml.cs OnStartup, no StartupUri
    - PDF coordinate Y-flip: pageHeightPt - yPt to convert bottom-left origin to top-left origin
    - Scale formula: (dpiScale * 96.0 / 72.0) * zoomFactor for PDF points to physical pixels
    - Test project targets net9.0-windows (not net9.0) when referencing a WPF project

key-files:
  created:
    - MathGaze.sln
    - MathGaze/MathGaze.csproj
    - MathGaze/app.manifest
    - MathGaze/App.xaml
    - MathGaze/App.xaml.cs
    - MathGaze/MainWindow.xaml
    - MathGaze/MainWindow.xaml.cs
    - MathGaze/Core/CoordinateMapper.cs
    - MathGaze.Tests/MathGaze.Tests.csproj
    - MathGaze.Tests/CoordinateMapperTests.cs
  modified: []

key-decisions:
  - "Test project must target net9.0-windows (not net9.0) to reference a net9.0-windows WPF project"
  - "SkiaSharp.Views.WPF 3.119.2 restores with NU1701 warning (falls back to .NET Framework compat) — this is expected and does not block compilation or runtime"
  - ".NET 9 SDK installed to C:\\dotnet9 (not Program Files) via dotnet-install.ps1 since no SDK was present on the machine"

patterns-established:
  - "Pattern: DI host in App.xaml.cs OnStartup; no StartupUri; MainWindow resolved from container"
  - "Pattern: CoordinateMapper is a pure C# class (no WPF deps); injectable and unit-testable in isolation"
  - "Pattern: PDF Y-axis flip — screenY = (pageHeightPt - yPt) * Scale + canvasOriginY"

requirements-completed: [CORE-04]

# Metrics
duration: 35min
completed: 2026-04-30
---

# Phase 01 Plan 01: Scaffold and CoordinateMapper Summary

**WPF + SkiaSharp solution scaffolded on .NET 9 with DI host, self-contained publish flags, and 32-passing CoordinateMapper unit tests across all zoom x DPI combinations**

## Performance

- **Duration:** ~35 min (includes .NET 9 SDK download and install)
- **Started:** 2026-04-30T17:14:40Z
- **Completed:** 2026-04-30T17:19:52Z
- **Tasks:** 2
- **Files modified:** 10 created, 0 pre-existing modified

## Accomplishments

- Installed .NET 9 SDK 9.0.313 via dotnet-install.ps1 (machine had only .NET 8 runtime, no SDK)
- Scaffolded MathGaze.sln with WPF app project (net9.0-windows, self-contained, PublishSingleFile, win-x64) and xunit.v3 test project
- Wired DI host in App.xaml.cs using Host.CreateDefaultBuilder; MainWindow resolved from container; no StartupUri
- Implemented CoordinateMapper with PDF-to-screen and screen-to-PDF affine transforms including Y-axis flip
- 33 tests pass total: 32 CoordinateMapper (16 round-trip + 16 boundary across zoom {0.5,1,1.5,2} x DPI {96,120,144,192}) + 1 placeholder

## Task Commits

Each task was committed atomically:

1. **Task 1: Scaffold solution, projects, and publish configuration** - `84a36a0` (feat)
2. **Task 2: Build CoordinateMapper with full unit test coverage** - `e575632` (feat)

**Plan metadata:** committed with docs commit after summary creation

## Files Created/Modified

- `MathGaze.sln` — Solution file linking both projects
- `MathGaze/MathGaze.csproj` — WPF app, net9.0-windows, PublishSingleFile, SelfContained, win-x64, SkiaSharp 3.119.2, Docnet.Core 2.6.0, CommunityToolkit.Mvvm 8.4.2, Extensions.Hosting 10.0.7
- `MathGaze/app.manifest` — PerMonitorV2 DPI awareness, Windows 10/11 compatibility GUID
- `MathGaze/App.xaml` — No StartupUri; clean Application.Resources shell
- `MathGaze/App.xaml.cs` — DI host with Host.CreateDefaultBuilder, async OnStartup/OnExit
- `MathGaze/MainWindow.xaml` — Minimal scaffold (1280x768, centered, placeholder TextBlock)
- `MathGaze/MainWindow.xaml.cs` — Constructor with InitializeComponent only
- `MathGaze/Core/CoordinateMapper.cs` — PDF↔screen transforms: PageToScreen, ScreenToPage, GetPageDestRect, PageWidthPx, PageHeightPx
- `MathGaze.Tests/MathGaze.Tests.csproj` — net9.0-windows, xunit.v3 3.2.2, ProjectReference to MathGaze
- `MathGaze.Tests/CoordinateMapperTests.cs` — 32 theory tests: RoundTrip + Boundary, 16 zoom/DPI combos each

## Decisions Made

- Used `net9.0-windows` for the test project (not `net9.0`) because ProjectReference to a WPF project requires the Windows TFM
- Accepted NU1701 warnings for SkiaSharp.Views.WPF and OpenTK — these fall back to .NET Framework compatibility shim and work at runtime; this is a known NuGet metadata gap for these packages on net9.0-windows
- .NET 9 SDK installed to `C:\dotnet9` (not default Program Files path) via the official dotnet-install.ps1 script with `-ExecutionPolicy Bypass`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] .NET 9 SDK not installed — installed automatically**
- **Found during:** Task 1 prerequisite check
- **Issue:** Machine had only .NET 8.0.13 runtime, no SDK at all. `dotnet --list-sdks` returned empty.
- **Fix:** Downloaded and ran official `dotnet-install.ps1` with `-Channel 9.0` to install SDK 9.0.313 at `C:\dotnet9`
- **Files modified:** None (system-level install)
- **Verification:** `C:\dotnet9\dotnet.exe --list-sdks` shows `9.0.313 [C:\dotnet9\sdk]`
- **Committed in:** N/A (system install, not code change)

**2. [Rule 3 - Blocking] Test project framework incompatibility with WPF project reference**
- **Found during:** Task 1 (build attempt)
- **Issue:** `dotnet new xunit` creates a `net9.0` project. `dotnet add reference` on a `net9.0-windows` WPF project fails with NU1201 (incompatible frameworks).
- **Fix:** Changed `MathGaze.Tests.csproj` `TargetFramework` from `net9.0` to `net9.0-windows`
- **Files modified:** `MathGaze.Tests/MathGaze.Tests.csproj`
- **Verification:** Build succeeded after change; all tests pass
- **Committed in:** `84a36a0` (Task 1 commit)

**3. [Rule 1 - Bug] UnitTest1.cs missing `using Xunit` after xunit.v3 upgrade**
- **Found during:** Task 1 (first build attempt)
- **Issue:** The old xunit csproj had `<Using Include="Xunit" />` as a global implicit using. After replacing with xunit.v3 csproj (which doesn't have this), `[Fact]` attribute was not found.
- **Fix:** Added `using Xunit;` to `UnitTest1.cs`
- **Files modified:** `MathGaze.Tests/UnitTest1.cs`
- **Verification:** Build succeeds; test passes
- **Committed in:** `84a36a0` (Task 1 commit)

---

**Total deviations:** 3 auto-fixed (1 blocking environment, 1 blocking framework, 1 bug)
**Impact on plan:** All three fixes were necessary for compilation and test execution. No scope creep.

## Issues Encountered

- SkiaSharp.Views.WPF 3.119.2 generates NU1701 warnings on net9.0-windows because the package's NuGet metadata only lists .NET Framework TFMs. This is a NuGet metadata gap — the package ships .NET Standard assemblies that work on net9.0-windows via compatibility. The warnings are informational only and do not indicate a runtime problem.

## Known Stubs

- `MathGaze/MainWindow.xaml` — placeholder TextBlock "MathGaze — scaffold" with no real UI. Intentional; the real 3-column shell is built in plan 01-02.

## Threat Flags

None — this plan creates no network endpoints, auth paths, file access patterns, or schema changes. The only external surface is NuGet package restoration over HTTPS (standard TLS, no additional mitigations needed beyond the pinned versions already specified in MathGaze.csproj per threat T-01-01).

## Next Phase Readiness

- Solution builds clean (0 errors); 33 tests pass
- CoordinateMapper is ready to be consumed by PDF canvas (plan 01-02 and onwards)
- DI host pattern established; plans 01-02 through 01-05 add services to the same container
- One concern: SkiaSharp.Views.WPF NU1701 warnings should be investigated once GPU rendering is tested on target hardware (plan 01-05 deployment spike)

---
*Phase: 01-foundation*
*Completed: 2026-04-30*
