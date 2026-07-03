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
    private static Line2D _mouseTrackingLine { get; set; }
    public static Line2D MouseTrackingLine => _mouseTrackingLine;

    private static BusLine _busLineInProgress;

    /// <summary>
    /// Tracks which disjointed segment the user is actively drawing from.
    /// </summary>
    private static DraftSegment _activeSegment;

    private static void BeginMouseTrackingLineAt(RoadNode node, Color color)
    {
        _mouseTrackingLine = CreateLineAt(node.GlobalPosition);
        _mouseTrackingLine.DefaultColor = color;
        CurrentLevel.AddChild(_mouseTrackingLine);
    }

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

    public static void FinalizeBusLineCreation()
    {
        ErrorMessage errorMessage = CurrentLevel.GetNode<ErrorMessage>(ErrorMessageNode);

        if (_busLineInProgress.DraftLineSegments.Count > 1)
        {
            errorMessage.DisplayMessage("Connect all route segments before finalizing.");
            return; // We don't ResetState here so they can keep connecting them!
        }

        if (_busLineInProgress.DraftLineSegments.Count == 0 || _busLineInProgress.DraftLineSegments[0].Nodes.Count < 2)
        {
            errorMessage.DisplayMessage("BusLine must have at least 2 stops");
            CleanupFailedBusLine();
            return;
        }

        // Now safe to commit the unified segment to the final Path
        _busLineInProgress.CommitDraftToPath();

        RoadNode firstNode = _busLineInProgress.Path[0];
        RoadNode lastNode = _busLineInProgress.Path[^1];

        if (firstNode is not BusStop || lastNode is not BusStop)
        {
            errorMessage.DisplayMessage("BusLine must start and end at a bus stop");
            CleanupFailedBusLine();
        }
        else
        {
            LevelState.AllBusLines.Add(_busLineInProgress);
            var routeList = CurrentLevel.GetNode<ItemList>(BusLineListNode);
            routeList.AddItem(_busLineInProgress.ColorName + " line");
            UpdateAllHouseStatuses();
            RefreshAllBusLineVisuals();
            
            GD.Print("Final busLine: " + string.Join(", ", _busLineInProgress.Path.Select(node => node.Name)));
            ResetState();
        }
    }

    private static void CleanupFailedBusLine()
    {
        ReturnBusLineColor(new KeyValuePair<string, Color>(_busLineInProgress.ColorName, _busLineInProgress.Color));
        _busLineInProgress.QueueFree();
        ResetState();
    }

    public static void FinalizeDraftSegment()
    {
        if (_busLineInProgress == null) return;

        CurrentBusLineCreationStep = PausedCreation;
        _mouseTrackingLine?.QueueFree();
        _mouseTrackingLine = null;
        _activeSegment = null;
    }

    public static bool CanResumeBusLineCreation(RoadNode clickedNode)
    {
        if (_busLineInProgress == null || CurrentBusLineCreationStep != PausedCreation)
            return false;

        // Loop through ALL draft segments to see if we clicked an endpoint
        foreach (var segment in _busLineInProgress.DraftLineSegments)
        {
            if (clickedNode == segment.FirstNode)
            {
                _activeSegment = segment;
                IsEditingFromStart = true;
                CurrentBusLineCreationStep = AddingSubsequentStops;
                BeginMouseTrackingLineAt(clickedNode, _busLineInProgress.Color);
                return true;
            }
            else if (clickedNode == segment.LastNode)
            {
                _activeSegment = segment;
                IsEditingFromStart = false;
                CurrentBusLineCreationStep = AddingSubsequentStops;
                BeginMouseTrackingLineAt(clickedNode, _busLineInProgress.Color);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Starts a completely new disjointed segment while the editor is in a paused state.
    /// </summary>
    public static void BeginNewDisjointedSegment(RoadNode startNode)
    {
        if (_busLineInProgress == null) return;

        GD.Print($"[DEBUG - Editor] Jumping to start new segment at {startNode.Name}");
        
        // Re-enter the active drawing state
        CurrentBusLineCreationStep = AddingSubsequentStops;
        IsEditingFromStart = false; // Default to appending for a new segment

        _busLineInProgress.StartNewSegment(startNode);
        _activeSegment = _busLineInProgress.DraftLineSegments.Last();

        BeginMouseTrackingLineAt(startNode, _busLineInProgress.Color);
    }

    public static void StartBusLineEdit(BusLine busLine, RoadNode clickedNode)
    {
        GD.Print($"Starting to edit busLine: {busLine.ColorName}");
        _busLineInProgress = new BusLine();
        
        // Push the existing path into a draft segment so it can be edited
        _busLineInProgress.StartNewSegment(busLine.Path[0]);
        for (int i = 1; i < busLine.Path.Count; i++)
        {
            _busLineInProgress.AppendToSegment(busLine.Path[i], 0);
        }

        _activeSegment = _busLineInProgress.DraftLineSegments[0];

        if (_activeSegment.FirstNode == clickedNode)
            IsEditingFromStart = true;
        else
            IsEditingFromStart = false;

        BeginMouseTrackingLineAt(clickedNode, busLine.Color);
    }

    public static void FinalizeBusLineEdit()
    {
        var editedBusLine = SelectedBusLine;
        ErrorMessage errorMessage = CurrentLevel.GetNode<ErrorMessage>(ErrorMessageNode);

        if (_busLineInProgress.DraftLineSegments.Count > 1)
        {
            errorMessage.DisplayMessage("Connect all route segments before finalizing edit.");
            return; 
        }

        _busLineInProgress.CommitDraftToPath();

        var firstNode = _busLineInProgress.Path.First();
        var lastNode = _busLineInProgress.Path.Last();

        if (_busLineInProgress.Path.Count < 2)
        {
            errorMessage.DisplayMessage("BusLine must have at least 2 stops");
        }
        else if (firstNode is not BusStop || lastNode is not BusStop)
        {
            errorMessage.DisplayMessage("BusLine must start and end at a bus stop");
        }
        else
        {
            GD.Print($"BusLine edit successful. New path: {string.Join(", ", _busLineInProgress.Path.Select(node => node.Name))}.");
            SelectedBusLine.SetPath(_busLineInProgress.Path);
            UpdateAllHouseStatuses();
        }
        
        LevelState.RefreshAllBusLineVisuals();
        _busLineInProgress.QueueFree(); // Clean up the temporary editing duplicate
        _busLineInProgress = null;
        ResetState();
    }

    private static void ResetState()
    {
        CurrentLevel.GetNode<Label>(CreatingNewLineLabelNode).Visible = false;
        CurrentLevel.GetNode<Button>(EndBusLineButtonNode).Visible = false;
        _busLineInProgress = null;
        _activeSegment = null;
        _mouseTrackingLine?.QueueFree();
        _mouseTrackingLine = null;
        EditorState.ActiveTool = EditorTool.None;
        IsEditingFromStart = false;
    }

public static void StartBusLineCreation(RoadNode startNode)
    {
        GD.Print($"[DEBUG - Editor] --- StartBusLineCreation called at {startNode.Name} ---");
        CurrentBusLineCreationStep = AddingSubsequentStops;
        IsEditingFromStart = false; 
        _busLineInProgress = new BusLine();
        CurrentLevel.AddChild(_busLineInProgress);
        
        _busLineInProgress.StartNewSegment(startNode);
        _activeSegment = _busLineInProgress.DraftLineSegments.Last();

        BeginMouseTrackingLineAt(startNode, _busLineInProgress.Color);
    }

    public static void ContinueBusLineCreation(RoadNode nextNode)
    {
        GD.Print($"[DEBUG - Editor] ContinueBusLineCreation called on {nextNode.Name}");
        
        if (_activeSegment == null)
        {
            GD.PrintErr("[DEBUG - Editor] _activeSegment is NULL! Cannot continue.");
            return;
        }

        var activeNode = IsEditingFromStart ? _activeSegment.FirstNode : _activeSegment.LastNode;
        GD.Print($"[DEBUG - Editor] Active endpoint is {activeNode.Name}. IsEditingFromStart: {IsEditingFromStart}");
        
        if (activeNode == nextNode)
        {
            GD.Print("[DEBUG - Editor] Ignored: Clicked the same node we are already on.");
            return;
        }

        bool isNeighbor = activeNode.Neighbors.Contains(nextNode);
        GD.Print($"[DEBUG - Editor] Neighbor check: Is {nextNode.Name} a neighbor of {activeNode.Name}? {isNeighbor}");

        if (!isNeighbor)
        {
            GD.Print("[DEBUG - Editor] JUMPING: Node is not a neighbor. Pausing current segment and starting a new one.");
            FinalizeDraftSegment(); 
            
            _busLineInProgress.StartNewSegment(nextNode);
            _activeSegment = _busLineInProgress.DraftLineSegments.Last();
            IsEditingFromStart = false;
            
            CurrentBusLineCreationStep = AddingSubsequentStops;
            BeginMouseTrackingLineAt(nextNode, _busLineInProgress.Color);
            return;
        }

        GD.Print("[DEBUG - Editor] CONTINUOUS: Node is a neighbor. Appending/Prepending...");
        if (IsEditingFromStart)
            _activeSegment.Prepend(nextNode);
        else
            _activeSegment.Append(nextNode);

        CheckAndMergeSegments();

        _mouseTrackingLine.SetPointPosition(_mouseTrackingLine.GetPointCount() - 1, nextNode.GlobalPosition);
        _mouseTrackingLine.AddPoint(nextNode.GlobalPosition);
    }

    private static void CheckAndMergeSegments()
    {
        var segments = _busLineInProgress.DraftLineSegments;
        
        for (int i = 0; i < segments.Count; i++)
        {
            for (int j = i + 1; j < segments.Count; j++)
            {
                if (segments[i].CanMergeWith(segments[j]))
                {
                    GD.Print($"[DEBUG - Editor] Collision detected between segment {i} and {j}!");
                    
                    var segmentToAbsorb = segments[j];
                    bool wasActive = (_activeSegment == segmentToAbsorb);
                    
                    segments[i].MergeWith(segmentToAbsorb);
                    
                    segmentToAbsorb.QueueFree();
                    segments.RemoveAt(j);
                    
                    if (wasActive) 
                    {
                        _activeSegment = segments[i];
                    }
                    
                    return; 
                }
            }
        }
    }
}