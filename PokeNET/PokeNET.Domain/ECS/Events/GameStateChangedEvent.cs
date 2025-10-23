using PokeNET.Domain.Models;

namespace PokeNET.Domain.ECS.Events;

/// <summary>
/// Event published when the game state changes.
/// Used by the reactive audio system to trigger appropriate music transitions.
/// </summary>
public class GameStateChangedEvent : IGameEvent
{
    /// <summary>
    /// Gets the previous game state.
    /// </summary>
    public GameState PreviousState { get; init; }

    /// <summary>
    /// Gets the new game state.
    /// </summary>
    public GameState NewState { get; init; }

    /// <summary>
    /// Gets the timestamp when the state changed.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Initializes a new instance of the GameStateChangedEvent class.
    /// </summary>
    /// <param name="previousState">The previous game state.</param>
    /// <param name="newState">The new game state.</param>
    public GameStateChangedEvent(GameState previousState, GameState newState)
    {
        PreviousState = previousState;
        NewState = newState;
    }
}
