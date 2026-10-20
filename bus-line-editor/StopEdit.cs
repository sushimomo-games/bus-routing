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

public partial class StopEdit : AbstractEdit { 

    private readonly bool _wasPlacement; 
    private readonly uint _budgetBefore; 
    private readonly Vector2 _position; 
    private readonly RoadNode _nodeA; 
    private readonly RoadNode _nodeB; 
    private BusStop _stop; 

    public StopEdit () { } 

    public override void Revert() {
        if (_wasPlacement) UndoPlacement(); 
        else UndoRemoval(); 

        LevelState.Budget = _budgetBefore; 
        LevelState.UpdateAllHouseStatuses(); 
        LevelState.RefreshAllBusLineVisuals();  
    }

    
}
