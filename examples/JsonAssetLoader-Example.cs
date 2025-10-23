using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PokeNET.Core.Assets;
using PokeNET.Core.Assets.Loaders;
using PokeNET.Domain.Assets;

namespace PokeNET.Examples;

/// <summary>
/// Complete example demonstrating JsonAssetLoader integration with AssetManager.
/// This shows registration, configuration, and usage patterns.
/// </summary>
public class JsonAssetLoaderExample
{
    // Example 1: DI Container Registration
    public static void RegisterServices(IServiceCollection services)
    {
        // Register AssetManager
        services.AddSingleton<IAssetManager>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<AssetManager>>();
            var basePath = "Content/Assets";
            return new AssetManager(logger, basePath);
        });

        // Register JsonAssetLoader for specific types
        services.AddSingleton<IAssetLoader<PokemonData>>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<JsonAssetLoader<PokemonData>>>();
            return new JsonAssetLoader<PokemonData>(logger);
        });

        services.AddSingleton<IAssetLoader<GameConfig>>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<JsonAssetLoader<GameConfig>>>();
            return new JsonAssetLoader<GameConfig>(logger);
        });

        services.AddSingleton<IAssetLoader<List<Item>>>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<JsonAssetLoader<List<Item>>>>();
            // Custom streaming threshold for item lists (500KB)
            return new JsonAssetLoader<List<Item>>(logger, streamingThreshold: 512_000);
        });
    }

    // Example 2: Manual Setup (No DI)
    public static IAssetManager CreateAssetManagerManually()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var assetManagerLogger = loggerFactory.CreateLogger<AssetManager>();
        var assetManager = new AssetManager(assetManagerLogger, "Content/Assets");

        // Register loaders
        var pokemonLogger = loggerFactory.CreateLogger<JsonAssetLoader<PokemonData>>();
        assetManager.RegisterLoader(new JsonAssetLoader<PokemonData>(pokemonLogger));

        var configLogger = loggerFactory.CreateLogger<JsonAssetLoader<GameConfig>>();
        assetManager.RegisterLoader(new JsonAssetLoader<GameConfig>(configLogger));

        return assetManager;
    }

    // Example 3: Loading Pokemon Data
    public static void LoadPokemonExample(IAssetManager assetManager)
    {
        try
        {
            // Load single Pokemon
            var pikachu = assetManager.Load<PokemonData>("pokemon/025_pikachu.json");
            Console.WriteLine($"Loaded: {pikachu.Name} (#{pikachu.Id:000})");
            Console.WriteLine($"Types: {string.Join(", ", pikachu.Types)}");
            Console.WriteLine($"Base Stats Total: {pikachu.BaseStats.Total}");

            // Try-Load pattern for optional assets
            var legendary = assetManager.TryLoad<PokemonData>("pokemon/150_mewtwo.json");
            if (legendary != null)
            {
                Console.WriteLine($"Found legendary: {legendary.Name}");
            }
        }
        catch (AssetLoadException ex)
        {
            Console.WriteLine($"Failed to load Pokemon: {ex.Message}");
            Console.WriteLine($"Asset path: {ex.AssetPath}");

            if (ex.InnerException is System.Text.Json.JsonException jsonEx)
            {
                Console.WriteLine($"JSON error at line {jsonEx.LineNumber}, position {jsonEx.BytePositionInLine}");
            }
        }
    }

    // Example 4: Loading Game Configuration
    public static GameConfig LoadConfigExample(IAssetManager assetManager)
    {
        var config = assetManager.Load<GameConfig>("config/game.json");

        Console.WriteLine("Game Configuration:");
        Console.WriteLine($"  Window: {config.Window.Width}x{config.Window.Height}");
        Console.WriteLine($"  Fullscreen: {config.Window.Fullscreen}");
        Console.WriteLine($"  Master Volume: {config.Audio.MasterVolume}%");

        return config;
    }

    // Example 5: Loading Item Database
    public static void LoadItemsExample(IAssetManager assetManager)
    {
        // Load list of items
        var items = assetManager.Load<List<Item>>("items/all_items.json");

        Console.WriteLine($"Loaded {items.Count} items");

        var pokeballs = items.Where(i => i.Category == "Pokeball").ToList();
        Console.WriteLine($"Found {pokeballs.Count} types of Pokéballs:");

        foreach (var ball in pokeballs.Take(5))
        {
            Console.WriteLine($"  - {ball.Name}: {ball.Description}");
        }
    }

    // Example 6: Mod System Integration
    public static void ModSystemExample(IAssetManager assetManager)
    {
        // Set mod paths (mods override base game assets)
        assetManager.SetModPaths(new[]
        {
            "Mods/CustomPokemon/Assets",     // Highest priority
            "Mods/BalanceChanges/Assets",     // Medium priority
            "Mods/GraphicsOverhaul/Assets"    // Lower priority
        });

        // This will load from mods first, then fall back to base game
        var pokemon = assetManager.Load<PokemonData>("pokemon/025_pikachu.json");

        // Check if asset was loaded from mod or base game
        Console.WriteLine(assetManager.IsLoaded("pokemon/025_pikachu.json")
            ? "Pokemon data is cached"
            : "Pokemon data not in cache");
    }

    // Example 7: Cache Management
    public static void CacheManagementExample(IAssetManager assetManager)
    {
        // Load some assets
        var pokemon1 = assetManager.Load<PokemonData>("pokemon/001_bulbasaur.json");
        var pokemon2 = assetManager.Load<PokemonData>("pokemon/004_charmander.json");
        var pokemon3 = assetManager.Load<PokemonData>("pokemon/007_squirtle.json");

        Console.WriteLine($"Loaded 3 Pokemon");

        // Check cache status
        Console.WriteLine($"Is Bulbasaur cached? {assetManager.IsLoaded("pokemon/001_bulbasaur.json")}");

        // Unload specific asset
        assetManager.Unload("pokemon/001_bulbasaur.json");
        Console.WriteLine($"Is Bulbasaur cached? {assetManager.IsLoaded("pokemon/001_bulbasaur.json")}");

        // Unload all assets (useful for memory management)
        assetManager.UnloadAll();
        Console.WriteLine("All assets unloaded");
    }

    // Example 8: Loading Localization Data
    public static void LocalizationExample(IAssetManager assetManager)
    {
        var english = assetManager.Load<LocalizationData>("localization/en-US.json");
        var spanish = assetManager.Load<LocalizationData>("localization/es-ES.json");

        string pokemonName = "pikachu";
        Console.WriteLine($"English: {english.Strings[$"pokemon.{pokemonName}"]}");
        Console.WriteLine($"Spanish: {spanish.Strings[$"pokemon.{pokemonName}"]}");
    }

    // Example 9: Batch Loading with Error Handling
    public static Dictionary<string, PokemonData> BatchLoadPokemon(
        IAssetManager assetManager,
        IEnumerable<int> pokemonIds)
    {
        var results = new Dictionary<string, PokemonData>();
        var errors = new List<string>();

        foreach (var id in pokemonIds)
        {
            var path = $"pokemon/{id:000}_pokemon.json";
            try
            {
                var pokemon = assetManager.Load<PokemonData>(path);
                results[pokemon.Name] = pokemon;
            }
            catch (AssetLoadException ex)
            {
                errors.Add($"Failed to load Pokemon #{id}: {ex.Message}");
            }
        }

        if (errors.Any())
        {
            Console.WriteLine($"Loaded {results.Count} Pokemon with {errors.Count} errors:");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
            }
        }

        return results;
    }

    // Example 10: Complete Application Setup
    public static void Main(string[] args)
    {
        // Setup DI container
        var services = new ServiceCollection();
        RegisterServices(services);

        var serviceProvider = services.BuildServiceProvider();
        var assetManager = serviceProvider.GetRequiredService<IAssetManager>();

        // Configure mod paths
        assetManager.SetModPaths(new[] { "Mods/CustomContent/Assets" });

        // Load game configuration
        var config = LoadConfigExample(assetManager);

        // Load Pokemon data
        LoadPokemonExample(assetManager);

        // Load items
        LoadItemsExample(assetManager);

        // Load localization
        LocalizationExample(assetManager);

        // Batch load Pokemon
        var generation1 = Enumerable.Range(1, 151);
        var pokemon = BatchLoadPokemon(assetManager, generation1);
        Console.WriteLine($"\nLoaded Generation 1: {pokemon.Count} Pokemon");

        // Cache management
        CacheManagementExample(assetManager);

        Console.WriteLine("\nAll examples completed successfully!");
    }
}

#region Data Models

public class PokemonData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Types { get; set; } = new();
    public PokemonStats BaseStats { get; set; } = new();
    public List<string> Abilities { get; set; } = new();
    public PokemonEvolution? Evolution { get; set; }
}

public class PokemonStats
{
    public int HP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int SpecialAttack { get; set; }
    public int SpecialDefense { get; set; }
    public int Speed { get; set; }

    public int Total => HP + Attack + Defense + SpecialAttack + SpecialDefense + Speed;
}

public class PokemonEvolution
{
    public int Level { get; set; }
    public int EvolvesTo { get; set; }
    public string? Condition { get; set; }
}

public class GameConfig
{
    public WindowSettings Window { get; set; } = new();
    public AudioSettings Audio { get; set; } = new();
    public GameplaySettings Gameplay { get; set; } = new();
}

public class WindowSettings
{
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    public bool Fullscreen { get; set; }
    public bool VSync { get; set; } = true;
}

public class AudioSettings
{
    public int MasterVolume { get; set; } = 100;
    public int MusicVolume { get; set; } = 80;
    public int SfxVolume { get; set; } = 90;
}

public class GameplaySettings
{
    public string Difficulty { get; set; } = "Normal";
    public bool AnimationsEnabled { get; set; } = true;
    public bool AutoSave { get; set; } = true;
}

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Price { get; set; }
    public bool Consumable { get; set; }
}

public class LocalizationData
{
    public string Language { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public Dictionary<string, string> Strings { get; set; } = new();
}

#endregion
