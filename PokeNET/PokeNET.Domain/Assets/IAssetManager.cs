using System;
using System.Collections.Generic;

namespace PokeNET.Domain.Assets;

/// <summary>
/// Manages loading, caching, and retrieval of game assets.
/// Follows the Single Responsibility Principle and Dependency Inversion Principle.
/// </summary>
public interface IAssetManager : IDisposable
{
    /// <summary>
    /// Loads an asset of the specified type from the given path.
    /// Assets are cached after first load.
    /// </summary>
    /// <typeparam name="T">The type of asset to load.</typeparam>
    /// <param name="path">The asset path (relative to asset directory).</param>
    /// <returns>The loaded asset instance.</returns>
    T Load<T>(string path)
        where T : class;

    /// <summary>
    /// Attempts to load an asset, returning null if it fails.
    /// </summary>
    /// <typeparam name="T">The type of asset to load.</typeparam>
    /// <param name="path">The asset path.</param>
    /// <returns>The loaded asset or null if loading failed.</returns>
    T? TryLoad<T>(string path)
        where T : class;

    /// <summary>
    /// Checks if an asset is currently loaded in the cache.
    /// </summary>
    /// <param name="path">The asset path to check.</param>
    /// <returns>True if the asset is cached.</returns>
    bool IsLoaded(string path);

    /// <summary>
    /// Unloads a specific asset from the cache.
    /// </summary>
    /// <param name="path">The asset path to unload.</param>
    void Unload(string path);

    /// <summary>
    /// Unloads all assets from the cache.
    /// </summary>
    void UnloadAll();

    /// <summary>
    /// Registers a custom asset loader for a specific type.
    /// Follows the Open/Closed Principle - new loaders can be added without modifying AssetManager.
    /// </summary>
    /// <typeparam name="T">The asset type.</typeparam>
    /// <param name="loader">The loader instance.</param>
    void RegisterLoader<T>(IAssetLoader<T> loader)
        where T : class;

    /// <summary>
    /// Sets the mod search paths for asset resolution.
    /// Assets in mod paths override base game assets.
    /// </summary>
    /// <param name="modPaths">Collection of mod directory paths in priority order.</param>
    void SetModPaths(IEnumerable<string> modPaths);
}
