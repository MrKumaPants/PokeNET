using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.Assets;

namespace PokeNET.Core.Assets.Loaders;

/// <summary>
/// Production-ready JSON asset loader with streaming, caching, and comprehensive error handling.
/// Implements IAssetLoader for generic JSON deserialization to any type.
/// </summary>
/// <typeparam name="T">The target type to deserialize JSON into.</typeparam>
public class JsonAssetLoader<T> : IAssetLoader<T>
    where T : class
{
    private readonly ILogger<JsonAssetLoader<T>> _logger;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly JsonDocumentOptions _documentOptions;
    private readonly Dictionary<string, T> _cache;
    private readonly object _cacheLock = new();
    private readonly long _streamingThreshold;

    /// <summary>
    /// Initializes a new JSON asset loader.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostics.</param>
    /// <param name="streamingThreshold">File size threshold (in bytes) for streaming mode. Default: 1MB.</param>
    public JsonAssetLoader(ILogger<JsonAssetLoader<T>> logger, long streamingThreshold = 1_048_576)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _streamingThreshold = streamingThreshold;
        _cache = new Dictionary<string, T>();

        // Configure JSON serialization options for performance and security
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            DefaultIgnoreCondition = System
                .Text
                .Json
                .Serialization
                .JsonIgnoreCondition
                .WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        // Configure document options for parsing
        _documentOptions = new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
            MaxDepth = 64,
        };

        _logger.LogDebug(
            "JsonAssetLoader<{TypeName}> initialized with streaming threshold: {Threshold} bytes",
            typeof(T).Name,
            _streamingThreshold
        );
    }

    /// <inheritdoc/>
    public T Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        if (!File.Exists(path))
        {
            throw new AssetLoadException(path, $"JSON asset file not found: {path}");
        }

        // Check cache first
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(path, out var cached))
            {
                _logger.LogTrace("JSON asset cache hit: {Path}", path);
                return cached;
            }
        }

        try
        {
            var fileInfo = new FileInfo(path);
            var fileSize = fileInfo.Length;

            _logger.LogDebug("Loading JSON asset: {Path} (Size: {Size} bytes)", path, fileSize);

            T asset;

            // Use streaming for large files to reduce memory footprint
            if (fileSize > _streamingThreshold)
            {
                _logger.LogDebug("Using streaming deserialization for large file: {Path}", path);
                asset = LoadWithStreaming(path);
            }
            else
            {
                // Use synchronous loading for smaller files (better performance)
                asset = LoadSynchronous(path);
            }

            // Validate the loaded asset
            if (asset == null)
            {
                throw new AssetLoadException(path, $"Deserialization resulted in null for: {path}");
            }

            // Cache the loaded asset
            lock (_cacheLock)
            {
                _cache[path] = asset;
            }

            _logger.LogInformation(
                "Successfully loaded JSON asset: {Path} as {TypeName}",
                path,
                typeof(T).Name
            );

            return asset;
        }
        catch (JsonException jsonEx)
        {
            // Provide detailed error message with line/position information
            var errorMessage = $"Malformed JSON in file: {path}";
            if (jsonEx.LineNumber.HasValue)
            {
                errorMessage +=
                    $" at line {jsonEx.LineNumber}, position {jsonEx.BytePositionInLine}";
            }
            errorMessage += $". Error: {jsonEx.Message}";

            _logger.LogError(jsonEx, "JSON parsing error: {Message}", errorMessage);
            throw new AssetLoadException(path, errorMessage, jsonEx);
        }
        catch (AssetLoadException)
        {
            // Re-throw AssetLoadException as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading JSON asset: {Path}", path);
            throw new AssetLoadException(path, $"Failed to load JSON asset: {path}", ex);
        }
    }

    /// <inheritdoc/>
    public bool CanHandle(string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extension, nameof(extension));

        var normalizedExtension = extension.TrimStart('.').ToLowerInvariant();
        var canHandle = normalizedExtension == "json";

        _logger.LogTrace(
            "CanHandle check for extension '{Extension}': {Result}",
            extension,
            canHandle
        );

        return canHandle;
    }

    /// <summary>
    /// Loads JSON synchronously for smaller files.
    /// </summary>
    private T LoadSynchronous(string path)
    {
        var json = File.ReadAllText(path);

        // Pre-validate JSON structure before deserialization
        ValidateJsonStructure(json, path);

        var asset = JsonSerializer.Deserialize<T>(json, _serializerOptions);
        if (asset == null)
        {
            throw new AssetLoadException(path, $"JSON deserialization returned null for: {path}");
        }

        return asset;
    }

    /// <summary>
    /// Loads JSON using streaming for large files to minimize memory usage.
    /// </summary>
    private T LoadWithStreaming(string path)
    {
        using var fileStream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.SequentialScan
        );

        var asset = JsonSerializer.Deserialize<T>(fileStream, _serializerOptions);
        if (asset == null)
        {
            throw new AssetLoadException(
                path,
                $"JSON streaming deserialization returned null for: {path}"
            );
        }

        return asset;
    }

    /// <summary>
    /// Validates JSON structure before deserialization to provide better error messages.
    /// </summary>
    private void ValidateJsonStructure(string json, string path)
    {
        try
        {
            using var document = JsonDocument.Parse(json, _documentOptions);
            var root = document.RootElement;

            // Perform basic type validation
            ValidateTypeCompatibility(root, path);
        }
        catch (JsonException jsonEx)
        {
            var errorMessage = $"Invalid JSON structure in {path}";
            if (jsonEx.LineNumber.HasValue)
            {
                errorMessage +=
                    $" at line {jsonEx.LineNumber}, position {jsonEx.BytePositionInLine}";
            }

            throw new AssetLoadException(path, errorMessage, jsonEx);
        }
    }

    /// <summary>
    /// Validates that the JSON structure is compatible with the target type.
    /// </summary>
    private void ValidateTypeCompatibility(JsonElement element, string path)
    {
        var targetType = typeof(T);

        // Check if target type expects an array/collection
        if (
            targetType.IsArray
            || (
                targetType.IsGenericType
                && (
                    targetType.GetGenericTypeDefinition() == typeof(List<>)
                    || targetType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    || targetType.GetGenericTypeDefinition() == typeof(IList<>)
                    || targetType.GetGenericTypeDefinition() == typeof(ICollection<>)
                )
            )
        )
        {
            if (element.ValueKind != JsonValueKind.Array)
            {
                throw new AssetLoadException(
                    path,
                    $"Type mismatch: Expected JSON array for collection type {targetType.Name}, but found {element.ValueKind}"
                );
            }
        }
        // Check if target type expects an object
        else if (targetType.IsClass)
        {
            if (
                element.ValueKind != JsonValueKind.Object
                && element.ValueKind != JsonValueKind.Array
            )
            {
                _logger.LogWarning(
                    "Type compatibility warning: Expected JSON object for class type {TypeName}, but found {ValueKind}",
                    targetType.Name,
                    element.ValueKind
                );
            }
        }
    }

    /// <summary>
    /// Clears the internal cache. Useful for testing or when memory optimization is needed.
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            var count = _cache.Count;
            _cache.Clear();
            _logger.LogDebug("Cleared JSON asset cache ({Count} entries)", count);
        }
    }

    /// <summary>
    /// Gets the current cache size.
    /// </summary>
    public int CacheSize
    {
        get
        {
            lock (_cacheLock)
            {
                return _cache.Count;
            }
        }
    }

    /// <summary>
    /// Checks if a specific path is cached.
    /// </summary>
    public bool IsCached(string path)
    {
        lock (_cacheLock)
        {
            return _cache.ContainsKey(path);
        }
    }
}
