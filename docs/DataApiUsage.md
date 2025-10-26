# Data API Usage Guide

## Overview

The Data API (`IDataApi`) provides read-only access to static Pokemon game data including species, moves, items, and encounters. It supports JSON-based data loading with automatic caching and mod override capabilities.

## Architecture

```
PokeNET.Domain/Data/
  ├── IDataApi.cs              # Interface (domain layer)
  ├── SpeciesData.cs           # Species model
  ├── MoveData.cs              # Move model
  ├── ItemData.cs              # Item model
  └── EncounterTable.cs        # Encounter model

PokeNET.Core/Data/
  ├── DataManager.cs           # Implementation
  └── DataServiceExtensions.cs # DI registration
```

## Dependency Injection Setup

### Basic Setup

```csharp
using PokeNET.Core.Data;

// In Program.cs or Startup.cs
services.AddDataServices(); // Uses default path: "Content/Data"
```

### Custom Data Path

```csharp
services.AddDataServices("MyGame/GameData");
```

### With Mod Support

```csharp
services.AddDataServices("Content/Data")
    .ConfigureModDataPaths(
        "Mods/SuperMod/Data",
        "Mods/CoolMod/Data"
    );
```

## Data File Structure

The DataManager expects JSON files in the data directory:

```
Content/Data/
  ├── species.json    # Array of SpeciesData
  ├── moves.json      # Array of MoveData
  ├── items.json      # Array of ItemData
  └── encounters.json # Array of EncounterTable
```

### Example: species.json

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
    "levelMoves": [
      { "level": 1, "moveName": "Tackle" },
      { "level": 3, "moveName": "Growl" },
      { "level": 7, "moveName": "Vine Whip" }
    ],
    "evolutions": [
      {
        "targetSpeciesId": 2,
        "method": "Level",
        "requiredLevel": 16
      }
    ]
  }
]
```

### Example: moves.json

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
    "description": "A physical attack in which the user charges and slams into the target.",
    "flags": ["Contact"]
  },
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
    "description": "A strong electric blast crashes down on the target.",
    "effect": {
      "effectType": "StatusCondition",
      "effectTarget": "Target",
      "statusCondition": "Paralysis"
    }
  }
]
```

## Usage Examples

### Injecting IDataApi

```csharp
public class BattleSystem
{
    private readonly IDataApi _dataApi;

    public BattleSystem(IDataApi dataApi)
    {
        _dataApi = dataApi;
    }

    public async Task InitializePokemon(int speciesId, int level)
    {
        var speciesData = await _dataApi.GetSpeciesAsync(speciesId);

        if (speciesData == null)
        {
            throw new InvalidOperationException($"Species {speciesId} not found");
        }

        // Use species data to create Pokemon...
        var hp = CalculateHP(speciesData.BaseStats.HP, level);
    }
}
```

### Loading Species Data

```csharp
// By ID
var bulbasaur = await dataApi.GetSpeciesAsync(1);
Console.WriteLine($"{bulbasaur.Name} - {string.Join("/", bulbasaur.Types)}");

// By name (case-insensitive)
var charmander = await dataApi.GetSpeciesByNameAsync("CHARMANDER");

// All species
var allSpecies = await dataApi.GetAllSpeciesAsync();
foreach (var species in allSpecies)
{
    Console.WriteLine($"#{species.Id}: {species.Name}");
}
```

### Loading Move Data

```csharp
// By name
var tackle = await dataApi.GetMoveAsync("Tackle");
Console.WriteLine($"{tackle.Name}: {tackle.Power} power, {tackle.PP} PP");

// By type
var fireMoves = await dataApi.GetMovesByTypeAsync("Fire");

// All moves
var allMoves = await dataApi.GetAllMovesAsync();
```

### Loading Item Data

```csharp
// By ID
var potion = await dataApi.GetItemAsync(1);

// By name
var pokeball = await dataApi.GetItemByNameAsync("Pokeball");

// By category
var medicines = await dataApi.GetItemsByCategoryAsync(ItemCategory.Medicine);
```

### Loading Encounter Data

```csharp
// By location
var route1 = await dataApi.GetEncountersAsync("route_1");

foreach (var encounter in route1.GrassEncounters)
{
    var species = await dataApi.GetSpeciesAsync(encounter.SpeciesId);
    Console.WriteLine($"{species.Name} - Lv.{encounter.MinLevel}-{encounter.MaxLevel} ({encounter.Rate}%)");
}
```

## Mod Support

### How Mod Overrides Work

1. DataManager checks mod paths **first** (in order of priority)
2. If a file exists in a mod path, it's used instead of the base file
3. Mods can override individual files (e.g., only `species.json`)
4. First mod in the list has highest priority

### Example Mod Structure

```
Mods/SuperMod/
  └── Data/
      ├── species.json    # Overrides base species
      └── moves.json      # Overrides base moves

Mods/BalanceMod/
  └── Data/
      └── moves.json      # Only overrides moves
```

### Configuring Mod Priority

```csharp
// SuperMod has priority over BalanceMod
services.AddDataServices()
    .ConfigureModDataPaths(
        "Mods/SuperMod/Data",     // Checked first
        "Mods/BalanceMod/Data"    // Checked second
    );
```

### Reloading Data After Mod Changes

```csharp
// When mods are loaded/unloaded at runtime
await dataApi.ReloadDataAsync();
```

## Performance Considerations

### Caching

- All data is loaded on first access
- Subsequent calls use in-memory cache (no disk I/O)
- Cache is thread-safe
- Reload only when mods change

### Async/Await

- All methods are async to support future database backends
- Current implementation loads from disk asynchronously
- Use `await` for all API calls

### Memory Usage

- All data loaded into memory on first access
- ~1-5 MB for typical Pokemon game data
- Caches persist until `ReloadDataAsync()` or `Dispose()`

## Thread Safety

DataManager is **fully thread-safe**:

```csharp
// Safe to call from multiple threads
var tasks = Enumerable.Range(1, 100)
    .Select(id => dataApi.GetSpeciesAsync(id % 10));

var results = await Task.WhenAll(tasks);
```

## Error Handling

### Missing Files

- Returns empty collections if JSON files don't exist
- Logs warnings for missing files
- No exceptions thrown

### Invalid JSON

- Logs errors and returns empty data
- Application continues running

### Missing Data

- Returns `null` for specific lookups (e.g., invalid species ID)
- Always check for null:

```csharp
var species = await dataApi.GetSpeciesAsync(999999);
if (species == null)
{
    // Handle missing species
}
```

## Testing

### Unit Testing with Test Data

```csharp
[Fact]
public async Task LoadTestData()
{
    var testDataPath = Path.Combine(Path.GetTempPath(), "TestData");
    Directory.CreateDirectory(testDataPath);

    // Create test JSON files...

    var dataManager = new DataManager(logger, testDataPath);
    var species = await dataManager.GetSpeciesAsync(1);

    Assert.NotNull(species);
}
```

### Integration Testing

```csharp
public class DataIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IDataApi _dataApi;

    public DataIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _dataApi = factory.Services.GetRequiredService<IDataApi>();
    }

    [Fact]
    public async Task RealDataLoads()
    {
        var bulbasaur = await _dataApi.GetSpeciesAsync(1);
        Assert.Equal("Bulbasaur", bulbasaur.Name);
    }
}
```

## Best Practices

### ✅ DO

- Inject `IDataApi` via constructor
- Check for null on specific lookups
- Use async/await consistently
- Cache species/move/item objects in components when needed
- Reload data when mods change

### ❌ DON'T

- Don't create multiple DataManager instances (use DI)
- Don't modify returned data objects (they're shared)
- Don't call `ReloadDataAsync()` frequently (cache is efficient)
- Don't block on async calls with `.Result`

## Migration Guide

### From Direct JSON Loading

**Before:**
```csharp
var json = File.ReadAllText("species.json");
var species = JsonSerializer.Deserialize<List<SpeciesData>>(json);
```

**After:**
```csharp
var species = await _dataApi.GetAllSpeciesAsync();
```

### From Database

**Before:**
```csharp
var species = await dbContext.Species.FindAsync(id);
```

**After:**
```csharp
var species = await _dataApi.GetSpeciesAsync(id);
```

## Future Enhancements

Planned features:

- [ ] Database backend support (SQLite, PostgreSQL)
- [ ] Hot-reloading when files change
- [ ] Query filters and pagination
- [ ] Binary format support for faster loading
- [ ] Data validation and schema enforcement
- [ ] Partial mod support (merge instead of replace)

## Troubleshooting

### "Data file not found"

- Check that JSON files exist in the data path
- Verify path is correct in DI registration
- Check file permissions

### "No data loading"

- Ensure `AddDataServices()` is called before building service provider
- Check logger output for errors
- Verify JSON file syntax

### "Mod data not loading"

- Ensure mod paths are set **before** first data access
- Check mod path exists and has read permissions
- Use `ReloadDataAsync()` after setting mod paths

## Support

For issues or questions:
- Check logs for detailed error messages
- Review JSON file syntax
- Verify DI configuration
- Test with minimal data set

---

**Next Steps:**
- [Save System Integration](SaveSystemIntegration.md)
- [Mod Development Guide](ModDevelopment.md)
- [Performance Optimization](PerformanceGuide.md)
