using System;

namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Represents the 3D position of an entity in the game world.
/// This component follows the Single Responsibility Principle by only handling position data.
/// Z-coordinate is used for rendering layer depth (higher Z = rendered in front).
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
    /// The Z coordinate for rendering layer depth (higher values render in front).
    /// </summary>
    public float Z { get; set; }

    /// <summary>
    /// Initializes a new position with the specified coordinates.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate for layer depth (default: 0).</param>
    public Position(float x, float y, float z = 0f)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Gets the distance between this position and another (2D distance, ignoring Z).
    /// </summary>
    /// <param name="other">The other position.</param>
    /// <returns>The Euclidean distance between the positions.</returns>
    public readonly float DistanceTo(Position other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public override readonly string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
}
