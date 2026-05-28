using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathGaze.Core;
using MathGaze.Core.Commands;
using MathGaze.Core.Geometry;
using MathGaze.Services;
using SkiaSharp;

namespace MathGaze.ViewModels;

public enum ToolMode { Select, Point, Line, Circle, Protractor, Text }
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

    // Protractor placement — the first selected line (DrawState.AnchorPlaced)
    public LineObject? AnchorLine { get; private set; }

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

    [RelayCommand] private void ActivateSelect()    { ResetDrawState(); ActiveTool = ToolMode.Select; }
    [RelayCommand] private void ActivatePoint()     { ResetDrawState(); ActiveTool = ToolMode.Point;     StatusMessage = "Click to place a point"; }
    [RelayCommand] private void ActivateLine()      { ResetDrawState(); ActiveTool = ToolMode.Line;      StatusMessage = "Click to place start point"; }
    [RelayCommand] private void ActivateCircle()    { ResetDrawState(); ActiveTool = ToolMode.Circle;    StatusMessage = "Click to place centre"; }
    [RelayCommand] private void ActivateProtractor(){ ResetDrawState(); ActiveTool = ToolMode.Protractor; StatusMessage = "Click vertex (or a line)"; }
    [RelayCommand] private void ActivateText()      { ResetDrawState(); ActiveTool = ToolMode.Text;       StatusMessage = "Copy text, then click canvas"; }

    private void ResetDrawState()
    {
        AnchorPt      = null;
        AnchorLine    = null;
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

            case (ToolMode.Protractor, DrawState.Idle):
            {
                var hit = GeometryHitTester.TryHitObject(screenPx, _geometryService.Objects, mapper);
                if (hit is LineObject line1)
                {
                    // === EXISTING two-line path ===
                    AnchorLine    = line1;
                    DrawState     = DrawState.AnchorPlaced;
                    StatusMessage = "Click 2nd line";
                    _geometryService.SetSelected(line1.Id);
                }
                else
                {
                    // === NEW two-point path: vertex click on empty canvas ===
                    var (xPt, yPt) = mapper.ScreenToPage(screenPx);
                    AnchorPt  = (xPt, yPt);
                    // AnchorLine stays null (set by ResetDrawState) — this is the discriminator
                    DrawState = DrawState.AnchorPlaced;
                    StatusMessage = "Click to set baseline direction";
                }
                GhostChanged?.Invoke(this, EventArgs.Empty);
                break;
            }

            case (ToolMode.Protractor, DrawState.AnchorPlaced):
            {
                if (AnchorLine is not null)
                {
                    // === EXISTING two-line path ===
                    var hit = GeometryHitTester.TryHitObject(screenPx, _geometryService.Objects, mapper);
                    if (hit is not LineObject line2) break;               // not a line — ignore
                    if (line2.Id == AnchorLine!.Id)
                    {
                        // Same line clicked twice — place protractor at the nearest endpoint.
                        // Baseline aligns to the line direction; arc faces upward on screen.
                        var ep1Screen = mapper.PageToScreen(AnchorLine.X1Pt, AnchorLine.Y1Pt);
                        var ep2Screen = mapper.PageToScreen(AnchorLine.X2Pt, AnchorLine.Y2Pt);

                        double d1sq = Math.Pow(screenPx.X - ep1Screen.X, 2) + Math.Pow(screenPx.Y - ep1Screen.Y, 2);
                        double d2sq = Math.Pow(screenPx.X - ep2Screen.X, 2) + Math.Pow(screenPx.Y - ep2Screen.Y, 2);
                        double centerXPt = d1sq <= d2sq ? AnchorLine.X1Pt : AnchorLine.X2Pt;
                        double centerYPt = d1sq <= d2sq ? AnchorLine.Y1Pt : AnchorLine.Y2Pt;

                        double sameLineAngle = Math.Atan2(ep2Screen.Y - ep1Screen.Y, ep2Screen.X - ep1Screen.X) * 180.0 / Math.PI;
                        // Arc lives at -Y in local space; flip baseline so arc faces upward on screen.
                        if (Math.Cos(sameLineAngle * Math.PI / 180.0) < 0)
                            sameLineAngle += 180.0;

                        var sameLine = new ProtractorObject(centerXPt, centerYPt, sameLineAngle, AnchorLine.Id, AnchorLine.Id);
                        _geometryService.ExecuteCommand(new PlaceObjectCommand(sameLine));
                        _geometryService.SetSelected(sameLine.Id);
                        ResetDrawState();
                        StatusMessage = "Protractor placed";
                        break;
                    }

                    // Compute intersection in PDF-point space
                    if (!GeometryMath.TryLineIntersectPt(AnchorLine, line2, out var interPt))
                    {
                        // Parallel lines (D-02)
                        StatusMessage = "Lines are parallel — pick two non-parallel lines";
                        AnchorLine    = null;
                        DrawState     = DrawState.Idle;
                        GhostChanged?.Invoke(this, EventArgs.Empty);
                        break;
                    }

                    // Reject if intersection falls outside page bounds — catches near-parallel lines
                    // whose mathematical crossing is far off-screen (would look like a random placement).
                    const double margin = 20.0;
                    if (interPt.xPt < -margin || interPt.xPt > mapper.PageWidthPt + margin ||
                        interPt.yPt < -margin || interPt.yPt > mapper.PageHeightPt + margin)
                    {
                        StatusMessage = "Lines don't intersect on this page — extend lines to create a crossing";
                        GhostChanged?.Invoke(this, EventArgs.Empty);
                        break;
                    }

                    // Gap 1: BaselineAngleDeg = screen-space angle of Line 1 (flat diameter lies along Line 1)
                    var p1Screen = mapper.PageToScreen(AnchorLine.X1Pt, AnchorLine.Y1Pt);
                    var p2Screen = mapper.PageToScreen(AnchorLine.X2Pt, AnchorLine.Y2Pt);
                    double line1AngleDeg = Math.Atan2(p2Screen.Y - p1Screen.Y, p2Screen.X - p1Screen.X) * 180.0 / Math.PI;

                    // Flip baseline 180° if Line 2's click falls on the positive-Y (flat/baseline) side
                    // of the protractor in local canvas space, so the arc always faces toward Line 2.
                    // All vectors must be in SCREEN space (Y down) because line1AngleDeg is screen-space.
                    // Using PDF-space coords here would invert Y and fire the flip in the wrong direction.
                    // Use the student's click point on Line 2 as the direction indicator.
                    // screenPx is already available as the HandleCanvasClick parameter.
                    var intPtScreen = mapper.PageToScreen(interPt.xPt, interPt.yPt);
                    double dxScreen = screenPx.X - intPtScreen.X;
                    double dyScreen = screenPx.Y - intPtScreen.Y;

                    // Degenerate case: click within 5px of intersection — fall back to Line 2's P1→P2 direction.
                    if (Math.Sqrt(dxScreen * dxScreen + dyScreen * dyScreen) < 5.0)
                    {
                        var p1s = mapper.PageToScreen(line2.X1Pt, line2.Y1Pt);
                        var p2s = mapper.PageToScreen(line2.X2Pt, line2.Y2Pt);
                        dxScreen = p2s.X - p1s.X;
                        dyScreen = p2s.Y - p1s.Y;
                    }

                    double rad    = line1AngleDeg * Math.PI / 180.0;
                    double localX =  dxScreen * Math.Cos(-rad) - dyScreen * Math.Sin(-rad);
                    double localY =  dxScreen * Math.Sin(-rad) + dyScreen * Math.Cos(-rad);
                    // Arc occupies negative-Y of local space; flip baseline if Line 2 click is on positive-Y side.
                    if (localY > 0)
                        line1AngleDeg += 180.0;

                    double baselineAngleDeg = line1AngleDeg;

                    var protractor = new ProtractorObject(
                        interPt.xPt, interPt.yPt,
                        baselineAngleDeg,
                        AnchorLine.Id, line2.Id);

                    _geometryService.ExecuteCommand(new PlaceObjectCommand(protractor));
                    _geometryService.SetSelected(protractor.Id);
                    ResetDrawState();
                    StatusMessage = "Protractor placed";
                }
                else
                {
                    // === NEW two-point path ===
                    // AnchorPt holds the vertex (set in click 1 above)
                    var anchorScreen = mapper.PageToScreen(AnchorPt!.Value.xPt, AnchorPt!.Value.yPt);
                    double baselineAngleDeg = Math.Atan2(
                        screenPx.Y - anchorScreen.Y,
                        screenPx.X - anchorScreen.X) * 180.0 / Math.PI;

                    var protractor = new ProtractorObject(
                        AnchorPt.Value.xPt, AnchorPt.Value.yPt,
                        baselineAngleDeg,
                        Guid.Empty, Guid.Empty);  // no source lines; suppresses Practice Mode readout

                    _geometryService.ExecuteCommand(new PlaceObjectCommand(protractor));
                    _geometryService.SetSelected(protractor.Id);
                    ResetDrawState();
                    StatusMessage = "Protractor placed";
                }
                break;
            }

            case (ToolMode.Text, DrawState.Idle):
            {
                // Clipboard MUST be accessed on STA (UI) thread.
                // HandleCanvasClick is called from PdfCanvas.xaml.cs MouseDown — always on UI thread.
                // DO NOT move this into async/Task.Run — COMException (Pitfall 4 in RESEARCH.md).
                bool hasText = System.Windows.Clipboard.ContainsText();
                if (!hasText)
                {
                    StatusMessage = "Copy text first, then click to place";
                    break;
                }
                string clipText = System.Windows.Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(clipText))
                {
                    StatusMessage = "Copy text first, then click to place";
                    break;
                }
                var (xPt, yPt) = mapper.ScreenToPage(screenPx);
                _geometryService.ExecuteCommand(
                    new PlaceObjectCommand(new TextObject(clipText, xPt, yPt)));
                StatusMessage = "Text placed";
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
            if (ActiveTool == ToolMode.Protractor)
            {
                // Protractor: no snap during placement; ghost tracks cursor freely
                LastSnap = null;
                StatusMessage = AnchorLine is null
                    ? "Click to set baseline direction"
                    : "Click 2nd line";
            }
            else
            {
                var result = snap.Snap(screenPx, _geometryService.Objects, mapper);
                LastSnap = result;
                StatusMessage = result.Label is not null
                    ? $"Click 2nd point · snap: {result.Label}"
                    : (ActiveTool == ToolMode.Circle ? "Click radius point" : "Click 2nd point");
            }
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
