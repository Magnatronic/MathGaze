using MathGaze.Services;

namespace MathGaze.Core.Commands;

/// <summary>
/// Nudges a specific sub-point of a selected object.
///
/// For LineObject (D-04):
///   endpointIndex 0 → nudges X1Pt/Y1Pt
///   endpointIndex 1 → nudges X2Pt/Y2Pt
///
/// For CircleObject (D-05):
///   subPointIndex 0 → nudges CenterXPt/CenterYPt (translates whole circle)
///   subPointIndex 1 → adjusts RadiusPt by dxPt (horizontal = radius change; dyPt ignored for radius)
/// </summary>
public sealed class NudgeEndpointCommand : IGeometryCommand
{
    private readonly Guid   _objectId;
    private readonly int    _subPointIndex;
    private readonly double _dxPt;
    private readonly double _dyPt;

    public NudgeEndpointCommand(Guid objectId, int subPointIndex, double dxPt, double dyPt)
    {
        _objectId       = objectId;
        _subPointIndex  = subPointIndex;
        _dxPt = dxPt;
        _dyPt = dyPt;
    }

    public void Execute(IGeometryService service)
        => service.NudgeSubPoint(_objectId, _subPointIndex,  _dxPt,  _dyPt);

    public void Undo(IGeometryService service)
        => service.NudgeSubPoint(_objectId, _subPointIndex, -_dxPt, -_dyPt);
}
