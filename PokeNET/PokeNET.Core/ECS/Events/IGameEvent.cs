using System;

namespace PokeNET.Core.ECS.Events;

/// <summary>
/// Base interface for all game events.
/// Events are used for loosely-coupled communication between systems.
/// Follows the Interface Segregation Principle.
/// </summary>
public interface IGameEvent
{
    /// <summary>
    /// Gets the timestamp when the event was created.
    /// </summary>
    DateTime Timestamp { get; }
}
