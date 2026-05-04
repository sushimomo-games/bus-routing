using Godot;

[Tool]
public partial class Destination : Building
{
    protected override Color HighlightFactor => new(1.7f, 1.7f, 1.7f, 1.0f);

    public override void _Ready()
    {
        base._Ready(); // Calls _Ready() of the base class, Building.
        LevelState.AllDestinations.Add(this);
    }
}
