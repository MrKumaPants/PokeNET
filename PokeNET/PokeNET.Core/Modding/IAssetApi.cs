using System;
using System.Threading;
using System.Threading.Tasks;

namespace PokeNET.Core.Modding;

/// <summary>
/// API for loading and accessing game assets from mods.
/// </summary>
/// <remarks>
/// <para>
/// The asset API provides access to:
/// - Data files (JSON, XML)
/// - Textures and sprites
/// - Audio files
/// - Fonts
/// - Other game assets
/// </para>
/// <para>
/// Assets are loaded with mod override support. When an asset is requested,
/// the system searches in this order:
/// 1. Last loaded mod's assets
/// 2. Previous mods' assets (in reverse load order)
/// 3. Base game assets
/// </para>
/// </remarks>
public interface IAssetApi
{
    /// <summary>
    /// Loads a data file (JSON/XML) and deserializes it to the specified type.
    /// </summary>
    /// <typeparam name="T">Type to deserialize to.</typeparam>
    /// <param name="assetPath">Relative path to the asset (e.g., "creatures/pikachu.json").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized data.</returns>
    /// <exception cref="AssetNotFoundException">Asset file not found.</exception>
    /// <exception cref="AssetLoadException">Asset failed to load or deserialize.</exception>
    /// <remarks>
    /// The path is relative to the mod's data directory (configured in manifest).
    /// Example: "creatures/pikachu.json" -> "Mods/MyMod/Data/creatures/pikachu.json"
    /// </remarks>
    Task<T> LoadDataAsync<T>(string assetPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a texture asset.
    /// </summary>
    /// <param name="assetPath">Relative path to the texture (e.g., "sprites/pikachu.png").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded texture.</returns>
    /// <exception cref="AssetNotFoundException">Texture file not found.</exception>
    /// <exception cref="AssetLoadException">Texture failed to load.</exception>
    Task<ITexture> LoadTextureAsync(
        string assetPath,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Loads an audio asset.
    /// </summary>
    /// <param name="assetPath">Relative path to the audio file (e.g., "music/battle.wav").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded audio.</returns>
    /// <exception cref="AssetNotFoundException">Audio file not found.</exception>
    /// <exception cref="AssetLoadException">Audio failed to load.</exception>
    Task<IAudioClip> LoadAudioAsync(
        string assetPath,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Checks if an asset exists (in this mod or base game).
    /// </summary>
    /// <param name="assetPath">Relative path to the asset.</param>
    /// <returns>True if the asset exists; otherwise false.</returns>
    bool AssetExists(string assetPath);

    /// <summary>
    /// Gets the actual file path where an asset will be loaded from.
    /// </summary>
    /// <param name="assetPath">Relative path to the asset.</param>
    /// <returns>Full file system path, or null if asset not found.</returns>
    /// <remarks>
    /// Useful for debugging asset overrides:
    /// <code>
    /// var path = context.Assets.ResolveAssetPath("sprites/pikachu.png");
    /// logger.LogDebug($"Loading pikachu sprite from: {path}");
    /// </code>
    /// </remarks>
    string? ResolveAssetPath(string assetPath);

    /// <summary>
    /// Invalidates the cache for a specific asset, forcing it to reload.
    /// </summary>
    /// <param name="assetPath">Relative path to the asset.</param>
    /// <remarks>
    /// Primarily for development hot-reload scenarios.
    /// </remarks>
    void InvalidateCache(string assetPath);

    /// <summary>
    /// Registers a custom asset loader for a specific file extension.
    /// </summary>
    /// <typeparam name="T">Type the loader produces.</typeparam>
    /// <param name="extension">File extension (e.g., ".custom").</param>
    /// <param name="loader">Loader function.</param>
    /// <remarks>
    /// Allows mods to support custom asset formats:
    /// <code>
    /// context.Assets.RegisterLoader&lt;MyCustomData&gt;(".mydata", async (path, ct) =>
    /// {
    ///     var content = await File.ReadAllTextAsync(path, ct);
    ///     return MyCustomDataParser.Parse(content);
    /// });
    /// </code>
    /// </remarks>
    void RegisterLoader<T>(string extension, Func<string, CancellationToken, Task<T>> loader);
}

/// <summary>
/// Represents a loaded texture asset.
/// </summary>
/// <remarks>
/// This is a lightweight wrapper around the underlying graphics API texture.
/// </remarks>
public interface ITexture : IDisposable
{
    /// <summary>
    /// Width of the texture in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Height of the texture in pixels.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Asset path this texture was loaded from.
    /// </summary>
    string AssetPath { get; }

    /// <summary>
    /// Gets the underlying MonoGame Texture2D object.
    /// </summary>
    /// <returns>The native texture object.</returns>
    /// <remarks>
    /// Use this when you need direct access to the MonoGame API.
    /// Cast to MonoGame.Framework.Graphics.Texture2D.
    /// </remarks>
    object GetNativeTexture();
}

/// <summary>
/// Represents a loaded audio clip.
/// </summary>
public interface IAudioClip : IDisposable
{
    /// <summary>
    /// Duration of the audio in seconds.
    /// </summary>
    double Duration { get; }

    /// <summary>
    /// Asset path this audio was loaded from.
    /// </summary>
    string AssetPath { get; }

    /// <summary>
    /// Whether this audio is streamed (large files) or fully loaded.
    /// </summary>
    bool IsStreamed { get; }

    /// <summary>
    /// Plays the audio clip.
    /// </summary>
    /// <param name="volume">Volume (0.0 to 1.0).</param>
    /// <param name="loop">Whether to loop the audio.</param>
    void Play(float volume = 1.0f, bool loop = false);

    /// <summary>
    /// Stops playing the audio clip.
    /// </summary>
    void Stop();
}

/// <summary>
/// Exception thrown when an asset is not found.
/// </summary>
public class AssetNotFoundException : Exception
{
    /// <summary>
    /// Path of the asset that was not found.
    /// </summary>
    public string AssetPath { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="AssetNotFoundException"/>.
    /// </summary>
    public AssetNotFoundException(string assetPath)
        : base($"Asset not found: {assetPath}")
    {
        AssetPath = assetPath;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AssetNotFoundException"/> with a custom message.
    /// </summary>
    public AssetNotFoundException(string assetPath, string message)
        : base(message)
    {
        AssetPath = assetPath;
    }
}

/// <summary>
/// Exception thrown when an asset fails to load.
/// </summary>
public class AssetLoadException : Exception
{
    /// <summary>
    /// Path of the asset that failed to load.
    /// </summary>
    public string AssetPath { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="AssetLoadException"/>.
    /// </summary>
    public AssetLoadException(string assetPath, string message)
        : base($"Failed to load asset '{assetPath}': {message}")
    {
        AssetPath = assetPath;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AssetLoadException"/> with an inner exception.
    /// </summary>
    public AssetLoadException(string assetPath, string message, Exception innerException)
        : base($"Failed to load asset '{assetPath}': {message}", innerException)
    {
        AssetPath = assetPath;
    }
}
