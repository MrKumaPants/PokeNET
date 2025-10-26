using System;
using Arch.Core;
using Arch.Core.Extensions;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Domain.Input.Commands;

/// <summary>
/// Command for entity interaction (talking to NPCs, picking up items, etc.).
/// </summary>
public class InteractCommand : CommandBase
{
    private readonly Entity _entity;
    private readonly IEventBus? _eventBus;

    /// <inheritdoc/>
    public override int Priority => 20; // Interaction has medium-high priority

    /// <summary>
    /// Initializes a new interact command.
    /// </summary>
    /// <param name="entity">The entity performing the interaction.</param>
    /// <param name="eventBus">Optional event bus for publishing interaction events.</param>
    public InteractCommand(Entity entity, IEventBus? eventBus = null)
    {
        _entity = entity;
        _eventBus = eventBus;
    }

    /// <inheritdoc/>
    public override bool CanExecute(World world)
    {
        if (!base.CanExecute(world))
            return false;

        return world.IsAlive(_entity);
    }

    /// <inheritdoc/>
    public override void Execute(World world)
    {
        // Publish interaction event if event bus is available
        _eventBus?.Publish(new InteractionEvent { Entity = _entity, Timestamp = DateTime.UtcNow });

        // TODO: Implement interaction logic
        // - Raycast or proximity check for nearby interactable entities
        // - Trigger dialogue, pick up items, activate switches, etc.
    }
}

/// <summary>
/// Event published when an entity performs an interaction.
/// </summary>
public class InteractionEvent : IGameEvent
{
    public Entity Entity { get; init; }
    public DateTime Timestamp { get; init; }
}
