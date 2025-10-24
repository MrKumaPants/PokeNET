namespace PokeNET.Audio.Abstractions;

/// <summary>
/// Manages volume control across all audio channels.
/// SOLID PRINCIPLE: Single Responsibility - Handles only volume management and ducking.
/// </summary>
public interface IAudioVolumeManager
{
    /// <summary>
    /// Gets the current master volume (0.0 to 1.0).
    /// </summary>
    float MasterVolume { get; }

    /// <summary>
    /// Gets the current music volume (0.0 to 1.0).
    /// </summary>
    float MusicVolume { get; }

    /// <summary>
    /// Gets the current sound effects volume (0.0 to 1.0).
    /// </summary>
    float SfxVolume { get; }

    /// <summary>
    /// Gets the current ambient volume (0.0 to 1.0).
    /// </summary>
    float AmbientVolume { get; }

    /// <summary>
    /// Gets the original music volume before ducking.
    /// </summary>
    float OriginalMusicVolume { get; }

    /// <summary>
    /// Sets the master volume for all audio channels.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    void SetMasterVolume(float volume);

    /// <summary>
    /// Sets the music volume.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    void SetMusicVolume(float volume);

    /// <summary>
    /// Sets the sound effects volume.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    void SetSfxVolume(float volume);

    /// <summary>
    /// Sets the ambient audio volume.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    void SetAmbientVolume(float volume);

    /// <summary>
    /// Applies volume settings to music and SFX players.
    /// </summary>
    void ApplyVolumesToPlayers();

    /// <summary>
    /// Ducks (lowers) music volume temporarily.
    /// </summary>
    /// <param name="duckAmount">Amount to duck (0.0 to 1.0, where 1.0 is full duck).</param>
    void DuckMusic(float duckAmount);

    /// <summary>
    /// Restores music volume after ducking.
    /// </summary>
    void StopDucking();
}
