using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;

namespace PokeNET.Audio.Services.Managers;

/// <summary>
/// Coordinates high-level audio caching operations.
/// Implements single responsibility for cache coordination.
/// </summary>
public sealed class AudioCacheCoordinator : IAudioCacheCoordinator
{
    private readonly ILogger<AudioCacheCoordinator> _logger;
    private readonly IAudioCache _cache;

    /// <summary>
    /// Initializes a new instance of the AudioCacheCoordinator class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="cache">Audio cache for managing audio data.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public AudioCacheCoordinator(
        ILogger<AudioCacheCoordinator> logger,
        IAudioCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <summary>
    /// Preloads an audio file into the cache.
    /// </summary>
    /// <param name="assetPath">Path to the audio file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task PreloadAsync(string assetPath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Preloading audio: {AssetPath}", assetPath);

            // Load audio data and cache it using the cache's GetOrLoadAsync
            await _cache.GetOrLoadAsync<object>(assetPath, () =>
            {
                // TODO: Implement actual file loading logic
                throw new FileNotFoundException($"Audio file not found: {assetPath}");
            });

            _logger.LogDebug("Preloaded audio: {AssetPath}", assetPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preload audio: {AssetPath}", assetPath);
            throw;
        }
    }

    /// <summary>
    /// Preloads multiple audio files into the cache in parallel.
    /// </summary>
    /// <param name="assetPaths">Array of asset paths to preload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task PreloadMultipleAsync(string[] assetPaths, CancellationToken cancellationToken = default)
    {
        if (assetPaths == null || assetPaths.Length == 0)
        {
            return;
        }

        _logger.LogInformation("Preloading {Count} audio files", assetPaths.Length);

        try
        {
            // Preload all files in parallel
            var tasks = assetPaths.Select(path => PreloadAsync(path, cancellationToken));
            await Task.WhenAll(tasks);

            _logger.LogInformation("Successfully preloaded {Count} audio files", assetPaths.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preload multiple audio files");
            throw;
        }
    }

    /// <summary>
    /// Clears all cached audio data.
    /// </summary>
    public async Task ClearAsync()
    {
        try
        {
            await _cache.ClearAsync();
            _logger.LogInformation("Cleared audio cache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cache");
            throw;
        }
    }

    /// <summary>
    /// Gets the current size of the audio cache in bytes.
    /// </summary>
    /// <returns>Cache size in bytes.</returns>
    public long GetSize()
    {
        return _cache.GetSize();
    }
}
