namespace PokeNET.Audio.Abstractions;

/// <summary>
/// Provides low-level audio playback functionality.
/// </summary>
public interface IAudioEngine
{
    /// <summary>
    /// Initializes the audio engine.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel initialization</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Plays audio data with specified volume and pitch.
    /// </summary>
    /// <param name="audioData">Raw audio data to play</param>
    /// <param name="volume">Volume level (0.0 to 1.0)</param>
    /// <param name="pitch">Pitch adjustment (1.0 = normal pitch, default: 1.0)</param>
    /// <param name="cancellationToken">Token to cancel playback</param>
    /// <returns>Unique identifier for the playing sound</returns>
    Task<int> PlaySoundAsync(
        byte[] audioData,
        float volume,
        float pitch = 1.0f,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Stops a specific playing sound.
    /// </summary>
    /// <param name="soundId">Identifier of the sound to stop</param>
    Task StopSoundAsync(int soundId);

    /// <summary>
    /// Stops all currently playing sounds.
    /// </summary>
    Task StopAllSoundsAsync();

    /// <summary>
    /// Adjusts the volume of a playing sound.
    /// </summary>
    /// <param name="soundId">Identifier of the sound</param>
    /// <param name="volume">New volume level (0.0 to 1.0)</param>
    Task SetSoundVolumeAsync(int soundId, float volume);

    /// <summary>
    /// Releases all resources used by the audio engine.
    /// </summary>
    void Dispose();
}
