using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using PokeNET.Core.Assets;

namespace PokeNET.Core.Assets.Loaders;

/// <summary>
/// Production-ready asset loader for MonoGame Texture2D assets.
/// Supports PNG, JPG, BMP formats with advanced features:
/// - Async loading with cancellation
/// - Texture pooling for frequently loaded textures
/// - Memory tracking and reporting
/// - Premultiply alpha options
/// - Comprehensive error handling
/// </summary>
public class TextureAssetLoader : IAssetLoader<Texture2D>, IDisposable
{
    private readonly ILogger<TextureAssetLoader> _logger;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ConcurrentDictionary<string, TexturePoolEntry> _texturePool;
    private readonly TextureLoadOptions _defaultOptions;
    private long _totalMemoryUsageBytes;
    private bool _disposed;

    private static readonly HashSet<string> SupportedExtensions = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".bmp",
        ".gif",
    };

    /// <summary>
    /// Gets the total memory usage of loaded textures in megabytes.
    /// </summary>
    public double MemoryUsageMB => _totalMemoryUsageBytes / (1024.0 * 1024.0);

    /// <summary>
    /// Gets the number of textures currently in the pool.
    /// </summary>
    public int PooledTextureCount => _texturePool.Count;

    /// <summary>
    /// Initializes a new TextureAssetLoader.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostics.</param>
    /// <param name="graphicsDevice">MonoGame graphics device for texture creation.</param>
    /// <param name="options">Default loading options (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown if required dependencies are null.</exception>
    public TextureAssetLoader(
        ILogger<TextureAssetLoader> logger,
        GraphicsDevice graphicsDevice,
        TextureLoadOptions? options = null
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _texturePool = new ConcurrentDictionary<string, TexturePoolEntry>();
        _defaultOptions = options ?? TextureLoadOptions.Default;

        _logger.LogInformation(
            "TextureAssetLoader initialized with default options: PremultiplyAlpha={PremultiplyAlpha}",
            _defaultOptions.PremultiplyAlpha
        );
    }

    /// <inheritdoc/>
    public bool CanHandle(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return false;

        return SupportedExtensions.Contains(extension);
    }

    /// <inheritdoc/>
    public Texture2D Load(string path)
    {
        return LoadAsync(path, _defaultOptions, CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Asynchronously loads a texture from the specified path with custom options.
    /// </summary>
    /// <param name="path">The file path to load the texture from.</param>
    /// <param name="options">Loading options (null uses default).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The loaded Texture2D instance.</returns>
    /// <exception cref="AssetLoadException">Thrown when loading fails.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if loader is disposed.</exception>
    /// <exception cref="OperationCanceledException">Thrown if operation is cancelled.</exception>
    public async Task<Texture2D> LoadAsync(
        string path,
        TextureLoadOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(TextureAssetLoader));
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        options ??= _defaultOptions;

        // Check if GraphicsDevice is valid
        if (_graphicsDevice.IsDisposed)
        {
            throw new AssetLoadException(path, "GraphicsDevice has been disposed");
        }

        // Check pool if enabled
        if (options.UsePooling && _texturePool.TryGetValue(path, out var poolEntry))
        {
            poolEntry.ReferenceCount++;
            _logger.LogTrace(
                "Texture pool hit for {Path} (References: {RefCount})",
                path,
                poolEntry.ReferenceCount
            );
            return poolEntry.Texture;
        }

        // Validate file exists
        if (!File.Exists(path))
        {
            throw new AssetLoadException(path, $"Texture file not found: {path}");
        }

        // Validate extension
        var extension = Path.GetExtension(path);
        if (!CanHandle(extension))
        {
            throw new AssetLoadException(
                path,
                $"Unsupported texture format: {extension}. Supported formats: {string.Join(", ", SupportedExtensions)}"
            );
        }

        try
        {
            _logger.LogDebug(
                "Loading texture from {Path} with options: PremultiplyAlpha={PremultiplyAlpha}, UsePooling={UsePooling}",
                path,
                options.PremultiplyAlpha,
                options.UsePooling
            );

            // Read file asynchronously
            byte[] imageData;
            try
            {
                imageData = await File.ReadAllBytesAsync(path, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new AssetLoadException(
                    path,
                    $"Failed to read texture file: {ex.Message}",
                    ex
                );
            }

            // Check for cancellation before expensive texture creation
            cancellationToken.ThrowIfCancellationRequested();

            // Validate image data
            if (imageData.Length == 0)
            {
                throw new AssetLoadException(path, "Texture file is empty");
            }

            // Detect and validate image format
            if (!ValidateImageFormat(imageData, extension))
            {
                throw new AssetLoadException(path, $"Invalid or corrupted {extension} file format");
            }

            // Load texture on main thread (MonoGame requirement)
            Texture2D texture;
            try
            {
                using var stream = new MemoryStream(imageData);
                texture = Texture2D.FromStream(_graphicsDevice, stream);

                if (texture == null)
                {
                    throw new AssetLoadException(path, "Texture2D.FromStream returned null");
                }
            }
            catch (Exception ex)
            {
                throw new AssetLoadException(path, $"Failed to create Texture2D: {ex.Message}", ex);
            }

            // Check for cancellation before post-processing
            cancellationToken.ThrowIfCancellationRequested();

            // Apply premultiply alpha if needed
            if (options.PremultiplyAlpha)
            {
                ApplyPremultiplyAlpha(texture);
            }

            // Calculate memory usage
            var memoryUsage = CalculateTextureMemoryUsage(texture);
            Interlocked.Add(ref _totalMemoryUsageBytes, memoryUsage);

            // Add to pool if enabled
            if (options.UsePooling)
            {
                var entry = new TexturePoolEntry(texture, memoryUsage, 1);
                _texturePool[path] = entry;
                _logger.LogTrace(
                    "Added texture to pool: {Path} ({MemoryMB:F2} MB)",
                    path,
                    memoryUsage / (1024.0 * 1024.0)
                );
            }

            _logger.LogInformation(
                "Successfully loaded texture: {Path} (Size: {Width}x{Height}, Memory: {MemoryMB:F2} MB)",
                path,
                texture.Width,
                texture.Height,
                memoryUsage / (1024.0 * 1024.0)
            );

            return texture;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Texture loading cancelled: {Path}", path);
            throw;
        }
        catch (AssetLoadException)
        {
            throw;
        }
        catch (OutOfMemoryException ex)
        {
            throw new AssetLoadException(
                path,
                $"Out of memory while loading texture (Current usage: {MemoryUsageMB:F2} MB)",
                ex
            );
        }
        catch (Exception ex)
        {
            throw new AssetLoadException(
                path,
                $"Unexpected error loading texture: {ex.Message}",
                ex
            );
        }
    }

    /// <summary>
    /// Releases a pooled texture reference. If reference count reaches zero, texture is disposed.
    /// </summary>
    /// <param name="path">The path of the texture to release.</param>
    public void ReleasePooledTexture(string path)
    {
        if (_texturePool.TryGetValue(path, out var entry))
        {
            entry.ReferenceCount--;
            _logger.LogTrace(
                "Released texture reference: {Path} (Remaining: {RefCount})",
                path,
                entry.ReferenceCount
            );

            if (entry.ReferenceCount <= 0)
            {
                _texturePool.TryRemove(path, out _);
                entry.Texture.Dispose();
                Interlocked.Add(ref _totalMemoryUsageBytes, -entry.MemoryUsage);
                _logger.LogDebug("Disposed pooled texture: {Path}", path);
            }
        }
    }

    /// <summary>
    /// Clears all pooled textures and disposes them.
    /// </summary>
    public void ClearPool()
    {
        _logger.LogInformation(
            "Clearing texture pool ({Count} textures, {MemoryMB:F2} MB)",
            _texturePool.Count,
            MemoryUsageMB
        );

        foreach (var kvp in _texturePool)
        {
            try
            {
                kvp.Value.Texture.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing pooled texture: {Path}", kvp.Key);
            }
        }

        _texturePool.Clear();
        Interlocked.Exchange(ref _totalMemoryUsageBytes, 0);
    }

    /// <summary>
    /// Validates image format based on file signature (magic bytes).
    /// </summary>
    private bool ValidateImageFormat(byte[] data, string extension)
    {
        if (data.Length < 4)
            return false;

        return extension.ToLowerInvariant() switch
        {
            ".png" => data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47,
            ".jpg" or ".jpeg" => data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF,
            ".bmp" => data[0] == 0x42 && data[1] == 0x4D,
            ".gif" => data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46,
            _ => true, // Unknown format, let MonoGame handle it
        };
    }

    /// <summary>
    /// Applies premultiplied alpha to the texture data.
    /// </summary>
    private void ApplyPremultiplyAlpha(Texture2D texture)
    {
        try
        {
            var data = new Microsoft.Xna.Framework.Color[texture.Width * texture.Height];
            texture.GetData(data);

            for (int i = 0; i < data.Length; i++)
            {
                var color = data[i];
                var alpha = color.A / 255f;
                data[i] = new Microsoft.Xna.Framework.Color(
                    (byte)(color.R * alpha),
                    (byte)(color.G * alpha),
                    (byte)(color.B * alpha),
                    color.A
                );
            }

            texture.SetData(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply premultiply alpha");
        }
    }

    /// <summary>
    /// Calculates memory usage for a texture in bytes.
    /// </summary>
    private static long CalculateTextureMemoryUsage(Texture2D texture)
    {
        // Estimate: width * height * 4 bytes per pixel (RGBA)
        return (long)texture.Width * texture.Height * 4;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogInformation(
            "Disposing TextureAssetLoader (Total memory freed: {MemoryMB:F2} MB)",
            MemoryUsageMB
        );
        ClearPool();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Options for texture loading.
/// </summary>
public class TextureLoadOptions
{
    /// <summary>
    /// Gets or sets whether to premultiply alpha values.
    /// Recommended for proper blending in most rendering scenarios.
    /// </summary>
    public bool PremultiplyAlpha { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use texture pooling for this load.
    /// Pooled textures are reference counted and shared.
    /// </summary>
    public bool UsePooling { get; set; } = true;

    /// <summary>
    /// Default loading options.
    /// </summary>
    public static TextureLoadOptions Default =>
        new() { PremultiplyAlpha = true, UsePooling = true };
}

/// <summary>
/// Represents a pooled texture entry with reference counting.
/// </summary>
internal class TexturePoolEntry
{
    public Texture2D Texture { get; }
    public long MemoryUsage { get; }
    public int ReferenceCount { get; set; }

    public TexturePoolEntry(Texture2D texture, long memoryUsage, int referenceCount)
    {
        Texture = texture;
        MemoryUsage = memoryUsage;
        ReferenceCount = referenceCount;
    }
}
