using MathGaze.Core.Geometry;
using MathGaze.Services;

namespace MathGaze.Core.Commands;

public sealed class DeleteObjectCommand : IGeometryCommand
{
    private readonly GeometryObject _obj;

    public DeleteObjectCommand(GeometryObject obj) => _obj = obj;

    public void Execute(IGeometryService service) => service.RemoveObject(_obj.Id);
    public void Undo(IGeometryService service)    => service.AddObject(_obj);
}
