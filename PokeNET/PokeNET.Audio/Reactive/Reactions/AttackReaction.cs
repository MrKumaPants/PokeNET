using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Audio.Reactive.Reactions;

/// <summary>
/// Handles audio reactions to attack events.
/// </summary>
public class AttackReaction : BaseAudioReaction
{
    public AttackReaction(ILogger<AttackReaction> logger)
        : base(logger) { }

    public override int Priority => 5;

    public override bool CanHandle(IGameEvent gameEvent)
    {
        return gameEvent is AttackEvent;
    }

    public override async Task ReactAsync(
        IGameEvent gameEvent,
        IAudioManager audioManager,
        CancellationToken cancellationToken = default
    )
    {
        if (!ShouldReact(gameEvent))
            return;

        var evt = (AttackEvent)gameEvent;
        Logger.LogDebug("Attack used: {AttackName} ({AttackType})", evt.AttackName, evt.AttackType);

        // Play type-specific attack sound
        var soundPath = $"audio/sfx/attack_{evt.AttackType?.ToLower() ?? "normal"}.wav";
        await audioManager.PlaySoundEffectAsync(soundPath, 0.8f, cancellationToken);
    }
}
