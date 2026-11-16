using System;

/// <summary>
/// The upgrades that can be purchased for a bus stop.
/// </summary>
public enum BusStopUpgrade
{
    Bench,
    Roof
}

/// <summary>
/// The result of trying to purchase a bus stop upgrade.
/// </summary>
public enum UpgradePurchaseResult
{
    Purchased,
    AlreadyPurchased,
    StopNotInUse,
    InsufficientBudget
}

/// <summary>
/// Looks up the price, comfort bonus, and display name of each upgrade.
/// </summary>
public static class BusStopUpgradeInfo
{
    /// <summary>
    /// Returns the price of an upgrade, taken from <see cref="Cost"/>.
    /// </summary>
    public static uint GetCost(BusStopUpgrade upgrade) => upgrade switch
    {
        BusStopUpgrade.Bench => Cost.BenchUpgrade,
        BusStopUpgrade.Roof => Cost.RoofUpgrade,
        _ => throw new ArgumentOutOfRangeException(nameof(upgrade), upgrade, null)
    };

    /// <summary>
    /// Returns how much an upgrade lowers disutility for residents boarding
    /// at the stop, taken from <see cref="GlobalConstants"/>.
    /// </summary>
    public static float GetComfortBonus(BusStopUpgrade upgrade) => upgrade switch
    {
        BusStopUpgrade.Bench => GlobalConstants.BenchComfortBonus,
        BusStopUpgrade.Roof => GlobalConstants.RoofComfortBonus,
        _ => throw new ArgumentOutOfRangeException(nameof(upgrade), upgrade, null)
    };

    /// <summary>
    /// Returns the name of an upgrade as shown to the player.
    /// </summary>
    public static string GetDisplayName(BusStopUpgrade upgrade) => upgrade switch
    {
        BusStopUpgrade.Bench => "Bench",
        BusStopUpgrade.Roof => "Roof",
        _ => throw new ArgumentOutOfRangeException(nameof(upgrade), upgrade, null)
    };
}
