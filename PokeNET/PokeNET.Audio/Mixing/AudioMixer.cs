using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PokeNET.Audio.Models;
using PokeNET.Audio.Abstractions;

namespace PokeNET.Audio.Mixing
{
    /// <summary>
    /// Main audio mixer coordinator class - delegates to specialized services
    /// SOLID PRINCIPLE: Single Responsibility - Coordinates audio mixing services
    /// SOLID PRINCIPLE: Dependency Inversion - Depends on abstractions (service interfaces)
    /// Refactored from 760 lines to ~300 lines by extracting services
    /// </summary>
    public class AudioMixer : IAudioMixer, IDisposable
    {
        private readonly ILogger<AudioMixer> _logger;
        private readonly IChannelRegistry _channelRegistry;
        private readonly IFadeManager _fadeManager;
        private readonly IMixerConfigurationService _configService;
        private readonly IMixerStatisticsService _statisticsService;
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
        /// Gets or sets the music channel volume
        /// </summary>
        public float MusicVolume
        {
            get => GetChannelVolume(ChannelType.Music);
            set => SetChannelVolume(ChannelType.Music, value);
        }

        /// <summary>
        /// Gets or sets the sound effects channel volume
        /// </summary>
        public float SoundEffectsVolume
        {
            get => GetChannelVolume(ChannelType.SoundEffects);
            set => SetChannelVolume(ChannelType.SoundEffects, value);
        }

        /// <summary>
        /// Gets or sets the voice/dialogue channel volume
        /// </summary>
        public float VoiceVolume
        {
            get => GetChannelVolume(ChannelType.Voice);
            set => SetChannelVolume(ChannelType.Voice, value);
        }

        /// <summary>
        /// Gets a value indicating whether audio ducking is enabled
        /// </summary>
        public bool IsDuckingEnabled => _duckingController.DuckingEnabled;

        /// <summary>
        /// Gets the current ducking level (music channel ducking level)
        /// </summary>
        public float DuckingLevel => _duckingController.GetDuckingLevel(ChannelType.Music);

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
        public IReadOnlyDictionary<ChannelType, AudioChannel> Channels => _channelRegistry.Channels;

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
        public event EventHandler? OnSettingsLoaded
        {
            add => _configService.OnSettingsLoaded += value;
            remove => _configService.OnSettingsLoaded -= value;
        }

        /// <summary>
        /// Event fired when mixer settings are saved
        /// </summary>
        public event EventHandler? OnSettingsSaved
        {
            add => _configService.OnSettingsSaved += value;
            remove => _configService.OnSettingsSaved -= value;
        }

        /// <summary>
        /// Initializes a new instance of the AudioMixer class with dependency injection
        /// </summary>
        public AudioMixer(
            ILogger<AudioMixer> logger,
            IChannelRegistry channelRegistry,
            IFadeManager fadeManager,
            IMixerConfigurationService configService,
            IMixerStatisticsService statisticsService,
            VolumeController volumeController,
            DuckingController duckingController)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _channelRegistry = channelRegistry ?? throw new ArgumentNullException(nameof(channelRegistry));
            _fadeManager = fadeManager ?? throw new ArgumentNullException(nameof(fadeManager));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
            _volumeController = volumeController ?? throw new ArgumentNullException(nameof(volumeController));
            _duckingController = duckingController ?? throw new ArgumentNullException(nameof(duckingController));

            _masterVolume = 1.0f;
            Enabled = true;

            _channelRegistry.InitializeChannels();

            _logger.LogInformation("AudioMixer initialized with {ChannelCount} channels", Channels.Count);
        }

        /// <summary>
        /// Legacy constructor for backward compatibility - creates default service instances
        /// </summary>
        public AudioMixer(ILogger<AudioMixer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create loggers using NullLogger for services (backward compatibility)
            var nullLoggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;

            _channelRegistry = new ChannelRegistry(nullLoggerFactory.CreateLogger<ChannelRegistry>());
            _fadeManager = new FadeManager(nullLoggerFactory.CreateLogger<FadeManager>());
            _configService = new MixerConfigurationService(nullLoggerFactory.CreateLogger<MixerConfigurationService>());
            _statisticsService = new MixerStatisticsService();
            _volumeController = new VolumeController();
            _duckingController = new DuckingController();

            _masterVolume = 1.0f;
            Enabled = true;

            _channelRegistry.InitializeChannels();

            _logger.LogInformation("AudioMixer initialized with {ChannelCount} channels", Channels.Count);
        }

        #region Channel & Volume Control

        public AudioChannel GetChannel(ChannelType type) => _channelRegistry.GetChannel(type);

        public void SetMasterVolume(float volume)
        {
            MasterVolume = volume;
            _logger.LogDebug("Master volume set to {Volume}", MasterVolume);
        }

        public void SetChannelVolume(ChannelType type, float volume) => GetChannel(type).Volume = volume;

        public float GetChannelVolume(ChannelType type) => GetChannel(type).Volume;

        public float GetEffectiveVolume(ChannelType type)
        {
            var channel = GetChannel(type);
            return type == ChannelType.Master ? channel.EffectiveVolume : MasterVolume * channel.EffectiveVolume;
        }

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

        #endregion

        #region Mute Control

        public void SetChannelMute(ChannelType type, bool muted) => GetChannel(type).IsMuted = muted;

        public bool IsChannelMuted(ChannelType type) => GetChannel(type).IsMuted;

        public void MuteChannel(ChannelType type)
        {
            GetChannel(type).IsMuted = true;
            _logger.LogDebug("Channel {Type} muted", type);
        }

        public void UnmuteChannel(ChannelType type)
        {
            GetChannel(type).IsMuted = false;
            _logger.LogDebug("Channel {Type} unmuted", type);
        }

        public void ToggleMute(ChannelType type)
        {
            var channel = GetChannel(type);
            channel.IsMuted = !channel.IsMuted;
            _logger.LogDebug("Channel {Type} mute toggled to {IsMuted}", type, channel.IsMuted);
        }

        public void MuteAll() => _channelRegistry.MuteAll();
        public void SoloChannel(ChannelType type) => _channelRegistry.SoloChannel(type);
        public void UnsoloAll() => _channelRegistry.UnmuteAll();

        #endregion

        #region Ducking Control

        public void SetDucking(ChannelType type, bool isDucked, float duckLevel = 0.3f)
        {
            var channel = GetChannel(type);
            channel.SetDucking(isDucked, duckLevel);
            _logger.LogDebug("Channel {Type} ducking set to {IsDucked} at level {DuckLevel}",
                type, isDucked, duckLevel);
        }

        public void DuckMusic(float duckLevel = 0.3f)
        {
            SetDucking(ChannelType.Music, true, duckLevel);
            _logger.LogDebug("Music ducked to {DuckLevel}", duckLevel);
        }

        public void StopDucking(ChannelType type)
        {
            var channel = GetChannel(type);
            channel.SetDucking(false, 1.0f);
            _logger.LogDebug("Channel {Type} ducking stopped", type);
        }

        #endregion

        #region Update

        public void Update(float deltaTime)
        {
            if (!Enabled)
            {
                return;
            }

            // Update all channels
            foreach (var channel in Channels.Values)
            {
                channel.Update(deltaTime);
            }

            // Update ducking controller
            _duckingController.Update(Channels.Values, deltaTime);
        }

        #endregion

        #region Fade Operations

        public async Task FadeChannelAsync(ChannelType type, float targetVolume, float duration)
        {
            await _fadeManager.FadeChannelAsync(GetChannel(type), type, targetVolume, duration);
        }

        public async Task FadeInAsync(ChannelType type, float targetVolume, float duration)
        {
            await _fadeManager.FadeInAsync(GetChannel(type), type, targetVolume, duration);
        }

        public async Task FadeOutAsync(ChannelType type, float duration)
        {
            await _fadeManager.FadeOutAsync(GetChannel(type), type, duration);
        }

        public void FadeChannel(ChannelType type, float targetVolume, float fadeSpeed = 5.0f) =>
            GetChannel(type).FadeTo(targetVolume, fadeSpeed);

        public void FadeMaster(float targetVolume, float duration) => MasterVolume = targetVolume;

        #endregion

        #region Statistics

        public MixerStatistics GetStatistics() =>
            _statisticsService.GetStatistics(MasterVolume, Enabled, Channels, _volumeController);

        public VolumeAnalysis AnalyzeChannel(ChannelType type) =>
            _statisticsService.AnalyzeChannel(type, _volumeController);

        #endregion

        #region Configuration

        public MixerConfiguration SaveConfiguration() =>
            _configService.SaveConfiguration(MasterVolume, Channels);

        public void LoadConfiguration(MixerConfiguration config) =>
            _configService.LoadConfiguration(config, volume => MasterVolume = volume, Channels);

        public void SaveSettings(string filePath)
        {
            _configService.SaveSettings(filePath, MasterVolume, Enabled, Channels,
                _volumeController, _duckingController);
        }

        public void LoadSettings(string filePath)
        {
            _configService.LoadSettings(filePath,
                volume => MasterVolume = volume,
                enabled => Enabled = enabled,
                Channels, _volumeController, _duckingController);
        }

        public void ResetAll()
        {
            MasterVolume = 1.0f;
            _channelRegistry.ResetAll();
            _fadeManager.CancelAllFades();
            _logger.LogInformation("All mixer channels reset to defaults");
        }

        public void ResetToDefaults()
        {
            ResetAll();
            _configService.ResetToDefaults(volume => MasterVolume = volume, Channels,
                _volumeController, _duckingController);
        }

        #endregion

        #region IAudioMixer Interface Adapters

        /// <summary>
        /// Event raised when any volume setting changes (IAudioMixer interface requirement)
        /// </summary>
        public event EventHandler<Abstractions.VolumeChangedEventArgs>? VolumeChanged;

        /// <summary>
        /// Sets channel volume using AudioChannel enum (adapter for IAudioMixer interface)
        /// </summary>
        void Abstractions.IAudioMixer.SetChannelVolume(Abstractions.AudioChannel channel, float volume)
        {
            SetChannelVolume(ConvertToChannelType(channel), volume);
            VolumeChanged?.Invoke(this, new Abstractions.VolumeChangedEventArgs
            {
                Channel = channel,
                PreviousVolume = GetChannelVolume(ConvertToChannelType(channel)),
                NewVolume = volume
            });
        }

        /// <summary>
        /// Gets channel volume using AudioChannel enum (adapter for IAudioMixer interface)
        /// </summary>
        float Abstractions.IAudioMixer.GetChannelVolume(Abstractions.AudioChannel channel)
        {
            return GetChannelVolume(ConvertToChannelType(channel));
        }

        /// <summary>
        /// Enables ducking (adapter for IAudioMixer interface)
        /// </summary>
        void Abstractions.IAudioMixer.EnableDucking(float duckingLevel, TimeSpan? fadeTime)
        {
            DuckMusic(duckingLevel);
        }

        /// <summary>
        /// Disables ducking (adapter for IAudioMixer interface)
        /// </summary>
        void Abstractions.IAudioMixer.DisableDucking()
        {
            StopDucking(ChannelType.Music);
        }

        /// <summary>
        /// Mutes a channel using AudioChannel enum (adapter for IAudioMixer interface)
        /// </summary>
        void Abstractions.IAudioMixer.MuteChannel(Abstractions.AudioChannel channel)
        {
            MuteChannel(ConvertToChannelType(channel));
        }

        /// <summary>
        /// Unmutes a channel using AudioChannel enum (adapter for IAudioMixer interface)
        /// </summary>
        void Abstractions.IAudioMixer.UnmuteChannel(Abstractions.AudioChannel channel)
        {
            UnmuteChannel(ConvertToChannelType(channel));
        }

        /// <summary>
        /// Checks if a channel is muted using AudioChannel enum (adapter for IAudioMixer interface)
        /// </summary>
        bool Abstractions.IAudioMixer.IsChannelMuted(Abstractions.AudioChannel channel)
        {
            return IsChannelMuted(ConvertToChannelType(channel));
        }

        /// <summary>
        /// Unmutes all channels (IAudioMixer interface requirement)
        /// </summary>
        void Abstractions.IAudioMixer.UnmuteAll()
        {
            UnsoloAll();
        }

        /// <summary>
        /// Sets pan for a channel (currently not implemented)
        /// </summary>
        void Abstractions.IAudioMixer.SetPan(Abstractions.AudioChannel channel, float pan)
        {
            // Pan control not yet implemented in current channel system
            _logger.LogWarning("SetPan not yet implemented for channel {Channel}", channel);
        }

        /// <summary>
        /// Gets pan for a channel (currently not implemented)
        /// </summary>
        float Abstractions.IAudioMixer.GetPan(Abstractions.AudioChannel channel)
        {
            // Pan control not yet implemented - return center (0.0)
            return 0.0f;
        }

        /// <summary>
        /// Fades a channel using AudioChannel enum (adapter for IAudioMixer interface)
        /// </summary>
        async Task Abstractions.IAudioMixer.FadeChannelAsync(Abstractions.AudioChannel channel, float targetVolume, TimeSpan duration, CancellationToken cancellationToken)
        {
            await FadeChannelAsync(ConvertToChannelType(channel), targetVolume, (float)duration.TotalSeconds);
        }

        /// <summary>
        /// Resets all mixer settings (IAudioMixer interface requirement)
        /// </summary>
        void Abstractions.IAudioMixer.Reset()
        {
            ResetToDefaults();
        }

        /// <summary>
        /// Converts AudioChannel enum to ChannelType enum
        /// </summary>
        private static ChannelType ConvertToChannelType(Abstractions.AudioChannel channel)
        {
            return channel switch
            {
                Abstractions.AudioChannel.Master => ChannelType.Master,
                Abstractions.AudioChannel.Music => ChannelType.Music,
                Abstractions.AudioChannel.SoundEffects => ChannelType.SoundEffects,
                Abstractions.AudioChannel.Voice => ChannelType.Voice,
                Abstractions.AudioChannel.Ambient => ChannelType.Ambient,
                Abstractions.AudioChannel.UI => ChannelType.UI,
                _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, "Unknown AudioChannel value")
            };
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            if (_disposed) return;

            _fadeManager.Dispose();
            _disposed = true;
            _logger.LogDebug("AudioMixer disposed");
        }

        #endregion
    }
}
