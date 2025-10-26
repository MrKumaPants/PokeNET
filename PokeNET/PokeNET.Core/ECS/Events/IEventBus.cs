using System;

namespace PokeNET.Core.ECS.Events;

/// <summary>
/// Event bus for publishing and subscribing to game events.
/// Implements the Observer pattern for event-driven architecture.
/// Follows the Single Responsibility Principle and Dependency Inversion Principle.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Subscribes to events of a specific type.
    /// </summary>
    /// <typeparam name="T">The event type to subscribe to.</typeparam>
    /// <param name="handler">The handler to invoke when events are published.</param>
    void Subscribe<T>(Action<T> handler)
        where T : IGameEvent;

    /// <summary>
    /// Unsubscribes from events of a specific type.
    /// </summary>
    /// <typeparam name="T">The event type to unsubscribe from.</typeparam>
    /// <param name="handler">The handler to remove.</param>
    void Unsubscribe<T>(Action<T> handler)
        where T : IGameEvent;

    /// <summary>
    /// Publishes an event to all subscribed handlers.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="gameEvent">The event to publish.</param>
    void Publish<T>(T gameEvent)
        where T : IGameEvent;

    /// <summary>
    /// Clears all subscriptions.
    /// </summary>
    void Clear();
}
