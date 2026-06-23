using Godot;
using System;
using static EditorTool;

public partial class AddLineButton : Button
{
    private void _on_pressed()
    {
        EditorState.ActiveTool = NewBusLine;
    }
}
