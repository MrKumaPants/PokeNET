using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Audio.Reactive.Reactions;

/// <summary>
/// Handles audio reactions to Pokemon caught events.
/// </summary>
public class PokemonCaughtReaction : BaseAudioReaction
{
    public PokemonCaughtReaction(ILogger<PokemonCaughtReaction> logger) : base(logger)
    {
    }

    public override int Priority => 5;

    public override bool CanHandle(IGameEvent gameEvent)
    {
        return gameEvent is PokemonCaughtEvent;
    }

    public override async Task ReactAsync(IGameEvent gameEvent, IAudioManager audioManager, CancellationToken cancellationToken = default)
    {
        if (!ShouldReact(gameEvent))
            return;

        var evt = (PokemonCaughtEvent)gameEvent;
        Logger.LogInformation("Pokemon caught: {PokemonName}", evt.PokemonName);
        await audioManager.PlaySoundEffectAsync("audio/sfx/pokemon_catch.wav", 1.0f, cancellationToken);
    }
}
