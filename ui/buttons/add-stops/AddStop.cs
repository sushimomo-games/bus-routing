using Godot;
using System;

public partial class AddStop : Control
{
    private TextureRect _texture;

    private float _scaleFactor = 1.25f;
    private Vector2 _targetScale = Vector2.One;
    [Export] private float _lerpSpeed = 15.0f;

    public override void _Ready()
    {
        _texture = GetNode<TextureRect>("TextureRect");
    }

    public override void _Process(double delta)
    {
        _texture.Scale = _texture.Scale.Lerp(_targetScale, (float)delta * _lerpSpeed);
    }

    public override Variant _GetDragData(Vector2 position)
    {
        return "BusStop";
    }

    private void _on_texture_rect_mouse_entered()
    {
        _targetScale = new Vector2(_scaleFactor, _scaleFactor);
    }

    private void _on_texture_rect_mouse_exited()
    {
        _targetScale = Vector2.One;
    }
}