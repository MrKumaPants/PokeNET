using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Audio.Reactive.Reactions;

/// <summary>
/// Handles audio reactions to item use events.
/// </summary>
public class ItemUseReaction : BaseAudioReaction
{
    public ItemUseReaction(ILogger<ItemUseReaction> logger) : base(logger)
    {
    }

    public override int Priority => 3;

    public override bool CanHandle(IGameEvent gameEvent)
    {
        return gameEvent is ItemUseEvent;
    }

    public override async Task ReactAsync(IGameEvent gameEvent, IAudioManager audioManager, CancellationToken cancellationToken = default)
    {
        if (!ShouldReact(gameEvent))
            return;

        var evt = (ItemUseEvent)gameEvent;
        Logger.LogDebug("Item used: {ItemName}", evt.ItemName);
        await audioManager.PlaySoundEffectAsync("audio/sfx/item_use.wav", 0.7f, cancellationToken);
    }
}
