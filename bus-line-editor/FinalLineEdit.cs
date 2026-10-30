using Godot; 
using System.Collections.Generic; 

/// <summary>
/// A state object class which groups individual line segments of a bus line into groups.  
/// </summary> 
/// <remarks> 
/// This class handles the case in which undoing a segment in a bus line, removes the whole
/// bus line, and does not leave disjointed segments. 
/// </remarks>  
public partial class FinalLineEdit : AbstractEdit {

    private readonly List<BusLine> _group =[];  
    public FinalLineEdit () { }

    public FinalLineEdit (BusLine line) {
        AddToGroup(line);
    }

    /// <summary>
    ///  Reverts the finalized grouped bus line by removing the entire line 
    ///  from the level and detaching it from the scene tree.  
    /// </summary>
    /// <remarks>
    /// <p> 
    ///  Every valid bus line in the group is treated as a part of the 
    ///  same finalized line. REmoving it from <see cref="LevelState.AllBusLines"/> 
    ///  removes all of the line's segments at once instead of leaving disjointed
    ///  segments behind.
    /// </p> 
    /// <p> 
    ///  The corresponding entry in BusLineList is removed and if the 
    ///  line is selected <see cref="EditorState.SelectedBusLine"/> is cleared. 
    ///  The bus line node is detached from its parent instead of 
    ///  freeing with <c>QueueFree()</c>, which allows redo to reinstantiate the 
    ///  undone line. 
    /// </p> 
    /// <p> 
    ///  It's important to note that <see cref="EditorState.SelectedBusLine"/> will
    ///  still point to a previously detached bus line. This is why it is being cleared
    ///  so that undefined behavior doesn't occure when editing a line that was removed from 
    ///  the scene tree. 
    /// </p> 
    /// </remarks> 
    public override void Revert () { 
      
      var busLineList = LevelState.CurrentLevel?.GetNodeOrNull<BusLineList>(Path.BusLineListNode); 
      
      foreach (var line in _group) {
        if (line == null || !GodotObject.IsInstanceValid(line)) continue; 
        
        int index = LevelState.AllBusLines.IndexOf(line); 
        if (index < 0) {
            line.GetParent()?.RemoveChild(line);
            continue; 
        }

        if (EditorState.SelectedBusLine == line) {
            EditorState.SelectedBusLine = null; 
            busLineList?.DeselectAll(); 
        }

        LevelState.AllBusLines.RemoveAt(index); 

        if(busLineList != null && index < busLineList.ItemCount) {
            bool wasBlocking = busLineList.IsBlockingSignals(); 
            busLineList.SetBlockSignals(true); 
            busLineList.RemoveItem(index); 
            busLineList.SetBlockSignals(wasBlocking); 
        }
        line.GetParent()?.RemoveChild(line);  
      } 
      
      if (EditorState.SelectedBusLine != null && GodotObject.IsInstanceValid(EditorState.SelectedBusLine)) {
        int selectedIndex = LevelState.AllBusLines.IndexOf(EditorState.SelectedBusLine); 
        if (selectedIndex >= 0) busLineList?.Select(selectedIndex); 
        else {
            EditorState.SelectedBusLine = null; 
            busLineList?.DeselectAll();
        } 
      }
      else {
        EditorState.SelectedBusLine = null; 
        busLineList?.DeselectAll(); 
      }

      LevelState.UpdateAllHouseStatuses(); 
      LevelState.RefreshAllBusLineVisuals(); 
    }

    /// <summary> 
    /// Adds a finalized bus line to the group.  
    /// </summary>
    /// <param name="line">
    /// The whole bus line added. 
    /// </param>
    public void AddToGroup (BusLine line) { // take segment 
       if (line == null || !GodotObject.IsInstanceValid(line)) return; 
       if (_group.Contains(line)) return; 
       _group.Add(line); 
    }    
}