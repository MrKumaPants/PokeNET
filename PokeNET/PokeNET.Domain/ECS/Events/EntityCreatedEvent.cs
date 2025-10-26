using System;
using Arch.Core;

namespace PokeNET.Domain.ECS.Events;

/// <summary>
/// Event published when a new entity is created by a factory.
/// Allows systems to react to entity creation (e.g., initialization, logging, analytics).
/// </summary>
public sealed record EntityCreatedEvent : IGameEvent
{
    /// <summary>
    /// The entity that was created.
    /// </summary>
    public Entity Entity { get; init; }

    /// <summary>
    /// The name/type of the entity template used.
    /// </summary>
    public string EntityType { get; init; }

    /// <summary>
    /// The name of the factory that created the entity.
    /// </summary>
    public string FactoryName { get; init; }

    /// <summary>
    /// When the entity was created.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Number of components added to the entity.
    /// </summary>
    public int ComponentCount { get; init; }

    public EntityCreatedEvent(
        Entity entity,
        string entityType,
        string factoryName,
        int componentCount
    )
    {
        Entity = entity;
        EntityType = entityType;
        FactoryName = factoryName;
        ComponentCount = componentCount;
        Timestamp = DateTime.UtcNow;
    }
}
