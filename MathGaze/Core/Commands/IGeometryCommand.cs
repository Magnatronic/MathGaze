using MathGaze.Services;

namespace MathGaze.Core.Commands;

/// <summary>
/// Per-action command. Every geometry mutation goes through this interface.
/// Execute() applies the change; Undo() reverses it exactly.
/// Per D-08: one command per discrete user action (place, delete, each nudge click).
/// Per D-09: all mutations go through the command stack — no direct list mutation.
/// </summary>
public interface IGeometryCommand
{
    void Execute(IGeometryService service);
    void Undo(IGeometryService service);
}
