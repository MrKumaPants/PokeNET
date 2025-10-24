using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Audio.Reactive;

/// <summary>
/// Base class for audio reactions with common functionality.
/// SOLID PRINCIPLE: Template Method Pattern - Provides common structure for reactions.
/// </summary>
public abstract class BaseAudioReaction : IAudioReaction
{
    protected readonly ILogger Logger;

    /// <summary>
    /// Initializes a new instance of the BaseAudioReaction class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    protected BaseAudioReaction(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public abstract int Priority { get; }

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public abstract bool CanHandle(IGameEvent gameEvent);

    /// <inheritdoc/>
    public abstract Task ReactAsync(IGameEvent gameEvent, IAudioManager audioManager, CancellationToken cancellationToken = default);

    /// <summary>
    /// Helper method to check if reaction should proceed.
    /// </summary>
    protected bool ShouldReact(IGameEvent gameEvent)
    {
        if (!IsEnabled)
        {
            Logger.LogDebug("Reaction {ReactionType} is disabled, skipping", GetType().Name);
            return false;
        }

        if (!CanHandle(gameEvent))
        {
            return false;
        }

        return true;
    }
}
