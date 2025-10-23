namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Represents friction/damping applied to an entity's velocity.
/// Used to slow down entities over time (air resistance, ground friction, etc.).
/// This component follows the Single Responsibility Principle by only handling friction data.
/// </summary>
public struct Friction
{
    /// <summary>
    /// The friction coefficient (0 = no friction, 1 = immediate stop).
    /// Applied as: velocity *= (1 - coefficient * deltaTime)
    /// </summary>
    public float Coefficient { get; set; }

    /// <summary>
    /// Initializes a new friction with the specified coefficient.
    /// </summary>
    /// <param name="coefficient">The friction coefficient (clamped between 0 and 1).</param>
    public Friction(float coefficient)
    {
        Coefficient = Math.Clamp(coefficient, 0f, 1f);
    }

    /// <summary>
    /// Standard ground friction for walking entities.
    /// </summary>
    public static readonly Friction Ground = new(0.8f);

    /// <summary>
    /// Light air resistance for flying/airborne entities.
    /// </summary>
    public static readonly Friction Air = new(0.1f);

    /// <summary>
    /// Water resistance for swimming entities.
    /// </summary>
    public static readonly Friction Water = new(0.5f);

    /// <summary>
    /// No friction (free movement).
    /// </summary>
    public static readonly Friction None = new(0f);

    public override readonly string ToString() => $"Friction({Coefficient:F2})";
}
