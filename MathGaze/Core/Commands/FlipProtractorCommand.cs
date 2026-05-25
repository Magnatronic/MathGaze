using MathGaze.Core.Geometry;
using MathGaze.Services;

namespace MathGaze.Core.Commands;

/// <summary>
/// Toggles ProtractorObject.IsFlipped between inner (0→180°) and outer (180→0°) scale.
/// Undo toggles back. Per D-03: IsFlipped false = inner scale, true = outer scale.
/// </summary>
public sealed class FlipProtractorCommand : IGeometryCommand
{
    private readonly Guid _id;

    public FlipProtractorCommand(Guid id) => _id = id;

    public void Execute(IGeometryService service) => Toggle(service);
    public void Undo   (IGeometryService service) => Toggle(service);

    private void Toggle(IGeometryService service)
    {
        if (service.Objects.FirstOrDefault(o => o.Id == _id) is ProtractorObject p)
            p.IsFlipped = !p.IsFlipped;
    }
}
