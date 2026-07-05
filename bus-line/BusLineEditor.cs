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

    /// <summary>
    /// The bus line that is currently being created or edited.
    /// </summary>
    private static BusLine _busLineInProgress;
    public static BusLine BusLineInProgress => _busLineInProgress;

    /// <summary>
    /// Tracks which disjointed segment the user is actively drawing from.
    /// </summary>
    private static DraftSegment _activeSegment;

    /// <summary>
    /// Tracks all the (disjointed) segments that make up the bus line currently
    /// being created or edited. Each segment is a temporary representation of
    /// a portion of the bus line, allowing for flexible editing and
    /// visualization before finalizing the route.
    /// </summary>
    public static List<DraftSegment> DraftLineSegments { get; private set; } = [];
    private static bool IsEditingFromStart;
    private static Label _creatingNewLineLabel;

    /// <summary>
    /// Obtains the road node to begin the the mouse tracking from, and what
    /// color the line should be drawn in.
    /// </summary>
    /// <param name="node">Starting node for the mouse tracking line</param>
    /// <param name="color">Color for the mouse tracking line</param>
    private static void BeginMouseTrackingLineAt(RoadNode node, Color color)
    {
        _mouseTrackingLine = CreateLineAt(node.GlobalPosition);
        _mouseTrackingLine.DefaultColor = color;
        CurrentLevel.AddChild(_mouseTrackingLine);
    }

    /// <summary>
    /// Draws a line that follows the user's cursor, providing visual feedback
    /// during the bus line creation process. This method is called every
    /// frame while the user is actively creating a bus line, allowing them to
    /// see where the next segment will be drawn as they move the mouse.
    /// <param name="mousePosition">The current position of the mouse in the game world.</param>
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

    /// <summary>
    /// Starts a new draft segment for the bus line creation process,
    /// beginning at the specified start node.This method is called when the
    /// user clicks on a road node.
    /// </summary>
    /// <param name="startNode">Node to begin the segment at</param>
    public static void StartNewSegment(RoadNode startNode)
    {
        if (startNode == null || _busLineInProgress == null) return;
        
        var newSegment = new DraftSegment(startNode, _busLineInProgress);
        _busLineInProgress.AddChild(newSegment); // Keeps it grouped visually under the bus line node
        
        DraftLineSegments.Add(newSegment);
        _activeSegment = newSegment;
    }

    /// <summary>
    /// Continues the bus line creation process by adding a new node to the
    /// currently active draft segment.
    /// </summary>
    /// <param name="nextNode">The next node to add to the active segment.</param>
    public static void ContinueBusLineCreation(RoadNode nextNode)
    {
        if (_activeSegment == null) return;

        var activeNode = IsEditingFromStart ? _activeSegment.FirstNode : _activeSegment.LastNode;
        if (activeNode == nextNode) return;

        if (!activeNode.Neighbors.Contains(nextNode))
        {
            // Handle Jump / Disjointed segment break
            FinalizeDraftSegment(); 
            StartNewSegment(nextNode);
            IsEditingFromStart = false;
            return;
        }

        if (IsEditingFromStart)
            _activeSegment.Prepend(nextNode);
        else
            _activeSegment.Append(nextNode);

        CheckAndMergeSegments();
    }

    /// <summary>
    /// Finalizes the bus line creation process, performing validation checks,
    /// either committing the new bus line or cleaning up if validation fails.
    /// This method is called when the user clicks the "End Bus Line" button.
    /// </summary>
    public static void FinalizeBusLineCreation()
    {
        ErrorMessage errorMessage = CurrentLevel.GetNode<ErrorMessage>(ErrorMessageNode);

        if (DraftLineSegments.Count > 1)
        {
            errorMessage.DisplayMessage("Connect all route segments before finalizing.");
            return;
        }

        if (DraftLineSegments.Count == 0 || DraftLineSegments[0].Nodes.Count < 2)
        {
            errorMessage.DisplayMessage("BusLine must have at least 2 stops");
            CleanupFailedBusLine();
            return;
        }

        _busLineInProgress.SetPath(DraftLineSegments[0].Nodes);

        DraftLineSegments[0].QueueFree();
        DraftLineSegments.Clear();

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
            
            ResetState();
        }
    }

    /// <summary>
    /// Cleans up the current bus line in progress if the creation process
    /// fails due to validation errors (e.g., not enough stops, not
    /// starting/ending at bus stops). This method returns the bus line's color
    /// to the pool and frees the temporary bus line.
    /// </summary>
    private static void CleanupFailedBusLine()
    {
        var busLineInProgressColor = new KeyValuePair<string, Color>
        (
            _busLineInProgress.ColorName,
            _busLineInProgress.Color
        );
        ReturnBusLineColor(busLineInProgressColor);
        _busLineInProgress.QueueFree();
        ResetState();
    }

    /// <summary>
    /// Finalizes the current draft segment, removing the mouse tracking line
    /// and pauses bus line creation until the user clicks on a node to resume.
    /// This method is called when the user completes a segment.
    /// </summary>
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
        foreach (var segment in DraftLineSegments)
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

        StartNewSegment(startNode);
        _activeSegment = DraftLineSegments.Last();

        BeginMouseTrackingLineAt(startNode, _busLineInProgress.Color);
    }

    public static void StartBusLineEdit(BusLine busLine, RoadNode clickedNode)
    {
        GD.Print($"Starting to edit busLine: {busLine.ColorName}");
        _busLineInProgress = new BusLine();
        
        // Push the existing path into a draft segment so it can be edited
        StartNewSegment(busLine.Path[0]);
        for (int i = 1; i < busLine.Path.Count; i++)
        {
            // _busLineInProgress.AppendToSegment(busLine.Path[i], 0);
        }

        _activeSegment = DraftLineSegments[0];

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

        if (DraftLineSegments.Count > 1)
        {
            errorMessage.DisplayMessage("Connect all route segments before finalizing edit.");
            return; 
        }

        // _busLineInProgress.CommitDraftToPath();

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

    /// <summary>
    /// Resets the editor state to its default values, clearing any temporary
    /// data and hiding UI elements related to bus line creation or editing.
    /// This method is called after a bus line creation or edit process is
    /// completed or canceled.
    /// </summary>
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

    /// <summary>
    /// Begins a new bus line creation process starting from the specified node.
    /// This method initializes the necessary state and visual elements for the user to start defining a new bus route.
    /// </summary>
    /// <param name="startNode">The node from which to start the bus line creation.</param>
    public static void StartBusLineCreation(RoadNode startNode)
    {
        _creatingNewLineLabel = CurrentLevel.GetNode<Label>(CreatingNewLineLabelNode);
        CurrentBusLineCreationStep = AddingSubsequentStops;
        IsEditingFromStart = false; 
        _busLineInProgress = new BusLine();
        CurrentLevel.AddChild(_busLineInProgress);
        
        _creatingNewLineLabel.Text = $"● Creating {_busLineInProgress.ColorName} Line";

        StartNewSegment(startNode);
        _activeSegment = DraftLineSegments.Last();

        BeginMouseTrackingLineAt(startNode, _busLineInProgress.Color);
    }

    /// <summary>
    /// Checks all draft segments to see if any can be merged together.
    /// </summary>
    private static void CheckAndMergeSegments()
    {
        var segments = DraftLineSegments;
        
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