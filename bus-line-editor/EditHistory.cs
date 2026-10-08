using Godot;
using System.Collections.Generic;

public partial class EditHistory : Node
{
    private readonly Stack<IEdit> _undo = new();

    public bool CanUndo => _undo.Count > 0;

    public void Undo()
    {
        if(!CanUndo) { return; }

        _undo.Pop().Revert();
    }
}
