namespace PokeNET.Audio.Services.Music;

/// <summary>
/// Manages music volume control, fading, and volume transitions.
/// Handles all volume-related operations including smooth fades.
/// </summary>
public interface IMusicVolumeController
{
    /// <summary>
    /// Gets or sets the current volume (0.0 to 1.0).
    /// </summary>
    float Volume { get; set; }

    /// <summary>
    /// Gets the master music volume from settings.
    /// </summary>
    float MasterVolume { get; }

    /// <summary>
    /// Sets the volume with validation.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0)</param>
    void SetVolume(float volume);

    /// <summary>
    /// Gets the current volume.
    /// </summary>
    float GetVolume();

    /// <summary>
    /// Fades volume from one level to another over a specified duration.
    /// </summary>
    /// <param name="fromVolume">Starting volume</param>
    /// <param name="toVolume">Target volume</param>
    /// <param name="durationMs">Duration in milliseconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task FadeVolumeAsync(
        float fromVolume,
        float toVolume,
        int durationMs,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Fades in from silence to the specified volume.
    /// </summary>
    /// <param name="targetVolume">Target volume</param>
    /// <param name="duration">Fade duration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task FadeInAsync(
        float targetVolume,
        TimeSpan duration,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Fades out from current volume to silence.
    /// </summary>
    /// <param name="duration">Fade duration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task FadeOutAsync(TimeSpan duration, CancellationToken cancellationToken = default);
}
