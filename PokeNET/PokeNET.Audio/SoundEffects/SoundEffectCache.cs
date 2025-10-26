using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Audio;

namespace PokeNET.Audio.SoundEffects
{
    /// <summary>
    /// Cache entry for sound effects with metadata
    /// </summary>
    internal class CacheEntry
    {
        public SoundEffect SoundEffect { get; set; }
        public SoundCategory Category { get; set; }
        public DateTime LastAccessed { get; set; }
        public int AccessCount { get; set; }

        public CacheEntry(SoundEffect soundEffect, SoundCategory category)
        {
            SoundEffect = soundEffect;
            Category = category;
            LastAccessed = DateTime.UtcNow;
            AccessCount = 0;
        }

        public void RecordAccess()
        {
            LastAccessed = DateTime.UtcNow;
            AccessCount++;
        }
    }

    /// <summary>
    /// LRU (Least Recently Used) cache for sound effects
    /// </summary>
    public class SoundEffectCache : IDisposable
    {
        private readonly Dictionary<string, CacheEntry> _cache;
        private readonly int _maxSize;
        private int _totalHits;
        private int _totalMisses;
        private bool _disposed;

        public int Count => _cache.Count;
        public int MaxSize => _maxSize;
        public int TotalHits => _totalHits;
        public int TotalMisses => _totalMisses;
        public float HitRate =>
            (_totalHits + _totalMisses) > 0
                ? (float)_totalHits / (_totalHits + _totalMisses)
                : 0.0f;

        public SoundEffectCache(int maxSize = 50)
        {
            _maxSize = Math.Max(1, maxSize);
            _cache = new Dictionary<string, CacheEntry>(maxSize);
            _totalHits = 0;
            _totalMisses = 0;
        }

        /// <summary>
        /// Add a sound effect to the cache
        /// </summary>
        public void Add(
            string key,
            SoundEffect soundEffect,
            SoundCategory category = SoundCategory.System
        )
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SoundEffectCache));

            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (soundEffect == null)
                throw new ArgumentNullException(nameof(soundEffect));

            // If key already exists, update it
            if (_cache.ContainsKey(key))
            {
                _cache[key].SoundEffect = soundEffect;
                _cache[key].Category = category;
                _cache[key].RecordAccess();
                return;
            }

            // Check if we need to evict
            if (_cache.Count >= _maxSize)
            {
                EvictLeastRecentlyUsed();
            }

            // Add new entry
            _cache[key] = new CacheEntry(soundEffect, category);
        }

        /// <summary>
        /// Get a sound effect from the cache
        /// </summary>
        public SoundEffect? Get(string key)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SoundEffectCache));

            if (string.IsNullOrEmpty(key))
                return null;

            if (_cache.TryGetValue(key, out var entry))
            {
                entry.RecordAccess();
                _totalHits++;
                return entry.SoundEffect;
            }

            _totalMisses++;
            return null;
        }

        /// <summary>
        /// Get the category for a cached sound effect
        /// </summary>
        public SoundCategory GetCategory(string key)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SoundEffectCache));

            if (string.IsNullOrEmpty(key))
                return SoundCategory.System;

            if (_cache.TryGetValue(key, out var entry))
            {
                return entry.Category;
            }

            return SoundCategory.System;
        }

        /// <summary>
        /// Check if a key exists in the cache
        /// </summary>
        public bool Contains(string key)
        {
            if (_disposed)
                return false;

            return !string.IsNullOrEmpty(key) && _cache.ContainsKey(key);
        }

        /// <summary>
        /// Remove a specific sound effect from the cache
        /// </summary>
        public bool Remove(string key)
        {
            if (_disposed || string.IsNullOrEmpty(key))
                return false;

            if (_cache.TryGetValue(key, out var entry))
            {
                _cache.Remove(key);
                // Note: We don't dispose the SoundEffect as it may be owned by the caller
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clear all entries from the cache
        /// </summary>
        public void Clear()
        {
            if (_disposed)
                return;

            _cache.Clear();
            _totalHits = 0;
            _totalMisses = 0;
        }

        /// <summary>
        /// Get all cached keys
        /// </summary>
        public IEnumerable<string> GetKeys()
        {
            if (_disposed)
                return Enumerable.Empty<string>();

            return _cache.Keys.ToList();
        }

        /// <summary>
        /// Get all keys for a specific category
        /// </summary>
        public IEnumerable<string> GetKeysByCategory(SoundCategory category)
        {
            if (_disposed)
                return Enumerable.Empty<string>();

            return _cache
                .Where(kvp => kvp.Value.Category == category)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public (int total, int hits, int misses) GetStats()
        {
            return (_cache.Count, _totalHits, _totalMisses);
        }

        /// <summary>
        /// Get detailed cache statistics
        /// </summary>
        public CacheStats GetDetailedStats()
        {
            var stats = new CacheStats
            {
                TotalEntries = _cache.Count,
                MaxSize = _maxSize,
                TotalHits = _totalHits,
                TotalMisses = _totalMisses,
                HitRate = HitRate,
                CategoryBreakdown = new Dictionary<SoundCategory, int>(),
            };

            // Calculate category breakdown
            foreach (var entry in _cache.Values)
            {
                if (!stats.CategoryBreakdown.ContainsKey(entry.Category))
                {
                    stats.CategoryBreakdown[entry.Category] = 0;
                }
                stats.CategoryBreakdown[entry.Category]++;
            }

            // Find most accessed entries
            stats.MostAccessedEntries = _cache
                .OrderByDescending(kvp => kvp.Value.AccessCount)
                .Take(5)
                .Select(kvp => new AccessInfo
                {
                    Key = kvp.Key,
                    AccessCount = kvp.Value.AccessCount,
                    Category = kvp.Value.Category,
                })
                .ToList();

            return stats;
        }

        /// <summary>
        /// Evict the least recently used entry
        /// </summary>
        private void EvictLeastRecentlyUsed()
        {
            if (_cache.Count == 0)
                return;

            // Find the entry with the oldest access time
            var lruEntry = _cache.OrderBy(kvp => kvp.Value.LastAccessed).First();

            _cache.Remove(lruEntry.Key);
        }

        /// <summary>
        /// Evict all entries in a specific category
        /// </summary>
        public void EvictCategory(SoundCategory category)
        {
            if (_disposed)
                return;

            var keysToRemove = _cache
                .Where(kvp => kvp.Value.Category == category)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
        }

        /// <summary>
        /// Trim cache to a specific size by removing least recently used entries
        /// </summary>
        public void TrimToSize(int targetSize)
        {
            if (_disposed)
                return;

            targetSize = Math.Max(0, targetSize);

            while (_cache.Count > targetSize)
            {
                EvictLeastRecentlyUsed();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Clear();
            _disposed = true;
        }
    }

    /// <summary>
    /// Detailed cache statistics
    /// </summary>
    public class CacheStats
    {
        public int TotalEntries { get; set; }
        public int MaxSize { get; set; }
        public int TotalHits { get; set; }
        public int TotalMisses { get; set; }
        public float HitRate { get; set; }
        public Dictionary<SoundCategory, int> CategoryBreakdown { get; set; } = null!;
        public List<AccessInfo> MostAccessedEntries { get; set; } = null!;

        public override string ToString()
        {
            return $"Cache: {TotalEntries}/{MaxSize} entries, "
                + $"Hits={TotalHits}, Misses={TotalMisses}, "
                + $"HitRate={HitRate:P1}";
        }
    }

    /// <summary>
    /// Access information for cache entries
    /// </summary>
    public class AccessInfo
    {
        public string Key { get; set; } = null!;
        public int AccessCount { get; set; }
        public SoundCategory Category { get; set; }

        public override string ToString()
        {
            return $"{Key} ({Category}): {AccessCount} accesses";
        }
    }
}
