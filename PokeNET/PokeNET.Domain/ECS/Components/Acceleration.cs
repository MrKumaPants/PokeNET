namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Represents acceleration (force application) on an entity.
/// Used for gravity, thrust, wind, and other continuous forces.
/// This component follows the Single Responsibility Principle by only handling acceleration data.
/// </summary>
public struct Acceleration
{
    /// <summary>
    /// The acceleration in the X direction (pixels per second squared).
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// The acceleration in the Y direction (pixels per second squared).
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Initializes a new acceleration with the specified components.
    /// </summary>
    /// <param name="x">The X component of acceleration.</param>
    /// <param name="y">The Y component of acceleration.</param>
    public Acceleration(float x, float y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Gets the magnitude of this acceleration vector.
    /// </summary>
    public readonly float Magnitude => MathF.Sqrt(X * X + Y * Y);

    /// <summary>
    /// Creates a gravitational acceleration (downward).
    /// </summary>
    /// <param name="gravity">Gravitational constant (positive = downward).</param>
    /// <returns>An acceleration vector representing gravity.</returns>
    public static Acceleration Gravity(float gravity = 980f) => new(0, gravity);

    public override readonly string ToString() => $"({X:F2}, {Y:F2}) mag={Magnitude:F2}";
}
