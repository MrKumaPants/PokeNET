using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Audio.Reactive.Reactions;

/// <summary>
/// Handles audio reactions to battle start events.
/// </summary>
public class BattleStartReaction : BaseAudioReaction
{
    public BattleStartReaction(ILogger<BattleStartReaction> logger)
        : base(logger) { }

    public override int Priority => 9;

    public override bool CanHandle(IGameEvent gameEvent)
    {
        return gameEvent is BattleStartEvent;
    }

    public override async Task ReactAsync(
        IGameEvent gameEvent,
        IAudioManager audioManager,
        CancellationToken cancellationToken = default
    )
    {
        if (!ShouldReact(gameEvent))
            return;

        var evt = (BattleStartEvent)gameEvent;
        Logger.LogInformation(
            "Battle started: Wild={IsWild}, Gym={IsGym}",
            evt.IsWildBattle,
            evt.IsGymLeader
        );

        // Play battle intro sound
        await audioManager.PlaySoundEffectAsync(
            "audio/sfx/battle_start.wav",
            1.0f,
            cancellationToken
        );

        // Play appropriate battle music
        if (evt.IsGymLeader)
        {
            await audioManager.PlayMusicAsync(
                "audio/music/battle_gym_leader.ogg",
                true,
                cancellationToken
            );
        }
        else if (evt.IsWildBattle)
        {
            await audioManager.PlayMusicAsync(
                "audio/music/battle_wild.ogg",
                true,
                cancellationToken
            );
        }
        else
        {
            await audioManager.PlayMusicAsync(
                "audio/music/battle_trainer.ogg",
                true,
                cancellationToken
            );
        }
    }
}
