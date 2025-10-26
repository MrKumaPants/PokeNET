namespace PokeNET.Audio.Abstractions;

/// <summary>
/// Provides caching functionality for audio data to improve performance.
/// </summary>
public interface IAudioCache
{
    /// <summary>
    /// Gets cached data or loads it using the provided loader function.
    /// </summary>
    /// <typeparam name="T">Type of data to cache (must be reference type)</typeparam>
    /// <param name="key">Unique identifier for the cached item</param>
    /// <param name="loader">Function to load data if not in cache</param>
    /// <returns>Cached or loaded data, or null if not found</returns>
    Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T>> loader)
        where T : class;

    /// <summary>
    /// Preloads data into the cache.
    /// </summary>
    /// <param name="key">Unique identifier for the cached item</param>
    /// <param name="data">Data to cache</param>
    Task PreloadAsync(string key, object data);

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Gets the approximate size of the cache in bytes.
    /// </summary>
    /// <returns>Cache size in bytes</returns>
    long GetSize();

    /// <summary>
    /// Releases all resources used by the cache.
    /// </summary>
    void Dispose();
}
