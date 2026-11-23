using Godot;

[GlobalClass]
public partial class SoundEffectSet : Resource
{
    [Export] public AudioStream MouseDown { get; set; }
    [Export] public AudioStream MouseUp { get; set; }
}