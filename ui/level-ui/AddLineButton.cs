using Godot;
using System;
using static EditorTool;
using static Path;

public partial class AddLineButton : Button
{
    /// <summary>
    /// The label that indicates to the player that they are currently creating a new bus line.
    /// </summary>
    private Label _creatingNewLineLabel;

    public override void _Ready()
    {
        _creatingNewLineLabel = GetTree().CurrentScene.GetNode<Label>(CreatingNewLineLabelNode);
    }

    private void _on_pressed()
    {
        EditorState.ActiveTool = NewBusLine;
        _creatingNewLineLabel.Visible = true;
    }
}
