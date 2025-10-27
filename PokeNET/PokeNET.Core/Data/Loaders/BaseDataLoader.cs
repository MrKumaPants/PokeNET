using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PokeNET.Core.Data.Loaders;

/// <summary>
/// Abstract base class for data loaders.
/// Provides common functionality and error handling for all data loaders.
/// Implements Template Method Pattern - defines the skeleton of loading algorithm.
/// </summary>
/// <typeparam name="T">The type of data to load.</typeparam>
public abstract class BaseDataLoader<T> : IDataLoader<T>
    where T : class
{
    /// <summary>
    /// Logger instance for this loader.
    /// </summary>
    protected readonly ILogger<BaseDataLoader<T>> Logger;

    /// <summary>
    /// Supported file extensions for this loader.
    /// </summary>
    protected abstract string[] SupportedExtensions { get; }

    /// <summary>
    /// Initializes a new data loader.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    protected BaseDataLoader(ILogger<BaseDataLoader<T>> logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public virtual bool CanHandle(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return false;

        var normalizedExt = extension.TrimStart('.');
        foreach (var supportedExt in SupportedExtensions)
        {
            if (
                normalizedExt.Equals(
                    supportedExt.TrimStart('.'),
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task<T> LoadAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new DataLoadException("Path cannot be null or empty");
        }

        if (!File.Exists(path))
        {
            throw new DataLoadException($"File not found: {path}");
        }

        var extension = Path.GetExtension(path);
        if (!CanHandle(extension))
        {
            throw new DataLoadException(
                $"This loader cannot handle {extension} files. Supported: {string.Join(", ", SupportedExtensions)}"
            );
        }

        try
        {
            Logger.LogDebug("Loading data from: {Path}", path);

            var data = await LoadDataAsync(path);

            if (data == null)
            {
                throw new DataLoadException($"Loaded data is null: {path}");
            }

            if (!Validate(data))
            {
                throw new DataLoadException($"Data validation failed: {path}");
            }

            Logger.LogInformation("Successfully loaded data from: {Path}", path);
            return data;
        }
        catch (DataLoadException)
        {
            throw; // Re-throw our own exceptions
        }
        catch (Exception ex)
        {
            throw new DataLoadException($"Failed to load data from {path}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<T?> TryLoadAsync(string path)
    {
        try
        {
            return await LoadAsync(path);
        }
        catch (DataLoadException ex)
        {
            Logger.LogWarning(ex, "Failed to load data (non-fatal): {Path}", path);
            return null;
        }
    }

    /// <inheritdoc/>
    public virtual bool Validate(T data)
    {
        // Default validation - just check for null
        // Override in derived classes for specific validation logic
        return data != null;
    }

    /// <summary>
    /// Performs the actual data loading.
    /// Must be implemented by derived classes.
    /// </summary>
    /// <param name="path">The file path to load from.</param>
    /// <returns>The loaded data.</returns>
    protected abstract Task<T> LoadDataAsync(string path);
}

/// <summary>
/// Exception thrown when data loading fails.
/// </summary>
public class DataLoadException : Exception
{
    /// <summary>
    /// Initializes a new DataLoadException.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DataLoadException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new DataLoadException with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DataLoadException(string message, Exception innerException)
        : base(message, innerException) { }
}
