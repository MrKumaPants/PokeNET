using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PokeNET.Audio.Configuration;

namespace PokeNET.Audio.Services.Music;

/// <summary>
/// Manages music volume control, fading, and volume transitions.
/// Handles all volume-related operations including smooth fades.
/// </summary>
public sealed class MusicVolumeController : IMusicVolumeController
{
    private readonly ILogger<MusicVolumeController> _logger;
    private readonly AudioSettings _settings;
    private float _volume;

    public MusicVolumeController(
        ILogger<MusicVolumeController> logger,
        IOptions<AudioSettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _volume = _settings.MusicVolume;
    }

    /// <inheritdoc/>
    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0.0f, 1.0f);
            _logger.LogDebug("Music volume set to {Volume}", _volume);
        }
    }

    /// <inheritdoc/>
    public float MasterVolume => _settings.MusicVolume;

    /// <inheritdoc/>
    public void SetVolume(float volume)
    {
        if (volume < 0.0f || volume > 1.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0.0 and 1.0");
        }

        Volume = volume;
    }

    /// <inheritdoc/>
    public float GetVolume()
    {
        return Volume;
    }

    /// <inheritdoc/>
    public async Task FadeVolumeAsync(float fromVolume, float toVolume, int durationMs, CancellationToken cancellationToken = default)
    {
        const int stepMs = 50; // Update every 50ms
        var steps = durationMs / stepMs;

        if (steps <= 0)
        {
            _volume = toVolume;
            return;
        }

        var volumeStep = (toVolume - fromVolume) / steps;

        _logger.LogDebug("Fading volume from {From} to {To} over {Duration}ms", fromVolume, toVolume, durationMs);

        for (int i = 0; i < steps; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            _volume = fromVolume + (volumeStep * i);
            await Task.Delay(stepMs, cancellationToken);
        }

        _volume = toVolume;
        _logger.LogDebug("Fade complete. Volume: {Volume}", _volume);
    }

    /// <inheritdoc/>
    public async Task FadeInAsync(float targetVolume, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fading in to volume {Volume} over {Duration}ms", targetVolume, duration.TotalMilliseconds);
        await FadeVolumeAsync(0.0f, targetVolume, (int)duration.TotalMilliseconds, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task FadeOutAsync(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fading out from volume {Volume} over {Duration}ms", _volume, duration.TotalMilliseconds);
        await FadeVolumeAsync(_volume, 0.0f, (int)duration.TotalMilliseconds, cancellationToken);
    }
}
