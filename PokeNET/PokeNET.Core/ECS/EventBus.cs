using Microsoft.Extensions.Logging;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Core.ECS;

/// <summary>
/// Thread-safe event bus implementation for game events.
/// Implements the Observer pattern.
/// Follows the Single Responsibility Principle.
/// </summary>
public class EventBus : IEventBus
{
    private readonly ILogger<EventBus> _logger;
    private readonly Dictionary<Type, List<Delegate>> _subscriptions = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new event bus with logging support.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Subscribe<T>(Action<T> handler) where T : IGameEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_lock)
        {
            var eventType = typeof(T);
            if (!_subscriptions.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                _subscriptions[eventType] = handlers;
            }

            handlers.Add(handler);
            _logger.LogDebug("Subscribed handler to event type {EventType}", eventType.Name);
        }
    }

    /// <inheritdoc/>
    public void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_lock)
        {
            var eventType = typeof(T);
            if (_subscriptions.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
                _logger.LogDebug("Unsubscribed handler from event type {EventType}", eventType.Name);

                if (handlers.Count == 0)
                {
                    _subscriptions.Remove(eventType);
                }
            }
        }
    }

    /// <inheritdoc/>
    public void Publish<T>(T gameEvent) where T : IGameEvent
    {
        ArgumentNullException.ThrowIfNull(gameEvent);

        List<Delegate>? handlersCopy;
        lock (_lock)
        {
            var eventType = typeof(T);
            if (!_subscriptions.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
            {
                return;
            }

            // Create a copy to avoid holding the lock during event dispatch
            handlersCopy = new List<Delegate>(handlers);
        }

        _logger.LogTrace("Publishing event {EventType} to {HandlerCount} handlers",
            typeof(T).Name, handlersCopy.Count);

        foreach (var handler in handlersCopy)
        {
            try
            {
                ((Action<T>)handler)(gameEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking event handler for {EventType}", typeof(T).Name);
            }
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        lock (_lock)
        {
            var count = _subscriptions.Count;
            _subscriptions.Clear();
            _logger.LogInformation("Cleared all event subscriptions ({Count} event types)", count);
        }
    }
}
