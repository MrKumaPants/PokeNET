using System;
using Arch.Core;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Domain.Input.Commands;

/// <summary>
/// Command for pausing/unpausing the game.
/// </summary>
public class PauseCommand : CommandBase
{
    private readonly bool _pause;
    private readonly IEventBus? _eventBus;

    /// <inheritdoc/>
    public override int Priority => 0; // Pause has highest priority (executes first)

    /// <summary>
    /// Initializes a new pause command.
    /// </summary>
    /// <param name="pause">True to pause, false to unpause.</param>
    /// <param name="eventBus">Optional event bus for publishing pause events.</param>
    public PauseCommand(bool pause, IEventBus? eventBus = null)
    {
        _pause = pause;
        _eventBus = eventBus;
    }

    /// <inheritdoc/>
    public override void Execute(World world)
    {
        _eventBus?.Publish(new PauseEvent { IsPaused = _pause, Timestamp = DateTime.UtcNow });

        // TODO: Implement pause logic
        // - Set game state to paused/unpaused
        // - Show/hide pause menu
        // - Disable/enable system updates
    }
}

/// <summary>
/// Event published when the game is paused or unpaused.
/// </summary>
public class PauseEvent : IGameEvent
{
    public bool IsPaused { get; init; }
    public DateTime Timestamp { get; init; }
}
