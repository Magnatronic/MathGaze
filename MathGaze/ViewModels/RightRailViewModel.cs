using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathGaze.Core.Commands;
using MathGaze.Core.Geometry;
using MathGaze.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace MathGaze.ViewModels;

/// <summary>
/// Drives the right rail panel: selection-aware nudge block, delete button, undo/redo.
/// Observes IGeometryService.ObjectsChanged to update all observable state.
/// Observes ToolViewModel.PropertyChanged to switch the DrawingGuidePanel (D-01).
///
/// Per D-07: NudgeLabel adapts to sub-selection state (endpoint A/B, centre/radius).
/// Per D-08/D-09: all mutations go through IGeometryService.ExecuteCommand().
/// Per Pitfall 2: nudge delta stored in PDF points (1 screen px ≈ 1 PDF pt at zoom=1).
/// </summary>
public sealed partial class RightRailViewModel : ObservableObject
{
    private readonly IGeometryService _geometryService;
    private readonly ToolViewModel    _toolVm;

    // ── Nudge step sizes ─────────────────────────────────────────────────────
    public static readonly int[] StepOptions = { 1, 5, 20 };

    [ObservableProperty] private int    _nudgeStepPx = 1;
    [ObservableProperty] private string _nudgeLabel  = string.Empty;
    [ObservableProperty] private bool   _hasSelection;
    [ObservableProperty] private string _selectedObjectType = string.Empty;
    [ObservableProperty] private bool   _isStyleClassic = true;
    [ObservableProperty] private bool   _isStyleFull    = false;

    // ── Drawing guide panel state ─────────────────────────────────────────────
    [ObservableProperty] private bool   _hasDrawingInProgress;
    [ObservableProperty] private bool   _hasSelectionPanel;
    [ObservableProperty] private string _drawingInstructionText = string.Empty;

    // ── Object list panel state (D-18) ────────────────────────────────────────
    [ObservableProperty] private bool _hasObjectList;

    /// <summary>
    /// Flat list of all objects on current page, rebuilt on every ObjectsChanged.
    /// One item per geometry object, ordered by placement (oldest first = document order).
    /// </summary>
    public ObservableCollection<ObjectListItem> ObjectList { get; } = new();

    /// <summary>
    /// Exposes ToolViewModel.CancelDrawCommand so the DrawingGuidePanel Cancel button
    /// can bind directly against the RightRailViewModel DataContext.
    /// </summary>
    public IRelayCommand CancelDrawCommand => _toolVm.CancelDrawCommand;

    public RightRailViewModel(IGeometryService geometryService, ToolViewModel toolVm)
    {
        _geometryService = geometryService;
        _toolVm          = toolVm;
        _toolVm.PropertyChanged          += OnToolPropertyChanged;
        _geometryService.ObjectsChanged  += OnObjectsChanged;
        Refresh();
    }

    // ── Internal refresh ─────────────────────────────────────────────────────

    private void OnObjectsChanged(object? sender, EventArgs e) => Refresh();

    private void OnToolPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ToolViewModel.HasDrawingInProgress)
                           or nameof(ToolViewModel.ActiveTool)
                           or nameof(ToolViewModel.DrawState))
        {
            UpdateDrawingState();
        }
    }

    private void UpdateDrawingState()
    {
        HasDrawingInProgress = _toolVm.HasDrawingInProgress;
        HasSelectionPanel    = HasSelection && !HasDrawingInProgress;
        HasObjectList        = _toolVm.ActiveTool == ToolMode.Select
                            && !HasSelection
                            && !HasDrawingInProgress;
        DrawingInstructionText = (_toolVm.ActiveTool, _toolVm.DrawState) switch
        {
            (ToolMode.Line,       DrawState.AnchorPlaced) => "Line in progress\nClick 2nd point",
            (ToolMode.Circle,     DrawState.AnchorPlaced) => "Circle in progress\nClick radius point",
            (ToolMode.Protractor, DrawState.AnchorPlaced) => "Protractor in progress\nClick 2nd point or line",
            _ => string.Empty,
        };
    }

    private void Refresh()
    {
        var obj = _geometryService.SelectedObject;
        HasSelection = obj is not null;

        SelectedObjectType = obj switch
        {
            PointObject      => "Point",
            LineObject       => "Line",
            CircleObject     => "Circle",
            ProtractorObject => "Protractor",
            TextObject       => "Text",        // D-06: no special rail controls needed
            _                => string.Empty,
        };

        NudgeLabel = obj switch
        {
            null             => string.Empty,
            LineObject l when l.SelectedEndpoint == 0   => "Move endpoint A",   // D-07
            LineObject l when l.SelectedEndpoint == 1   => "Move endpoint B",   // D-07
            CircleObject c when c.SelectedSubPoint == 0 => "Move centre",       // D-07
            CircleObject c when c.SelectedSubPoint == 1 => "Move radius",       // D-07
            ProtractorObject => "Move protractor",
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
        RotateMinus5Command.NotifyCanExecuteChanged();
        RotateMinus1Command.NotifyCanExecuteChanged();
        RotatePlus1Command.NotifyCanExecuteChanged();
        RotatePlus5Command.NotifyCanExecuteChanged();
        FlipScaleCommand.NotifyCanExecuteChanged();
        SetStyleClassicCommand.NotifyCanExecuteChanged();
        SetStyleFullCommand.NotifyCanExecuteChanged();
        ClearPageCommand.NotifyCanExecuteChanged();

        // Update protractor style toggle state
        if (obj is ProtractorObject prot)
        {
            IsStyleClassic = prot.Style == ProtractorStyle.Classic180;
            IsStyleFull    = prot.Style == ProtractorStyle.Full360;
        }
        else
        {
            IsStyleClassic = true;
            IsStyleFull    = false;
        }

        // Rebuild object list (D-18, D-20)
        var pageObjects = _geometryService.Objects;
        ObjectList.Clear();
        int lineCount = 0, circleCount = 0, pointCount = 0, protCount = 0, textCount = 0;
        foreach (var geoObj in pageObjects)
        {
            string typeName = geoObj switch
            {
                PointObject      => "Point",
                LineObject       => "Line",
                CircleObject     => "Circle",
                ProtractorObject => "Protractor",
                TextObject       => "Text",
                _                => "Object",
            };
            int idx = geoObj switch
            {
                PointObject      => ++pointCount,
                LineObject       => ++lineCount,
                CircleObject     => ++circleCount,
                ProtractorObject => ++protCount,
                TextObject       => ++textCount,
                _                => 0,
            };
            var capturedId = geoObj.Id;  // Capture Id in local for RelayCommand lambda closure (D-19)
            ObjectList.Add(new ObjectListItem
            {
                DisplayName   = $"{typeName} {idx}",
                TypeLabel     = typeName,
                SelectCommand = new RelayCommand(() => _geometryService.SetSelected(capturedId)),
            });
        }

        // Sync drawing state after selection changes (also recomputes HasObjectList)
        UpdateDrawingState();
    }

    // ── Clear page command ────────────────────────────────────────────────────

    [RelayCommand]
    private void ClearPage()
    {
        var snapshot = _geometryService.Objects.ToList();
        if (snapshot.Count == 0) return;  // no-op if page already empty
        _geometryService.ExecuteCommand(new ClearPageCommand(snapshot));
        // ExecuteCommand raises ObjectsChanged → auto-save triggers (D-10)
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

    // ── Protractor commands ──────────────────────────────────────────────────

    private bool CanProtractor() => _geometryService.SelectedObject is ProtractorObject;

    [RelayCommand(CanExecute = nameof(CanProtractor))]
    private void RotateMinus5()
    {
        if (_geometryService.SelectedObject is ProtractorObject p)
            _geometryService.ExecuteCommand(new RotateProtractorCommand(p.Id, -5.0));
    }

    [RelayCommand(CanExecute = nameof(CanProtractor))]
    private void RotateMinus1()
    {
        if (_geometryService.SelectedObject is ProtractorObject p)
            _geometryService.ExecuteCommand(new RotateProtractorCommand(p.Id, -1.0));
    }

    [RelayCommand(CanExecute = nameof(CanProtractor))]
    private void RotatePlus1()
    {
        if (_geometryService.SelectedObject is ProtractorObject p)
            _geometryService.ExecuteCommand(new RotateProtractorCommand(p.Id, +1.0));
    }

    [RelayCommand(CanExecute = nameof(CanProtractor))]
    private void RotatePlus5()
    {
        if (_geometryService.SelectedObject is ProtractorObject p)
            _geometryService.ExecuteCommand(new RotateProtractorCommand(p.Id, +5.0));
    }

    [RelayCommand(CanExecute = nameof(CanProtractor))]
    private void FlipScale()
    {
        if (_geometryService.SelectedObject is ProtractorObject p)
            _geometryService.ExecuteCommand(new FlipProtractorCommand(p.Id));
    }

    [RelayCommand(CanExecute = nameof(CanProtractor))]
    private void SetStyleClassic()
    {
        if (_geometryService.SelectedObject is ProtractorObject p &&
            p.Style != ProtractorStyle.Classic180)
            _geometryService.ExecuteCommand(
                new StyleProtractorCommand(p.Id, ProtractorStyle.Classic180, p.Style));
    }

    [RelayCommand(CanExecute = nameof(CanProtractor))]
    private void SetStyleFull()
    {
        if (_geometryService.SelectedObject is ProtractorObject p &&
            p.Style != ProtractorStyle.Full360)
            _geometryService.ExecuteCommand(
                new StyleProtractorCommand(p.Id, ProtractorStyle.Full360, p.Style));
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

/// <summary>
/// Display record for the object list panel (D-18). One item per geometry object.
/// SelectCommand selects that object — equivalent to clicking it on canvas.
/// </summary>
public sealed class ObjectListItem
{
    public string        DisplayName   { get; init; } = string.Empty;
    public string        TypeLabel     { get; init; } = string.Empty;
    public IRelayCommand SelectCommand { get; init; } = null!;
}
