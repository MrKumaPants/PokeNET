using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PokeNET.Audio.Configuration;

/// <summary>
/// Manages audio configuration from appsettings.json and runtime settings
/// </summary>
public class AudioConfiguration
{
    private readonly ILogger<AudioConfiguration>? _logger;
    private readonly AudioOptions _options;
    private readonly AudioSettings _settings;
    private readonly string? _settingsFilePath;

    /// <summary>
    /// Static configuration options from appsettings.json
    /// </summary>
    public AudioOptions Options => _options;

    /// <summary>
    /// Runtime settings that can be changed during gameplay
    /// </summary>
    public AudioSettings Settings => _settings;

    /// <summary>
    /// Event raised when configuration is reloaded
    /// </summary>
    public event EventHandler? ConfigurationReloaded;

    /// <summary>
    /// Create audio configuration with default values
    /// </summary>
    public AudioConfiguration()
    {
        _options = new AudioOptions();
        _options.ApplyQualityPreset();
        _settings = new AudioSettings();
        _settings.InitializeFromOptions(_options);
    }

    /// <summary>
    /// Create audio configuration from IConfiguration (DI)
    /// </summary>
    public AudioConfiguration(
        IConfiguration configuration,
        ILogger<AudioConfiguration>? logger = null,
        string? settingsFilePath = null)
    {
        _logger = logger;
        _settingsFilePath = settingsFilePath;

        // Load options from configuration
        _options = new AudioOptions();
        configuration.GetSection(AudioOptions.SectionName).Bind(_options);

        // Apply quality preset
        _options.ApplyQualityPreset();

        // Validate configuration
        if (!_options.Validate(out var errors))
        {
            var errorMessage = string.Join(", ", errors);
            _logger?.LogError("Invalid audio configuration: {Errors}", errorMessage);
            throw new InvalidOperationException($"Invalid audio configuration: {errorMessage}");
        }

        _logger?.LogInformation(
            "Audio configuration loaded: Quality={Quality}, SampleRate={SampleRate}, BufferSize={BufferSize}",
            _options.Quality, _options.SampleRate, _options.BufferSize);

        // Initialize settings
        _settings = new AudioSettings();
        _settings.InitializeFromOptions(_options);

        // Subscribe to setting changes for logging
        _settings.SettingChanged += OnSettingChanged;
    }

    /// <summary>
    /// Create audio configuration from options instance
    /// </summary>
    public AudioConfiguration(
        AudioOptions options,
        ILogger<AudioConfiguration>? logger = null,
        string? settingsFilePath = null)
    {
        _logger = logger;
        _settingsFilePath = settingsFilePath;
        _options = options.Clone();

        // Apply quality preset
        _options.ApplyQualityPreset();

        // Validate configuration
        if (!_options.Validate(out var errors))
        {
            var errorMessage = string.Join(", ", errors);
            _logger?.LogError("Invalid audio configuration: {Errors}", errorMessage);
            throw new InvalidOperationException($"Invalid audio configuration: {errorMessage}");
        }

        // Initialize settings
        _settings = new AudioSettings();
        _settings.InitializeFromOptions(_options);
        _settings.SettingChanged += OnSettingChanged;
    }

    /// <summary>
    /// Create audio configuration from IOptions (DI)
    /// </summary>
    public AudioConfiguration(
        IOptions<AudioOptions> options,
        ILogger<AudioConfiguration>? logger = null,
        string? settingsFilePath = null)
        : this(options.Value, logger, settingsFilePath)
    {
    }

    /// <summary>
    /// Update audio quality and apply preset
    /// </summary>
    public void SetQuality(AudioQuality quality)
    {
        _options.Quality = quality;
        _options.ApplyQualityPreset();

        _logger?.LogInformation(
            "Audio quality changed to {Quality}: SampleRate={SampleRate}, BufferSize={BufferSize}",
            quality, _options.SampleRate, _options.BufferSize);

        ConfigurationReloaded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Reload configuration from source
    /// </summary>
    public void Reload(IConfiguration configuration)
    {
        configuration.GetSection(AudioOptions.SectionName).Bind(_options);
        _options.ApplyQualityPreset();

        if (!_options.Validate(out var errors))
        {
            var errorMessage = string.Join(", ", errors);
            _logger?.LogError("Invalid audio configuration after reload: {Errors}", errorMessage);
            throw new InvalidOperationException($"Invalid audio configuration: {errorMessage}");
        }

        _logger?.LogInformation("Audio configuration reloaded");
        ConfigurationReloaded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Reset runtime settings to defaults from options
    /// </summary>
    public void ResetSettings()
    {
        _settings.ResetToDefaults(_options);
        _logger?.LogInformation("Audio settings reset to defaults");
    }

    /// <summary>
    /// Save runtime settings to file
    /// </summary>
    public async Task SaveSettingsAsync(string? filePath = null)
    {
        var path = filePath ?? _settingsFilePath;
        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException("No settings file path specified");
        }

        await _settings.SaveToFileAsync(path);
        _logger?.LogInformation("Audio settings saved to {Path}", path);
    }

    /// <summary>
    /// Load runtime settings from file
    /// </summary>
    public async Task LoadSettingsAsync(string? filePath = null)
    {
        var path = filePath ?? _settingsFilePath;
        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException("No settings file path specified");
        }

        await _settings.LoadFromFileAsync(path);
        _logger?.LogInformation("Audio settings loaded from {Path}", path);
    }

    /// <summary>
    /// Get configuration summary for diagnostics
    /// </summary>
    public string GetConfigurationSummary()
    {
        return $@"Audio Configuration:
  Quality: {_options.Quality}
  Sample Rate: {_options.SampleRate} Hz
  Buffer Size: {_options.BufferSize} samples
  Max Concurrent Sounds: {_options.MaxConcurrentSounds}
  Music Enabled: {_options.EnableMusic}
  SFX Enabled: {_options.EnableSoundEffects}
  Compression: {_options.EnableCompression}
  Caching: {_options.EnableCaching} (Max {_options.MaxCacheSizeMB} MB)

Runtime Settings:
  Master Volume: {_settings.MasterVolume:P0}
  Music Volume: {_settings.MusicVolume:P0} (Effective: {_settings.EffectiveMusicVolume:P0})
  SFX Volume: {_settings.SfxVolume:P0} (Effective: {_settings.EffectiveSfxVolume:P0})
  Music Enabled: {_settings.MusicEnabled}
  SFX Enabled: {_settings.SfxEnabled}";
    }

    /// <summary>
    /// Validate current configuration
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        return _options.Validate(out errors);
    }

    private void OnSettingChanged(object? sender, AudioSettingChangedEventArgs e)
    {
        _logger?.LogDebug(
            "Audio setting changed: {Setting} from {OldValue} to {NewValue}",
            e.SettingName, e.OldValue, e.NewValue);
    }

    /// <summary>
    /// Create a default appsettings.json section example
    /// </summary>
    public static string GetDefaultConfigurationJson()
    {
        return @"{
  ""Audio"": {
    ""EnableMusic"": true,
    ""EnableSoundEffects"": true,
    ""Quality"": ""Medium"",
    ""MaxConcurrentSounds"": 16,
    ""BufferSize"": 2048,
    ""SampleRate"": 44100,
    ""DefaultMasterVolume"": 0.8,
    ""DefaultMusicVolume"": 0.7,
    ""DefaultSfxVolume"": 1.0,
    ""EnableCompression"": true,
    ""EnableCaching"": true,
    ""MaxCacheSizeMB"": 64
  }
}";
    }
}
