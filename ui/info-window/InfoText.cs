using Godot;
using System;
using static EditorState;

public partial class InfoText : Label
{
    public override void _Ready()
    {
        Text = $"{SelectedRoute.TimeToComplete:F2} minutes";
    }
}
