using System.Threading;
using Godot;
using static LevelState;
using System.Linq;
using static Path;
using System.Collections.Generic;

public partial class BusStopDeletor : Area2D
{
    private void _on_input_event(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (!@event.IsRightMouseClick())
        {
            return;
        }
        if (Budget < Cost.BusStopRemoval)
        {
            var errorMessage = GetTree().CurrentScene.GetNode<ErrorMessage>
            (
                ErrorMessageNode
            );
            errorMessage.DisplayMessage("Insufficient budget to remove bus stop.");
            return;
        }

        var busStop = GetParent<BusStop>();

        var affectedRoutes = AllBusLines.Where(busLine => busLine.ContainsNode(busStop)).ToList();
        foreach (var busLine in affectedRoutes)
        {
            busLine.RemoveNode(busStop);
            if (busLine.Path.OfType<BusStop>().Count() <= 1)
            {
                var busLineList = GetTree().CurrentScene.GetNode<BusLineList>(BusLineListNode);
                busLineList.DeleteBusLine(busLine);
                busLine.Delete();
            }
        }

        var A = busStop.Neighbors[0];
        var B = busStop.Neighbors[1];
        A.AddNeighbor(B);
        B.AddNeighbor(A);
        
        A.RemoveNeighbor(busStop);
        B.RemoveNeighbor(busStop);
        
        for (int i = busStop.ConnectedEdges.Count - 1; i >= 0; i--)
        {
            busStop.ConnectedEdges[i]?.QueueFree();
        }

        busStop.QueueFree(); // check _ExitTree() of the BusStop class to see side effects

        var roadEdgeScene = GD.Load<PackedScene>(RoadEdgeScene);
        var edge = roadEdgeScene.Instantiate<RoadEdge>();
        CurrentLevel.AddChild(edge);
        edge.SetEndpoints(A, B);

        UpdateAllHouseStatuses();
    }
}