using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class DraftSegment : Node
{
    public List<RoadNode> Nodes { get; private set; }

    public RoadNode FirstNode => Nodes.FirstOrDefault();
    public RoadNode LastNode => Nodes.LastOrDefault();

    public DraftSegment(RoadNode startNode)
    {
        Nodes = new List<RoadNode> { startNode };
        GD.Print($"[DEBUG - DraftSegment] Created new segment. Initial node: {startNode.Name}");
    }

    public void Append(RoadNode node)
    {
        Nodes.Add(node);
        GD.Print($"[DEBUG - DraftSegment] Appended {node.Name}. Segment length is now {Nodes.Count}.");
    }

    public void Prepend(RoadNode node)
    {
        Nodes.Insert(0, node);
        GD.Print($"[DEBUG - DraftSegment] Prepended {node.Name}. Segment length is now {Nodes.Count}.");
    }

    public bool CanMergeWith(DraftSegment other)
    {
        if (other == null) return false;
        
        bool canMerge = LastNode == other.FirstNode || 
                        FirstNode == other.LastNode ||
                        LastNode == other.LastNode || 
                        FirstNode == other.FirstNode;
                        
        if (canMerge) GD.Print($"[DEBUG - DraftSegment] Merge match found between {this.Name} and {other.Name}!");
        return canMerge;
    }

public void MergeWith(DraftSegment other)
    {
        GD.Print($"[DEBUG - DraftSegment] Merging: {this.Name} (Count: {Nodes.Count}) with {other.Name} (Count: {other.Nodes.Count})");
        
        // 1. Standard: Last of current connects to First of other
        if (LastNode == other.FirstNode)
        {
            other.Nodes.RemoveAt(0);
            Nodes.AddRange(other.Nodes);
            GD.Print("[DEBUG - DraftSegment] Merge: End-to-Start successful.");
        }
        // 2. Standard: First of current connects to Last of other
        else if (FirstNode == other.LastNode)
        {
            other.Nodes.RemoveAt(other.Nodes.Count - 1);
            Nodes.InsertRange(0, other.Nodes);
            GD.Print("[DEBUG - DraftSegment] Merge: Start-to-End successful.");
        }
        // 3. Flip Required: Last of current connects to Last of other
        else if (LastNode == other.LastNode)
        {
            other.Nodes.RemoveAt(other.Nodes.Count - 1); // Remove the common node
            other.Nodes.Reverse();                       // Flip the nodes so the start is now at the end
            Nodes.AddRange(other.Nodes);                 // Append the flipped list
            GD.Print("[DEBUG - DraftSegment] Merge: End-to-End (Flip) successful.");
        }
        // 4. Flip Required: First of current connects to First of other
        else if (FirstNode == other.FirstNode)
        {
            other.Nodes.RemoveAt(0);                     // Remove the common node
            other.Nodes.Reverse();                       // Flip the nodes
            Nodes.InsertRange(0, other.Nodes);           // Prepend the flipped list
            GD.Print("[DEBUG - DraftSegment] Merge: Start-to-Start (Flip) successful.");
        }
        else
        {
            GD.PrintErr($"[DEBUG - DraftSegment] Logic Error: No valid endpoints found for merge between {this.Name} and {other.Name}");
        }
    }
}