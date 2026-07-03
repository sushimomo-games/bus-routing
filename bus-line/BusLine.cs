using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static LevelState;

/// <summary>
/// Represents a busLine consisting of a sequence of bus stops and its visual
/// representation.
/// </summary>
public partial class BusLine : Node
{
    /// <summary>
    /// A static counter to ensure every new bus line gets a unique ID.
    /// </summary>
    private static uint _nextID = 1;

    private uint _ID;
    public uint ID
    { 
        get => _ID;
        private set => _ID = value;
    }

    private float _timeToComplete;

    /// <summary>
    /// The estimated minutes it takes to complete the bus line,
    /// calculated as the sum of distances
    /// between consecutive nodes. 
    /// </summary>
    public float TimeToComplete
    {
        get
        {
            float total = 0f;
            for (int i = 0; i < Path.Count - 1; i++)
            {
                total += Path[i].GlobalPosition.DistanceTo(Path[i + 1].GlobalPosition);
            }
            return total;
        }
    }

    /// <summary>
    /// List of bus stops and intersection nodes that make up the bus line.
    /// </summary>
    public List<RoadNode> Path { get; set; }

    /// <summary>
    /// Potentially disjointed line segments used exclusively during the drafting phase.
    /// </summary>
    public List<DraftSegment> DraftLineSegments { get; set; } = [];

    /// <summary>
    /// The name of the color assigned to this busLine opposed to the hex value.
    /// </summary>
    public string ColorName { get; private set; }

    /// <summary>
    /// The color assigned to this bus line. Set by hex value or Godot Color constants.
    /// </summary>
    public Color Color { get; private set; }

    /// <summary>
    /// The visual representation of this bus line.
    /// </summary>
    public BusLineVisual Visual { get; private set; }

    /// <summary>
    /// Fired when the bus line's path has been changed.
    /// </summary>
    public event Action OnPathChanged;

    /// <summary>
    /// Fired when the bus line is deleted.
    /// </summary>
    public event Action OnDeleted;

    /// <summary>
    /// Appends a new node to the end of the busLine's path and visual line.
    /// </summary>
    /// <param name="node">The Node2D to add to the path.</param>
    public void AppendNode(RoadNode node)
    {
        if (node == null) return;

        Path.Add(node);
        Visual?.AppendPoint(node.GlobalPosition);
        OnPathChanged?.Invoke();
    }

    /// <summary>
    /// Inserts a new node at the beginning of the busLine's path and visual line.
    /// </summary>
    /// <param name="node">The RoadNode to add to the path.</param>
    public void PrependNode(RoadNode node)
    {
        if (node == null) return;

        Path.Insert(0, node);
        Visual?.PrependPoint(node.GlobalPosition);
        OnPathChanged?.Invoke();
    }

    /// <summary>
    /// Removes a node from the busLine's path and updates the visual line.
    /// </summary>
    /// <param name="node">The RoadNode to remove from the path.</param>
    public void RemoveNode(RoadNode node)
    {
        if (ContainsNode(node))
        {
            var newPath = Path.Where(n => n != node).ToList();
            SetPath(newPath);
        }
    }

    /// <summary>
    /// Clears all nodes from the busLine's path and its visual line.
    /// </summary>
    public void ClearPath()
    {
        Path.Clear();
        
        // Ensure we wipe out any temporary drafting visuals
        foreach (var segment in DraftLineSegments)
        {
            segment.QueueFree(); 
        }
        DraftLineSegments.Clear();
        
        Visual?.ClearPoints();
        OnPathChanged?.Invoke();
    }

    /// <summary>
    /// Sets the busLine's path to a new list of nodes, updating the visual line.
    /// </summary>
    public void SetPath(List<RoadNode> newPath)
    {
        Path.Clear();
        Visual?.ClearPoints();
        foreach (var node in newPath)
        {
            Path.Add(node);
            Visual?.AppendPoint(node.GlobalPosition);
        }
        OnPathChanged?.Invoke();
    }
    
    public bool ContainsNode(RoadNode node)
    {
        return Path.Contains(node);
    }

    /// <summary>
    /// Automatically assigns a unique ID initializes the path list, and
    /// assigns a color.
    /// </summary>
    public BusLine()
    {
        ID = _nextID++;
        Path = [];
        var colorInfo = LevelState.GetNextBusLineColor();
        if (colorInfo.HasValue)
        {
            ColorName = colorInfo.Value.Key;
            Color = colorInfo.Value.Value;
        }
        else
        {
            // Fallback if no colors are left. TODO: make it so players cannot
            // create more busLines.
            ColorName = "Default";
            Color = Colors.White;
        }

        Visual = new BusLineVisual(this);
        AddChild(Visual);
    }

    /// <summary>
    /// Frees visual representation of BusLine and the BusLine itself.
    /// This is needed because _ExitTree does not work when QueueFreeing the
    /// node.
    /// </summary>
    public void Delete()
    {
        LevelState.AllBusLines.Remove(this);
        LevelState.ReturnBusLineColor(new KeyValuePair<string, Color>(ColorName, Color));
        Visual?.QueueFree();
        UpdateAllHouseStatuses(); 
        
        OnDeleted?.Invoke();
        
        QueueFree();
    }

    public void StartNewSegment(RoadNode startNode)
    {
        if (startNode == null) return;
        
        GD.Print($"[DEBUG - BusLine] Starting new segment at {startNode.Name}");
        
        var newSegment = new DraftSegment(startNode);
        
        // UNCOMMENTED: We must add this to the tree for QueueFree to work later!
        AddChild(newSegment); 
        
        DraftLineSegments.Add(newSegment);
        OnPathChanged?.Invoke();
        
        GD.Print($"[DEBUG - BusLine] DraftLineSegments count is now {DraftLineSegments.Count}");
    }
    
    public void CommitDraftToPath()
    {
        GD.Print($"[DEBUG - BusLine] Attempting to commit {DraftLineSegments.Count} segments to Path.");
        if (DraftLineSegments.Count == 1)
        {
            Path = new List<RoadNode>(DraftLineSegments[0].Nodes);
            DraftLineSegments[0].QueueFree();
            DraftLineSegments.Clear();
            Visual?.UpdateVisual();
            OnPathChanged?.Invoke();
            GD.Print("[DEBUG - BusLine] Successfully committed draft to path.");
        }
        else
        {
            GD.PrintErr($"[DEBUG - BusLine] Cannot commit: Route contains {DraftLineSegments.Count} disjointed segments.");
        }
    }

    /// <summary>
    /// Appends a node to a specific segment (defaults to the most recently created one).
    /// </summary>
    public void AppendToSegment(RoadNode node, int segmentIndex = -1)
    {
        if (node == null || DraftLineSegments.Count == 0) return;
        
        int index = segmentIndex < 0 ? DraftLineSegments.Count - 1 : segmentIndex;
        DraftLineSegments[index].Append(node);
        
        OnPathChanged?.Invoke();
    }

    /// <summary>
    /// Prepends a node to a specific segment (defaults to the most recently created one).
    /// </summary>
    public void PrependToSegment(RoadNode node, int segmentIndex = -1)
    {
        if (node == null || DraftLineSegments.Count == 0) return;

        int index = segmentIndex < 0 ? DraftLineSegments.Count - 1 : segmentIndex;
        DraftLineSegments[index].Prepend(node);
        
        OnPathChanged?.Invoke();
    }
}