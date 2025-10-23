# JsonAssetLoader Integration Guide

## Overview

The `JsonAssetLoader<T>` is a production-ready asset loader for the PokeNET asset management system. It provides high-performance JSON deserialization with comprehensive error handling, caching, and streaming support for large files.

## Features

- **Generic Type Support**: Load JSON into any C# type
- **High Performance**: Uses System.Text.Json for fast deserialization
- **Streaming**: Automatic streaming for large files (>1MB by default)
- **Caching**: Built-in caching to avoid redundant file reads
- **Error Handling**: Detailed error messages with line numbers for malformed JSON
- **JSON Comments**: Supports comments in JSON files
- **Thread-Safe**: Safe for concurrent access
- **Type Validation**: Validates JSON structure matches expected type

## Installation & Registration

### 1. Register with AssetManager

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PokeNET.Core.Assets;
using PokeNET.Core.Assets.Loaders;

// In your DI container setup
services.AddSingleton<IAssetManager>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<AssetManager>>();
    var basePath = "Content/Assets"; // Your asset base path
    var assetManager = new AssetManager(logger, basePath);

    // Register JsonAssetLoader for Pokemon data
    var jsonLogger = sp.GetRequiredService<ILogger<JsonAssetLoader<PokemonData>>>();
    var jsonLoader = new JsonAssetLoader<PokemonData>(jsonLogger);
    assetManager.RegisterLoader(jsonLoader);

    return assetManager;
});
```

### 2. Basic Usage

```csharp
public class PokemonData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Types { get; set; } = new();
    public Stats BaseStats { get; set; } = new();
}

public class Stats
{
    public int HP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
}

// Load a Pokemon from JSON
var pokemon = assetManager.Load<PokemonData>("pokemon/pikachu.json");
Console.WriteLine($"Loaded {pokemon.Name} (#{pokemon.Id})");
```

### 3. Example JSON File

```json
{
    // Pokemon data for Pikachu
    "id": 25,
    "name": "Pikachu",
    "types": ["Electric"],
    "baseStats": {
        "hp": 35,
        "attack": 55,
        "defense": 40,
        "specialAttack": 50,
        "specialDefense": 50,
        "speed": 90
    },
    // Trailing commas are supported
}
```

## Advanced Features

### Custom Streaming Threshold

For memory-constrained environments or when dealing with many large JSON files:

```csharp
// Set streaming threshold to 500KB instead of default 1MB
var loader = new JsonAssetLoader<LargeDataSet>(
    logger,
    streamingThreshold: 512_000
);
```

### Cache Management

```csharp
var loader = new JsonAssetLoader<PokemonData>(logger);

// Check if a file is cached
if (loader.IsCached("pokemon/pikachu.json"))
{
    Console.WriteLine("Already loaded!");
}

// Get current cache size
Console.WriteLine($"Cache contains {loader.CacheSize} items");

// Clear cache to free memory
loader.ClearCache();
```

### Loading Arrays/Collections

```csharp
// Register loader for list type
var listLoader = new JsonAssetLoader<List<PokemonData>>(logger);
assetManager.RegisterLoader(listLoader);

// Load array of Pokemon
var allPokemon = assetManager.Load<List<PokemonData>>("pokemon/generation1.json");
```

### Error Handling

The loader provides detailed error information:

```csharp
try
{
    var data = assetManager.Load<PokemonData>("pokemon/invalid.json");
}
catch (AssetLoadException ex)
{
    Console.WriteLine($"Failed to load: {ex.AssetPath}");
    Console.WriteLine($"Error: {ex.Message}");

    if (ex.InnerException is JsonException jsonEx)
    {
        Console.WriteLine($"JSON error at line {jsonEx.LineNumber}, " +
                         $"position {jsonEx.BytePositionInLine}");
    }
}
```

## Integration with Mod System

The AssetManager automatically handles mod path resolution:

```csharp
// Set mod paths (highest priority first)
assetManager.SetModPaths(new[]
{
    "Mods/CustomPokemon/Assets",
    "Mods/EnhancedGraphics/Assets"
});

// This will check mod paths first, then fall back to base assets
var pokemon = assetManager.Load<PokemonData>("pokemon/pikachu.json");
// Loads from mod if available, otherwise from base game
```

## Performance Characteristics

| Feature | Performance |
|---------|-------------|
| Small files (<1MB) | Direct deserialization, very fast |
| Large files (>1MB) | Streaming deserialization, memory-efficient |
| Cached files | Instant return (same instance) |
| Concurrent access | Thread-safe with lock-based caching |
| Type validation | Minimal overhead, only on first load |

## Configuration Examples

### Game Configuration Loader

```csharp
public class GameConfig
{
    public WindowSettings Window { get; set; }
    public AudioSettings Audio { get; set; }
    public Dictionary<string, object> Custom { get; set; }
}

// Register and load
var configLoader = new JsonAssetLoader<GameConfig>(logger);
assetManager.RegisterLoader(configLoader);
var config = assetManager.Load<GameConfig>("config/game.json");
```

### Item Database Loader

```csharp
public class ItemDatabase
{
    public List<Item> Items { get; set; }
    public Dictionary<string, ItemCategory> Categories { get; set; }
}

var itemDbLoader = new JsonAssetLoader<ItemDatabase>(logger);
assetManager.RegisterLoader(itemDbLoader);
var items = assetManager.Load<ItemDatabase>("data/items.json");
```

### Localization Files

```csharp
public class LocalizationData
{
    public string Language { get; set; }
    public Dictionary<string, string> Strings { get; set; }
}

var locLoader = new JsonAssetLoader<LocalizationData>(logger);
assetManager.RegisterLoader(locLoader);
var english = assetManager.Load<LocalizationData>("localization/en-US.json");
```

## Best Practices

1. **Type Safety**: Always use strongly-typed classes instead of `Dictionary<string, object>`
2. **Caching**: Let the loader handle caching - don't implement your own cache on top
3. **Error Handling**: Always wrap `Load()` calls in try-catch for production code
4. **Memory Management**: For large datasets, consider clearing cache periodically
5. **Mod Support**: Always use relative paths for mod compatibility
6. **Validation**: Add DataAnnotations to your model classes for extra validation

## Testing

The JsonAssetLoader includes comprehensive tests covering:
- Valid JSON loading (simple, nested, arrays, comments)
- Caching behavior
- Error handling (malformed JSON, missing files, type mismatches)
- Streaming for large files
- Thread-safe concurrent access

Run tests with:
```bash
dotnet test --filter "FullyQualifiedName~JsonAssetLoaderTests"
```

## Troubleshooting

### Issue: "Type mismatch" errors

**Solution**: Ensure your JSON structure matches your C# class. Arrays should be loaded as `List<T>` or `T[]`.

### Issue: Out of memory with large files

**Solution**: Lower the streaming threshold or increase system memory. The default 1MB threshold is suitable for most scenarios.

### Issue: JSON comments not supported

**Solution**: The loader supports both C-style (`//`) and C++-style (`/* */`) comments by default.

### Issue: Case sensitivity problems

**Solution**: The loader is case-insensitive by default. Use exact property names for best results.

## File Location

- **Implementation**: `/PokeNET/PokeNET.Core/Assets/Loaders/JsonAssetLoader.cs`
- **Tests**: `/tests/Assets/Loaders/JsonAssetLoaderTests.cs`
- **Interface**: `/PokeNET/PokeNET.Domain/Assets/IAssetLoader.cs`

## See Also

- AssetManager documentation
- IAssetLoader interface specification
- Modding API documentation
