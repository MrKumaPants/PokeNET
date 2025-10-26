using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Audio.Reactive
{
    /// <summary>
    /// Manages smooth music transitions with crossfading, volume ducking,
    /// and environment-based audio filtering.
    /// </summary>
    public class MusicTransitionManager : IDisposable
    {
        private readonly ILogger<MusicTransitionManager> _logger;
        private readonly Dictionary<string, MusicTrackState> _trackStates;
        private readonly Queue<TransitionCommand> _transitionQueue;

        private MusicTrackState? _currentTrack;
        private MusicTrackState? _targetTrack;
        private TransitionState _transitionState;

        private float _masterVolume;
        private float _duckingMultiplier;
        private float _targetDuckingMultiplier;
        private float _duckingTransitionSpeed;

        private bool _isInitialized;
        private bool _isDisposed;

        /// <summary>
        /// Gets the name of the currently playing track.
        /// </summary>
        public string? CurrentTrack => _currentTrack?.TrackName;

        /// <summary>
        /// Gets whether a transition is currently in progress.
        /// </summary>
        public bool IsTransitioning => _transitionState.IsActive;

        /// <summary>
        /// Gets the current volume including ducking.
        /// </summary>
        public float CurrentVolume => _masterVolume * _duckingMultiplier;

        /// <summary>
        /// Initializes a new instance of the MusicTransitionManager class.
        /// </summary>
        /// <param name="logger">Logger for diagnostics.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        public MusicTransitionManager(ILogger<MusicTransitionManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _trackStates = new Dictionary<string, MusicTrackState>();
            _transitionQueue = new Queue<TransitionCommand>();

            _masterVolume = 1.0f;
            _duckingMultiplier = 1.0f;
            _targetDuckingMultiplier = 1.0f;
            _duckingTransitionSpeed = 2.0f;

            _transitionState = new TransitionState();
        }

        /// <summary>
        /// Initialize the music transition manager.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                _logger.LogWarning("MusicTransitionManager is already initialized");
                return;
            }

            _logger.LogInformation("Initializing MusicTransitionManager...");

            _transitionState.IsActive = false;
            _isInitialized = true;

            _logger.LogInformation("MusicTransitionManager initialized successfully");
        }

        /// <summary>
        /// Transition to a new music track with specified transition type and duration.
        /// </summary>
        public void TransitionToTrack(
            string trackName,
            TransitionType transitionType,
            float duration
        )
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("MusicTransitionManager is not initialized.");
            }

            if (string.IsNullOrEmpty(trackName))
            {
                return;
            }

            // If already on this track, skip transition
            if (_currentTrack != null && _currentTrack.TrackName == trackName)
            {
                return;
            }

            var command = new TransitionCommand
            {
                TargetTrack = trackName,
                TransitionType = transitionType,
                Duration = duration,
                StartTime = 0f,
            };

            // For immediate transitions or if no current track, apply immediately
            if (transitionType == TransitionType.Immediate || _currentTrack == null)
            {
                ExecuteImmediateTransition(command);
            }
            else
            {
                // Queue the transition
                _transitionQueue.Enqueue(command);

                // Start transition if not already transitioning
                if (!_transitionState.IsActive)
                {
                    StartNextTransition();
                }
            }
        }

        /// <summary>
        /// Start the next queued transition.
        /// </summary>
        private void StartNextTransition()
        {
            if (_transitionQueue.Count == 0)
            {
                _transitionState.IsActive = false;
                return;
            }

            var command = _transitionQueue.Dequeue();

            // Get or create track states
            _targetTrack = GetOrCreateTrackState(command.TargetTrack);

            _transitionState.IsActive = true;
            _transitionState.Type = command.TransitionType;
            _transitionState.Duration = command.Duration;
            _transitionState.ElapsedTime = 0f;
            _transitionState.SourceTrack = _currentTrack;
            _transitionState.TargetTrack = _targetTrack;

            // Initialize transition volumes
            if (_currentTrack != null)
            {
                _currentTrack.Volume = 1.0f;
            }
            _targetTrack.Volume = 0.0f;
        }

        /// <summary>
        /// Execute an immediate transition without fading.
        /// </summary>
        private void ExecuteImmediateTransition(TransitionCommand command)
        {
            if (_currentTrack != null)
            {
                _currentTrack.Volume = 0.0f;
                _currentTrack.IsPlaying = false;
            }

            _currentTrack = GetOrCreateTrackState(command.TargetTrack);
            _currentTrack.Volume = 1.0f;
            _currentTrack.IsPlaying = true;

            _transitionState.IsActive = false;
        }

        /// <summary>
        /// Get or create a track state for a given track name.
        /// </summary>
        private MusicTrackState GetOrCreateTrackState(string trackName)
        {
            if (!_trackStates.TryGetValue(trackName, out var trackState))
            {
                trackState = new MusicTrackState
                {
                    TrackName = trackName,
                    Volume = 0.0f,
                    IsPlaying = false,
                };
                _trackStates[trackName] = trackState;
            }

            return trackState;
        }

        /// <summary>
        /// Update the transition manager (call per frame).
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_isInitialized)
            {
                return;
            }

            UpdateDucking(deltaTime);

            if (_transitionState.IsActive)
            {
                UpdateTransition(deltaTime);
            }
        }

        /// <summary>
        /// Update active music transition.
        /// </summary>
        private void UpdateTransition(float deltaTime)
        {
            _transitionState.ElapsedTime += deltaTime;

            float progress = Math.Min(
                _transitionState.ElapsedTime / _transitionState.Duration,
                1.0f
            );

            switch (_transitionState.Type)
            {
                case TransitionType.Crossfade:
                    UpdateCrossfade(progress);
                    break;

                case TransitionType.Fade:
                    UpdateFade(progress);
                    break;

                case TransitionType.Immediate:
                    // Already handled in TransitionToTrack
                    break;
            }

            // Complete transition when finished
            if (progress >= 1.0f)
            {
                CompleteTransition();
            }
        }

        /// <summary>
        /// Update crossfade transition between tracks.
        /// </summary>
        private void UpdateCrossfade(float progress)
        {
            // Use smooth curve for crossfade
            float easedProgress = SmoothStep(progress);

            if (_transitionState.SourceTrack != null)
            {
                _transitionState.SourceTrack.Volume = 1.0f - easedProgress;
            }

            if (_transitionState.TargetTrack != null)
            {
                _transitionState.TargetTrack.Volume = easedProgress;
                _transitionState.TargetTrack.IsPlaying = true;
            }
        }

        /// <summary>
        /// Update fade out then fade in transition.
        /// </summary>
        private void UpdateFade(float progress)
        {
            float halfProgress = progress * 2.0f;

            if (halfProgress <= 1.0f)
            {
                // Fade out current track
                if (_transitionState.SourceTrack != null)
                {
                    _transitionState.SourceTrack.Volume = 1.0f - halfProgress;
                }
            }
            else
            {
                // Fade in new track
                if (_transitionState.SourceTrack != null)
                {
                    _transitionState.SourceTrack.Volume = 0.0f;
                    _transitionState.SourceTrack.IsPlaying = false;
                }

                if (_transitionState.TargetTrack != null)
                {
                    float fadeInProgress = halfProgress - 1.0f;
                    _transitionState.TargetTrack.Volume = fadeInProgress;
                    _transitionState.TargetTrack.IsPlaying = true;
                }
            }
        }

        /// <summary>
        /// Complete the current transition and start the next one if queued.
        /// </summary>
        private void CompleteTransition()
        {
            if (_transitionState.SourceTrack != null)
            {
                _transitionState.SourceTrack.IsPlaying = false;
                _transitionState.SourceTrack.Volume = 0.0f;
            }

            _currentTrack = _transitionState.TargetTrack;

            if (_currentTrack != null)
            {
                _currentTrack.Volume = 1.0f;
                _currentTrack.IsPlaying = true;
            }

            _transitionState.IsActive = false;

            // Start next transition if queued
            if (_transitionQueue.Count > 0)
            {
                StartNextTransition();
            }
        }

        /// <summary>
        /// Duck music volume for sound effects or dialogue.
        /// </summary>
        public void DuckMusic(float targetMultiplier, float transitionTime)
        {
            _targetDuckingMultiplier = Math.Clamp(targetMultiplier, 0.0f, 1.0f);
            _duckingTransitionSpeed = transitionTime > 0 ? 1.0f / transitionTime : 10.0f;
        }

        /// <summary>
        /// Restore music volume after ducking.
        /// </summary>
        public void RestoreMusicVolume(float transitionTime)
        {
            DuckMusic(1.0f, transitionTime);
        }

        /// <summary>
        /// Update volume ducking interpolation.
        /// </summary>
        private void UpdateDucking(float deltaTime)
        {
            if (Math.Abs(_duckingMultiplier - _targetDuckingMultiplier) < 0.001f)
            {
                _duckingMultiplier = _targetDuckingMultiplier;
                return;
            }

            float step = _duckingTransitionSpeed * deltaTime;
            _duckingMultiplier = Lerp(_duckingMultiplier, _targetDuckingMultiplier, step);
        }

        /// <summary>
        /// Adjust music intensity dynamically (for adaptive music).
        /// </summary>
        public void AdjustIntensity(float intensity)
        {
            intensity = Math.Clamp(intensity, 0.0f, 1.0f);

            // This would typically adjust playback layers or parameters
            // For now, we'll use it to slightly modulate volume
            if (_currentTrack != null)
            {
                float intensityMultiplier = 0.8f + (intensity * 0.2f);
                _currentTrack.IntensityMultiplier = intensityMultiplier;
            }
        }

        /// <summary>
        /// Apply environment-based audio filtering.
        /// </summary>
        public void ApplyEnvironmentFilter(EnvironmentFilter filter)
        {
            // This would apply DSP effects in a real implementation
            // For now, store the filter type for reference
            if (_currentTrack != null)
            {
                _currentTrack.EnvironmentFilter = filter;
            }
        }

        /// <summary>
        /// Apply weather-based audio effects.
        /// </summary>
        public void ApplyWeatherEffects(Weather weather)
        {
            // This would apply weather-specific audio effects
            // For now, store the weather type for reference
            if (_currentTrack != null)
            {
                _currentTrack.Weather = weather;
            }
        }

        /// <summary>
        /// Set master volume for all music.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Math.Clamp(volume, 0.0f, 1.0f);
        }

        /// <summary>
        /// Get effective volume for a track (with ducking applied).
        /// </summary>
        public float GetEffectiveVolume(string trackName)
        {
            if (_trackStates.TryGetValue(trackName, out var trackState))
            {
                return trackState.Volume
                    * _duckingMultiplier
                    * _masterVolume
                    * trackState.IntensityMultiplier;
            }

            return 0.0f;
        }

        /// <summary>
        /// Smooth step interpolation function.
        /// </summary>
        private float SmoothStep(float t)
        {
            return t * t * (3.0f - 2.0f * t);
        }

        /// <summary>
        /// Linear interpolation.
        /// </summary>
        private float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Math.Clamp(t, 0.0f, 1.0f);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _trackStates.Clear();
            _transitionQueue.Clear();

            _isDisposed = true;
        }
    }

    #region Supporting Classes and Enums

    public enum TransitionType
    {
        Immediate,
        Fade,
        Crossfade,
    }

    public enum EnvironmentFilter
    {
        None,
        Indoor,
        Outdoor,
        Cave,
        Underwater,
    }

    /// <summary>
    /// Represents the state of a music track.
    /// </summary>
    public class MusicTrackState
    {
        public required string TrackName { get; set; }
        public float Volume { get; set; }
        public bool IsPlaying { get; set; }
        public float IntensityMultiplier { get; set; } = 1.0f;
        public EnvironmentFilter EnvironmentFilter { get; set; }
        public Weather Weather { get; set; }
    }

    /// <summary>
    /// Represents the state of an active transition.
    /// </summary>
    public class TransitionState
    {
        public bool IsActive { get; set; }
        public TransitionType Type { get; set; }
        public float Duration { get; set; }
        public float ElapsedTime { get; set; }
        public MusicTrackState? SourceTrack { get; set; }
        public MusicTrackState? TargetTrack { get; set; }
    }

    /// <summary>
    /// Represents a command to transition to a new track.
    /// </summary>
    public class TransitionCommand
    {
        public required string TargetTrack { get; set; }
        public required TransitionType TransitionType { get; set; }
        public float Duration { get; set; }
        public float StartTime { get; set; }
    }

    #endregion
}
