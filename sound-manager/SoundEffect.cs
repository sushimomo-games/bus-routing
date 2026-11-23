using Godot;

[GlobalClass]
public partial class SoundEffect : Resource
{
    [Export] public AudioStream MouseDown { get; set; }
    [Export] public AudioStream MouseUp { get; set; }
}