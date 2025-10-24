namespace PokeNET.Audio.Abstractions;

/// <summary>
/// Manages ambient audio lifecycle and playback.
/// SOLID PRINCIPLE: Single Responsibility - Handles only ambient audio.
/// </summary>
public interface IAmbientAudioManager
{
    /// <summary>
    /// Gets the current ambient track name.
    /// </summary>
    string CurrentAmbientTrack { get; }

    /// <summary>
    /// Gets the current ambient volume.
    /// </summary>
    float CurrentVolume { get; }

    /// <summary>
    /// Plays ambient audio (looping background sounds).
    /// </summary>
    /// <param name="ambientName">The name/path of the ambient audio.</param>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task PlayAsync(string ambientName, float volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses ambient audio playback.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    Task PauseAsync();

    /// <summary>
    /// Resumes paused ambient audio playback.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    Task ResumeAsync();

    /// <summary>
    /// Stops ambient audio playback.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    Task StopAsync();
}
