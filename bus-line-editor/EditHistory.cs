using Godot;
using System.Collections.Generic;

/// <summary>
/// A tracking system for edits made via the bus line editor tool.
/// </summary>
public partial class EditHistory : Node
{
    private const int MaxHistory = 50;

    /// <summary>
    /// A read only stack for storing the descendants of the AbstractEdit class.
    /// </summary>
    private readonly LinkedList<AbstractEdit> _undo = new();

    /// <summary>
    /// Gets a value indicating whether there are any actions available in the history stack to undo.
    /// </summary>
    /// <value>
    /// <c>true</c> if the history stack contains at least one item; otherwise, <c>false</c>.
    /// </value>
    public bool CanUndo => _undo.Count > 0;

    public void LogEdit(AbstractEdit edit)
    {
        _undo.AddLast(edit);

        if (_undo.Count > MaxHistory)
        {
            _undo.RemoveFirst();
        }
    }
    
    /// <summary>
    /// Reverts the last player action in the undo history.
    /// </summary>
    /// <remarks>
    /// This method checks <see cref="CanUndo"/> before proceeding.
    /// If there are no changes to revert, the method returns.
    /// </remarks>
    public void Undo()
    {
        if (!CanUndo) { return; }

        AbstractEdit edit = _undo.Last.Value;
        _undo.RemoveLast();
        edit.Revert();
    }
}
