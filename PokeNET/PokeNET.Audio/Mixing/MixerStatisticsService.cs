using System;
using System.Collections.Generic;
using PokeNET.Audio.Models;

namespace PokeNET.Audio.Mixing
{
    /// <summary>
    /// Interface for mixer statistics service
    /// </summary>
    public interface IMixerStatisticsService
    {
        /// <summary>
        /// Gets comprehensive mixer statistics
        /// </summary>
        MixerStatistics GetStatistics(float masterVolume, bool enabled,
            IReadOnlyDictionary<ChannelType, AudioChannel> channels, VolumeController volumeController);

        /// <summary>
        /// Analyzes a specific channel
        /// </summary>
        VolumeAnalysis AnalyzeChannel(ChannelType type, VolumeController volumeController);
    }

    /// <summary>
    /// Provides mixer statistics and analytics
    /// SOLID PRINCIPLE: Single Responsibility - Only handles statistics gathering and analysis
    /// </summary>
    public class MixerStatisticsService : IMixerStatisticsService
    {
        /// <summary>
        /// Gets comprehensive mixer statistics
        /// </summary>
        public MixerStatistics GetStatistics(float masterVolume, bool enabled,
            IReadOnlyDictionary<ChannelType, AudioChannel> channels, VolumeController volumeController)
        {
            var stats = new MixerStatistics
            {
                MasterVolume = masterVolume,
                Enabled = enabled,
                ChannelStats = new Dictionary<ChannelType, ChannelStatistics>()
            };

            foreach (var kvp in channels)
            {
                var channel = kvp.Value;
                var analysis = volumeController.AnalyzeChannel($"{kvp.Key}");

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
        /// Analyzes a specific channel
        /// </summary>
        public VolumeAnalysis AnalyzeChannel(ChannelType type, VolumeController volumeController)
        {
            return volumeController.AnalyzeChannel($"{type}");
        }
    }
}
