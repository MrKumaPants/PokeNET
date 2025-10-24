using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PokeNET.Audio.Models;

namespace PokeNET.Audio.Mixing
{
    /// <summary>
    /// Interface for fade management service
    /// </summary>
    public interface IFadeManager : IDisposable
    {
        /// <summary>
        /// Fades a channel to a target volume over a duration
        /// </summary>
        Task FadeChannelAsync(AudioChannel channel, ChannelType type, float targetVolume, float duration);

        /// <summary>
        /// Fades a channel in from 0 to target volume
        /// </summary>
        Task FadeInAsync(AudioChannel channel, ChannelType type, float targetVolume, float duration);

        /// <summary>
        /// Fades a channel out to 0
        /// </summary>
        Task FadeOutAsync(AudioChannel channel, ChannelType type, float duration);

        /// <summary>
        /// Cancels all active fade operations
        /// </summary>
        void CancelAllFades();
    }

    /// <summary>
    /// Manages async fade operations for audio channels
    /// SOLID PRINCIPLE: Single Responsibility - Only handles fade operations
    /// </summary>
    public class FadeManager : IFadeManager
    {
        private readonly ILogger<FadeManager> _logger;
        private readonly Dictionary<ChannelType, CancellationTokenSource> _activeFades;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the FadeManager class
        /// </summary>
        public FadeManager(ILogger<FadeManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeFades = new Dictionary<ChannelType, CancellationTokenSource>();
        }

        /// <summary>
        /// Fades a channel to a target volume over a duration
        /// </summary>
        public async Task FadeChannelAsync(AudioChannel channel, ChannelType type, float targetVolume, float duration)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            if (duration <= 0)
            {
                channel.Volume = targetVolume;
                return;
            }

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
                await PerformFadeAsync(channel, type, targetVolume, duration, cts.Token);
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
        public async Task FadeInAsync(AudioChannel channel, ChannelType type, float targetVolume, float duration)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            channel.Volume = 0.0f;
            await FadeChannelAsync(channel, type, targetVolume, duration);
        }

        /// <summary>
        /// Fades a channel out to 0
        /// </summary>
        public async Task FadeOutAsync(AudioChannel channel, ChannelType type, float duration)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            await FadeChannelAsync(channel, type, 0.0f, duration);
        }

        /// <summary>
        /// Cancels all active fade operations
        /// </summary>
        public void CancelAllFades()
        {
            foreach (var cts in _activeFades.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _activeFades.Clear();
            _logger.LogDebug("All fade operations cancelled");
        }

        /// <summary>
        /// Performs the actual fade operation
        /// </summary>
        private async Task PerformFadeAsync(AudioChannel channel, ChannelType type, float targetVolume,
            float duration, CancellationToken cancellationToken)
        {
            var startVolume = channel.Volume;
            var elapsed = 0f;
            const float frameTime = 0.016f; // ~60 FPS
            const int frameDelay = 16; // milliseconds

            while (elapsed < duration && !cancellationToken.IsCancellationRequested)
            {
                elapsed += frameTime;
                var t = Math.Min(elapsed / duration, 1.0f);
                var newVolume = startVolume + (targetVolume - startVolume) * t;
                channel.Volume = newVolume;

                await Task.Delay(frameDelay, cancellationToken);
            }

            // Ensure we hit the exact target
            if (!cancellationToken.IsCancellationRequested)
            {
                channel.Volume = targetVolume;
                _logger.LogDebug("Channel {Type} faded to {Volume} over {Duration}s",
                    type, targetVolume, duration);
            }
        }

        /// <summary>
        /// Disposes the fade manager and cancels all active operations
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            CancelAllFades();
            _disposed = true;
            _logger.LogDebug("FadeManager disposed");
        }
    }
}
