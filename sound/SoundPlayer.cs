using Godot;

/// <summary>
/// Plays sound effects when the left mouse button is pressed or released on the parent
/// <see cref="Control"/>. Works on any Control, including ones that only act like buttons.
/// </summary>
public partial class SoundPlayer : Node
{
    /// <summary>
    /// The sound effects to play on mouse down and mouse up.
    /// If null, the default sounds from SoundManager will be used.
    /// </summary>
    [Export] public SoundEffectSet Sounds { get; set; }

    public override void _Ready()
    {
        if (GetParent() is not Control control)
        {
            GD.PushWarning($"{nameof(SoundPlayer)} needs a Control parent: {GetParent()?.Name}");
            return;
        }

        control.GuiInput += OnGuiInput;
    }

    /// <summary>
    /// Plays the mouse down or mouse up sound when the left mouse button is pressed or
    /// released on the parent Control.
    /// </summary>
    /// <param name="event">The input event received by the parent Control.</param>
    private void OnGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseEvent)
            PlaySound(mouseEvent.Pressed);
    }

    /// <summary>
    /// Plays the mouse down or mouse up sound from the assigned set, falling back to the
    /// SoundManager defaults when no set is assigned.
    /// </summary>
    /// <param name="down">True to play the mouse down sound, false for mouse up.</param>
    private void PlaySound(bool down)
    {
        var set = Sounds ?? SoundManager.Instance.DefaultSounds;
        SoundManager.Instance.Play(down ? set?.MouseDown : set?.MouseUp);
    }
}