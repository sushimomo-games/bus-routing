using Godot;

[GlobalClass]
public partial class SoundEffectSet : Resource
{
    [Export] public AudioStream MouseDown { get; set; }
    [Export] public AudioStream MouseUp { get; set; }

    /// <summary>
    /// This sound is for when the player drags an item and drops it in a
    /// valid location i.e. the road.
    /// </summary>
    [Export] public AudioStream DropSuccess { get; set; }

    /// <summary>
    /// This sound is for when the player drags an item and drops it in an
    /// invalid location i.e. outside of the road.
    /// </summary>
    [Export] public AudioStream DropFail { get; set; }
}