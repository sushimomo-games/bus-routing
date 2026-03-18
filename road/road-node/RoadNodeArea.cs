using Godot;
using System.Collections.Generic;
using System.Linq;
using static EditorState;
using static LineFactory;
using static LevelState;
using static RouteCreationStep;
using static Path;

public partial class RoadNodeArea : Area2D
{
    /// <summary>
    /// This exists so that we do not need to add a Route to LevelState until
    /// it's fully created and valid.
    /// </summary>
    private static Route _tempRoute;

    /// <summary>
    /// Backup of the route's path for reverting invalid edits.
    /// </summary>
    private static List<RoadNode> _routeBackup;
    private ErrorMessage errorMessage;

    public override void _Ready()
    {
        errorMessage = GetTree().CurrentScene.GetNode<ErrorMessage>
        (
            ErrorMessageNode
        );
    }

    private void DrawPreviewLine()
    {
        if (RoutePreviewLine == null)
            return;

        if (RoutePreviewLine.GetPointCount() < 2)
            RoutePreviewLine.AddPoint(GetGlobalMousePosition());
        else
            RoutePreviewLine.SetPointPosition
            (
                RoutePreviewLine.GetPointCount() - 1, GetGlobalMousePosition()
            );
    }

    public override void _Process(double delta)
    {
        if (CurrentRouteCreationStep == AddingSubsequentStops
         || CurrentRouteCreationStep == EditingRoute)
        {
            DrawPreviewLine();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsLeftMouseRelease()
        && (CurrentRouteCreationStep == AddingSubsequentStops || CurrentRouteCreationStep == EditingRoute))
        {
            FinalizeRoute();
        }
    }

    private void _on_input_event(Node viewport, InputEvent @event, long shapeIdx)
    {
        var clickedRoadNode = GetParent<RoadNode>();

        if (@event.IsLeftMouseClick())
        {
            if (SelectedRoute != null)
            {
                if (SelectedRoute.Path.First() == clickedRoadNode
                 || SelectedRoute.Path.Last() == clickedRoadNode)
                {
                    IsEditingFromStart = SelectedRoute.Path.First() == clickedRoadNode;
                    StartRouteEdit(SelectedRoute, clickedRoadNode);
                    return;
                }
            }

            if (clickedRoadNode is BusStop && CurrentRouteCreationStep == NotCreating)
            {
                StartRouteCreation(clickedRoadNode);
            }
        }
        else if (@event is InputEventMouseMotion)
        {
            if (CurrentRouteCreationStep == AddingSubsequentStops || CurrentRouteCreationStep == EditingRoute)
            {
                ContinueRoute(clickedRoadNode);
            }
        }
    }

    /// <summary>
    /// Begins editing an existing route from the specified start node.
    /// </summary>
    /// <param name="route">The route to edit.</param>
    /// <param name="clickedNode">The node that was clicked </param>
    private void StartRouteEdit(Route route, RoadNode clickedNode)
    {
        GD.Print($"Starting to edit route: {route.ColorName}");
        CurrentRouteCreationStep = EditingRoute;
        _routeBackup = [.. route.Path];

        if (route.Path.First() == clickedNode)
        {
            IsEditingFromStart = true;
        }

        // Setup the preview line
        RoutePreviewLine = CreateLineAt(clickedNode.GlobalPosition);
        RoutePreviewLine.DefaultColor = route.Color;
        CurrentLevel.AddChild(RoutePreviewLine);
    }

    private void StartRouteCreation(RoadNode startNode)
    {
        GD.Print("Route creation started at: " + startNode.Name);
        CurrentRouteCreationStep = AddingSubsequentStops;

        _tempRoute = new Route();
        _tempRoute.AppendNode(startNode);

        _tempRoute.Visual = new RouteVisual(_tempRoute);
        CurrentLevel.AddChild(_tempRoute.Visual);

        RoutePreviewLine = CreateLineAt(startNode.GlobalPosition);
        RoutePreviewLine.DefaultColor = _tempRoute.Color;
        CurrentLevel.AddChild(RoutePreviewLine);
    }

    private void ContinueRoute(RoadNode nextNode)
    {
        Route routeToEdit = (CurrentRouteCreationStep == EditingRoute) ? SelectedRoute : _tempRoute;
        
        if (routeToEdit == null) return;

        RoadNode lastNode;
        if (IsEditingFromStart)
        {
            lastNode = routeToEdit.Path.FirstOrDefault();
        }
        else
        {
            lastNode = routeToEdit.Path.LastOrDefault();
        }

        if (lastNode == nextNode) // Prevent adding the same node twice
        {
            return;
        }

        if (IsEditingFromStart)
        {
            routeToEdit.PrependNode(nextNode);
            RoutePreviewLine.SetPointPosition(0, nextNode.GlobalPosition);
            RoutePreviewLine.AddPoint(nextNode.GlobalPosition, 0);
        }
        else
        {
            routeToEdit.AppendNode(nextNode);
            RoutePreviewLine.SetPointPosition(RoutePreviewLine.GetPointCount() - 1, nextNode.GlobalPosition);
            RoutePreviewLine.AddPoint(nextNode.GlobalPosition);
        }
    }

    private void FinalizeRoute()
    {
        if (CurrentRouteCreationStep == EditingRoute)
        {
            FinalizeRouteEdit();
        }
        else if (CurrentRouteCreationStep == AddingSubsequentStops)
        {
            FinalizeRouteCreation();
        }
    }

    private void FinalizeRouteCreation()
    {
        var lastNode = _tempRoute.Path[^1];
        if (_tempRoute.Path.Count < 2 || lastNode is not BusStop)
        {
            errorMessage.DisplayMessage("Route must start and end at a bus stop.");
            ReturnRouteColor(new KeyValuePair<string, Color>(_tempRoute.ColorName, _tempRoute.Color));
            _tempRoute.Visual?.QueueFree();
        }
        else
        {
            LevelState.AllRoutes.Add(_tempRoute);
            var routeList = GetTree().CurrentScene.GetNode<ItemList>(Path.RouteListNode);
            routeList.AddItem(_tempRoute.ColorName + " line");
            LevelState.UpdateAllHouseStatuses();
            LevelState.RefreshAllRouteVisuals();
        }
        foreach (var route in LevelState.AllRoutes)
        {
            GD.Print($"Route {route.ColorName}: {string.Join(" -> ", route.Path.Select(n => n.Name))}");
        }
        _tempRoute = null;
        ResetState();
    }

    private void FinalizeRouteEdit()
    {
        var editedRoute = SelectedRoute;
        var firstNode = editedRoute.Path.First();
        var lastNode = editedRoute.Path.Last();

        if (editedRoute.Path.Count < 2 || firstNode is not BusStop || lastNode is not BusStop)
        {
            GD.Print("Reverting to: " + string.Join(" -> ", _routeBackup.Select(n => n.Name)));
            editedRoute.SetPath(_routeBackup); // Revert to backup
        }
        else
        {
            GD.Print("Route edit successful.");
            UpdateAllHouseStatuses();
        }
        LevelState.RefreshAllRouteVisuals();
        _routeBackup = null;
        ResetState();
    }

    private void ResetState()
    {
        RoutePreviewLine?.QueueFree();
        RoutePreviewLine = null;
        CurrentRouteCreationStep = NotCreating;
        IsEditingFromStart = false;
    }
}