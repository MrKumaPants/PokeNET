using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PokeNET.Core.Data;
using PokeNET.Core.Data.Extensions;

namespace PokeNET.Core.Data;

/// <summary>
/// Manages loading and caching of static Pokemon game data from JSON files.
/// Implements thread-safe access with mod override support using Entity Framework Core.
/// Follows the Single Responsibility Principle - only manages data loading.
/// </summary>
public class DataManager : IDataApi, IDisposable
{
    private readonly ILogger<DataManager> _logger;
    private readonly GameDataContext _context;
    private readonly string _dataPath;
    private readonly List<string> _modDataPaths = new();
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private bool _isLoaded;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters =
        {
            new System.Text.Json.Serialization.JsonStringEnumConverter(
                System.Text.Json.JsonNamingPolicy.CamelCase
            ),
        },
    };

    /// <summary>
    /// Initializes a new data manager.
    /// </summary>
    /// <param name="context">Entity Framework Core context for game data.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="dataPath">Base path for game data JSON files.</param>
    public DataManager(GameDataContext context, ILogger<DataManager> logger, string dataPath)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
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
    public async Task<SpeciesData?> GetSpeciesAsync(string speciesId)
    {
        await EnsureDataLoadedAsync();

        return await _context.Species.AsNoTracking().FirstOrDefaultAsync(s => s.Id == speciesId);
    }

    /// <inheritdoc/>
    public async Task<SpeciesData?> GetSpeciesByNameAsync(string name)
    {
        await EnsureDataLoadedAsync();

        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalizedName = name.Trim();
        return await _context
            .Species.AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)
            );
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SpeciesData>> GetAllSpeciesAsync()
    {
        await EnsureDataLoadedAsync();

        return await _context.Species.AsNoTracking().ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SpeciesData>> QuerySpeciesAsync(
        Expression<Func<SpeciesData, bool>> predicate
    )
    {
        await EnsureDataLoadedAsync();

        return await _context.Species.AsNoTracking().Where(predicate).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SpeciesData>> GetSpeciesByTypeAsync(string type)
    {
        await EnsureDataLoadedAsync();

        return await _context.GetSpeciesByTypeAsync(type);
    }

    // ==================== Move Data ====================

    /// <inheritdoc/>
    public async Task<MoveData?> GetMoveAsync(string moveId)
    {
        await EnsureDataLoadedAsync();

        return await _context.Moves.AsNoTracking().FirstOrDefaultAsync(m => m.Id == moveId);
    }

    /// <inheritdoc/>
    public async Task<MoveData?> GetMoveByNameAsync(string name)
    {
        await EnsureDataLoadedAsync();

        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalizedName = name.Trim();
        return await _context
            .Moves.AsNoTracking()
            .FirstOrDefaultAsync(m =>
                m.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)
            );
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MoveData>> GetAllMovesAsync()
    {
        await EnsureDataLoadedAsync();

        return await _context.Moves.AsNoTracking().ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MoveData>> GetMovesByTypeAsync(string type)
    {
        await EnsureDataLoadedAsync();

        if (string.IsNullOrWhiteSpace(type))
            return Array.Empty<MoveData>().ToList().AsReadOnly();

        return await _context
            .Moves.AsNoTracking()
            .Where(m => m.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MoveData>> QueryMovesAsync(
        Expression<Func<MoveData, bool>> predicate
    )
    {
        await EnsureDataLoadedAsync();

        return await _context.Moves.AsNoTracking().Where(predicate).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MoveData>> GetMovesByPowerRangeAsync(int minPower, int maxPower)
    {
        await EnsureDataLoadedAsync();

        return await _context.GetMovesByPowerRangeAsync(minPower, maxPower);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MoveData>> GetMovesByCategoryAsync(MoveCategory category)
    {
        await EnsureDataLoadedAsync();

        return await _context.GetMovesByCategoryAsync(category);
    }

    // ==================== Item Data ====================

    /// <inheritdoc/>
    public async Task<ItemData?> GetItemAsync(string itemId)
    {
        await EnsureDataLoadedAsync();

        return await _context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == itemId);
    }

    /// <inheritdoc/>
    public async Task<ItemData?> GetItemByNameAsync(string name)
    {
        await EnsureDataLoadedAsync();

        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalizedName = name.Trim();
        return await _context
            .Items.AsNoTracking()
            .FirstOrDefaultAsync(i =>
                i.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)
            );
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ItemData>> GetAllItemsAsync()
    {
        await EnsureDataLoadedAsync();

        return await _context.Items.AsNoTracking().ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ItemData>> GetItemsByCategoryAsync(ItemCategory category)
    {
        await EnsureDataLoadedAsync();

        return await _context.Items.AsNoTracking().Where(i => i.Category == category).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ItemData>> QueryItemsAsync(
        Expression<Func<ItemData, bool>> predicate
    )
    {
        await EnsureDataLoadedAsync();

        return await _context.Items.AsNoTracking().Where(predicate).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ItemData>> GetItemsByPriceRangeAsync(int minPrice, int maxPrice)
    {
        await EnsureDataLoadedAsync();

        return await _context.GetItemsByPriceRangeAsync(minPrice, maxPrice);
    }

    // ==================== Encounter Data ====================

    /// <inheritdoc/>
    public async Task<EncounterTable?> GetEncountersAsync(string locationId)
    {
        await EnsureDataLoadedAsync();

        return await _context
            .Encounters.AsNoTracking()
            .FirstOrDefaultAsync(e => e.LocationId == locationId);
    }

    /// <inheritdoc/>
    public async Task<EncounterTable?> GetEncountersByNameAsync(string name)
    {
        await EnsureDataLoadedAsync();

        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalizedName = name.Trim();
        return await _context
            .Encounters.AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.LocationName.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)
            );
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EncounterTable>> GetAllEncountersAsync()
    {
        await EnsureDataLoadedAsync();

        return await _context.Encounters.AsNoTracking().ToListAsync();
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

        // Load data from JSON files first (before clearing database)
        var species = await LoadAllJsonFromDirectoryAsync<SpeciesData>("species");
        var moves = await LoadAllJsonFromDirectoryAsync<MoveData>("moves");
        var items = await LoadAllJsonFromDirectoryAsync<ItemData>("items");
        var encounters = await LoadAllJsonFromDirectoryAsync<EncounterTable>("encounters");
        var types = await LoadAllJsonFromDirectoryAsync<TypeData>("types");

        // Clear existing data and change tracker
        _context.Species.RemoveRange(_context.Species);
        _context.Moves.RemoveRange(_context.Moves);
        _context.Items.RemoveRange(_context.Items);
        _context.Encounters.RemoveRange(_context.Encounters);
        _context.Types.RemoveRange(_context.Types);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear(); // Clear change tracker to avoid tracking conflicts

        // Process loaded data and add to database
        // IMPORTANT: Add and save species first separately to avoid owned entity tracking conflicts
        // EF Core needs each batch of entities saved before adding more with similar owned entity keys
        await ProcessSpeciesDataAsync(species);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear(); // Clear after species to avoid conflicts with other entities

        await ProcessMoveDataAsync(moves);
        await ProcessItemDataAsync(items);
        await ProcessEncounterDataAsync(encounters);
        await ProcessTypeDataAsync(types);

        // Save all remaining loaded data to database
        await _context.SaveChangesAsync();

        _isLoaded = true;
        _logger.LogInformation(
            "Data loaded: {SpeciesCount} species, {MoveCount} moves, {ItemCount} items, {EncounterCount} encounters, {TypeCount} types",
            await _context.Species.CountAsync(),
            await _context.Moves.CountAsync(),
            await _context.Items.CountAsync(),
            await _context.Encounters.CountAsync(),
            await _context.Types.CountAsync()
        );
    }

    private async Task ProcessSpeciesDataAsync(List<SpeciesData> species)
    {
        // Process entities: mod data comes first in the list (from LoadAllJsonFromDirectoryAsync),
        // so first entity wins (mods override base). Use a dictionary to track which IDs we've seen.
        var seenIds = new HashSet<string>();
        var speciesToAdd = new List<SpeciesData>();

        foreach (var s in species)
        {
            if (seenIds.Contains(s.Id))
            {
                // Skip duplicates - mods were loaded first, so first occurrence wins
                continue;
            }

            seenIds.Add(s.Id);

            // Create a clean copy to avoid tracking conflicts with owned entities
            // This ensures each LevelMove, Evolution, etc. is a new instance
            var cleanSpecies = CreateCleanSpeciesCopy(s);
            speciesToAdd.Add(cleanSpecies);
        }

        // Add and save entities one at a time to avoid EF Core owned entity tracking conflicts
        // CRITICAL ISSUE: EF Core tracks owned entities by their composite key (Level, MoveName) during Add(),
        // and when multiple parent entities (SpeciesData) have owned entities with the same composite key values,
        // EF Core incorrectly identifies them as the same entity instance, causing a tracking conflict.
        //
        // This happens BEFORE SaveChanges() - EF Core's change tracker identifies conflicts during Add/AddRange.
        // The only reliable solution is to save immediately after each entity to persist it and clear tracking state.
        //
        // PERFORMANCE NOTE: This is slower than bulk operations, but data loading happens once at startup,
        // so the performance impact is acceptable for correctness. Typically 100-1000 species = 100-1000ms overhead.
        foreach (var cleanSpecies in speciesToAdd)
        {
            // Check if species already exists in database (shouldn't happen due to deduplication, but safety check)
            var existing = await _context
                .Species.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == cleanSpecies.Id);

            if (existing != null)
            {
                _logger.LogWarning(
                    "Skipping duplicate species ID: {SpeciesId} ({SpeciesName})",
                    cleanSpecies.Id,
                    cleanSpecies.Name
                );
                continue;
            }

            await _context.Species.AddAsync(cleanSpecies);
            try
            {
                await _context.SaveChangesAsync(); // Save immediately to persist and clear tracking
            }
            catch (ArgumentException ex) when (ex.Message.Contains("same key"))
            {
                // Log detailed error with sensitive data logging enabled
                _logger.LogError(
                    ex,
                    "Duplicate key error when saving species {SpeciesId} ({SpeciesName}). "
                        + "LevelMoves count: {LevelMoveCount}. First few LevelMoves: {LevelMoves}",
                    cleanSpecies.Id,
                    cleanSpecies.Name,
                    cleanSpecies.LevelMoves.Count,
                    string.Join(
                        ", ",
                        cleanSpecies.LevelMoves.Take(5).Select(lm => $"L{lm.Level}:{lm.MoveName}")
                    )
                );
                throw; // Re-throw to see full stack trace
            }
            _context.ChangeTracker.Clear(); // Clear tracker after each save to prevent any residual conflicts
        }

        _logger.LogDebug("Loaded {Count} species", seenIds.Count);
    }

    private static SpeciesData CreateCleanSpeciesCopy(SpeciesData source)
    {
        // Create a new instance with clean collections to avoid EF Core tracking conflicts
        // Deduplicate LevelMoves by key (Level, MoveName) - if duplicates exist, keep the first occurrence
        // Use Dictionary for O(1) lookup performance
        var levelMoveDict = new Dictionary<(int Level, string MoveName), LevelMove>();

        foreach (var lm in source.LevelMoves ?? Enumerable.Empty<LevelMove>())
        {
            if (lm == null)
                continue;

            var key = (lm.Level, lm.MoveName);
            // Only add if we haven't seen this key before
            if (!levelMoveDict.ContainsKey(key))
            {
                levelMoveDict[key] = new LevelMove { Level = lm.Level, MoveName = lm.MoveName };
            }
        }

        var deduplicatedLevelMoves = levelMoveDict.Values.ToList();

        var copy = new SpeciesData
        {
            Id = source.Id,
            NationalDexNumber = source.NationalDexNumber,
            Name = source.Name,
            Types = new List<string>(source.Types),
            Abilities = new List<string>(source.Abilities),
            EggGroups = new List<string>(source.EggGroups),
            BaseStats = source.BaseStats, // Value type, so safe to copy
            HiddenAbility = source.HiddenAbility,
            LevelMoves = deduplicatedLevelMoves,
            Evolutions = new List<Evolution>(
                source.Evolutions.Select(e => new Evolution
                {
                    TargetSpeciesId = e.TargetSpeciesId,
                    Method = e.Method,
                    RequiredLevel = e.RequiredLevel,
                    RequiredItem = e.RequiredItem,
                    Conditions = new Dictionary<string, string>(e.Conditions),
                })
            ),
            TmMoves = new List<string>(source.TmMoves),
            EggMoves = new List<string>(source.EggMoves),
            CatchRate = source.CatchRate,
            BaseFriendship = source.BaseFriendship,
            GenderRatio = source.GenderRatio,
            GrowthRate = source.GrowthRate,
            BaseExperience = source.BaseExperience,
            HatchSteps = source.HatchSteps,
            Height = source.Height,
            Weight = source.Weight,
            Description = source.Description,
        };
        return copy;
    }

    private async Task LoadSpeciesDataAsync()
    {
        // This method is kept for compatibility but data loading is now done in LoadAllDataAsync
        // This should not be called directly anymore
        var species = await LoadAllJsonFromDirectoryAsync<SpeciesData>("species");
        await ProcessSpeciesDataAsync(species);
    }

    private async Task ProcessMoveDataAsync(List<MoveData> moves)
    {
        // Use a dictionary to track which IDs we've seen
        var seenIds = new HashSet<string>();

        foreach (var m in moves)
        {
            if (seenIds.Contains(m.Id))
            {
                continue;
            }

            seenIds.Add(m.Id);
            await _context.Moves.AddAsync(m);
        }

        _logger.LogDebug("Loaded {Count} moves", moves.Count);
    }

    private async Task LoadMoveDataAsync()
    {
        var moves = await LoadAllJsonFromDirectoryAsync<MoveData>("moves");
        await ProcessMoveDataAsync(moves);
    }

    private async Task ProcessItemDataAsync(List<ItemData> items)
    {
        // Use a dictionary to track which IDs we've seen
        var seenIds = new HashSet<string>();

        foreach (var i in items)
        {
            if (seenIds.Contains(i.Id))
            {
                continue;
            }

            seenIds.Add(i.Id);
            await _context.Items.AddAsync(i);
        }

        _logger.LogDebug("Loaded {Count} items", items.Count);
    }

    private async Task LoadItemDataAsync()
    {
        var items = await LoadAllJsonFromDirectoryAsync<ItemData>("items");
        await ProcessItemDataAsync(items);
    }

    private async Task ProcessEncounterDataAsync(List<EncounterTable> encounters)
    {
        // Use a dictionary to track which IDs we've seen
        var seenIds = new HashSet<string>();

        foreach (var e in encounters)
        {
            if (seenIds.Contains(e.LocationId))
            {
                continue;
            }

            seenIds.Add(e.LocationId);
            await _context.Encounters.AddAsync(e);
        }

        _logger.LogDebug("Loaded {Count} encounter tables", encounters.Count);
    }

    private async Task LoadEncounterDataAsync()
    {
        var encounters = await LoadAllJsonFromDirectoryAsync<EncounterTable>("encounters");
        await ProcessEncounterDataAsync(encounters);
    }

    private async Task ProcessTypeDataAsync(List<TypeData> types)
    {
        // Use a dictionary to track which IDs we've seen
        var seenIds = new HashSet<string>();

        foreach (var t in types)
        {
            if (seenIds.Contains(t.Id))
            {
                continue;
            }

            seenIds.Add(t.Id);
            await _context.Types.AddAsync(t);
        }

        if (types.Count == 0)
        {
            _logger.LogWarning(
                "types directory not found or empty. Type effectiveness will be neutral (1.0) for all matchups. "
                    + "This will result in incorrect battle calculations!"
            );
        }

        _logger.LogDebug("Loaded {Count} types with matchups", types.Count);
    }

    private async Task LoadTypesAsync()
    {
        var types = await LoadAllJsonFromDirectoryAsync<TypeData>("types");
        await ProcessTypeDataAsync(types);
    }

    // ==================== Type Data Methods ====================

    /// <inheritdoc/>
    public async Task<TypeData?> GetTypeAsync(string typeId)
    {
        await EnsureDataLoadedAsync();

        return await _context.Types.AsNoTracking().FirstOrDefaultAsync(t => t.Id == typeId);
    }

    /// <inheritdoc/>
    public async Task<TypeData?> GetTypeByNameAsync(string name)
    {
        await EnsureDataLoadedAsync();

        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalizedName = name.Trim();
        return await _context
            .Types.AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)
            );
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TypeData>> GetAllTypesAsync()
    {
        await EnsureDataLoadedAsync();

        return await _context.Types.AsNoTracking().ToListAsync();
    }

    // ==================== Type Effectiveness Methods ====================

    /// <inheritdoc/>
    public async Task<double> GetTypeEffectivenessAsync(string attackType, string defenseType)
    {
        await EnsureDataLoadedAsync();

        if (string.IsNullOrWhiteSpace(attackType) || string.IsNullOrWhiteSpace(defenseType))
        {
            return 1.0; // Neutral if invalid input
        }

        var attackTypeName = attackType.Trim();
        var defenseTypeName = defenseType.Trim();

        var attackTypeInfo = await _context
            .Types.AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.Name.Equals(attackTypeName, StringComparison.OrdinalIgnoreCase)
            );

        if (attackTypeInfo == null)
        {
            _logger.LogWarning("Attack type '{AttackType}' not found in type data", attackType);
            return 1.0; // Neutral if type not found
        }

        // Try exact match first
        if (attackTypeInfo.Matchups.TryGetValue(defenseTypeName, out var effectiveness))
        {
            return effectiveness;
        }

        // Try case-insensitive match
        var matchup = attackTypeInfo.Matchups.FirstOrDefault(m =>
            m.Key.Equals(defenseTypeName, StringComparison.OrdinalIgnoreCase)
        );

        if (matchup.Key != null)
        {
            return matchup.Value;
        }

        return 1.0; // Neutral by default
    }

    /// <inheritdoc/>
    public async Task<double> GetDualTypeEffectivenessAsync(
        string attackType,
        string defenseType1,
        string? defenseType2
    )
    {
        await EnsureDataLoadedAsync();

        var effectiveness1 = await GetTypeEffectivenessAsync(attackType, defenseType1);

        if (string.IsNullOrEmpty(defenseType2))
            return effectiveness1;

        var effectiveness2 = await GetTypeEffectivenessAsync(attackType, defenseType2);
        return effectiveness1 * effectiveness2;
    }

    /// <summary>
    /// Loads all JSON files from a directory (including mod overrides).
    /// Each file can contain either a single object or an array of objects.
    /// </summary>
    private async Task<List<T>> LoadAllJsonFromDirectoryAsync<T>(string directoryName)
    {
        var allData = new List<T>();
        var loadedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Get all directories to search (mods first, then base)
        var searchPaths = new List<string>();
        foreach (var modPath in _modDataPaths)
        {
            var modDir = Path.Combine(modPath, directoryName);
            if (Directory.Exists(modDir))
            {
                searchPaths.Add(modDir);
            }
        }

        var baseDir = Path.Combine(_dataPath, directoryName);
        if (Directory.Exists(baseDir))
        {
            searchPaths.Add(baseDir);
        }

        if (searchPaths.Count == 0)
        {
            _logger.LogWarning("No directories found for: {DirectoryName}", directoryName);
            return allData;
        }

        // Load files from each path (mods can override base files by name)
        foreach (var searchPath in searchPaths)
        {
            var jsonFiles = Directory.GetFiles(searchPath, "*.json", SearchOption.TopDirectoryOnly);

            foreach (var filePath in jsonFiles)
            {
                var fileName = Path.GetFileName(filePath);

                // Skip if already loaded (mod override)
                if (loadedFiles.Contains(fileName))
                {
                    _logger.LogTrace("Skipping {FileName} (already loaded from mod)", fileName);
                    continue;
                }

                try
                {
                    _logger.LogTrace("Loading {FilePath}", filePath);
                    var json = await File.ReadAllTextAsync(filePath);

                    // Try to deserialize as array first
                    try
                    {
                        var arrayData = JsonSerializer.Deserialize<List<T>>(json, JsonOptions);
                        if (arrayData != null)
                        {
                            allData.AddRange(arrayData);
                            loadedFiles.Add(fileName);
                            _logger.LogTrace(
                                "Loaded {Count} items from {FileName}",
                                arrayData.Count,
                                fileName
                            );
                            continue;
                        }
                    }
                    catch
                    {
                        // Not an array, try single object
                    }

                    // Try to deserialize as single object
                    var singleData = JsonSerializer.Deserialize<T>(json, JsonOptions);
                    if (singleData != null)
                    {
                        allData.Add(singleData);
                        loadedFiles.Add(fileName);
                        _logger.LogTrace("Loaded 1 item from {FileName}", fileName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load {FilePath}", filePath);
                }
            }
        }

        _logger.LogDebug(
            "Loaded {Count} total items from {DirectoryName}",
            allData.Count,
            directoryName
        );
        return allData;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogInformation("Disposing DataManager");

        _loadLock.Dispose();
        // Note: We don't dispose _context here as it's typically managed by DI container

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
