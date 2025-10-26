# PokeNET Modding API Reference - Phase 4

## Table of Contents

- [Overview](#overview)
- [Core Interfaces](#core-interfaces)
  - [IMod](#imod)
  - [IModContext](#imodcontext)
- [Data Access](#data-access)
  - [IGameData](#igamedata)
  - [ICreatureData](#icreaturedata)
  - [IItemData](#iitemdata)
- [Content Loading](#content-loading)
  - [IContentLoader](#icontentloader)
  - [IAssetProcessor](#iassetprocessor)
- [Event System](#event-system)
  - [IEventBus](#ieventbus)
  - [Event Types](#event-types)
- [Settings & Configuration](#settings--configuration)
  - [ISettingsManager](#isettingsmanager)
- [Inter-Mod Communication](#inter-mod-communication)
  - [IModRegistry](#imodregistry)
- [Logging](#logging)
  - [ILogger](#ilogger)
- [Harmony Integration](#harmony-integration)
- [Complete Examples](#complete-examples)

---

## Overview

The PokeNET Modding API provides a comprehensive set of interfaces for creating mods. All interfaces are in the `PokeNET.ModApi` namespace.

**Key principles:**
- Event-driven architecture
- Dependency injection for services
- Immutable game data where possible
- Thread-safe operations
- Clear separation of concerns

---

## Core Interfaces

### IMod

The main entry point for all mods. Every mod must implement this interface.

```csharp
namespace PokeNET.ModApi
{
    /// <summary>
    /// Main interface that all mods must implement.
    /// </summary>
    public interface IMod
    {
        /// <summary>
        /// Unique identifier for this mod (e.g., "com.example.mymod").
        /// Must match the ID in modinfo.json.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Display name of the mod.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Semantic version of the mod.
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// Called when the mod is loaded.
        /// Initialize your mod here, apply Harmony patches, register event handlers.
        /// </summary>
        /// <param name="context">Mod context with access to game systems</param>
        void OnLoad(IModContext context);

        /// <summary>
        /// Called when the mod is unloaded.
        /// Clean up resources, unpatch Harmony, unregister events.
        /// </summary>
        void OnUnload();
    }
}
```

**Usage Example:**

```csharp
using System;
using HarmonyLib;
using PokeNET.ModApi;

public class MyMod : IMod
{
    public string Id => "com.example.mymod";
    public string Name => "My Awesome Mod";
    public Version Version => new Version(1, 0, 0);

    private Harmony _harmony;
    private IModContext _context;

    public void OnLoad(IModContext context)
    {
        _context = context;

        // Apply Harmony patches
        _harmony = new Harmony(Id);
        _harmony.PatchAll();

        // Subscribe to events
        context.Events.OnBattleStart += OnBattleStart;

        // Log successful load
        context.Logger.Info($"{Name} v{Version} loaded successfully");
    }

    public void OnUnload()
    {
        // Unpatch Harmony
        _harmony?.UnpatchAll(Id);

        // Unsubscribe from events
        if (_context != null)
        {
            _context.Events.OnBattleStart -= OnBattleStart;
        }

        _context?.Logger.Info($"{Name} unloaded");
    }

    private void OnBattleStart(object sender, BattleEventArgs e)
    {
        _context.Logger.Info($"Battle started: {e.Battle.Id}");
    }
}
```

---

### IModContext

Provides access to all game systems and services.

```csharp
namespace PokeNET.ModApi
{
    /// <summary>
    /// Context object providing access to game systems.
    /// </summary>
    public interface IModContext
    {
        /// <summary>
        /// Access to game data (creatures, items, abilities, etc.)
        /// </summary>
        IGameData GameData { get; }

        /// <summary>
        /// Event bus for subscribing to game events
        /// </summary>
        IEventBus Events { get; }

        /// <summary>
        /// Content loading and asset management
        /// </summary>
        IContentLoader ContentLoader { get; }

        /// <summary>
        /// Mod settings and configuration
        /// </summary>
        ISettingsManager Settings { get; }

        /// <summary>
        /// Inter-mod communication and API registry
        /// </summary>
        IModRegistry ModRegistry { get; }

        /// <summary>
        /// Logging interface
        /// </summary>
        ILogger Logger { get; }

        /// <summary>
        /// Path to the mod's directory
        /// </summary>
        string ModPath { get; }

        /// <summary>
        /// Information about this mod
        /// </summary>
        ModInfo ModInfo { get; }
    }

    /// <summary>
    /// Metadata about a mod from modinfo.json
    /// </summary>
    public class ModInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public List<ModDependency> Dependencies { get; set; }
        public List<string> LoadAfter { get; set; }
        public List<string> LoadBefore { get; set; }
        public List<string> IncompatibleWith { get; set; }
    }

    public class ModDependency
    {
        public string Id { get; set; }
        public string Version { get; set; }
    }
}
```

---

## Data Access

### IGameData

Access and modify game data.

```csharp
namespace PokeNET.ModApi
{
    /// <summary>
    /// Access to all game data.
    /// </summary>
    public interface IGameData
    {
        /// <summary>
        /// All registered creatures
        /// </summary>
        IReadOnlyDictionary<string, Creature> Creatures { get; }

        /// <summary>
        /// All registered items
        /// </summary>
        IReadOnlyDictionary<string, Item> Items { get; }

        /// <summary>
        /// All registered abilities
        /// </summary>
        IReadOnlyDictionary<string, Ability> Abilities { get; }

        /// <summary>
        /// All registered moves
        /// </summary>
        IReadOnlyDictionary<string, Move> Moves { get; }

        /// <summary>
        /// Register a new creature definition
        /// </summary>
        void RegisterCreature(Creature creature);

        /// <summary>
        /// Register a new item definition
        /// </summary>
        void RegisterItem(Item item);

        /// <summary>
        /// Register a new ability definition
        /// </summary>
        void RegisterAbility(Ability ability);

        /// <summary>
        /// Register a new move definition
        /// </summary>
        void RegisterMove(Move move);

        /// <summary>
        /// Get creature by ID
        /// </summary>
        Creature GetCreature(string id);

        /// <summary>
        /// Get item by ID
        /// </summary>
        Item GetItem(string id);

        /// <summary>
        /// Get ability by ID
        /// </summary>
        Ability GetAbility(string id);

        /// <summary>
        /// Get move by ID
        /// </summary>
        Move GetMove(string id);

        /// <summary>
        /// Check if creature exists
        /// </summary>
        bool HasCreature(string id);

        /// <summary>
        /// Check if item exists
        /// </summary>
        bool HasItem(string id);
    }
}
```

**Usage Example:**

```csharp
public void OnLoad(IModContext context)
{
    // Access existing creature
    var pikachu = context.GameData.GetCreature("pikachu");
    context.Logger.Info($"Pikachu base attack: {pikachu.BaseStats.Attack}");

    // Register new creature
    var newCreature = new Creature
    {
        Id = "flame_dragon",
        Name = "Flame Dragon",
        Types = new[] { "fire", "dragon" },
        BaseStats = new Stats
        {
            HP = 78,
            Attack = 84,
            Defense = 78,
            SpecialAttack = 109,
            SpecialDefense = 85,
            Speed = 100
        }
    };
    context.GameData.RegisterCreature(newCreature);

    // Check if item exists
    if (context.GameData.HasItem("master_ball"))
    {
        var masterBall = context.GameData.GetItem("master_ball");
        context.Logger.Info($"Master Ball found: {masterBall.Description}");
    }
}
```

---

### ICreatureData

Detailed creature data structures.

```csharp
namespace PokeNET.ModApi
{
    public class Creature
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string[] Types { get; set; }
        public Stats BaseStats { get; set; }
        public string[] Abilities { get; set; }
        public LearnsetEntry[] Learnset { get; set; }
        public EvolutionChain[] EvolutionChain { get; set; }
        public string SpriteId { get; set; }
        public string Description { get; set; }
        public float CatchRate { get; set; }
        public int BaseExperience { get; set; }
        public string GrowthRate { get; set; }
    }

    public class Stats
    {
        public int HP { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int SpecialAttack { get; set; }
        public int SpecialDefense { get; set; }
        public int Speed { get; set; }
    }

    public class LearnsetEntry
    {
        public int Level { get; set; }
        public string Move { get; set; }
    }

    public class EvolutionChain
    {
        public string From { get; set; }
        public string To { get; set; }
        public int? Level { get; set; }
        public string Item { get; set; }
        public string Condition { get; set; }
    }
}
```

---

### IItemData

Item data structures.

```csharp
namespace PokeNET.ModApi
{
    public class Item
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public int Cost { get; set; }
        public string SpriteId { get; set; }
        public ItemEffect Effect { get; set; }
        public bool Consumable { get; set; }
        public bool KeyItem { get; set; }
    }

    public class ItemEffect
    {
        public string Type { get; set; }
        public int Amount { get; set; }
        public string Target { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
}
```

---

## Content Loading

### IContentLoader

Load assets and content files.

```csharp
namespace PokeNET.ModApi
{
    /// <summary>
    /// Load assets and content files.
    /// </summary>
    public interface IContentLoader
    {
        /// <summary>
        /// Load JSON file and deserialize to type T
        /// </summary>
        T LoadJson<T>(string relativePath);

        /// <summary>
        /// Load sprite from file
        /// </summary>
        Sprite LoadSprite(string relativePath);

        /// <summary>
        /// Load sprite asynchronously
        /// </summary>
        void LoadSpriteAsync(string relativePath, Action<Sprite> callback);

        /// <summary>
        /// Load audio clip
        /// </summary>
        AudioClip LoadAudio(string relativePath);

        /// <summary>
        /// Load audio clip asynchronously
        /// </summary>
        void LoadAudioAsync(string relativePath, Action<AudioClip> callback);

        /// <summary>
        /// Load text file
        /// </summary>
        string LoadText(string relativePath);

        /// <summary>
        /// Load binary file
        /// </summary>
        byte[] LoadBytes(string relativePath);

        /// <summary>
        /// Check if file exists
        /// </summary>
        bool FileExists(string relativePath);
    }
}
```

**Usage Example:**

```csharp
public void OnLoad(IModContext context)
{
    // Load JSON data
    var customData = context.ContentLoader.LoadJson<CustomData>(
        "Data/custom_creatures.json");

    // Load sprite
    var sprite = context.ContentLoader.LoadSprite(
        "Content/Sprites/flame_dragon.png");

    // Load sprite asynchronously
    context.ContentLoader.LoadSpriteAsync(
        "Content/Sprites/big_sprite.png",
        sprite =>
        {
            context.Logger.Info("Sprite loaded!");
            RegisterSprite("big_sprite", sprite);
        });

    // Load audio
    var music = context.ContentLoader.LoadAudio(
        "Content/Audio/Music/battle_theme.ogg");

    // Load text file
    var dialogue = context.ContentLoader.LoadText(
        "Data/dialogue.txt");
}
```

---

### IAssetProcessor

Create custom asset processors for non-standard formats.

```csharp
namespace PokeNET.ModApi
{
    /// <summary>
    /// Interface for custom asset processors.
    /// </summary>
    public interface IAssetProcessor
    {
        /// <summary>
        /// Can this processor handle files with this extension?
        /// </summary>
        bool CanProcess(string extension);

        /// <summary>
        /// Process raw file data into a game object
        /// </summary>
        object Process(byte[] data);
    }

    /// <summary>
    /// Content pipeline for registering custom processors
    /// </summary>
    public interface IContentPipeline
    {
        void RegisterProcessor(IAssetProcessor processor);
    }
}
```

**Usage Example:**

```csharp
public class CustomSpriteProcessor : IAssetProcessor
{
    public bool CanProcess(string extension)
    {
        return extension == ".custom";
    }

    public object Process(byte[] data)
    {
        // Parse custom format
        var header = ParseHeader(data);
        var pixels = DecompressPixels(data);
        return CreateSprite(pixels, header.Width, header.Height);
    }
}

public void OnLoad(IModContext context)
{
    context.ContentPipeline.RegisterProcessor(new CustomSpriteProcessor());
}
```

---

## Event System

### IEventBus

Subscribe to game events.

```csharp
namespace PokeNET.ModApi
{
    /// <summary>
    /// Event bus for game events.
    /// </summary>
    public interface IEventBus
    {
        // Battle Events
        event EventHandler<BattleEventArgs> OnBattleStart;
        event EventHandler<BattleEventArgs> OnBattleEnd;
        event EventHandler<TurnEventArgs> OnTurnStart;
        event EventHandler<TurnEventArgs> OnTurnEnd;
        event EventHandler<DamageEventArgs> OnDamageDealt;
        event EventHandler<MoveEventArgs> OnMoveUsed;

        // Creature Events
        event EventHandler<CaptureEventArgs> OnCreatureCaptured;
        event EventHandler<EvolutionEventArgs> OnCreatureEvolved;
        event EventHandler<LevelUpEventArgs> OnCreatureLevelUp;
        event EventHandler<FaintEventArgs> OnCreatureFainted;

        // Item Events
        event EventHandler<ItemEventArgs> OnItemUsed;
        event EventHandler<ItemEventArgs> OnItemObtained;

        // Game Events
        event EventHandler<SaveEventArgs> OnGameSaved;
        event EventHandler<LoadEventArgs> OnGameLoaded;
        event EventHandler<SceneEventArgs> OnSceneChanged;
    }
}
```

---

### Event Types

Event argument classes for different event types.

```csharp
namespace PokeNET.ModApi
{
    public class BattleEventArgs : EventArgs
    {
        public Battle Battle { get; set; }
        public Creature PlayerCreature { get; set; }
        public Creature OpponentCreature { get; set; }
    }

    public class TurnEventArgs : EventArgs
    {
        public Battle Battle { get; set; }
        public int TurnNumber { get; set; }
    }

    public class DamageEventArgs : EventArgs
    {
        public Creature Attacker { get; set; }
        public Creature Defender { get; set; }
        public Move Move { get; set; }
        public int Damage { get; set; }
        public bool IsCritical { get; set; }
        public float Effectiveness { get; set; }
    }

    public class MoveEventArgs : EventArgs
    {
        public Creature User { get; set; }
        public Creature Target { get; set; }
        public Move Move { get; set; }
        public bool Hit { get; set; }
    }

    public class CaptureEventArgs : EventArgs
    {
        public Creature Creature { get; set; }
        public Item Ball { get; set; }
        public bool Success { get; set; }
        public int ShakeCount { get; set; }
    }

    public class EvolutionEventArgs : EventArgs
    {
        public Creature OldForm { get; set; }
        public Creature NewForm { get; set; }
        public string Trigger { get; set; }
    }

    public class LevelUpEventArgs : EventArgs
    {
        public Creature Creature { get; set; }
        public int OldLevel { get; set; }
        public int NewLevel { get; set; }
        public Move[] LearnedMoves { get; set; }
    }

    public class FaintEventArgs : EventArgs
    {
        public Creature Creature { get; set; }
        public Creature Attacker { get; set; }
    }

    public class ItemEventArgs : EventArgs
    {
        public Item Item { get; set; }
        public Creature Target { get; set; }
        public bool Success { get; set; }
    }

    public class SaveEventArgs : EventArgs
    {
        public string SaveSlot { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class LoadEventArgs : EventArgs
    {
        public string SaveSlot { get; set; }
    }

    public class SceneEventArgs : EventArgs
    {
        public string OldScene { get; set; }
        public string NewScene { get; set; }
    }
}
```

**Usage Example:**

```csharp
public void OnLoad(IModContext context)
{
    // Subscribe to battle events
    context.Events.OnBattleStart += (sender, e) =>
    {
        context.Logger.Info(
            $"Battle: {e.PlayerCreature.Name} vs {e.OpponentCreature.Name}");
    };

    // Subscribe to damage events
    context.Events.OnDamageDealt += (sender, e) =>
    {
        if (e.IsCritical)
        {
            context.Logger.Info($"Critical hit! {e.Damage} damage!");
        }
    };

    // Subscribe to capture events
    context.Events.OnCreatureCaptured += (sender, e) =>
    {
        if (e.Success)
        {
            context.Logger.Info($"Caught {e.Creature.Name}!");
        }
    };
}
```

---

## Settings & Configuration

### ISettingsManager

Manage mod settings and configuration.

```csharp
namespace PokeNET.ModApi
{
    /// <summary>
    /// Manage mod settings and configuration.
    /// </summary>
    public interface ISettingsManager
    {
        /// <summary>
        /// Load settings from file
        /// </summary>
        T Load<T>(string filename) where T : new();

        /// <summary>
        /// Save settings to file
        /// </summary>
        void Save<T>(string filename, T settings);

        /// <summary>
        /// Get setting value
        /// </summary>
        T GetValue<T>(string key, T defaultValue = default);

        /// <summary>
        /// Set setting value
        /// </summary>
        void SetValue<T>(string key, T value);

        /// <summary>
        /// Check if setting exists
        /// </summary>
        bool HasSetting(string key);

        /// <summary>
        /// Delete setting
        /// </summary>
        void DeleteSetting(string key);
    }
}
```

**Usage Example:**

```csharp
public class ModSettings
{
    public float DamageMultiplier { get; set; } = 2.0f;
    public bool EnableDebugMode { get; set; } = false;
    public string CustomMessage { get; set; } = "Hello!";
}

public class MyMod : IMod
{
    private ModSettings _settings;

    public void OnLoad(IModContext context)
    {
        // Load settings (creates default if not exists)
        _settings = context.Settings.Load<ModSettings>("settings.json");

        context.Logger.Info($"Damage multiplier: {_settings.DamageMultiplier}");

        // Use individual settings
        var debugMode = context.Settings.GetValue("debug_mode", false);

        // Modify and save settings
        _settings.DamageMultiplier = 3.0f;
        context.Settings.Save("settings.json", _settings);
    }
}
```

---

## Inter-Mod Communication

### IModRegistry

Register and access APIs from other mods.

```csharp
namespace PokeNET.ModApi
{
    /// <summary>
    /// Inter-mod communication and API registry.
    /// </summary>
    public interface IModRegistry
    {
        /// <summary>
        /// Register an API for other mods to use
        /// </summary>
        void RegisterAPI<T>(T api) where T : class;

        /// <summary>
        /// Get API from another mod
        /// </summary>
        T GetAPI<T>() where T : class;

        /// <summary>
        /// Check if API is available
        /// </summary>
        bool HasAPI<T>() where T : class;

        /// <summary>
        /// Get all loaded mods
        /// </summary>
        IReadOnlyList<ModInfo> GetLoadedMods();

        /// <summary>
        /// Check if mod is loaded
        /// </summary>
        bool IsModLoaded(string modId);

        /// <summary>
        /// Get mod info by ID
        /// </summary>
        ModInfo GetModInfo(string modId);
    }
}
```

**Usage Example:**

```csharp
// Mod A: Provide API
public interface IDragonAPI
{
    float GetDragonPower(Creature creature);
    void RegisterDragonType(string typeId);
}

public class DragonMod : IMod, IDragonAPI
{
    public void OnLoad(IModContext context)
    {
        // Register API for other mods
        context.ModRegistry.RegisterAPI<IDragonAPI>(this);
    }

    public float GetDragonPower(Creature creature)
    {
        return creature.Types.Contains("dragon") ? 2.0f : 1.0f;
    }

    public void RegisterDragonType(string typeId)
    {
        // Implementation
    }
}

// Mod B: Use API
public class MyMod : IMod
{
    public void OnLoad(IModContext context)
    {
        // Check if Dragon API is available
        if (context.ModRegistry.HasAPI<IDragonAPI>())
        {
            var dragonAPI = context.ModRegistry.GetAPI<IDragonAPI>();
            var power = dragonAPI.GetDragonPower(myCreature);
            context.Logger.Info($"Dragon power: {power}");
        }

        // Check if specific mod is loaded
        if (context.ModRegistry.IsModLoaded("com.example.dragonmod"))
        {
            context.Logger.Info("Dragon mod detected!");
        }
    }
}
```

---

## Logging

### ILogger

Logging interface for mods.

```csharp
namespace PokeNET.ModApi
{
    /// <summary>
    /// Logging interface for mods.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Log info message
        /// </summary>
        void Info(string message);

        /// <summary>
        /// Log warning message
        /// </summary>
        void Warning(string message);

        /// <summary>
        /// Log error message
        /// </summary>
        void Error(string message);

        /// <summary>
        /// Log error with exception
        /// </summary>
        void Error(string message, Exception exception);

        /// <summary>
        /// Log debug message (only in debug builds)
        /// </summary>
        void Debug(string message);

        /// <summary>
        /// Log with custom severity
        /// </summary>
        void Log(LogLevel level, string message);
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}
```

**Usage Example:**

```csharp
public void OnLoad(IModContext context)
{
    context.Logger.Info("Mod loaded successfully");
    context.Logger.Debug($"Mod path: {context.ModPath}");

    try
    {
        // Risky operation
        LoadCustomData();
    }
    catch (Exception ex)
    {
        context.Logger.Error("Failed to load custom data", ex);
    }

    context.Logger.Warning("This feature is experimental");
}
```

---

## Harmony Integration

While not part of the API interfaces, Harmony is the recommended way to patch game code.

### Common Harmony Patterns

**Postfix (run after method):**

```csharp
using HarmonyLib;
using PokeNET.Core.Battle;

[HarmonyPatch(typeof(BattleSystem), "CalculateDamage")]
public class DamagePostfix
{
    static void Postfix(ref int __result, Creature attacker, Creature defender)
    {
        // Modify result
        __result = (int)(__result * 1.5f);
    }
}
```

**Prefix (run before method, can skip original):**

```csharp
[HarmonyPatch(typeof(CaptureSystem), "CalculateCatchRate")]
public class CapturePrefix
{
    static bool Prefix(ref float __result, Creature creature, Item ball)
    {
        if (ball.Id == "master_ball")
        {
            __result = 1.0f; // 100% catch rate
            return false; // Skip original method
        }
        return true; // Run original method
    }
}
```

**Transpiler (modify IL code):**

```csharp
[HarmonyPatch(typeof(Experience), "GainExp")]
public class ExpTranspiler
{
    static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldarg_1)
            {
                yield return instruction;
                yield return new CodeInstruction(OpCodes.Ldc_I4_2);
                yield return new CodeInstruction(OpCodes.Mul);
            }
            else
            {
                yield return instruction;
            }
        }
    }
}
```

---

## Complete Examples

### Example 1: Simple Data Mod

```csharp
using System;
using PokeNET.ModApi;

namespace SimpleDataMod
{
    public class SimpleDataMod : IMod
    {
        public string Id => "com.example.simpledatamod";
        public string Name => "Simple Data Mod";
        public Version Version => new Version(1, 0, 0);

        public void OnLoad(IModContext context)
        {
            // Load custom creatures
            var creatures = context.ContentLoader.LoadJson<CreatureList>(
                "Data/creatures.json");

            foreach (var creature in creatures.Creatures)
            {
                context.GameData.RegisterCreature(creature);
                context.Logger.Info($"Registered creature: {creature.Name}");
            }
        }

        public void OnUnload()
        {
            // Nothing to clean up
        }
    }

    public class CreatureList
    {
        public Creature[] Creatures { get; set; }
    }
}
```

### Example 2: Event-Driven Mod

```csharp
using System;
using HarmonyLib;
using PokeNET.ModApi;

namespace EventMod
{
    public class EventMod : IMod
    {
        public string Id => "com.example.eventmod";
        public string Name => "Event Mod";
        public Version Version => new Version(1, 0, 0);

        private IModContext _context;

        public void OnLoad(IModContext context)
        {
            _context = context;

            // Subscribe to various events
            context.Events.OnBattleStart += OnBattleStart;
            context.Events.OnDamageDealt += OnDamageDealt;
            context.Events.OnCreatureCaptured += OnCreatureCaptured;

            context.Logger.Info("Event mod loaded");
        }

        public void OnUnload()
        {
            if (_context != null)
            {
                _context.Events.OnBattleStart -= OnBattleStart;
                _context.Events.OnDamageDealt -= OnDamageDealt;
                _context.Events.OnCreatureCaptured -= OnCreatureCaptured;
            }
        }

        private void OnBattleStart(object sender, BattleEventArgs e)
        {
            _context.Logger.Info(
                $"Battle: {e.PlayerCreature.Name} vs {e.OpponentCreature.Name}");
        }

        private void OnDamageDealt(object sender, DamageEventArgs e)
        {
            if (e.IsCritical)
            {
                _context.Logger.Info($"Critical hit! {e.Damage} damage!");
            }
        }

        private void OnCreatureCaptured(object sender, CaptureEventArgs e)
        {
            if (e.Success)
            {
                _context.Logger.Info($"Successfully caught {e.Creature.Name}!");
            }
        }
    }
}
```

### Example 3: Harmony Code Mod

```csharp
using System;
using HarmonyLib;
using PokeNET.ModApi;
using PokeNET.Core.Battle;

namespace HarmonyMod
{
    public class HarmonyMod : IMod
    {
        public string Id => "com.example.harmonymod";
        public string Name => "Harmony Mod";
        public Version Version => new Version(1, 0, 0);

        private Harmony _harmony;

        public void OnLoad(IModContext context)
        {
            // Apply all Harmony patches
            _harmony = new Harmony(Id);
            _harmony.PatchAll();

            context.Logger.Info("Harmony patches applied");
        }

        public void OnUnload()
        {
            _harmony?.UnpatchAll(Id);
        }
    }

    // Harmony patches
    [HarmonyPatch(typeof(BattleSystem), "CalculateDamage")]
    public class DamagePatch
    {
        static void Postfix(ref int __result)
        {
            __result *= 2; // Double all damage
        }
    }

    [HarmonyPatch(typeof(CaptureSystem), "AttemptCapture")]
    public class CapturePatch
    {
        static void Prefix(Creature creature, Item ball)
        {
            Console.WriteLine($"Attempting to catch {creature.Name} with {ball.Name}");
        }
    }
}
```

### Example 4: Inter-Mod Communication

```csharp
using System;
using PokeNET.ModApi;

namespace CommunicationMod
{
    // Define public API
    public interface ICustomAPI
    {
        int GetCustomValue(string key);
        void SetCustomValue(string key, int value);
    }

    public class CommunicationMod : IMod, ICustomAPI
    {
        public string Id => "com.example.commmod";
        public string Name => "Communication Mod";
        public Version Version => new Version(1, 0, 0);

        private readonly Dictionary<string, int> _customValues = new();

        public void OnLoad(IModContext context)
        {
            // Register API for other mods
            context.ModRegistry.RegisterAPI<ICustomAPI>(this);

            // Check for other mod's API
            if (context.ModRegistry.HasAPI<ISomeOtherAPI>())
            {
                var otherAPI = context.ModRegistry.GetAPI<ISomeOtherAPI>();
                otherAPI.DoSomething();
            }

            context.Logger.Info("Communication mod loaded");
        }

        public void OnUnload()
        {
            // Nothing to clean up
        }

        // Implement API
        public int GetCustomValue(string key)
        {
            return _customValues.GetValueOrDefault(key, 0);
        }

        public void SetCustomValue(string key, int value)
        {
            _customValues[key] = value;
        }
    }
}
```

---

## Additional Resources

- **Modding Guide**: `/docs/modding/phase4-modding-guide.md`
- **Example Mods**: `/docs/examples/`
- **Tutorials**: `/docs/tutorials/mod-development.md`
- **Harmony Documentation**: https://harmony.pardeike.net/

---

**Last Updated**: Phase 4 - Modding Framework Implementation
