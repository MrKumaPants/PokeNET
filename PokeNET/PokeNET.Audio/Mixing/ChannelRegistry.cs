using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using PokeNET.Audio.Models;

namespace PokeNET.Audio.Mixing
{
    /// <summary>
    /// Interface for channel registry service
    /// </summary>
    public interface IChannelRegistry
    {
        /// <summary>
        /// Gets all channels
        /// </summary>
        IReadOnlyDictionary<ChannelType, AudioChannel> Channels { get; }

        /// <summary>
        /// Gets a channel by type
        /// </summary>
        AudioChannel GetChannel(ChannelType type);

        /// <summary>
        /// Initializes all audio channels
        /// </summary>
        void InitializeChannels();

        /// <summary>
        /// Mutes all channels except Master
        /// </summary>
        void MuteAll();

        /// <summary>
        /// Unmutes all channels
        /// </summary>
        void UnmuteAll();

        /// <summary>
        /// Mutes all channels except the specified one
        /// </summary>
        void SoloChannel(ChannelType type);

        /// <summary>
        /// Resets all channels to default state
        /// </summary>
        void ResetAll();
    }

    /// <summary>
    /// Manages audio channel lifecycle and registry
    /// SOLID PRINCIPLE: Single Responsibility - Only handles channel registry and bulk operations
    /// </summary>
    public class ChannelRegistry : IChannelRegistry
    {
        private readonly ILogger<ChannelRegistry> _logger;
        private readonly Dictionary<ChannelType, AudioChannel> _channels;

        /// <summary>
        /// Gets all channels
        /// </summary>
        public IReadOnlyDictionary<ChannelType, AudioChannel> Channels => _channels;

        /// <summary>
        /// Initializes a new instance of the ChannelRegistry class
        /// </summary>
        public ChannelRegistry(ILogger<ChannelRegistry> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _channels = new Dictionary<ChannelType, AudioChannel>();
        }

        /// <summary>
        /// Initializes all audio channels with default values
        /// </summary>
        public void InitializeChannels()
        {
            foreach (ChannelType channelType in Enum.GetValues(typeof(ChannelType)))
            {
                var channelName = GetChannelName(channelType);
                var defaultVolume = channelType.GetDefaultVolume();
                _channels[channelType] = new AudioChannel(channelType, channelName, defaultVolume);
            }

            _logger.LogInformation("Initialized {ChannelCount} audio channels", _channels.Count);
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
            _logger.LogDebug("All non-master channels muted");
        }

        /// <summary>
        /// Unmutes all channels
        /// </summary>
        public void UnmuteAll()
        {
            foreach (var channel in _channels.Values)
            {
                channel.IsMuted = false;
            }
            _logger.LogDebug("All channels unmuted");
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

            _logger.LogDebug("Soloed channel {Type}", type);
        }

        /// <summary>
        /// Resets all channels to default state
        /// </summary>
        public void ResetAll()
        {
            foreach (var channel in _channels.Values)
            {
                channel.Reset();
            }
            _logger.LogInformation("All channels reset to defaults");
        }

        /// <summary>
        /// Gets the display name for a channel type
        /// </summary>
        private string GetChannelName(ChannelType channelType)
        {
            return channelType switch
            {
                ChannelType.Master => "Master",
                ChannelType.Music => "Music",
                ChannelType.SoundEffects => "Sound Effects",
                ChannelType.Voice => "Voice/Dialogue",
                ChannelType.Ambient => "Ambient",
                ChannelType.UI => "UI",
                _ => channelType.ToString(),
            };
        }
    }
}
