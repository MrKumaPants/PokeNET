using PokeNET.Audio.Abstractions;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Audio.Reactive;

/// <summary>
/// Strategy pattern interface for audio reactions to game events.
/// SOLID PRINCIPLE: Strategy Pattern - Defines contract for pluggable audio reactions.
/// SOLID PRINCIPLE: Single Responsibility - Each reaction handles one specific event type.
/// SOLID PRINCIPLE: Open/Closed - New reactions can be added without modifying engine.
/// </summary>
public interface IAudioReaction
{
    /// <summary>
    /// Gets the priority of this reaction (higher values processed first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets whether this reaction is currently enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Determines if this reaction can handle the given game event.
    /// </summary>
    /// <param name="gameEvent">The game event to check.</param>
    /// <returns>True if this reaction can handle the event; otherwise, false.</returns>
    bool CanHandle(IGameEvent gameEvent);

    /// <summary>
    /// Reacts to the game event by triggering appropriate audio changes.
    /// </summary>
    /// <param name="gameEvent">The game event to react to.</param>
    /// <param name="audioManager">The audio manager for controlling audio playback.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReactAsync(
        IGameEvent gameEvent,
        IAudioManager audioManager,
        CancellationToken cancellationToken = default
    );
}
