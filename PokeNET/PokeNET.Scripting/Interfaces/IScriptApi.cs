using PokeNET.Domain.ECS.Events;
using PokeNET.Domain.Modding;

namespace PokeNET.Scripting.Interfaces;

/// <summary>
/// Provides safe, controlled access to the ECS world and game systems for scripts.
/// </summary>
/// <remarks>
/// <para>
/// This API serves as a security boundary between untrusted script code and the
/// core game engine. All methods validate inputs and enforce security policies.
/// </para>
/// <para>
/// Key design principles:
/// - **Defense in depth**: Multiple layers of validation
/// - **Fail-safe defaults**: Operations fail closed on security violations
/// - **Least privilege**: Scripts only get access to what they need
/// </para>
/// </remarks>
public interface IScriptApi
{
    /// <summary>
    /// Gets the entity API for ECS world manipulation.
    /// </summary>
    IEntityApi Entities { get; }

    /// <summary>
    /// Gets the event bus for publishing and subscribing to game events.
    /// </summary>
    IEventBus Events { get; }

    /// <summary>
    /// Creates a new entity with the specified components.
    /// </summary>
    /// <param name="components">Components to attach to the entity.</param>
    /// <returns>The created entity.</returns>
    /// <exception cref="ArgumentNullException">Components array is null.</exception>
    /// <exception cref="InvalidOperationException">Script lacks permission to create entities.</exception>
    /// <example>
    /// <code>
    /// var entity = context.Api.CreateEntity(
    ///     new Position(10, 20),
    ///     new Sprite("custom_sprite.png")
    /// );
    /// </code>
    /// </example>
    Entity CreateEntity(params object[] components);

    /// <summary>
    /// Destroys an entity and removes all its components.
    /// </summary>
    /// <param name="entity">The entity to destroy.</param>
    /// <exception cref="InvalidOperationException">Script lacks permission to destroy entities.</exception>
    void DestroyEntity(Entity entity);

    /// <summary>
    /// Publishes a game event to all registered handlers.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="gameEvent">The event to publish.</param>
    /// <exception cref="ArgumentNullException">Event is null.</exception>
    /// <exception cref="InvalidOperationException">Script lacks permission to publish events.</exception>
    /// <example>
    /// <code>
    /// context.Api.PublishEvent(new CustomGameEvent
    /// {
    ///     Message = "Something happened!"
    /// });
    /// </code>
    /// </example>
    void PublishEvent<T>(T gameEvent) where T : IGameEvent;

    /// <summary>
    /// Subscribes to game events of a specific type.
    /// </summary>
    /// <typeparam name="T">The event type to subscribe to.</typeparam>
    /// <param name="handler">The handler to invoke when events are published.</param>
    /// <exception cref="ArgumentNullException">Handler is null.</exception>
    /// <exception cref="InvalidOperationException">Script lacks permission to subscribe to events.</exception>
    void SubscribeToEvent<T>(Action<T> handler) where T : IGameEvent;

    /// <summary>
    /// Unsubscribes from game events of a specific type.
    /// </summary>
    /// <typeparam name="T">The event type to unsubscribe from.</typeparam>
    /// <param name="handler">The handler to remove.</param>
    void UnsubscribeFromEvent<T>(Action<T> handler) where T : IGameEvent;

    /// <summary>
    /// Logs a message at the specified log level.
    /// </summary>
    /// <param name="level">The log level.</param>
    /// <param name="message">The message to log.</param>
    void Log(LogLevel level, string message);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void LogInfo(string message);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void LogWarning(string message);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void LogError(string message);
}

/// <summary>
/// Log levels for script logging.
/// </summary>
public enum LogLevel
{
    /// <summary>Debug information.</summary>
    Debug = 0,
    /// <summary>Informational messages.</summary>
    Information = 1,
    /// <summary>Warning messages.</summary>
    Warning = 2,
    /// <summary>Error messages.</summary>
    Error = 3
}
