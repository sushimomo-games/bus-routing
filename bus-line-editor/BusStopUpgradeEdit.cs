using Godot;
using System.Linq;

/// <summary>
/// Represents the purchase of a bus stop upgrade, so it can be undone.
/// </summary>
/// <remarks>
/// Reverting removes the upgrade and restores the budget from before the purchase.
/// </remarks>
public partial class BusStopUpgradeEdit : AbstractEdit
{
    private readonly uint _budgetBefore;
    private readonly uint _budgetAfter;
    private readonly Vector2 _position;
    private readonly BusStopUpgrade _upgrade;
    private BusStop _stop;

    /// <summary>
    /// Creates an edit for an upgrade that was just purchased.
    /// </summary>
    /// <param name="stop">The upgraded bus stop.</param>
    /// <param name="upgrade">The upgrade that was purchased.</param>
    /// <param name="budgetBefore">The budget before the upgrade was paid for.</param>
    public BusStopUpgradeEdit(BusStop stop, BusStopUpgrade upgrade, uint budgetBefore)
    {
        _stop = stop;
        _position = stop.GlobalPosition;
        _upgrade = upgrade;
        _budgetBefore = budgetBefore;
        _budgetAfter = budgetBefore - BusStopUpgradeInfo.GetCost(upgrade);
    }

    /// <summary>
    /// Removes the upgrade, restores the budget, and recalculates the houses.
    /// </summary>
    /// <remarks>
    /// If the stop was deleted and restored by undo, it is a new object, so it
    /// is found again by position. The budget is restored even if no stop is found.
    /// </remarks>
    public override void Revert()
    {
        FindStop()?.RemoveUpgrade(_upgrade);

        LevelState.Budget = _budgetBefore;
        LevelState.UpdateAllHouseStatuses();
    }

    /// <summary>
    /// Buys the upgrade again after it was undone, without logging a new edit.
    /// </summary>
    public override void Apply()
    {
        FindStop()?.AddUpgrade(_upgrade);

        LevelState.Budget = _budgetAfter;
        LevelState.UpdateAllHouseStatuses();
    }

    /// <summary>
    /// Returns the upgraded stop, finding it again by position if the
    /// original object is no longer valid.
    /// </summary>
    private BusStop FindStop()
    {
        if (_stop == null || !GodotObject.IsInstanceValid(_stop))
        {
            _stop = LevelState.AllBusStops
                .OfType<BusStop>()
                .FirstOrDefault(s => GodotObject.IsInstanceValid(s) && s.GlobalPosition == _position);
        }
        return _stop;
    }
}
