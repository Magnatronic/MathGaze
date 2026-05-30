using MathGaze.Core.Geometry;
using MathGaze.Services;

namespace MathGaze.Core.Commands;

/// <summary>
/// Removes all geometry objects on the current page in a single undoable action (D-08, D-09).
/// The snapshot is captured by the caller before calling ExecuteCommand, then passed in the
/// constructor. Execute() iterates the snapshot to remove; Undo() re-adds all from snapshot.
///
/// CRITICAL: Snapshot must be a defensive copy (ToList()) taken BEFORE ExecuteCommand is called.
/// _geometryService.Objects is a live view — iterating while removing would corrupt it (Pitfall 3).
/// </summary>
public sealed class ClearPageCommand : IGeometryCommand
{
    private readonly IReadOnlyList<GeometryObject> _snapshot;

    public ClearPageCommand(IReadOnlyList<GeometryObject> snapshot)
    {
        _snapshot = snapshot;
    }

    public void Execute(IGeometryService service)
    {
        foreach (var obj in _snapshot)
            service.RemoveObject(obj.Id);
        service.ClearSelection();
    }

    public void Undo(IGeometryService service)
    {
        foreach (var obj in _snapshot)
            service.AddObject(obj);
    }
}
