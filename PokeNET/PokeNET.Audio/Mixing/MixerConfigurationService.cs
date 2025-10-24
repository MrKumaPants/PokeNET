using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PokeNET.Audio.Models;

namespace PokeNET.Audio.Mixing
{
    /// <summary>
    /// Interface for mixer configuration service
    /// </summary>
    public interface IMixerConfigurationService
    {
        /// <summary>
        /// Event fired when settings are loaded
        /// </summary>
        event EventHandler? OnSettingsLoaded;

        /// <summary>
        /// Event fired when settings are saved
        /// </summary>
        event EventHandler? OnSettingsSaved;

        /// <summary>
        /// Saves the current mixer configuration
        /// </summary>
        MixerConfiguration SaveConfiguration(float masterVolume, IReadOnlyDictionary<ChannelType, AudioChannel> channels);

        /// <summary>
        /// Loads a mixer configuration
        /// </summary>
        void LoadConfiguration(MixerConfiguration config, Action<float> setMasterVolume,
            IReadOnlyDictionary<ChannelType, AudioChannel> channels);

        /// <summary>
        /// Saves mixer settings to file
        /// </summary>
        void SaveSettings(string filePath, float masterVolume, bool enabled,
            IReadOnlyDictionary<ChannelType, AudioChannel> channels,
            VolumeController volumeController, DuckingController duckingController);

        /// <summary>
        /// Loads mixer settings from file
        /// </summary>
        void LoadSettings(string filePath, Action<float> setMasterVolume, Action<bool> setEnabled,
            IReadOnlyDictionary<ChannelType, AudioChannel> channels,
            VolumeController volumeController, DuckingController duckingController);

        /// <summary>
        /// Resets mixer to default values
        /// </summary>
        void ResetToDefaults(Action<float> setMasterVolume, IReadOnlyDictionary<ChannelType, AudioChannel> channels,
            VolumeController volumeController, DuckingController duckingController);
    }

    /// <summary>
    /// Manages mixer configuration persistence and defaults
    /// SOLID PRINCIPLE: Single Responsibility - Only handles configuration save/load
    /// </summary>
    public class MixerConfigurationService : IMixerConfigurationService
    {
        private readonly ILogger<MixerConfigurationService> _logger;

        /// <summary>
        /// Event fired when settings are loaded
        /// </summary>
        public event EventHandler? OnSettingsLoaded;

        /// <summary>
        /// Event fired when settings are saved
        /// </summary>
        public event EventHandler? OnSettingsSaved;

        /// <summary>
        /// Initializes a new instance of the MixerConfigurationService class
        /// </summary>
        public MixerConfigurationService(ILogger<MixerConfigurationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Saves the current mixer configuration
        /// </summary>
        public MixerConfiguration SaveConfiguration(float masterVolume, IReadOnlyDictionary<ChannelType, AudioChannel> channels)
        {
            var config = new MixerConfiguration
            {
                MasterVolume = masterVolume,
                Channels = channels.Select(kvp => new ChannelConfig
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
        public void LoadConfiguration(MixerConfiguration config, Action<float> setMasterVolume,
            IReadOnlyDictionary<ChannelType, AudioChannel> channels)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            setMasterVolume(config.MasterVolume);

            foreach (var channelConfig in config.Channels)
            {
                if (channels.TryGetValue(channelConfig.Type, out var channel))
                {
                    channel.LoadConfig(channelConfig);
                }
            }

            _logger.LogInformation("Mixer configuration loaded");
        }

        /// <summary>
        /// Saves mixer settings to file
        /// </summary>
        public void SaveSettings(string filePath, float masterVolume, bool enabled,
            IReadOnlyDictionary<ChannelType, AudioChannel> channels,
            VolumeController volumeController, DuckingController duckingController)
        {
            try
            {
                var settings = new MixerSettings
                {
                    MasterVolume = masterVolume,
                    Enabled = enabled,
                    Channels = channels.Values.Select(c => c.GetConfig()).ToList(),
                    VolumeController = volumeController.GetConfig(),
                    DuckingController = duckingController.GetConfig()
                };

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(filePath, json);

                OnSettingsSaved?.Invoke(this, EventArgs.Empty);
                _logger.LogInformation("Mixer settings saved to {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save mixer settings to {FilePath}", filePath);
                throw new InvalidOperationException($"Failed to save mixer settings to {filePath}", ex);
            }
        }

        /// <summary>
        /// Loads mixer settings from file
        /// </summary>
        public void LoadSettings(string filePath, Action<float> setMasterVolume, Action<bool> setEnabled,
            IReadOnlyDictionary<ChannelType, AudioChannel> channels,
            VolumeController volumeController, DuckingController duckingController)
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

                setMasterVolume(settings.MasterVolume);
                setEnabled(settings.Enabled);

                // Load channel configurations
                foreach (var channelConfig in settings.Channels)
                {
                    if (channels.TryGetValue(channelConfig.Type, out var channel))
                    {
                        channel.LoadConfig(channelConfig);
                    }
                }

                // Load controller configurations
                volumeController.LoadConfig(settings.VolumeController);
                duckingController.LoadConfig(settings.DuckingController);

                OnSettingsLoaded?.Invoke(this, EventArgs.Empty);
                _logger.LogInformation("Mixer settings loaded from {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load mixer settings from {FilePath}", filePath);
                throw new InvalidOperationException($"Failed to load mixer settings from {filePath}", ex);
            }
        }

        /// <summary>
        /// Resets mixer to default values
        /// </summary>
        public void ResetToDefaults(Action<float> setMasterVolume, IReadOnlyDictionary<ChannelType, AudioChannel> channels,
            VolumeController volumeController, DuckingController duckingController)
        {
            setMasterVolume(1.0f);

            // Reset to specific default volumes
            if (channels.TryGetValue(ChannelType.Master, out var master))
                master.Volume = 1.0f;
            if (channels.TryGetValue(ChannelType.Music, out var music))
                music.Volume = 0.8f;
            if (channels.TryGetValue(ChannelType.SoundEffects, out var sfx))
                sfx.Volume = 1.0f;
            if (channels.TryGetValue(ChannelType.Voice, out var voice))
                voice.Volume = 1.0f;
            if (channels.TryGetValue(ChannelType.Ambient, out var ambient))
                ambient.Volume = 0.6f;
            if (channels.TryGetValue(ChannelType.UI, out var ui))
                ui.Volume = 0.6f;

            volumeController.Reset();
            duckingController.Reset();

            _logger.LogInformation("Mixer reset to default settings");
        }
    }
}
