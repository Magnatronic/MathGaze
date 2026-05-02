using MathGaze.Core.Commands;
using MathGaze.Core.Geometry;
using MathGaze.Services;
using Xunit;

namespace MathGaze.Tests;

public class UndoServiceTests
{
    // Minimal in-memory service for testing — avoids WPF DI overhead in tests
    private static GeometryService MakeService() => new GeometryService();

    [Fact]
    public void Execute_PushesToUndoStack_ClearsRedoStack()
    {
        var svc = MakeService();
        var undo = new UndoService();
        var pt = new PointObject(100, 200);
        var cmd = new PlaceObjectCommand(pt);

        undo.Execute(cmd, svc);

        Assert.True(undo.CanUndo);
        Assert.False(undo.CanRedo);
        Assert.Single(svc.Objects);
    }

    [Fact]
    public void Undo_RemovesFromUndoStack_PushesToRedoStack_ReversesChange()
    {
        var svc = MakeService();
        var undo = new UndoService();
        var pt = new PointObject(100, 200);
        undo.Execute(new PlaceObjectCommand(pt), svc);

        undo.Undo(svc);

        Assert.False(undo.CanUndo);
        Assert.True(undo.CanRedo);
        Assert.Empty(svc.Objects);
    }

    [Fact]
    public void Redo_MovesCommandBackToUndoStack_ReappliesChange()
    {
        var svc = MakeService();
        var undo = new UndoService();
        var pt = new PointObject(100, 200);
        undo.Execute(new PlaceObjectCommand(pt), svc);
        undo.Undo(svc);

        undo.Redo(svc);

        Assert.True(undo.CanUndo);
        Assert.False(undo.CanRedo);
        Assert.Single(svc.Objects);
    }

    [Fact]
    public void NewActionAfterUndo_ClearsRedoStack()
    {
        var svc = MakeService();
        var undo = new UndoService();
        var pt1 = new PointObject(100, 200);
        var pt2 = new PointObject(300, 400);
        undo.Execute(new PlaceObjectCommand(pt1), svc);
        undo.Undo(svc);

        // New action after undo — must clear redo
        undo.Execute(new PlaceObjectCommand(pt2), svc);

        Assert.False(undo.CanRedo);
        Assert.True(undo.CanUndo);
    }

    [Fact]
    public void ThreeNudges_UndoThrice_RestoresOriginalPosition()
    {
        var svc = MakeService();
        var undo = new UndoService();
        var pt = new PointObject(100, 200);
        // Place the point via command so it's in the service list
        svc.AddObject(pt);

        // Three nudges of (10, 0) in PDF points
        undo.Execute(new NudgeObjectCommand(pt.Id, 10, 0), svc);
        undo.Execute(new NudgeObjectCommand(pt.Id, 10, 0), svc);
        undo.Execute(new NudgeObjectCommand(pt.Id, 10, 0), svc);

        undo.Undo(svc); undo.Undo(svc); undo.Undo(svc);

        Assert.Equal(100, ((PointObject)svc.Objects[0]).XPt, precision: 6);
        Assert.Equal(200, ((PointObject)svc.Objects[0]).YPt, precision: 6);
    }

    [Fact]
    public void DeleteCommand_Execute_RemovesObject_Undo_RestoresObject()
    {
        var svc = MakeService();
        var undo = new UndoService();
        var pt = new PointObject(100, 200);
        svc.AddObject(pt);

        undo.Execute(new DeleteObjectCommand(pt), svc);
        Assert.Empty(svc.Objects);

        undo.Undo(svc);
        Assert.Single(svc.Objects);
    }

    [Fact]
    public void NudgeEndpointCommand_EndpointA_MovesOnlyFirstEndpoint()
    {
        var svc = MakeService();
        var undo = new UndoService();
        var line = new LineObject(0, 0, 100, 0);
        svc.AddObject(line);

        // Nudge endpoint A by (10, 5) in PDF points
        undo.Execute(new NudgeEndpointCommand(line.Id, 0, 10, 5), svc);

        Assert.Equal(10,  line.X1Pt, precision: 6);
        Assert.Equal(5,   line.Y1Pt, precision: 6);
        Assert.Equal(100, line.X2Pt, precision: 6); // endpoint B unchanged
        Assert.Equal(0,   line.Y2Pt, precision: 6);
    }
}
