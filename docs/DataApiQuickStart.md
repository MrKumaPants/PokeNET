# Data API Quick Start

## 5-Minute Integration Guide

### Step 1: Register Services (Program.cs)

```csharp
using PokeNET.Core.Data;

var builder = WebApplication.CreateBuilder(args);

// Add Data API services
builder.Services.AddDataServices("Content/Data");

// Optional: Enable mod support
builder.Services.ConfigureModDataPaths(
    "Mods/Mod1/Data",
    "Mods/Mod2/Data"
);

var app = builder.Build();
```

### Step 2: Create Data Files

Create `Content/Data/species.json`:

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
    "growthRate": "Medium Slow",
    "catchRate": 45
  }
]
```

Create empty files for: `moves.json`, `items.json`, `encounters.json`

### Step 3: Use in Your Code

```csharp
public class PokemonService
{
    private readonly IDataApi _dataApi;

    public PokemonService(IDataApi dataApi)
    {
        _dataApi = dataApi;
    }

    public async Task<Pokemon> CreatePokemon(int speciesId, int level)
    {
        var speciesData = await _dataApi.GetSpeciesAsync(speciesId);

        if (speciesData == null)
            throw new ArgumentException($"Species {speciesId} not found");

        return new Pokemon
        {
            SpeciesId = speciesId,
            Name = speciesData.Name,
            Level = level,
            HP = CalculateHP(speciesData.BaseStats.HP, level)
        };
    }

    private int CalculateHP(int baseHP, int level)
    {
        return ((2 * baseHP + 31) * level / 100) + level + 10;
    }
}
```

### Step 4: Test It

```csharp
// In your test or controller
var pokemon = await pokemonService.CreatePokemon(1, 5);
Console.WriteLine($"Created {pokemon.Name} at level {pokemon.Level}");
```

## Common Patterns

### Loading Species with Moves

```csharp
var species = await _dataApi.GetSpeciesAsync(1);

foreach (var levelMove in species.LevelMoves)
{
    var moveData = await _dataApi.GetMoveAsync(levelMove.MoveName);
    if (moveData != null)
    {
        Console.WriteLine($"Lv.{levelMove.Level}: {moveData.Name} ({moveData.Power} power)");
    }
}
```

### Wild Encounter Generation

```csharp
var encounters = await _dataApi.GetEncountersAsync("route_1");

if (encounters != null)
{
    // Pick random grass encounter
    var encounter = encounters.GrassEncounters[Random.Shared.Next(encounters.GrassEncounters.Count)];
    var level = Random.Shared.Next(encounter.MinLevel, encounter.MaxLevel + 1);

    var pokemon = await CreatePokemon(encounter.SpeciesId, level);
}
```

### Item Usage

```csharp
var potion = await _dataApi.GetItemByNameAsync("Potion");

if (potion?.Effect?.HealAmount is int healAmount)
{
    pokemon.HP = Math.Min(pokemon.HP + healAmount, pokemon.MaxHP);
}
```

## Directory Structure

```
YourProject/
├── Content/
│   └── Data/
│       ├── species.json      # Required
│       ├── moves.json        # Required
│       ├── items.json        # Required
│       └── encounters.json   # Required
├── Mods/                     # Optional
│   ├── Mod1/
│   │   └── Data/
│   │       └── species.json  # Override
│   └── Mod2/
│       └── Data/
│           └── moves.json    # Override
└── Program.cs
```

## Troubleshooting

**"Data file not found"**
- Ensure JSON files exist in `Content/Data/`
- Check file names are exactly: `species.json`, `moves.json`, `items.json`, `encounters.json`

**"Empty collections returned"**
- Check JSON syntax (use [jsonlint.com](https://jsonlint.com))
- Ensure arrays are wrapped in `[]`
- Check logs for parsing errors

**"Null reference exception"**
- Always check if returned data is null: `if (species != null)`
- Species/Item IDs are 1-indexed (not 0)

## Next Steps

- [Full API Documentation](DataApiUsage.md)
- [Data Model Reference](DataModels.md)
- [Mod Development Guide](ModDevelopment.md)
