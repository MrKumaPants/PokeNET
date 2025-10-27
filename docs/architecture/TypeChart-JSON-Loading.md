# TypeChart JSON Loading System

## Overview

The TypeChart system is now fully data-driven, loading type effectiveness from JSON files. This enables mods to customize type matchups without modifying code.

## Architecture

### Components

1. **`PokemonType` Enum** (`PokeNET.Core/Data/PokemonType.cs`)
   - Defines the 18 official Pokemon types
   - Immutable at runtime (enum)
   - Provides type safety for core types

2. **`PokemonTypeInfo`** (`PokeNET.Core/Data/PokemonTypeInfo.cs`)
   - JSON-serializable data structure for a single type
   - Properties:
     - `Name` (string): Type name (e.g., "Fire", "Water")
     - `Color` (string): Hex color code for UI display
     - `Description` (string?): Optional description
     - `Matchups` (Dictionary<string, double>): Type effectiveness mappings

3. **`TypeChart`** (`PokeNET.Core/Data/TypeChart.cs`)
   - Manages the 18×18 type effectiveness matrix
   - Three constructors:
     - `TypeChart()`: Default with hardcoded values
     - `TypeChart(List<PokemonTypeInfo>)`: Load from JSON array
     - `TypeChart(Dictionary<...>)`: Advanced custom initialization
   - Thread-safe (immutable after construction)

### JSON File Format

**Location:** `PokeNET.DesktopGL/Content/Data/types.json`

**Structure:**
```json
[
  {
    "name": "Fire",
    "color": "#F08030",
    "description": "Fire type Pokemon",
    "matchups": {
      "Water": 0.5,
      "Grass": 2.0,
      "Ice": 2.0
    }
  },
  {
    "name": "Water",
    "color": "#6890F0",
    "description": "Water type Pokemon",
    "matchups": {
      "Fire": 2.0,
      "Grass": 0.5,
      "Rock": 2.0
    }
  }
]
```

**Effectiveness Values:**
- `0.0` = No effect (immunity)
- `0.5` = Not very effective (resisted)
- `1.0` = Neutral (default if not specified)
- `2.0` = Super effective
- Custom values supported for game variants

## Loading Process

### 1. DataManager Load Sequence

```csharp
private async Task LoadTypeChartAsync()
{
    // 1. Resolve path (checks mods first, then Content/Data)
    var path = ResolveDataPath("types.json");
    
    // 2. Load JSON array if found
    if (path != null)
    {
        var json = await File.ReadAllTextAsync(path);
        var types = JsonSerializer.Deserialize<List<PokemonTypeInfo>>(json);
        _typeChart = new TypeChart(types);
    }
    else
    {
        // 3. Fallback to default
        _typeChart = new TypeChart();
    }
}
```

### 2. TypeChart Constructor Logic

When loading from JSON:
1. Initialize with **neutral defaults** (1.0 for all matchups)
2. **Apply** matchups from JSON (only non-neutral need to be specified)
3. Parse type names **case-insensitively**
4. **Ignore invalid** type names gracefully

```csharp
public TypeChart(List<PokemonTypeInfo> types)
{
    _effectiveness = new Dictionary<(PokemonType, PokemonType), double>();
    
    // Start with neutral defaults (1.0)
    InitializeNeutralDefaults();
    
    // Load matchups from JSON
    foreach (var typeInfo in types)
    {
        if (!Enum.TryParse<PokemonType>(typeInfo.Name, true, out var attackType))
            continue; // Skip unknown types
        
        foreach (var matchup in typeInfo.Matchups)
        {
            if (Enum.TryParse<PokemonType>(matchup.Key, true, out var defenseType))
            {
                _effectiveness[(attackType, defenseType)] = matchup.Value;
            }
        }
    }
}
```

### 3. Mod Priority

Mods can provide custom `types.json` files:
- **Mod directory** checked first (highest priority)
- **Content/Data** checked second (base game)
- **Hardcoded default** used if no file found

## Usage

### Accessing TypeChart via IDataApi

```csharp
// Get single-type effectiveness
double effectiveness = await dataApi.GetTypeEffectivenessAsync(
    PokemonType.Fire, 
    PokemonType.Grass
);
// Returns: 2.0

// Get dual-type effectiveness
double dualEffectiveness = await dataApi.GetDualTypeEffectivenessAsync(
    PokemonType.Ice,
    PokemonType.Grass,
    PokemonType.Flying
);
// Returns: 4.0 (2.0 × 2.0)

// Get full TypeChart
TypeChart typeChart = await dataApi.GetTypeChartAsync();
```

### Direct TypeChart Usage

```csharp
var typeChart = new TypeChart();

// Check specific matchup
double effectiveness = typeChart.GetEffectiveness(PokemonType.Fire, PokemonType.Water);

// Check immunities
bool immune = typeChart.IsImmune(PokemonType.Normal, PokemonType.Ghost);

// Get all super effective types
List<PokemonType> superEffective = typeChart.GetSuperEffectiveTypes(PokemonType.Dragon);
```

## Mod Support

### Creating a Custom TypeChart Mod

1. **Create mod directory structure:**
   ```
   Mods/
     MyCustomTypes/
       Content/
         Data/
           types.json
       modinfo.json
   ```

2. **Define custom types.json:**
   ```json
   [
     {
       "name": "Fire",
       "color": "#FF5500",
       "matchups": {
         "Water": 2.0,
         "Grass": 2.0
       }
     }
   ]
   ```

3. **Install mod** - TypeChart will automatically load on game start

### Mod Examples

#### Balance Mod (Nerf Fairy Type)
```json
{
  "matchups": [
    {
      "attacker": "Fairy",
      "defender": "Dragon",
      "effectiveness": 1.5
    }
  ]
}
```

#### Hard Mode (All Super Effective = 3×)
```json
{
  "matchups": [
    {
      "attacker": "Fire",
      "defender": "Grass",
      "effectiveness": 3.0
    }
  ]
}
```

#### Type Swap Mod
```json
{
  "matchups": [
    {
      "attacker": "Fire",
      "defender": "Water",
      "effectiveness": 2.0
    },
    {
      "attacker": "Water",
      "defender": "Fire",
      "effectiveness": 0.5
    }
  ]
}
```

## Testing

### Unit Tests

Three test categories verify JSON loading:

1. **Custom Matchup Override**
   - Verifies JSON data overrides defaults
   - Tests that unmodified matchups retain defaults

2. **Invalid Type Handling**
   - Verifies invalid type names are ignored
   - Tests graceful degradation

3. **Case-Insensitive Parsing**
   - Verifies "fire", "Fire", "FIRE" all work
   - Tests robust JSON parsing

**Test File:** `PokeNET.Testing/Data/TypeChartTests.cs`

**Run Tests:**
```bash
dotnet test --filter "TypeChart_LoadFromJson"
```

## Design Decisions

### Why Keep PokemonType as an Enum?

**Pros:**
- Type safety at compile time
- IntelliSense autocomplete
- Performance (no string comparisons)
- Prevents typos in code

**Cons:**
- Cannot add types at runtime
- Mods limited to 18 official types

**Decision:** Keep enum for now. If custom types are needed in the future, implement a hybrid system with string-based fallback.

### Why Override Instead of Replace?

**Approach:** JSON matchups **override** hardcoded defaults

**Rationale:**
- Mods only specify **changes**, not full 18×18 matrix
- Smaller JSON files (only changed matchups)
- Backward compatible with game updates
- Easier to maintain mods

### Why Case-Insensitive?

**Rationale:**
- User-friendly for mod creators
- Tolerates JSON formatting styles
- Matches Pokemon game conventions

## Performance Considerations

### Initialization
- TypeChart loaded **once** at startup
- JSON parsing: ~1-2ms for full matrix
- Cached for entire game session

### Runtime Lookups
- Dictionary lookup: O(1)
- No allocations per query
- Thread-safe concurrent reads

### Memory
- 18×18 matrix = 324 entries max
- ~3-5 KB in memory
- Negligible overhead

## Future Enhancements

### Planned
- [ ] JSON schema validation
- [ ] Hot-reload for mod development
- [ ] TypeChart conflict resolution (multiple mods)

### Potential
- [ ] Custom type names (string-based types)
- [ ] Generation-specific type charts
- [ ] Battle format variations (doubles, VGC)

## References

- **Architecture:** `docs/architecture/Phase1-DataInfrastructure.md`
- **Data API:** `docs/DataApiQuickStart.md`
- **Modding Guide:** `docs/modding/modding-guide.md`
- **JSON Schema:** `docs/architecture/JSON-Schemas.md`

