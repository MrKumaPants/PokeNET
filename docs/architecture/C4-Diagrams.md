# C4 Architecture Diagrams - Phase 1 Data Infrastructure

**Version:** 1.0
**Date:** 2025-10-26

This document contains C4 model architecture diagrams for the PokeNET data infrastructure.

---

## Level 1: System Context Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                          PokeNET Game Engine                         │
│                                                                      │
│  ┌────────────────────┐                    ┌──────────────────────┐│
│  │   Game Players     │                    │   Mod Developers     ││
│  │  (End Users)       │                    │  (Content Creators)  ││
│  └─────────┬──────────┘                    └──────────┬───────────┘│
│            │                                           │            │
│            │ plays game                                │ creates    │
│            │                                           │ content    │
│            ▼                                           ▼            │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │                    PokeNET Core Engine                        │ │
│  │                                                                │ │
│  │  ┌────────────┐  ┌──────────────┐  ┌─────────────────────┐  │ │
│  │  │    ECS     │  │ Battle System│  │   Data Manager      │  │ │
│  │  │  Systems   │──│              │──│   (IDataApi)        │  │ │
│  │  └────────────┘  └──────────────┘  └─────────┬───────────┘  │ │
│  │                                               │              │ │
│  └───────────────────────────────────────────────┼──────────────┘ │
│                                                  │                 │
│                                                  ▼                 │
│                                    ┌──────────────────────────┐   │
│                                    │   JSON Data Files        │   │
│                                    │   - species.json         │   │
│                                    │   - moves.json           │   │
│                                    │   - items.json           │   │
│                                    │   - encounters.json      │   │
│                                    │   - typechart.json       │   │
│                                    └──────────────────────────┘   │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘

External Systems:
- JSON Files: Static game data storage
- Mod System: Content override mechanism
```

---

## Level 2: Container Diagram - Data Layer

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      PokeNET.Core.Data (Namespace)                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                        IDataApi Interface                         │  │
│  │                    (Domain Contract Layer)                        │  │
│  │                                                                    │  │
│  │  Methods:                                                          │  │
│  │  + GetSpeciesAsync(int id) → SpeciesData?                         │  │
│  │  + GetMoveAsync(string name) → MoveData?                          │  │
│  │  + GetItemAsync(int id) → ItemData?                               │  │
│  │  + GetEncountersAsync(string locationId) → EncounterTable?        │  │
│  │  + GetTypeEffectivenessAsync(type1, type2) → double               │  │
│  │  + ReloadDataAsync() → Task                                       │  │
│  └────────────────────────────┬─────────────────────────────────────┘  │
│                               │                                         │
│                               │ implements                              │
│                               ▼                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                       DataManager                                 │  │
│  │                  (Infrastructure Layer)                           │  │
│  │                                                                    │  │
│  │  Responsibilities:                                                 │  │
│  │  ✓ Load JSON files from disk (async I/O)                         │  │
│  │  ✓ Parse JSON → strongly-typed models                            │  │
│  │  ✓ Cache data in memory (Dictionary<K,V>)                        │  │
│  │  ✓ Provide thread-safe access (SemaphoreSlim)                    │  │
│  │  ✓ Support mod overrides (priority chain)                        │  │
│  │                                                                    │  │
│  │  Private Fields:                                                   │  │
│  │  - Dictionary<int, SpeciesData> _speciesById                      │  │
│  │  - Dictionary<string, SpeciesData> _speciesByName                 │  │
│  │  - Dictionary<string, MoveData> _movesByName                      │  │
│  │  - Dictionary<int, ItemData> _itemsById                           │  │
│  │  - Dictionary<string, ItemData> _itemsByName                      │  │
│  │  - Dictionary<string, EncounterTable> _encountersById             │  │
│  │  - TypeChart _typeChart                                           │  │
│  │  - SemaphoreSlim _loadLock                                        │  │
│  │  - List<string> _modDataPaths                                     │  │
│  │                                                                    │  │
│  │  Key Methods:                                                      │  │
│  │  - LoadAllDataAsync() [parallel loading]                          │  │
│  │  - ResolveDataPath(filename) [mod override logic]                │  │
│  │  - EnsureDataLoadedAsync() [lazy loading]                        │  │
│  └────────────────────────────┬─────────────────────────────────────┘  │
│                               │                                         │
│                               │ uses                                    │
│                               ▼                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                      Data Models                                  │  │
│  │                  (Pure Data Transfer Objects)                     │  │
│  │                                                                    │  │
│  │  Core Models:                                                      │  │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │  │
│  │  │  SpeciesData    │  │    MoveData     │  │    ItemData     │  │  │
│  │  ├─────────────────┤  ├─────────────────┤  ├─────────────────┤  │  │
│  │  │ int Id          │  │ string Name     │  │ int Id          │  │  │
│  │  │ string Name     │  │ string Type     │  │ string Name     │  │  │
│  │  │ List<string>    │  │ MoveCategory    │  │ ItemCategory    │  │  │
│  │  │   Types         │  │   Category      │  │   Category      │  │  │
│  │  │ BaseStats       │  │ int Power       │  │ int BuyPrice    │  │  │
│  │  │ List<LevelMove> │  │ int Accuracy    │  │ bool Consumable │  │  │
│  │  │ List<Evolution> │  │ int PP          │  │ string?         │  │  │
│  │  │ ...             │  │ ...             │  │   EffectScript  │  │  │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────┘  │  │
│  │                                                                    │  │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │  │
│  │  │ EncounterTable  │  │   TypeChart     │  │  PokemonType    │  │  │
│  │  ├─────────────────┤  ├─────────────────┤  ├─────────────────┤  │  │
│  │  │ string          │  │ Dictionary<     │  │ enum {          │  │  │
│  │  │   LocationId    │  │   (Type,Type),  │  │   Normal,       │  │  │
│  │  │ List<Encounter> │  │   double>       │  │   Fire,         │  │  │
│  │  │   Grass         │  │   _effectiveness│  │   Water,        │  │  │
│  │  │ List<Special    │  │                 │  │   Electric,     │  │  │
│  │  │   Encounter>    │  │ + GetEffective- │  │   ... (18)      │  │  │
│  │  │ ...             │  │     ness()      │  │ }               │  │  │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────┘  │  │
│  │                                                                    │  │
│  │  Supporting Models:                                                │  │
│  │  - BaseStats (HP, Atk, Def, SpA, SpD, Spe)                        │  │
│  │  - LevelMove (Level, MoveName)                                    │  │
│  │  - Evolution (TargetSpeciesId, Method, Conditions)                │  │
│  │  - Encounter (SpeciesId, MinLevel, MaxLevel, Rate)                │  │
│  │  - SpecialEncounter (EncounterId, SpeciesId, Level, Conditions)   │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘

External Dependencies:
┌────────────────────────────────────────────────────────────────────┐
│  System.Text.Json (JSON parsing)                                   │
│  Microsoft.Extensions.Logging (Logging)                             │
│  System.Threading (SemaphoreSlim for thread safety)                │
└────────────────────────────────────────────────────────────────────┘
```

---

## Level 3: Component Diagram - DataManager Internal Architecture

```
┌────────────────────────────────────────────────────────────────────────┐
│                         DataManager Class                               │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │                    Public API (IDataApi)                          │ │
│  ├──────────────────────────────────────────────────────────────────┤ │
│  │                                                                    │ │
│  │  GetSpeciesAsync(int id)                                          │ │
│  │  GetMoveAsync(string name)                                        │ │
│  │  GetItemAsync(int id)                                             │ │
│  │  GetEncountersAsync(string locationId)                            │ │
│  │  GetTypeEffectivenessAsync(type1, type2)                          │ │
│  │  ReloadDataAsync()                                                │ │
│  │                                                                    │ │
│  └────────────────────────┬──────────────────────────────────────────┘ │
│                           │                                            │
│                           ▼                                            │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │               Lazy Loading & Thread Safety Layer                  │ │
│  ├──────────────────────────────────────────────────────────────────┤ │
│  │                                                                    │ │
│  │  EnsureDataLoadedAsync()                                          │ │
│  │  ├─ Check _isLoaded flag                                          │ │
│  │  ├─ Acquire _loadLock (SemaphoreSlim)                            │ │
│  │  ├─ Double-check _isLoaded (thread-safe pattern)                 │ │
│  │  └─ Call LoadAllDataAsync() if needed                            │ │
│  │                                                                    │ │
│  └────────────────────────┬──────────────────────────────────────────┘ │
│                           │                                            │
│                           ▼                                            │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │                    Parallel Data Loading                          │ │
│  ├──────────────────────────────────────────────────────────────────┤ │
│  │                                                                    │ │
│  │  LoadAllDataAsync()                                               │ │
│  │  └─ Task.WhenAll([                                                │ │
│  │       LoadSpeciesDataAsync(),     ← species.json                  │ │
│  │       LoadMoveDataAsync(),        ← moves.json                    │ │
│  │       LoadItemDataAsync(),        ← items.json                    │ │
│  │       LoadEncounterDataAsync(),   ← encounters.json               │ │
│  │       LoadTypeChartAsync()        ← typechart.json (optional)     │ │
│  │     ])                                                             │ │
│  │                                                                    │ │
│  └────────────────────────┬──────────────────────────────────────────┘ │
│                           │                                            │
│                           ▼                                            │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │                  JSON File Resolution (Mod Support)               │ │
│  ├──────────────────────────────────────────────────────────────────┤ │
│  │                                                                    │ │
│  │  ResolveDataPath(string filename)                                 │ │
│  │  ├─ foreach (modPath in _modDataPaths)                           │ │
│  │  │    ├─ Check: modPath/filename exists?                         │ │
│  │  │    └─ Return first found (priority override)                  │ │
│  │  └─ Fallback: _dataPath/filename (base game data)                │ │
│  │                                                                    │ │
│  │  Example resolution chain:                                        │ │
│  │  species.json →                                                   │ │
│  │    1. /Mods/CompetitiveMod/Data/species.json  [Priority 1]       │ │
│  │    2. /Mods/RebalanceMod/Data/species.json    [Priority 2]       │ │
│  │    3. /Data/species.json                      [Base game]        │ │
│  │                                                                    │ │
│  └────────────────────────┬──────────────────────────────────────────┘ │
│                           │                                            │
│                           ▼                                            │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │                    JSON Deserialization                           │ │
│  ├──────────────────────────────────────────────────────────────────┤ │
│  │                                                                    │ │
│  │  LoadJsonArrayAsync<T>(string filename)                           │ │
│  │  ├─ path = ResolveDataPath(filename)                             │ │
│  │  ├─ json = await File.ReadAllTextAsync(path)                     │ │
│  │  └─ return JsonSerializer.Deserialize<List<T>>(json)             │ │
│  │                                                                    │ │
│  │  JsonSerializerOptions:                                           │ │
│  │  - PropertyNameCaseInsensitive = true                             │ │
│  │  - ReadCommentHandling = Skip                                     │ │
│  │  - AllowTrailingCommas = true                                     │ │
│  │                                                                    │ │
│  └────────────────────────┬──────────────────────────────────────────┘ │
│                           │                                            │
│                           ▼                                            │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │                    In-Memory Cache Layer                          │ │
│  ├──────────────────────────────────────────────────────────────────┤ │
│  │                                                                    │ │
│  │  Thread-safe dictionaries (read-only after load):                 │ │
│  │                                                                    │ │
│  │  _speciesById:       Dictionary<int, SpeciesData>                 │ │
│  │  _speciesByName:     Dictionary<string, SpeciesData>              │ │
│  │  _movesByName:       Dictionary<string, MoveData>                 │ │
│  │  _itemsById:         Dictionary<int, ItemData>                    │ │
│  │  _itemsByName:       Dictionary<string, ItemData>                 │ │
│  │  _encountersById:    Dictionary<string, EncounterTable>           │ │
│  │  _typeChart:         TypeChart                                    │ │
│  │                                                                    │ │
│  │  Access pattern: O(1) dictionary lookup                           │ │
│  │  Memory: ~3 MB total for all game data                            │ │
│  │                                                                    │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Level 4: Code Diagram - Type Effectiveness Calculation

```
┌────────────────────────────────────────────────────────────────────────┐
│                    Type Effectiveness Calculation Flow                  │
└────────────────────────────────────────────────────────────────────────┘

BattleSystem.ExecuteMoveAsync()
    │
    ├─ Get move data
    │  └─ moveData = await _dataApi.GetMoveAsync("Thunderbolt")
    │
    ├─ Get attacker/defender types
    │  ├─ attackerData = attacker.Get<PokemonData>()
    │  └─ defenderData = defender.Get<PokemonData>()
    │
    ├─ Parse types
    │  ├─ attackType = PokemonTypeExtensions.ParseType(moveData.Type)
    │  ├─ defenseType1 = PokemonTypeExtensions.ParseType(defenderData.Types[0])
    │  └─ defenseType2 = PokemonTypeExtensions.ParseType(defenderData.Types[1])
    │
    ├─ Calculate type effectiveness
    │  └─ effectiveness = await _dataApi.GetTypeEffectivenessAsync(
    │         attackType, defenseType1, defenseType2)
    │
    └─ Apply damage multiplier
       └─ damage = baseDamage * effectiveness

┌────────────────────────────────────────────────────────────────────────┐
│            DataManager.GetTypeEffectivenessAsync()                      │
└────────────────────────────────────────────────────────────────────────┘

GetTypeEffectivenessAsync(PokemonType attack, PokemonType defense1,
                          PokemonType? defense2)
    │
    ├─ Ensure data loaded
    │  └─ await EnsureDataLoadedAsync()
    │
    ├─ Get type chart
    │  └─ typeChart = _typeChart
    │
    └─ Calculate effectiveness
       └─ return typeChart.GetDualTypeEffectiveness(attack, defense1, defense2)

┌────────────────────────────────────────────────────────────────────────┐
│              TypeChart.GetDualTypeEffectiveness()                       │
└────────────────────────────────────────────────────────────────────────┘

GetDualTypeEffectiveness(PokemonType attack, PokemonType def1,
                         PokemonType? def2)
    │
    ├─ Look up first type matchup
    │  └─ eff1 = _effectiveness[(attack, def1)] ?? 1.0
    │
    ├─ If single-type, return
    │  └─ if (def2 == null) return eff1
    │
    ├─ Look up second type matchup
    │  └─ eff2 = _effectiveness[(attack, def2.Value)] ?? 1.0
    │
    └─ Multiply effectiveness
       └─ return eff1 * eff2

Example: Thunderbolt (Electric) vs Gyarados (Water/Flying)
    eff1 = _effectiveness[(Electric, Water)]   = 2.0   (super effective)
    eff2 = _effectiveness[(Electric, Flying)]  = 2.0   (super effective)
    result = 2.0 * 2.0 = 4.0                          (quadruple damage!)
```

---

## Sequence Diagram - First Data Access (Lazy Loading)

```
Thread A          DataManager       SemaphoreSlim      FileSystem      Cache
   │                   │                  │                │            │
   │──GetSpeciesAsync(1)──────────────────▶│                │            │
   │                   │                  │                │            │
   │                   ├─ EnsureDataLoaded()────────────────▶│            │
   │                   │                  │                │            │
   │                   │   _isLoaded?     │                │            │
   │                   │   (false)        │                │            │
   │                   │                  │                │            │
   │                   ├────WaitAsync()───▶│                │            │
   │                   │                  ├─[ACQUIRE LOCK] │            │
   │                   │◀─────────────────┤                │            │
   │                   │                  │                │            │
   │                   ├─ LoadAllDataAsync()──────────────▶│            │
   │                   │                  │                │            │
   │                   │                  │    species.json│            │
   │                   │◀──────────────────────────────────┤            │
   │                   │                  │                │            │
   │                   ├─ Parse JSON      │                │            │
   │                   │                  │                │            │
   │                   ├─ Build dictionary│                │            │
   │                   │                  │                │            │
   │                   ├────────────────────────────────────────────────▶│
   │                   │                  │                │   _speciesById[1]
   │                   │                  │                │            │
   │                   ├─ _isLoaded = true│                │            │
   │                   │                  │                │            │
   │                   ├────Release()─────▶│                │            │
   │                   │                  └─[RELEASE LOCK] │            │
   │                   │                                   │            │
   │                   ├───────────────────────────────────────────────▶│
   │                   │   Lookup: _speciesById[1]         │   Bulbasaur│
   │                   │◀───────────────────────────────────────────────┤
   │                   │                                                │
   │◀──return SpeciesData(Bulbasaur)─────────────────────────────────────│
   │                                                                    │


Thread B (concurrent)
   │──GetMoveAsync("Tackle")──────────────────▶DataManager
   │                   │                      │
   │                   ├─ EnsureDataLoaded()  │
   │                   │                      │
   │                   │   _isLoaded?         │
   │                   │   (true - already loaded by Thread A)
   │                   │                      │
   │                   ├───────────────────────────────────▶Cache
   │                   │   Lookup: _movesByName["tackle"]  │
   │                   │◀──────────────────────────────────┤
   │                   │                                   │
   │◀──return MoveData(Tackle)──────────────────────────────
```

---

## Data Flow Diagram - Mod Override Resolution

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Mod Override Resolution Flow                      │
└─────────────────────────────────────────────────────────────────────┘

ModLoader
    │
    ├─ Load mod metadata (priority order)
    │  ├─ Mod 1: CompetitiveMod (Priority: 10)
    │  ├─ Mod 2: RebalanceMod (Priority: 5)
    │  └─ Mod 3: SpritePackMod (Priority: 1)
    │
    ├─ Build mod path list (sorted by priority)
    │  └─ ["/Mods/CompetitiveMod/Data",
    │      "/Mods/RebalanceMod/Data",
    │      "/Mods/SpritePackMod/Data"]
    │
    └─▶ dataManager.SetModDataPaths(modPaths)

DataManager.SetModDataPaths()
    │
    ├─ _modDataPaths = [mod1, mod2, mod3]
    └─ ReloadDataAsync()

DataManager.ResolveDataPath("species.json")
    │
    ├─ Check: /Mods/CompetitiveMod/Data/species.json
    │  └─ EXISTS ✓ → Return this path (mod wins!)
    │
    └─ [Not reached] /Data/species.json (base game)

DataManager.ResolveDataPath("items.json")
    │
    ├─ Check: /Mods/CompetitiveMod/Data/items.json
    │  └─ NOT FOUND ✗
    │
    ├─ Check: /Mods/RebalanceMod/Data/items.json
    │  └─ EXISTS ✓ → Return this path
    │
    └─ [Not reached] /Data/items.json

DataManager.ResolveDataPath("encounters.json")
    │
    ├─ Check: /Mods/CompetitiveMod/Data/encounters.json
    │  └─ NOT FOUND ✗
    │
    ├─ Check: /Mods/RebalanceMod/Data/encounters.json
    │  └─ NOT FOUND ✗
    │
    ├─ Check: /Mods/SpritePackMod/Data/encounters.json
    │  └─ NOT FOUND ✗
    │
    └─ Fallback: /Data/encounters.json ✓ (base game)

Result:
    species.json   ← CompetitiveMod (highest priority)
    items.json     ← RebalanceMod (second priority)
    encounters.json← Base game (no mod override)
```

---

## Deployment Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                     File System Layout                               │
└─────────────────────────────────────────────────────────────────────┘

/PokeNET/
│
├── Data/                               [Base game data]
│   ├── species.json                    (1025 Pokemon, ~2 MB)
│   ├── moves.json                      (919 moves, ~450 KB)
│   ├── items.json                      (800 items, ~240 KB)
│   ├── encounters.json                 (200 locations, ~200 KB)
│   └── typechart.json                  (Optional, ~2 KB)
│
├── Mods/                               [User-installed mods]
│   │
│   ├── CompetitiveMod/
│   │   ├── mod.json                    (Mod metadata: priority=10)
│   │   └── Data/
│   │       ├── species.json            [Overrides base game]
│   │       └── moves.json              [Overrides base game]
│   │
│   ├── RebalanceMod/
│   │   ├── mod.json                    (Mod metadata: priority=5)
│   │   └── Data/
│   │       └── items.json              [Overrides base game]
│   │
│   └── SpritePackMod/
│       ├── mod.json                    (Mod metadata: priority=1)
│       └── Assets/
│           └── sprites/                [No data overrides]
│
├── PokeNET.Core/                       [Core engine]
│   └── Data/
│       ├── IDataApi.cs                 (Interface)
│       ├── DataManager.cs              (Implementation)
│       ├── SpeciesData.cs              (Model)
│       ├── MoveData.cs                 (Model)
│       ├── ItemData.cs                 (Model)
│       ├── EncounterTable.cs           (Model)
│       ├── TypeChart.cs                (Type effectiveness)
│       └── PokemonType.cs              (Type enum)
│
└── PokeNET.WindowsDX/                  [Executable]
    └── bin/Debug/net8.0/
        └── PokeNET.exe

Memory Layout (Runtime):
┌─────────────────────────────────────────────────────────────────┐
│                      Process Memory                              │
├─────────────────────────────────────────────────────────────────┤
│  DataManager Instance                                            │
│  ├── _speciesById:     ~2 MB    (1025 SpeciesData objects)     │
│  ├── _speciesByName:   ~2 MB    (same instances, diff index)   │
│  ├── _movesByName:     ~450 KB  (919 MoveData objects)         │
│  ├── _itemsById:       ~240 KB  (800 ItemData objects)         │
│  ├── _itemsByName:     ~240 KB  (same instances, diff index)   │
│  ├── _encountersById:  ~200 KB  (200 EncounterTable objects)   │
│  └── _typeChart:       ~2 KB    (18×18 effectiveness matrix)   │
│                                                                  │
│  Total: ~3 MB (negligible on modern hardware)                   │
└─────────────────────────────────────────────────────────────────┘
```

---

**Next Steps:**
1. Review diagrams with development team
2. Validate against actual implementation
3. Update diagrams as architecture evolves
4. Create PlantUML versions for automated rendering
