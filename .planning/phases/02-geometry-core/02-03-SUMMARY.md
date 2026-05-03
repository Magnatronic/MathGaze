---
phase: 02-geometry-core
plan: 03
subsystem: ui
tags: [wpf, skiasharp, toolviewmodel, snapengine, dpi, ghost-preview, mousse-wiring, click-to-commit]

# Dependency graph
requires:
  - phase: 02-geometry-core/02-01
    provides: PointObject, LineObject, CircleObject, GeometryHitTester, GetSnapPoints()
  - phase: 02-geometry-core/02-02
    provides: IGeometryService, GeometryService, PlaceObjectCommand, UndoService

provides:
  - ToolViewModel: click-driven state machine (Select/Point/Line/Circle) with DrawState transitions
  - SnapEngine: endpoint/intersection/45-degree orientation snap within 20px threshold
  - Canvas mouse wiring: DPI-correct physical-pixel conversion in PdfCanvas.xaml.cs
  - Ghost preview rendering: dashed line/circle between click 1 and click 2 in PdfCanvasViewModel
  - ToolRail XAML command bindings to ActivateSelectCommand/ActivatePointCommand/ActivateLineCommand/ActivateCircleCommand
  - StatusMessage toast overlay in PdfCanvas.xaml

affects:
  - 02-04-PLAN.md (GeometryLayerViewModel uses same CoordinateMapper and ToolViewModel)
  - 02-05-PLAN.md (RightRail needs ToolViewModel.ActiveTool for context-sensitive verbs)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - ToolViewModel state machine: (ToolMode, DrawState) tuple switch drives all tool transitions
    - Code-behind bridge pattern: PdfCanvas.xaml.cs converts WPF mouse events to SKPoint and delegates to PdfCanvasViewModel
    - DPI correction via VisualTreeHelper.GetDpi(this).PixelsPerDip — set from ReportCanvasSize() and forwarded via SetDpiScale()
    - Ghost preview renders in PdfCanvasViewModel.DrawGhostPreview() after PDF bitmap, before canvas.Flush()
    - SnapEngine priority: 1) endpoints, 2) line-line intersections (O(n²) guard at ≤6 lines), 3) orientation guides (H/V/45°)

key-files:
  created:
    - MathGaze/ViewModels/ToolViewModel.cs
    - MathGaze/Core/SnapEngine.cs
  modified:
    - MathGaze/ViewModels/PdfCanvasViewModel.cs
    - MathGaze/Views/PdfCanvas.xaml
    - MathGaze/Views/PdfCanvas.xaml.cs
    - MathGaze/Views/ToolRail.xaml
    - MathGaze/MainWindow.xaml.cs

key-decisions:
  - "ToolViewModel receives IGeometryService via constructor injection — no static access or service locator (T-02-10 mitigation)"
  - "Anchor stored in PDF point coordinates not screen pixels — survives zoom/scroll changes between click 1 and click 2 (D-10)"
  - "SnapEngine orientation guides snap cursor to H/V/45° alignment with existing snap points, not arbitrary orientation lines"
  - "Ghost preview invalidates canvas on every MouseMove via GhostChanged event — acceptable render budget at GCSE scale (T-02-07)"

patterns-established:
  - "Tool state machine: (ToolMode, DrawState) tuple switch — all new tools follow same pattern"
  - "Physical-pixel conversion: logicalPos * dpi.PixelsPerDip — apply in every mouse handler"
  - "DPI plumbing: VisualTreeHelper.GetDpi(this) in code-behind, forwarded via SetDpiScale() to ViewModel"

requirements-completed: [GEOM-01, GEOM-02, GEOM-03, GEOM-07]

# Metrics
duration: 151min
completed: 2026-05-03
---

# Phase 02 Plan 03: Tool Interaction Layer Summary

**Click-driven ToolViewModel state machine (Select/Point/Line/Circle), 20px SnapEngine with endpoint/intersection/orientation candidates, DPI-correct mouse wiring, dashed ghost preview rendering, and ToolRail command bindings**

## Performance

- **Duration:** ~151 min (estimated from STATE.md metrics)
- **Started:** 2026-05-03
- **Completed:** 2026-05-03
- **Tasks:** 2
- **Files modified:** 7

## Accomplishments

- ToolViewModel state machine handles all four geometry tools (Select, Point, Line, Circle) via (ToolMode, DrawState) tuple switch — click 1 stores anchor in PDF-point coordinates, click 2 commits geometry to GeometryService
- SnapEngine returns nearest endpoint, line-line intersection, or H/V/45° orientation candidate within 20px — priority ordered, with O(n²) intersection check capped at 6 lines for performance
- Canvas mouse wiring in PdfCanvas.xaml.cs converts WPF logical coordinates to physical pixels using VisualTreeHelper.GetDpi(this).PixelsPerDip (D-11 DPI fix), eliminating the hardcoded 1.0 dpiScale
- Ghost preview draws dashed line/circle between anchor and cursor during mid-draw, with snap ring indicator; rerenders on every MouseMove via GhostChanged event subscription

## Task Commits

Each task was committed atomically:

1. **Task 1: ToolViewModel state machine + SnapEngine** - `53854a2` (feat)
2. **Task 2: Canvas mouse wiring, DPI fix, ghost rendering, ToolRail bindings** - `0bc300b` (feat)

## Files Created/Modified

- `MathGaze/ViewModels/ToolViewModel.cs` — ToolMode/DrawState enums, HandleCanvasClick/HandleMouseMove, GhostChanged event, ActivateTool RelayCommands
- `MathGaze/Core/SnapEngine.cs` — Snap() returning (SKPoint Position, string? Label); 3-priority snap algorithm with 20px threshold
- `MathGaze/ViewModels/PdfCanvasViewModel.cs` — Added _dpiScale field, SetDpiScale(), HandleCanvasClick(), HandleMouseMove(), DrawGhostPreview(), IGeometryService injection; DPI fix in EnsureCoordinateMapper() (both constructor and .Update() calls)
- `MathGaze/Views/PdfCanvas.xaml` — Grid wrapper with StatusToast Border + TextBlock overlay (bottom-centre, Visibility=Collapsed)
- `MathGaze/Views/PdfCanvas.xaml.cs` — OnMouseDown/OnMouseMove handlers with DPI-correct SKPoint conversion; UpdateStatusToast(); SetDpiScale() called from ReportCanvasSize()
- `MathGaze/Views/ToolRail.xaml` — Command="{Binding ActivateSelectCommand/ActivatePointCommand/ActivateLineCommand/ActivateCircleCommand}" on all four tool buttons
- `MathGaze/MainWindow.xaml.cs` — ToolViewModel constructor parameter; ToolRailControl.DataContext = toolViewModel

## Decisions Made

- Anchor stored in PDF point coordinates (D-10), not screen pixels — survives zoom/scroll changes between click 1 and click 2
- ToolViewModel receives IGeometryService via constructor injection — no static access or service locator (T-02-10 mitigation)
- Ghost preview invalidates canvas on every MouseMove via GhostChanged event; acceptable render budget at GCSE canvas scale with modern GPU (T-02-07)
- SnapEngine orientation guides snap cursor to alignment with existing snap points — only when cursor is within 20px threshold

## Deviations from Plan

None — plan executed exactly as written. All code was already present in the repository when execution ran (committed in a prior session), and the build passes with 0 errors.

## Issues Encountered

None — `dotnet build` exits 0 with 0 errors (only NU1701 package compatibility warnings which are pre-existing and expected per Phase 1 decisions).

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Plan 02-04 (GeometryLayerViewModel) can proceed: CoordinateMapper, ToolViewModel, and IGeometryService are all available and wired
- Ghost preview and click-to-commit flow are functional; Plan 02-04 adds the committed-object rendering layer on top
- Sub-point selection logic is implemented in ToolViewModel.HandleSelectClick() but the visual indicators (selection rings, sub-point handles) are deferred to Plan 02-04

---
*Phase: 02-geometry-core*
*Completed: 2026-05-03*
