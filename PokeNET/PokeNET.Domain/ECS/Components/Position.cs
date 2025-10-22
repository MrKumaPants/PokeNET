namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Represents the 2D position of an entity in the game world.
/// This component follows the Single Responsibility Principle by only handling position data.
/// </summary>
public struct Position
{
    /// <summary>
    /// The X coordinate in world space.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// The Y coordinate in world space.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Initializes a new position with the specified coordinates.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public Position(float x, float y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Gets the distance between this position and another.
    /// </summary>
    /// <param name="other">The other position.</param>
    /// <returns>The Euclidean distance between the positions.</returns>
    public readonly float DistanceTo(Position other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public override readonly string ToString() => $"({X:F2}, {Y:F2})";
}
