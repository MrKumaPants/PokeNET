using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Audio.Reactive.Reactions;

/// <summary>
/// Handles audio reactions to critical hit events.
/// </summary>
public class CriticalHitReaction : BaseAudioReaction
{
    public CriticalHitReaction(ILogger<CriticalHitReaction> logger)
        : base(logger) { }

    public override int Priority => 6;

    public override bool CanHandle(IGameEvent gameEvent)
    {
        return gameEvent is CriticalHitEvent;
    }

    public override async Task ReactAsync(
        IGameEvent gameEvent,
        IAudioManager audioManager,
        CancellationToken cancellationToken = default
    )
    {
        if (!ShouldReact(gameEvent))
            return;

        Logger.LogInformation("Critical hit!");
        await audioManager.PlaySoundEffectAsync("audio/sfx/critical.wav", 1.0f, cancellationToken);
    }
}
