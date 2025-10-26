using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Audio.Reactive.Reactions;

/// <summary>
/// Handles audio reactions to level up events.
/// </summary>
public class LevelUpReaction : BaseAudioReaction
{
    public LevelUpReaction(ILogger<LevelUpReaction> logger)
        : base(logger) { }

    public override int Priority => 4;

    public override bool CanHandle(IGameEvent gameEvent)
    {
        return gameEvent is LevelUpEvent;
    }

    public override async Task ReactAsync(
        IGameEvent gameEvent,
        IAudioManager audioManager,
        CancellationToken cancellationToken = default
    )
    {
        if (!ShouldReact(gameEvent))
            return;

        var evt = (LevelUpEvent)gameEvent;
        Logger.LogInformation("Pokemon leveled up to level {Level}", evt.NewLevel);
        await audioManager.PlaySoundEffectAsync("audio/sfx/level_up.wav", 1.0f, cancellationToken);
    }
}
