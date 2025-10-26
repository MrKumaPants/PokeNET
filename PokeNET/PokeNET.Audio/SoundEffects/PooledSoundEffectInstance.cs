using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace PokeNET.Audio.SoundEffects
{
    /// <summary>
    /// Wrapper for SoundEffectInstance that supports pooling, 3D audio, and category management
    /// </summary>
    public class PooledSoundEffectInstance : IDisposable
    {
        private readonly SoundEffectInstance _instance;
        private readonly SoundCategory _category;
        private bool _disposed;
        private bool _is3D;
        private float _baseVolume;

        public SoundEffectInstance NativeInstance => _instance;
        public SoundCategory Category => _category;
        public bool Is3D => _is3D;
        public float BaseVolume => _baseVolume;

        /// <summary>
        /// Gets the source sound effect (if available through reflection or caching)
        /// </summary>
        public SoundEffect SourceEffect { get; private set; }

        public SoundState State => _instance?.State ?? SoundState.Stopped;

        public bool IsLooped
        {
            get => _instance?.IsLooped ?? false;
            set
            {
                if (_instance != null)
                    _instance.IsLooped = value;
            }
        }

        public float Pan
        {
            get => _instance?.Pan ?? 0.0f;
            set
            {
                if (_instance != null)
                    _instance.Pan = Math.Clamp(value, -1.0f, 1.0f);
            }
        }

        public float Pitch
        {
            get => _instance?.Pitch ?? 0.0f;
            set
            {
                if (_instance != null)
                    _instance.Pitch = Math.Clamp(value, -1.0f, 1.0f);
            }
        }

        public float Volume
        {
            get => _instance?.Volume ?? 0.0f;
            set
            {
                if (_instance != null)
                {
                    _instance.Volume = Math.Clamp(value, 0.0f, 1.0f);
                    _baseVolume = value;
                }
            }
        }

        public PooledSoundEffectInstance(SoundEffectInstance instance, SoundCategory category)
        {
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            _category = category;
            _is3D = false;
            _baseVolume = 1.0f;
            SourceEffect = null!; // Will be set by TryGetSourceEffect if available

            // Try to get source effect through reflection (MonoGame specific)
            TryGetSourceEffect();
        }

        /// <summary>
        /// Play the sound effect
        /// </summary>
        public void Play()
        {
            if (_disposed || _instance == null)
                return;

            try
            {
                _instance.Play();
            }
            catch (InstancePlayLimitException)
            {
                // Too many instances playing, silently fail
            }
            catch (Exception)
            {
                // Other errors, silently fail to prevent crashes
            }
        }

        /// <summary>
        /// Pause the sound effect
        /// </summary>
        public void Pause()
        {
            if (_disposed || _instance == null)
                return;

            if (_instance.State == SoundState.Playing)
            {
                _instance.Pause();
            }
        }

        /// <summary>
        /// Resume the sound effect
        /// </summary>
        public void Resume()
        {
            if (_disposed || _instance == null)
                return;

            if (_instance.State == SoundState.Paused)
            {
                _instance.Resume();
            }
        }

        /// <summary>
        /// Stop the sound effect
        /// </summary>
        public void Stop(bool immediate = true)
        {
            if (_disposed || _instance == null)
                return;

            _instance.Stop(immediate);
        }

        /// <summary>
        /// Apply 3D audio settings
        /// </summary>
        public void Apply3D(AudioListener listener, AudioEmitter emitter)
        {
            if (_disposed || _instance == null)
                return;

            try
            {
                _instance.Apply3D(listener, emitter);
                _is3D = true;
            }
            catch (Exception)
            {
                // 3D audio not supported or error occurred
                _is3D = false;
            }
        }

        /// <summary>
        /// Apply 3D audio with position only
        /// </summary>
        public void Apply3D(Vector3 listenerPosition, Vector3 emitterPosition)
        {
            if (_disposed || _instance == null)
                return;

            var listener = new AudioListener { Position = listenerPosition };

            var emitter = new AudioEmitter { Position = emitterPosition };

            Apply3D(listener, emitter);
        }

        /// <summary>
        /// Reset the instance to default values
        /// </summary>
        public void Reset()
        {
            if (_disposed || _instance == null)
                return;

            _instance.Volume = 1.0f;
            _instance.Pitch = 0.0f;
            _instance.Pan = 0.0f;
            _instance.IsLooped = false;
            _baseVolume = 1.0f;
            _is3D = false;
        }

        /// <summary>
        /// Try to get the source SoundEffect through reflection
        /// </summary>
        private void TryGetSourceEffect()
        {
            try
            {
                // MonoGame stores the parent SoundEffect in a private field
                var field = _instance
                    .GetType()
                    .GetField(
                        "_soundEffect",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    );

                if (field != null)
                {
                    SourceEffect = (field.GetValue(_instance) as SoundEffect)!;
                }
            }
            catch
            {
                // Reflection failed, source effect will remain null
                SourceEffect = null!;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Stop(immediate: true);
            _instance?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Extension methods for sound effect instance management
    /// </summary>
    public static class SoundEffectInstanceExtensions
    {
        /// <summary>
        /// Fade in a sound effect instance
        /// </summary>
        public static void FadeIn(
            this PooledSoundEffectInstance instance,
            float duration,
            float targetVolume = 1.0f
        )
        {
            if (instance == null || duration <= 0)
                return;

            // This is a simplified version - real implementation would require game loop integration
            instance.Volume = 0.0f;
            instance.Play();
            // Note: Actual fade would need to be implemented in game's Update loop
        }

        /// <summary>
        /// Fade out a sound effect instance
        /// </summary>
        public static void FadeOut(this PooledSoundEffectInstance instance, float duration)
        {
            if (instance == null || duration <= 0)
                return;

            // This is a simplified version - real implementation would require game loop integration
            // Note: Actual fade would need to be implemented in game's Update loop
            instance.Stop(immediate: false);
        }

        /// <summary>
        /// Check if the instance is currently playing
        /// </summary>
        public static bool IsPlaying(this PooledSoundEffectInstance instance)
        {
            return instance?.State == SoundState.Playing;
        }

        /// <summary>
        /// Check if the instance is currently paused
        /// </summary>
        public static bool IsPaused(this PooledSoundEffectInstance instance)
        {
            return instance?.State == SoundState.Paused;
        }

        /// <summary>
        /// Check if the instance is currently stopped
        /// </summary>
        public static bool IsStopped(this PooledSoundEffectInstance instance)
        {
            return instance?.State == SoundState.Stopped;
        }
    }
}
