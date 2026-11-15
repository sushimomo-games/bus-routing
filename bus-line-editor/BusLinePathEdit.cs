using Godot;
using System.Collections.Generic;

/// <summary>
/// A change to the path of a finalized bus line, such as losing a deleted bus stop.
/// </summary>
public partial class BusLinePathEdit : AbstractEdit
{
    private readonly BusLine _line;
    private readonly List<RoadNode> _before;
    private readonly List<RoadNode> _after;

    public BusLinePathEdit(BusLine line, List<RoadNode> before, List<RoadNode> after)
    {
        _line = line;
        _before = before;
        _after = after;
    }

    /// <summary>
    /// Sets the line's path back to how it was before the change.
    /// </summary>
    public override void Revert() => SetPath(_before);

    /// <summary>
    /// Sets the line's path to how it was after the change.
    /// </summary>
    public override void Apply() => SetPath(_after);

    private void SetPath(List<RoadNode> path)
    {
        if (GodotObject.IsInstanceValid(_line)) _line.SetPath(path);
    }
}
