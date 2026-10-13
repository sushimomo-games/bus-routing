/// <summary>
/// Holds the constant values used to estimate how long each part of a
/// commute takes. Keeping them in one place makes travel speeds easy to tune.
/// </summary>
public static class TravelConstants
{
    /// <summary>
    /// The number of minutes it takes a resident to walk one pixel.
    /// Raising this value makes walking slower.
    /// </summary>
    public const float WalkMinutesPerPixel = 2.5f;
}
