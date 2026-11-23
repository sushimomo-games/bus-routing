using Godot;
using System.Collections.Generic;

/// <summary>
/// Undo and redo history for edits made with the bus line editor.
/// </summary>
/// <remarks>
/// Two stacks, both stored as linked lists so the oldest undo entry can be trimmed from the front.
/// <see cref="Undo"/> moves the newest undo entry to the redo stack, and <see cref="Redo"/> moves it back.
/// <see cref="LogEdit"/> clears the redo stack because a new action replaces the undone ones.
/// </remarks>
public partial class EditHistory
{
    /// <summary>
    /// The maximum number of edits kept for undo.
    /// </summary>
    private const int MaxHistory = 50;

    /// <summary>
    /// Edits that can be undone. The newest edit is last.
    /// </summary>
    private readonly LinkedList<AbstractEdit> _undo = new();

    /// <summary>
    /// Edits that were undone and can be redone. The most recently undone edit is last.
    /// </summary>
    private readonly LinkedList<AbstractEdit> _redo = new();

    /// <summary>
    /// True if there is an edit to undo.
    /// </summary>
    public bool CanUndo => _undo.Count > 0;

    /// <summary>
    /// True if there is an undone edit to redo.
    /// </summary>
    public bool CanRedo => _redo.Count > 0;

    /// <summary>
    /// Empties both stacks and lets each edit free what it kept alive.
    /// </summary>
    public void Clear()
    {
        foreach (var edit in _undo) edit.OnDiscarded();
        foreach (var edit in _redo) edit.OnDiscarded();
        _undo.Clear();
        _redo.Clear();
    }

    /// <summary>
    /// Records a new edit and discards the redo stack.
    /// </summary>
    /// <param name="edit">The edit the player just made.</param>
    public void LogEdit(AbstractEdit edit)
    {
        foreach (var undone in _redo) undone.OnDiscarded();
        _redo.Clear();

        PushUndo(edit);
    }

    /// <summary>
    /// Adds an edit to the undo stack and drops the oldest edit past <see cref="MaxHistory"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Redo"/> calls this instead of <see cref="LogEdit"/> so redoing one edit keeps the rest of the redo stack.
    /// </remarks>
    private void PushUndo(AbstractEdit edit)
    {
        _undo.AddLast(edit);

        if (_undo.Count > MaxHistory)
        {
            _undo.First.Value.OnDiscarded();
            _undo.RemoveFirst();
        }
        GD.Print("[DEBUG - Editor] number of edits in history: " + _undo.Count);
    }

    /// <summary>
    /// Undoes the newest edit and moves it to the redo stack.
    /// </summary>
    /// <remarks>
    /// Does nothing if there is no edit to undo, or if the edit is a bus stop change while a line is being drawn.
    /// A skipped edit stays on its stack.
    /// </remarks>
    public void Undo()
    {
        if (!CanUndo) { return; }

        AbstractEdit edit = _undo.Last.Value;

        // The draft segments may reference the stop, so leave it alone while a line is open.
        if (edit is BusStopEdit && BusLineEditor.BusLineInProgress != null) { return; }

        _undo.RemoveLast();
        edit.Revert();
        _redo.AddLast(edit);
        GD.Print("[DEBUG - Editor] Undoing edit: " + edit.GetType().Name);
        GD.Print("[DEBUG - Editor] number of edits in history: " + _undo.Count);
    }

    /// <summary>
    /// Redoes the most recently undone edit and moves it back to the undo stack.
    /// </summary>
    /// <remarks>
    /// Follows the same skip rules as <see cref="Undo"/>.
    /// </remarks>
    public void Redo()
    {
        if (!CanRedo) { return; }

        AbstractEdit edit = _redo.Last.Value;

        if (edit is BusStopEdit && BusLineEditor.BusLineInProgress != null) { return; }

        _redo.RemoveLast();
        edit.Apply();
        PushUndo(edit);
        GD.Print("[DEBUG - Editor] Redoing edit: " + edit.GetType().Name);
    }
}
