using MathGaze.Core.Geometry;
using MathGaze.Services;

namespace MathGaze.Core.Commands;

/// <summary>
/// Swaps ProtractorObject.Style between Classic180 and Full360.
/// Undo swaps back. Per D-05: Classic180 = semicircle, Full360 = full circle.
/// </summary>
public sealed class StyleProtractorCommand : IGeometryCommand
{
    private readonly Guid            _id;
    private readonly ProtractorStyle _newStyle;
    private readonly ProtractorStyle _oldStyle;

    public StyleProtractorCommand(Guid id, ProtractorStyle newStyle, ProtractorStyle oldStyle)
    {
        _id       = id;
        _newStyle = newStyle;
        _oldStyle = oldStyle;
    }

    public void Execute(IGeometryService service) => SetStyle(service, _newStyle);
    public void Undo   (IGeometryService service) => SetStyle(service, _oldStyle);

    private void SetStyle(IGeometryService service, ProtractorStyle style)
    {
        if (service.Objects.FirstOrDefault(o => o.Id == _id) is ProtractorObject p)
            p.Style = style;
    }
}
