using Godot;
using System.Collections.Generic;
using System.Linq;
using static EditorState;
using static LevelState;
using static LineFactory;
using static Path;
using static RouteCreationStep;

public partial class RouteEditor : Node
{
    // public static List<RoadNode> SelectedRoadNodes { get; set; } = [];

    /// <summary>
    /// This exists so that we do not need to add a Route to LevelState until
    /// it's fully created and valid.
    /// </summary>
    private static Route _routeInProgress;

    public static void StartRouteCreation(RoadNode startNode)
    {
        GD.Print("Route creation started at: " + startNode.Name);
        CurrentRouteCreationStep = RouteCreationStep.AddingSubsequentStops;
        _routeInProgress = new Route();
        _routeInProgress.AppendNode(startNode);

        _routeInProgress.Visual = new RouteVisual(_routeInProgress);
        CurrentLevel.AddChild(_routeInProgress.Visual);

        RoutePreviewLine = CreateLineAt(startNode.GlobalPosition);
        RoutePreviewLine.DefaultColor = _routeInProgress.Color;
        CurrentLevel.AddChild(RoutePreviewLine);
    }

    public static void ContinueRoute(RoadNode nextNode)
    {
        GD.Print("Continuing route to: " + nextNode.Name);
        _routeInProgress.AppendNode(nextNode);
        RoutePreviewLine.SetPointPosition(RoutePreviewLine.GetPointCount() - 1, nextNode.GlobalPosition);
        RoutePreviewLine.AddPoint(nextNode.GlobalPosition);
    }

    public static void FinalizeRouteCreation()
    {
        RoadNode lastNode = _routeInProgress.Path[^1];

        if (_routeInProgress.Path.Count < 2 || lastNode is not BusStop)
        {
            CurrentLevel.GetNode<ErrorMessage>(Path.ErrorMessageNode).DisplayMessage("Route must start and end at a bus stop");
            ReturnRouteColor(new KeyValuePair<string, Color>(_routeInProgress.ColorName, _routeInProgress.Color));
            _routeInProgress.Visual?.QueueFree();
        }
        else
        {
            LevelState.AllRoutes.Add(_routeInProgress);
            var routeList = CurrentLevel.GetNode<ItemList>(Path.RouteListNode);
            routeList.AddItem(_routeInProgress.ColorName + " line");
            LevelState.UpdateAllHouseStatuses();
            LevelState.RefreshAllRouteVisuals();
        }
        foreach (var route in LevelState.AllRoutes)
        {
            GD.Print($"Route {route.ColorName}: {string.Join(" -> ", route.Path.Select(n => n.Name))}");
        }
        ResetState();
    }

    private static void ResetState()
    {
        _routeInProgress = null;
        RoutePreviewLine?.QueueFree();
        RoutePreviewLine = null;
        CurrentRouteCreationStep = NotCreating;
        IsEditingFromStart = false;
    }
}