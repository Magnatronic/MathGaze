using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathGaze.Core;
using MathGaze.Core.Commands;
using MathGaze.Core.Geometry;
using MathGaze.Services;
using SkiaSharp;

namespace MathGaze.ViewModels;

public enum ToolMode { Select, Point, Line, Circle }
public enum DrawState { Idle, AnchorPlaced }

/// <summary>
/// Manages the active drawing tool and in-progress geometry state.
/// Receives canvas clicks and moves from PdfCanvas.xaml.cs (code-behind pattern, per Phase 1).
/// All geometry mutations go through IGeometryService.ExecuteCommand() (D-09).
/// </summary>
public partial class ToolViewModel : ObservableObject
{
    private readonly IGeometryService _geometryService;

    [ObservableProperty] private ToolMode  _activeTool    = ToolMode.Select;
    [ObservableProperty] private DrawState _drawState     = DrawState.Idle;
    [ObservableProperty] private string    _statusMessage = string.Empty;

    // In-progress anchor stored in PDF point coordinates (D-10)
    public (double xPt, double yPt)? AnchorPt { get; private set; }

    // Ghost cursor position in physical screen pixels — updated on every MouseMove
    public SKPoint GhostCursorPx { get; private set; }

    // Last snap result — used by ghost renderer to draw snap indicator
    public (SKPoint Position, string? Label)? LastSnap { get; private set; }

    /// <summary>Raised when ghost state changes — PdfCanvasViewModel subscribes to invalidate canvas.</summary>
    public event EventHandler? GhostChanged;

    public ToolViewModel(IGeometryService geometryService)
    {
        _geometryService = geometryService;
    }

    // ── Tool activation commands (bound from ToolRail) ───────────────────────

    [RelayCommand] private void ActivateSelect() { ResetDrawState(); ActiveTool = ToolMode.Select; }
    [RelayCommand] private void ActivatePoint()  { ResetDrawState(); ActiveTool = ToolMode.Point;  StatusMessage = "Click to place a point"; }
    [RelayCommand] private void ActivateLine()   { ResetDrawState(); ActiveTool = ToolMode.Line;   StatusMessage = "Click to place start point"; }
    [RelayCommand] private void ActivateCircle() { ResetDrawState(); ActiveTool = ToolMode.Circle; StatusMessage = "Click to place centre"; }

    private void ResetDrawState()
    {
        AnchorPt      = null;
        DrawState     = DrawState.Idle;
        LastSnap      = null;
        StatusMessage = string.Empty;
        GhostChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── Canvas interaction (called from PdfCanvas.xaml.cs code-behind) ───────

    /// <summary>
    /// Process a canvas click. screenPx is in physical pixels (DPI-corrected).
    /// mapper must be current (obtained from PdfCanvasViewModel).
    /// snap is used to find the snapped position before converting to PDF points.
    /// </summary>
    public void HandleCanvasClick(SKPoint screenPx, CoordinateMapper mapper, SnapEngine snap)
    {
        switch (ActiveTool, DrawState)
        {
            case (ToolMode.Point, DrawState.Idle):
            {
                // First click — exact placement, no snap
                var (xPt, yPt) = mapper.ScreenToPage(screenPx);
                _geometryService.ExecuteCommand(new PlaceObjectCommand(new PointObject(xPt, yPt)));
                StatusMessage = "Point placed";
                break;
            }

            case (ToolMode.Select, DrawState.Idle):
                HandleSelectClick(screenPx, mapper);
                break;

            case (ToolMode.Line, DrawState.Idle):
            {
                // First click — exact anchor placement, no snap
                var (xPt, yPt) = mapper.ScreenToPage(screenPx);
                AnchorPt  = (xPt, yPt);
                DrawState = DrawState.AnchorPlaced;
                StatusMessage = "Click 2nd point";
                GhostChanged?.Invoke(this, EventArgs.Empty);
                break;
            }

            case (ToolMode.Line, DrawState.AnchorPlaced):
            {
                // Second click — snap to existing geometry
                var (snappedPx, _) = snap.Snap(screenPx, _geometryService.Objects, mapper);
                var (xPt, yPt) = mapper.ScreenToPage(snappedPx);
                var anchor = AnchorPt!.Value;
                _geometryService.ExecuteCommand(new PlaceObjectCommand(
                    new LineObject(anchor.xPt, anchor.yPt, xPt, yPt)));
                ResetDrawState();
                StatusMessage = "Line placed";
                break;
            }

            case (ToolMode.Circle, DrawState.Idle):
            {
                // First click — exact center placement, no snap
                var (xPt, yPt) = mapper.ScreenToPage(screenPx);
                AnchorPt  = (xPt, yPt);
                DrawState = DrawState.AnchorPlaced;
                StatusMessage = "Click radius point";
                GhostChanged?.Invoke(this, EventArgs.Empty);
                break;
            }

            case (ToolMode.Circle, DrawState.AnchorPlaced):
            {
                // Second click — snap to existing geometry
                var (snappedPx, _) = snap.Snap(screenPx, _geometryService.Objects, mapper);
                var (xPt, yPt) = mapper.ScreenToPage(snappedPx);
                var ctr = AnchorPt!.Value;
                double dx = xPt - ctr.xPt, dy = yPt - ctr.yPt;
                double radiusPt = Math.Sqrt(dx * dx + dy * dy);
                if (radiusPt < 1.0) radiusPt = 1.0;
                _geometryService.ExecuteCommand(new PlaceObjectCommand(
                    new CircleObject(ctr.xPt, ctr.yPt, radiusPt)));
                ResetDrawState();
                StatusMessage = "Circle placed";
                break;
            }
        }
    }

    /// <summary>
    /// Process a mouse move. Updates ghost cursor position and snap candidate.
    /// screenPx is in physical pixels (DPI-corrected).
    /// </summary>
    public void HandleMouseMove(SKPoint screenPx, CoordinateMapper mapper, SnapEngine snap)
    {
        GhostCursorPx = screenPx;

        // Only run snap during mid-draw (AnchorPlaced) — snap ring shows where click 2 will land.
        // During Idle, snap is disabled, so no ring needed.
        if (DrawState == DrawState.AnchorPlaced)
        {
            var result = snap.Snap(screenPx, _geometryService.Objects, mapper);
            LastSnap = result;
            StatusMessage = result.Label is not null
                ? $"Click 2nd point · snap: {result.Label}"
                : (ActiveTool == ToolMode.Circle ? "Click radius point" : "Click 2nd point");
        }
        else
        {
            LastSnap = null;
        }

        GhostChanged?.Invoke(this, EventArgs.Empty);
    }

    private void HandleSelectClick(SKPoint screenPx, CoordinateMapper mapper)
    {
        // First: check if current selection has a sub-point being targeted
        var selected = _geometryService.SelectedObject;
        if (selected is LineObject selLine)
        {
            var subHit = GeometryHitTester.TryHitLineSubPoint(screenPx, selLine, mapper);
            if (subHit is not null)
            {
                selLine.SelectedEndpoint = subHit.Value.endpointIndex;
                _geometryService.ObjectsChanged_ForceRaise();
                return;
            }
            // Click elsewhere on canvas while line selected → clear sub-selection, keep line selected (D-04)
            if (selLine.SelectedEndpoint.HasValue)
            {
                selLine.SelectedEndpoint = null;
                _geometryService.ObjectsChanged_ForceRaise();
            }
        }
        else if (selected is CircleObject selCircle)
        {
            var subHit = GeometryHitTester.TryHitCircleSubPoint(screenPx, selCircle, mapper);
            if (subHit is not null)
            {
                selCircle.SelectedSubPoint = subHit.Value.subPointIndex;
                _geometryService.ObjectsChanged_ForceRaise();
                return;
            }
            if (selCircle.SelectedSubPoint.HasValue)
            {
                selCircle.SelectedSubPoint = null;
                _geometryService.ObjectsChanged_ForceRaise();
            }
        }

        // Full hit test — select or deselect object
        var hit = GeometryHitTester.TryHitObject(screenPx, _geometryService.Objects, mapper);
        if (hit is not null)
            _geometryService.SetSelected(hit.Id);
        else
            _geometryService.ClearSelection();
    }
}
