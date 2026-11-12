using Godot;
using System.Linq;
using System.Collections.Generic;

/// <summary> 
/// Represents an edit operation that handles the addition or removal of bus stops in a bus line.
/// </summary> 
/// <remarks> 
/// This class is used to manage the state of a bus stop before an after an edit
/// operation is performed. It allows for the reversion of changes made to the bus stop 
/// assigning reassigning the AllBusStops level state, to the captured bus stop state snapshot. 
/// This allows undo/redo functionality in the bus line editor. 
/// </remarks

public partial class BusStopEdit : AbstractEdit
{

    private readonly bool _wasPlacement;
    private readonly uint _budgetBefore;
    private readonly uint _budgetAfter;
    private readonly Vector2 _position;
    private readonly RoadNode _nodeA;
    private readonly RoadNode _nodeB;
    private BusStop _stop;
    private readonly List<AbstractEdit> _lineEdits;

    public BusStopEdit(bool wasPlacement, uint budgetBefore, Vector2 position, RoadNode nodeA, RoadNode nodeB, BusStop stop, List<AbstractEdit> lineEdits = null)
    {
        this._wasPlacement = wasPlacement;
        this._budgetBefore = budgetBefore;
        this._budgetAfter = budgetBefore - (wasPlacement ? Cost.BusStopPlacement : Cost.BusStopRemoval);
        this._position = position;
        this._nodeA = nodeA;
        this._nodeB = nodeB;
        this._stop = stop;
        this._lineEdits = lineEdits ?? [];
    }

    /// <summary> 
    /// Performs inverse operation of previous state change action. 
    /// </summary> 
    /// <remarks> 
    /// Is called by <see cref="EditHistory.cs"/> when state object is 
    /// popped from stack. 
    /// </remarks> 
    public override void Revert()
    {
        if (_wasPlacement) UndoPlacement();
        else UndoRemoval();

        for (int i = _lineEdits.Count - 1; i >= 0; i--) _lineEdits[i].Revert();

        Finish(_budgetBefore);
    }
    /// <summary> 
    /// Performs the redo operation of the previous state change action.
    /// </summary> 
    /// <remarks> 
    /// Is called by <see cref="EditHistory.cs"/> when state object is 
    /// popped from redo stack.
    /// </remarks>
    public override void Apply()
    {
        foreach (var edit in _lineEdits) edit.Apply();

        if (_wasPlacement) UndoRemoval();
        else UndoPlacement();

        Finish(_budgetAfter);
    }

    private static void Finish(uint budget)
    {
        LevelState.Budget = budget;
        LevelState.UpdateAllHouseStatuses();
        LevelState.RefreshAllBusLineVisuals();
    }

    /// <summary> 
    /// Creates and returns a new RoadEdge object between two 
    /// existing RoadNodes. 
    /// <summary/> 
    private static RoadEdge CreateRoadEdge(RoadNode nodeA, RoadNode nodeB)
    {
        var scene = GD.Load<PackedScene>(Path.RoadEdgeScene);
        var edge = scene.Instantiate<RoadEdge>();
        LevelState.CurrentLevel.AddChild(edge);
        edge.SetEndpoints(nodeA, nodeB);
        return edge;
    }


    /// <summary> 
    /// Returns existing RoadEdge object if found within <see cref="LevelState.AllRoadEdges"/> 
    /// </summary> 
    private static RoadEdge FindRoadEdge(RoadNode nodeA, RoadNode nodeB) =>
        LevelState.AllRoadEdges.FirstOrDefault(e =>
            GodotObject.IsInstanceValid(e) &&
            ((e.NodeA == nodeA && e.NodeB == nodeB) || (e.NodeA == nodeB && e.NodeB == nodeA)));

    /// <summary> 
    /// Detaches RoadEdge object from BusStops. 
    /// </summary> 
    private static void ForgetEdge(RoadEdge edge)
    {
        if (edge.NodeA is BusStop a) a.ConnectedEdges.Remove(edge);
        if (edge.NodeB is BusStop b) b.ConnectedEdges.Remove(edge);
    }

    /// <summary> 
    /// Performs inverse operation on a BusStop placement action. 
    /// Removes and destroys old edges created to attach the previously placed
    /// BusStops, and creates a single new edge.   
    /// </summary> 
    private void UndoPlacement()
    {
        if (!GodotObject.IsInstanceValid(_nodeA) || !GodotObject.IsInstanceValid(_nodeB)) return;

        // If the stop was deleted and then restored by undo, it's a new object.
        // Find the stop that now sits between the same two road nodes.
        if (_stop == null || !GodotObject.IsInstanceValid(_stop))
            _stop = _nodeA.Neighbors.OfType<BusStop>().FirstOrDefault(s => _nodeB.Neighbors.Contains(s));
        if (_stop == null) return;


        foreach (var edge in _stop.ConnectedEdges.ToArray())
        {
            if (!GodotObject.IsInstanceValid(edge)) continue;
            ForgetEdge(edge);
            edge.QueueFree();
        }

        _nodeA.RemoveNeighbor(_stop);
        _nodeB.RemoveNeighbor(_stop);

        _nodeA.AddNeighbor(_nodeB);
        _nodeB.AddNeighbor(_nodeA);

        CreateRoadEdge(_nodeA, _nodeB);

        _stop.GetParent()?.RemoveChild(_stop);
        LevelState.AllBusStops.RemoveAll(s => s == _stop);
    }

    public override void OnDiscarded()
    {
        foreach (var edit in _lineEdits) edit.OnDiscarded();
        if (GodotObject.IsInstanceValid(_stop) && !_stop.IsInsideTree()) _stop.QueueFree();
    }

    /// <summary> 
    /// Performs inverse operation of previous BusStop removal state change. 
    /// Old Edge that replaced removed BusStop is detached from RoadNodes A and B. 
    /// The previously removed BusStop is reinstantiated, and reattached to RoadNodes 
    /// A and B with new created RoadEdges. 
    /// </summary> 
    private void UndoRemoval()
    {
        if (!GodotObject.IsInstanceValid(_nodeA) || !GodotObject.IsInstanceValid(_nodeB)) return;

        LevelState.CurrentLevel.AddChild(_stop);
        LevelState.AllBusStops.Add(_stop);
        if (!LevelState.AllRoadNodes.Contains(_stop)) LevelState.AllRoadNodes.Add(_stop);
        _stop.GlobalPosition = _position;

        var oldEdge = FindRoadEdge(_nodeA, _nodeB);
        if (oldEdge != null)
        {
            ForgetEdge(oldEdge);
            oldEdge.QueueFree();
        }

        CreateRoadEdge(_nodeA, _stop);
        CreateRoadEdge(_stop, _nodeB);

        _nodeA.RemoveNeighbor(_nodeB);
        _nodeB.RemoveNeighbor(_nodeA);

        _nodeA.AddNeighbor(_stop);
        _stop.AddNeighbor(_nodeA);

        _stop.AddNeighbor(_nodeB);
        _nodeB.AddNeighbor(_stop);
    }


}
