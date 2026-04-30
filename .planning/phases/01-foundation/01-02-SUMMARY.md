---
phase: 01-foundation
plan: 02
subsystem: ui-shell
tags: [wpf, xaml, skiasharp, mvvm, design-tokens, usercontrols, gaze-ux]

# Dependency graph
requires:
  - 01-01 (WPF project, SkiaSharp package, DI host, CommunityToolkit.Mvvm)
provides:
  - AppStyles.xaml: full design token brush set + ToolTileStyle (84x56) + IconButtonStyle
  - MainViewModel: ObservableProperty state skeleton (FileName, IsPdfOpen, IsPracticeMode, ZoomFactor, ZoomLabel, CurrentPage, TotalPages, PageLabel) + stub RelayCommands for all TopBar actions
  - TopBar UserControl: branding pill, file chip, mode pill bound to IsPracticeMode, zoom strip, page nav strip
  - ToolRail UserControl: 6 ToolTile stub buttons (Select, Point, Line, Circle, Protractor, Text) each 84x56 logical px
  - PdfCanvas UserControl: SKElement placeholder with BrushBg background (full impl in Plan 03)
  - ScrollRail UserControl: PageUp/Up/Down/PageDown buttons at 56px height gaze floor
  - RightRailPlaceholder UserControl: dashed border Nothing Selected state
  - MainWindow: 3-column Grid (104/*/148) with 60px TopBar row; DataContext = MainViewModel from DI
affects: [01-03, 01-04, 01-05]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Design tokens mapped from shared.jsx buildTokens({ theme=light, accent=cobalt, density=comfortable }) to WPF SolidColorBrush ResourceDictionary
    - ToolTileStyle: Button with CornerRadius=10, Width=84, Height=56, ContentPresenter HorizontalAlignment=Left — gaze target floor met
    - SKElement (not SKXamlCanvas): SkiaSharp.Views.WPF 3.119.2 on net9.0-windows resolves via .NETFramework4.6.2 compat shim; the control is SKElement, not SKXamlCanvas (WinUI 3 name)
    - PaintSurface wired in code-behind (not XAML): avoids XAML temp-project compat shim issue where SKPaintSurfaceEventArgs from SkiaSharp.Views.Desktop.Common is not resolved
    - using SkiaSharp.Views.Desktop required in PdfCanvas.xaml.cs to resolve SKPaintSurfaceEventArgs
    - DashArray on Rectangle (not Border): WPF Border does not support dashed borders natively; Rectangle with StrokeDashArray="4,4" layered over content achieves the dashed effect

key-files:
  created:
    - MathGaze/Styles/AppStyles.xaml
    - MathGaze/ViewModels/MainViewModel.cs
    - MathGaze/Views/TopBar.xaml
    - MathGaze/Views/TopBar.xaml.cs
    - MathGaze/Views/ToolRail.xaml
    - MathGaze/Views/ToolRail.xaml.cs
    - MathGaze/Views/PdfCanvas.xaml
    - MathGaze/Views/PdfCanvas.xaml.cs
    - MathGaze/Views/ScrollRail.xaml
    - MathGaze/Views/ScrollRail.xaml.cs
    - MathGaze/Views/RightRailPlaceholder.xaml
    - MathGaze/Views/RightRailPlaceholder.xaml.cs
  modified:
    - MathGaze/App.xaml (merged AppStyles.xaml ResourceDictionary)
    - MathGaze/App.xaml.cs (registered MainViewModel in DI)
    - MathGaze/MainWindow.xaml (replaced scaffold TextBlock with 3-column Grid)
    - MathGaze/MainWindow.xaml.cs (DI constructor injection of MainViewModel)

key-decisions:
  - "SKElement is the correct WPF control name in SkiaSharp.Views.WPF 3.119.2 — SKXamlCanvas is WinUI 3 only"
  - "PaintSurface event wired in code-behind (not XAML attribute) to avoid XAML temp-project compat shim resolving SKPaintSurfaceEventArgs"
  - "using SkiaSharp.Views.Desktop resolves SKPaintSurfaceEventArgs from SkiaSharp.Views.Desktop.Common transitive package"
  - "WPF Border does not support StrokeDashArray — used Rectangle with StrokeDashArray='4,4' layered over content in RightRailPlaceholder"
  - "All stub RelayCommands added to MainViewModel upfront to prevent XAML binding warnings at runtime when TopBar/ScrollRail bind to them"

requirements-completed: [CORE-03]

# Metrics
duration: ~20min
completed: 2026-04-30
---

# Phase 01 Plan 02: 3-Column WPF Shell Summary

**3-column WPF shell built with design token ResourceDictionary, MainViewModel observable state skeleton, and 6 UserControls (TopBar, ToolRail, PdfCanvas, ScrollRail, RightRailPlaceholder) — build exits 0, 33 tests pass, human visual checkpoint approved**

## Performance

- **Duration:** ~20 min
- **Completed:** 2026-04-30T17:40:00Z
- **Tasks:** 3 of 3 (complete — human visual checkpoint approved)
- **Files:** 12 created, 4 modified

## Accomplishments

- Created AppStyles.xaml with full design token brush set (15 brushes: BrushBg, BrushSurface, BrushSurface2, BrushBorder, BrushInk, BrushInk2, BrushInk3, BrushAccent, BrushAccentSoft, BrushAccentInk, BrushExam, BrushPractice, BrushTransparent) plus FontBody/FontMono, ToolTileStyle (84×56 gaze target), IconButtonStyle
- Created MainViewModel with all observable properties and 12 stub RelayCommands pre-registered so XAML bindings in TopBar/ScrollRail don't produce runtime binding warnings
- Built TopBar UserControl with all 7 visual sections: branding pill, file chip, mode pill (DataTrigger on IsPracticeMode), zoom strip, page nav strip, settings button — all commands bound to MainViewModel
- Built ToolRail with 6 ToolTile stub buttons: Select, Point, Line, Circle, Protractor, Text — each 84×56 logical px meeting gaze floor
- Built PdfCanvas with SKElement placeholder painting BrushBg background (#F5F3EE)
- Built ScrollRail with 4 navigation buttons (PageUp/Up/Down/PageDown) each 56px height; visual scroll track indicator
- Built RightRailPlaceholder with dashed Rectangle overlay and "NOTHING SELECTED" text
- Replaced MainWindow scaffold with 3-column Grid (104/*/148 columns, 60px TopBar row); MainViewModel injected via DI constructor

## Task Commits

1. **Task 1: Design tokens, AppStyles, and MainViewModel** — `fa67c92`
2. **Task 2: Build 3-column shell** — `ffe8794`
3. **Task 3: Human visual checkpoint** — APPROVED by user (no code changes required)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] SKXamlCanvas does not exist in SkiaSharp.Views.WPF 3.119.2 compat shim**
- **Found during:** Task 2 (first build attempt)
- **Issue:** The plan specifies `<skia:SKXamlCanvas>` in PdfCanvas.xaml. This type name is from WinUI 3 / Windows App SDK. In the WPF package (`SkiaSharp.Views.WPF`), the equivalent control is `SKElement`. Since the package restores via .NETFramework4.6.2 compat shim on net9.0-windows, only the net4.6.2 DLL is used — which exposes `SKElement` (not `SKXamlCanvas`).
- **Fix:** Replaced `skia:SKXamlCanvas` with `skia:SKElement` in PdfCanvas.xaml
- **Files modified:** `MathGaze/Views/PdfCanvas.xaml`
- **Commit:** `ffe8794`

**2. [Rule 1 - Bug] SKPaintSurfaceEventArgs not resolvable in XAML temp project**
- **Found during:** Task 2 (second build attempt after SKElement fix)
- **Issue:** The XAML compiler generates a temp project for XAML parsing. When `PaintSurface="OnPaintSurface"` is declared in XAML, the temp project must resolve `SKPaintSurfaceEventArgs` (the event args type). This type is in `SkiaSharp.Views.Desktop.Common`, which is a transitive dependency — the temp project doesn't get the full transitive chain from the compat shim.
- **Fix 1:** Removed `PaintSurface="OnPaintSurface"` from XAML; wired `SkCanvas.PaintSurface += OnPaintSurface` in code-behind constructor instead.
- **Fix 2:** Added `using SkiaSharp.Views.Desktop;` to `PdfCanvas.xaml.cs` so the C# compiler finds `SKPaintSurfaceEventArgs` from the transitive package.
- **Files modified:** `MathGaze/Views/PdfCanvas.xaml`, `MathGaze/Views/PdfCanvas.xaml.cs`
- **Commit:** `ffe8794`

**3. [Rule 2 - Missing] Stub RelayCommands not in plan but required for zero binding warnings**
- **Found during:** Task 1 design review
- **Issue:** TopBar XAML binds to OpenFileCommand, CloseFileCommand, ZoomInCommand, ZoomOutCommand, FitPageCommand, PreviousPageCommand, NextPageCommand. ScrollRail binds to ScrollUpCommand, ScrollDownCommand, ScrollPageUpCommand, ScrollPageDownCommand. Plan only specified ToggleMode. Missing commands cause runtime binding warnings and silently broken buttons.
- **Fix:** Added all 12 stub [RelayCommand] methods to MainViewModel with empty bodies (commented "wired in Plan 04").
- **Files modified:** `MathGaze/ViewModels/MainViewModel.cs`
- **Commit:** `fa67c92`

---

**Total deviations:** 3 auto-fixed (2 bugs, 1 missing critical functionality)

## Known Stubs

- `MathGaze/Views/PdfCanvas.xaml.cs` — `OnPaintSurface` fills canvas with flat BrushBg colour; no PDF rendering. Intentional; full SKElement rendering wired in Plan 03.
- `MathGaze/Views/ToolRail.xaml` — All 6 ToolTile buttons have no Command binding. Intentional; commands wired in Plan 04.
- `MathGaze/ViewModels/MainViewModel.cs` — All RelayCommand methods have empty bodies (stubs). Intentional; real implementations in Plan 04.
- `MathGaze/MainWindow.xaml` — FileName shows empty string (displays "No file open" via TargetNullValue fallback). Intentional; real PDF loading in Plan 03/04.

## Threat Flags

None — this plan creates no network endpoints, auth paths, file access patterns, or schema changes. All surfaces are local UI only.

## Self-Check: PASSED

- `MathGaze/Styles/AppStyles.xaml` exists and contains `ToolTileStyle` and `BrushAccent` ✓
- `MathGaze/ViewModels/MainViewModel.cs` exists and contains `ObservableProperty`, `IsPracticeMode`, `ZoomFactor`, `PageLabel` ✓
- `MathGaze/Views/TopBar.xaml` exists and contains `IsPracticeMode` ✓
- `MathGaze/Views/ToolRail.xaml` exists and contains `ToolTileStyle` (6 times) ✓
- `MathGaze/Views/PdfCanvas.xaml` exists and contains `SKElement` ✓
- `MathGaze/Views/ScrollRail.xaml` exists and contains `ScrollPageUpCommand` ✓
- `MathGaze/Views/RightRailPlaceholder.xaml` exists and contains `NOTHING SELECTED` ✓
- `MathGaze/MainWindow.xaml` contains `Width="104"` and `Width="148"` ✓
- `MathGaze/MainWindow.xaml.cs` contains `MainViewModel viewModel` ✓
- `MathGaze/App.xaml` contains `AppStyles.xaml` ✓
- Commits `fa67c92` and `ffe8794` exist ✓
- Build exits 0, 33 tests pass ✓
- Task 3 (human-verify checkpoint) approved by user — layout confirmed correct

---
*Phase: 01-foundation*
*Completed: 2026-04-30*
