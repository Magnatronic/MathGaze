using MathGaze.Core.Geometry;
using MathGaze.Services;

namespace MathGaze.Core.Commands;

public sealed class PlaceObjectCommand : IGeometryCommand
{
    private readonly GeometryObject _obj;

    public PlaceObjectCommand(GeometryObject obj) => _obj = obj;

    public void Execute(IGeometryService service) => service.AddObject(_obj);
    public void Undo(IGeometryService service)    => service.RemoveObject(_obj.Id);
}
