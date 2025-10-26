using System;
using Arch.Core;

namespace PokeNET.Core.ECS.Events;

/// <summary>
/// Event raised when an entity moves.
/// Allows other systems to react to movement (collision detection, animations, etc.).
/// </summary>
public class MovementEvent : IGameEvent
{
    /// <inheritdoc/>
    public DateTime Timestamp { get; }

    /// <summary>
    /// The entity that moved.
    /// </summary>
    public Entity Entity { get; }

    /// <summary>
    /// The previous X position.
    /// </summary>
    public float OldX { get; }

    /// <summary>
    /// The previous Y position.
    /// </summary>
    public float OldY { get; }

    /// <summary>
    /// The new X position.
    /// </summary>
    public float NewX { get; }

    /// <summary>
    /// The new Y position.
    /// </summary>
    public float NewY { get; }

    /// <summary>
    /// The time elapsed during this movement.
    /// </summary>
    public float DeltaTime { get; }

    /// <summary>
    /// Whether the entity was constrained by boundaries during this movement.
    /// </summary>
    public bool WasConstrained { get; }

    public MovementEvent(
        Entity entity,
        float oldX,
        float oldY,
        float newX,
        float newY,
        float deltaTime,
        bool wasConstrained = false
    )
    {
        Timestamp = DateTime.UtcNow;
        Entity = entity;
        OldX = oldX;
        OldY = oldY;
        NewX = newX;
        NewY = newY;
        DeltaTime = deltaTime;
        WasConstrained = wasConstrained;
    }

    /// <summary>
    /// Gets the distance moved during this event.
    /// </summary>
    public float DistanceMoved
    {
        get
        {
            var dx = NewX - OldX;
            var dy = NewY - OldY;
            return MathF.Sqrt(dx * dx + dy * dy);
        }
    }

    public override string ToString() =>
        $"Movement: ({OldX:F2},{OldY:F2}) -> ({NewX:F2},{NewY:F2}) [{DistanceMoved:F2} units]";
}
