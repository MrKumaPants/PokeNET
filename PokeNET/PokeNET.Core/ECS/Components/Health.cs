using System;

namespace PokeNET.Core.ECS.Components;

/// <summary>
/// Represents the health/hit points of an entity.
/// This component follows the Single Responsibility Principle by only handling health data.
/// </summary>
public struct Health
{
    /// <summary>
    /// The current health value.
    /// </summary>
    public int Current { get; set; }

    /// <summary>
    /// The maximum health value.
    /// </summary>
    public int Maximum { get; set; }

    /// <summary>
    /// Initializes a new health component with the specified maximum.
    /// Current health starts at maximum.
    /// </summary>
    /// <param name="maximum">The maximum health value.</param>
    public Health(int maximum)
    {
        Maximum = maximum;
        Current = maximum;
    }

    /// <summary>
    /// Initializes a new health component with specified current and maximum values.
    /// </summary>
    /// <param name="current">The current health value.</param>
    /// <param name="maximum">The maximum health value.</param>
    public Health(int current, int maximum)
    {
        Current = current;
        Maximum = maximum;
    }

    /// <summary>
    /// Gets whether the entity is still alive (health > 0).
    /// </summary>
    public readonly bool IsAlive => Current > 0;

    /// <summary>
    /// Gets the health percentage (0.0 to 1.0).
    /// </summary>
    public readonly float Percentage => Maximum > 0 ? (float)Current / Maximum : 0f;

    /// <summary>
    /// Applies damage to this health component.
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
    public void TakeDamage(int amount)
    {
        Current = Math.Max(0, Current - amount);
    }

    /// <summary>
    /// Heals this health component.
    /// </summary>
    /// <param name="amount">The amount of healing to apply.</param>
    public void Heal(int amount)
    {
        Current = Math.Min(Maximum, Current + amount);
    }

    public override readonly string ToString() => $"{Current}/{Maximum} ({Percentage:P0})";
}
