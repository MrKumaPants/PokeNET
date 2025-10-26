namespace PokeNET.ModAPI.Interfaces;

/// <summary>
/// Provides operations for loading, registering, and managing game assets.
/// </summary>
/// <remarks>
/// This API handles asset lifecycle including loading from disk, registration in memory,
/// and cleanup. All asset operations are cached for performance.
/// </remarks>
/// <example>
/// <code>
/// // Load a texture asset
/// var texture = api.AssetApi.LoadAsset&lt;Texture2D&gt;("mods/mymod/textures/pikachu.png");
///
/// // Register a custom asset
/// api.AssetApi.RegisterAsset("mymod:custom_shader", myShaderAsset);
///
/// // Retrieve registered asset
/// var shader = api.AssetApi.GetAsset&lt;Shader&gt;("mymod:custom_shader");
/// </code>
/// </example>
public interface IAssetApi
{
    /// <summary>
    /// Loads an asset from the specified path.
    /// </summary>
    /// <typeparam name="T">The asset type to load.</typeparam>
    /// <param name="path">The file path relative to the mod directory.</param>
    /// <returns>The loaded asset instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the asset file doesn't exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the asset cannot be loaded as type T.</exception>
    T LoadAsset<T>(string path)
        where T : class;

    /// <summary>
    /// Registers an asset with a unique identifier for later retrieval.
    /// </summary>
    /// <typeparam name="T">The asset type.</typeparam>
    /// <param name="id">Unique identifier for the asset (format: "modid:assetname").</param>
    /// <param name="asset">The asset instance to register.</param>
    /// <exception cref="ArgumentException">Thrown when id is invalid or already registered.</exception>
    void RegisterAsset<T>(string id, T asset)
        where T : class;

    /// <summary>
    /// Unloads an asset and removes it from the registry.
    /// </summary>
    /// <param name="id">The unique identifier of the asset to unload.</param>
    /// <returns>True if the asset was unloaded, false if it wasn't registered.</returns>
    bool UnloadAsset(string id);

    /// <summary>
    /// Retrieves a previously registered asset.
    /// </summary>
    /// <typeparam name="T">The asset type.</typeparam>
    /// <param name="id">The unique identifier of the asset.</param>
    /// <returns>The asset instance, or null if not found.</returns>
    T? GetAsset<T>(string id)
        where T : class;

    /// <summary>
    /// Checks if an asset is registered with the given identifier.
    /// </summary>
    /// <param name="id">The unique identifier to check.</param>
    /// <returns>True if the asset is registered, false otherwise.</returns>
    bool IsAssetRegistered(string id);
}
