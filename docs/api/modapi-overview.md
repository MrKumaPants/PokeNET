# ModApi Overview

## Introduction

The PokeNET ModApi is the stable, versioned interface that mods use to interact with the game. It provides a safe, controlled API surface while protecting internal implementation details.

## Design Philosophy

### Stability First
- **Semantic Versioning**: Breaking changes increment major version
- **Backward Compatibility**: Old mods continue to work when possible
- **Deprecation Warnings**: Advanced notice before API removal
- **Migration Guides**: Clear upgrade paths for breaking changes

### Safety by Design
- **Controlled Access**: Mods cannot access dangerous APIs
- **Validation**: Input validation on all public methods
- **Isolation**: Mods cannot interfere with each other
- **Sandboxing**: Scripts run in restricted environments

### Performance Conscious
- **Minimal Overhead**: Thin wrapper over internal APIs
- **Batching Support**: Batch operations for efficiency
- **Async Operations**: Non-blocking for long-running tasks
- **Caching**: Smart caching for frequently accessed data

## API Layers

### 1. Core API
**Namespace**: `PokeNET.ModApi.Core`

Fundamental game interaction:
- Entity creation and manipulation
- Component access
- World queries
- Event subscription

```csharp
public interface IEntityApi
{
    Entity CreateEntity(string templateId);
    void DestroyEntity(Entity entity);
    bool HasComponent<T>(Entity entity) where T : struct;
    ref T GetComponent<T>(Entity entity) where T : struct;
    void AddComponent<T>(Entity entity, T component) where T : struct;
}
```

### 2. Data API
**Namespace**: `PokeNET.ModApi.Data`

Game data access and modification:
- Creature definitions
- Move data
- Item data
- Ability data

```csharp
public interface IDataApi
{
    CreatureDefinition GetCreature(string id);
    MoveDefinition GetMove(string id);
    ItemDefinition GetItem(string id);
    IEnumerable<T> GetAllDefinitions<T>() where T : IDefinition;
}
```

### 3. Asset API
**Namespace**: `PokeNET.ModApi.Assets`

Asset loading and management:
- Texture loading
- Audio loading
- Custom asset types
- Asset hot-reload support

```csharp
public interface IAssetApi
{
    Texture2D LoadTexture(string path);
    SoundEffect LoadSound(string path);
    T LoadData<T>(string path) where T : class;
    void RegisterAssetLoader<T>(IAssetLoader<T> loader);
}
```

### 4. Event API
**Namespace**: `PokeNET.ModApi.Events`

Game event subscription:
- Battle events
- Creature events
- Item events
- Custom events

```csharp
public interface IEventApi
{
    void Subscribe<T>(Action<T> handler) where T : IGameEvent;
    void Unsubscribe<T>(Action<T> handler) where T : IGameEvent;
    void Publish<T>(T gameEvent) where T : IGameEvent;
}
```

### 5. UI API
**Namespace**: `PokeNET.ModApi.UI`

User interface extensions:
- Custom UI elements
- Dialog windows
- HUD elements
- Menu entries

```csharp
public interface IUIApi
{
    void RegisterMenuEntry(string menuId, MenuEntry entry);
    IDialog CreateDialog(DialogOptions options);
    void ShowNotification(string message, NotificationType type);
}
```

### 6. Audio API
**Namespace**: `PokeNET.ModApi.Audio`

Audio system interaction:
- Music playback
- Sound effect playback
- Procedural music
- Audio events

```csharp
public interface IAudioApi
{
    void PlayMusic(string trackId, bool loop = true);
    void PlaySound(string soundId, float volume = 1.0f);
    void StopMusic(float fadeOutTime = 0.0f);
    IProceduralMusic CreateProceduralMusic(MusicSettings settings);
}
```

### 7. Scripting API
**Namespace**: `PokeNET.ModApi.Scripting`

For C# script mods:
- Script context
- Global objects
- Utilities
- Debugging

```csharp
public interface IScriptApi
{
    IEntityApi Entities { get; }
    IDataApi Data { get; }
    IEventApi Events { get; }
    ILogger Logger { get; }
    void Log(string message, LogLevel level = LogLevel.Information);
}
```

## API Versioning

### Version Format
ModApi uses semantic versioning: `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes, mods may need updates
- **MINOR**: New features, backward compatible
- **PATCH**: Bug fixes, fully compatible

### Version Declaration

```csharp
namespace PokeNET.ModApi
{
    [assembly: AssemblyVersion("1.0.0")]

    public static class ApiVersion
    {
        public const int Major = 1;
        public const int Minor = 0;
        public const int Patch = 0;

        public static Version Current => new(Major, Minor, Patch);
    }
}
```

### Version Checking

Mods can check API version compatibility:

```csharp
public class MyMod : IMod
{
    public void Initialize(IModContext context)
    {
        var requiredVersion = new Version(1, 0, 0);
        if (context.ApiVersion < requiredVersion)
        {
            throw new InvalidOperationException(
                $"This mod requires ModApi {requiredVersion} or higher");
        }
    }
}
```

## IMod Interface

All code mods must implement `IMod`:

```csharp
namespace PokeNET.ModApi
{
    public interface IMod
    {
        /// <summary>
        /// Mod metadata
        /// </summary>
        ModManifest Manifest { get; }

        /// <summary>
        /// Called when mod is loaded
        /// </summary>
        void Initialize(IModContext context);

        /// <summary>
        /// Called before game starts
        /// </summary>
        void OnGameStart(IGameContext context);

        /// <summary>
        /// Called when mod is unloaded
        /// </summary>
        void Shutdown();
    }
}
```

## IModContext

The context provided to mods during initialization:

```csharp
public interface IModContext
{
    /// <summary>
    /// Current ModApi version
    /// </summary>
    Version ApiVersion { get; }

    /// <summary>
    /// Mod's manifest data
    /// </summary>
    ModManifest Manifest { get; }

    /// <summary>
    /// Mod's root directory
    /// </summary>
    string ModDirectory { get; }

    /// <summary>
    /// Logger for this mod
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Configuration for this mod
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Core game APIs
    /// </summary>
    IEntityApi Entities { get; }
    IDataApi Data { get; }
    IAssetApi Assets { get; }
    IEventApi Events { get; }
    IUIApi UI { get; }
    IAudioApi Audio { get; }
}
```

## Example: Complete Mod Implementation

```csharp
using PokeNET.ModApi;
using PokeNET.ModApi.Core;
using PokeNET.ModApi.Events;

namespace MyCustomMod
{
    public class CustomMod : IMod
    {
        private IModContext _context;

        public ModManifest Manifest => new()
        {
            Id = "myname.custommod",
            Name = "Custom Mod",
            Version = new Version(1, 0, 0),
            Author = "My Name"
        };

        public void Initialize(IModContext context)
        {
            _context = context;
            context.Logger.LogInformation("Custom Mod initializing...");

            // Subscribe to events
            context.Events.Subscribe<BattleStartEvent>(OnBattleStart);

            // Register custom data
            RegisterCustomCreature();

            context.Logger.LogInformation("Custom Mod initialized!");
        }

        public void OnGameStart(IGameContext context)
        {
            _context.Logger.LogInformation("Game started with Custom Mod!");
        }

        public void Shutdown()
        {
            _context.Logger.LogInformation("Custom Mod shutting down...");

            // Cleanup
            _context.Events.Unsubscribe<BattleStartEvent>(OnBattleStart);
        }

        private void OnBattleStart(BattleStartEvent evt)
        {
            _context.Logger.LogInformation($"Battle started: {evt.BattleId}");
        }

        private void RegisterCustomCreature()
        {
            var creature = _context.Assets.LoadData<CreatureDefinition>(
                "Defs/Creatures/MyCreature.json");

            _context.Data.RegisterDefinition(creature);
        }
    }
}
```

## API Usage Guidelines

### 1. Always Use Dependency Injection

```csharp
// ✅ GOOD: Use injected services
public class MySystem
{
    private readonly IEntityApi _entities;

    public MySystem(IModContext context)
    {
        _entities = context.Entities;
    }
}

// ❌ BAD: Don't access globals or singletons
public class MySystem
{
    public void DoSomething()
    {
        var entity = GameGlobals.EntityManager.Create(); // Don't do this!
    }
}
```

### 2. Handle Errors Gracefully

```csharp
// ✅ GOOD: Handle exceptions
try
{
    var creature = context.Data.GetCreature("my_creature");
}
catch (KeyNotFoundException ex)
{
    context.Logger.LogWarning($"Creature not found: {ex.Message}");
    // Provide fallback or skip
}

// ❌ BAD: Let exceptions crash the game
var creature = context.Data.GetCreature("my_creature"); // Might throw!
```

### 3. Dispose Resources

```csharp
// ✅ GOOD: Dispose when done
public class MyMod : IMod
{
    private IDisposable _subscription;

    public void Initialize(IModContext context)
    {
        _subscription = context.Events.Subscribe<MyEvent>(Handler);
    }

    public void Shutdown()
    {
        _subscription?.Dispose();
    }
}
```

### 4. Use Async for Long Operations

```csharp
// ✅ GOOD: Async for slow operations
public async Task LoadAssetsAsync(IAssetApi assets)
{
    var texture = await assets.LoadTextureAsync("large_texture.png");
    var data = await assets.LoadDataAsync<MyData>("large_data.json");
}

// ❌ BAD: Blocking the game loop
public void LoadAssets(IAssetApi assets)
{
    var texture = assets.LoadTexture("large_texture.png"); // Blocks!
}
```

## API Restrictions

The following are **NOT** available to mods:

- ❌ Direct file system access (use AssetApi)
- ❌ Network access
- ❌ Process creation
- ❌ Direct graphics device access
- ❌ Low-level input handling
- ❌ Reflection on game internals
- ❌ Unsafe code
- ❌ Native interop

## Performance Best Practices

### 1. Cache Frequently Used Data

```csharp
// Cache definition lookups
private readonly Dictionary<string, CreatureDefinition> _creatureCache = new();

public CreatureDefinition GetCreature(string id)
{
    if (!_creatureCache.TryGetValue(id, out var creature))
    {
        creature = _context.Data.GetCreature(id);
        _creatureCache[id] = creature;
    }
    return creature;
}
```

### 2. Batch Operations

```csharp
// ✅ GOOD: Batch create entities
var entities = _context.Entities.CreateBatch(templateIds);

// ❌ BAD: Create one at a time
foreach (var id in templateIds)
{
    _context.Entities.CreateEntity(id); // Slow!
}
```

### 3. Use Queries Efficiently

```csharp
// ✅ GOOD: Single query with multiple components
var query = _context.Entities.Query<Position, Velocity, Sprite>();

// ❌ BAD: Multiple separate queries
var positions = _context.Entities.Query<Position>();
var velocities = _context.Entities.Query<Velocity>();
```

## Next Steps

- [Core Interfaces](core-interfaces.md) - Detailed interface documentation
- [Component Reference](components.md) - Available components
- [Event System](events.md) - Event types and usage
- [Code Mod Tutorial](../modding/code-mods.md) - Create your first code mod

---

*Last Updated: 2025-10-22 | ModApi Version: 1.0.0*
