using System;
using System.Collections.Generic;
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
    /// Mixer statistics snapshot
    /// </summary>
    public class MixerStatistics
    {
        public float MasterVolume { get; set; }
        public bool Enabled { get; set; }
        public Dictionary<ChannelType, ChannelStatistics> ChannelStats { get; set; } = new();
    }

    /// <summary>
    /// Channel statistics snapshot
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
    /// Serializable mixer settings for persistence
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
