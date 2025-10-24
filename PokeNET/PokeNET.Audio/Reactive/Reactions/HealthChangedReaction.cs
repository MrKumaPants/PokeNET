using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Audio.Reactive.Reactions;

/// <summary>
/// Handles audio reactions to health changed events.
/// </summary>
public class HealthChangedReaction : BaseAudioReaction
{
    private bool _isLowHealthMusicPlaying;
    private const float LowHealthThreshold = 0.25f;

    public HealthChangedReaction(ILogger<HealthChangedReaction> logger) : base(logger)
    {
    }

    public override int Priority => 8;

    public override bool CanHandle(IGameEvent gameEvent)
    {
        return gameEvent is HealthChangedEvent;
    }

    public override async Task ReactAsync(IGameEvent gameEvent, IAudioManager audioManager, CancellationToken cancellationToken = default)
    {
        if (!ShouldReact(gameEvent))
            return;

        var evt = (HealthChangedEvent)gameEvent;

        if (evt.HealthPercentage <= LowHealthThreshold && !_isLowHealthMusicPlaying)
        {
            Logger.LogWarning("Health critical: {Percentage:P0}", evt.HealthPercentage);
            await audioManager.PlayMusicAsync("audio/music/low_health.ogg", true, cancellationToken);
            _isLowHealthMusicPlaying = true;
        }
        else if (evt.HealthPercentage > LowHealthThreshold && _isLowHealthMusicPlaying)
        {
            Logger.LogInformation("Health restored above critical threshold");
            await audioManager.StopMusicAsync();
            _isLowHealthMusicPlaying = false;
        }
    }
}
