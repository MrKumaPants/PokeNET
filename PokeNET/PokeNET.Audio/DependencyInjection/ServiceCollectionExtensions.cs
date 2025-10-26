using Microsoft.Extensions.DependencyInjection;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Infrastructure;
using PokeNET.Audio.Reactive;
using PokeNET.Audio.Reactive.Reactions;
using PokeNET.Audio.Services;
using PokeNET.Audio.Services.Managers;

namespace PokeNET.Audio.DependencyInjection;

/// <summary>
/// Extension methods for registering audio services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all audio services to the dependency injection container.
    /// Registers core players, managers, and the orchestrating AudioManager.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddAudioServices(this IServiceCollection services)
    {
        // Register MIDI output device wrapper (for testability)
        services.AddSingleton<IMidiOutputDevice>(sp => OutputDeviceWrapper.GetByIndex(0)); // Default MIDI device

        // Register core audio services
        services.AddSingleton<IAudioCache, AudioCache>();
        services.AddSingleton<IMusicPlayer, MusicPlayer>();
        services.AddSingleton<ISoundEffectPlayer, SoundEffectPlayer>();

        // Register specialized managers (SRP compliance)
        services.AddSingleton<IAudioVolumeManager, AudioVolumeManager>();
        services.AddSingleton<IAudioStateManager, AudioStateManager>();
        services.AddSingleton<IAudioCacheCoordinator, AudioCacheCoordinator>();
        services.AddSingleton<IAmbientAudioManager, AmbientAudioManager>();

        // Register orchestrating AudioManager (Facade pattern)
        services.AddSingleton<IAudioManager, AudioManager>();

        // Register all audio reactions (Strategy pattern)
        services.AddSingleton<Reactive.IAudioReaction, GameStateReaction>();
        services.AddSingleton<Reactive.IAudioReaction, BattleStartReaction>();
        services.AddSingleton<Reactive.IAudioReaction, BattleEndReaction>();
        services.AddSingleton<Reactive.IAudioReaction, PokemonFaintReaction>();
        services.AddSingleton<Reactive.IAudioReaction, AttackReaction>();
        services.AddSingleton<Reactive.IAudioReaction, CriticalHitReaction>();
        services.AddSingleton<Reactive.IAudioReaction, HealthChangedReaction>();
        services.AddSingleton<Reactive.IAudioReaction, WeatherChangedReaction>();
        services.AddSingleton<Reactive.IAudioReaction, ItemUseReaction>();
        services.AddSingleton<Reactive.IAudioReaction, PokemonCaughtReaction>();
        services.AddSingleton<Reactive.IAudioReaction, LevelUpReaction>();

        // Register reaction registry
        services.AddSingleton<AudioReactionRegistry>();

        // Register reactive audio engine
        services.AddSingleton<ReactiveAudioEngine>();

        return services;
    }

    /// <summary>
    /// Adds audio services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddAudioServices(
        this IServiceCollection services,
        Action<AudioServicesOptions> configure
    )
    {
        var options = new AudioServicesOptions();
        configure(options);

        // Apply options-based registration here if needed in the future

        return services.AddAudioServices();
    }
}

/// <summary>
/// Configuration options for audio services.
/// </summary>
public class AudioServicesOptions
{
    /// <summary>
    /// Gets or sets the maximum cache size in bytes.
    /// </summary>
    public long MaxCacheSize { get; set; } = 100 * 1024 * 1024; // 100MB default

    /// <summary>
    /// Gets or sets the default master volume.
    /// </summary>
    public float DefaultMasterVolume { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets whether to enable audio caching.
    /// </summary>
    public bool EnableCaching { get; set; } = true;
}
