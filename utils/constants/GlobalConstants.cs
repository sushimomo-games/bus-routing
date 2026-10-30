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
}