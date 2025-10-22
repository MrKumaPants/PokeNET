namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Represents the velocity (movement speed and direction) of an entity.
/// This component follows the Single Responsibility Principle by only handling velocity data.
/// </summary>
public struct Velocity
{
    /// <summary>
    /// The velocity in the X direction (pixels per second).
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// The velocity in the Y direction (pixels per second).
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Initializes a new velocity with the specified components.
    /// </summary>
    /// <param name="x">The X component of velocity.</param>
    /// <param name="y">The Y component of velocity.</param>
    public Velocity(float x, float y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Gets the magnitude (speed) of this velocity vector.
    /// </summary>
    public readonly float Magnitude => MathF.Sqrt(X * X + Y * Y);

    /// <summary>
    /// Returns a normalized version of this velocity (direction with magnitude 1).
    /// </summary>
    public readonly Velocity Normalized()
    {
        var mag = Magnitude;
        return mag > 0 ? new Velocity(X / mag, Y / mag) : this;
    }

    public override readonly string ToString() => $"({X:F2}, {Y:F2}) mag={Magnitude:F2}";
}
