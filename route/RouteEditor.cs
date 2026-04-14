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
    /// <summary>
    /// The temporary line segment that follows the player's cursor during
    /// route creation.
    /// </summary>
    private static Line2D _mouseTrackingLine { get; set; }
    public static Line2D MouseTrackingLine => _mouseTrackingLine;

    /// <summary>
    /// This exists so that we do not need to add a Route to LevelState until
    /// it's fully created and valid.
    /// </summary>
    private static Route _routeInProgress;

    /// <summary>
    /// Draws the Line2D segment that follows the player's cursor during
    /// route creation/editing.
    /// </summary>
    /// <param name="mousePosition">The position of the mouse cursor.</param>
    public static void DrawMouseTrackingLine(Vector2 mousePosition)
    {
        if (MouseTrackingLine == null)
            return;

        if (MouseTrackingLine.GetPointCount() < 2)
            MouseTrackingLine.AddPoint(mousePosition);
        else
            MouseTrackingLine.SetPointPosition
            (
                MouseTrackingLine.GetPointCount() - 1, mousePosition
            );
    }

    public static void StartRouteCreation(RoadNode startNode)
    {
        CurrentRouteCreationStep = AddingSubsequentStops;
        _routeInProgress = new Route();
        CurrentLevel.AddChild(_routeInProgress);
        _routeInProgress.AppendNode(startNode);

        _mouseTrackingLine = CreateLineAt(startNode.GlobalPosition);
        _mouseTrackingLine.DefaultColor = _routeInProgress.Color;
        CurrentLevel.AddChild(_mouseTrackingLine);
    }

    public static void ContinueRouteCreation(RoadNode nextNode)
    {
        if (_routeInProgress.Path.Last() != nextNode)
            _routeInProgress.AppendNode(nextNode);
        _mouseTrackingLine.SetPointPosition(_mouseTrackingLine.GetPointCount() - 1, nextNode.GlobalPosition);
        _mouseTrackingLine.AddPoint(nextNode.GlobalPosition);
    }

    public static void FinalizeRouteCreation()
    {
        RoadNode lastNode = _routeInProgress.Path[^1];
        ErrorMessage errorMessage = CurrentLevel.GetNode<ErrorMessage>(ErrorMessageNode);

        if (_routeInProgress.Path.Count < 2)
        {
            errorMessage.DisplayMessage("Route must have at least 2 stops");
        }
        if (lastNode is not BusStop)
        {
            errorMessage.DisplayMessage("Route must start and end at a bus stop");
            ReturnRouteColor(new KeyValuePair<string, Color>(_routeInProgress.ColorName, _routeInProgress.Color));
            _routeInProgress.QueueFree();
        }
        else
        {
            LevelState.AllRoutes.Add(_routeInProgress);
            var routeList = CurrentLevel.GetNode<ItemList>(RouteListNode);
            routeList.AddItem(_routeInProgress.ColorName + " line");
            UpdateAllHouseStatuses();
            RefreshAllRouteVisuals();
        }
        GD.Print("Final route: " + string.Join(", ", _routeInProgress.Path.Select(node => node.Name)));
        ResetState();
    }

    /// <summary>
    /// Begins editing an existing route from the specified start node.
    /// </summary>
    /// <param name="route">The route to edit.</param>
    /// <param name="clickedNode">The node that was clicked </param>
    public static void StartRouteEdit(Route route, RoadNode clickedNode)
    {
        GD.Print($"Starting to edit route: {route.ColorName}");
        _routeInProgress = new Route();
        foreach (RoadNode node in route.Path)
        {
            _routeInProgress.AppendNode(node);
        }

        if (route.Path.First() == clickedNode)
        {
            IsEditingFromStart = true;
        }

        // Setup the preview line
        _mouseTrackingLine = CreateLineAt(clickedNode.GlobalPosition);
        _mouseTrackingLine.DefaultColor = route.Color;
        CurrentLevel.AddChild(_mouseTrackingLine);
    }

    public static void FinalizeRouteEdit()
    {
        var editedRoute = SelectedRoute;
        // var firstNode = editedRoute.Path.First();
        // var lastNode = editedRoute.Path.Last();

        // if (editedRoute.Path.Count < 2 || firstNode is not BusStop || lastNode is not BusStop)
        // {
        //     editedRoute.SetPath(_routeInProgress.Path); // Revert to backup
        // }
        // else
        {
            GD.Print($"Route edit successful. New path: {string.Join(", ", _routeInProgress.Path.Select(node => node.Name))}");
            SelectedRoute.SetPath(_routeInProgress.Path);
            UpdateAllHouseStatuses();
        }
        LevelState.RefreshAllRouteVisuals();
        _routeInProgress = null;
        ResetState();
    }

    /// <summary>
    /// Resets all static state related to route creation and editing. Should
    /// be be called after a route creation or edit process is completed.
    /// </summary>
    private static void ResetState()
    {
        _routeInProgress = null;
        _mouseTrackingLine?.QueueFree();
        _mouseTrackingLine = null;
        CurrentRouteCreationStep = NotCreating;
        IsEditingFromStart = false;
    }
}