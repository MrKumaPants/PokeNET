using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PokeNET.Core.Data;

namespace PokeNET.Core.Data;

/// <summary>
/// Manages loading and caching of static Pokemon game data from JSON files.
/// Implements thread-safe access with mod override support.
/// Follows the Single Responsibility Principle - only manages data loading.
/// </summary>
public class DataManager : IDataApi
{
    private readonly ILogger<DataManager> _logger;
    private readonly string _dataPath;
    private readonly List<string> _modDataPaths = new();
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    // Thread-safe caches
    private Dictionary<int, SpeciesData> _speciesById = new();
    private Dictionary<string, SpeciesData> _speciesByName = new();
    private Dictionary<string, MoveData> _movesByName = new();
    private Dictionary<int, ItemData> _itemsById = new();
    private Dictionary<string, ItemData> _itemsByName = new();
    private Dictionary<string, EncounterTable> _encountersById = new();

    private bool _isLoaded;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Initializes a new data manager.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="dataPath">Base path for game data JSON files.</param>
    public DataManager(ILogger<DataManager> logger, string dataPath)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dataPath = dataPath ?? throw new ArgumentNullException(nameof(dataPath));

        if (!Directory.Exists(dataPath))
        {
            _logger.LogWarning("Data path does not exist, creating: {DataPath}", dataPath);
            Directory.CreateDirectory(dataPath);
        }

        _logger.LogInformation("DataManager initialized with path: {DataPath}", dataPath);
    }

    /// <summary>
    /// Sets mod data paths for override support.
    /// Mods are checked in order of priority (first mod wins).
    /// </summary>
    /// <param name="modPaths">Mod data directory paths.</param>
    public void SetModDataPaths(IEnumerable<string> modPaths)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(modPaths);

        _modDataPaths.Clear();
        _modDataPaths.AddRange(modPaths.Where(Directory.Exists));

        _logger.LogInformation("Set {Count} mod data paths", _modDataPaths.Count);
    }

    // ==================== Species Data ====================

    /// <inheritdoc/>
    public async Task<SpeciesData?> GetSpeciesAsync(int speciesId)
    {
        await EnsureDataLoadedAsync();

        return _speciesById.TryGetValue(speciesId, out var species) ? species : null;
    }

    /// <inheritdoc/>
    public async Task<SpeciesData?> GetSpeciesByNameAsync(string name)
    {
        await EnsureDataLoadedAsync();

        var normalizedName = name?.Trim().ToLowerInvariant();
        return normalizedName != null && _speciesByName.TryGetValue(normalizedName, out var species)
            ? species
            : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SpeciesData>> GetAllSpeciesAsync()
    {
        await EnsureDataLoadedAsync();

        return _speciesById.Values.ToList().AsReadOnly();
    }

    // ==================== Move Data ====================

    /// <inheritdoc/>
    public async Task<MoveData?> GetMoveAsync(string moveName)
    {
        await EnsureDataLoadedAsync();

        var normalizedName = moveName?.Trim().ToLowerInvariant();
        return normalizedName != null && _movesByName.TryGetValue(normalizedName, out var move)
            ? move
            : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MoveData>> GetAllMovesAsync()
    {
        await EnsureDataLoadedAsync();

        return _movesByName.Values.ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MoveData>> GetMovesByTypeAsync(string type)
    {
        await EnsureDataLoadedAsync();

        return _movesByName
            .Values.Where(m => m.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    // ==================== Item Data ====================

    /// <inheritdoc/>
    public async Task<ItemData?> GetItemAsync(int itemId)
    {
        await EnsureDataLoadedAsync();

        return _itemsById.TryGetValue(itemId, out var item) ? item : null;
    }

    /// <inheritdoc/>
    public async Task<ItemData?> GetItemByNameAsync(string name)
    {
        await EnsureDataLoadedAsync();

        var normalizedName = name?.Trim().ToLowerInvariant();
        return normalizedName != null && _itemsByName.TryGetValue(normalizedName, out var item)
            ? item
            : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ItemData>> GetAllItemsAsync()
    {
        await EnsureDataLoadedAsync();

        return _itemsById.Values.ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ItemData>> GetItemsByCategoryAsync(ItemCategory category)
    {
        await EnsureDataLoadedAsync();

        return _itemsById.Values.Where(i => i.Category == category).ToList().AsReadOnly();
    }

    // ==================== Encounter Data ====================

    /// <inheritdoc/>
    public async Task<EncounterTable?> GetEncountersAsync(string locationId)
    {
        await EnsureDataLoadedAsync();

        var normalizedId = locationId?.Trim().ToLowerInvariant();
        return normalizedId != null && _encountersById.TryGetValue(normalizedId, out var table)
            ? table
            : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EncounterTable>> GetAllEncountersAsync()
    {
        await EnsureDataLoadedAsync();

        return _encountersById.Values.ToList().AsReadOnly();
    }

    // ==================== Cache Management ====================

    /// <inheritdoc/>
    public async Task ReloadDataAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.LogInformation("Reloading all game data...");

        await _loadLock.WaitAsync();
        try
        {
            _isLoaded = false;
            ClearCaches();
            await LoadAllDataAsync();
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <inheritdoc/>
    public bool IsDataLoaded() => _isLoaded;

    // ==================== Private Helper Methods ====================

    private async Task EnsureDataLoadedAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isLoaded)
            return;

        await _loadLock.WaitAsync();
        try
        {
            if (_isLoaded)
                return;

            await LoadAllDataAsync();
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task LoadAllDataAsync()
    {
        _logger.LogInformation("Loading game data from disk...");

        var tasks = new[]
        {
            LoadSpeciesDataAsync(),
            LoadMoveDataAsync(),
            LoadItemDataAsync(),
            LoadEncounterDataAsync(),
        };

        await Task.WhenAll(tasks);

        _isLoaded = true;
        _logger.LogInformation(
            "Data loaded: {SpeciesCount} species, {MoveCount} moves, {ItemCount} items, {EncounterCount} encounters",
            _speciesById.Count,
            _movesByName.Count,
            _itemsById.Count,
            _encountersById.Count
        );
    }

    private async Task LoadSpeciesDataAsync()
    {
        var species = await LoadJsonArrayAsync<SpeciesData>("species.json");

        var byId = new Dictionary<int, SpeciesData>();
        var byName = new Dictionary<string, SpeciesData>();

        foreach (var s in species)
        {
            byId[s.Id] = s;
            byName[s.Name.ToLowerInvariant()] = s;
        }

        _speciesById = byId;
        _speciesByName = byName;

        _logger.LogDebug("Loaded {Count} species", species.Count);
    }

    private async Task LoadMoveDataAsync()
    {
        var moves = await LoadJsonArrayAsync<MoveData>("moves.json");

        var byName = new Dictionary<string, MoveData>();

        foreach (var m in moves)
        {
            byName[m.Name.ToLowerInvariant()] = m;
        }

        _movesByName = byName;

        _logger.LogDebug("Loaded {Count} moves", moves.Count);
    }

    private async Task LoadItemDataAsync()
    {
        var items = await LoadJsonArrayAsync<ItemData>("items.json");

        var byId = new Dictionary<int, ItemData>();
        var byName = new Dictionary<string, ItemData>();

        foreach (var i in items)
        {
            byId[i.Id] = i;
            byName[i.Name.ToLowerInvariant()] = i;
        }

        _itemsById = byId;
        _itemsByName = byName;

        _logger.LogDebug("Loaded {Count} items", items.Count);
    }

    private async Task LoadEncounterDataAsync()
    {
        var encounters = await LoadJsonArrayAsync<EncounterTable>("encounters.json");

        var byId = new Dictionary<string, EncounterTable>();

        foreach (var e in encounters)
        {
            byId[e.LocationId.ToLowerInvariant()] = e;
        }

        _encountersById = byId;

        _logger.LogDebug("Loaded {Count} encounter tables", encounters.Count);
    }

    private async Task<List<T>> LoadJsonArrayAsync<T>(string fileName)
    {
        var path = ResolveDataPath(fileName);

        if (path == null)
        {
            _logger.LogWarning("Data file not found: {FileName}", fileName);
            return new List<T>();
        }

        try
        {
            _logger.LogDebug("Loading {FileName} from {Path}", fileName, path);

            var json = await File.ReadAllTextAsync(path);
            var data = JsonSerializer.Deserialize<List<T>>(json, JsonOptions);

            return data ?? new List<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load {FileName}", fileName);
            return new List<T>();
        }
    }

    private string? ResolveDataPath(string fileName)
    {
        // Check mod paths first (in priority order)
        foreach (var modPath in _modDataPaths)
        {
            var fullPath = Path.Combine(modPath, fileName);
            if (File.Exists(fullPath))
            {
                _logger.LogTrace("Resolved {FileName} to mod path: {Path}", fileName, fullPath);
                return fullPath;
            }
        }

        // Fall back to base data path
        var basePath = Path.Combine(_dataPath, fileName);
        if (File.Exists(basePath))
        {
            _logger.LogTrace("Resolved {FileName} to base path: {Path}", fileName, basePath);
            return basePath;
        }

        return null;
    }

    private void ClearCaches()
    {
        _speciesById.Clear();
        _speciesByName.Clear();
        _movesByName.Clear();
        _itemsById.Clear();
        _itemsByName.Clear();
        _encountersById.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogInformation("Disposing DataManager");

        ClearCaches();
        _loadLock.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
