using Godot;

[Tool]
public partial class Destination : Building
{
    public override void _Ready()
    {
        base._Ready(); // Calls _Ready() of the base class, Building.
        LevelState.AllDestinations.Add(this);
    }
}
