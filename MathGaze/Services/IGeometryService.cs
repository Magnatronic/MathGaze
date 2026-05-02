using MathGaze.Core.Commands;
using MathGaze.Core.Geometry;

namespace MathGaze.Services;

/// <summary>
/// Single source of truth for all geometry objects in the current session.
/// All mutations go through ExecuteCommand() so every action is undoable (D-09).
/// </summary>
public interface IGeometryService
{
    IReadOnlyList<GeometryObject> Objects { get; }
    GeometryObject? SelectedObject { get; }

    /// <summary>Raised after any mutation (add/remove/nudge/select). Subscribers should repaint canvas.</summary>
    event EventHandler? ObjectsChanged;

    // ── Direct mutation methods (called by commands, not by external callers) ──
    void AddObject(GeometryObject obj);
    void RemoveObject(Guid id);
    void NudgeObject(Guid id, double dxPt, double dyPt);
    void NudgeSubPoint(Guid id, int subPointIndex, double dxPt, double dyPt);

    // ── Selection (not undoable — selection is transient UI state) ──
    void SetSelected(Guid id);
    void ClearSelection();

    // ── Command dispatch (records undo entry) ──
    void ExecuteCommand(IGeometryCommand cmd);

    // ── Undo/Redo (delegated to UndoService) ──
    void Undo();
    void Redo();
    bool CanUndo { get; }
    bool CanRedo { get; }

    /// <summary>Clear all objects and reset undo/redo stacks (e.g., when closing a document).</summary>
    void Reset();
}
