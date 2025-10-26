using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Core.ECS.Events;

namespace PokeNET.Audio.Reactive.Reactions;

/// <summary>
/// Handles audio reactions to Pokemon faint events.
/// </summary>
public class PokemonFaintReaction : BaseAudioReaction
{
    public PokemonFaintReaction(ILogger<PokemonFaintReaction> logger)
        : base(logger) { }

    public override int Priority => 7;

    public override bool CanHandle(IGameEvent gameEvent)
    {
        return gameEvent is PokemonFaintEvent;
    }

    public override async Task ReactAsync(
        IGameEvent gameEvent,
        IAudioManager audioManager,
        CancellationToken cancellationToken = default
    )
    {
        if (!ShouldReact(gameEvent))
            return;

        var evt = (PokemonFaintEvent)gameEvent;
        Logger.LogInformation("Pokemon fainted: {PokemonName}", evt.PokemonName);
        await audioManager.PlaySoundEffectAsync("audio/sfx/faint.wav", 1.0f, cancellationToken);
    }
}
