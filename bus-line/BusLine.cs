using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static LevelState;

/// <summary>
/// Represents a finalized bus line running in the simulation.
/// </summary>
public partial class BusLine : Node
{
    private static uint _nextID = 1;

    public uint ID { get; private set; }
    public List<RoadNode> Path { get; set; } = [];
    public string ColorName { get; set; }
    public Color Color { get; set; }
    public BusLineVisual Visual { get; private set; }

    public event Action OnPathChanged;
    public event Action OnDeleted;

    public float TimeToComplete
    {
        get
        {
            float total = 0f;
            for (int i = 0; i < Path.Count - 1; i++)
            {
                var a = Path[i];
                var b = Path[i + 1];
                if (!GodotObject.IsInstanceValid(a) || !GodotObject.IsInstanceValid(b))
                {
                    continue;
                }
                total += Path[i].GlobalPosition.DistanceTo(Path[i + 1].GlobalPosition);
            }
            return total;
        }
    }

    public BusLine()
    {
        ID = _nextID++;

        var colorInfo = LevelState.GetNextBusLineColor();
        if (colorInfo.HasValue)
        {
            ColorName = colorInfo.Value.Key;
            Color = colorInfo.Value.Value;
        }
        else
        {
            ColorName = "Default";
            Color = Colors.White;
        }

        Visual = new BusLineVisual(this);
        AddChild(Visual);
    }

    public void SetPath(List<RoadNode> newPath)
    {
        Path = newPath.Where(n => n != null && GodotObject.IsInstanceValid(n)).ToList();
        Visual?.UpdateVisual();
        OnPathChanged?.Invoke();
    }

    public void ClearPath()
    {
        Path.Clear();
        Visual?.ClearPoints();
        OnPathChanged?.Invoke();
    }

    public bool ContainsNode(RoadNode node) => Path.Contains(node);

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
    /// Called when a bus stop is deleted; removes all segments between
    /// the deleted bus stop and its neighbor bus stop, so that the line
    /// is not without start and end stops.
    /// </summary>
    public void RemoveExcessLineSegments(BusStop deletedStop)
    {
        int index = Path.IndexOf(deletedStop);
        if (index < 0) return;

        if (index == 0)
        {
            int nextStopIndex = -1;
            for (int i = 1; i < Path.Count; i++)
            {
                if (Path[i] is BusStop)
                {
                    nextStopIndex = i;
                    break;
                }
            }

            SetPath(nextStopIndex != -1 ? Path.Skip(nextStopIndex).ToList() : []);
            return;
        }

        if (index == Path.Count - 1)
        {
            int prevStopIndex = -1;
            for (int i = index - 1; i >= 0; i--)
            {
                if (Path[i] is BusStop)
                {
                    prevStopIndex = i;
                    break;
                }
            }

            SetPath(prevStopIndex != -1 ? Path.Take(prevStopIndex + 1).ToList() : []);
            return;
        }

        var remaining = new List<RoadNode>(Path);
        remaining.RemoveAt(index);

        var roadFollowedPath = EnsureRoadConnectedPath(remaining);
        if (roadFollowedPath != null)
        {
            SetPath(roadFollowedPath);
        }
        else
        {
            Delete();
        }
    }

    private static List<RoadNode> EnsureRoadConnectedPath(List<RoadNode> nodes)
    {
        if (nodes.Count == 0) return [];
        if (nodes.Count == 1) return nodes;

        var result = new List<RoadNode> { nodes[0] };

        for (int i = 0; i < nodes.Count - 1; i++)
        {
            var current = nodes[i];
            var next = nodes[i + 1];

            if (current == next) continue;

            if (current.Neighbors.Contains(next))
            {
                result.Add(next);
            }
            else
            {
                var bridge = FindShortestRoadPath(current, next);
                if (bridge == null || bridge.Count < 2)
                    return null;

                // Skip bridge[0] since it's already `current`
                for (int b = 1; b < bridge.Count; b++)
                    result.Add(bridge[b]);
            }
        }

        return result;
    }

    private static List<RoadNode> FindShortestRoadPath(RoadNode start, RoadNode goal)
    {
        var queue = new Queue<RoadNode>();
        var cameFrom = new Dictionary<RoadNode, RoadNode>();
        var visited = new HashSet<RoadNode> { start };

        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (node == goal)
            {
                var path = new List<RoadNode>();
                for (RoadNode cur = goal; cur != null; cur = cameFrom.GetValueOrDefault(cur))
                    path.Add(cur);
                path.Reverse();
                return path;
            }

            foreach (var neighbor in node.Neighbors)
            {
                if (neighbor != null && GodotObject.IsInstanceValid(neighbor) && visited.Add(neighbor))
                {
                    cameFrom[neighbor] = node;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Removes the busLine from the level and returns its color to the pool,
    /// but keeps the node alive (detached from the scene tree) so the removal
    /// can be undone.
    /// </summary>
    public void Detach()
    {
        LevelState.AllBusLines.Remove(this);
        LevelState.ReturnBusLineColor(new KeyValuePair<string, Color>(ColorName, Color));
        GetParent()?.RemoveChild(this);
        UpdateAllHouseStatuses();

        OnDeleted?.Invoke();
    }

    /// <summary>
    /// Removes the bus line from the level as a player action.
    /// </summary>
    /// <param name="undoable">
    /// If true, the line is detached and a <see cref="DeleteBusLineEdit"/> is logged so the delete can be undone.
    /// If false, the line is freed immediately, for example when deleting a bus stop invalidates it.
    /// </param>
    public void DeleteFromLevel(bool undoable = false)
    {
        int index = LevelState.AllBusLines.IndexOf(this);

        if (undoable)
        {
            Detach();
            EditorState.History.LogEdit(new DeleteBusLineEdit(this, index));
        }
        else
        {
            Delete();
        }

        if (EditorState.SelectedBusLine == this) EditorState.SelectedBusLine = null;
        UpdateAllHouseStatuses();
    }

    /// <summary>
    /// Use this instead of QueueFree() directly on the bus line to properly clean
    /// up the busLine and return its color to the pool.
    /// </summary>
    public void Delete()
    {
        Detach();
        QueueFree();
    }

}
