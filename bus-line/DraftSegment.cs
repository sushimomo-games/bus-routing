using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class DraftSegment : Node2D
{
    public List<RoadNode> Nodes { get; private set; }
    private BusLineVisual _visual; // Swap Line2D for BusLineVisual

    public RoadNode FirstNode => Nodes.FirstOrDefault();
    public RoadNode LastNode => Nodes.LastOrDefault();

    public DraftSegment(RoadNode startNode, BusLine associatedBusLine)
    {
        Nodes = [startNode];

        _visual = new BusLineVisual(associatedBusLine);

        AddChild(_visual);
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        _visual.UpdateVisualFromPath(Nodes);
    }

    public void Append(RoadNode node)
    {
        Nodes.Add(node);
        UpdateVisuals();
    }

    public void Prepend(RoadNode node)
    {
        Nodes.Insert(0, node);
        UpdateVisuals();
    }

    /// <summary>
    /// How far from a road, in pixels, a click still counts as hitting it.
    /// </summary>
    private const float SliceClickTolerance = 15f;

    /// <summary>
    /// In slice mode, a left click on one of this segment's roads slices it.
    /// </summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (EditorState.CurrentBusLineCreationStep != BusLineCreationStep.Slicing) return;
        if (!@event.IsLeftMouseClick()) return;

        int road = FindRoadAt(GetGlobalMousePosition());
        if (road < 0) return;

        // Stops other segments and road nodes from handling the same click.
        GetViewport().SetInputAsHandled();
        BusLineEditor.SliceDraftSegment(this, road);
    }

    /// <summary>
    /// Finds the road nearest to a point.
    /// </summary>
    /// <returns>
    /// The index i of the road from Nodes[i] to Nodes[i + 1], or -1 if no road is within
    /// <see cref="SliceClickTolerance"/>.
    /// </returns>
    private int FindRoadAt(Vector2 point)
    {
        int nearest = -1;
        float nearestDistance = SliceClickTolerance;

        for (int i = 0; i < Nodes.Count - 1; i++)
        {
            Vector2 closest = Geometry2D.GetClosestPointToSegment(
                point, Nodes[i].GlobalPosition, Nodes[i + 1].GlobalPosition);
            float distance = point.DistanceTo(closest);

            if (distance < nearestDistance)
            {
                nearest = i;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    public new void QueueFree()
    {
        _visual?.QueueFree();
        base.QueueFree();
    }

    public bool CanMergeWith(DraftSegment other)
    {
        // A segment can only merge with another segment if their endpoints touch.
        // This physically prevents T-junctions while allowing self-crossings.
        return FirstNode == other.FirstNode ||
            FirstNode == other.LastNode ||
            LastNode == other.FirstNode ||
            LastNode == other.LastNode;
    }

    public void MergeWith(DraftSegment other)
    {
        if (LastNode == other.FirstNode)
        {
            other.Nodes.RemoveAt(0);
            Nodes.AddRange(other.Nodes);
        }
        else if (FirstNode == other.LastNode)
        {
            other.Nodes.RemoveAt(other.Nodes.Count - 1);
            Nodes.InsertRange(0, other.Nodes);
        }
        else if (LastNode == other.LastNode)
        {
            other.Nodes.RemoveAt(other.Nodes.Count - 1);
            other.Nodes.Reverse();
            Nodes.AddRange(other.Nodes);
        }
        else if (FirstNode == other.FirstNode)
        {
            other.Nodes.RemoveAt(0);
            other.Nodes.Reverse();
            Nodes.InsertRange(0, other.Nodes);
        }

        UpdateVisuals();
    }
}