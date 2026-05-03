using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathGaze.Core.Commands;
using MathGaze.Core.Geometry;
using MathGaze.Services;

namespace MathGaze.ViewModels;

/// <summary>
/// Drives the right rail panel: selection-aware nudge block, delete button, undo/redo.
/// Observes IGeometryService.ObjectsChanged to update all observable state.
///
/// Per D-07: NudgeLabel adapts to sub-selection state (endpoint A/B, centre/radius).
/// Per D-08/D-09: all mutations go through IGeometryService.ExecuteCommand().
/// Per Pitfall 2: nudge delta stored in PDF points (1 screen px ≈ 1 PDF pt at zoom=1).
/// </summary>
public sealed partial class RightRailViewModel : ObservableObject
{
    private readonly IGeometryService _geometryService;

    // ── Nudge step sizes ─────────────────────────────────────────────────────
    public static readonly int[] StepOptions = { 1, 5, 20 };

    [ObservableProperty] private int    _nudgeStepPx = 1;
    [ObservableProperty] private string _nudgeLabel  = string.Empty;
    [ObservableProperty] private bool   _hasSelection;
    [ObservableProperty] private string _selectedObjectType = string.Empty;

    public RightRailViewModel(IGeometryService geometryService)
    {
        _geometryService = geometryService;
        _geometryService.ObjectsChanged += OnObjectsChanged;
        Refresh();
    }

    // ── Internal refresh ─────────────────────────────────────────────────────

    private void OnObjectsChanged(object? sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        var obj = _geometryService.SelectedObject;
        HasSelection = obj is not null;

        SelectedObjectType = obj switch
        {
            PointObject  => "Point",
            LineObject   => "Line",
            CircleObject => "Circle",
            _            => string.Empty,
        };

        NudgeLabel = obj switch
        {
            null => string.Empty,
            LineObject l when l.SelectedEndpoint == 0 => "Move endpoint A",   // D-07
            LineObject l when l.SelectedEndpoint == 1 => "Move endpoint B",   // D-07
            CircleObject c when c.SelectedSubPoint == 0 => "Move centre",     // D-07
            CircleObject c when c.SelectedSubPoint == 1 => "Move radius",     // D-07
            _ => "Move",
        };

        // Notify all commands of potential CanExecute change
        NudgeUpCommand.NotifyCanExecuteChanged();
        NudgeDownCommand.NotifyCanExecuteChanged();
        NudgeLeftCommand.NotifyCanExecuteChanged();
        NudgeRightCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    // ── Step selection ───────────────────────────────────────────────────────

    [RelayCommand]
    private void SetStep(string stepStr)
    {
        if (int.TryParse(stepStr, out var step))
            NudgeStepPx = step;
    }

    // ── Nudge commands ───────────────────────────────────────────────────────

    private bool CanNudge() => _geometryService.SelectedObject is not null;

    // PDF Y-axis: 0 = bottom, positive = upward. Increasing YPt moves object UP on screen.
    // PageToScreen: screenY = (pageHeightPt - yPt) * Scale + originY → higher YPt = lower screenY.
    [RelayCommand(CanExecute = nameof(CanNudge))]
    private void NudgeUp()    => DispatchNudge(0, +NudgeStepPx);   // GAP-3 fix: +Y = upward in PDF space

    [RelayCommand(CanExecute = nameof(CanNudge))]
    private void NudgeDown()  => DispatchNudge(0, -NudgeStepPx);   // GAP-3 fix: -Y = downward in PDF space

    [RelayCommand(CanExecute = nameof(CanNudge))]
    private void NudgeLeft()  => DispatchNudge(-NudgeStepPx, 0);

    [RelayCommand(CanExecute = nameof(CanNudge))]
    private void NudgeRight() => DispatchNudge(+NudgeStepPx, 0);

    /// <summary>
    /// Convert screen-pixel step to PDF-point delta and dispatch the correct command.
    /// dxPx/dyPx are in screen pixels. At zoom=1.0, 1 screen px = 1 PDF pt.
    /// The command stores PDF-pt deltas; zoom-independence is a property of the command
    /// pattern (D-10 + Pitfall 2 in RESEARCH.md — this is intentional and correct).
    /// </summary>
    private void DispatchNudge(double dxPx, double dyPx)
    {
        var obj = _geometryService.SelectedObject;
        if (obj is null) return;

        // Treat step as PDF points directly (see comment above)
        double dxPt = dxPx;
        double dyPt = dyPx;

        // Choose endpoint vs. whole-object command based on active sub-point
        int? subPoint = obj switch
        {
            LineObject l   => l.SelectedEndpoint,
            CircleObject c => c.SelectedSubPoint,
            _              => null,
        };

        IGeometryCommand cmd = subPoint.HasValue
            ? new NudgeEndpointCommand(obj.Id, subPoint.Value, dxPt, dyPt)
            : new NudgeObjectCommand(obj.Id, dxPt, dyPt);

        _geometryService.ExecuteCommand(cmd);
    }

    // ── Delete command ───────────────────────────────────────────────────────

    private bool CanDelete() => _geometryService.SelectedObject is not null;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void Delete()
    {
        var obj = _geometryService.SelectedObject;
        if (obj is null) return;
        _geometryService.ExecuteCommand(new DeleteObjectCommand(obj));
    }

    // ── Undo/Redo commands ───────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanUndoExec))]
    private void Undo() => _geometryService.Undo();
    private bool CanUndoExec() => _geometryService.CanUndo;

    [RelayCommand(CanExecute = nameof(CanRedoExec))]
    private void Redo() => _geometryService.Redo();
    private bool CanRedoExec() => _geometryService.CanRedo;
}
