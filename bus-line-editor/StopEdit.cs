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

    /// <summary> 
    /// Before snapshotof saved state of bus stops before user's edit operation. 
    /// </summary>     
    private List<BusStop> _before;
    /// <summary>
    /// After snapshot of saved state of bus stop after the user's edit operation.
    /// </summary>  
    private List<BusStop> _after; 
    /// <summary> 
    /// Before snapshot of budget before edit operation. 
    /// </summary> 
    private uint _budgetBefore; 
    /// <summary> 
    /// After snapshot of budget after edit operation. 
    /// </summary> 
    private uint _budgetAfter; 

    public StopEdit(List<BusStop> before, List<BusStop> after, uint budgetBefore, uint budgetAfter) {
        this._before = before; 
        this._after = after; 
        this._budgetBefore = budgetBefore; 
        this._budgetAfter = budgetAfter; 
    }  

    /// <summary> 
    ///  Reverts the last player action in the undo history by restoring the bus stops to their state before edit operation. 
    /// </summary>
    /// <remarks>
    /// Updates the global LevelState.AllBusStops to the state captured in the 'before' snapshot, effectively undoing the changes made by the user. 
    /// Original budget value before state change is returned. 
    /// </remarks>  
    public override void Revert() {
        LevelState.AllBusStops = _before;
        Budget = _budgetBefore;  
    }
    
}
