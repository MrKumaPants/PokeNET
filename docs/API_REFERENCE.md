# ModAPI Reference Documentation

## Overview

The PokeNET ModAPI provides safe, sandboxed access to game systems for mod developers. The API follows the **Interface Segregation Principle (ISP)** by providing focused interfaces instead of one monolithic API.

---

## Quick Start

```csharp
public class MyMod : ICodeMod
{
    public string Id => "my-mod";
    public string Name => "My First Mod";
    public Version Version => new Version(1, 0, 0);

    public void OnInitialize(IModContext context)
    {
        // Access focused APIs
        var entities = context.Entities;
        var assets = context.Assets;
        var events = context.BattleEvents;

        // Subscribe to events
        events.OnBattleStart += HandleBattleStart;

        // Create custom Pokemon
        var pikachu = entities.CreateEntity(
            new PokemonData { SpeciesId = 25, Level = 15 },
            new GridPosition(10, 20),
            new Sprite { TexturePath = "mods/my-mod/pikachu.png" }
        );
    }

    private void HandleBattleStart(object sender, BattleStartEventArgs e)
    {
        Console.WriteLine("Battle started!");
    }
}
```

---

## IModContext

**Purpose:** Root context providing access to all mod APIs.

```csharp
public interface IModContext
{
    // Entity manipulation
    IEntityApi Entities { get; }

    // Asset loading
    IAssetApi Assets { get; }

    // Event subscriptions (focused interfaces)
    IGameplayEvents GameplayEvents { get; }
    IBattleEvents BattleEvents { get; }
    IUIEvents UIEvents { get; }
    ISaveEvents SaveEvents { get; }
    IModEvents ModEvents { get; }

    // Configuration
    IConfigurationApi Configuration { get; }

    // Mod info
    string ModId { get; }
    string ModDirectory { get; }
}
```

---

## IEntityApi

**Purpose:** Create, query, and manipulate entities in the ECS world.

### Methods

#### CreateEntity

```csharp
Entity CreateEntity(params object[] components);
```

**Description:** Creates a new entity with the specified components.

**Example:**
```csharp
var pokemon = context.Entities.CreateEntity(
    new PokemonData
    {
        SpeciesId = 25,
        Level = 15,
        Nickname = "Sparky"
    },
    new PokemonStats
    {
        MaxHP = 45,
        HP = 45,
        Attack = 30
    },
    new GridPosition(5, 10)
);
```

---

#### DestroyEntity

```csharp
void DestroyEntity(Entity entity);
```

**Description:** Removes an entity and all its components from the world.

**Example:**
```csharp
context.Entities.DestroyEntity(pokemon);
```

---

#### HasComponent

```csharp
bool HasComponent<T>(Entity entity);
```

**Description:** Checks if an entity has a specific component type.

**Example:**
```csharp
if (context.Entities.HasComponent<PokemonData>(entity))
{
    Console.WriteLine("This entity is a Pokemon!");
}
```

---

#### GetComponent

```csharp
ref T GetComponent<T>(Entity entity);
```

**Description:** Gets a reference to a component (allows modification).

**Example:**
```csharp
ref var stats = ref context.Entities.GetComponent<PokemonStats>(entity);
stats.HP -= 10;  // Modify component directly
```

---

#### SetComponent

```csharp
void SetComponent<T>(Entity entity, T component);
```

**Description:** Sets or adds a component to an entity.

**Example:**
```csharp
context.Entities.SetComponent(entity, new StatusCondition
{
    Status = ConditionType.Poison,
    TurnsActive = 0
});
```

---

#### Query

```csharp
IEntityQuery<T1> Query<T1>();
IEntityQuery<T1, T2> Query<T1, T2>();
IEntityQuery<T1, T2, T3> Query<T1, T2, T3>();
```

**Description:** Queries entities with specific component combinations.

**Example:**
```csharp
// Find all Pokemon in battle
var query = context.Entities.Query<PokemonData, BattleState>();

foreach (var (entity, pokemon, battle) in query)
{
    if (battle.Status == BattleStatus.InBattle)
    {
        Console.WriteLine($"{pokemon.Nickname} is battling!");
    }
}
```

**Advanced Filtering:**
```csharp
// Find Pokemon with GridPosition but WITHOUT Sprite (invisible Pokemon)
var query = context.Entities.Query<PokemonData, GridPosition>()
    .Without<Sprite>();

Console.WriteLine($"Found {query.Count()} invisible Pokemon");
```

---

## IAssetApi

**Purpose:** Load game assets (textures, audio, JSON data).

### Methods

#### LoadAsset

```csharp
T LoadAsset<T>(string path);
```

**Description:** Loads an asset of the specified type.

**Supported Types:**
- `Texture2D` - Images (.png, .jpg)
- `AudioTrack` - Music (.mid, .ogg)
- `JsonObject` - JSON data
- `string` - Text files

**Example:**
```csharp
// Load custom sprite
var texture = context.Assets.LoadAsset<Texture2D>("mods/my-mod/sprites/shiny_pikachu.png");

// Load custom Pokemon data
var pokemonData = context.Assets.LoadAsset<JsonObject>("mods/my-mod/data/custom_pokemon.json");

// Load custom music
var battleMusic = context.Assets.LoadAsset<AudioTrack>("mods/my-mod/music/battle_theme.mid");
```

---

#### RegisterAssetLoader

```csharp
void RegisterAssetLoader<T>(IAssetLoader<T> loader);
```

**Description:** Registers a custom asset loader for a new file type.

**Example:**
```csharp
public class CustomFormatLoader : IAssetLoader<CustomData>
{
    public CustomData Load(string path)
    {
        // Custom loading logic
        var bytes = File.ReadAllBytes(path);
        return ParseCustomFormat(bytes);
    }
}

// Register in mod
context.Assets.RegisterAssetLoader(new CustomFormatLoader());

// Now you can load custom format
var data = context.Assets.LoadAsset<CustomData>("mods/my-mod/data.custom");
```

---

## IGameplayEvents

**Purpose:** Subscribe to general gameplay events.

### Events

#### OnUpdate

```csharp
event EventHandler<GameUpdateEventArgs> OnUpdate;
```

**Fired:** Every frame.

**Warning:** Use sparingly! Prefer component systems for per-frame logic.

**Example:**
```csharp
context.GameplayEvents.OnUpdate += (sender, e) =>
{
    Console.WriteLine($"Delta time: {e.DeltaTime}");
};
```

---

#### OnLocationChanged

```csharp
event EventHandler<LocationChangedEventArgs> OnLocationChanged;
```

**Fired:** When the player moves to a new map/area.

**Example:**
```csharp
context.GameplayEvents.OnLocationChanged += (sender, e) =>
{
    Console.WriteLine($"Moved from {e.OldLocation} to {e.NewLocation}");

    // Spawn custom Pokemon in specific areas
    if (e.NewLocation == "Viridian Forest")
    {
        SpawnCustomPokemon(context, "MissingNo");
    }
};
```

---

#### OnItemUsed

```csharp
event EventHandler<ItemUsedEventArgs> OnItemUsed;
```

**Fired:** When the player uses an item.

**Example:**
```csharp
context.GameplayEvents.OnItemUsed += (sender, e) =>
{
    // Custom item effects
    if (e.Item == customItemId)
    {
        // Apply custom buff
        ref var stats = ref context.Entities.GetComponent<PokemonStats>(e.Target);
        stats.Attack *= 2;
    }
};
```

---

## IBattleEvents

**Purpose:** Subscribe to battle-specific events.

### Events

#### OnBattleStart

```csharp
event EventHandler<BattleStartEventArgs> OnBattleStart;
```

**Fired:** When a battle begins.

**Example:**
```csharp
context.BattleEvents.OnBattleStart += (sender, e) =>
{
    Console.WriteLine($"Battle started! Wild: {e.IsWildBattle}");

    // Custom battle intro music
    if (e.IsWildBattle)
    {
        PlayCustomMusic("mods/my-mod/music/wild_battle.mid");
    }
};
```

---

#### OnDamageCalculated

```csharp
event EventHandler<DamageCalculatedEventArgs> OnDamageCalculated;
```

**Fired:** After damage is calculated but before it's applied.

**Mutable:** You can modify `e.Damage` to change the final damage!

**Example:**
```csharp
context.BattleEvents.OnDamageCalculated += (sender, e) =>
{
    // Double damage on critical hits
    if (e.IsCritical)
    {
        e.Damage *= 2;
    }

    // Custom type effectiveness
    if (e.MoveName == "CustomMove")
    {
        e.Damage = (int)(e.Damage * 1.5f);
    }

    Console.WriteLine($"{e.Attacker} will deal {e.Damage} damage to {e.Defender}");
};
```

---

#### OnCreatureFainted

```csharp
event EventHandler<CreatureFaintedEventArgs> OnCreatureFainted;
```

**Fired:** When a Pokemon faints.

**Example:**
```csharp
context.BattleEvents.OnCreatureFainted += (sender, e) =>
{
    // Award custom items on defeat
    if (e.Attacker != null)
    {
        ref var inventory = ref context.Entities.GetComponent<Inventory>(e.Attacker);
        inventory.AddItem(customItemId, 1);
    }
};
```

---

#### OnMoveUsed

```csharp
event EventHandler<MoveUsedEventArgs> OnMoveUsed;
```

**Fired:** When a Pokemon uses a move.

**Example:**
```csharp
context.BattleEvents.OnMoveUsed += (sender, e) =>
{
    Console.WriteLine($"{e.Attacker} used {e.MoveName} on {e.Defender}!");

    // Custom move effects
    if (e.MoveName == "CustomHeal")
    {
        ref var stats = ref context.Entities.GetComponent<PokemonStats>(e.Attacker);
        stats.HP = Math.Min(stats.HP + 20, stats.MaxHP);
    }
};
```

---

## ISaveEvents

**Purpose:** Persist mod data across save/load.

### Events

#### OnSaving

```csharp
event EventHandler<SavingEventArgs> OnSaving;
```

**Fired:** Before the game saves.

**Example:**
```csharp
context.SaveEvents.OnSaving += (sender, e) =>
{
    // Store custom mod data
    e.ModData["my-mod"] = new
    {
        customFlag = true,
        customCounter = 42,
        customData = GetModSpecificData()
    };
};
```

---

#### OnLoaded

```csharp
event EventHandler<LoadedEventArgs> OnLoaded;
```

**Fired:** After a save is loaded.

**Example:**
```csharp
context.SaveEvents.OnLoaded += (sender, e) =>
{
    // Restore custom mod data
    if (e.ModData.TryGetValue("my-mod", out var data))
    {
        var modData = (JsonObject)data;
        var customFlag = modData["customFlag"].GetValue<bool>();
        var customCounter = modData["customCounter"].GetValue<int>();

        RestoreModData(customFlag, customCounter);
    }
};
```

---

## IConfigurationApi

**Purpose:** Load and save mod configuration settings.

### Methods

#### Get

```csharp
T Get<T>(string key, T defaultValue = default);
```

**Example:**
```csharp
// Load config value
var expMultiplier = context.Configuration.Get<float>("exp_multiplier", 1.0f);
var enableCustomMusic = context.Configuration.Get<bool>("custom_music", true);
```

---

#### Set

```csharp
void Set<T>(string key, T value);
```

**Example:**
```csharp
// Save config value
context.Configuration.Set("exp_multiplier", 2.0f);
context.Configuration.Set("custom_music", false);
```

---

#### Save

```csharp
void Save();
```

**Description:** Persists configuration to disk (`mods/{mod-id}/config.json`).

**Example:**
```csharp
context.Configuration.Set("difficulty", "hard");
context.Configuration.Save();  // Writes to disk
```

---

## Advanced Examples

### Example 1: Custom Evolution Method

```csharp
public class CustomEvolutionMod : ICodeMod
{
    public void OnInitialize(IModContext context)
    {
        context.BattleEvents.OnBattleEnd += (sender, e) =>
        {
            if (!e.PlayerWon) return;

            // Custom evolution: Evolve Pikachu after 10 wins
            var query = context.Entities.Query<PokemonData, BattleState>();

            foreach (var (entity, data, battle) in query)
            {
                if (data.SpeciesId == 25 && battle.Wins >= 10)
                {
                    // Evolve to Raichu
                    data.SpeciesId = 26;
                    Console.WriteLine($"{data.Nickname} evolved into Raichu!");
                }
            }
        };
    }
}
```

---

### Example 2: Dynamic Difficulty Scaling

```csharp
public class DifficultyScalingMod : ICodeMod
{
    public void OnInitialize(IModContext context)
    {
        context.BattleEvents.OnBattleStart += (sender, e) =>
        {
            if (!e.IsWildBattle) return;

            // Get player's average level
            var playerLevel = GetAveragePartyLevel(context, e.PlayerTeam);

            // Scale wild Pokemon level
            ref var enemyData = ref context.Entities.GetComponent<PokemonData>(e.EnemyTeam);
            enemyData.Level = playerLevel + context.Configuration.Get<int>("level_offset", 2);

            // Recalculate stats
            RecalculateStats(context, e.EnemyTeam);
        };
    }

    private int GetAveragePartyLevel(IModContext context, Entity playerTeam)
    {
        var party = context.Entities.GetComponent<Party>(playerTeam);
        int totalLevel = 0;

        for (int i = 0; i < party.Count; i++)
        {
            var pokemon = party.GetPokemon(i);
            var data = context.Entities.GetComponent<PokemonData>(pokemon);
            totalLevel += data.Level;
        }

        return party.Count > 0 ? totalLevel / party.Count : 1;
    }
}
```

---

### Example 3: Custom Status Condition

```csharp
public class FrozenSolidMod : ICodeMod
{
    public void OnInitialize(IModContext context)
    {
        context.BattleEvents.OnMoveUsed += (sender, e) =>
        {
            if (e.MoveName == "Deep Freeze")
            {
                // Apply custom status: Frozen for 3 turns (can't thaw early)
                ref var condition = ref context.Entities.GetComponent<StatusCondition>(e.Defender);
                condition.Status = ConditionType.Freeze;
                condition.TurnsActive = 0;
                condition.CustomData = 3;  // Frozen for exactly 3 turns
            }
        };

        context.BattleEvents.OnTurnStart += (sender, e) =>
        {
            // Process custom status logic
            var query = context.Entities.Query<StatusCondition, BattleState>();

            foreach (var (entity, condition, battle) in query)
            {
                if (condition.Status == ConditionType.Freeze && condition.CustomData > 0)
                {
                    condition.TurnsActive++;

                    if (condition.TurnsActive >= condition.CustomData)
                    {
                        // Thaw after exact turns
                        condition.Status = ConditionType.None;
                        Console.WriteLine($"Pokemon thawed!");
                    }
                }
            }
        };
    }
}
```

---

## Type Reference

### Component Types

All component types available for querying:

| Component | Description |
|-----------|-------------|
| `PokemonData` | Species, level, nature, gender |
| `PokemonStats` | HP, Attack, Defense, IVs, EVs |
| `BattleState` | Battle status, turn counter, stat stages |
| `MoveSet` | Up to 4 moves |
| `StatusCondition` | Poison, burn, paralysis, etc. |
| `GridPosition` | Tile-based position (X, Y, Z) |
| `MovementState` | Moving, target position |
| `Sprite` | Texture path, dimensions |
| `Inventory` | Items and quantities |
| `Party` | 6 Pokemon team |
| `Pokedex` | Seen/caught Pokemon |

---

### Event Argument Types

| Event Args | Properties |
|------------|------------|
| `BattleStartEventArgs` | PlayerTeam, EnemyTeam, IsWildBattle |
| `DamageCalculatedEventArgs` | Attacker, Defender, Damage (mutable), IsCritical |
| `CreatureFaintedEventArgs` | Creature, Attacker |
| `MoveUsedEventArgs` | Attacker, Defender, MoveName |
| `LocationChangedEventArgs` | OldLocation, NewLocation, Player |
| `SavingEventArgs` | SavePath, ModData (mutable) |
| `LoadedEventArgs` | SavePath, ModData |

---

## Best Practices

### 1. **Use Focused APIs**

```csharp
// ✅ GOOD: Only access what you need
var entities = context.Entities;
var battleEvents = context.BattleEvents;

// ❌ BAD: Don't depend on the deprecated IEventApi
#pragma warning disable CS0618
var eventApi = context.Events;  // Obsolete!
#pragma warning restore CS0618
```

---

### 2. **Handle Missing Components**

```csharp
// ✅ GOOD: Check before accessing
if (context.Entities.TryGetComponent<PokemonData>(entity, out var data))
{
    Console.WriteLine($"Pokemon level: {data.Level}");
}

// ❌ BAD: Assumes component exists (throws if missing)
var data = context.Entities.GetComponent<PokemonData>(entity);  // May throw!
```

---

### 3. **Clean Up Event Subscriptions**

```csharp
public class MyMod : ICodeMod
{
    private EventHandler<BattleStartEventArgs> _battleHandler;

    public void OnInitialize(IModContext context)
    {
        _battleHandler = (s, e) => HandleBattle(e);
        context.BattleEvents.OnBattleStart += _battleHandler;
    }

    public void OnShutdown(IModContext context)
    {
        // Clean up to prevent memory leaks
        context.BattleEvents.OnBattleStart -= _battleHandler;
    }
}
```

---

### 4. **Use Configuration for User Settings**

```csharp
// Allow users to configure your mod
public void OnInitialize(IModContext context)
{
    var expMultiplier = context.Configuration.Get<float>("exp_multiplier", 1.5f);
    var enableShinyBoost = context.Configuration.Get<bool>("shiny_boost", true);

    // Users can edit mods/my-mod/config.json:
    // {
    //   "exp_multiplier": 2.0,
    //   "shiny_boost": false
    // }
}
```

---

## Security Restrictions

The ModAPI has the following security restrictions:

1. **No File I/O** (except through AssetApi)
2. **No Network Access**
3. **No Reflection** on game internals
4. **Memory Limits:** 100MB per mod
5. **Execution Timeout:** 5 seconds per event handler

Attempting to violate these restrictions will result in a `SecurityException`.

---

## Support

- **Documentation:** [PokeNET Mod Docs](https://github.com/pokenet/docs)
- **Examples:** `docs/examples/` directory
- **Discord:** [PokeNET Modding Community](https://discord.gg/pokenet)
- **Bug Reports:** [GitHub Issues](https://github.com/pokenet/issues)
