namespace PokeNET.Audio.Abstractions;

/// <summary>
/// Coordinates high-level audio caching operations.
/// SOLID PRINCIPLE: Single Responsibility - Handles only cache coordination.
/// </summary>
public interface IAudioCacheCoordinator
{
    /// <summary>
    /// Preloads an audio file into the cache.
    /// </summary>
    /// <param name="assetPath">Path to the audio file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task PreloadAsync(string assetPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Preloads multiple audio files into the cache in parallel.
    /// </summary>
    /// <param name="assetPaths">Array of asset paths to preload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task PreloadMultipleAsync(string[] assetPaths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cached audio data.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    Task ClearAsync();

    /// <summary>
    /// Gets the current size of the audio cache in bytes.
    /// </summary>
    /// <returns>Cache size in bytes.</returns>
    long GetSize();
}
