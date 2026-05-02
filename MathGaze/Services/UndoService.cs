using MathGaze.Core.Commands;

namespace MathGaze.Services;

/// <summary>
/// Per-click undo/redo stack (D-08). Every Execute() call pushes to undoStack and clears redoStack.
/// No time-window batching — each command is one undo entry.
/// </summary>
public sealed class UndoService
{
    private readonly Stack<IGeometryCommand> _undoStack = new();
    private readonly Stack<IGeometryCommand> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void Execute(IGeometryCommand cmd, IGeometryService service)
    {
        cmd.Execute(service);
        _undoStack.Push(cmd);
        _redoStack.Clear();
    }

    public void Undo(IGeometryService service)
    {
        if (_undoStack.TryPop(out var cmd))
        {
            cmd.Undo(service);
            _redoStack.Push(cmd);
        }
    }

    public void Redo(IGeometryService service)
    {
        if (_redoStack.TryPop(out var cmd))
        {
            cmd.Execute(service);
            _undoStack.Push(cmd);
        }
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
