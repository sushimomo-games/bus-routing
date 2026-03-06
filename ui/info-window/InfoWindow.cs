using Godot;
using System;
using static EditorState;

public partial class InfoWindow : Control
{
    public override void _Ready()
    {
        Position = new Vector2(100, 100);
    }
}
