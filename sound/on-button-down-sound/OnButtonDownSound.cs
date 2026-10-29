using Godot;
using System;

/// <summary>
/// Script for a component node that plays a sound when its parent button is pressed down.
/// </summary>
public partial class OnButtonDownSound : AudioStreamPlayer2D
{
    public override void _Ready()
    {
        Node parent = GetParent();

        if (parent is BaseButton button)
        {
            button.ButtonDown += OnParentButtonDown;
        }
        else
        {
            GD.PushWarning($"ButtonSoundPlayer attached to a non-button node: {parent.Name}");
        }
    }

    /// <summary>
    /// Plays the sound when the parent button is pressed down.
    /// </summary>
    private void OnParentButtonDown()
    {
        Play();
    }
}
