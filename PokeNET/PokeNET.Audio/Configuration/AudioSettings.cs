using System.IO;
using System.Text.Json;

namespace PokeNET.Audio.Configuration;

/// <summary>
/// Runtime audio settings that can be changed during gameplay
/// </summary>
public class AudioSettings
{
    private float _masterVolume = 0.8f;
    private float _musicVolume = 0.7f;
    private float _sfxVolume = 1.0f;
    private bool _musicEnabled = true;
    private bool _sfxEnabled = true;

    /// <summary>
    /// Event raised when any setting changes
    /// </summary>
    public event EventHandler<AudioSettingChangedEventArgs>? SettingChanged;

    /// <summary>
    /// Master volume (0.0 to 1.0)
    /// </summary>
    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            var clamped = Math.Clamp(value, 0.0f, 1.0f);
            if (Math.Abs(_masterVolume - clamped) > float.Epsilon)
            {
                var oldValue = _masterVolume;
                _masterVolume = clamped;
                OnSettingChanged(nameof(MasterVolume), oldValue, clamped);
            }
        }
    }

    /// <summary>
    /// Music volume (0.0 to 1.0)
    /// </summary>
    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            var clamped = Math.Clamp(value, 0.0f, 1.0f);
            if (Math.Abs(_musicVolume - clamped) > float.Epsilon)
            {
                var oldValue = _musicVolume;
                _musicVolume = clamped;
                OnSettingChanged(nameof(MusicVolume), oldValue, clamped);
            }
        }
    }

    /// <summary>
    /// Sound effects volume (0.0 to 1.0)
    /// </summary>
    public float SfxVolume
    {
        get => _sfxVolume;
        set
        {
            var clamped = Math.Clamp(value, 0.0f, 1.0f);
            if (Math.Abs(_sfxVolume - clamped) > float.Epsilon)
            {
                var oldValue = _sfxVolume;
                _sfxVolume = clamped;
                OnSettingChanged(nameof(SfxVolume), oldValue, clamped);
            }
        }
    }

    /// <summary>
    /// Whether music is enabled
    /// </summary>
    public bool MusicEnabled
    {
        get => _musicEnabled;
        set
        {
            if (_musicEnabled != value)
            {
                var oldValue = _musicEnabled;
                _musicEnabled = value;
                OnSettingChanged(nameof(MusicEnabled), oldValue, value);
            }
        }
    }

    /// <summary>
    /// Whether sound effects are enabled
    /// </summary>
    public bool SfxEnabled
    {
        get => _sfxEnabled;
        set
        {
            if (_sfxEnabled != value)
            {
                var oldValue = _sfxEnabled;
                _sfxEnabled = value;
                OnSettingChanged(nameof(SfxEnabled), oldValue, value);
            }
        }
    }

    /// <summary>
    /// Get the effective music volume (master * music)
    /// </summary>
    public float EffectiveMusicVolume => MusicEnabled ? MasterVolume * MusicVolume : 0.0f;

    /// <summary>
    /// Get the effective SFX volume (master * sfx)
    /// </summary>
    public float EffectiveSfxVolume => SfxEnabled ? MasterVolume * SfxVolume : 0.0f;

    /// <summary>
    /// Maximum number of concurrent sound effects allowed
    /// </summary>
    public int MaxConcurrentSounds { get; set; } = 32;

    /// <summary>
    /// Sound effect volume (alias for SfxVolume for compatibility)
    /// </summary>
    public float SoundEffectVolume
    {
        get => SfxVolume;
        set => SfxVolume = value;
    }

    /// <summary>
    /// Base path for audio assets
    /// </summary>
    public string AssetBasePath { get; set; } = "Content/Audio";

    /// <summary>
    /// Whether to preload assets at startup
    /// </summary>
    public bool PreloadAssets { get; set; } = false;

    /// <summary>
    /// Whether to preload common assets
    /// </summary>
    public bool PreloadCommonAssets { get; set; } = true;

    /// <summary>
    /// Maximum cache size in megabytes
    /// </summary>
    public long MaxCacheSizeMB { get; set; } = 50;

    /// <summary>
    /// MIDI output device index (0 for default)
    /// </summary>
    public int MidiOutputDevice { get; set; } = 0;

    /// <summary>
    /// Validates the audio settings
    /// </summary>
    public void Validate()
    {
        if (MaxConcurrentSounds < 1 || MaxConcurrentSounds > 256)
        {
            throw new InvalidOperationException(
                $"MaxConcurrentSounds must be between 1 and 256, got {MaxConcurrentSounds}"
            );
        }

        if (MaxCacheSizeMB < 1)
        {
            throw new InvalidOperationException(
                $"MaxCacheSizeMB must be at least 1, got {MaxCacheSizeMB}"
            );
        }

        if (string.IsNullOrWhiteSpace(AssetBasePath))
        {
            throw new InvalidOperationException("AssetBasePath cannot be null or whitespace");
        }
    }

    /// <summary>
    /// Initialize settings from options
    /// </summary>
    public void InitializeFromOptions(AudioOptions options)
    {
        _masterVolume = options.DefaultMasterVolume;
        _musicVolume = options.DefaultMusicVolume;
        _sfxVolume = options.DefaultSfxVolume;
        _musicEnabled = options.EnableMusic;
        _sfxEnabled = options.EnableSoundEffects;
    }

    /// <summary>
    /// Reset to default values
    /// </summary>
    public void ResetToDefaults(AudioOptions options)
    {
        MasterVolume = options.DefaultMasterVolume;
        MusicVolume = options.DefaultMusicVolume;
        SfxVolume = options.DefaultSfxVolume;
        MusicEnabled = options.EnableMusic;
        SfxEnabled = options.EnableSoundEffects;
    }

    /// <summary>
    /// Save settings to JSON file
    /// </summary>
    public async Task SaveToFileAsync(string filePath)
    {
        var data = new
        {
            MasterVolume,
            MusicVolume,
            SfxVolume,
            MusicEnabled,
            SfxEnabled,
        };

        var json = JsonSerializer.Serialize(
            data,
            new JsonSerializerOptions { WriteIndented = true }
        );

        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Load settings from JSON file
    /// </summary>
    public async Task LoadFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            if (data.TryGetProperty("MasterVolume", out var masterVol))
            {
                MasterVolume = masterVol.GetSingle();
            }

            if (data.TryGetProperty("MusicVolume", out var musicVol))
            {
                MusicVolume = musicVol.GetSingle();
            }

            if (data.TryGetProperty("SfxVolume", out var sfxVol))
            {
                SfxVolume = sfxVol.GetSingle();
            }

            if (data.TryGetProperty("MusicEnabled", out var musicEnabled))
            {
                MusicEnabled = musicEnabled.GetBoolean();
            }

            if (data.TryGetProperty("SfxEnabled", out var sfxEnabled))
            {
                SfxEnabled = sfxEnabled.GetBoolean();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to load audio settings from {filePath}",
                ex
            );
        }
    }

    /// <summary>
    /// Create a snapshot of current settings
    /// </summary>
    public AudioSettingsSnapshot CreateSnapshot()
    {
        return new AudioSettingsSnapshot
        {
            MasterVolume = MasterVolume,
            MusicVolume = MusicVolume,
            SfxVolume = SfxVolume,
            MusicEnabled = MusicEnabled,
            SfxEnabled = SfxEnabled,
        };
    }

    /// <summary>
    /// Restore settings from snapshot
    /// </summary>
    public void RestoreFromSnapshot(AudioSettingsSnapshot snapshot)
    {
        MasterVolume = snapshot.MasterVolume;
        MusicVolume = snapshot.MusicVolume;
        SfxVolume = snapshot.SfxVolume;
        MusicEnabled = snapshot.MusicEnabled;
        SfxEnabled = snapshot.SfxEnabled;
    }

    private void OnSettingChanged(string settingName, object oldValue, object newValue)
    {
        SettingChanged?.Invoke(
            this,
            new AudioSettingChangedEventArgs(settingName, oldValue, newValue)
        );
    }
}

/// <summary>
/// Event arguments for audio setting changes
/// </summary>
public class AudioSettingChangedEventArgs : EventArgs
{
    public string SettingName { get; }
    public object OldValue { get; }
    public object NewValue { get; }

    public AudioSettingChangedEventArgs(string settingName, object oldValue, object newValue)
    {
        SettingName = settingName;
        OldValue = oldValue;
        NewValue = newValue;
    }
}

/// <summary>
/// Snapshot of audio settings for save/restore
/// </summary>
public class AudioSettingsSnapshot
{
    public float MasterVolume { get; init; }
    public float MusicVolume { get; init; }
    public float SfxVolume { get; init; }
    public bool MusicEnabled { get; init; }
    public bool SfxEnabled { get; init; }
}
