using Godot;
using System.Collections.Generic;

/// <summary>
/// A tracking system for edits made via the bus line editor tool.
/// </summary>
public partial class EditHistory : Node
{
    /// <summary>
    /// A read only stack for storing the descendants of the IEdit interface.
    /// </summary>
    private readonly Stack<IEdit> _undo = new();

    /// <summary>
    /// Gets a value indicating whether there are any actions available in the history stack to undo.
    /// </summary>
    /// <value>
    /// <c>true</c> if the history stack contains at least one item; otherwise, <c>false</c>.
    /// </value>
    public bool CanUndo => _undo.Count > 0;
    
    /// <summary>
    /// Reverts the last player action in the undo history.
    /// </summary>
    /// <remarks>
    /// This method checks <see cref="CanUndo"/> before proceeding.
    /// If there are no changes to revert, the method returns.
    /// </remarks>
    public void Undo()
    {
        if(!CanUndo) { return; }

        _undo.Pop().Revert();
    }
}
