using System.Threading.Tasks;

namespace PokeNET.Core.Data.Loaders;

/// <summary>
/// Generic interface for loading data of type T from storage.
/// Provides extensibility for different data loading strategies and formats.
/// Follows the Strategy Pattern - allows different loading implementations.
/// </summary>
/// <typeparam name="T">The type of data to load.</typeparam>
public interface IDataLoader<T>
    where T : class
{
    /// <summary>
    /// Loads data from the specified path.
    /// </summary>
    /// <param name="path">The file path to load from.</param>
    /// <returns>The loaded data of type T.</returns>
    /// <exception cref="DataLoadException">Thrown when loading fails.</exception>
    Task<T> LoadAsync(string path);

    /// <summary>
    /// Loads data from the specified path, returning null on failure.
    /// </summary>
    /// <param name="path">The file path to load from.</param>
    /// <returns>The loaded data, or null if loading fails.</returns>
    Task<T?> TryLoadAsync(string path);

    /// <summary>
    /// Checks if this loader can handle the specified file format.
    /// </summary>
    /// <param name="extension">The file extension (e.g., ".json", ".xml").</param>
    /// <returns>True if this loader can handle the format.</returns>
    bool CanHandle(string extension);

    /// <summary>
    /// Validates the data after loading.
    /// </summary>
    /// <param name="data">The data to validate.</param>
    /// <returns>True if the data is valid.</returns>
    bool Validate(T data);
}
