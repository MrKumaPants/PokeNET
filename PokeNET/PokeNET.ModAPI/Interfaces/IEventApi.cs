namespace PokeNET.ModAPI.Interfaces;

/// <summary>
/// Provides a type-safe event subscription and publishing system for mod communication.
/// </summary>
/// <remarks>
/// This API enables loose coupling between mods and game systems through events.
/// All event handlers are invoked on the game thread and should be lightweight.
/// </remarks>
/// <example>
/// <code>
/// // Subscribe to an event
/// api.EventApi.Subscribe&lt;PlayerDamagedEvent&gt;(e =>
/// {
///     api.Logger.Info($"Player took {e.Damage} damage!");
/// });
///
/// // Publish a custom event
/// api.EventApi.Publish(new CustomModEvent
/// {
///     ModId = "mymod",
///     Message = "Something happened!"
/// });
///
/// // Unsubscribe when done
/// api.EventApi.Unsubscribe&lt;PlayerDamagedEvent&gt;(myHandler);
/// </code>
/// </example>
public interface IEventApi
{
    /// <summary>
    /// Subscribes to events of the specified type.
    /// </summary>
    /// <typeparam name="T">The event type to subscribe to.</typeparam>
    /// <param name="handler">The handler to invoke when events are published.</param>
    /// <exception cref="ArgumentNullException">Thrown when handler is null.</exception>
    void Subscribe<T>(Action<T> handler) where T : class;

    /// <summary>
    /// Unsubscribes a handler from events of the specified type.
    /// </summary>
    /// <typeparam name="T">The event type to unsubscribe from.</typeparam>
    /// <param name="handler">The handler to remove.</param>
    /// <returns>True if the handler was removed, false if it wasn't subscribed.</returns>
    bool Unsubscribe<T>(Action<T> handler) where T : class;

    /// <summary>
    /// Publishes an event to all subscribed handlers.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="eventData">The event data to publish.</param>
    /// <exception cref="ArgumentNullException">Thrown when eventData is null.</exception>
    void Publish<T>(T eventData) where T : class;

    /// <summary>
    /// Checks if there are any subscribers for the specified event type.
    /// </summary>
    /// <typeparam name="T">The event type to check.</typeparam>
    /// <returns>True if there are subscribers, false otherwise.</returns>
    bool HasSubscribers<T>() where T : class;
}
