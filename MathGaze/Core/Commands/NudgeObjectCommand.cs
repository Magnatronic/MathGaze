using MathGaze.Services;

namespace MathGaze.Core.Commands;

/// <summary>
/// Translates a whole geometry object by (dxPt, dyPt) in PDF point coordinates.
/// Per D-03: used when no sub-point is selected.
/// Per D-10 + Pitfall 2: deltas are stored in PDF points (not screen pixels) so undo
/// applies the same physical displacement regardless of the zoom level at undo time.
/// </summary>
public sealed class NudgeObjectCommand : IGeometryCommand
{
    private readonly Guid   _objectId;
    private readonly double _dxPt;
    private readonly double _dyPt;

    public NudgeObjectCommand(Guid objectId, double dxPt, double dyPt)
    {
        _objectId = objectId;
        _dxPt = dxPt;
        _dyPt = dyPt;
    }

    public void Execute(IGeometryService service) => service.NudgeObject(_objectId,  _dxPt,  _dyPt);
    public void Undo(IGeometryService service)    => service.NudgeObject(_objectId, -_dxPt, -_dyPt);
}
