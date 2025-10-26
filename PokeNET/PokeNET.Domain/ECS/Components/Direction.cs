namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// 8-directional facing for Pokemon-style movement and sprite orientation.
/// </summary>
/// <remarks>
/// Represents the cardinal and diagonal directions used in Pokemon games.
/// Used for determining sprite facing direction and movement input.
/// </remarks>
public enum Direction
{
    /// <summary>No direction / stationary.</summary>
    None = 0,

    /// <summary>Facing upward (North).</summary>
    North = 1,

    /// <summary>Facing diagonally up-right (NorthEast).</summary>
    NorthEast = 2,

    /// <summary>Facing right (East).</summary>
    East = 3,

    /// <summary>Facing diagonally down-right (SouthEast).</summary>
    SouthEast = 4,

    /// <summary>Facing downward (South).</summary>
    South = 5,

    /// <summary>Facing diagonally down-left (SouthWest).</summary>
    SouthWest = 6,

    /// <summary>Facing left (West).</summary>
    West = 7,

    /// <summary>Facing diagonally up-left (NorthWest).</summary>
    NorthWest = 8,
}

/// <summary>
/// Extension methods for <see cref="Direction"/> enum.
/// </summary>
public static class DirectionExtensions
{
    /// <summary>
    /// Converts a direction to tile offset coordinates.
    /// </summary>
    /// <param name="direction">The direction to convert.</param>
    /// <returns>A tuple containing the X and Y tile offsets (dx, dy).</returns>
    public static (int dx, int dy) ToOffset(this Direction direction)
    {
        return direction switch
        {
            Direction.North => (0, -1),
            Direction.NorthEast => (1, -1),
            Direction.East => (1, 0),
            Direction.SouthEast => (1, 1),
            Direction.South => (0, 1),
            Direction.SouthWest => (-1, 1),
            Direction.West => (-1, 0),
            Direction.NorthWest => (-1, -1), // Fixed: was (-1, 1)
            _ => (0, 0),
        };
    }

    /// <summary>
    /// Gets the opposite direction.
    /// </summary>
    /// <param name="direction">The direction to reverse.</param>
    /// <returns>The opposite direction.</returns>
    public static Direction Opposite(this Direction direction)
    {
        return direction switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East => Direction.West,
            Direction.West => Direction.East,
            Direction.NorthEast => Direction.SouthWest,
            Direction.SouthWest => Direction.NorthEast,
            Direction.NorthWest => Direction.SouthEast,
            Direction.SouthEast => Direction.NorthWest,
            _ => Direction.None,
        };
    }
}
