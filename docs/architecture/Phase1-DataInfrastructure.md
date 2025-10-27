# Phase 1: Data Infrastructure Architecture

**Version:** 1.0
**Date:** 2025-10-26
**Status:** Design Complete
**Author:** System Architecture Designer

---

## Executive Summary

This document defines the complete data infrastructure architecture for PokeNET Phase 1. The system provides thread-safe, high-performance access to Pokemon game data (species, moves, items, encounters, type effectiveness) loaded from JSON files with comprehensive mod support.

### Key Architectural Decisions

| Decision | Rationale | Trade-offs |
|----------|-----------|------------|
| **JSON-based data storage** | Human-readable, mod-friendly, version-controllable | Slower than binary formats, but acceptable for ~1000 species/moves |
| **Async-first API** | Non-blocking I/O for large data files | Slight complexity increase, but essential for UI responsiveness |
| **Lazy loading with caching** | Fast startup, memory-efficient | First access slower, but subsequent access O(1) |
| **Immutable data models** | Thread-safe, cache-friendly | Cannot modify after load (intended behavior) |
| **Mod override system** | First-found-wins priority chain | Predictable override behavior, mods must be carefully ordered |

---

## System Architecture Overview

### C4 Context Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     PokeNET Game Engine                      │
│                                                              │
│  ┌────────────┐        ┌──────────────┐      ┌──────────┐  │
│  │    ECS     │───────▶│  DataManager │◀─────│   Mods   │  │
│  │  Systems   │        │   (IDataApi) │      │  System  │  │
│  └────────────┘        └──────────────┘      └──────────┘  │
│       │                        │                     │       │
│       │                        ▼                     │       │
│       │              ┌──────────────────┐            │       │
│       │              │  JSON Data Files │◀───────────┘       │
│       │              │  - species.json  │                    │
│       │              │  - moves.json    │                    │
│       │              │  - items.json    │                    │
│       │              │  - encounters.json│                   │
│       │              │  - typechart.json│                    │
│       │              └──────────────────┘                    │
│       │                                                      │
│       ▼                                                      │
│  ┌────────────────────────────────────────┐                 │
│  │        Battle System (Phase 2)         │                 │
│  │  - Type effectiveness calculations     │                 │
│  │  - Move execution                      │                 │
│  │  - Damage formulas                     │                 │
│  └────────────────────────────────────────┘                 │
└─────────────────────────────────────────────────────────────┘
```

### C4 Container Diagram - Data Layer

```
┌─────────────────────────────────────────────────────────────────┐
│                    PokeNET.Core.Data Namespace                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                      IDataApi                            │   │
│  │  (Interface - Domain Contract)                           │   │
│  │                                                           │   │
│  │  + GetSpeciesAsync(int id)                               │   │
│  │  + GetMoveAsync(string name)                             │   │
│  │  + GetItemAsync(int id)                                  │   │
│  │  + GetEncountersAsync(string locationId)                 │   │
│  │  + GetTypeEffectiveness(type1, type2)                    │   │
│  │  + ReloadDataAsync()                                     │   │
│  └────────────────────┬────────────────────────────────────┘   │
│                       │                                          │
│                       │ implements                               │
│                       ▼                                          │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                  DataManager                             │   │
│  │  (Implementation - Infrastructure)                       │   │
│  │                                                           │   │
│  │  Fields:                                                  │   │
│  │  - Dictionary<int, SpeciesData> _speciesById             │   │
│  │  - Dictionary<string, MoveData> _movesByName             │   │
│  │  - Dictionary<int, ItemData> _itemsById                  │   │
│  │  - Dictionary<string, EncounterTable> _encountersById    │   │
│  │  - TypeChart _typeChart                                  │   │
│  │  - SemaphoreSlim _loadLock (thread safety)               │   │
│  │  - List<string> _modDataPaths                            │   │
│  │                                                           │   │
│  │  Methods:                                                 │   │
│  │  - LoadAllDataAsync() [private]                          │   │
│  │  - LoadJsonArrayAsync<T>() [private]                     │   │
│  │  - ResolveDataPath() [mod override logic]                │   │
│  │  - EnsureDataLoadedAsync() [lazy loading]                │   │
│  └────────────────────┬────────────────────────────────────┘   │
│                       │                                          │
│                       │ uses                                     │
│                       ▼                                          │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │               Data Model Classes                         │   │
│  │                                                           │   │
│  │  - SpeciesData      (Pokemon species definitions)        │   │
│  │  - MoveData         (Move definitions)                   │   │
│  │  - ItemData         (Item definitions)                   │   │
│  │  - EncounterTable   (Wild encounter data)                │   │
│  │  - TypeChart        (Type effectiveness matrix)          │   │
│  │  - BaseStats        (HP, Atk, Def, SpA, SpD, Spe)        │   │
│  │  - Evolution        (Evolution conditions)               │   │
│  │  - LevelMove        (Level-up learnset)                  │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Component Specifications

### 1. IDataApi Interface

**Location:** `/PokeNET/PokeNET.Core/Data/IDataApi.cs`
**Status:** ✅ Implemented

**Purpose:** Define the domain contract for data access. Follows Dependency Inversion Principle - domain layer defines interface, infrastructure layer implements it.

**Key Design Principles:**
- All methods are async for I/O operations
- Returns nullable types for "not found" scenarios
- Returns `IReadOnlyList<T>` for collections (immutability)
- Thread-safe by contract (implementations must be thread-safe)

**Missing Methods (to be added):**

```csharp
/// <summary>
/// Retrieves type effectiveness multiplier for attack type vs defense type.
/// </summary>
/// <param name="attackType">Attacking move's type.</param>
/// <param name="defenseType">Defending Pokemon's type.</param>
/// <returns>Effectiveness multiplier (0, 0.25, 0.5, 1, 2, 4).</returns>
Task<double> GetTypeEffectivenessAsync(PokemonType attackType, PokemonType defenseType);

/// <summary>
/// Retrieves complete type chart for reference.
/// </summary>
/// <returns>Type chart with all effectiveness data.</returns>
Task<TypeChart> GetTypeChartAsync();
```

---

### 2. DataManager Implementation

**Location:** `/PokeNET/PokeNET.Core/Data/DataManager.cs`
**Status:** ✅ Implemented (needs TypeChart integration)

**Responsibilities:**
1. Load JSON data files from disk
2. Parse JSON into strongly-typed data models
3. Cache parsed data in memory (Dictionary lookups)
4. Provide thread-safe access via SemaphoreSlim
5. Support mod overrides (priority-based file resolution)
6. Lazy loading (load on first access)

**Thread Safety Strategy:**

```
Reader Thread A                Reader Thread B                 Writer Thread
     │                              │                               │
     ├─ GetSpeciesAsync(1)          │                               │
     │  └─ EnsureDataLoadedAsync()  │                               │
     │     ├─ _loadLock.WaitAsync() │                               │
     │     │  [ACQUIRED]             │                               │
     │     ├─ LoadAllDataAsync()    │                               │
     │     │                         ├─ GetMoveAsync("Tackle")       │
     │     │                         │  └─ EnsureDataLoadedAsync()   │
     │     │                         │     └─ _loadLock.WaitAsync()  │
     │     │                         │        [WAITING...]           │
     │     │                         │                               │
     │     └─ _loadLock.Release()   │                               │
     │        [RELEASED]             │                               │
     │                               │     [ACQUIRED]                │
     │                               │     └─ Return cached data     │
     │                               │        _loadLock.Release()    │
     │                               │                               │
     │                               │                               ├─ ReloadDataAsync()
     │                               │                               │  └─ _loadLock.WaitAsync()
     │                               │                               │     [BLOCKS all readers]
     │                               │                               │     Clear caches
     │                               │                               │     Reload from disk
     │                               │                               │     _loadLock.Release()
```

**Caching Strategy:**

| Data Type | Primary Index | Secondary Index | Rationale |
|-----------|---------------|-----------------|-----------|
| Species | `Dictionary<int, SpeciesData>` | `Dictionary<string, SpeciesData>` | Lookup by ID (1-1025) or name ("Pikachu") |
| Moves | `Dictionary<string, MoveData>` | None | Moves identified by name ("Thunderbolt") |
| Items | `Dictionary<int, ItemData>` | `Dictionary<string, ItemData>` | Lookup by ID or name |
| Encounters | `Dictionary<string, EncounterTable>` | None | Keyed by location ID ("route_1") |
| Type Chart | Single `TypeChart` instance | None | Single global type effectiveness matrix |

**Mod Override Resolution:**

```
ResolveDataPath("species.json"):
  1. Check /Mods/CompetitiveMod/Data/species.json       [Priority 1]
  2. Check /Mods/RebalanceMod/Data/species.json         [Priority 2]
  3. Check /Data/species.json                           [Base game - Fallback]

  → Returns first found file path
  → Mods can fully replace base game data
  → Partial overrides not supported (whole file replacement)
```

---

### 3. Data Models

#### 3.1 SpeciesData

**Location:** `/PokeNET/PokeNET.Core/Data/SpeciesData.cs`
**Status:** ✅ Implemented

**Schema:**
```csharp
class SpeciesData {
    int Id                          // National Pokedex number (1-1025+)
    string Name                     // "Bulbasaur"
    List<string> Types              // ["Grass", "Poison"]
    BaseStats BaseStats             // HP: 45, Atk: 49, Def: 49, SpA: 65, SpD: 65, Spe: 45
    List<string> Abilities          // ["Overgrow"]
    string? HiddenAbility           // "Chlorophyll"
    string GrowthRate               // "Medium Slow"
    int BaseExperience              // 64
    int GenderRatio                 // 31 (87.5% male)
    int CatchRate                   // 45
    int BaseFriendship              // 70
    List<string> EggGroups          // ["Monster", "Grass"]
    int HatchSteps                  // 5120
    int Height                      // 7 (0.7m)
    int Weight                      // 69 (6.9kg)
    string Description              // Pokedex entry
    List<LevelMove> LevelMoves      // Level-up learnset
    List<string> TmMoves            // TM/HM compatibility
    List<string> EggMoves           // Egg moves
    List<Evolution> Evolutions      // Evolution chains
}
```

**JSON Example:**
```json
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
    { "level": 3, "moveName": "Growl" },
    { "level": 7, "moveName": "Vine Whip" }
  ],
  "tmMoves": ["Toxic", "Venoshock", "Hidden Power"],
  "eggMoves": ["Skull Bash", "Amnesia"],
  "evolutions": [
    {
      "targetSpeciesId": 2,
      "method": "Level",
      "requiredLevel": 16
    }
  ]
}
```

#### 3.2 MoveData

**Location:** `/PokeNET/PokeNET.Core/Data/MoveData.cs`
**Status:** ✅ Implemented

**Schema:**
```csharp
class MoveData {
    string Name                             // "Thunderbolt"
    string Type                             // "Electric"
    MoveCategory Category                   // Special
    int Power                               // 90
    int Accuracy                            // 100
    int PP                                  // 15
    int Priority                            // 0
    string Target                           // "SingleTarget"
    int EffectChance                        // 10 (10% paralysis)
    string Description                      // "A strong electric attack..."
    List<string> Flags                      // ["Contact", "Protect"]
    string? EffectScript                    // "scripts/moves/paralysis.csx"
    Dictionary<string, object>? EffectParameters  // { "statusCondition": "Paralysis" }
}

enum MoveCategory { Physical, Special, Status }
```

**JSON Example:**
```json
{
  "name": "Thunderbolt",
  "type": "Electric",
  "category": "Special",
  "power": 90,
  "accuracy": 100,
  "pp": 15,
  "priority": 0,
  "target": "SingleTarget",
  "effectChance": 10,
  "description": "A strong electric blast that may paralyze the target.",
  "flags": ["Protect", "Mirror"],
  "effectScript": "scripts/moves/paralysis.csx",
  "effectParameters": {
    "statusCondition": "Paralysis",
    "chance": 10
  }
}
```

#### 3.3 ItemData

**Location:** `/PokeNET/PokeNET.Core/Data/ItemData.cs`
**Status:** ✅ Implemented

**Schema:**
```csharp
class ItemData {
    int Id                                  // Item ID
    string Name                             // "Potion"
    ItemCategory Category                   // Medicine
    int BuyPrice                            // 200
    int SellPrice                           // 100
    string Description                      // "Restores 20 HP"
    bool Consumable                         // true
    bool UsableInBattle                     // true
    bool UsableOutsideBattle                // true
    bool Holdable                           // false
    string? EffectScript                    // "scripts/items/potion.csx"
    Dictionary<string, object>? EffectParameters  // { "healAmount": 20 }
    string? SpritePath                      // "sprites/items/potion.png"
}

enum ItemCategory {
    Medicine, Pokeball, BattleItem, HeldItem,
    EvolutionItem, TM, KeyItem, Berry, Miscellaneous
}
```

**JSON Example:**
```json
{
  "id": 17,
  "name": "Potion",
  "category": "Medicine",
  "buyPrice": 200,
  "sellPrice": 100,
  "description": "A spray-type medicine for treating wounds. It restores 20 HP.",
  "consumable": true,
  "usableInBattle": true,
  "usableOutsideBattle": true,
  "holdable": false,
  "effectScript": "scripts/items/potion.csx",
  "effectParameters": {
    "healAmount": 20
  },
  "spritePath": "sprites/items/potion.png"
}
```

#### 3.4 EncounterTable

**Location:** `/PokeNET/PokeNET.Core/Data/EncounterTable.cs`
**Status:** ✅ Implemented

**Schema:**
```csharp
class EncounterTable {
    string LocationId                       // "route_1"
    string LocationName                     // "Route 1"
    List<Encounter> GrassEncounters         // Wild grass encounters
    List<Encounter> WaterEncounters         // Surfing encounters
    List<Encounter> OldRodEncounters        // Fishing (Old Rod)
    List<Encounter> GoodRodEncounters       // Fishing (Good Rod)
    List<Encounter> SuperRodEncounters      // Fishing (Super Rod)
    List<Encounter> CaveEncounters          // Cave/rock encounters
    List<SpecialEncounter> SpecialEncounters // Legendary/scripted
}

class Encounter {
    int SpeciesId                           // Pokemon species
    int MinLevel                            // 2
    int MaxLevel                            // 4
    int Rate                                // 40 (40% encounter rate)
    string TimeOfDay                        // "Any" | "Morning" | "Day" | "Night"
    string Weather                          // "Any" | "Sunny" | "Rainy"
}

class SpecialEncounter {
    string EncounterId                      // "mewtwo_cerulean_cave"
    int SpeciesId                           // 150 (Mewtwo)
    int Level                               // 70
    bool OneTime                            // true
    Dictionary<string, object> Conditions   // { "badge_count": 8 }
    string? Script                          // "scripts/encounters/mewtwo.csx"
}
```

**JSON Example:**
```json
{
  "locationId": "route_1",
  "locationName": "Route 1",
  "grassEncounters": [
    {
      "speciesId": 16,
      "minLevel": 2,
      "maxLevel": 4,
      "rate": 40,
      "timeOfDay": "Any",
      "weather": "Any"
    },
    {
      "speciesId": 19,
      "minLevel": 2,
      "maxLevel": 3,
      "rate": 35,
      "timeOfDay": "Any",
      "weather": "Any"
    }
  ],
  "waterEncounters": [],
  "specialEncounters": [
    {
      "encounterId": "route1_tutorial_pokemon",
      "speciesId": 25,
      "level": 5,
      "oneTime": true,
      "conditions": {
        "flag": "tutorial_complete"
      },
      "script": "scripts/encounters/starter_pikachu.csx"
    }
  ]
}
```

---

### 4. TypeChart System (NEW)

**Location:** `/PokeNET/PokeNET.Core/Data/TypeChart.cs` *(to be created)*
**Status:** ⚠️ Not yet implemented

**Purpose:** Manage type effectiveness calculations for battle system.

**Design Specifications:**

```csharp
/// <summary>
/// Pokemon type enumeration (18 types).
/// </summary>
public enum PokemonType
{
    Normal, Fire, Water, Electric, Grass, Ice, Fighting, Poison,
    Ground, Flying, Psychic, Bug, Rock, Ghost, Dragon, Dark, Steel, Fairy
}

/// <summary>
/// Type effectiveness chart (18x18 matrix).
/// </summary>
public class TypeChart
{
    // 18x18 matrix: [AttackType][DefenseType] = Multiplier
    private readonly Dictionary<(PokemonType, PokemonType), double> _effectiveness;

    /// <summary>
    /// Gets type effectiveness multiplier.
    /// </summary>
    /// <param name="attackType">Type of the attacking move.</param>
    /// <param name="defenseType">Type of the defending Pokemon.</param>
    /// <returns>Effectiveness multiplier (0, 0.25, 0.5, 1, 2, 4).</returns>
    public double GetEffectiveness(PokemonType attackType, PokemonType defenseType);

    /// <summary>
    /// Calculates total effectiveness against dual-type Pokemon.
    /// </summary>
    /// <param name="attackType">Type of the attacking move.</param>
    /// <param name="defenseType1">First type of defender.</param>
    /// <param name="defenseType2">Second type of defender (nullable).</param>
    /// <returns>Combined effectiveness (multiplies both type matchups).</returns>
    public double GetDualTypeEffectiveness(
        PokemonType attackType,
        PokemonType defenseType1,
        PokemonType? defenseType2);

    /// <summary>
    /// Checks if move has no effect (0x damage).
    /// </summary>
    public bool IsImmune(PokemonType attackType, PokemonType defenseType);

    /// <summary>
    /// Checks if move is not very effective (0.5x or 0.25x damage).
    /// </summary>
    public bool IsNotVeryEffective(PokemonType attackType, PokemonType defenseType);

    /// <summary>
    /// Checks if move is super effective (2x or 4x damage).
    /// </summary>
    public bool IsSuperEffective(PokemonType attackType, PokemonType defenseType);
}
```

**TypeChart JSON Schema:**

```json
{
  "effectiveness": {
    "Normal": {
      "Normal": 1.0,
      "Fire": 1.0,
      "Water": 1.0,
      "Rock": 0.5,
      "Ghost": 0.0,
      "Steel": 0.5
    },
    "Fire": {
      "Fire": 0.5,
      "Water": 0.5,
      "Grass": 2.0,
      "Ice": 2.0,
      "Bug": 2.0,
      "Rock": 0.5,
      "Dragon": 0.5,
      "Steel": 2.0
    },
    "Electric": {
      "Water": 2.0,
      "Electric": 0.5,
      "Grass": 0.5,
      "Ground": 0.0,
      "Flying": 2.0,
      "Dragon": 0.5
    }
    // ... (18 types × ~18 matchups each)
  }
}
```

**Dual-Type Calculation Example:**

```csharp
// Thunderbolt (Electric) vs Gyarados (Water/Flying)
double effectiveness = typeChart.GetDualTypeEffectiveness(
    PokemonType.Electric,  // Attack type
    PokemonType.Water,     // Defense type 1 (2x)
    PokemonType.Flying     // Defense type 2 (2x)
);
// Result: 2.0 × 2.0 = 4.0× (quadruple damage!)

// Ice Beam (Ice) vs Torterra (Grass/Ground)
double effectiveness = typeChart.GetDualTypeEffectiveness(
    PokemonType.Ice,       // Attack type
    PokemonType.Grass,     // Defense type 1 (2x)
    PokemonType.Ground     // Defense type 2 (2x)
);
// Result: 2.0 × 2.0 = 4.0×
```

---

## Integration Points

### 4.1 ECS Integration

**How ECS systems will use IDataApi:**

```csharp
// BattleSystem needs move data
public class BattleSystem : ISystem
{
    private readonly IDataApi _dataApi;

    public async Task ExecuteMoveAsync(Entity attacker, Entity defender, int moveId)
    {
        // Get move data from DataManager
        var moveData = await _dataApi.GetMoveAsync(moveName);
        if (moveData == null) return;

        // Get type effectiveness
        var attackerTypes = attacker.Get<PokemonData>().Types;
        var defenderTypes = defender.Get<PokemonData>().Types;

        var effectiveness = await _dataApi.GetTypeEffectivenessAsync(
            Enum.Parse<PokemonType>(moveData.Type),
            Enum.Parse<PokemonType>(defenderTypes[0])
        );

        // Calculate damage using move data + type effectiveness
        int damage = CalculateDamage(attacker, defender, moveData, effectiveness);

        // Apply damage...
    }
}

// WildEncounterSystem needs encounter tables
public class WildEncounterSystem : ISystem
{
    private readonly IDataApi _dataApi;

    public async Task<Entity?> GenerateWildPokemonAsync(string locationId)
    {
        var encounterTable = await _dataApi.GetEncountersAsync(locationId);
        if (encounterTable == null) return null;

        // Select random encounter based on rates
        var encounter = SelectRandomEncounter(encounterTable.GrassEncounters);

        // Get species data
        var species = await _dataApi.GetSpeciesAsync(encounter.SpeciesId);

        // Create Pokemon entity using species data
        var pokemon = CreatePokemonEntity(species, encounter.MinLevel, encounter.MaxLevel);
        return pokemon;
    }
}
```

### 4.2 Mod System Integration

**Mod loading workflow:**

```csharp
// In ModLoader
public class ModLoader
{
    private readonly IDataApi _dataApi;

    public async Task LoadModsAsync(List<ModMetadata> mods)
    {
        // Priority order: Last loaded mod = highest priority
        var modDataPaths = mods
            .OrderByDescending(m => m.Priority)
            .Select(m => Path.Combine(m.ModPath, "Data"))
            .ToList();

        // Configure DataManager with mod paths
        if (_dataApi is DataManager dataManager)
        {
            dataManager.SetModDataPaths(modDataPaths);
            await dataManager.ReloadDataAsync();
        }
    }
}
```

**Example mod override:**

```
Base game:
  /Data/species.json (151 Gen 1 Pokemon)

Mod installed:
  /Mods/Gen2Expansion/Data/species.json (251 Pokemon)

Result:
  dataApi.GetSpeciesAsync(152) → Chikorita (from mod)
  dataApi.GetSpeciesAsync(1)   → Bulbasaur (from mod, replaces base)

Note: Partial overrides not supported. Mod must provide complete species.json.
```

---

## Performance Characteristics

### Memory Usage (Estimated)

| Data Type | Count | Size per Item | Total Size |
|-----------|-------|---------------|------------|
| Species | 1,025 | ~2 KB | ~2 MB |
| Moves | 919 | ~500 bytes | ~450 KB |
| Items | 800 | ~300 bytes | ~240 KB |
| Encounters | 200 | ~1 KB | ~200 KB |
| Type Chart | 1 | ~2 KB | ~2 KB |
| **Total** | | | **~3 MB** |

**Notes:**
- Very small memory footprint (~3 MB for all game data)
- All data cached in memory after first load
- Subsequent lookups are O(1) dictionary access
- No need for complex memory management

### Access Times

| Operation | First Access | Subsequent Access | Notes |
|-----------|-------------|-------------------|-------|
| `GetSpeciesAsync(1)` | ~5-10ms | ~0.01ms | First loads from disk, then cached |
| `GetMoveAsync("Tackle")` | ~5-10ms | ~0.01ms | Dictionary lookup after load |
| `GetEncountersAsync("route_1")` | ~5-10ms | ~0.01ms | O(1) after initial load |
| `GetTypeEffectivenessAsync(Electric, Water)` | ~5-10ms | ~0.01ms | Matrix lookup after load |
| `ReloadDataAsync()` | ~50-100ms | N/A | Full reload (mod changes) |

**Lazy Loading Benefits:**
- Game starts in ~100ms (no data load)
- First battle triggers data load (~50ms one-time cost)
- Every subsequent operation is instant (cached)

---

## Quality Attributes

### 1. Performance
- ✅ **Lazy loading:** No startup delay
- ✅ **Caching:** O(1) lookup after first access
- ✅ **Async I/O:** Non-blocking file reads
- ✅ **Parallel loading:** `Task.WhenAll` loads all data types concurrently

### 2. Thread Safety
- ✅ **SemaphoreSlim:** Prevents race conditions during lazy load
- ✅ **Immutable data:** SpeciesData, MoveData, etc. are read-only after load
- ✅ **Concurrent reads:** Multiple threads can read cached data simultaneously

### 3. Extensibility
- ✅ **Interface-based:** Easy to mock for testing
- ✅ **Mod support:** Drop-in JSON file replacement
- ✅ **Script integration:** Moves/items can reference external .csx scripts
- ✅ **Future-proof:** Can add new data types (abilities, natures, etc.)

### 4. Maintainability
- ✅ **JSON format:** Human-readable, easy to edit
- ✅ **Separation of concerns:** Data layer isolated from ECS/battle logic
- ✅ **Logging:** Comprehensive logging for debugging
- ✅ **Error handling:** Graceful fallback if files missing

### 5. Security
- ⚠️ **JSON deserialization:** Uses System.Text.Json (no code execution risk)
- ⚠️ **Mod scripts:** .csx files executed via Roslyn (sandboxed, but trust required)
- ✅ **No SQL injection:** No database queries
- ✅ **No network calls:** All data local

---

## Testing Strategy

### Unit Tests

```csharp
// DataManager unit tests
[Fact]
public async Task GetSpeciesAsync_ReturnsCorrectSpecies()
{
    // Arrange
    var dataManager = CreateDataManager();

    // Act
    var bulbasaur = await dataManager.GetSpeciesAsync(1);

    // Assert
    Assert.NotNull(bulbasaur);
    Assert.Equal("Bulbasaur", bulbasaur.Name);
    Assert.Equal(2, bulbasaur.Types.Count);
    Assert.Contains("Grass", bulbasaur.Types);
}

[Fact]
public async Task GetTypeEffectivenessAsync_FireVsGrass_Returns2x()
{
    var dataManager = CreateDataManager();

    var effectiveness = await dataManager.GetTypeEffectivenessAsync(
        PokemonType.Fire,
        PokemonType.Grass
    );

    Assert.Equal(2.0, effectiveness);
}

[Fact]
public async Task ModOverride_PrioritizesModData()
{
    // Arrange: Create mod with custom species
    var dataManager = CreateDataManager();
    dataManager.SetModDataPaths(new[] { "TestMod/Data" });

    // Act
    var species = await dataManager.GetSpeciesAsync(1);

    // Assert: Should load from mod, not base game
    Assert.Equal("ModifiedBulbasaur", species.Name);
}

[Fact]
public async Task ConcurrentAccess_ThreadSafe()
{
    var dataManager = CreateDataManager();

    // Act: 100 concurrent reads
    var tasks = Enumerable.Range(1, 100)
        .Select(i => dataManager.GetSpeciesAsync(i % 151 + 1))
        .ToArray();

    var results = await Task.WhenAll(tasks);

    // Assert: All successful, no exceptions
    Assert.All(results, r => Assert.NotNull(r));
}
```

### Integration Tests

```csharp
[Fact]
public async Task BattleSystem_UsesDataApi_CalculatesDamageCorrectly()
{
    // Arrange: Real DataManager + BattleSystem
    var dataApi = CreateRealDataManager();
    var battleSystem = new BattleSystem(dataApi);

    var charizard = CreatePokemon(6, 50); // Charizard, level 50
    var blastoise = CreatePokemon(9, 50); // Blastoise, level 50

    // Act: Charizard uses Flamethrower on Blastoise
    await battleSystem.ExecuteMoveAsync(charizard, blastoise, "Flamethrower");

    // Assert: Damage should be reduced (Water resists Fire)
    var damage = blastoise.Get<Health>().MaxHP - blastoise.Get<Health>().CurrentHP;
    Assert.InRange(damage, 10, 50); // Not very effective damage range
}
```

---

## Migration Path (Future Phases)

### Phase 2: Battle System
- Add `GetAbilityAsync(string name)` to IDataApi
- Add `GetNatureAsync(string name)` for stat modifiers
- Extend TypeChart with ability interactions (e.g., Levitate immunity)

### Phase 3: Persistence
- Add `IDataWriter` interface for save data
- Separate static game data (IDataApi) from dynamic save data (IDataWriter)

### Phase 4: Online Features
- Add data versioning (schema version number)
- Implement delta updates (only download changed data)
- Add data validation (checksum verification)

---

## Risks and Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Large JSON files slow startup** | High | Low | Lazy loading + async I/O minimizes impact |
| **Mod data corruption** | Medium | Medium | Validation + error handling with fallback to base game |
| **Memory usage in low-end devices** | Medium | Low | 3 MB is negligible on modern hardware |
| **Type chart errors (wrong multipliers)** | High | Low | Comprehensive unit tests + reference data |
| **Concurrent mod loading race conditions** | High | Low | SemaphoreSlim ensures atomic reload |

---

## Architecture Decision Records (ADRs)

### ADR-001: Use JSON for Data Storage

**Context:** Need human-readable format for modding, version control, and ease of editing.

**Decision:** Use JSON with System.Text.Json serializer.

**Alternatives Considered:**
- Binary format (faster, smaller, but not human-readable)
- XML (verbose, harder to edit)
- SQLite (overkill for read-only static data)

**Consequences:**
- ✅ Easy for modders to edit
- ✅ Git-friendly (diff/merge)
- ❌ Slower than binary (acceptable for ~3 MB data)

---

### ADR-002: Lazy Loading with Full Caching

**Context:** Balance between startup time and memory usage.

**Decision:** Load nothing at startup, load all data on first access, cache forever.

**Alternatives Considered:**
- Eager loading (slow startup)
- Streaming/partial loading (complex implementation)
- No caching (repeated disk I/O)

**Consequences:**
- ✅ Fast startup
- ✅ Simple implementation
- ❌ First access slightly slower (one-time cost)

---

### ADR-003: Immutable Data Models

**Context:** Thread safety and predictability.

**Decision:** All data models are immutable after deserialization.

**Alternatives Considered:**
- Mutable models with locks (complex, error-prone)
- Copy-on-write (unnecessary for static data)

**Consequences:**
- ✅ Thread-safe by design
- ✅ Cache-friendly
- ❌ Cannot modify at runtime (intended behavior)

---

## Conclusion

The Phase 1 data infrastructure provides a solid foundation for PokeNET:

✅ **Complete:** All required data types defined (species, moves, items, encounters, types)
✅ **Performant:** ~3 MB memory, O(1) lookups, lazy loading
✅ **Thread-safe:** SemaphoreSlim + immutable data
✅ **Extensible:** Mod support, script integration
✅ **Testable:** Interface-based, comprehensive unit tests

**Next Steps:**
1. Implement `TypeChart` class and `PokemonType` enum
2. Add type effectiveness methods to `IDataApi` and `DataManager`
3. Create sample JSON data files (10 species, 20 moves, 10 items, 5 encounters)
4. Write unit tests for TypeChart calculations
5. Integrate with BattleSystem (Phase 2)

---

**Reviewed by:** System Architecture Designer
**Approved for implementation:** 2025-10-26
