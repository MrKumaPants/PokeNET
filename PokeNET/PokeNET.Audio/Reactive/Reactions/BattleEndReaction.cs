using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Audio.Reactive.Reactions;

/// <summary>
/// Handles audio reactions to battle end events.
/// </summary>
public class BattleEndReaction : BaseAudioReaction
{
    public BattleEndReaction(ILogger<BattleEndReaction> logger) : base(logger)
    {
    }

    public override int Priority => 8;

    public override bool CanHandle(IGameEvent gameEvent)
    {
        return gameEvent is BattleEndEvent;
    }

    public override async Task ReactAsync(IGameEvent gameEvent, IAudioManager audioManager, CancellationToken cancellationToken = default)
    {
        if (!ShouldReact(gameEvent))
            return;

        var evt = (BattleEndEvent)gameEvent;
        Logger.LogInformation("Battle ended: Victory={PlayerWon}", evt.PlayerWon);

        if (evt.PlayerWon)
        {
            await audioManager.PlayMusicAsync("audio/music/victory.ogg", true, cancellationToken);
        }
    }
}
