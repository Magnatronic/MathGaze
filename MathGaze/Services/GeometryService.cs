using MathGaze.Core.Commands;
using MathGaze.Core.Geometry;

namespace MathGaze.Services;

/// <summary>
/// Singleton owner of all geometry objects and the undo/redo history.
/// All mutations MUST flow through ExecuteCommand(); do not call AddObject/RemoveObject/Nudge directly
/// from outside commands.
/// </summary>
public sealed class GeometryService : IGeometryService
{
    private readonly List<GeometryObject> _objects = new();
    private readonly UndoService _undoService = new();

    public IReadOnlyList<GeometryObject> Objects => _objects;

    public GeometryObject? SelectedObject => _objects.FirstOrDefault(o => o.IsSelected);

    public event EventHandler? ObjectsChanged;

    // ── Direct mutation (called by IGeometryCommand implementations only) ─────

    public void AddObject(GeometryObject obj)
    {
        _objects.Add(obj);
        // Do NOT raise ObjectsChanged here — ExecuteCommand raises it after the full command
    }

    public void RemoveObject(Guid id)
    {
        var obj = _objects.FirstOrDefault(o => o.Id == id);
        if (obj is not null) _objects.Remove(obj);
    }

    public void NudgeObject(Guid id, double dxPt, double dyPt)
    {
        var obj = _objects.FirstOrDefault(o => o.Id == id);
        if (obj is null) return;

        switch (obj)
        {
            case PointObject p:
                p.XPt += dxPt;
                p.YPt += dyPt;
                break;
            case LineObject l:
                l.X1Pt += dxPt; l.Y1Pt += dyPt;
                l.X2Pt += dxPt; l.Y2Pt += dyPt;
                break;
            case CircleObject c:
                c.CenterXPt += dxPt;
                c.CenterYPt += dyPt;
                break;
        }
    }

    public void NudgeSubPoint(Guid id, int subPointIndex, double dxPt, double dyPt)
    {
        var obj = _objects.FirstOrDefault(o => o.Id == id);
        if (obj is null) return;

        switch (obj)
        {
            case LineObject l:
                // D-04: index 0 = endpoint A, index 1 = endpoint B
                // Out-of-range subPointIndex silently no-ops (T-02-06 mitigation)
                if (subPointIndex == 0)      { l.X1Pt += dxPt; l.Y1Pt += dyPt; }
                else if (subPointIndex == 1) { l.X2Pt += dxPt; l.Y2Pt += dyPt; }
                // else: no-op (out-of-range index)
                break;
            case CircleObject c:
                // D-05: index 0 = center (translate), index 1 = edge (radius change)
                // Out-of-range subPointIndex silently no-ops (T-02-06 mitigation)
                if (subPointIndex == 0)      { c.CenterXPt += dxPt; c.CenterYPt += dyPt; }
                else if (subPointIndex == 1) { c.RadiusPt   += dxPt; } // horizontal nudge = radius change
                // else: no-op (out-of-range index)
                break;
        }
    }

    // ── Transient UI state helpers ────────────────────────────────────────────

    /// <summary>Raises ObjectsChanged without going through the command stack. Use only for transient UI state (sub-point selection).</summary>
    public void ObjectsChanged_ForceRaise() => ObjectsChanged?.Invoke(this, EventArgs.Empty);

    // ── Selection (not undoable — transient UI state) ─────────────────────────

    public void SetSelected(Guid id)
    {
        foreach (var o in _objects) o.IsSelected = (o.Id == id);
        ObjectsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearSelection()
    {
        foreach (var o in _objects) o.IsSelected = false;
        ObjectsChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── Command dispatch ──────────────────────────────────────────────────────

    public void ExecuteCommand(IGeometryCommand cmd)
    {
        _undoService.Execute(cmd, this);
        ObjectsChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── Undo/Redo ─────────────────────────────────────────────────────────────

    public void Undo()
    {
        _undoService.Undo(this);
        ObjectsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        _undoService.Redo(this);
        ObjectsChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool CanUndo => _undoService.CanUndo;
    public bool CanRedo => _undoService.CanRedo;

    public void Reset()
    {
        _objects.Clear();
        _undoService.Clear();
        ObjectsChanged?.Invoke(this, EventArgs.Empty);
    }
}
