using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace PokeNET.Audio.SoundEffects
{
    /// <summary>
    /// Sound effect categories for volume control and organization
    /// </summary>
    public enum SoundCategory
    {
        UI,
        Battle,
        Ambient,
        Character,
        Environment,
        Item,
        System,
    }

    /// <summary>
    /// Configuration for sound effect playback
    /// </summary>
    public class SoundEffectPlaybackConfig
    {
        public float Volume { get; set; } = 1.0f;
        public float Pitch { get; set; } = 0.0f;
        public float Pan { get; set; } = 0.0f;
        public bool Loop { get; set; } = false;
        public SoundCategory Category { get; set; } = SoundCategory.System;
        public bool Use3D { get; set; } = false;
        public Vector3? Position { get; set; }
        public bool RandomizeVariations { get; set; } = false;
        public float PitchVariation { get; set; } = 0.0f;
        public float VolumeVariation { get; set; } = 0.0f;
    }

    /// <summary>
    /// Centralized sound effect management system with pooling, caching, and channel management
    /// </summary>
    public class SoundEffectManager : IDisposable
    {
        private readonly SoundEffectCache _cache;
        private readonly Dictionary<SoundCategory, SoundEffectPool> _pools;
        private readonly Dictionary<SoundCategory, float> _categoryVolumes;
        private readonly List<PooledSoundEffectInstance> _activeInstances;
        private readonly AudioListener _listener;
        private readonly AudioEmitter _emitter;
        private readonly Random _random;

        private float _masterVolume;
        private int _maxConcurrentSounds;
        private int _activeSoundCount;
        private bool _disposed;

        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Math.Clamp(value, 0.0f, 1.0f);
                UpdateAllActiveVolumes();
            }
        }

        public int MaxConcurrentSounds
        {
            get => _maxConcurrentSounds;
            set => _maxConcurrentSounds = Math.Max(1, value);
        }

        public int ActiveSoundCount => _activeSoundCount;

        public SoundEffectManager(int maxCacheSize = 50, int maxConcurrentSounds = 32)
        {
            _cache = new SoundEffectCache(maxCacheSize);
            _pools = new Dictionary<SoundCategory, SoundEffectPool>();
            _categoryVolumes = new Dictionary<SoundCategory, float>();
            _activeInstances = new List<PooledSoundEffectInstance>();
            _listener = new AudioListener();
            _emitter = new AudioEmitter();
            _random = new Random();

            _masterVolume = 1.0f;
            _maxConcurrentSounds = maxConcurrentSounds;
            _activeSoundCount = 0;

            // Initialize category volumes
            foreach (SoundCategory category in Enum.GetValues(typeof(SoundCategory)))
            {
                _categoryVolumes[category] = 1.0f;
                _pools[category] = new SoundEffectPool(category, 10);
            }
        }

        /// <summary>
        /// Load a sound effect into the cache
        /// </summary>
        public void LoadSoundEffect(
            string soundKey,
            SoundEffect soundEffect,
            SoundCategory category = SoundCategory.System
        )
        {
            if (string.IsNullOrEmpty(soundKey))
                throw new ArgumentNullException(nameof(soundKey));
            if (soundEffect == null)
                throw new ArgumentNullException(nameof(soundEffect));

            _cache.Add(soundKey, soundEffect, category);
        }

        /// <summary>
        /// Play a sound effect with optional configuration
        /// </summary>
        public PooledSoundEffectInstance? Play(
            string soundKey,
            SoundEffectPlaybackConfig? config = null
        )
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SoundEffectManager));

            if (string.IsNullOrEmpty(soundKey))
                return null;

            // Check concurrent sound limit
            if (_activeSoundCount >= _maxConcurrentSounds)
            {
                // Remove oldest stopped sound
                CleanupStoppedSounds();
                if (_activeSoundCount >= _maxConcurrentSounds)
                    return null; // Still at limit, can't play new sound
            }

            var soundEffect = _cache.Get(soundKey);
            if (soundEffect == null)
                return null;

            config ??= new SoundEffectPlaybackConfig();

            // Get category from cache if not specified
            if (config.Category == SoundCategory.System)
                config.Category = _cache.GetCategory(soundKey);

            // Get instance from pool
            var pool = _pools[config.Category];
            var instance = pool.GetInstance(soundEffect);

            if (instance == null)
                return null;

            // Apply variations if enabled
            float finalVolume = config.Volume;
            float finalPitch = config.Pitch;

            if (config.RandomizeVariations)
            {
                finalVolume *=
                    1.0f + ((float)_random.NextDouble() * 2.0f - 1.0f) * config.VolumeVariation;
                finalPitch += ((float)_random.NextDouble() * 2.0f - 1.0f) * config.PitchVariation;
            }

            finalVolume = Math.Clamp(finalVolume, 0.0f, 1.0f);
            finalPitch = Math.Clamp(finalPitch, -1.0f, 1.0f);

            // Apply category and master volume
            float effectiveVolume =
                finalVolume * GetCategoryVolume(config.Category) * _masterVolume;

            // Configure the instance
            instance.Volume = effectiveVolume;
            instance.Pitch = finalPitch;
            instance.Pan = config.Pan;
            instance.IsLooped = config.Loop;

            // Handle 3D audio if requested
            if (config.Use3D && config.Position.HasValue)
            {
                _emitter.Position = config.Position.Value;
                instance.Apply3D(_listener, _emitter);
            }

            // Play the sound
            instance.Play();

            // Track active instance
            _activeInstances.Add(instance);
            _activeSoundCount++;

            return instance;
        }

        /// <summary>
        /// Play a simple sound effect with default settings
        /// </summary>
        public PooledSoundEffectInstance? PlaySimple(
            string soundKey,
            SoundCategory category = SoundCategory.System,
            float volume = 1.0f
        )
        {
            return Play(
                soundKey,
                new SoundEffectPlaybackConfig { Category = category, Volume = volume }
            );
        }

        /// <summary>
        /// Play a 3D positional sound effect
        /// </summary>
        public PooledSoundEffectInstance? Play3D(
            string soundKey,
            Vector3 position,
            SoundCategory category = SoundCategory.System,
            float volume = 1.0f
        )
        {
            return Play(
                soundKey,
                new SoundEffectPlaybackConfig
                {
                    Category = category,
                    Volume = volume,
                    Use3D = true,
                    Position = position,
                }
            );
        }

        /// <summary>
        /// Play a sound effect with random variations
        /// </summary>
        public PooledSoundEffectInstance? PlayWithVariation(
            string soundKey,
            SoundCategory category = SoundCategory.System,
            float volume = 1.0f,
            float pitchVariation = 0.1f,
            float volumeVariation = 0.1f
        )
        {
            return Play(
                soundKey,
                new SoundEffectPlaybackConfig
                {
                    Category = category,
                    Volume = volume,
                    RandomizeVariations = true,
                    PitchVariation = pitchVariation,
                    VolumeVariation = volumeVariation,
                }
            );
        }

        /// <summary>
        /// Stop all sounds in a specific category
        /// </summary>
        public void StopCategory(SoundCategory category)
        {
            var instancesToStop = _activeInstances.Where(i => i.Category == category).ToList();
            foreach (var instance in instancesToStop)
            {
                instance.Stop();
            }
        }

        /// <summary>
        /// Stop all currently playing sounds
        /// </summary>
        public void StopAll()
        {
            foreach (var instance in _activeInstances.ToList())
            {
                instance.Stop();
            }
            CleanupStoppedSounds();
        }

        /// <summary>
        /// Set volume for a specific category
        /// </summary>
        public void SetCategoryVolume(SoundCategory category, float volume)
        {
            _categoryVolumes[category] = Math.Clamp(volume, 0.0f, 1.0f);
            UpdateCategoryVolumes(category);
        }

        /// <summary>
        /// Get volume for a specific category
        /// </summary>
        public float GetCategoryVolume(SoundCategory category)
        {
            return _categoryVolumes.TryGetValue(category, out float volume) ? volume : 1.0f;
        }

        /// <summary>
        /// Update the audio listener position for 3D audio
        /// </summary>
        public void UpdateListener(Vector3 position, Vector3 forward, Vector3 up)
        {
            _listener.Position = position;
            _listener.Forward = forward;
            _listener.Up = up;

            // Update all 3D sounds
            foreach (var instance in _activeInstances.Where(i => i.Is3D))
            {
                instance.Apply3D(_listener, _emitter);
            }
        }

        /// <summary>
        /// Update method to clean up stopped sounds and manage resources
        /// </summary>
        public void Update()
        {
            if (_disposed)
                return;

            CleanupStoppedSounds();
        }

        /// <summary>
        /// Preload multiple sound effects
        /// </summary>
        public void PreloadSounds(
            Dictionary<string, (SoundEffect effect, SoundCategory category)> sounds
        )
        {
            foreach (var kvp in sounds)
            {
                LoadSoundEffect(kvp.Key, kvp.Value.effect, kvp.Value.category);
            }
        }

        /// <summary>
        /// Clear the cache and release unused resources
        /// </summary>
        public void ClearCache()
        {
            StopAll();
            _cache.Clear();
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public (int total, int hits, int misses) GetCacheStats()
        {
            return _cache.GetStats();
        }

        private void CleanupStoppedSounds()
        {
            var stoppedInstances = _activeInstances
                .Where(i => i.State == SoundState.Stopped)
                .ToList();

            foreach (var instance in stoppedInstances)
            {
                _activeInstances.Remove(instance);
                _pools[instance.Category].ReturnInstance(instance);
                _activeSoundCount--;
            }
        }

        private void UpdateAllActiveVolumes()
        {
            foreach (var instance in _activeInstances)
            {
                float categoryVolume = GetCategoryVolume(instance.Category);
                instance.Volume = instance.BaseVolume * categoryVolume * _masterVolume;
            }
        }

        private void UpdateCategoryVolumes(SoundCategory category)
        {
            float categoryVolume = GetCategoryVolume(category);

            foreach (var instance in _activeInstances.Where(i => i.Category == category))
            {
                instance.Volume = instance.BaseVolume * categoryVolume * _masterVolume;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            StopAll();

            foreach (var pool in _pools.Values)
            {
                pool.Dispose();
            }

            _cache.Dispose();
            _disposed = true;
        }
    }
}
