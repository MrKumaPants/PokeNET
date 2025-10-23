namespace PokeNET.Scripting.Models;

/// <summary>
/// Represents statistics for the script compilation cache.
/// </summary>
public sealed class CacheStatistics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CacheStatistics"/> class.
    /// </summary>
    /// <param name="totalRequests">Total number of cache requests.</param>
    /// <param name="cacheHits">Number of successful cache hits.</param>
    /// <param name="cacheMisses">Number of cache misses.</param>
    /// <param name="currentSize">Current number of items in the cache.</param>
    /// <param name="maxSize">Maximum cache size.</param>
    public CacheStatistics(
        long totalRequests,
        long cacheHits,
        long cacheMisses,
        int currentSize,
        int maxSize)
    {
        TotalRequests = totalRequests;
        CacheHits = cacheHits;
        CacheMisses = cacheMisses;
        CurrentSize = currentSize;
        MaxSize = maxSize;
    }

    /// <summary>
    /// Gets the total number of cache requests.
    /// </summary>
    public long TotalRequests { get; }

    /// <summary>
    /// Gets the number of successful cache hits.
    /// </summary>
    public long CacheHits { get; }

    /// <summary>
    /// Gets the number of cache misses.
    /// </summary>
    public long CacheMisses { get; }

    /// <summary>
    /// Gets the current number of items in the cache.
    /// </summary>
    public int CurrentSize { get; }

    /// <summary>
    /// Gets the maximum cache size.
    /// </summary>
    public int MaxSize { get; }

    /// <summary>
    /// Gets the cache hit rate as a percentage (0-100).
    /// </summary>
    public double HitRate => TotalRequests > 0 ? (CacheHits * 100.0) / TotalRequests : 0;

    /// <summary>
    /// Gets the cache usage percentage (0-100).
    /// </summary>
    public double UsagePercentage => MaxSize > 0 ? (CurrentSize * 100.0) / MaxSize : 0;

    /// <summary>
    /// Creates a statistics snapshot with no activity.
    /// </summary>
    public static CacheStatistics Empty(int maxSize) =>
        new CacheStatistics(0, 0, 0, 0, maxSize);
}
