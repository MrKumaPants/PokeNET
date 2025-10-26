using System;

namespace PokeNET.Domain.Assets;

/// <summary>
/// Exception thrown when asset loading fails.
/// </summary>
public class AssetLoadException : Exception
{
    /// <summary>
    /// Gets the asset path that failed to load.
    /// </summary>
    public string AssetPath { get; }

    /// <summary>
    /// Initializes a new asset load exception.
    /// </summary>
    /// <param name="assetPath">The asset path that failed.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public AssetLoadException(string assetPath, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        AssetPath = assetPath;
    }
}
