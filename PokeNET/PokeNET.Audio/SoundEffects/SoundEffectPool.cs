using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PokeNET.Audio.SoundEffects
{
    /// <summary>
    /// Object pool for sound effect instances to reduce allocation overhead
    /// </summary>
    public class SoundEffectPool : IDisposable
    {
        private readonly SoundCategory _category;
        private readonly Dictionary<SoundEffect, Queue<PooledSoundEffectInstance>> _pools;
        private readonly HashSet<PooledSoundEffectInstance> _activeInstances;
        private readonly int _initialSize;
        private int _totalCreated;
        private int _totalReused;
        private bool _disposed;

        public SoundCategory Category => _category;
        public int TotalCreated => _totalCreated;
        public int TotalReused => _totalReused;
        public int ActiveCount => _activeInstances.Count;

        public SoundEffectPool(SoundCategory category, int initialSize = 10)
        {
            _category = category;
            _initialSize = Math.Max(1, initialSize);
            _pools = new Dictionary<SoundEffect, Queue<PooledSoundEffectInstance>>();
            _activeInstances = new HashSet<PooledSoundEffectInstance>();
            _totalCreated = 0;
            _totalReused = 0;
        }

        /// <summary>
        /// Get a sound effect instance from the pool
        /// </summary>
        public PooledSoundEffectInstance GetInstance(SoundEffect soundEffect)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SoundEffectPool));

            if (soundEffect == null)
                throw new ArgumentNullException(nameof(soundEffect));

            // Get or create pool for this sound effect
            if (!_pools.TryGetValue(soundEffect, out var pool))
            {
                pool = new Queue<PooledSoundEffectInstance>();
                _pools[soundEffect] = pool;
            }

            PooledSoundEffectInstance instance;

            // Try to reuse an existing instance
            if (pool.Count > 0)
            {
                instance = pool.Dequeue();
                _totalReused++;
            }
            else
            {
                // Create a new instance
                var nativeInstance = soundEffect.CreateInstance();
                if (nativeInstance == null)
                    return null;

                instance = new PooledSoundEffectInstance(nativeInstance, _category);
                _totalCreated++;
            }

            _activeInstances.Add(instance);
            return instance;
        }

        /// <summary>
        /// Return a sound effect instance to the pool
        /// </summary>
        public void ReturnInstance(PooledSoundEffectInstance instance)
        {
            if (_disposed || instance == null)
                return;

            if (!_activeInstances.Remove(instance))
                return; // Not from this pool

            // Stop the instance if it's still playing
            if (instance.State != SoundState.Stopped)
            {
                instance.Stop(immediate: true);
            }

            // Reset the instance to default values
            instance.Reset();

            // Get the source sound effect
            var sourceEffect = instance.SourceEffect;
            if (sourceEffect != null && _pools.TryGetValue(sourceEffect, out var pool))
            {
                pool.Enqueue(instance);
            }
            else
            {
                // Source effect not found or pool doesn't exist anymore, dispose the instance
                instance.Dispose();
            }
        }

        /// <summary>
        /// Prewarm the pool with instances for a specific sound effect
        /// </summary>
        public void Prewarm(SoundEffect soundEffect, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SoundEffectPool));

            if (soundEffect == null)
                throw new ArgumentNullException(nameof(soundEffect));

            count = Math.Max(0, count);

            if (!_pools.TryGetValue(soundEffect, out var pool))
            {
                pool = new Queue<PooledSoundEffectInstance>();
                _pools[soundEffect] = pool;
            }

            for (int i = 0; i < count; i++)
            {
                var nativeInstance = soundEffect.CreateInstance();
                if (nativeInstance == null)
                    break;

                var instance = new PooledSoundEffectInstance(nativeInstance, _category);
                pool.Enqueue(instance);
                _totalCreated++;
            }
        }

        /// <summary>
        /// Clear all pooled instances for a specific sound effect
        /// </summary>
        public void ClearPool(SoundEffect soundEffect)
        {
            if (_disposed || soundEffect == null)
                return;

            if (_pools.TryGetValue(soundEffect, out var pool))
            {
                while (pool.Count > 0)
                {
                    var instance = pool.Dequeue();
                    instance.Dispose();
                }
                _pools.Remove(soundEffect);
            }
        }

        /// <summary>
        /// Clear all pools and dispose instances
        /// </summary>
        public void ClearAll()
        {
            if (_disposed)
                return;

            // Stop and dispose all active instances
            foreach (var instance in _activeInstances.ToList())
            {
                instance.Stop(immediate: true);
                instance.Dispose();
            }
            _activeInstances.Clear();

            // Dispose all pooled instances
            foreach (var pool in _pools.Values)
            {
                while (pool.Count > 0)
                {
                    var instance = pool.Dequeue();
                    instance.Dispose();
                }
            }
            _pools.Clear();

            _totalCreated = 0;
            _totalReused = 0;
        }

        /// <summary>
        /// Get pool statistics
        /// </summary>
        public PoolStats GetStats()
        {
            return new PoolStats
            {
                Category = _category,
                TotalCreated = _totalCreated,
                TotalReused = _totalReused,
                ActiveInstances = _activeInstances.Count,
                PooledInstances = _pools.Values.Sum(p => p.Count),
                UniqueEffects = _pools.Count,
                ReuseRate = _totalCreated > 0 ? (float)_totalReused / (_totalCreated + _totalReused) : 0.0f
            };
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            ClearAll();
            _disposed = true;
        }
    }

    /// <summary>
    /// Statistics for a sound effect pool
    /// </summary>
    public class PoolStats
    {
        public SoundCategory Category { get; set; }
        public int TotalCreated { get; set; }
        public int TotalReused { get; set; }
        public int ActiveInstances { get; set; }
        public int PooledInstances { get; set; }
        public int UniqueEffects { get; set; }
        public float ReuseRate { get; set; }

        public override string ToString()
        {
            return $"Pool [{Category}]: Created={TotalCreated}, Reused={TotalReused}, " +
                   $"Active={ActiveInstances}, Pooled={PooledInstances}, " +
                   $"Unique={UniqueEffects}, ReuseRate={ReuseRate:P1}";
        }
    }
}
