# PokeNET Data Infrastructure Architecture Design

**Architect Agent**: Hive Mind Swarm 1761503054594-0amyzoky7
**Date**: 2025-10-26
**Status**: Architecture Design Complete
**Dependencies**: IDataApi + DataManager (✅ Implemented by Coder #2)

---

## Executive Summary

This document provides the complete architectural design for PokeNET's critical data infrastructure systems that were identified as missing in the research phase. The design builds upon the **already-implemented IDataApi and DataManager** and adds the three critical missing components:

1. **TypeChart System** - 18×18 type effectiveness matrix with dual-type support
2. **PokemonStats Consolidation** - Unified stats model eliminating duplication
3. **Enhanced JSON Loading** - Building on existing DataManager with optimization

### Design Principles

✅ **Domain-Driven Design** - Interfaces in Domain, implementations in Core
✅ **Dependency Injection** - All services registered and mockable
✅ **Mod Support** - JSON overrides through DataManager's existing mod system
✅ **Type Safety** - Strong typing throughout with proper enum usage
✅ **Testability** - Pure functions, mockable interfaces, no statics
✅ **Performance** - Lazy loading, caching, async operations

---

## 1. TypeChart System Architecture

### 1.1 Domain Interface Design

**Location**: `/PokeNET/PokeNET.Domain/Battle/ITypeChart.cs`

```csharp
namespace PokeNET.Domain.Battle;

/// <summary>
/// Interface for Pokemon type effectiveness calculations.
/// Provides type matchup data for damage calculation in battles.
/// Follows the Dependency Inversion Principle - domain defines the contract.
/// </summary>
public interface ITypeChart
{
    /// <summary>
    /// Calculates type effectiveness multiplier for a single type matchup.
    /// </summary>
    /// <param name="attackingType">Type of the attacking move.</param>
    /// <param name="defendingType">Type being attacked.</param>
    /// <returns>Effectiveness multiplier (0x, 0.25x, 0.5x, 1x, 2x, or 4x)</returns>
    float GetEffectiveness(PokemonType attackingType, PokemonType defendingType);

    /// <summary>
    /// Calculates type effectiveness for dual-type Pokemon.
    /// Multiplies effectiveness against both types.
    /// </summary>
    /// <param name="attackingType">Type of the attacking move.</param>
    /// <param name="defendingType1">Primary defending type.</param>
    /// <param name="defendingType2">Secondary defending type (null for single-type).</param>
    /// <returns>Combined effectiveness (0x to 4x)</returns>
    float GetDualTypeEffectiveness(
        PokemonType attackingType,
        PokemonType defendingType1,
        PokemonType? defendingType2);

    /// <summary>
    /// Gets all types that the attacking type is super effective against.
    /// Useful for AI decision-making and UI hints.
    /// </summary>
    /// <param name="attackingType">Type to check.</param>
    /// <returns>Array of types that take 2x or 4x damage.</returns>
    PokemonType[] GetSuperEffectiveAgainst(PokemonType attackingType);

    /// <summary>
    /// Gets all types that resist the attacking type.
    /// </summary>
    /// <param name="attackingType">Type to check.</param>
    /// <returns>Array of types that take 0.5x or 0.25x damage.</returns>
    PokemonType[] GetNotVeryEffectiveAgainst(PokemonType attackingType);

    /// <summary>
    /// Gets all types that are immune to the attacking type.
    /// </summary>
    /// <param name="attackingType">Type to check.</param>
    /// <returns>Array of types that take 0x damage.</returns>
    PokemonType[] GetImmuneTypes(PokemonType attackingType);

    /// <summary>
    /// Checks if type chart data is loaded and ready.
    /// </summary>
    bool IsLoaded { get; }
}
```

### 1.2 PokemonType Enum Design

**Location**: `/PokeNET/PokeNET.Domain/Battle/PokemonType.cs`

```csharp
namespace PokeNET.Domain.Battle;

/// <summary>
/// Pokemon type classifications.
/// Based on Generation VI+ type system (18 types with Fairy).
/// </summary>
public enum PokemonType
{
    Normal = 0,
    Fire = 1,
    Water = 2,
    Electric = 3,
    Grass = 4,
    Ice = 5,
    Fighting = 6,
    Poison = 7,
    Ground = 8,
    Flying = 9,
    Psychic = 10,
    Bug = 11,
    Rock = 12,
    Ghost = 13,
    Dragon = 14,
    Dark = 15,
    Steel = 16,
    Fairy = 17
}

/// <summary>
/// Extension methods for PokemonType enum.
/// </summary>
public static class PokemonTypeExtensions
{
    /// <summary>
    /// Converts type enum to display string.
    /// </summary>
    public static string ToDisplayString(this PokemonType type) => type.ToString();

    /// <summary>
    /// Parses a string to PokemonType enum.
    /// Case-insensitive.
    /// </summary>
    public static PokemonType Parse(string typeString)
    {
        if (Enum.TryParse<PokemonType>(typeString, ignoreCase: true, out var result))
            return result;

        throw new ArgumentException($"Invalid Pokemon type: {typeString}", nameof(typeString));
    }

    /// <summary>
    /// Tries to parse a string to PokemonType enum.
    /// </summary>
    public static bool TryParse(string typeString, out PokemonType result)
    {
        return Enum.TryParse(typeString, ignoreCase: true, out result);
    }

    /// <summary>
    /// Gets the UI color associated with this type.
    /// Used for type badges, move listings, etc.
    /// </summary>
    public static (byte R, byte G, byte B) GetTypeColor(this PokemonType type)
    {
        return type switch
        {
            PokemonType.Normal => (168, 168, 120),
            PokemonType.Fire => (240, 128, 48),
            PokemonType.Water => (104, 144, 240),
            PokemonType.Electric => (248, 208, 48),
            PokemonType.Grass => (120, 200, 80),
            PokemonType.Ice => (152, 216, 216),
            PokemonType.Fighting => (192, 48, 40),
            PokemonType.Poison => (160, 64, 160),
            PokemonType.Ground => (224, 192, 104),
            PokemonType.Flying => (168, 144, 240),
            PokemonType.Psychic => (248, 88, 136),
            PokemonType.Bug => (168, 184, 32),
            PokemonType.Rock => (184, 160, 56),
            PokemonType.Ghost => (112, 88, 152),
            PokemonType.Dragon => (112, 56, 248),
            PokemonType.Dark => (112, 88, 72),
            PokemonType.Steel => (184, 184, 208),
            PokemonType.Fairy => (238, 153, 172),
            _ => (128, 128, 128)
        };
    }
}
```

### 1.3 Core Implementation Design

**Location**: `/PokeNET/PokeNET.Core/Battle/TypeChart.cs`

```csharp
namespace PokeNET.Core.Battle;

using Microsoft.Extensions.Logging;
using PokeNET.Domain.Battle;
using System.Text.Json;

/// <summary>
/// Implements Pokemon type effectiveness system.
/// Loads type matchup data from JSON with mod override support.
/// Thread-safe with immutable effectiveness matrix.
/// </summary>
public class TypeChart : ITypeChart
{
    private readonly ILogger<TypeChart> _logger;
    private readonly float[,] _effectiveness;
    private readonly string _dataPath;
    private bool _isLoaded;

    // Official Pokemon type effectiveness matrix (Generation VI+)
    // Rows = Attacking type, Columns = Defending type
    // 0 = No effect (0x), 0.5 = Not very effective (0.5x),
    // 1 = Normal (1x), 2 = Super effective (2x)
    private static readonly float[,] DefaultEffectiveness = new float[18, 18]
    {
        //        Nor  Fir  Wat  Ele  Gra  Ice  Fig  Poi  Gro  Fly  Psy  Bug  Roc  Gho  Dra  Dar  Ste  Fai
        /* Nor */ { 1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f, 0.5f, 0f,  1f,  1f, 0.5f, 1f },
        /* Fir */ { 1f, 0.5f,0.5f, 1f,  2f,  2f,  1f,  1f,  1f,  1f,  1f,  2f, 0.5f, 1f, 0.5f, 1f,  2f,  1f },
        /* Wat */ { 1f,  2f, 0.5f, 1f, 0.5f, 1f,  1f,  1f,  2f,  1f,  1f,  1f,  2f,  1f, 0.5f, 1f,  1f,  1f },
        /* Ele */ { 1f,  1f,  2f, 0.5f,0.5f, 1f,  1f,  1f,  0f,  2f,  1f,  1f,  1f,  1f, 0.5f, 1f,  1f,  1f },
        /* Gra */ { 1f, 0.5f, 2f,  1f, 0.5f, 1f,  1f, 0.5f, 2f, 0.5f, 1f, 0.5f, 2f,  1f, 0.5f, 1f, 0.5f, 1f },
        /* Ice */ { 1f, 0.5f,0.5f, 1f,  2f, 0.5f, 1f,  1f,  2f,  2f,  1f,  1f,  1f,  1f,  2f,  1f, 0.5f, 1f },
        /* Fig */ { 2f,  1f,  1f,  1f,  1f,  2f,  1f, 0.5f, 1f, 0.5f,0.5f,0.5f, 2f,  0f,  1f,  2f,  2f, 0.5f},
        /* Poi */ { 1f,  1f,  1f,  1f,  2f,  1f,  1f, 0.5f,0.5f, 1f,  1f,  1f, 0.5f,0.5f, 1f,  1f,  0f,  2f },
        /* Gro */ { 1f,  2f,  1f,  2f, 0.5f, 1f,  1f,  2f,  1f,  0f,  1f, 0.5f, 2f,  1f,  1f,  1f,  2f,  1f },
        /* Fly */ { 1f,  1f,  1f, 0.5f, 2f,  1f,  2f,  1f,  1f,  1f,  1f,  2f, 0.5f, 1f,  1f,  1f, 0.5f, 1f },
        /* Psy */ { 1f,  1f,  1f,  1f,  1f,  1f,  2f,  2f,  1f,  1f, 0.5f, 1f,  1f,  1f,  1f,  0f, 0.5f, 1f },
        /* Bug */ { 1f, 0.5f, 1f,  1f,  2f,  1f, 0.5f,0.5f, 1f, 0.5f, 2f,  1f,  1f, 0.5f, 1f,  2f, 0.5f,0.5f},
        /* Roc */ { 1f,  2f,  1f,  1f,  1f,  2f, 0.5f, 1f, 0.5f, 2f,  1f,  2f,  1f,  1f,  1f,  1f, 0.5f, 1f },
        /* Gho */ { 0f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  2f,  1f,  1f,  2f,  1f, 0.5f, 1f,  1f },
        /* Dra */ { 1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  2f,  1f, 0.5f, 0f },
        /* Dar */ { 1f,  1f,  1f,  1f,  1f,  1f, 0.5f, 1f,  1f,  1f,  2f,  1f,  1f,  2f,  1f, 0.5f, 1f, 0.5f},
        /* Ste */ { 1f, 0.5f,0.5f,0.5f, 1f,  2f,  1f,  1f,  1f,  1f,  1f,  1f,  2f,  1f,  1f,  1f, 0.5f, 2f },
        /* Fai */ { 1f, 0.5f, 1f,  1f,  1f,  1f,  2f, 0.5f, 1f,  1f,  1f,  1f,  1f,  1f,  2f,  2f, 0.5f, 1f }
    };

    public bool IsLoaded => _isLoaded;

    public TypeChart(ILogger<TypeChart> logger, string dataPath)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dataPath = dataPath ?? throw new ArgumentNullException(nameof(dataPath));

        // Initialize with default effectiveness matrix
        _effectiveness = (float[,])DefaultEffectiveness.Clone();
        _isLoaded = false;

        _logger.LogInformation("TypeChart initialized with default Gen VI+ type matchups");
    }

    /// <summary>
    /// Loads type chart from JSON file with mod override support.
    /// Called automatically by DI container on startup.
    /// </summary>
    public async Task LoadTypeChartAsync(IEnumerable<string>? modPaths = null)
    {
        try
        {
            var typeChartPath = ResolveDataPath("type_chart.json", modPaths);

            if (typeChartPath != null)
            {
                _logger.LogInformation("Loading custom type chart from: {Path}", typeChartPath);

                var json = await File.ReadAllTextAsync(typeChartPath);
                var customChart = JsonSerializer.Deserialize<TypeChartData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });

                if (customChart != null)
                {
                    ApplyCustomChart(customChart);
                    _logger.LogInformation("Loaded custom type chart with {Count} overrides",
                        customChart.Overrides?.Count ?? 0);
                }
            }
            else
            {
                _logger.LogInformation("No custom type chart found, using default Gen VI+ matchups");
            }

            _isLoaded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load custom type chart, using defaults");
            _isLoaded = true; // Still mark as loaded with defaults
        }
    }

    public float GetEffectiveness(PokemonType attackingType, PokemonType defendingType)
    {
        if (!_isLoaded)
        {
            _logger.LogWarning("TypeChart accessed before loading, using defaults");
        }

        int attackIdx = (int)attackingType;
        int defendIdx = (int)defendingType;

        if (attackIdx < 0 || attackIdx >= 18 || defendIdx < 0 || defendIdx >= 18)
        {
            _logger.LogWarning("Invalid type indices: attacking={Attack}, defending={Defend}",
                attackingType, defendingType);
            return 1.0f; // Neutral effectiveness for invalid types
        }

        return _effectiveness[attackIdx, defendIdx];
    }

    public float GetDualTypeEffectiveness(
        PokemonType attackingType,
        PokemonType defendingType1,
        PokemonType? defendingType2)
    {
        float effectiveness = GetEffectiveness(attackingType, defendingType1);

        if (defendingType2.HasValue && defendingType2.Value != defendingType1)
        {
            effectiveness *= GetEffectiveness(attackingType, defendingType2.Value);
        }

        return effectiveness;
    }

    public PokemonType[] GetSuperEffectiveAgainst(PokemonType attackingType)
    {
        var results = new List<PokemonType>();
        int attackIdx = (int)attackingType;

        for (int i = 0; i < 18; i++)
        {
            if (_effectiveness[attackIdx, i] >= 2.0f)
            {
                results.Add((PokemonType)i);
            }
        }

        return results.ToArray();
    }

    public PokemonType[] GetNotVeryEffectiveAgainst(PokemonType attackingType)
    {
        var results = new List<PokemonType>();
        int attackIdx = (int)attackingType;

        for (int i = 0; i < 18; i++)
        {
            float eff = _effectiveness[attackIdx, i];
            if (eff > 0 && eff <= 0.5f)
            {
                results.Add((PokemonType)i);
            }
        }

        return results.ToArray();
    }

    public PokemonType[] GetImmuneTypes(PokemonType attackingType)
    {
        var results = new List<PokemonType>();
        int attackIdx = (int)attackingType;

        for (int i = 0; i < 18; i++)
        {
            if (_effectiveness[attackIdx, i] == 0f)
            {
                results.Add((PokemonType)i);
            }
        }

        return results.ToArray();
    }

    private void ApplyCustomChart(TypeChartData customChart)
    {
        if (customChart.Overrides == null) return;

        foreach (var (key, value) in customChart.Overrides)
        {
            // Key format: "Fire->Water" or "Fire->Water/Grass"
            var parts = key.Split("->", StringSplitOptions.TrimEntries);
            if (parts.Length != 2) continue;

            if (!PokemonTypeExtensions.TryParse(parts[0], out var attackType)) continue;

            var defendTypes = parts[1].Split('/', StringSplitOptions.TrimEntries);
            foreach (var defendTypeStr in defendTypes)
            {
                if (PokemonTypeExtensions.TryParse(defendTypeStr, out var defendType))
                {
                    _effectiveness[(int)attackType, (int)defendType] = value;
                    _logger.LogDebug("Applied override: {Attack}->{Defend} = {Value}x",
                        attackType, defendType, value);
                }
            }
        }
    }

    private string? ResolveDataPath(string fileName, IEnumerable<string>? modPaths)
    {
        // Check mod paths first
        if (modPaths != null)
        {
            foreach (var modPath in modPaths)
            {
                var fullPath = Path.Combine(modPath, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
        }

        // Check base data path
        var basePath = Path.Combine(_dataPath, fileName);
        return File.Exists(basePath) ? basePath : null;
    }

    // JSON schema for type chart customization
    private class TypeChartData
    {
        public Dictionary<string, float>? Overrides { get; set; }
    }
}
```

### 1.4 JSON Schema for Type Chart

**Location**: `/data/type_chart.json`

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "Pokemon Type Chart Overrides",
  "description": "Custom type effectiveness multipliers for modding",
  "overrides": {
    "Fire->Grass": 2.0,
    "Water->Fire": 2.0,
    "Fairy->Dragon": 2.0,
    "Ghost->Normal": 0.0,
    "Normal->Ghost": 0.0,
    "Ground->Flying": 0.0
  },
  "_comments": {
    "format": "AttackingType->DefendingType: multiplier",
    "multipliers": "0 = immune, 0.5 = not very effective, 1 = neutral, 2 = super effective",
    "dual_types": "Use Fire->Water/Grass for multiple targets",
    "note": "Only specify overrides - defaults are Gen VI+ standard"
  }
}
```

---

## 2. PokemonStats Consolidation

### 2.1 Current State Analysis

**Problem**: The research identified potential stats duplication:
- `PokemonStats` component exists in `/Domain/ECS/Components/PokemonStats.cs`
- Reviewer mentioned possible `Stats` component duplication

**Review of Existing PokemonStats.cs**:
```csharp
public struct PokemonStats
{
    public int HP { get; set; }
    public int MaxHP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int SpAttack { get; set; }
    public int SpDefense { get; set; }
    public int Speed { get; set; }

    // IVs (Individual Values)
    public int IV_HP { get; set; }
    // ... (all IV fields)

    // EVs (Effort Values)
    public int EV_HP { get; set; }
    // ... (all EV fields)

    // Calculation methods
    public int CalculateHP(int baseHP, int level) { ... }
    public int CalculateStat(int baseStat, int iv, int ev, int level, float natureModifier) { ... }
}
```

**Assessment**: ✅ **No consolidation needed**

The existing `PokemonStats` component is already well-designed:
- Contains all battle stats (HP, Attack, Defense, SpAttack, SpDefense, Speed)
- Includes IVs and EVs for stat calculation
- Has official Pokemon stat calculation formulas
- Is a `struct` for performance (value type)
- No duplicate `Stats` component found in codebase

**Recommendation**: Keep existing `PokemonStats` as-is. No changes required.

### 2.2 Integration with SpeciesData

The existing `SpeciesData.BaseStats` class properly separates concerns:

```csharp
// SpeciesData.cs - Static species base stats
public class BaseStats
{
    public int HP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int SpecialAttack { get; set; }
    public int SpecialDefense { get; set; }
    public int Speed { get; set; }
    public int Total => HP + Attack + Defense + SpecialAttack + SpecialDefense + Speed;
}

// PokemonStats.cs - Instance stats with IVs/EVs
public struct PokemonStats
{
    // Calculated stats using BaseStats + IVs + EVs + Level + Nature
    public int Attack { get; set; }
    // ...
}
```

**Design Pattern**: ✅ Correct separation
- `SpeciesData.BaseStats` = Static, immutable species data (from JSON)
- `PokemonStats` component = Dynamic, per-Pokemon instance data (ECS component)

---

## 3. Enhanced DataManager Architecture

### 3.1 Current Implementation Review

**Status**: ✅ **Already implemented by Coder #2**

The existing `DataManager` at `/PokeNET/PokeNET.Core/Data/DataManager.cs` is excellent:

✅ Implements `IDataApi` interface
✅ Thread-safe with `SemaphoreSlim`
✅ Mod override support via `SetModDataPaths()`
✅ Lazy loading with `EnsureDataLoadedAsync()`
✅ Caches data in dictionaries (by ID and by name)
✅ Async loading with `Task.WhenAll()` for parallel JSON parsing
✅ Proper disposal pattern

### 3.2 Recommended Enhancements

#### Enhancement 1: Add TypeChart Loading

**Location**: `/PokeNET/PokeNET.Core/Data/DataManager.cs`

```csharp
// Add to DataManager constructor
public DataManager(ILogger<DataManager> logger, string dataPath, ITypeChart typeChart)
{
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _dataPath = dataPath ?? throw new ArgumentNullException(nameof(dataPath));
    _typeChart = typeChart ?? throw new ArgumentNullException(nameof(typeChart));

    // ... existing code
}

// Add to LoadAllDataAsync()
private async Task LoadAllDataAsync()
{
    _logger.LogInformation("Loading game data from disk...");

    var tasks = new[]
    {
        LoadSpeciesDataAsync(),
        LoadMoveDataAsync(),
        LoadItemDataAsync(),
        LoadEncounterDataAsync(),
        LoadTypeChartAsync() // NEW
    };

    await Task.WhenAll(tasks);

    _isLoaded = true;
    // ... existing logging
}

// New method
private async Task LoadTypeChartAsync()
{
    if (_typeChart is TypeChart typeChartImpl)
    {
        await typeChartImpl.LoadTypeChartAsync(_modDataPaths);
        _logger.LogDebug("TypeChart loaded");
    }
}
```

#### Enhancement 2: Batch Loading Optimization

```csharp
/// <summary>
/// Preloads commonly accessed data into cache.
/// Call during game startup to reduce first-access latency.
/// </summary>
public async Task PreloadCommonDataAsync()
{
    await EnsureDataLoadedAsync();

    // Preload starter Pokemon (1, 4, 7 = Bulbasaur, Charmander, Squirtle)
    _ = await Task.WhenAll(
        GetSpeciesAsync(1),
        GetSpeciesAsync(4),
        GetSpeciesAsync(7)
    );

    // Preload common moves
    _ = await Task.WhenAll(
        GetMoveAsync("Tackle"),
        GetMoveAsync("Scratch"),
        GetMoveAsync("Growl")
    );

    _logger.LogInformation("Common data preloaded");
}
```

#### Enhancement 3: Metrics and Diagnostics

```csharp
/// <summary>
/// Gets data loading metrics for diagnostics.
/// </summary>
public DataMetrics GetMetrics()
{
    return new DataMetrics
    {
        SpeciesCount = _speciesById.Count,
        MoveCount = _movesByName.Count,
        ItemCount = _itemsById.Count,
        EncounterTableCount = _encountersById.Count,
        IsLoaded = _isLoaded,
        ModPathsCount = _modDataPaths.Count,
        MemoryEstimateKB = EstimateMemoryUsage()
    };
}

private long EstimateMemoryUsage()
{
    // Rough estimate: avg 500 bytes per species, 200 per move, etc.
    return (_speciesById.Count * 500 +
            _movesByName.Count * 200 +
            _itemsById.Count * 150 +
            _encountersById.Count * 300) / 1024;
}

public class DataMetrics
{
    public int SpeciesCount { get; set; }
    public int MoveCount { get; set; }
    public int ItemCount { get; set; }
    public int EncounterTableCount { get; set; }
    public bool IsLoaded { get; set; }
    public int ModPathsCount { get; set; }
    public long MemoryEstimateKB { get; set; }
}
```

---

## 4. Integration with Battle System

### 4.1 BattleSystem Dependency Injection

**Location**: `/PokeNET/PokeNET.Domain/ECS/Systems/BattleSystem.cs`

```csharp
public partial class BattleSystem : BaseSystem<World, float>
{
    private readonly IDataApi _dataApi;
    private readonly ITypeChart _typeChart;
    private readonly ILogger<BattleSystem> _logger;

    public BattleSystem(IDataApi dataApi, ITypeChart typeChart, ILogger<BattleSystem> logger)
    {
        _dataApi = dataApi ?? throw new ArgumentNullException(nameof(dataApi));
        _typeChart = typeChart ?? throw new ArgumentNullException(nameof(typeChart));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ... existing code
}
```

### 4.2 Complete Damage Calculation

```csharp
/// <summary>
/// Calculates damage using the official Pokemon damage formula.
/// Incorporates type effectiveness, STAB, critical hits, and random variance.
/// </summary>
private async Task<int> CalculateDamageAsync(
    Entity attacker,
    Entity defender,
    string moveName)
{
    // Get Pokemon data
    ref var attackerData = ref World.Get<PokemonData>(attacker);
    ref var attackerStats = ref World.Get<PokemonStats>(attacker);
    ref var defenderData = ref World.Get<PokemonData>(defender);
    ref var defenderStats = ref World.Get<PokemonStats>(defender);

    // Load move data
    var move = await _dataApi.GetMoveAsync(moveName);
    if (move == null)
    {
        _logger.LogWarning("Move not found: {MoveName}", moveName);
        return 0;
    }

    // Status moves do no damage
    if (move.Category == MoveCategory.Status)
        return 0;

    // Load species data for types
    var attackerSpecies = await _dataApi.GetSpeciesAsync(attackerData.SpeciesId);
    var defenderSpecies = await _dataApi.GetSpeciesAsync(defenderData.SpeciesId);

    if (attackerSpecies == null || defenderSpecies == null)
    {
        _logger.LogError("Species data not found for attacker or defender");
        return 0;
    }

    // Select attack/defense stats based on move category
    int attack = move.Category == MoveCategory.Physical
        ? attackerStats.Attack
        : attackerStats.SpAttack;

    int defense = move.Category == MoveCategory.Physical
        ? defenderStats.Defense
        : defenderStats.SpDefense;

    // Base damage formula (Generation III+)
    // Damage = (((2 * Level / 5 + 2) * Power * Attack / Defense) / 50 + 2) * Modifiers
    int baseDamage = (((2 * attackerData.Level / 5 + 2) * move.Power * attack / defense) / 50) + 2;

    // Modifier 1: STAB (Same Type Attack Bonus) - 1.5x if move matches Pokemon type
    float stab = 1.0f;
    var attackerTypes = ParseTypes(attackerSpecies.Types);
    if (PokemonTypeExtensions.TryParse(move.Type, out var moveType))
    {
        if (attackerTypes.Contains(moveType))
        {
            stab = 1.5f;
            _logger.LogDebug("STAB bonus applied for {Type} move", moveType);
        }
    }

    // Modifier 2: Type effectiveness
    var defenderTypes = ParseTypes(defenderSpecies.Types);
    float effectiveness = _typeChart.GetDualTypeEffectiveness(
        moveType,
        defenderTypes[0],
        defenderTypes.Length > 1 ? defenderTypes[1] : null);

    if (effectiveness == 0)
    {
        _logger.LogInformation("{DefenderSpecies} is immune to {MoveType} moves!",
            defenderSpecies.Name, moveType);
        return 0;
    }

    // Modifier 3: Critical hit (1/24 chance in Gen VI+, 1.5x damage)
    float critical = 1.0f;
    if (Random.Shared.Next(24) == 0)
    {
        critical = 1.5f;
        _logger.LogInformation("Critical hit!");
    }

    // Modifier 4: Random variance (85% - 100%)
    float random = 0.85f + (Random.Shared.NextSingle() * 0.15f);

    // Calculate final damage
    int damage = (int)(baseDamage * stab * effectiveness * critical * random);

    // Minimum 1 damage if move hits
    damage = Math.Max(1, damage);

    // Log effectiveness message
    LogEffectivenessMessage(effectiveness);

    return damage;
}

private void LogEffectivenessMessage(float effectiveness)
{
    if (effectiveness > 2.0f)
        _logger.LogInformation("It's super effective! (4x)");
    else if (effectiveness >= 2.0f)
        _logger.LogInformation("It's super effective!");
    else if (effectiveness < 0.5f && effectiveness > 0)
        _logger.LogInformation("It's not very effective...");
    else if (effectiveness <= 0.25f && effectiveness > 0)
        _logger.LogInformation("It's not very effective... (0.25x)");
}

private PokemonType[] ParseTypes(List<string> typeStrings)
{
    return typeStrings
        .Select(t => PokemonTypeExtensions.TryParse(t, out var type) ? type : (PokemonType?)null)
        .Where(t => t.HasValue)
        .Select(t => t!.Value)
        .ToArray();
}
```

---

## 5. Dependency Injection Configuration

### 5.1 Complete DI Registration

**Location**: `/PokeNET/PokeNET.DesktopGL/Program.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using PokeNET.Domain.Data;
using PokeNET.Domain.Battle;
using PokeNET.Core.Data;
using PokeNET.Core.Battle;

// ... in ConfigureServices method

// ==================== Data Infrastructure ====================

// Core ECS World
services.AddSingleton<World>(_ => World.Create());

// Data API (already implemented)
services.AddSingleton<IDataApi>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<DataManager>>();
    var dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
    return new DataManager(logger, dataPath);
});

// Type Chart System (NEW)
services.AddSingleton<ITypeChart>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<TypeChart>>();
    var dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
    var typeChart = new TypeChart(logger, dataPath);

    // Load type chart on startup
    typeChart.LoadTypeChartAsync().GetAwaiter().GetResult();

    return typeChart;
});

// ==================== ECS Systems ====================

// Battle System (with dependencies)
services.AddSingleton(sp =>
{
    var dataApi = sp.GetRequiredService<IDataApi>();
    var typeChart = sp.GetRequiredService<ITypeChart>();
    var logger = sp.GetRequiredService<ILogger<BattleSystem>>();
    return new BattleSystem(dataApi, typeChart, logger);
});

// Movement System
services.AddSingleton<MovementSystem>();

// Input System
services.AddSingleton<InputSystem>();

// Party Management System
services.AddSingleton<PartyManagementSystem>();

// Render System (NOTE: Should be moved to Core layer - see Architecture Analysis)
services.AddSingleton(sp =>
{
    var graphicsDevice = sp.GetRequiredService<GraphicsDevice>();
    var logger = sp.GetRequiredService<ILogger<RenderSystem>>();
    return new RenderSystem(graphicsDevice, logger);
});

// ==================== Game Initialization ====================

// Preload common data
var dataApi = serviceProvider.GetRequiredService<IDataApi>();
if (dataApi is DataManager dataManager)
{
    await dataManager.PreloadCommonDataAsync();
}

logger.LogInformation("Data infrastructure initialized successfully");
```

### 5.2 DI Container Validation

```csharp
/// <summary>
/// Validates that all required services are registered.
/// Call during application startup.
/// </summary>
public static void ValidateServices(IServiceProvider services)
{
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        // Validate data services
        _ = services.GetRequiredService<IDataApi>();
        _ = services.GetRequiredService<ITypeChart>();

        // Validate systems
        _ = services.GetRequiredService<BattleSystem>();
        _ = services.GetRequiredService<MovementSystem>();

        // Validate data is loaded
        var dataApi = services.GetRequiredService<IDataApi>();
        if (!dataApi.IsDataLoaded())
        {
            throw new InvalidOperationException("Data not loaded!");
        }

        var typeChart = services.GetRequiredService<ITypeChart>();
        if (!typeChart.IsLoaded)
        {
            throw new InvalidOperationException("TypeChart not loaded!");
        }

        logger.LogInformation("✅ All services validated successfully");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "❌ Service validation failed!");
        throw;
    }
}
```

---

## 6. Class Diagrams

### 6.1 Type Chart System Architecture

```
┌──────────────────────────────────────────┐
│         PokeNET.Domain (Interfaces)      │
├──────────────────────────────────────────┤
│                                          │
│  ┌────────────────────────────────────┐ │
│  │      <<enum>> PokemonType          │ │
│  ├────────────────────────────────────┤ │
│  │  Normal, Fire, Water, Electric...  │ │
│  │  (18 types total)                  │ │
│  └────────────────────────────────────┘ │
│                                          │
│  ┌────────────────────────────────────┐ │
│  │    <<interface>> ITypeChart        │ │
│  ├────────────────────────────────────┤ │
│  │  + GetEffectiveness()              │ │
│  │  + GetDualTypeEffectiveness()      │ │
│  │  + GetSuperEffectiveAgainst()      │ │
│  │  + GetNotVeryEffectiveAgainst()    │ │
│  │  + GetImmuneTypes()                │ │
│  │  + IsLoaded: bool                  │ │
│  └────────────────────────────────────┘ │
│                    ▲                     │
└────────────────────┼─────────────────────┘
                     │ implements
┌────────────────────┼─────────────────────┐
│         PokeNET.Core (Implementation)    │
├────────────────────┼─────────────────────┤
│                    │                     │
│  ┌─────────────────┴──────────────────┐ │
│  │         TypeChart                  │ │
│  ├────────────────────────────────────┤ │
│  │  - _effectiveness: float[18,18]    │ │
│  │  - _logger: ILogger                │ │
│  │  - _dataPath: string               │ │
│  │  - _isLoaded: bool                 │ │
│  ├────────────────────────────────────┤ │
│  │  + LoadTypeChartAsync()            │ │
│  │  + GetEffectiveness()              │ │
│  │  + GetDualTypeEffectiveness()      │ │
│  │  - ApplyCustomChart()              │ │
│  │  - ResolveDataPath()               │ │
│  └────────────────────────────────────┘ │
│                                          │
└──────────────────────────────────────────┘
```

### 6.2 Data Loading Architecture

```
┌────────────────────────────────────────────────────┐
│            PokeNET.Domain (Contracts)              │
├────────────────────────────────────────────────────┤
│                                                    │
│  ┌──────────────────────────────────────────────┐ │
│  │        <<interface>> IDataApi                │ │
│  ├──────────────────────────────────────────────┤ │
│  │  + GetSpeciesAsync(id): SpeciesData?         │ │
│  │  + GetSpeciesByNameAsync(name): SpeciesData? │ │
│  │  + GetAllSpeciesAsync(): IReadOnlyList       │ │
│  │  + GetMoveAsync(name): MoveData?             │ │
│  │  + GetAllMovesAsync(): IReadOnlyList         │ │
│  │  + GetItemAsync(id): ItemData?               │ │
│  │  + GetEncountersAsync(id): EncounterTable?   │ │
│  │  + ReloadDataAsync()                         │ │
│  │  + IsDataLoaded(): bool                      │ │
│  └──────────────────────────────────────────────┘ │
│                                                    │
│  ┌──────────────┐  ┌──────────────┐              │
│  │ SpeciesData  │  │   MoveData   │              │
│  ├──────────────┤  ├──────────────┤              │
│  │ + Id         │  │ + Name       │              │
│  │ + Name       │  │ + Type       │              │
│  │ + Types[]    │  │ + Category   │              │
│  │ + BaseStats  │  │ + Power      │              │
│  │ + Abilities[]│  │ + Accuracy   │              │
│  │ + Evolutions │  │ + PP         │              │
│  └──────────────┘  └──────────────┘              │
│                                                    │
│  ┌──────────────┐  ┌──────────────────┐          │
│  │   ItemData   │  │  EncounterTable  │          │
│  ├──────────────┤  ├──────────────────┤          │
│  │ + Id         │  │ + LocationId     │          │
│  │ + Name       │  │ + GrassEnc[]     │          │
│  │ + Category   │  │ + WaterEnc[]     │          │
│  │ + BuyPrice   │  │ + FishingEnc[]   │          │
│  │ + Effect     │  │ + SpecialEnc[]   │          │
│  └──────────────┘  └──────────────────┘          │
│                                                    │
└──────────────────────▲─────────────────────────────┘
                       │ implements
┌──────────────────────┼─────────────────────────────┐
│         PokeNET.Core (Implementation)              │
├──────────────────────┼─────────────────────────────┤
│                      │                             │
│  ┌───────────────────┴──────────────────────────┐ │
│  │              DataManager                     │ │
│  ├──────────────────────────────────────────────┤ │
│  │  - _speciesById: Dictionary<int, Species>    │ │
│  │  - _speciesByName: Dictionary<str, Species>  │ │
│  │  - _movesByName: Dictionary<str, Move>       │ │
│  │  - _itemsById: Dictionary<int, Item>         │ │
│  │  - _encountersById: Dictionary<str, Enc>     │ │
│  │  - _modDataPaths: List<string>               │ │
│  │  - _loadLock: SemaphoreSlim                  │ │
│  │  - _isLoaded: bool                           │ │
│  ├──────────────────────────────────────────────┤ │
│  │  + GetSpeciesAsync()                         │ │
│  │  + GetMoveAsync()                            │ │
│  │  + SetModDataPaths()                         │ │
│  │  + ReloadDataAsync()                         │ │
│  │  - EnsureDataLoadedAsync()                   │ │
│  │  - LoadAllDataAsync()                        │ │
│  │  - LoadSpeciesDataAsync()                    │ │
│  │  - LoadMoveDataAsync()                       │ │
│  │  - LoadItemDataAsync()                       │ │
│  │  - LoadEncounterDataAsync()                  │ │
│  │  - LoadJsonArrayAsync<T>()                   │ │
│  │  - ResolveDataPath()                         │ │
│  └──────────────────────────────────────────────┘ │
│                                                    │
└────────────────────────────────────────────────────┘
```

### 6.3 Battle System Integration

```
┌─────────────────────────────────────────────────────────┐
│              BattleSystem (ECS System)                  │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Dependencies (Injected):                               │
│  ┌────────────┐  ┌─────────────┐  ┌──────────────────┐ │
│  │  IDataApi  │  │ ITypeChart  │  │ ILogger<Battle>  │ │
│  └──────┬─────┘  └──────┬──────┘  └────────┬─────────┘ │
│         │               │                  │           │
│         ▼               ▼                  ▼           │
│  ┌──────────────────────────────────────────────────┐  │
│  │           ExecuteMoveAsync(Entity, Move)         │  │
│  ├──────────────────────────────────────────────────┤  │
│  │  1. Get PokemonData components                   │  │
│  │  2. Load MoveData from IDataApi                  │  │
│  │  3. Load SpeciesData from IDataApi               │  │
│  │  4. Calculate base damage                        │  │
│  │  5. Apply STAB (Same Type Attack Bonus)          │  │
│  │  6. Get type effectiveness from ITypeChart       │  │
│  │  7. Apply critical hit and random variance       │  │
│  │  8. Update defender HP                           │  │
│  │  9. Log battle messages                          │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  ECS Components Used:                                   │
│  ┌─────────────┐  ┌────────────────┐  ┌─────────────┐ │
│  │ PokemonData │  │  PokemonStats  │  │ BattleState │ │
│  ├─────────────┤  ├────────────────┤  ├─────────────┤ │
│  │ + SpeciesId │  │ + HP, Attack   │  │ + Status    │ │
│  │ + Level     │  │ + Defense      │  │ + Stages[]  │ │
│  │ + Nature    │  │ + IVs, EVs     │  │ + TurnCount │ │
│  └─────────────┘  └────────────────┘  └─────────────┘ │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 7. Data Flow Diagrams

### 7.1 Application Startup - Data Loading Flow

```
┌─────────────────────────────────────────────────────────────┐
│                   Application Startup                       │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│              DI Container Configuration                      │
│  (Program.cs - ConfigureServices)                            │
├──────────────────────────────────────────────────────────────┤
│  1. Register IDataApi -> DataManager                         │
│  2. Register ITypeChart -> TypeChart                         │
│  3. Register BattleSystem (with dependencies)                │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│                DataManager Creation                          │
├──────────────────────────────────────────────────────────────┤
│  - Sets _dataPath = "data/"                                  │
│  - Initializes empty caches                                  │
│  - _isLoaded = false                                         │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│              TypeChart.LoadTypeChartAsync()                  │
├──────────────────────────────────────────────────────────────┤
│  1. Check for custom type_chart.json                         │
│  2. If exists: Load and apply overrides                      │
│  3. If not: Use default Gen VI+ matrix                       │
│  4. _isLoaded = true                                         │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│         DataManager.PreloadCommonDataAsync()                 │
│         (Optional - called after DI setup)                   │
├──────────────────────────────────────────────────────────────┤
│  Triggers first data load via EnsureDataLoadedAsync():       │
│  1. Acquire _loadLock semaphore                              │
│  2. Call LoadAllDataAsync()                                  │
│     ├─> Task.WhenAll() parallel loading:                    │
│     │   ├─> LoadSpeciesDataAsync()                          │
│     │   │   ├─> ResolveDataPath("species.json")            │
│     │   │   ├─> Check mod paths first                       │
│     │   │   ├─> Deserialize List<SpeciesData>              │
│     │   │   └─> Build _speciesById & _speciesByName caches │
│     │   │                                                    │
│     │   ├─> LoadMoveDataAsync()                            │
│     │   │   ├─> ResolveDataPath("moves.json")              │
│     │   │   └─> Build _movesByName cache                   │
│     │   │                                                    │
│     │   ├─> LoadItemDataAsync()                            │
│     │   │   ├─> ResolveDataPath("items.json")              │
│     │   │   └─> Build _itemsById & _itemsByName caches     │
│     │   │                                                    │
│     │   └─> LoadEncounterDataAsync()                       │
│     │       ├─> ResolveDataPath("encounters.json")         │
│     │       └─> Build _encountersById cache                │
│     │                                                        │
│  3. _isLoaded = true                                         │
│  4. Release _loadLock                                        │
│  5. Log: "Data loaded: X species, Y moves, Z items..."      │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│             Game Ready - Systems Initialized                 │
│  All data cached in memory, ready for instant access         │
└──────────────────────────────────────────────────────────────┘
```

### 7.2 Battle Damage Calculation Flow

```
┌─────────────────────────────────────────────────────────────┐
│         Player Selects Move in Battle UI                    │
│         (e.g., "Charmander uses Ember!")                     │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│      BattleSystem.ExecuteMoveAsync(attacker, defender,       │
│                                     "Ember")                 │
├──────────────────────────────────────────────────────────────┤
│  Step 1: Get ECS Component Data                              │
│  ├─> attackerData = World.Get<PokemonData>(attacker)        │
│  │    └─> SpeciesId = 4, Level = 5                          │
│  ├─> attackerStats = World.Get<PokemonStats>(attacker)      │
│  │    └─> SpAttack = 18                                     │
│  ├─> defenderData = World.Get<PokemonData>(defender)        │
│  │    └─> SpeciesId = 1                                     │
│  └─> defenderStats = World.Get<PokemonStats>(defender)      │
│       └─> SpDefense = 20, HP = 25/25                        │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  Step 2: Load Move Data from IDataApi                        │
│  var move = await _dataApi.GetMoveAsync("Ember");            │
├──────────────────────────────────────────────────────────────┤
│  DataManager.GetMoveAsync("Ember"):                          │
│  ├─> await EnsureDataLoadedAsync() [already loaded, skip]   │
│  ├─> Normalize name: "ember"                                │
│  ├─> Lookup _movesByName["ember"]                           │
│  └─> Return MoveData:                                        │
│       - Name: "Ember"                                        │
│       - Type: "Fire"                                         │
│       - Category: Special                                    │
│       - Power: 40                                            │
│       - Accuracy: 100                                        │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  Step 3: Load Species Data from IDataApi                     │
│  var attackerSpecies = await _dataApi.GetSpeciesAsync(4);    │
│  var defenderSpecies = await _dataApi.GetSpeciesAsync(1);    │
├──────────────────────────────────────────────────────────────┤
│  DataManager returns SpeciesData:                            │
│  ├─> Charmander: Types = ["Fire"]                           │
│  └─> Bulbasaur: Types = ["Grass", "Poison"]                 │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  Step 4: Calculate Base Damage                               │
│  baseDamage = (((2*Level/5 + 2) * Power * SpAttack /        │
│                 SpDefense) / 50) + 2                         │
├──────────────────────────────────────────────────────────────┤
│  baseDamage = (((2*5/5 + 2) * 40 * 18 / 20) / 50) + 2       │
│             = (((2 + 2) * 40 * 18 / 20) / 50) + 2           │
│             = ((4 * 40 * 18 / 20) / 50) + 2                 │
│             = ((2880 / 20) / 50) + 2                         │
│             = (144 / 50) + 2                                 │
│             = 2.88 + 2                                       │
│             = 4 (truncated)                                  │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  Step 5: Apply STAB (Same Type Attack Bonus)                 │
│  moveType = Fire, attackerTypes = [Fire]                     │
│  stab = 1.5 (move matches Pokemon type)                      │
│  damage = 4 * 1.5 = 6                                        │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  Step 6: Get Type Effectiveness from ITypeChart              │
│  var eff = _typeChart.GetDualTypeEffectiveness(              │
│      Fire, Grass, Poison);                                   │
├──────────────────────────────────────────────────────────────┤
│  TypeChart.GetDualTypeEffectiveness():                       │
│  ├─> eff1 = _effectiveness[Fire][Grass] = 2.0x              │
│  ├─> eff2 = _effectiveness[Fire][Poison] = 1.0x             │
│  └─> Combined = 2.0 * 1.0 = 2.0x                            │
│  damage = 6 * 2.0 = 12                                       │
│  Log: "It's super effective!"                                │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  Step 7: Apply Critical Hit & Random Variance                │
│  critical = Random.Next(24) == 0 ? 1.5 : 1.0                │
│  random = 0.85 + Random.NextSingle() * 0.15 (0.85-1.0)      │
├──────────────────────────────────────────────────────────────┤
│  Assume: critical = 1.0 (no crit), random = 0.92            │
│  damage = 12 * 1.0 * 0.92 = 11.04 = 11 (truncated)          │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  Step 8: Update Defender HP                                  │
│  defenderStats.HP = 25 - 11 = 14                             │
│  World.Set(defender, defenderStats)                          │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  Step 9: Log Battle Messages                                 │
│  "Charmander used Ember!"                                    │
│  "It's super effective!"                                     │
│  "Bulbasaur took 11 damage!"                                 │
│  "Bulbasaur: 14/25 HP"                                       │
└──────────────────────────────────────────────────────────────┘
```

---

## 8. JSON Schema Examples

### 8.1 Species Data Schema

**Location**: `/data/species.json`

```json
[
  {
    "id": 1,
    "name": "Bulbasaur",
    "types": ["Grass", "Poison"],
    "baseStats": {
      "hp": 45,
      "attack": 49,
      "defense": 49,
      "specialAttack": 65,
      "specialDefense": 65,
      "speed": 45
    },
    "abilities": ["Overgrow"],
    "hiddenAbility": "Chlorophyll",
    "growthRate": "Medium Slow",
    "baseExperience": 64,
    "genderRatio": 31,
    "catchRate": 45,
    "baseFriendship": 70,
    "eggGroups": ["Monster", "Grass"],
    "hatchSteps": 5120,
    "height": 7,
    "weight": 69,
    "description": "A strange seed was planted on its back at birth. The plant sprouts and grows with this Pokémon.",
    "levelMoves": [
      { "level": 1, "moveName": "Tackle" },
      { "level": 1, "moveName": "Growl" },
      { "level": 3, "moveName": "Vine Whip" },
      { "level": 6, "moveName": "Poison Powder" },
      { "level": 9, "moveName": "Sleep Powder" },
      { "level": 12, "moveName": "Razor Leaf" }
    ],
    "tmMoves": ["Swords Dance", "Cut", "Toxic", "Body Slam"],
    "eggMoves": ["Skull Bash", "Petal Dance", "Magical Leaf"],
    "evolutions": [
      {
        "targetSpeciesId": 2,
        "method": "Level",
        "requiredLevel": 16
      }
    ]
  },
  {
    "id": 4,
    "name": "Charmander",
    "types": ["Fire"],
    "baseStats": {
      "hp": 39,
      "attack": 52,
      "defense": 43,
      "specialAttack": 60,
      "specialDefense": 50,
      "speed": 65
    },
    "abilities": ["Blaze"],
    "hiddenAbility": "Solar Power",
    "growthRate": "Medium Slow",
    "baseExperience": 62,
    "genderRatio": 31,
    "catchRate": 45,
    "baseFriendship": 70,
    "eggGroups": ["Monster", "Dragon"],
    "hatchSteps": 5120,
    "height": 6,
    "weight": 85,
    "description": "Obviously prefers hot places. When it rains, steam is said to spout from the tip of its tail.",
    "levelMoves": [
      { "level": 1, "moveName": "Scratch" },
      { "level": 1, "moveName": "Growl" },
      { "level": 4, "moveName": "Ember" },
      { "level": 8, "moveName": "Smokescreen" }
    ],
    "tmMoves": ["Dragon Claw", "Mega Punch", "Fire Punch"],
    "eggMoves": ["Belly Drum", "Ancient Power", "Dragon Pulse"],
    "evolutions": [
      {
        "targetSpeciesId": 5,
        "method": "Level",
        "requiredLevel": 16
      }
    ]
  }
]
```

### 8.2 Move Data Schema

**Location**: `/data/moves.json`

```json
[
  {
    "name": "Tackle",
    "type": "Normal",
    "category": "Physical",
    "power": 40,
    "accuracy": 100,
    "pp": 35,
    "priority": 0,
    "target": "SingleTarget",
    "effectChance": 0,
    "description": "A physical attack in which the user charges and slams into the target with its whole body.",
    "flags": ["Contact", "Protect", "Mirror"]
  },
  {
    "name": "Ember",
    "type": "Fire",
    "category": "Special",
    "power": 40,
    "accuracy": 100,
    "pp": 25,
    "priority": 0,
    "target": "SingleTarget",
    "effectChance": 10,
    "description": "The target is attacked with small flames. This may also leave the target with a burn.",
    "flags": ["Protect", "Mirror"],
    "effect": {
      "effectType": "StatusCondition",
      "effectTarget": "Target",
      "statusCondition": "Burn"
    }
  },
  {
    "name": "Vine Whip",
    "type": "Grass",
    "category": "Physical",
    "power": 45,
    "accuracy": 100,
    "pp": 25,
    "priority": 0,
    "target": "SingleTarget",
    "effectChance": 0,
    "description": "The target is struck with slender, whiplike vines to inflict damage.",
    "flags": ["Contact", "Protect", "Mirror"]
  },
  {
    "name": "Swords Dance",
    "type": "Normal",
    "category": "Status",
    "power": 0,
    "accuracy": 0,
    "pp": 20,
    "priority": 0,
    "target": "User",
    "effectChance": 100,
    "description": "A frenetic dance to uplift the fighting spirit. This sharply raises the user's Attack stat.",
    "flags": ["Snatch", "Dance"],
    "effect": {
      "effectType": "StatChange",
      "effectTarget": "Self",
      "statChanges": {
        "Attack": 2
      }
    }
  }
]
```

### 8.3 Encounter Table Schema

**Location**: `/data/encounters.json`

```json
[
  {
    "locationId": "route_1",
    "locationName": "Route 1",
    "grassEncounters": [
      {
        "speciesId": 16,
        "minLevel": 2,
        "maxLevel": 5,
        "rate": 50,
        "timeOfDay": "Any",
        "weather": "Any"
      },
      {
        "speciesId": 19,
        "minLevel": 2,
        "maxLevel": 4,
        "rate": 50,
        "timeOfDay": "Any",
        "weather": "Any"
      }
    ],
    "waterEncounters": [],
    "oldRodEncounters": [],
    "goodRodEncounters": [],
    "superRodEncounters": [],
    "caveEncounters": [],
    "specialEncounters": []
  },
  {
    "locationId": "viridian_forest",
    "locationName": "Viridian Forest",
    "grassEncounters": [
      {
        "speciesId": 10,
        "minLevel": 3,
        "maxLevel": 5,
        "rate": 40,
        "timeOfDay": "Any",
        "weather": "Any"
      },
      {
        "speciesId": 11,
        "minLevel": 4,
        "maxLevel": 6,
        "rate": 5,
        "timeOfDay": "Any",
        "weather": "Any"
      },
      {
        "speciesId": 16,
        "minLevel": 3,
        "maxLevel": 5,
        "rate": 55,
        "timeOfDay": "Any",
        "weather": "Any"
      }
    ],
    "specialEncounters": [
      {
        "encounterId": "pikachu_special",
        "speciesId": 25,
        "level": 3,
        "oneTime": false,
        "conditions": {
          "probability": "5%"
        }
      }
    ]
  }
]
```

---

## 9. DI Registration Patterns

### 9.1 Service Lifetimes

```csharp
// SINGLETON - Shared across entire application
// Use for: Data managers, type charts, stateless services
services.AddSingleton<IDataApi, DataManager>();
services.AddSingleton<ITypeChart, TypeChart>();

// SCOPED - New instance per request/scope
// Use for: Battle contexts, save game sessions
services.AddScoped<IBattleContext, BattleContext>();

// TRANSIENT - New instance every time
// Use for: Factories, lightweight services
services.AddTransient<IPokemonFactory, PokemonFactory>();
```

### 9.2 Factory Pattern for Pokemon Creation

```csharp
public interface IPokemonFactory
{
    Entity CreateWildPokemon(int speciesId, int level);
    Entity CreateStarterPokemon(int speciesId);
}

public class PokemonFactory : IPokemonFactory
{
    private readonly World _world;
    private readonly IDataApi _dataApi;

    public PokemonFactory(World world, IDataApi dataApi)
    {
        _world = world;
        _dataApi = dataApi;
    }

    public Entity CreateWildPokemon(int speciesId, int level)
    {
        var species = await _dataApi.GetSpeciesAsync(speciesId);
        if (species == null)
            throw new ArgumentException($"Species {speciesId} not found");

        var entity = _world.Create();

        // Set PokemonData component
        _world.Set(entity, new PokemonData
        {
            SpeciesId = speciesId,
            Nickname = species.Name,
            Level = level,
            Nature = GetRandomNature(),
            Gender = DetermineGender(species.GenderRatio),
            IsShiny = Random.Shared.Next(4096) == 0,
            Exp = CalculateExpForLevel(level, species.GrowthRate),
            Friendship = species.BaseFriendship
        });

        // Set PokemonStats component with random IVs
        var stats = new PokemonStats();
        stats.IV_HP = Random.Shared.Next(32);
        stats.IV_Attack = Random.Shared.Next(32);
        // ... set all IVs

        // Calculate actual stats
        stats.MaxHP = stats.CalculateHP(species.BaseStats.HP, level);
        stats.HP = stats.MaxHP;
        // ... calculate all stats

        _world.Set(entity, stats);

        return entity;
    }
}

// Register factory
services.AddTransient<IPokemonFactory, PokemonFactory>();
```

---

## 10. Testing Strategy

### 10.1 Unit Tests for TypeChart

```csharp
[TestFixture]
public class TypeChartTests
{
    private ITypeChart _typeChart;

    [SetUp]
    public void Setup()
    {
        var logger = new NullLogger<TypeChart>();
        _typeChart = new TypeChart(logger, "test_data");
        await ((TypeChart)_typeChart).LoadTypeChartAsync();
    }

    [Test]
    public void GetEffectiveness_FireVsGrass_Returns2x()
    {
        var effectiveness = _typeChart.GetEffectiveness(
            PokemonType.Fire,
            PokemonType.Grass);

        Assert.That(effectiveness, Is.EqualTo(2.0f));
    }

    [Test]
    public void GetEffectiveness_WaterVsFire_Returns2x()
    {
        var effectiveness = _typeChart.GetEffectiveness(
            PokemonType.Water,
            PokemonType.Fire);

        Assert.That(effectiveness, Is.EqualTo(2.0f));
    }

    [Test]
    public void GetEffectiveness_NormalVsGhost_Returns0x()
    {
        var effectiveness = _typeChart.GetEffectiveness(
            PokemonType.Normal,
            PokemonType.Ghost);

        Assert.That(effectiveness, Is.EqualTo(0.0f));
    }

    [Test]
    public void GetDualTypeEffectiveness_FireVsGrassPoison_Returns2x()
    {
        // Fire is 2x vs Grass, 1x vs Poison = 2x total
        var effectiveness = _typeChart.GetDualTypeEffectiveness(
            PokemonType.Fire,
            PokemonType.Grass,
            PokemonType.Poison);

        Assert.That(effectiveness, Is.EqualTo(2.0f));
    }

    [Test]
    public void GetDualTypeEffectiveness_RockVsFlyingBug_Returns4x()
    {
        // Rock is 2x vs Flying, 2x vs Bug = 4x total
        var effectiveness = _typeChart.GetDualTypeEffectiveness(
            PokemonType.Rock,
            PokemonType.Flying,
            PokemonType.Bug);

        Assert.That(effectiveness, Is.EqualTo(4.0f));
    }

    [Test]
    public void GetSuperEffectiveAgainst_Fire_ReturnsGrassIceSteel()
    {
        var superEffective = _typeChart.GetSuperEffectiveAgainst(PokemonType.Fire);

        Assert.That(superEffective, Contains.Item(PokemonType.Grass));
        Assert.That(superEffective, Contains.Item(PokemonType.Ice));
        Assert.That(superEffective, Contains.Item(PokemonType.Steel));
        Assert.That(superEffective, Contains.Item(PokemonType.Bug));
    }

    [Test]
    public void GetImmuneTypes_Normal_ReturnsGhost()
    {
        var immuneTypes = _typeChart.GetImmuneTypes(PokemonType.Normal);

        Assert.That(immuneTypes, Contains.Item(PokemonType.Ghost));
        Assert.That(immuneTypes.Length, Is.EqualTo(1));
    }
}
```

### 10.2 Integration Tests for DataManager

```csharp
[TestFixture]
public class DataManagerIntegrationTests
{
    private IDataApi _dataApi;

    [SetUp]
    public void Setup()
    {
        var logger = new NullLogger<DataManager>();
        var testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "test_data");

        _dataApi = new DataManager(logger, testDataPath);
    }

    [Test]
    public async Task GetSpeciesAsync_Bulbasaur_ReturnsCorrectData()
    {
        var species = await _dataApi.GetSpeciesAsync(1);

        Assert.That(species, Is.Not.Null);
        Assert.That(species.Name, Is.EqualTo("Bulbasaur"));
        Assert.That(species.Types, Contains.Item("Grass"));
        Assert.That(species.Types, Contains.Item("Poison"));
        Assert.That(species.BaseStats.HP, Is.EqualTo(45));
    }

    [Test]
    public async Task GetMoveAsync_Ember_ReturnsCorrectData()
    {
        var move = await _dataApi.GetMoveAsync("Ember");

        Assert.That(move, Is.Not.Null);
        Assert.That(move.Type, Is.EqualTo("Fire"));
        Assert.That(move.Category, Is.EqualTo(MoveCategory.Special));
        Assert.That(move.Power, Is.EqualTo(40));
        Assert.That(move.Accuracy, Is.EqualTo(100));
    }

    [Test]
    public async Task GetEncountersAsync_Route1_ReturnsEncounterTable()
    {
        var encounters = await _dataApi.GetEncountersAsync("route_1");

        Assert.That(encounters, Is.Not.Null);
        Assert.That(encounters.LocationName, Is.EqualTo("Route 1"));
        Assert.That(encounters.GrassEncounters.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task ReloadDataAsync_WithModOverride_LoadsModData()
    {
        // Set up mod path with custom species data
        var modPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "test_mods");

        if (_dataApi is DataManager dm)
        {
            dm.SetModDataPaths(new[] { modPath });
            await dm.ReloadDataAsync();
        }

        var species = await _dataApi.GetSpeciesAsync(1);

        // Assert mod override was applied
        Assert.That(species.Name, Is.EqualTo("ModifiedBulbasaur"));
    }
}
```

### 10.3 BattleSystem Integration Tests

```csharp
[TestFixture]
public class BattleSystemTests
{
    private World _world;
    private BattleSystem _battleSystem;
    private IDataApi _dataApi;
    private ITypeChart _typeChart;

    [SetUp]
    public async Task Setup()
    {
        _world = World.Create();

        var loggerFactory = new NullLoggerFactory();
        var dataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "test_data");

        _dataApi = new DataManager(loggerFactory.CreateLogger<DataManager>(), dataPath);
        _typeChart = new TypeChart(loggerFactory.CreateLogger<TypeChart>(), dataPath);
        await ((TypeChart)_typeChart).LoadTypeChartAsync();

        _battleSystem = new BattleSystem(_dataApi, _typeChart, loggerFactory.CreateLogger<BattleSystem>());
    }

    [Test]
    public async Task CalculateDamage_FireVsGrass_IsSuperEffective()
    {
        // Create Charmander (Fire)
        var charmander = _world.Create();
        _world.Set(charmander, new PokemonData { SpeciesId = 4, Level = 5 });
        _world.Set(charmander, new PokemonStats { SpAttack = 18 });

        // Create Bulbasaur (Grass/Poison)
        var bulbasaur = _world.Create();
        _world.Set(bulbasaur, new PokemonData { SpeciesId = 1, Level = 5 });
        _world.Set(bulbasaur, new PokemonStats { SpDefense = 20, HP = 25, MaxHP = 25 });

        // Execute Ember (Fire move)
        await _battleSystem.ExecuteMoveAsync(charmander, bulbasaur, "Ember");

        // Verify damage was dealt (should be super effective)
        var finalHP = _world.Get<PokemonStats>(bulbasaur).HP;
        Assert.That(finalHP, Is.LessThan(25));
        Assert.That(finalHP, Is.GreaterThan(10)); // Reasonable damage range
    }

    [TearDown]
    public void TearDown()
    {
        _world.Dispose();
    }
}
```

---

## 11. Architecture Decision Records (ADRs)

### ADR-001: Use Interface-Based Dependency Injection

**Status**: ✅ Accepted
**Date**: 2025-10-26
**Context**: Need to decouple systems from concrete implementations for testability and modding support.

**Decision**: All major systems (DataManager, TypeChart, BattleSystem) will depend on interfaces (`IDataApi`, `ITypeChart`) rather than concrete classes.

**Consequences**:
- ✅ Easy to mock for unit testing
- ✅ Mod API can provide alternative implementations
- ✅ Clear contracts defined in Domain layer
- ⚠️ Slight overhead from virtual dispatch (negligible in practice)

---

### ADR-002: Store Type Effectiveness as 18×18 Float Matrix

**Status**: ✅ Accepted
**Date**: 2025-10-26
**Context**: Need fast type effectiveness lookups during battle damage calculation.

**Decision**: Use a `float[18, 18]` 2D array indexed by `PokemonType` enum values. Default to Gen VI+ official matchups with JSON override support.

**Consequences**:
- ✅ O(1) lookup time (constant performance)
- ✅ Entire matrix fits in L1 cache (18×18×4 = 1296 bytes)
- ✅ Easy to modify via JSON for custom game modes
- ⚠️ Requires type enums to be consecutive 0-17

**Alternatives Considered**:
- Dictionary<(Type, Type), float> - Slower lookup, more memory
- Calculation-based approach - Too complex, harder to mod

---

### ADR-003: Consolidate PokemonStats into Single Component

**Status**: ✅ Accepted
**Date**: 2025-10-26
**Context**: Research identified potential stats duplication between `PokemonStats` and hypothetical `Stats` component.

**Decision**: After code review, confirmed no duplication exists. Keep existing `PokemonStats` struct as the single source of truth for Pokemon instance stats.

**Consequences**:
- ✅ No changes required (already optimal)
- ✅ Clear separation: `SpeciesData.BaseStats` (static) vs `PokemonStats` (instance)
- ✅ Struct type ensures value semantics and performance

---

### ADR-004: JSON-Based Data Loading with Mod Override Support

**Status**: ✅ Accepted (Already Implemented)
**Date**: 2025-10-26
**Context**: Game data must be easily modifiable without recompiling code.

**Decision**: Use JSON files for all static game data (species, moves, items, encounters) with a mod path priority system. DataManager checks mod paths first, then falls back to base data.

**Consequences**:
- ✅ Easy modding - users just replace JSON files
- ✅ Hot-reload support via `ReloadDataAsync()`
- ✅ Human-readable data format
- ⚠️ Larger file size than binary (acceptable trade-off)
- ⚠️ Slower initial load than compiled data (mitigated by caching)

---

### ADR-005: Async Data Loading with Lazy Initialization

**Status**: ✅ Accepted (Already Implemented)
**Date**: 2025-10-26
**Context**: Game startup should be fast, but data loading is I/O-bound.

**Decision**: Use `async Task` for all data loading with lazy initialization via `EnsureDataLoadedAsync()`. Data is only loaded when first accessed.

**Consequences**:
- ✅ Fast startup time (data loads in background)
- ✅ Thread-safe with `SemaphoreSlim`
- ✅ Parallel loading with `Task.WhenAll()`
- ⚠️ First data access has slight latency (mitigated by `PreloadCommonDataAsync()`)

---

## 12. Summary and Next Steps

### 12.1 What This Design Delivers

✅ **TypeChart System** - Complete 18×18 type effectiveness matrix with:
- `ITypeChart` interface in Domain
- `TypeChart` implementation in Core
- JSON override support for modding
- Helper methods for AI and UI (GetSuperEffectiveAgainst, etc.)
- Full unit test coverage

✅ **PokemonStats Consolidation** - Architectural review confirms:
- No duplicate components exist
- Existing `PokemonStats` is optimal
- Clear separation from `SpeciesData.BaseStats`
- No changes required

✅ **Enhanced DataManager** - Building on existing implementation:
- TypeChart loading integration
- Preload optimization for common data
- Metrics and diagnostics API
- Already has mod support, async loading, caching

✅ **Battle System Integration** - Complete damage calculation:
- Type effectiveness integration
- STAB calculation
- Critical hits and random variance
- Official Pokemon damage formula

### 12.2 Implementation Checklist

**Phase 1: TypeChart Implementation** (6 hours)
- [ ] Create `PokemonType` enum in Domain/Battle/PokemonType.cs
- [ ] Create `ITypeChart` interface in Domain/Battle/ITypeChart.cs
- [ ] Implement `TypeChart` class in Core/Battle/TypeChart.cs
- [ ] Create default type_chart.json in /data
- [ ] Add TypeChart unit tests
- [ ] Register TypeChart in DI container

**Phase 2: DataManager Enhancement** (4 hours)
- [ ] Add TypeChart dependency to DataManager constructor
- [ ] Implement `LoadTypeChartAsync()` in DataManager
- [ ] Add `PreloadCommonDataAsync()` method
- [ ] Add `GetMetrics()` diagnostics method
- [ ] Update integration tests

**Phase 3: BattleSystem Integration** (8 hours)
- [ ] Add ITypeChart dependency to BattleSystem
- [ ] Implement complete `CalculateDamageAsync()` method
- [ ] Add STAB calculation logic
- [ ] Add type effectiveness integration
- [ ] Add critical hit and random variance
- [ ] Add battle message logging
- [ ] Write BattleSystem integration tests

**Phase 4: DI and Validation** (2 hours)
- [ ] Update Program.cs DI registration
- [ ] Implement `ValidateServices()` method
- [ ] Add startup logging for data metrics
- [ ] Test complete integration end-to-end

**Total Estimated Effort**: 20 hours

### 12.3 Coordination Memory

**Store in Swarm Memory**:
```bash
npx claude-flow@alpha memory store architecture-design-complete "TypeChart, DataManager enhancements, and BattleSystem integration fully designed. Ready for coder implementation." --namespace swarm/architect

npx claude-flow@alpha memory store critical-files "Domain/Battle/ITypeChart.cs, Domain/Battle/PokemonType.cs, Core/Battle/TypeChart.cs, data/type_chart.json" --namespace swarm/architect

npx claude-flow@alpha memory store di-registration "TypeChart singleton, DataManager with TypeChart dep, BattleSystem with both deps" --namespace swarm/architect
```

### 12.4 Next Agent Actions

**Coder Agent Should**:
1. Review this design document
2. Implement TypeChart system (Phase 1)
3. Enhance DataManager (Phase 2)
4. Integrate BattleSystem (Phase 3)
5. Update DI registration (Phase 4)

**Tester Agent Should**:
1. Write TypeChart unit tests (11 test cases minimum)
2. Write DataManager integration tests
3. Write BattleSystem integration tests with real Pokemon data
4. Verify type effectiveness calculations against Pokemon Showdown calculator

**Reviewer Agent Should**:
1. Verify architectural boundaries (Domain vs Core)
2. Check for proper dependency injection usage
3. Validate thread safety in TypeChart and DataManager
4. Ensure no static dependencies introduced
5. Confirm JSON schema matches implementation

---

**Architecture Design Complete** ✅
**Document Version**: 1.0
**Ready for Implementation**: Yes
**Estimated MVP Impact**: Unblocks battle system (critical path)

