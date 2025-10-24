# PokeNET Architecture Documentation

## Executive Summary

PokeNET is a Pokemon-inspired game engine built with modern C# architecture principles. The system uses an Entity Component System (ECS) for game object management, dependency injection for services, and a modular plugin architecture for extensibility.

**Key Technologies:**
- **ECS Framework:** Arch.Core (high-performance archetype-based ECS)
- **Rendering:** MonoGame (cross-platform game framework)
- **Audio:** DryWetMidi + custom reactive audio engine
- **Modding:** Harmony for runtime patching + plugin system
- **Scripting:** Roslyn for C# script compilation with sandboxing

---

## System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     PokeNET.DesktopGL                           │
│                   (Entry Point / MonoGame)                      │
└────────────────────────┬────────────────────────────────────────┘
                         │
        ┌────────────────┴────────────────┐
        │                                 │
┌───────▼────────┐              ┌────────▼────────┐
│  PokeNET.Core  │              │ PokeNET.Domain  │
│   (Systems)    │◄─────────────┤ (ECS Components │
│                │              │   & Interfaces) │
└───────┬────────┘              └─────────────────┘
        │
        ├──────┬──────┬──────┬──────┬──────┐
        │      │      │      │      │      │
    ┌───▼──┐ ┌▼────┐ ┌▼────┐ ┌▼──┐ ┌▼───┐ ┌▼─────┐
    │Audio │ │Mod  │ │Save │ │UI │ │Net │ │Script│
    │      │ │API  │ │     │ │   │ │    │ │      │
    └──────┘ └─────┘ └─────┘ └───┘ └────┘ └──────┘
```

---

## 1. Entity Component System (ECS)

### Architecture Pattern: Archetype-Based ECS

PokeNET uses **Arch.Core**, an archetype-based ECS that groups entities by component composition for cache-friendly iteration.

**Key Concepts:**
- **Entity:** Lightweight ID (int wrapper)
- **Component:** Pure data struct (no behavior)
- **System:** Behavior that operates on entities with specific components
- **Archetype:** Internal storage for entities with identical component combinations

### Core Components

#### Pokemon Components

```csharp
// Core identity and progression
public struct PokemonData
{
    public int SpeciesId;           // Pokedex number (1-1025)
    public string? Nickname;        // Custom name (max 12 chars)
    public int Level;               // 1-100
    public Nature Nature;           // Stat growth modifier
    public Gender Gender;           // Male/Female/Unknown
    public bool IsShiny;            // Shiny coloration
    public int ExperiencePoints;    // Current EXP
    public int ExperienceToNextLevel;
    public int OriginalTrainerId;   // For traded Pokemon
    public int FriendshipLevel;     // 0-255
}

// Battle statistics
public struct PokemonStats
{
    public int MaxHP;
    public int HP;
    public int Attack;
    public int Defense;
    public int SpAttack;
    public int SpDefense;
    public int Speed;

    // Hidden stats for calculation
    public int IV_HP, IV_Attack, IV_Defense, IV_SpAttack, IV_SpDefense, IV_Speed;
    public int EV_HP, EV_Attack, EV_Defense, EV_SpAttack, EV_SpDefense, EV_Speed;

    // Stat calculation methods (Pokemon formula)
    public int CalculateHP(int base, int level);
    public int CalculateStat(int base, int iv, int ev, int level, float natureMod);
}

// Battle state tracking
public struct BattleState
{
    public BattleStatus Status;      // Ready/InBattle/Fainted
    public int TurnCounter;
    public int LastDamageTaken;
    public int LastMoveUsed;

    // Stat stage modifiers (-6 to +6)
    public int AttackStage;
    public int DefenseStage;
    public int SpAttackStage;
    public int SpDefenseStage;
    public int SpeedStage;
    public int AccuracyStage;
    public int EvasionStage;

    public float GetStageMultiplier(int stage);
}

// Move set (4 moves max)
public struct MoveSet
{
    private Move? _move1, _move2, _move3, _move4;

    public void LearnMove(Move move, int slot);
    public Move? GetMove(int slot);
    public bool HasMove(int moveId);
}

// Status conditions
public struct StatusCondition
{
    public ConditionType Status;     // None/Poison/Burn/Paralysis/Freeze/Sleep
    public int TurnsActive;

    public bool CanAct();            // False if frozen/asleep
    public int StatusTick(int maxHP); // Damage from poison/burn
}
```

#### Spatial Components (Grid-Based Movement)

```csharp
// Grid position (tile-based)
public struct GridPosition
{
    public int X;
    public int Y;
    public int Z;  // For layering (bridges, caves)
}

// Movement state
public struct MovementState
{
    public bool IsMoving;
    public int TargetX;
    public int TargetY;
    public float MovementProgress;  // 0.0 to 1.0
    public Direction FacingDirection;
}

// Pixel-level velocity for smooth animation
public struct PixelVelocity
{
    public float VelocityX;
    public float VelocityY;
}

// Direction enum
public enum Direction
{
    North, South, East, West
}
```

#### Rendering Components

```csharp
public struct Sprite
{
    public string TexturePath;
    public int Width;
    public int Height;
    public int Layer;              // Render layer (0 = background, 1 = entities, 2 = UI)
    public int FrameIndex;         // For sprite animations
    public float AnimationSpeed;
}

public struct Camera
{
    public float X;
    public float Y;
    public float Zoom;
}
```

#### Player Components

```csharp
public struct PlayerProgress
{
    public int BadgesEarned;
    public List<int> DefeatedTrainers;
    public bool[] StoryFlags;
}

public struct Pokedex
{
    public HashSet<int> Seen;
    public HashSet<int> Caught;
    public int GetCompletionPercentage();
}

public struct Inventory
{
    public Dictionary<int, int> Items;  // ItemId -> Quantity
    public int AddItem(int itemId, int quantity);
    public bool RemoveItem(int itemId, int quantity);
}

public struct Party
{
    private Entity[] _pokemon;  // Max 6 Pokemon
    public int Count { get; }

    public void AddPokemon(Entity pokemon);
    public void RemovePokemon(Entity pokemon);
    public Entity GetPokemon(int index);
}
```

---

### Core Systems

Systems implement `SystemBase` and operate on component queries.

```csharp
public abstract class SystemBase : ISystem
{
    protected ILogger Logger { get; }
    protected World World { get; private set; }

    public abstract int Priority { get; }  // Execution order (lower = earlier)

    public void SetWorld(World world);
    public void Initialize();
    public void Update(float deltaTime);
    public void Shutdown();

    protected abstract void OnInitialize();
    protected abstract void OnUpdate(float deltaTime);
    protected abstract void OnShutdown();
}
```

#### BattleSystem

```csharp
public class BattleSystem : SystemBase
{
    public override int Priority => 50;

    protected override void OnInitialize()
    {
        // Query: PokemonData + PokemonStats + BattleState + MoveSet
        _battleQuery = new QueryDescription()
            .WithAll<PokemonData, PokemonStats, BattleState, MoveSet>();
    }

    protected override void OnUpdate(float deltaTime)
    {
        // 1. Collect all Pokemon in battle
        // 2. Sort by Speed stat (faster acts first)
        // 3. Process each battler's turn
        // 4. Apply status effects
        // 5. Check victory/defeat conditions
    }

    public bool ExecuteMove(Entity attacker, Entity defender, int moveId)
    {
        // 1. Find move in attacker's moveset
        // 2. Calculate damage (Pokemon formula)
        // 3. Apply damage
        // 4. Check if defender fainted
        // 5. Award experience if defeated
    }

    private int CalculateDamage(/* parameters */)
    {
        // Official Pokemon damage formula:
        // Damage = ((((2 * Level / 5) + 2) * Power * A/D) / 50) + 2) * Modifiers
        // Modifiers: STAB, Type Effectiveness, Critical Hit, Random (0.85-1.0)
    }
}
```

#### MovementSystem

```csharp
public class MovementSystem : SystemBase
{
    public override int Priority => 10;  // Early (before rendering)

    protected override void OnUpdate(float deltaTime)
    {
        // Query: GridPosition + MovementState + PixelVelocity

        // For each moving entity:
        // 1. Interpolate position towards target
        // 2. Update MovementProgress
        // 3. When complete, snap to GridPosition
        // 4. Set IsMoving = false
    }
}
```

#### RenderSystem

```csharp
public class RenderSystem : SystemBase
{
    public override int Priority => 100;  // Late (after all logic)

    protected override void OnUpdate(float deltaTime)
    {
        // Query: Sprite + GridPosition (or Sprite + PixelPosition)

        // 1. Sort entities by Layer and Y position
        // 2. Apply camera transform
        // 3. Render sprites to screen
        // 4. Update sprite animations
    }
}
```

#### InputSystem

```csharp
public class InputSystem : SystemBase
{
    protected override void OnUpdate(float deltaTime)
    {
        // Query: Player components + MovementState

        // 1. Read keyboard/gamepad input
        // 2. Set MovementState.IsMoving and Target
        // 3. Trigger menu open/close events
        // 4. Handle interaction (talk, examine)
    }
}
```

---

## 2. Audio System Architecture

### Reactive Audio Engine

The audio system uses **event-driven reactive patterns** to automatically respond to game events.

```
┌─────────────────────────────────────────────────────────┐
│              ReactiveAudioEngine                        │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │          AudioReactionConfiguration              │  │
│  │  ┌────────────┐  ┌────────────┐  ┌────────────┐ │  │
│  │  │ BattleStart│  │   Victory  │  │  Location  │ │  │
│  │  │  → Music   │  │  → Jingle  │  │  → Ambient │ │  │
│  │  └────────────┘  └────────────┘  └────────────┘ │  │
│  └──────────────────────────────────────────────────┘  │
│                          ▲                              │
│                          │ Events                       │
│                   ┌──────┴────────┐                     │
│                   │   EventBus    │                     │
│                   └───────────────┘                     │
└─────────────────────────────────────────────────────────┘
            │                          │
    ┌───────▼────────┐        ┌───────▼────────┐
    │  MusicPlayer   │        │ SoundEffectPl  │
    │  (MIDI/Audio)  │        │ (WAV/OGG)      │
    └────────────────┘        └────────────────┘
                  │
          ┌───────▼────────┐
          │  AudioMixer    │
          │  (Ducking/Vol) │
          └────────────────┘
```

**Key Classes:**
- **AudioManager:** Facade coordinating all audio components
- **MusicPlayer:** MIDI playback with crossfading
- **SoundEffectPlayer:** Short audio clips (attack sounds, UI beeps)
- **AudioMixer:** Volume control, ducking, channel management
- **ReactiveAudioEngine:** Event subscriptions and automatic reactions
- **ProceduralMusicGenerator:** MIDI generation for dynamic music

**Features:**
- Crossfade transitions between tracks (1 second default)
- Music ducking for dialogue/UI sounds
- Event-driven reactions (battle start → battle theme)
- Procedural MIDI generation for unique tracks
- Multi-channel mixing (Master, Music, SFX, Voice, Ambient)

---

## 3. Modding System

### Plugin Architecture

```
┌──────────────────────────────────────────────────────┐
│                  IModLoader                          │
│                                                      │
│  ┌────────────────┐  ┌────────────────────────────┐ │
│  │  ModRegistry   │  │   HarmonyPatcher           │ │
│  │  (Discovery)   │  │   (Runtime Patching)       │ │
│  └────────────────┘  └────────────────────────────┘ │
└──────────────────────────────────────────────────────┘
            │                          │
    ┌───────▼──────────┐     ┌────────▼─────────┐
    │  IContentMod     │     │   ICodeMod       │
    │  (JSON/Assets)   │     │   (C# Patches)   │
    └──────────────────┘     └──────────────────┘
```

### Mod Manifest

```json
{
  "id": "example-mod",
  "name": "Example Pokemon Mod",
  "version": "1.0.0",
  "author": "Modder Name",
  "description": "Adds new Pokemon and moves",
  "dependencies": {
    "core": ">=1.0.0",
    "another-mod": "1.2.0"
  },
  "contentPacks": [
    "data/pokemon.json",
    "data/moves.json"
  ],
  "assemblyName": "ExampleMod.dll"
}
```

### ModAPI (Interface Segregation Principle)

Instead of one monolithic `IModApi`, we provide focused interfaces:

```csharp
public interface IModContext
{
    IEntityApi Entities { get; }
    IAssetApi Assets { get; }
    IGameplayEvents GameplayEvents { get; }
    IBattleEvents BattleEvents { get; }
    IUIEvents UIEvents { get; }
    ISaveEvents SaveEvents { get; }
    IModEvents ModEvents { get; }
    IConfigurationApi Configuration { get; }
}

// Entity manipulation
public interface IEntityApi
{
    Entity CreateEntity(params object[] components);
    void DestroyEntity(Entity entity);
    ref T GetComponent<T>(Entity entity);
    IEntityQuery<T1, T2, T3> Query<T1, T2, T3>();
}

// Asset loading
public interface IAssetApi
{
    T LoadAsset<T>(string path);
    void RegisterAssetLoader<T>(IAssetLoader<T> loader);
}

// Event subscriptions
public interface IBattleEvents
{
    event EventHandler<BattleStartEventArgs> OnBattleStart;
    event EventHandler<DamageCalculatedEventArgs> OnDamageCalculated;
    event EventHandler<CreatureFaintedEventArgs> OnCreatureFainted;
}
```

### Harmony Patching Example

```csharp
public class DoubleExpMod : ICodeMod
{
    public void OnInitialize(IModContext context)
    {
        var harmony = new Harmony("example.double-exp");

        // Patch BattleSystem.AwardExperience
        harmony.Patch(
            original: typeof(BattleSystem).GetMethod("AwardExperience"),
            prefix: new HarmonyMethod(typeof(DoubleExpMod).GetMethod("PrefixAwardExp"))
        );
    }

    public static void PrefixAwardExp(ref int experienceGained)
    {
        experienceGained *= 2;  // Double all experience
    }
}
```

---

## 4. Scripting System

### Roslyn-based C# Scripting

Scripts are compiled at runtime using Roslyn with sandboxing for security.

```csharp
public class ScriptingEngine
{
    private readonly CSharpCompilation _compilation;
    private readonly ScriptSandbox _sandbox;

    public async Task<object?> ExecuteScriptAsync(string scriptCode, IModContext context)
    {
        // 1. Parse script code
        var syntaxTree = CSharpSyntaxTree.ParseText(scriptCode);

        // 2. Validate against whitelist
        _sandbox.ValidateScript(syntaxTree);

        // 3. Compile
        var assembly = _compilation.Compile(syntaxTree);

        // 4. Execute in restricted AppDomain
        return await assembly.EntryPoint.InvokeAsync(context);
    }
}
```

**Sandbox Restrictions:**
- No file I/O (except through AssetApi)
- No network access
- No reflection on game internals
- Whitelist of allowed types and methods
- Memory limits per script

---

## 5. Saving System

### Save Data Structure

```json
{
  "version": "1.0.0",
  "playerName": "Ash",
  "playTime": "12:34:56",
  "entities": [
    {
      "id": 123,
      "components": {
        "PokemonData": { "speciesId": 25, "level": 15 },
        "GridPosition": { "x": 10, "y": 20 }
      }
    }
  ],
  "modData": {
    "example-mod": { "customField": "value" }
  }
}
```

### Save System Architecture

```csharp
public interface ISaveSystem
{
    Task SaveGameAsync(string saveName);
    Task<SaveData> LoadGameAsync(string saveName);
}

public class SaveSystem : ISaveSystem
{
    private readonly IEntitySerializer _entitySerializer;
    private readonly IEventBus _eventBus;

    public async Task SaveGameAsync(string saveName)
    {
        // 1. Fire OnSaving event (mods can add data)
        var savingArgs = new SavingEventArgs { ModData = new Dictionary<string, object>() };
        _eventBus.Publish(savingArgs);

        // 2. Serialize all entities with SaveableTag component
        var entities = SerializeEntities();

        // 3. Write to JSON file
        var saveData = new SaveData { Entities = entities, ModData = savingArgs.ModData };
        await File.WriteAllTextAsync($"saves/{saveName}.json", JsonSerializer.Serialize(saveData));

        // 4. Fire OnSaved event
        _eventBus.Publish(new SavedEventArgs { Success = true });
    }
}
```

---

## 6. Performance Optimizations

### ECS Query Optimization

- **Archetype iteration:** Arch.Core groups entities by component composition, allowing linear iteration
- **Cache locality:** Components of same type stored contiguously in memory
- **Query caching:** Reuse `QueryDescription` objects to avoid re-parsing

```csharp
// ✅ GOOD: Cache query description
private QueryDescription _query;

protected override void OnInitialize()
{
    _query = new QueryDescription().WithAll<A, B, C>();
}

protected override void OnUpdate(float deltaTime)
{
    World.Query(in _query, (Entity e, ref A a, ref B b) => { /* fast */ });
}

// ❌ BAD: Create query every frame
protected override void OnUpdate(float deltaTime)
{
    var query = new QueryDescription().WithAll<A, B, C>();  // Slow!
    World.Query(in query, ...);
}
```

### Struct Components (Value Types)

All components are structs to avoid heap allocation and GC pressure.

```csharp
// ✅ GOOD: Struct component
public struct PokemonData
{
    public int SpeciesId;
    public int Level;
}

// ❌ BAD: Class component (heap allocation)
public class PokemonData  // Don't do this!
{
    public int SpeciesId;
    public int Level;
}
```

### Batch Operations

```csharp
// Create many entities efficiently
var entities = _world.CreateBulk(
    new ComponentType[] { typeof(PokemonData), typeof(GridPosition) },
    1000  // Create 1000 entities at once
);
```

---

## 7. Dependency Injection

Services are registered in `Program.cs` and injected into systems.

```csharp
var services = new ServiceCollection();

// Core services
services.AddSingleton<World>(_ => World.Create());
services.AddSingleton<IEventBus, EventBus>();

// Systems (transient to avoid singleton state issues)
services.AddTransient<BattleSystem>();
services.AddTransient<MovementSystem>();
services.AddTransient<RenderSystem>();

// Audio
services.AddSingleton<IAudioManager, AudioManager>();
services.AddSingleton<IMusicPlayer, MusicPlayer>();
services.AddSingleton<IAudioMixer, AudioMixer>();

// Modding
services.AddSingleton<IModLoader, ModLoader>();

var provider = services.BuildServiceProvider();
```

---

## 8. Testing Strategy

### Test Pyramid

```
        ┌─────────────┐
        │   E2E Tests │  ← Integration/End-to-End
        └─────────────┘
      ┌─────────────────┐
      │ Integration Tests│  ← System interactions
      └─────────────────┘
    ┌─────────────────────┐
    │    Unit Tests       │  ← Individual systems/components
    └─────────────────────┘
```

**Test Categories:**
1. **Unit Tests:** Individual systems (BattleSystem, MovementSystem, etc.)
2. **Integration Tests:** Multiple systems working together
3. **Regression Tests:** Prevent fixed bugs from reoccurring
4. **End-to-End Tests:** Complete feature workflows

### Example Test Structure

```csharp
public class BattleSystemTests
{
    [Fact]
    public void ExecuteMove_WithValidAttacker_DealsDamage()
    {
        // Arrange
        var world = World.Create();
        var battleSystem = new BattleSystem(Mock.Of<ILogger<BattleSystem>>());
        battleSystem.SetWorld(world);

        var attacker = CreatePokemon(world, level: 20, attack: 50);
        var defender = CreatePokemon(world, level: 15, hp: 50);

        // Act
        battleSystem.ExecuteMove(attacker, defender, moveId: 33);  // Tackle

        // Assert
        ref var defenderStats = ref world.Get<PokemonStats>(defender);
        defenderStats.HP.Should().BeLessThan(50);
    }
}
```

---

## 9. Migration from Old Architecture

### Physics → Grid-Based Movement

**Old (Physics-based):**
```csharp
// Used continuous physics
public struct Velocity
{
    public float VelocityX;
    public float VelocityY;
}

public struct Position
{
    public float X;
    public float Y;
}
```

**New (Grid-based):**
```csharp
// Tile-based movement
public struct GridPosition
{
    public int X;
    public int Y;
}

public struct MovementState
{
    public bool IsMoving;
    public int TargetX;
    public int TargetY;
}
```

**Migration Strategy:**
1. Replace `Position` with `GridPosition`
2. Replace `Velocity` with `MovementState + PixelVelocity`
3. Update MovementSystem to interpolate grid movement
4. Remove physics components (Rigidbody, Collider)

---

## 10. Future Architecture Improvements

### Planned Features

1. **Network Multiplayer:**
   - Client-server architecture
   - Server-authoritative battle logic
   - Client prediction for movement
   - ECS state synchronization

2. **Advanced AI:**
   - Behavior trees for trainer AI
   - Utility-based move selection
   - Team composition analysis

3. **Performance:**
   - Job system for parallel ECS queries
   - Burst compilation for hot paths
   - Texture atlasing for rendering

4. **Tooling:**
   - Visual entity inspector
   - Performance profiler
   - Mod development tools

---

## Appendix: Component Reference

### Complete Component List

| Component | Purpose | Systems Using It |
|-----------|---------|------------------|
| PokemonData | Species, level, identity | BattleSystem, UI |
| PokemonStats | HP, Attack, Defense, etc. | BattleSystem |
| BattleState | Turn state, stat stages | BattleSystem |
| MoveSet | 4 moves max | BattleSystem |
| StatusCondition | Poison, burn, etc. | BattleSystem |
| GridPosition | Tile coordinates | MovementSystem, RenderSystem |
| MovementState | Movement progress | MovementSystem |
| PixelVelocity | Smooth animation | MovementSystem |
| Sprite | Texture, animation | RenderSystem |
| Camera | Viewport position | RenderSystem |
| Inventory | Items owned | UI, ItemSystem |
| Party | 6 Pokemon team | BattleSystem, UI |
| PlayerProgress | Badges, story flags | SaveSystem |
| Pokedex | Seen/caught Pokemon | UI, SaveSystem |

---

## Diagrams

### Component Dependency Graph

```
PokemonData ──────► BattleState ──────► StatusCondition
     │                   │
     │                   │
     ▼                   ▼
PokemonStats ────────► MoveSet
     │
     │
     ▼
GridPosition ────────► Sprite
     │                   │
     │                   │
     ▼                   ▼
MovementState       RenderSystem
```

### System Execution Order

```
Priority 0:  EventBus.ProcessEvents()
Priority 10: InputSystem.Update()
Priority 20: MovementSystem.Update()
Priority 50: BattleSystem.Update()
Priority 80: AudioSystem.Update()
Priority 100: RenderSystem.Update()
Priority 120: UISystem.Update()
```

---

## References

- [Arch.Core Documentation](https://github.com/genaray/Arch)
- [MonoGame Documentation](https://docs.monogame.net/)
- [Harmony Documentation](https://harmony.pardeike.net/)
- [Roslyn Scripting API](https://github.com/dotnet/roslyn/wiki/Scripting-API-Samples)
- [Pokemon Damage Calculator](https://bulbapedia.bulbagarden.net/wiki/Damage)
