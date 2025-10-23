using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PokeNET.Audio.Models;

namespace PokeNET.Audio.Mixing
{
    /// <summary>
    /// Configuration for saving/loading mixer state
    /// </summary>
    public class MixerConfiguration
    {
        public float MasterVolume { get; set; } = 1.0f;
        public IEnumerable<ChannelConfig> Channels { get; set; } = Array.Empty<ChannelConfig>();
    }

    /// <summary>
    /// Interface for audio mixer functionality
    /// </summary>
    public interface IAudioMixer
    {
        /// <summary>
        /// Gets the master volume (0.0 to 1.0)
        /// </summary>
        float MasterVolume { get; set; }

        /// <summary>
        /// Gets a channel by type
        /// </summary>
        AudioChannel GetChannel(ChannelType type);

        /// <summary>
        /// Sets the volume for a specific channel
        /// </summary>
        void SetChannelVolume(ChannelType type, float volume);

        /// <summary>
        /// Gets the volume for a specific channel
        /// </summary>
        float GetChannelVolume(ChannelType type);

        /// <summary>
        /// Mutes or unmutes a channel
        /// </summary>
        void SetChannelMute(ChannelType type, bool muted);

        /// <summary>
        /// Gets whether a channel is muted
        /// </summary>
        bool IsChannelMuted(ChannelType type);

        /// <summary>
        /// Calculates the final volume for a channel (master * channel * ducking)
        /// </summary>
        float GetFinalVolume(ChannelType type);

        /// <summary>
        /// Updates the mixer state
        /// </summary>
        void Update(float deltaTime);

        /// <summary>
        /// Saves mixer settings to file
        /// </summary>
        void SaveSettings(string filePath);

        /// <summary>
        /// Loads mixer settings from file
        /// </summary>
        void LoadSettings(string filePath);

        /// <summary>
        /// Resets all mixer settings to defaults
        /// </summary>
        void ResetToDefaults();
    }

    /// <summary>
    /// Main audio mixer class with volume controls and channel management
    /// </summary>
    public class AudioMixer : IAudioMixer, IDisposable
    {
        private readonly ILogger<AudioMixer> _logger;
        private readonly Dictionary<ChannelType, AudioChannel> _channels;
        private readonly Dictionary<ChannelType, CancellationTokenSource> _activeFades;
        private readonly VolumeController _volumeController;
        private readonly DuckingController _duckingController;
        private float _masterVolume;
        private bool _disposed;

        /// <summary>
        /// Gets or sets the master volume (0.0 to 1.0)
        /// </summary>
        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Math.Clamp(value, 0.0f, 1.0f);
                OnMasterVolumeChanged?.Invoke(this, _masterVolume);
            }
        }

        /// <summary>
        /// Gets the volume controller
        /// </summary>
        public VolumeController VolumeController => _volumeController;

        /// <summary>
        /// Gets the ducking controller
        /// </summary>
        public DuckingController DuckingController => _duckingController;

        /// <summary>
        /// Gets all channels
        /// </summary>
        public IReadOnlyDictionary<ChannelType, AudioChannel> Channels => _channels;

        /// <summary>
        /// Gets or sets whether the mixer is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Event fired when master volume changes
        /// </summary>
        public event EventHandler<float>? OnMasterVolumeChanged;

        /// <summary>
        /// Event fired when mixer settings are loaded
        /// </summary>
        public event EventHandler? OnSettingsLoaded;

        /// <summary>
        /// Event fired when mixer settings are saved
        /// </summary>
        public event EventHandler? OnSettingsSaved;

        /// <summary>
        /// Initializes a new instance of the AudioMixer class
        /// </summary>
        public AudioMixer(ILogger<AudioMixer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _channels = new Dictionary<ChannelType, AudioChannel>();
            _activeFades = new Dictionary<ChannelType, CancellationTokenSource>();
            _volumeController = new VolumeController();
            _duckingController = new DuckingController();
            _masterVolume = 1.0f;
            Enabled = true;

            InitializeChannels();

            _logger.LogInformation("AudioMixer initialized with {ChannelCount} channels", _channels.Count);
        }

        /// <summary>
        /// Initializes all audio channels
        /// </summary>
        private void InitializeChannels()
        {
            // Initialize all channel types
            foreach (ChannelType channelType in Enum.GetValues(typeof(ChannelType)))
            {
                var channelName = channelType switch
                {
                    ChannelType.Master => "Master",
                    ChannelType.Music => "Music",
                    ChannelType.SoundEffects => "Sound Effects",
                    ChannelType.Voice => "Voice/Dialogue",
                    ChannelType.Ambient => "Ambient",
                    ChannelType.UI => "UI",
                    _ => channelType.ToString()
                };

                var defaultVolume = channelType.GetDefaultVolume();
                _channels[channelType] = new AudioChannel(channelType, channelName, defaultVolume);
            }
        }

        /// <summary>
        /// Gets a channel by type
        /// </summary>
        public AudioChannel GetChannel(ChannelType type)
        {
            if (_channels.TryGetValue(type, out var channel))
            {
                return channel;
            }

            throw new ArgumentException($"Channel type {type} not found", nameof(type));
        }

        /// <summary>
        /// Sets the master volume
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            MasterVolume = volume;
            _logger.LogDebug("Master volume set to {Volume}", MasterVolume);
        }

        /// <summary>
        /// Sets the volume for a specific channel
        /// </summary>
        public void SetChannelVolume(ChannelType type, float volume)
        {
            GetChannel(type).Volume = volume;
        }

        /// <summary>
        /// Gets the volume for a specific channel
        /// </summary>
        public float GetChannelVolume(ChannelType type)
        {
            return GetChannel(type).Volume;
        }

        /// <summary>
        /// Mutes or unmutes a channel
        /// </summary>
        public void SetChannelMute(ChannelType type, bool muted)
        {
            GetChannel(type).IsMuted = muted;
        }

        /// <summary>
        /// Gets whether a channel is muted
        /// </summary>
        public bool IsChannelMuted(ChannelType type)
        {
            return GetChannel(type).IsMuted;
        }

        /// <summary>
        /// Mutes a channel
        /// </summary>
        public void MuteChannel(ChannelType type)
        {
            GetChannel(type).IsMuted = true;
            _logger.LogDebug("Channel {Type} muted", type);
        }

        /// <summary>
        /// Unmutes a channel
        /// </summary>
        public void UnmuteChannel(ChannelType type)
        {
            GetChannel(type).IsMuted = false;
            _logger.LogDebug("Channel {Type} unmuted", type);
        }

        /// <summary>
        /// Toggles mute state for a channel
        /// </summary>
        public void ToggleMute(ChannelType type)
        {
            var channel = GetChannel(type);
            channel.IsMuted = !channel.IsMuted;
            _logger.LogDebug("Channel {Type} mute toggled to {IsMuted}", type, channel.IsMuted);
        }

        /// <summary>
        /// Mutes all channels except Master
        /// </summary>
        public void MuteAll()
        {
            foreach (var channel in _channels.Values)
            {
                if (channel.Type != ChannelType.Master)
                {
                    channel.IsMuted = true;
                }
            }
            _logger.LogDebug("All channels muted");
        }

        /// <summary>
        /// Sets ducking on a channel
        /// </summary>
        public void SetDucking(ChannelType type, bool isDucked, float duckLevel = 0.3f)
        {
            var channel = GetChannel(type);
            channel.SetDucking(isDucked, duckLevel);
            _logger.LogDebug("Channel {Type} ducking set to {IsDucked} at level {DuckLevel}",
                type, isDucked, duckLevel);
        }

        /// <summary>
        /// Ducks the music channel (commonly used when voice plays)
        /// </summary>
        public void DuckMusic(float duckLevel = 0.3f)
        {
            SetDucking(ChannelType.Music, true, duckLevel);
            _logger.LogDebug("Music ducked to {DuckLevel}", duckLevel);
        }

        /// <summary>
        /// Stops ducking on a channel
        /// </summary>
        public void StopDucking(ChannelType type)
        {
            var channel = GetChannel(type);
            channel.SetDucking(false, 1.0f);
            _logger.LogDebug("Channel {Type} ducking stopped", type);
        }

        /// <summary>
        /// Gets the effective volume for a channel (master * channel * ducking)
        /// </summary>
        public float GetEffectiveVolume(ChannelType type)
        {
            var channel = GetChannel(type);

            // Don't apply master volume to the master channel itself
            if (type == ChannelType.Master)
            {
                return channel.EffectiveVolume;
            }

            return MasterVolume * channel.EffectiveVolume;
        }

        /// <summary>
        /// Calculates the final volume for a channel
        /// </summary>
        public float GetFinalVolume(ChannelType type)
        {
            if (!Enabled)
            {
                return 0.0f;
            }

            var channel = GetChannel(type);

            // Don't apply master volume to the master channel itself
            if (type == ChannelType.Master)
            {
                return channel.EffectiveVolume;
            }

            // Calculate final volume: Master * Channel * Normalization
            float finalVolume = _masterVolume * channel.EffectiveVolume;

            // Apply volume normalization if enabled
            if (_volumeController.NormalizationEnabled)
            {
                finalVolume = _volumeController.ProcessVolume(
                    $"{type}",
                    finalVolume,
                    0.016f // Approximate delta time for 60fps
                );
            }

            return Math.Clamp(finalVolume, 0.0f, 1.0f);
        }

        /// <summary>
        /// Updates the mixer state
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!Enabled)
            {
                return;
            }

            // Update all channels
            foreach (var channel in _channels.Values)
            {
                channel.Update(deltaTime);
            }

            // Update ducking controller
            _duckingController.Update(_channels.Values, deltaTime);
        }

        /// <summary>
        /// Fades a channel to a target volume over a duration
        /// </summary>
        public async Task FadeChannelAsync(ChannelType type, float targetVolume, float duration)
        {
            // Cancel any existing fade for this channel
            if (_activeFades.TryGetValue(type, out var existingCts))
            {
                existingCts.Cancel();
                existingCts.Dispose();
            }

            var cts = new CancellationTokenSource();
            _activeFades[type] = cts;

            try
            {
                var channel = GetChannel(type);
                var startVolume = channel.Volume;
                var elapsed = 0f;

                while (elapsed < duration && !cts.Token.IsCancellationRequested)
                {
                    elapsed += 0.016f; // Approximate 60 FPS
                    var t = Math.Min(elapsed / duration, 1.0f);
                    var newVolume = startVolume + (targetVolume - startVolume) * t;
                    channel.Volume = newVolume;

                    await Task.Delay(16, cts.Token); // ~60 FPS
                }

                // Ensure we hit the exact target
                if (!cts.Token.IsCancellationRequested)
                {
                    channel.Volume = targetVolume;
                }

                _logger.LogDebug("Channel {Type} faded to {Volume} over {Duration}s",
                    type, targetVolume, duration);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Fade operation cancelled for channel {Type}", type);
            }
            finally
            {
                _activeFades.Remove(type);
                cts.Dispose();
            }
        }

        /// <summary>
        /// Fades a channel in from 0 to target volume
        /// </summary>
        public async Task FadeInAsync(ChannelType type, float targetVolume, float duration)
        {
            var channel = GetChannel(type);
            channel.Volume = 0.0f;
            await FadeChannelAsync(type, targetVolume, duration);
        }

        /// <summary>
        /// Fades a channel out to 0
        /// </summary>
        public async Task FadeOutAsync(ChannelType type, float duration)
        {
            await FadeChannelAsync(type, 0.0f, duration);
        }

        /// <summary>
        /// Fades a channel to a target volume
        /// </summary>
        public void FadeChannel(ChannelType type, float targetVolume, float fadeSpeed = 5.0f)
        {
            GetChannel(type).FadeTo(targetVolume, fadeSpeed);
        }

        /// <summary>
        /// Fades the master volume
        /// </summary>
        public void FadeMaster(float targetVolume, float duration)
        {
            // This would need to be implemented with a coroutine or update loop
            // For now, just set it directly
            MasterVolume = targetVolume;
        }

        /// <summary>
        /// Mutes all channels except the specified one
        /// </summary>
        public void SoloChannel(ChannelType type)
        {
            foreach (var channel in _channels.Values)
            {
                if (channel.Type != type && channel.Type != ChannelType.Master)
                {
                    channel.IsMuted = true;
                }
            }

            if (type != ChannelType.Master)
            {
                GetChannel(type).IsMuted = false;
            }
        }

        /// <summary>
        /// Unmutes all channels
        /// </summary>
        public void UnsoloAll()
        {
            foreach (var channel in _channels.Values)
            {
                channel.IsMuted = false;
            }
        }

        /// <summary>
        /// Gets mixer statistics
        /// </summary>
        public MixerStatistics GetStatistics()
        {
            var stats = new MixerStatistics
            {
                MasterVolume = MasterVolume,
                Enabled = Enabled,
                ChannelStats = new Dictionary<ChannelType, ChannelStatistics>()
            };

            foreach (var kvp in _channels)
            {
                var channel = kvp.Value;
                var analysis = _volumeController.AnalyzeChannel($"{kvp.Key}");

                stats.ChannelStats[kvp.Key] = new ChannelStatistics
                {
                    Volume = channel.Volume,
                    EffectiveVolume = channel.EffectiveVolume,
                    IsMuted = channel.IsMuted,
                    IsDucked = channel.IsDucked,
                    DuckingLevel = channel.DuckingLevel,
                    PeakLevel = analysis.PeakLevel,
                    AverageLevel = analysis.AverageLevel,
                    RMSLevel = analysis.RMSLevel
                };
            }

            return stats;
        }

        /// <summary>
        /// Saves the current mixer configuration
        /// </summary>
        public MixerConfiguration SaveConfiguration()
        {
            var config = new MixerConfiguration
            {
                MasterVolume = MasterVolume,
                Channels = _channels.Select(kvp => new ChannelConfig
                {
                    Type = kvp.Key,
                    Name = kvp.Value.Name,
                    Volume = kvp.Value.Volume,
                    IsMuted = kvp.Value.IsMuted
                }).ToList()
            };

            _logger.LogInformation("Mixer configuration saved");
            return config;
        }

        /// <summary>
        /// Loads a mixer configuration
        /// </summary>
        public void LoadConfiguration(MixerConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            MasterVolume = config.MasterVolume;

            foreach (var channelConfig in config.Channels)
            {
                if (_channels.TryGetValue(channelConfig.Type, out var channel))
                {
                    channel.LoadConfig(channelConfig);
                }
            }

            _logger.LogInformation("Mixer configuration loaded");
        }

        /// <summary>
        /// Saves mixer settings to file
        /// </summary>
        public void SaveSettings(string filePath)
        {
            try
            {
                var settings = new MixerSettings
                {
                    MasterVolume = MasterVolume,
                    Enabled = Enabled,
                    Channels = _channels.Values.Select(c => c.GetConfig()).ToList(),
                    VolumeController = _volumeController.GetConfig(),
                    DuckingController = _duckingController.GetConfig()
                };

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(filePath, json);

                OnSettingsSaved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save mixer settings to {filePath}", ex);
            }
        }

        /// <summary>
        /// Loads mixer settings from file
        /// </summary>
        public void LoadSettings(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Settings file not found: {filePath}");
                }

                var json = File.ReadAllText(filePath);
                var settings = JsonSerializer.Deserialize<MixerSettings>(json);

                if (settings == null)
                {
                    throw new InvalidOperationException("Failed to deserialize mixer settings");
                }

                MasterVolume = settings.MasterVolume;
                Enabled = settings.Enabled;

                // Load channel configurations
                foreach (var channelConfig in settings.Channels)
                {
                    if (_channels.TryGetValue(channelConfig.Type, out var channel))
                    {
                        channel.LoadConfig(channelConfig);
                    }
                }

                // Load controller configurations
                _volumeController.LoadConfig(settings.VolumeController);
                _duckingController.LoadConfig(settings.DuckingController);

                OnSettingsLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load mixer settings from {filePath}", ex);
            }
        }

        /// <summary>
        /// Resets all channels to default state
        /// </summary>
        public void ResetAll()
        {
            MasterVolume = 1.0f;

            foreach (var channel in _channels.Values)
            {
                channel.Reset();
            }

            // Cancel all active fades
            foreach (var cts in _activeFades.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _activeFades.Clear();

            _logger.LogInformation("All mixer channels reset to defaults");
        }

        /// <summary>
        /// Resets all mixer settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            ResetAll();

            // Reset to specific default volumes
            _channels[ChannelType.Master].Volume = 1.0f;
            _channels[ChannelType.Music].Volume = 0.8f;
            _channels[ChannelType.SoundEffects].Volume = 1.0f;
            _channels[ChannelType.Voice].Volume = 1.0f;
            _channels[ChannelType.Ambient].Volume = 0.6f;
            if (_channels.ContainsKey(ChannelType.UI))
            {
                _channels[ChannelType.UI].Volume = 0.6f;
            }

            _volumeController.Reset();
            _duckingController.Reset();
        }

        /// <summary>
        /// Analyzes a specific channel
        /// </summary>
        public VolumeAnalysis AnalyzeChannel(ChannelType type)
        {
            return _volumeController.AnalyzeChannel($"{type}");
        }

        /// <summary>
        /// Disposes the mixer and cancels all active operations
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            foreach (var cts in _activeFades.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _activeFades.Clear();

            _disposed = true;
            _logger.LogDebug("AudioMixer disposed");
        }
    }

    /// <summary>
    /// Mixer statistics
    /// </summary>
    public class MixerStatistics
    {
        public float MasterVolume { get; set; }
        public bool Enabled { get; set; }
        public Dictionary<ChannelType, ChannelStatistics> ChannelStats { get; set; } = new();
    }

    /// <summary>
    /// Channel statistics
    /// </summary>
    public class ChannelStatistics
    {
        public float Volume { get; set; }
        public float EffectiveVolume { get; set; }
        public bool IsMuted { get; set; }
        public bool IsDucked { get; set; }
        public float DuckingLevel { get; set; }
        public float PeakLevel { get; set; }
        public float AverageLevel { get; set; }
        public float RMSLevel { get; set; }
    }

    /// <summary>
    /// Serializable mixer settings
    /// </summary>
    public class MixerSettings
    {
        public float MasterVolume { get; set; }
        public bool Enabled { get; set; }
        public List<ChannelConfig> Channels { get; set; } = new();
        public VolumeControllerConfig VolumeController { get; set; } = new();
        public DuckingControllerConfig DuckingController { get; set; } = new();
    }
}
