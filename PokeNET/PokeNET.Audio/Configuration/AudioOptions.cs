using System.ComponentModel.DataAnnotations;

namespace PokeNET.Audio.Configuration;

/// <summary>
/// Audio quality levels for playback
/// </summary>
public enum AudioQuality
{
    /// <summary>Low quality - 22050 Hz, reduced buffer size</summary>
    Low = 0,

    /// <summary>Medium quality - 44100 Hz, standard buffer</summary>
    Medium = 1,

    /// <summary>High quality - 48000 Hz, larger buffer</summary>
    High = 2,
}

/// <summary>
/// Audio configuration options loaded from appsettings.json
/// </summary>
public class AudioOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Audio";

    /// <summary>
    /// Enable or disable background music playback
    /// </summary>
    public bool EnableMusic { get; set; } = true;

    /// <summary>
    /// Enable or disable sound effects playback
    /// </summary>
    public bool EnableSoundEffects { get; set; } = true;

    /// <summary>
    /// Audio quality setting
    /// </summary>
    public AudioQuality Quality { get; set; } = AudioQuality.Medium;

    /// <summary>
    /// Maximum number of sounds that can play simultaneously
    /// </summary>
    [Range(1, 64)]
    public int MaxConcurrentSounds { get; set; } = 16;

    /// <summary>
    /// Audio buffer size in samples (per channel)
    /// </summary>
    [Range(128, 8192)]
    public int BufferSize { get; set; } = 2048;

    /// <summary>
    /// Sample rate in Hz
    /// </summary>
    [Range(8000, 96000)]
    public int SampleRate { get; set; } = 44100;

    /// <summary>
    /// Default master volume (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public float DefaultMasterVolume { get; set; } = 0.8f;

    /// <summary>
    /// Default music volume (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public float DefaultMusicVolume { get; set; } = 0.7f;

    /// <summary>
    /// Default sound effects volume (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public float DefaultSfxVolume { get; set; } = 1.0f;

    /// <summary>
    /// Enable audio compression (reduces memory usage)
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Enable audio caching for frequently played sounds
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Maximum cache size in MB
    /// </summary>
    [Range(1, 512)]
    public int MaxCacheSizeMB { get; set; } = 64;

    /// <summary>
    /// Apply quality-specific settings to this options instance
    /// </summary>
    public void ApplyQualityPreset()
    {
        switch (Quality)
        {
            case AudioQuality.Low:
                SampleRate = 22050;
                BufferSize = 1024;
                EnableCompression = true;
                MaxConcurrentSounds = 8;
                MaxCacheSizeMB = 32;
                break;

            case AudioQuality.Medium:
                SampleRate = 44100;
                BufferSize = 2048;
                EnableCompression = true;
                MaxConcurrentSounds = 16;
                MaxCacheSizeMB = 64;
                break;

            case AudioQuality.High:
                SampleRate = 48000;
                BufferSize = 4096;
                EnableCompression = false;
                MaxConcurrentSounds = 32;
                MaxCacheSizeMB = 128;
                break;
        }
    }

    /// <summary>
    /// Validate the configuration options
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (MaxConcurrentSounds < 1 || MaxConcurrentSounds > 64)
        {
            errors.Add($"MaxConcurrentSounds must be between 1 and 64, got {MaxConcurrentSounds}");
        }

        if (BufferSize < 128 || BufferSize > 8192)
        {
            errors.Add($"BufferSize must be between 128 and 8192, got {BufferSize}");
        }

        if (SampleRate < 8000 || SampleRate > 96000)
        {
            errors.Add($"SampleRate must be between 8000 and 96000, got {SampleRate}");
        }

        if (DefaultMasterVolume < 0.0f || DefaultMasterVolume > 1.0f)
        {
            errors.Add(
                $"DefaultMasterVolume must be between 0.0 and 1.0, got {DefaultMasterVolume}"
            );
        }

        if (DefaultMusicVolume < 0.0f || DefaultMusicVolume > 1.0f)
        {
            errors.Add($"DefaultMusicVolume must be between 0.0 and 1.0, got {DefaultMusicVolume}");
        }

        if (DefaultSfxVolume < 0.0f || DefaultSfxVolume > 1.0f)
        {
            errors.Add($"DefaultSfxVolume must be between 0.0 and 1.0, got {DefaultSfxVolume}");
        }

        if (MaxCacheSizeMB < 1 || MaxCacheSizeMB > 512)
        {
            errors.Add($"MaxCacheSizeMB must be between 1 and 512, got {MaxCacheSizeMB}");
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Create a deep copy of these options
    /// </summary>
    public AudioOptions Clone()
    {
        return new AudioOptions
        {
            EnableMusic = EnableMusic,
            EnableSoundEffects = EnableSoundEffects,
            Quality = Quality,
            MaxConcurrentSounds = MaxConcurrentSounds,
            BufferSize = BufferSize,
            SampleRate = SampleRate,
            DefaultMasterVolume = DefaultMasterVolume,
            DefaultMusicVolume = DefaultMusicVolume,
            DefaultSfxVolume = DefaultSfxVolume,
            EnableCompression = EnableCompression,
            EnableCaching = EnableCaching,
            MaxCacheSizeMB = MaxCacheSizeMB,
        };
    }
}
