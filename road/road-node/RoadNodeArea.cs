using Godot;
using System.Collections.Generic;
using System.Linq;
using static EditorState;
using static LevelState;
using static RouteCreationStep;
using static Path;
using static RouteEditor;

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

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsLeftMouseRelease())
            if (CurrentRouteCreationStep == AddingSubsequentStops)
                RouteEditor.FinalizeRouteCreation();
            else if (CurrentRouteCreationStep == ContinuingEdit)
                RouteEditor.FinalizeRouteEdit();
    }

    private void _on_input_event(Node viewport, InputEvent @event, long shapeIdx)
    {
        var selectedRoadNode = GetParent<RoadNode>();

        if (@event.IsLeftMouseClick())
        {
            if (selectedRoadNode is BusStop && CurrentRouteCreationStep == NotCreating)
            {
                RouteEditor.StartRouteCreation(selectedRoadNode);
            }
            if (CurrentRouteCreationStep == BeginningEdit)
            {
                GD.Print($"Clicked on node: {selectedRoadNode.Name} during route edit. {SelectedRoute.ColorName}");
                if (SelectedRoute.Path.First() == selectedRoadNode
                 || SelectedRoute.Path.Last() == selectedRoadNode)
                {
                    IsEditingFromStart = SelectedRoute.Path.First() == selectedRoadNode;
                    RouteEditor.StartRouteEdit(SelectedRoute, selectedRoadNode);
                    GD.Print($"Starting edit for: {SelectedRoute.ColorName}");
                    CurrentRouteCreationStep = ContinuingEdit;
                    return;
                }
            }
        }
        else if (@event is InputEventMouseMotion)
        {
            if (CurrentRouteCreationStep == AddingSubsequentStops || CurrentRouteCreationStep == ContinuingEdit)
            {
                RouteEditor.ContinueRouteCreation(selectedRoadNode);
            }
        }
    }
}