using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;

namespace PokeNET.Audio.Services.Music;

/// <summary>
/// Handles music track transitions including crossfades and smooth transitions.
/// Coordinates transitions between tracks with configurable effects.
/// </summary>
public sealed class MusicTransitionHandler : IMusicTransitionHandler
{
    private readonly ILogger<MusicTransitionHandler> _logger;
    private TimeSpan _crossfadeDuration;

    /// <inheritdoc/>
    public event EventHandler<TrackTransitionEventArgs>? TrackTransitioning;

    public MusicTransitionHandler(ILogger<MusicTransitionHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _crossfadeDuration = TimeSpan.FromSeconds(1); // Default 1 second crossfade
    }

    /// <inheritdoc/>
    public TimeSpan CrossfadeDuration
    {
        get => _crossfadeDuration;
        set
        {
            _crossfadeDuration = value;
            _logger.LogDebug("Crossfade duration set to {Duration}ms", value.TotalMilliseconds);
        }
    }

    /// <inheritdoc/>
    public async Task TransitionAsync(
        AudioTrack? fromTrack,
        AudioTrack toTrack,
        bool useCrossfade,
        Func<AudioTrack, CancellationToken, Task> playNewTrackAsync,
        Func<TimeSpan, CancellationToken, Task> fadeOutAsync,
        Func<TimeSpan, CancellationToken, Task> fadeInAsync,
        CancellationToken cancellationToken = default)
    {
        if (toTrack == null)
        {
            throw new ArgumentNullException(nameof(toTrack));
        }

        _logger.LogInformation("Transitioning to music: {TrackName}, Crossfade: {UseCrossfade}",
            toTrack.Name, useCrossfade);

        // Raise transition event
        TrackTransitioning?.Invoke(this, new TrackTransitionEventArgs
        {
            FromTrack = fromTrack,
            ToTrack = toTrack,
            IsCrossfading = useCrossfade,
            Duration = useCrossfade ? _crossfadeDuration : TimeSpan.Zero
        });

        if (useCrossfade && fromTrack != null)
        {
            // Perform crossfade: fade out current track
            await fadeOutAsync(_crossfadeDuration, cancellationToken);
        }

        // Play new track
        await playNewTrackAsync(toTrack, cancellationToken);

        if (useCrossfade)
        {
            // Fade in new track
            await fadeInAsync(_crossfadeDuration, cancellationToken);
        }

        _logger.LogInformation("Transition completed to: {Track}", toTrack.Name);
    }

    /// <inheritdoc/>
    public async Task CrossfadeAsync(
        float currentVolume,
        float targetVolume,
        int crossfadeDurationMs,
        Func<Task> playNewTrackAsync,
        Func<float, float, int, CancellationToken, Task> fadeVolumeAsync,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing crossfade, Duration: {Duration}ms", crossfadeDurationMs);

        // Fade out current track
        await fadeVolumeAsync(currentVolume, 0.0f, crossfadeDurationMs, cancellationToken);

        // Play new track
        await playNewTrackAsync();

        // Fade in new track
        await fadeVolumeAsync(0.0f, targetVolume, crossfadeDurationMs, cancellationToken);

        _logger.LogInformation("Crossfade completed");
    }
}
