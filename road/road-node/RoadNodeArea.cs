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
    private Route _tempRoute; // Used during route creation before the route is finalized.
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
            RouteEditor.FinalizeRouteCreation();
        }
    }

    private void _on_input_event(Node viewport, InputEvent @event, long shapeIdx)
    {
        var selectedRoadNode = GetParent<RoadNode>();

        if (@event.IsLeftMouseClick())
        {
        //     if (SelectedRoute != null)
        //     {
        //         if (SelectedRoute.Path.First() == clickedRoadNode
        //          || SelectedRoute.Path.Last() == clickedRoadNode)
        //         {
        //             IsEditingFromStart = SelectedRoute.Path.First() == clickedRoadNode;
        //             StartRouteEdit(SelectedRoute, clickedRoadNode);
        //             return;
        //         }
        //     }

            if (selectedRoadNode is BusStop && CurrentRouteCreationStep == NotCreating)
            {
                // StartRouteCreation(selectedRoadNode);
                RouteEditor.StartRouteCreation(selectedRoadNode);
            }
        }
        else if (@event is InputEventMouseMotion)
        {
            if (CurrentRouteCreationStep == AddingSubsequentStops || CurrentRouteCreationStep == EditingRoute)
            {
                RouteEditor.ContinueRoute(selectedRoadNode);
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
        // ResetState();
    }
}