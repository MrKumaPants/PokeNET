using System.Collections.Concurrent;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Exceptions;

namespace PokeNET.Audio.Services;

/// <summary>
/// Thread-safe cache for audio assets to prevent redundant loading.
/// Implements LRU eviction when cache size limits are exceeded.
/// </summary>
public sealed class AudioCache : IAudioCache
{
    private readonly ILogger<AudioCache> _logger;
    private readonly ConcurrentDictionary<string, CachedAsset> _cache;
    private readonly ReaderWriterLockSlim _cacheLock;
    private readonly long _maxCacheSizeBytes;
    private long _currentCacheSize;
    private bool _disposed;

    /// <summary>
    /// Gets the number of cached assets.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Gets the current cache size in bytes.
    /// </summary>
    public long CurrentSize => Interlocked.Read(ref _currentCacheSize);

    public AudioCache(ILogger<AudioCache> logger, long maxCacheSizeMB = 100)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = new ConcurrentDictionary<string, CachedAsset>(StringComparer.OrdinalIgnoreCase);
        _cacheLock = new ReaderWriterLockSlim();
        _maxCacheSizeBytes = maxCacheSizeMB * 1024 * 1024;
        _currentCacheSize = 0;

        _logger.LogInformation(
            "AudioCache initialized with max size: {MaxSize} MB",
            maxCacheSizeMB
        );
    }

    /// <summary>
    /// Attempts to get a cached asset.
    /// </summary>
    /// <typeparam name="T">The type of cached asset.</typeparam>
    /// <param name="key">The cache key (asset path).</param>
    /// <param name="asset">The cached asset if found.</param>
    /// <returns>True if the asset was found in cache, false otherwise.</returns>
    public bool TryGet<T>(string key, out T? asset)
        where T : class
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null or whitespace", nameof(key));
        }

        if (_cache.TryGetValue(key, out var cachedAsset))
        {
            cachedAsset.UpdateAccessTime();

            if (cachedAsset.Data is T typedAsset)
            {
                asset = typedAsset;
                _logger.LogDebug("Cache hit for asset: {Key}", key);
                return true;
            }

            _logger.LogWarning(
                "Cache type mismatch for asset: {Key}. Expected {Expected}, got {Actual}",
                key,
                typeof(T).Name,
                cachedAsset.Data?.GetType().Name ?? "null"
            );
        }

        asset = null;
        _logger.LogDebug("Cache miss for asset: {Key}", key);
        return false;
    }

    /// <summary>
    /// Adds or updates an asset in the cache.
    /// </summary>
    /// <typeparam name="T">The type of asset to cache.</typeparam>
    /// <param name="key">The cache key (asset path).</param>
    /// <param name="asset">The asset to cache.</param>
    /// <param name="sizeBytes">The size of the asset in bytes.</param>
    public void Set<T>(string key, T asset, long sizeBytes)
        where T : class
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null or whitespace", nameof(key));
        }

        if (asset == null)
        {
            throw new ArgumentNullException(nameof(asset));
        }

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Size must be positive");
        }

        _cacheLock.EnterWriteLock();
        try
        {
            // Check if adding this asset would exceed cache size
            while (_currentCacheSize + sizeBytes > _maxCacheSizeBytes && _cache.Count > 0)
            {
                EvictLeastRecentlyUsed();
            }

            var cachedAsset = new CachedAsset(asset, sizeBytes);

            if (_cache.TryAdd(key, cachedAsset))
            {
                Interlocked.Add(ref _currentCacheSize, sizeBytes);
                _logger.LogDebug("Cached asset: {Key}, Size: {Size} bytes", key, sizeBytes);
            }
            else
            {
                // Key already exists, update it
                if (_cache.TryGetValue(key, out var existingAsset))
                {
                    var oldSize = existingAsset.SizeBytes;
                    _cache[key] = cachedAsset;
                    Interlocked.Add(ref _currentCacheSize, sizeBytes - oldSize);
                    _logger.LogDebug(
                        "Updated cached asset: {Key}, Size delta: {Delta} bytes",
                        key,
                        sizeBytes - oldSize
                    );
                }
            }
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Removes an asset from the cache.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    /// <returns>True if the asset was removed, false if it wasn't found.</returns>
    public bool Remove(string key)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        _cacheLock.EnterWriteLock();
        try
        {
            if (_cache.TryRemove(key, out var cachedAsset))
            {
                Interlocked.Add(ref _currentCacheSize, -cachedAsset.SizeBytes);

                if (cachedAsset.Data is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _logger.LogDebug(
                    "Removed cached asset: {Key}, Size: {Size} bytes",
                    key,
                    cachedAsset.SizeBytes
                );
                return true;
            }

            return false;
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Clears all cached assets.
    /// </summary>
    public void Clear()
    {
        ThrowIfDisposed();

        _cacheLock.EnterWriteLock();
        try
        {
            foreach (var kvp in _cache)
            {
                if (kvp.Value.Data is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing cached asset: {Key}", kvp.Key);
                    }
                }
            }

            _cache.Clear();
            Interlocked.Exchange(ref _currentCacheSize, 0);
            _logger.LogInformation("Cache cleared");
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets statistics about the cache contents.
    /// </summary>
    /// <returns>Dictionary of asset types to counts.</returns>
    public IDictionary<string, int> GetStatistics()
    {
        ThrowIfDisposed();

        var stats = new Dictionary<string, int>();

        foreach (var kvp in _cache)
        {
            var typeName = kvp.Value.Data?.GetType().Name ?? "Unknown";

            if (!stats.ContainsKey(typeName))
            {
                stats[typeName] = 0;
            }

            stats[typeName]++;
        }

        return stats;
    }

    /// <summary>
    /// Evicts the least recently used asset from the cache.
    /// </summary>
    private void EvictLeastRecentlyUsed()
    {
        // Must be called within write lock
        var lruKey = _cache
            .OrderBy(kvp => kvp.Value.LastAccessTime)
            .Select(kvp => kvp.Key)
            .FirstOrDefault();

        if (lruKey != null && _cache.TryRemove(lruKey, out var evictedAsset))
        {
            Interlocked.Add(ref _currentCacheSize, -evictedAsset.SizeBytes);

            if (evictedAsset.Data is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing evicted asset: {Key}", lruKey);
                }
            }

            _logger.LogDebug(
                "Evicted LRU asset: {Key}, Size: {Size} bytes",
                lruKey,
                evictedAsset.SizeBytes
            );
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AudioCache));
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T>> loader)
        where T : class
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null or whitespace", nameof(key));
        }

        ArgumentNullException.ThrowIfNull(loader);

        // Try to get from cache first
        if (TryGet<T>(key, out var cachedAsset) && cachedAsset != null)
        {
            return cachedAsset;
        }

        // Load the asset
        var asset = await loader().ConfigureAwait(false);
        if (asset == null)
        {
            return null;
        }

        // Estimate size for caching
        long sizeBytes = asset switch
        {
            byte[] bytes => bytes.Length,
            string str => str.Length * 2,
            _ => 1024, // Default estimate
        };

        Set(key, asset, sizeBytes);
        return asset;
    }

    /// <inheritdoc />
    public Task PreloadAsync(string key, object data)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null or whitespace", nameof(key));
        }

        ArgumentNullException.ThrowIfNull(data);

        // Estimate size for caching
        long sizeBytes = data switch
        {
            byte[] bytes => bytes.Length,
            string str => str.Length * 2,
            _ => 1024, // Default estimate
        };

        var cachedAsset = new CachedAsset(data, sizeBytes);

        _cacheLock.EnterWriteLock();
        try
        {
            _cache[key] = cachedAsset;
            Interlocked.Add(ref _currentCacheSize, sizeBytes);
            _logger.LogDebug("Preloaded asset: {Key}, Size: {Size} bytes", key, sizeBytes);
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ClearAsync()
    {
        Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public long GetSize()
    {
        ThrowIfDisposed();
        return CurrentSize;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Clear();
        _cacheLock.Dispose();
        _disposed = true;

        _logger.LogInformation("AudioCache disposed");
    }

    /// <summary>
    /// Represents a cached asset with metadata.
    /// </summary>
    private sealed class CachedAsset
    {
        public object Data { get; }
        public long SizeBytes { get; }
        public DateTime LastAccessTime { get; private set; }

        public CachedAsset(object data, long sizeBytes)
        {
            Data = data;
            SizeBytes = sizeBytes;
            LastAccessTime = DateTime.UtcNow;
        }

        public void UpdateAccessTime()
        {
            LastAccessTime = DateTime.UtcNow;
        }
    }
}
