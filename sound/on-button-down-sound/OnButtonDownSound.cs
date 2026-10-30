using Godot;

/// <summary>
/// Plays a sound when the left mouse button is clicked on the parent Control node.
/// </summary>
public partial class OnButtonDownSound : AudioStreamPlayer
{
    public override void _Ready()
    {
        if (GetParent() is Control control)
            control.GuiInput += OnGuiInput;
        else
            GD.PushWarning($"{nameof(OnButtonDownSound)} needs a Control parent: {GetParent().Name}");
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (@event.IsLeftMouseClick())
            Play();
    }
}