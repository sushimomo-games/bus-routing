using Godot;
using System;
using static EditorState;

public partial class RouteName : Label
{
    public override void _Ready()
    {
        Text = $"{SelectedRoute.ColorName} Line";
    }
}
