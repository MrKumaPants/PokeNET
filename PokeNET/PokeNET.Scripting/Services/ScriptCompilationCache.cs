using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PokeNET.Scripting.Abstractions;
using PokeNET.Scripting.Models;

namespace PokeNET.Scripting.Services;

/// <summary>
/// Manages caching of compiled scripts to avoid redundant compilation.
/// Thread-safe implementation using ConcurrentDictionary with LRU eviction policy.
/// </summary>
public sealed class ScriptCompilationCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache;
    private readonly ILogger<ScriptCompilationCache> _logger;
    private readonly int _maxCacheSize;
    private long _totalRequests;
    private long _cacheHits;
    private long _cacheMisses;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptCompilationCache"/> class.
    /// </summary>
    /// <param name="logger">Logger for cache operations.</param>
    /// <param name="maxCacheSize">Maximum number of compiled scripts to cache. Default is 100.</param>
    public ScriptCompilationCache(ILogger<ScriptCompilationCache> logger, int maxCacheSize = 100)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxCacheSize =
            maxCacheSize > 0
                ? maxCacheSize
                : throw new ArgumentOutOfRangeException(nameof(maxCacheSize));
        _cache = new ConcurrentDictionary<string, CacheEntry>();
        _totalRequests = 0;
        _cacheHits = 0;
        _cacheMisses = 0;

        _logger.LogInformation(
            "ScriptCompilationCache initialized with max size: {MaxSize}",
            _maxCacheSize
        );
    }

    /// <summary>
    /// Attempts to retrieve a compiled script from the cache.
    /// </summary>
    /// <param name="sourceHash">The hash of the source code.</param>
    /// <param name="compiledScript">The cached compiled script, if found.</param>
    /// <returns>True if the script was found in cache; otherwise, false.</returns>
    public bool TryGet(string sourceHash, out ICompiledScript? compiledScript)
    {
        Interlocked.Increment(ref _totalRequests);

        if (_cache.TryGetValue(sourceHash, out var entry))
        {
            // Update last accessed time for LRU
            entry.UpdateLastAccessed();
            compiledScript = entry.CompiledScript;
            Interlocked.Increment(ref _cacheHits);

            _logger.LogDebug(
                "Cache HIT for script hash: {SourceHash}. Total hits: {Hits}/{Total} ({HitRate:F2}%)",
                sourceHash,
                _cacheHits,
                _totalRequests,
                GetHitRate()
            );

            return true;
        }

        compiledScript = null;
        Interlocked.Increment(ref _cacheMisses);

        _logger.LogDebug(
            "Cache MISS for script hash: {SourceHash}. Total misses: {Misses}/{Total}",
            sourceHash,
            _cacheMisses,
            _totalRequests
        );

        return false;
    }

    /// <summary>
    /// Adds a compiled script to the cache with LRU eviction if necessary.
    /// Thread-safe implementation prevents race conditions during eviction.
    /// </summary>
    /// <param name="sourceHash">The hash of the source code.</param>
    /// <param name="compiledScript">The compiled script to cache.</param>
    public void Add(string sourceHash, ICompiledScript compiledScript)
    {
        if (string.IsNullOrWhiteSpace(sourceHash))
            throw new ArgumentNullException(nameof(sourceHash));
        if (compiledScript == null)
            throw new ArgumentNullException(nameof(compiledScript));

        var entry = new CacheEntry(compiledScript);

        // Atomic add operation - prevents race condition
        if (_cache.TryAdd(sourceHash, entry))
        {
            // After successful add, check if we need eviction
            // Use while loop to handle concurrent additions
            while (_cache.Count > _maxCacheSize)
            {
                EvictOldestEntry();
            }

            _logger.LogDebug(
                "Added script to cache. Hash: {SourceHash}, Cache size: {CurrentSize}/{MaxSize}",
                sourceHash,
                _cache.Count,
                _maxCacheSize
            );
        }
        else
        {
            // Entry already exists, update it
            _cache[sourceHash] = entry;
            _logger.LogDebug("Updated existing cache entry for hash: {SourceHash}", sourceHash);
        }
    }

    /// <summary>
    /// Clears all entries from the cache.
    /// </summary>
    public void Clear()
    {
        var count = _cache.Count;
        _cache.Clear();

        _logger.LogInformation("Cache cleared. Removed {Count} entries", count);
    }

    /// <summary>
    /// Gets current cache statistics for monitoring.
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        return new CacheStatistics(
            _totalRequests,
            _cacheHits,
            _cacheMisses,
            _cache.Count,
            _maxCacheSize
        );
    }

    /// <summary>
    /// Evicts the least recently used entry from the cache.
    /// </summary>
    private void EvictOldestEntry()
    {
        if (_cache.IsEmpty)
            return;

        // Find the entry with the oldest LastAccessed time
        var oldestEntry = _cache.OrderBy(kvp => kvp.Value.LastAccessed).FirstOrDefault();

        if (oldestEntry.Key != null && _cache.TryRemove(oldestEntry.Key, out _))
        {
            _logger.LogDebug(
                "Evicted oldest cache entry. Hash: {SourceHash}, Last accessed: {LastAccessed}",
                oldestEntry.Key,
                oldestEntry.Value.LastAccessed
            );
        }
    }

    /// <summary>
    /// Gets the current cache hit rate as a percentage.
    /// </summary>
    private double GetHitRate()
    {
        return _totalRequests > 0 ? (_cacheHits * 100.0) / _totalRequests : 0;
    }

    /// <summary>
    /// Represents a cache entry with LRU tracking.
    /// </summary>
    private sealed class CacheEntry
    {
        public CacheEntry(ICompiledScript compiledScript)
        {
            CompiledScript = compiledScript;
            LastAccessed = DateTime.UtcNow;
        }

        public ICompiledScript CompiledScript { get; }
        public DateTime LastAccessed { get; private set; }

        public void UpdateLastAccessed()
        {
            LastAccessed = DateTime.UtcNow;
        }
    }
}
