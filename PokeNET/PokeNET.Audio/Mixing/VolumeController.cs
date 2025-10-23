using System;
using System.Collections.Generic;
using System.Linq;

namespace PokeNET.Audio.Mixing
{
    /// <summary>
    /// Controls volume normalization and dynamic range compression
    /// </summary>
    public class VolumeController
    {
        private readonly Dictionary<string, float> _peakLevels;
        private readonly Dictionary<string, Queue<float>> _volumeHistory;
        private const int HistorySize = 100;

        /// <summary>
        /// Gets or sets whether normalization is enabled
        /// </summary>
        public bool NormalizationEnabled { get; set; }

        /// <summary>
        /// Gets or sets the target normalization level (0.0 to 1.0)
        /// </summary>
        public float TargetLevel { get; set; }

        /// <summary>
        /// Gets or sets the compression threshold (0.0 to 1.0)
        /// </summary>
        public float CompressionThreshold { get; set; }

        /// <summary>
        /// Gets or sets the compression ratio (1.0 = no compression, higher = more compression)
        /// </summary>
        public float CompressionRatio { get; set; }

        /// <summary>
        /// Gets or sets whether dynamic range compression is enabled
        /// </summary>
        public bool CompressionEnabled { get; set; }

        /// <summary>
        /// Gets or sets the attack time for compression (in seconds)
        /// </summary>
        public float AttackTime { get; set; }

        /// <summary>
        /// Gets or sets the release time for compression (in seconds)
        /// </summary>
        public float ReleaseTime { get; set; }

        /// <summary>
        /// Initializes a new instance of the VolumeController class
        /// </summary>
        public VolumeController()
        {
            _peakLevels = new Dictionary<string, float>();
            _volumeHistory = new Dictionary<string, Queue<float>>();

            NormalizationEnabled = false;
            TargetLevel = 0.8f;
            CompressionThreshold = 0.7f;
            CompressionRatio = 2.0f;
            CompressionEnabled = false;
            AttackTime = 0.01f;
            ReleaseTime = 0.1f;
        }

        /// <summary>
        /// Processes audio volume with normalization and compression
        /// </summary>
        /// <param name="channelId">Unique channel identifier</param>
        /// <param name="inputVolume">Input volume level (0.0 to 1.0)</param>
        /// <param name="deltaTime">Time since last update in seconds</param>
        /// <returns>Processed volume level</returns>
        public float ProcessVolume(string channelId, float inputVolume, float deltaTime)
        {
            if (string.IsNullOrEmpty(channelId))
            {
                throw new ArgumentException("Channel ID cannot be null or empty", nameof(channelId));
            }

            float processedVolume = inputVolume;

            // Track volume history
            TrackVolume(channelId, inputVolume);

            // Apply normalization
            if (NormalizationEnabled)
            {
                processedVolume = ApplyNormalization(channelId, processedVolume);
            }

            // Apply dynamic range compression
            if (CompressionEnabled)
            {
                processedVolume = ApplyCompression(channelId, processedVolume, deltaTime);
            }

            return Math.Clamp(processedVolume, 0.0f, 1.0f);
        }

        /// <summary>
        /// Applies normalization to the input volume
        /// </summary>
        private float ApplyNormalization(string channelId, float inputVolume)
        {
            // Get peak level for this channel
            float peakLevel = GetPeakLevel(channelId);

            if (peakLevel > 0.01f) // Avoid division by very small numbers
            {
                // Calculate normalization gain
                float gain = TargetLevel / peakLevel;

                // Apply gentle normalization (don't boost too aggressively)
                gain = Math.Min(gain, 2.0f); // Max 2x boost

                return inputVolume * gain;
            }

            return inputVolume;
        }

        /// <summary>
        /// Applies dynamic range compression
        /// </summary>
        private float ApplyCompression(string channelId, float inputVolume, float deltaTime)
        {
            if (inputVolume <= CompressionThreshold)
            {
                return inputVolume;
            }

            // Calculate how much we're over the threshold
            float overThreshold = inputVolume - CompressionThreshold;

            // Apply compression ratio
            float compressedOver = overThreshold / CompressionRatio;

            // Calculate output with envelope follower
            float targetVolume = CompressionThreshold + compressedOver;

            // Get current peak level
            if (!_peakLevels.TryGetValue(channelId, out float currentPeak))
            {
                currentPeak = inputVolume;
            }

            // Apply attack/release envelope
            float envelopeTime = inputVolume > currentPeak ? AttackTime : ReleaseTime;
            float alpha = 1.0f - (float)Math.Exp(-deltaTime / envelopeTime);

            float compressedVolume = currentPeak + alpha * (targetVolume - currentPeak);

            // Update peak level
            _peakLevels[channelId] = compressedVolume;

            return compressedVolume;
        }

        /// <summary>
        /// Tracks volume levels for analysis
        /// </summary>
        private void TrackVolume(string channelId, float volume)
        {
            if (!_volumeHistory.TryGetValue(channelId, out var history))
            {
                history = new Queue<float>(HistorySize);
                _volumeHistory[channelId] = history;
            }

            history.Enqueue(volume);

            // Maintain fixed history size
            while (history.Count > HistorySize)
            {
                history.Dequeue();
            }

            // Update peak level from history
            if (history.Count > 0)
            {
                float peak = history.Max();
                _peakLevels[channelId] = peak;
            }
        }

        /// <summary>
        /// Gets the peak level for a channel
        /// </summary>
        public float GetPeakLevel(string channelId)
        {
            return _peakLevels.TryGetValue(channelId, out float peak) ? peak : 0.0f;
        }

        /// <summary>
        /// Gets the average level for a channel
        /// </summary>
        public float GetAverageLevel(string channelId)
        {
            if (_volumeHistory.TryGetValue(channelId, out var history) && history.Count > 0)
            {
                return history.Average();
            }
            return 0.0f;
        }

        /// <summary>
        /// Gets the RMS (Root Mean Square) level for a channel
        /// </summary>
        public float GetRMSLevel(string channelId)
        {
            if (_volumeHistory.TryGetValue(channelId, out var history) && history.Count > 0)
            {
                float sumSquares = history.Sum(v => v * v);
                return (float)Math.Sqrt(sumSquares / history.Count);
            }
            return 0.0f;
        }

        /// <summary>
        /// Resets the volume controller state
        /// </summary>
        public void Reset()
        {
            _peakLevels.Clear();
            _volumeHistory.Clear();
        }

        /// <summary>
        /// Resets state for a specific channel
        /// </summary>
        public void ResetChannel(string channelId)
        {
            _peakLevels.Remove(channelId);
            _volumeHistory.Remove(channelId);
        }

        /// <summary>
        /// Calculates optimal gain for normalization based on channel history
        /// </summary>
        public float CalculateOptimalGain(string channelId)
        {
            float peakLevel = GetPeakLevel(channelId);
            float rmsLevel = GetRMSLevel(channelId);

            if (rmsLevel > 0.01f)
            {
                // Use RMS for more consistent normalization
                float gain = TargetLevel / rmsLevel;

                // Limit gain based on peak to avoid clipping
                if (peakLevel > 0.01f)
                {
                    float peakGain = 1.0f / peakLevel;
                    gain = Math.Min(gain, peakGain);
                }

                return Math.Clamp(gain, 0.1f, 3.0f); // Reasonable gain range
            }

            return 1.0f;
        }

        /// <summary>
        /// Analyzes volume dynamics for a channel
        /// </summary>
        public VolumeAnalysis AnalyzeChannel(string channelId)
        {
            return new VolumeAnalysis
            {
                ChannelId = channelId,
                PeakLevel = GetPeakLevel(channelId),
                AverageLevel = GetAverageLevel(channelId),
                RMSLevel = GetRMSLevel(channelId),
                DynamicRange = CalculateDynamicRange(channelId),
                RecommendedGain = CalculateOptimalGain(channelId)
            };
        }

        /// <summary>
        /// Calculates the dynamic range of a channel
        /// </summary>
        private float CalculateDynamicRange(string channelId)
        {
            if (_volumeHistory.TryGetValue(channelId, out var history) && history.Count > 0)
            {
                float peak = history.Max();
                float min = history.Min();
                return peak - min;
            }
            return 0.0f;
        }

        /// <summary>
        /// Gets the controller configuration
        /// </summary>
        public VolumeControllerConfig GetConfig()
        {
            return new VolumeControllerConfig
            {
                NormalizationEnabled = NormalizationEnabled,
                TargetLevel = TargetLevel,
                CompressionEnabled = CompressionEnabled,
                CompressionThreshold = CompressionThreshold,
                CompressionRatio = CompressionRatio,
                AttackTime = AttackTime,
                ReleaseTime = ReleaseTime
            };
        }

        /// <summary>
        /// Loads controller configuration
        /// </summary>
        public void LoadConfig(VolumeControllerConfig config)
        {
            NormalizationEnabled = config.NormalizationEnabled;
            TargetLevel = config.TargetLevel;
            CompressionEnabled = config.CompressionEnabled;
            CompressionThreshold = config.CompressionThreshold;
            CompressionRatio = config.CompressionRatio;
            AttackTime = config.AttackTime;
            ReleaseTime = config.ReleaseTime;
        }
    }

    /// <summary>
    /// Volume analysis results
    /// </summary>
    public class VolumeAnalysis
    {
        public string ChannelId { get; set; } = string.Empty;
        public float PeakLevel { get; set; }
        public float AverageLevel { get; set; }
        public float RMSLevel { get; set; }
        public float DynamicRange { get; set; }
        public float RecommendedGain { get; set; }
    }

    /// <summary>
    /// Serializable volume controller configuration
    /// </summary>
    public class VolumeControllerConfig
    {
        public bool NormalizationEnabled { get; set; }
        public float TargetLevel { get; set; }
        public bool CompressionEnabled { get; set; }
        public float CompressionThreshold { get; set; }
        public float CompressionRatio { get; set; }
        public float AttackTime { get; set; }
        public float ReleaseTime { get; set; }
    }
}
