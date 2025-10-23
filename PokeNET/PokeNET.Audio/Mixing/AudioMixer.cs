using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PokeNET.Audio.Mixing
{
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
    public class AudioMixer : IAudioMixer
    {
        private readonly Dictionary<ChannelType, AudioChannel> _channels;
        private readonly VolumeController _volumeController;
        private readonly DuckingController _duckingController;
        private float _masterVolume;

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
        public AudioMixer()
        {
            _channels = new Dictionary<ChannelType, AudioChannel>();
            _volumeController = new VolumeController();
            _duckingController = new DuckingController();
            _masterVolume = 1.0f;
            Enabled = true;

            InitializeChannels();
        }

        /// <summary>
        /// Initializes all audio channels
        /// </summary>
        private void InitializeChannels()
        {
            _channels[ChannelType.Master] = new AudioChannel(ChannelType.Master, "Master", 1.0f);
            _channels[ChannelType.Music] = new AudioChannel(ChannelType.Music, "Music", 0.8f);
            _channels[ChannelType.SoundEffects] = new AudioChannel(ChannelType.SoundEffects, "Sound Effects", 1.0f);
            _channels[ChannelType.Voice] = new AudioChannel(ChannelType.Voice, "Voice/Dialogue", 1.0f);
            _channels[ChannelType.Ambient] = new AudioChannel(ChannelType.Ambient, "Ambient", 0.6f);
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
        /// Resets all mixer settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            MasterVolume = 1.0f;
            Enabled = true;

            foreach (var channel in _channels.Values)
            {
                channel.Reset();
            }

            // Reset to default volumes
            _channels[ChannelType.Master].Volume = 1.0f;
            _channels[ChannelType.Music].Volume = 0.8f;
            _channels[ChannelType.SoundEffects].Volume = 1.0f;
            _channels[ChannelType.Voice].Volume = 1.0f;
            _channels[ChannelType.Ambient].Volume = 0.6f;

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
