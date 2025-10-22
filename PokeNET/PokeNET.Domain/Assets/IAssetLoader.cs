namespace PokeNET.Domain.Assets;

/// <summary>
/// Generic interface for loading specific types of assets.
/// Follows the Single Responsibility Principle and Open/Closed Principle.
/// Implementations can be added for new asset types without modifying existing code.
/// </summary>
/// <typeparam name="T">The type of asset this loader handles.</typeparam>
public interface IAssetLoader<T> where T : class
{
    /// <summary>
    /// Loads an asset from the specified path.
    /// </summary>
    /// <param name="path">The asset path to load from.</param>
    /// <returns>The loaded asset instance.</returns>
    /// <exception cref="AssetLoadException">Thrown when asset cannot be loaded.</exception>
    T Load(string path);

    /// <summary>
    /// Determines if this loader can handle the specified file extension.
    /// </summary>
    /// <param name="extension">The file extension (e.g., ".png", ".json").</param>
    /// <returns>True if this loader can handle the extension.</returns>
    bool CanHandle(string extension);
}
