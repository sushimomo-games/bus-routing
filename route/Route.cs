using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a route consisting of a sequence of bus stops and its visual
/// representation.
/// </summary>
public partial class Route : Node
{
    /// <summary>
    /// A static counter to ensure every new route gets a unique ID.
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
    /// The estimated minutes it takes to complete the route,
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
    /// List of bus stops and intersection nodes that make up the route.
    /// </summary>
    public List<RoadNode> Path { get; set; }

    /// <summary>
    /// The name of the color assigned to this route opposed to the hex value.
    /// </summary>
    public string ColorName { get; private set; }

    /// <summary>
    /// The color assigned to this route. Set by hex value or Godot Color constants.
    /// </summary>
    public Color Color { get; private set; }

    /// <summary>
    /// The visual representation of this route.
    /// </summary>
    public RouteVisual Visual { get; set; }

    /// <summary>
    /// Appends a new node to the end of the route's path and visual line.
    /// </summary>
    /// <param name="node">The Node2D to add to the path.</param>
    public void AppendNode(RoadNode node)
    {
        if (node == null) return;

        Path.Add(node);
        Visual?.AppendPoint(node.GlobalPosition);
    }

    /// <summary>
    /// Inserts a new node at the beginning of the route's path and visual line.
    /// </summary>
    /// <param name="node">The RoadNode to add to the path.</param>
    public void PrependNode(RoadNode node)
    {
        if (node == null) return;

        Path.Insert(0, node);
        Visual?.PrependPoint(node.GlobalPosition);
    }

    /// <summary>
    /// Removes a node from the route's path and updates the visual line.
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
    /// Clears all nodes from the route's path and its visual line.
    /// </summary>
    public void ClearPath()
    {
        Path.Clear();
        Visual?.ClearPoints();
    }

    /// <summary>
    /// Sets the route's path to a new list of nodes, updating the visual line.
    /// </summary>
    public void SetPath(List<RoadNode> newPath)
    {
        ClearPath();
        foreach (var node in newPath)
        {
            AppendNode(node);
        }
    }
    
    public bool ContainsNode(RoadNode node)
    {
        return Path.Contains(node);
    }

    /// <summary>
    /// Automatically assigns a unique ID initializes the path list, and
    /// assigns a color.
    /// </summary>
    public Route()
    {
        ID = _nextID++;
        Path = [];
        var colorInfo = LevelState.GetNextRouteColor();
        if (colorInfo.HasValue)
        {
            ColorName = colorInfo.Value.Key;
            Color = colorInfo.Value.Value;
        }
        else
        {
            // Fallback if no colors are left. TODO: make it so players cannot
            // create more routes.
            ColorName = "Default";
            Color = Colors.White;
        }
    }


}