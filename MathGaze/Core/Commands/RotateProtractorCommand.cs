using MathGaze.Core.Geometry;
using MathGaze.Services;

namespace MathGaze.Core.Commands;

/// <summary>
/// Rotates a ProtractorObject by deltaDeg degrees (added to RotationOffsetDeg).
/// Undo subtracts the same delta. Per D-08 Phase 2: each button press = one undo entry.
/// </summary>
public sealed class RotateProtractorCommand : IGeometryCommand
{
    private readonly Guid   _id;
    private readonly double _deltaDeg;

    public RotateProtractorCommand(Guid id, double deltaDeg)
    {
        _id       = id;
        _deltaDeg = deltaDeg;
    }

    public void Execute(IGeometryService service) => Apply(service,  _deltaDeg);
    public void Undo   (IGeometryService service) => Apply(service, -_deltaDeg);

    private void Apply(IGeometryService service, double delta)
    {
        if (service.Objects.FirstOrDefault(o => o.Id == _id) is ProtractorObject p)
            p.RotationOffsetDeg += delta;
    }
}
