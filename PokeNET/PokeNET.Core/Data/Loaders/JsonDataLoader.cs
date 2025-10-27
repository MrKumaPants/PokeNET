using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PokeNET.Core.Data.Loaders;

/// <summary>
/// JSON-based data loader using System.Text.Json.
/// Supports loading both single objects and arrays.
/// </summary>
/// <typeparam name="T">The type of data to load.</typeparam>
public class JsonDataLoader<T> : BaseDataLoader<T>
    where T : class
{
    private readonly JsonSerializerOptions _jsonOptions;

    /// <inheritdoc/>
    protected override string[] SupportedExtensions => new[] { ".json" };

    /// <summary>
    /// Default JSON serializer options.
    /// </summary>
    public static readonly JsonSerializerOptions DefaultJsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

    /// <summary>
    /// Initializes a new JSON data loader.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="jsonOptions">Optional JSON serializer options.</param>
    public JsonDataLoader(ILogger<BaseDataLoader<T>> logger, JsonSerializerOptions? jsonOptions = null)
        : base(logger)
    {
        _jsonOptions = jsonOptions ?? DefaultJsonOptions;
    }

    /// <inheritdoc/>
    protected override async Task<T> LoadDataAsync(string path)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path);

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new DataLoadException($"File is empty: {path}");
            }

            var data = JsonSerializer.Deserialize<T>(json, _jsonOptions);

            if (data == null)
            {
                throw new DataLoadException($"JSON deserialization returned null: {path}");
            }

            return data;
        }
        catch (JsonException ex)
        {
            throw new DataLoadException($"Invalid JSON in file: {path}", ex);
        }
        catch (IOException ex)
        {
            throw new DataLoadException($"IO error reading file: {path}", ex);
        }
    }

    /// <inheritdoc/>
    public override bool Validate(T data)
    {
        if (!base.Validate(data))
            return false;

        // Additional validation for collections
        if (data is ICollection<object> collection)
        {
            if (collection.Count == 0)
            {
                Logger.LogWarning("Loaded collection is empty");
            }
        }

        return true;
    }
}

/// <summary>
/// Specialized JSON loader for loading arrays/lists of data.
/// </summary>
/// <typeparam name="T">The element type in the list.</typeparam>
public class JsonArrayLoader<T> : BaseDataLoader<List<T>>
    where T : class
{
    private readonly JsonSerializerOptions _jsonOptions;

    /// <inheritdoc/>
    protected override string[] SupportedExtensions => new[] { ".json" };

    /// <summary>
    /// Initializes a new JSON array loader.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="jsonOptions">Optional JSON serializer options.</param>
    public JsonArrayLoader(
        ILogger<BaseDataLoader<List<T>>> logger,
        JsonSerializerOptions? jsonOptions = null
    )
        : base(logger)
    {
        _jsonOptions = jsonOptions ?? JsonDataLoader<T>.DefaultJsonOptions;
    }

    /// <inheritdoc/>
    protected override async Task<List<T>> LoadDataAsync(string path)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path);

            if (string.IsNullOrWhiteSpace(json))
            {
                Logger.LogWarning("File is empty, returning empty list: {Path}", path);
                return new List<T>();
            }

            var data = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);

            if (data == null)
            {
                Logger.LogWarning(
                    "JSON deserialization returned null, returning empty list: {Path}",
                    path
                );
                return new List<T>();
            }

            return data;
        }
        catch (JsonException ex)
        {
            throw new DataLoadException($"Invalid JSON in file: {path}", ex);
        }
        catch (IOException ex)
        {
            throw new DataLoadException($"IO error reading file: {path}", ex);
        }
    }

    /// <inheritdoc/>
    public override bool Validate(List<T> data)
    {
        if (!base.Validate(data))
            return false;

        if (data.Count == 0)
        {
            Logger.LogWarning("Loaded array is empty");
        }

        return true;
    }
}
