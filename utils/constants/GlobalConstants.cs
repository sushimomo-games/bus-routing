/// <summary>
/// Holds global constant values used throughout the project.
/// </summary>
public static class GlobalConstants
{
    /// <summary>
    /// The number of pixels that represent one mile in the game world.
    /// </summary>
    public const uint PixelsPerMile = 200;

    /// <summary>
    /// The number of minutes it takes a resident to walk one pixel.
    /// Raising this value makes walking slower.
    /// </summary>
    public const float WalkMinutesPerPixel = 2.5f;

    /// <summary>
    /// The number of pixels a bus travels in one minute (~30 mph).
    /// Raising this value makes buses faster.
    /// </summary>
    public const float BusPixelsPerMinute = 100f;

    /// <summary>
    /// The time in minutes a bus spends at each stop it passes through.
    /// Raising this value makes rides with many stops slower.
    /// </summary>
    public const float DwellTimeMinutesPerStop = 0.5f;

    /// <summary>
    /// The number of pixels a car travels in one minute. Matches the current
    /// bus speed (~30 mph). Raising this value makes cars faster.
    /// </summary>
    public const float CarPixelsPerMinute = 100f;

    /// <summary>
    /// The lower bound on a car commute's time in minutes, so the bus-to-car
    /// time ratio never divides by zero.
    /// </summary>
    public const float MinimumCarMinutes = 1f;
}