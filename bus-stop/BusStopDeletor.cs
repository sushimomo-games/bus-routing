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

        var affectedRoutes = AllRoutes.Where(route => route.ContainsNode(busStop)).ToList();
        foreach (var route in affectedRoutes)
        {
            route.RemoveNode(busStop);
            if (route.Path.OfType<BusStop>().Count() <= 1)
            {
                var routeList = GetTree().CurrentScene.GetNode<RouteList>(RouteListNode);
                routeList.DeleteRoute(route);
                LevelState.AllRoutes.Remove(route);
                LevelState.ReturnRouteColor(new KeyValuePair<string, Color>(route.ColorName, route.Color));
                route.Visual.QueueFree();
                route.QueueFree();
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