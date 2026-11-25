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
/// Looks up the price and display name of each upgrade. How much each
/// upgrade lowers the amenity penalty is calculated in
/// <see cref="BusUsageModel.CalculateBusStopAmenityPenalty"/>.
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
    /// Returns the name of an upgrade as shown to the player.
    /// </summary>
    public static string GetDisplayName(BusStopUpgrade upgrade) => upgrade switch
    {
        BusStopUpgrade.Bench => "Bench",
        BusStopUpgrade.Roof => "Roof",
        _ => throw new ArgumentOutOfRangeException(nameof(upgrade), upgrade, null)
    };
}
