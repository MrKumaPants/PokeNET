namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Represents movement boundaries and constraints for an entity.
/// Used to limit movement to specific areas or apply maximum velocity.
/// This component follows the Single Responsibility Principle by only handling constraint data.
/// </summary>
public struct MovementConstraint
{
    /// <summary>
    /// Minimum X coordinate (left boundary).
    /// </summary>
    public float? MinX { get; set; }

    /// <summary>
    /// Maximum X coordinate (right boundary).
    /// </summary>
    public float? MaxX { get; set; }

    /// <summary>
    /// Minimum Y coordinate (top boundary).
    /// </summary>
    public float? MinY { get; set; }

    /// <summary>
    /// Maximum Y coordinate (bottom boundary).
    /// </summary>
    public float? MaxY { get; set; }

    /// <summary>
    /// Maximum allowed velocity magnitude (speed limit).
    /// </summary>
    public float? MaxVelocity { get; set; }

    /// <summary>
    /// Initializes a new movement constraint with optional boundaries.
    /// </summary>
    public MovementConstraint(float? minX = null, float? maxX = null, float? minY = null, float? maxY = null, float? maxVelocity = null)
    {
        MinX = minX;
        MaxX = maxX;
        MinY = minY;
        MaxY = maxY;
        MaxVelocity = maxVelocity;
    }

    /// <summary>
    /// Creates a rectangular boundary constraint.
    /// </summary>
    /// <param name="x">Left coordinate.</param>
    /// <param name="y">Top coordinate.</param>
    /// <param name="width">Width of the boundary.</param>
    /// <param name="height">Height of the boundary.</param>
    /// <returns>A movement constraint with rectangular boundaries.</returns>
    public static MovementConstraint Rectangle(float x, float y, float width, float height)
    {
        return new MovementConstraint(x, x + width, y, y + height);
    }

    /// <summary>
    /// Creates a speed limit constraint (no boundaries).
    /// </summary>
    /// <param name="maxSpeed">Maximum allowed speed.</param>
    /// <returns>A movement constraint with only velocity limiting.</returns>
    public static MovementConstraint SpeedLimit(float maxSpeed)
    {
        return new MovementConstraint(maxVelocity: maxSpeed);
    }

    /// <summary>
    /// Checks if the constraint has any boundary limits.
    /// </summary>
    public readonly bool HasBoundaries => MinX.HasValue || MaxX.HasValue || MinY.HasValue || MaxY.HasValue;

    /// <summary>
    /// Checks if the constraint has a velocity limit.
    /// </summary>
    public readonly bool HasVelocityLimit => MaxVelocity.HasValue;

    public override readonly string ToString()
    {
        var bounds = HasBoundaries ? $"Bounds[{MinX},{MaxX},{MinY},{MaxY}]" : "NoBounds";
        var speed = HasVelocityLimit ? $" MaxSpeed={MaxVelocity:F2}" : "";
        return $"{bounds}{speed}";
    }
}
