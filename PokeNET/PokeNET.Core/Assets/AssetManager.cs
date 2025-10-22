using Microsoft.Extensions.Logging;
using PokeNET.Domain.Assets;

namespace PokeNET.Core.Assets;

/// <summary>
/// Manages asset loading with caching and mod support.
/// Follows the Single Responsibility Principle - only manages assets.
/// Follows the Open/Closed Principle - extensible via IAssetLoader registration.
/// </summary>
public class AssetManager : IAssetManager
{
    private readonly ILogger<AssetManager> _logger;
    private readonly Dictionary<Type, object> _loaders = new();
    private readonly Dictionary<string, object> _cache = new();
    private readonly List<string> _modPaths = new();
    private readonly string _basePath;
    private bool _disposed;

    /// <summary>
    /// Initializes a new asset manager.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="basePath">Base path for game assets.</param>
    public AssetManager(ILogger<AssetManager> logger, string basePath)
    {
        _logger = logger;
        _basePath = basePath;

        if (!Directory.Exists(basePath))
        {
            _logger.LogWarning("Base asset path does not exist: {BasePath}", basePath);
            Directory.CreateDirectory(basePath);
        }

        _logger.LogInformation("AssetManager initialized with base path: {BasePath}", basePath);
    }

    /// <inheritdoc/>
    public void RegisterLoader<T>(IAssetLoader<T> loader) where T : class
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AssetManager));

        ArgumentNullException.ThrowIfNull(loader);

        var type = typeof(T);
        _loaders[type] = loader;

        _logger.LogInformation("Registered asset loader for type {AssetType}", type.Name);
    }

    /// <inheritdoc/>
    public void SetModPaths(IEnumerable<string> modPaths)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AssetManager));

        ArgumentNullException.ThrowIfNull(modPaths);

        _modPaths.Clear();
        _modPaths.AddRange(modPaths.Where(Directory.Exists));

        _logger.LogInformation("Set {Count} mod paths for asset resolution", _modPaths.Count);

        // Clear cache when mod paths change as assets may resolve differently
        UnloadAll();
    }

    /// <inheritdoc/>
    public T Load<T>(string path) where T : class
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AssetManager));

        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Check cache first
        if (_cache.TryGetValue(path, out var cached))
        {
            _logger.LogTrace("Asset cache hit: {Path}", path);
            return (T)cached;
        }

        // Resolve the actual file path (mod override or base)
        var resolvedPath = ResolvePath(path);
        if (resolvedPath == null)
        {
            throw new AssetLoadException(path, $"Asset not found: {path}");
        }

        // Get the appropriate loader
        if (!_loaders.TryGetValue(typeof(T), out var loaderObj))
        {
            throw new AssetLoadException(path,
                $"No asset loader registered for type {typeof(T).Name}");
        }

        var loader = (IAssetLoader<T>)loaderObj;
        var extension = Path.GetExtension(resolvedPath);

        if (!loader.CanHandle(extension))
        {
            throw new AssetLoadException(path,
                $"Loader for {typeof(T).Name} cannot handle extension {extension}");
        }

        // Load the asset
        try
        {
            _logger.LogDebug("Loading asset {Path} from {ResolvedPath}", path, resolvedPath);
            var asset = loader.Load(resolvedPath);

            // Cache the loaded asset
            _cache[path] = asset;

            _logger.LogInformation("Successfully loaded asset: {Path}", path);
            return asset;
        }
        catch (Exception ex)
        {
            throw new AssetLoadException(path, $"Failed to load asset: {path}", ex);
        }
    }

    /// <inheritdoc/>
    public T? TryLoad<T>(string path) where T : class
    {
        try
        {
            return Load<T>(path);
        }
        catch (AssetLoadException ex)
        {
            _logger.LogWarning(ex, "Failed to load asset (non-fatal): {Path}", path);
            return null;
        }
    }

    /// <inheritdoc/>
    public bool IsLoaded(string path)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AssetManager));

        return _cache.ContainsKey(path);
    }

    /// <inheritdoc/>
    public void Unload(string path)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AssetManager));

        if (_cache.Remove(path, out var asset))
        {
            _logger.LogDebug("Unloaded asset: {Path}", path);

            // Dispose if the asset is disposable
            if (asset is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <inheritdoc/>
    public void UnloadAll()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AssetManager));

        _logger.LogInformation("Unloading all cached assets ({Count} assets)", _cache.Count);

        foreach (var kvp in _cache)
        {
            if (kvp.Value is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing asset: {Path}", kvp.Key);
                }
            }
        }

        _cache.Clear();
    }

    /// <summary>
    /// Resolves an asset path, checking mod directories first, then base path.
    /// This enables mods to override base game assets.
    /// </summary>
    /// <param name="path">The relative asset path.</param>
    /// <returns>The resolved absolute path, or null if not found.</returns>
    private string? ResolvePath(string path)
    {
        // Check mod paths first (in order of priority)
        foreach (var modPath in _modPaths)
        {
            var fullPath = Path.Combine(modPath, path);
            if (File.Exists(fullPath))
            {
                _logger.LogTrace("Resolved asset {Path} to mod path: {FullPath}", path, fullPath);
                return fullPath;
            }
        }

        // Fall back to base path
        var basePath = Path.Combine(_basePath, path);
        if (File.Exists(basePath))
        {
            _logger.LogTrace("Resolved asset {Path} to base path: {BasePath}", path, basePath);
            return basePath;
        }

        _logger.LogWarning("Asset not found in any search path: {Path}", path);
        return null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogInformation("Disposing AssetManager");
        UnloadAll();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
