using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Domain.ECS.Events;
using PokeNET.Domain.Models;

namespace PokeNET.Audio.Reactive.Reactions;

/// <summary>
/// Handles audio reactions to game state changes.
/// </summary>
public class GameStateReaction : BaseAudioReaction
{
    public GameStateReaction(ILogger<GameStateReaction> logger)
        : base(logger) { }

    public override int Priority => 10;

    public override bool CanHandle(IGameEvent gameEvent)
    {
        return gameEvent is GameStateChangedEvent;
    }

    public override async Task ReactAsync(
        IGameEvent gameEvent,
        IAudioManager audioManager,
        CancellationToken cancellationToken = default
    )
    {
        if (!ShouldReact(gameEvent))
            return;

        var evt = (GameStateChangedEvent)gameEvent;
        Logger.LogInformation(
            "Game state changed from {PreviousState} to {NewState}",
            evt.PreviousState,
            evt.NewState
        );

        // Handle battle state
        if (evt.NewState == GameState.Battle)
        {
            await audioManager.PlayMusicAsync(
                "audio/music/battle_wild.ogg",
                true,
                cancellationToken
            );
        }
        else if (evt.PreviousState == GameState.Battle && evt.NewState == GameState.Overworld)
        {
            await audioManager.PlayMusicAsync("audio/music/route_01.ogg", true, cancellationToken);
        }
        // Handle menu state
        else if (evt.NewState == GameState.Menu)
        {
            await audioManager.DuckMusicAsync(0.3f, cancellationToken);
        }
        else if (evt.PreviousState == GameState.Menu)
        {
            await audioManager.StopDuckingAsync(cancellationToken);
        }
        // Handle overworld
        else if (evt.NewState == GameState.Overworld)
        {
            await audioManager.PlayMusicAsync("audio/music/route_01.ogg", true, cancellationToken);
        }
        // Handle cutscene
        else if (evt.NewState == GameState.Cutscene)
        {
            Logger.LogInformation("Entered cutscene state");
        }
    }
}
