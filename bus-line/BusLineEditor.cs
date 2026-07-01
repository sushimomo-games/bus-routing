using Godot;
using System.Collections.Generic;
using System.Linq;
using static EditorState;
using static LevelState;
using static LineFactory;
using static Path;
using static BusLineCreationStep;

public partial class BusLineEditor : Node
{
    /// <summary>
    /// The temporary line segment that follows the player's cursor during
    /// busLine creation.
    /// </summary>
    private static Line2D _mouseTrackingLine { get; set; }
    public static Line2D MouseTrackingLine => _mouseTrackingLine;

    /// <summary>
    /// This exists so that we do not need to add a BusLine to LevelState until
    /// it's fully created and valid.
    /// </summary>
    private static BusLine _busLineInProgress;

    private static void BeginMouseTrackingLineAt(RoadNode node, Color color)
    {
        _mouseTrackingLine = CreateLineAt(node.GlobalPosition);
        _mouseTrackingLine.DefaultColor = color;
        CurrentLevel.AddChild(_mouseTrackingLine);
    }

    /// <summary>
    /// Draws the Line2D segment that follows the player's cursor during
    /// busLine creation/editing.
    /// </summary>
    /// <param name="mousePosition">The position of the mouse cursor.</param>
    public static void DrawMouseTrackingLine(Vector2 mousePosition)
    {
        if (CurrentBusLineCreationStep != AddingSubsequentStops && CurrentBusLineCreationStep != ContinuingEdit)
            return;

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

    public static void StartBusLineCreation(RoadNode startNode)
    {
        CurrentBusLineCreationStep = AddingSubsequentStops;
        _busLineInProgress = new BusLine();
        CurrentLevel.AddChild(_busLineInProgress);
        _busLineInProgress.AppendNode(startNode);

        BeginMouseTrackingLineAt(startNode, _busLineInProgress.Color);
    }

    public static void ContinueBusLineCreation(RoadNode nextNode)
    {
        var lastNode = _busLineInProgress.Path.Last();
        if (lastNode == nextNode)
            return;
        if (!lastNode.Neighbors.Contains(nextNode)) // should we be able to skip bus stops?
            return;

        _busLineInProgress.AppendNode(nextNode);
        _mouseTrackingLine.SetPointPosition(_mouseTrackingLine.GetPointCount() - 1, nextNode.GlobalPosition);
        _mouseTrackingLine.AddPoint(nextNode.GlobalPosition);
    }

    public static void FinalizeBusLineCreation()
    {
        RoadNode firstNode = _busLineInProgress.Path[0];
        RoadNode lastNode = _busLineInProgress.Path[^1];
        ErrorMessage errorMessage = CurrentLevel.GetNode<ErrorMessage>(ErrorMessageNode);

        if (_busLineInProgress.Path.Count < 2)
        {
            errorMessage.DisplayMessage("BusLine must have at least 2 stops");
            ResetState();
            return;
        }
        if (firstNode is not BusStop || lastNode is not BusStop)
        {
            errorMessage.DisplayMessage("BusLine must start and end at a bus stop");
            ReturnBusLineColor(new KeyValuePair<string, Color>(_busLineInProgress.ColorName, _busLineInProgress.Color));
            _busLineInProgress.QueueFree();
        }
        else if (!HasContinuousPath(_busLineInProgress.Path))
        {
            errorMessage.DisplayMessage("BusLine must be made up of continuous segments");
            ReturnBusLineColor(new KeyValuePair<string, Color>(_busLineInProgress.ColorName, _busLineInProgress.Color));
            _busLineInProgress.QueueFree();
        }
        else
        {
            LevelState.AllBusLines.Add(_busLineInProgress);
            var routeList = CurrentLevel.GetNode<ItemList>(BusLineListNode);
            routeList.AddItem(_busLineInProgress.ColorName + " line");
            UpdateAllHouseStatuses();
            RefreshAllBusLineVisuals();
        }
        GD.Print("Final busLine: " + string.Join(", ", _busLineInProgress.Path.Select(node => node.Name)));
        ResetState();
    }

    /// <summary>
    /// 
    /// </summary>
    public static void FinalizeDraftSegment()
    {
        if (_busLineInProgress == null)
            return;

        CurrentBusLineCreationStep = PausedCreation;
        _mouseTrackingLine?.QueueFree();
        _mouseTrackingLine = null;
        GD.Print("Paused draft segment: " + string.Join(", ", _busLineInProgress.Path.Select(node => node.Name)));
    }

    public static bool ResumeBusLineCreation(RoadNode clickedNode)
    {
        if (_busLineInProgress == null || CurrentBusLineCreationStep != PausedCreation)
            return false;

        var lastNode = _busLineInProgress.Path.Last();
        if (lastNode != clickedNode)
            return false;

        CurrentBusLineCreationStep = AddingSubsequentStops;
        BeginMouseTrackingLineAt(clickedNode, _busLineInProgress.Color);
        return true;
    }

    /// <summary>
    /// Begins editing an existing busLine from the specified start node.
    /// </summary>
    /// <param name="busLine">The busLine to edit.</param>
    /// <param name="clickedNode">The node that was clicked </param>
    public static void StartBusLineEdit(BusLine busLine, RoadNode clickedNode)
    {
        GD.Print($"Starting to edit busLine: {busLine.ColorName}");
        _busLineInProgress = new BusLine();
        foreach (RoadNode node in busLine.Path)
        {
            _busLineInProgress.AppendNode(node);
        }

        if (busLine.Path.First() == clickedNode)
        {
            IsEditingFromStart = true;
        }

        BeginMouseTrackingLineAt(clickedNode, busLine.Color);
    }

    public static void FinalizeBusLineEdit()
    {
        var editedBusLine = SelectedBusLine;
        var firstNode = _busLineInProgress.Path.First();
        var lastNode = _busLineInProgress.Path.Last();
        ErrorMessage errorMessage = CurrentLevel.GetNode<ErrorMessage>(ErrorMessageNode);

        if (_busLineInProgress.Path.Count < 2)
        {
            errorMessage.DisplayMessage("BusLine must have at least 2 stops");
        }
        else if (firstNode is not BusStop || lastNode is not BusStop)
        {
            errorMessage.DisplayMessage("BusLine must start and end at a bus stop");
        }
        else if (!HasContinuousPath(_busLineInProgress.Path))
        {
            errorMessage.DisplayMessage("BusLine must be made up of continuous segments");
        }
        else
        {
            GD.Print($"BusLine edit successful. New path: {string.Join(", ", _busLineInProgress.Path.Select(node => node.Name))}.");
            SelectedBusLine.SetPath(_busLineInProgress.Path);
            UpdateAllHouseStatuses();
        }
        LevelState.RefreshAllBusLineVisuals();
        _busLineInProgress = null;
        ResetState();
    }

    /// <summary>
    /// Resets all static state related to busLine creation and editing. Should
    /// be be called after a busLine creation or edit process is completed.
    /// </summary>
    private static void ResetState()
    {
        CurrentLevel.GetNode<Label>(CreatingNewLineLabelNode).Visible = false;
        CurrentLevel.GetNode<Button>(EndBusLineButtonNode).Visible = false;
        _busLineInProgress = null;
        _mouseTrackingLine?.QueueFree();
        _mouseTrackingLine = null;
        EditorState.ActiveTool = EditorTool.None;
        IsEditingFromStart = false;
    }

    private static bool HasContinuousPath(List<RoadNode> path)
    {
        if (path == null || path.Count < 2)
            return false;

        for (int i = 0; i < path.Count - 1; i++)
        {
            if (!path[i].Neighbors.Contains(path[i + 1]))
                return false;
        }

        return true;
    }
}