using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Core.ECS.Events;

namespace PokeNET.Audio.Reactive.Reactions;

/// <summary>
/// Handles audio reactions to weather changed events.
/// </summary>
public class WeatherChangedReaction : BaseAudioReaction
{
    public WeatherChangedReaction(ILogger<WeatherChangedReaction> logger)
        : base(logger) { }

    public override int Priority => 4;

    public override bool CanHandle(IGameEvent gameEvent)
    {
        return gameEvent is WeatherChangedEvent;
    }

    public override async Task ReactAsync(
        IGameEvent gameEvent,
        IAudioManager audioManager,
        CancellationToken cancellationToken = default
    )
    {
        if (!ShouldReact(gameEvent))
            return;

        var evt = (WeatherChangedEvent)gameEvent;
        Logger.LogInformation("Weather changed to: {Weather}", evt.NewWeather);

        if (evt.NewWeather == Weather.Clear)
        {
            await audioManager.StopAmbientAsync();
        }
        else
        {
            var ambientPath = evt.NewWeather switch
            {
                Weather.Rain => "audio/ambient/rain.ogg",
                Weather.Snow => "audio/ambient/snow.ogg",
                Weather.Sandstorm => "audio/ambient/sandstorm.ogg",
                Weather.Fog => "audio/ambient/fog.ogg",
                _ => null,
            };

            if (ambientPath != null)
            {
                await audioManager.PlayAmbientAsync(ambientPath, 0.6f, cancellationToken);
            }
        }
    }
}
