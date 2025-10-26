using Microsoft.Extensions.Logging;
using PokeNET.Audio.Reactive;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Audio.Services;

/// <summary>
/// Registry for managing audio reactions with query and configuration capabilities.
/// SOLID PRINCIPLE: Single Responsibility - Manages reaction lifecycle and queries.
/// SOLID PRINCIPLE: Dependency Inversion - Depends on IAudioReaction abstraction.
/// </summary>
public class AudioReactionRegistry
{
    private readonly ILogger<AudioReactionRegistry> _logger;
    private readonly List<IAudioReaction> _reactions;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the AudioReactionRegistry class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="reactions">Collection of audio reactions to register.</param>
    public AudioReactionRegistry(
        ILogger<AudioReactionRegistry> logger,
        IEnumerable<IAudioReaction> reactions
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _reactions =
            reactions?.OrderByDescending(r => r.Priority).ToList()
            ?? throw new ArgumentNullException(nameof(reactions));

        _logger.LogInformation(
            "AudioReactionRegistry initialized with {Count} reactions",
            _reactions.Count
        );
    }

    /// <summary>
    /// Gets all registered reactions.
    /// </summary>
    public IReadOnlyList<IAudioReaction> Reactions => _reactions.AsReadOnly();

    /// <summary>
    /// Gets reactions that can handle the specified event type.
    /// </summary>
    /// <param name="gameEvent">The game event to query.</param>
    /// <returns>Collection of reactions that can handle the event.</returns>
    public IEnumerable<IAudioReaction> GetReactionsForEvent(IGameEvent gameEvent)
    {
        lock (_lock)
        {
            return _reactions
                .Where(r => r.IsEnabled && r.CanHandle(gameEvent))
                .OrderByDescending(r => r.Priority)
                .ToList();
        }
    }

    /// <summary>
    /// Enables or disables a specific reaction by type.
    /// </summary>
    /// <typeparam name="T">The reaction type to configure.</typeparam>
    /// <param name="enabled">True to enable, false to disable.</param>
    public void SetReactionEnabled<T>(bool enabled)
        where T : IAudioReaction
    {
        lock (_lock)
        {
            var reaction = _reactions.OfType<T>().FirstOrDefault();
            if (reaction != null)
            {
                reaction.IsEnabled = enabled;
                _logger.LogDebug(
                    "Reaction {ReactionType} set to {Enabled}",
                    typeof(T).Name,
                    enabled
                );
            }
            else
            {
                _logger.LogWarning(
                    "Reaction type {ReactionType} not found in registry",
                    typeof(T).Name
                );
            }
        }
    }

    /// <summary>
    /// Enables or disables all reactions.
    /// </summary>
    /// <param name="enabled">True to enable all, false to disable all.</param>
    public void SetAllReactionsEnabled(bool enabled)
    {
        lock (_lock)
        {
            foreach (var reaction in _reactions)
            {
                reaction.IsEnabled = enabled;
            }
            _logger.LogInformation("All reactions set to {Enabled}", enabled);
        }
    }

    /// <summary>
    /// Gets whether a specific reaction type is enabled.
    /// </summary>
    /// <typeparam name="T">The reaction type to check.</typeparam>
    /// <returns>True if enabled, false otherwise.</returns>
    public bool IsReactionEnabled<T>()
        where T : IAudioReaction
    {
        lock (_lock)
        {
            var reaction = _reactions.OfType<T>().FirstOrDefault();
            return reaction?.IsEnabled ?? false;
        }
    }

    /// <summary>
    /// Gets the count of enabled reactions.
    /// </summary>
    public int EnabledCount
    {
        get
        {
            lock (_lock)
            {
                return _reactions.Count(r => r.IsEnabled);
            }
        }
    }

    /// <summary>
    /// Gets statistics about the registry.
    /// </summary>
    public AudioReactionRegistryStats GetStats()
    {
        lock (_lock)
        {
            return new AudioReactionRegistryStats
            {
                TotalReactions = _reactions.Count,
                EnabledReactions = _reactions.Count(r => r.IsEnabled),
                DisabledReactions = _reactions.Count(r => !r.IsEnabled),
                HighestPriority = _reactions.Any() ? _reactions.Max(r => r.Priority) : 0,
                LowestPriority = _reactions.Any() ? _reactions.Min(r => r.Priority) : 0,
            };
        }
    }
}

/// <summary>
/// Statistics about the audio reaction registry.
/// </summary>
public class AudioReactionRegistryStats
{
    public int TotalReactions { get; init; }
    public int EnabledReactions { get; init; }
    public int DisabledReactions { get; init; }
    public int HighestPriority { get; init; }
    public int LowestPriority { get; init; }
}
