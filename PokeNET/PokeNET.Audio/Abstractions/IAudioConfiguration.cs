namespace PokeNET.Audio.Abstractions;

/// <summary>
/// Interface for audio system configuration and settings.
/// SOLID PRINCIPLE: Single Responsibility - Manages only configuration concerns.
/// SOLID PRINCIPLE: Open/Closed - Extensible through custom settings dictionary.
/// </summary>
/// <remarks>
/// This interface provides access to audio configuration settings that can be
/// loaded from files, user preferences, or runtime modifications. It supports
/// both typed properties for common settings and a dictionary for custom values.
/// </remarks>
public interface IAudioConfiguration
{
    /// <summary>
    /// Gets or sets the default sample rate for audio playback.
    /// </summary>
    /// <value>Sample rate in Hz (e.g., 44100, 48000).</value>
    int SampleRate { get; set; }

    /// <summary>
    /// Gets or sets the default bit depth for audio.
    /// </summary>
    /// <value>Bit depth (e.g., 16, 24, 32).</value>
    int BitDepth { get; set; }

    /// <summary>
    /// Gets or sets the number of audio channels (1 = mono, 2 = stereo).
    /// </summary>
    int Channels { get; set; }

    /// <summary>
    /// Gets or sets the buffer size for audio playback.
    /// </summary>
    /// <value>Buffer size in samples.</value>
    /// <remarks>
    /// Larger buffers reduce crackling but increase latency.
    /// Typical values: 256, 512, 1024, 2048.
    /// </remarks>
    int BufferSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of simultaneous sound effects.
    /// </summary>
    int MaxSimultaneousSounds { get; set; }

    /// <summary>
    /// Gets or sets the default music volume.
    /// </summary>
    float DefaultMusicVolume { get; set; }

    /// <summary>
    /// Gets or sets the default sound effects volume.
    /// </summary>
    float DefaultSoundEffectsVolume { get; set; }

    /// <summary>
    /// Gets or sets the default master volume.
    /// </summary>
    float DefaultMasterVolume { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether audio ducking is enabled by default.
    /// </summary>
    bool EnableDucking { get; set; }

    /// <summary>
    /// Gets or sets the ducking level (how much to reduce music during SFX).
    /// </summary>
    float DuckingLevel { get; set; }

    /// <summary>
    /// Gets or sets the ducking fade time.
    /// </summary>
    TimeSpan DuckingFadeTime { get; set; }

    /// <summary>
    /// Gets or sets the default crossfade duration for music transitions.
    /// </summary>
    TimeSpan DefaultCrossfadeDuration { get; set; }

    /// <summary>
    /// Gets or sets the audio file base path for loading tracks.
    /// </summary>
    string AudioBasePath { get; set; }

    /// <summary>
    /// Gets or sets the music file subdirectory.
    /// </summary>
    string MusicPath { get; set; }

    /// <summary>
    /// Gets or sets the sound effects file subdirectory.
    /// </summary>
    string SoundEffectsPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to preload all audio files at startup.
    /// </summary>
    bool PreloadAudio { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable procedural music generation.
    /// </summary>
    bool EnableProceduralMusic { get; set; }

    /// <summary>
    /// Gets or sets the audio backend to use (e.g., "OpenAL", "XAudio2", "WASAPI").
    /// </summary>
    string AudioBackend { get; set; }

    /// <summary>
    /// Gets custom configuration values not covered by standard properties.
    /// </summary>
    IReadOnlyDictionary<string, object> CustomSettings { get; }

    /// <summary>
    /// Sets a custom configuration value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    void SetCustomSetting<T>(string key, T value);

    /// <summary>
    /// Gets a custom configuration value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="defaultValue">Default value if key doesn't exist.</param>
    /// <returns>The setting value or default if not found.</returns>
    T GetCustomSetting<T>(string key, T defaultValue = default!);

    /// <summary>
    /// Loads configuration from a file.
    /// </summary>
    /// <param name="filePath">Path to the configuration file.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the load operation.</returns>
    Task LoadAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the current configuration to a file.
    /// </summary>
    /// <param name="filePath">Path where to save the configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the save operation.</returns>
    Task SaveAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    void ResetToDefaults();

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>True if configuration is valid, false otherwise.</returns>
    /// <remarks>
    /// This checks for common issues like invalid sample rates, buffer sizes, etc.
    /// </remarks>
    bool Validate();

    /// <summary>
    /// Gets validation errors if Validate() returns false.
    /// </summary>
    /// <returns>A collection of validation error messages.</returns>
    IReadOnlyCollection<string> GetValidationErrors();

    /// <summary>
    /// Event raised when configuration changes.
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
}

/// <summary>
/// Event arguments for configuration changes.
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the name of the setting that changed.
    /// </summary>
    public string SettingName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the old value.
    /// </summary>
    public object? OldValue { get; init; }

    /// <summary>
    /// Gets the new value.
    /// </summary>
    public object? NewValue { get; init; }

    /// <summary>
    /// Gets the timestamp of the change.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
