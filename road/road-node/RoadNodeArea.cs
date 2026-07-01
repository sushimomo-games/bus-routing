using Godot;
using System.Collections.Generic;
using System.Linq;
using static EditorState;
using static LevelState;
using static BusLineCreationStep;
using static Path;
using static BusLineEditor;

public partial class RoadNodeArea : Area2D
{
    private BusLine _tempBusLine; // Used during busLine creation before the busLine is finalized.

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
            if (CurrentBusLineCreationStep == AddingSubsequentStops)
                BusLineEditor.FinalizeDraftSegment();
            else if (CurrentBusLineCreationStep == ContinuingEdit)
                BusLineEditor.FinalizeBusLineEdit();
    }

    private void _on_input_event(Node viewport, InputEvent @event, long shapeIdx)
    {
        var selectedRoadNode = GetParent<RoadNode>();

        if (@event.IsLeftMouseClick())
        {
            if (CurrentBusLineCreationStep == PausedCreation)
            {
                if (BusLineEditor.CanResumeBusLineCreation(selectedRoadNode))
                    return;
            }

            if (selectedRoadNode is BusStop && CurrentBusLineCreationStep == AddingFirstStop)
            {
                BusLineEditor.StartBusLineCreation(selectedRoadNode);
            }
            if (CurrentBusLineCreationStep == BeginningEdit)
            {
                GD.Print($"Clicked on node: {selectedRoadNode.Name} during busLine edit. {SelectedBusLine.ColorName}");
                if (SelectedBusLine.Path.First() == selectedRoadNode
                 || SelectedBusLine.Path.Last() == selectedRoadNode)
                {
                    IsEditingFromStart = SelectedBusLine.Path.First() == selectedRoadNode;
                    BusLineEditor.StartBusLineEdit(SelectedBusLine, selectedRoadNode);
                    GD.Print($"Starting edit for: {SelectedBusLine.ColorName}");
                    CurrentBusLineCreationStep = ContinuingEdit;
                    return;
                }
            }
        }
        else if (@event is InputEventMouseMotion)
        {
            if (CurrentBusLineCreationStep == AddingSubsequentStops || CurrentBusLineCreationStep == ContinuingEdit)
            {
                BusLineEditor.ContinueBusLineCreation(selectedRoadNode);
            }
        }
    }
}