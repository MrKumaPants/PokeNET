# SOLID Principles Implementation

PokeNET is built from the ground up following SOLID principles for clean, maintainable, and extensible code.

## Table of Contents

1. [Single Responsibility Principle](#single-responsibility-principle)
2. [Open/Closed Principle](#openclosed-principle)
3. [Liskov Substitution Principle](#liskov-substitution-principle)
4. [Interface Segregation Principle](#interface-segregation-principle)
5. [Dependency Inversion Principle](#dependency-inversion-principle)

---

## Single Responsibility Principle (SRP)

> "A class should have one, and only one, reason to change."

Each component in PokeNET has a single, well-defined responsibility.

### Example: ECS Components

```csharp
// ✅ GOOD: Each component represents ONE aspect
public struct Position
{
    public float X;
    public float Y;
}

public struct Velocity
{
    public float VX;
    public float VY;
}

public struct Health
{
    public int Current;
    public int Maximum;
}

// ❌ BAD: Multiple responsibilities
public struct CreatureData
{
    public float X, Y;           // Position responsibility
    public float VX, VY;         // Movement responsibility
    public int Health, MaxHealth; // Health responsibility
    public string Name;          // Identity responsibility
    // Too many reasons to change!
}
```

### Example: Systems

```csharp
// ✅ GOOD: MovementSystem only handles movement
public class MovementSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        var query = world.Query<Position, Velocity>();

        foreach (var entity in query)
        {
            ref var position = ref entity.Get<Position>();
            ref var velocity = ref entity.Get<Velocity>();

            position.X += velocity.VX * deltaTime;
            position.Y += velocity.VY * deltaTime;
        }
    }
}

// ✅ GOOD: RenderSystem only handles rendering
public class RenderSystem : ISystem
{
    private readonly SpriteBatch _spriteBatch;

    public void Draw(World world)
    {
        var query = world.Query<Position, Sprite>();

        _spriteBatch.Begin();
        foreach (var entity in query)
        {
            var position = entity.Get<Position>();
            var sprite = entity.Get<Sprite>();

            _spriteBatch.Draw(sprite.Texture,
                new Vector2(position.X, position.Y),
                Color.White);
        }
        _spriteBatch.End();
    }
}
```

### Benefits in PokeNET

- **Modular Design**: Systems can be added/removed independently
- **Easy Testing**: Test one responsibility at a time
- **Clear Ownership**: Each class has a clear purpose
- **Reduced Coupling**: Changes don't ripple across unrelated code

---

## Open/Closed Principle (OCP)

> "Software entities should be open for extension, but closed for modification."

PokeNET is designed to be extended through mods without modifying core code.

### Example: Asset Loading

```csharp
// Base abstraction (closed for modification)
public interface IAssetLoader<T>
{
    T Load(string path);
    bool CanLoad(string extension);
}

// Extended through new implementations (open for extension)
public class TextureLoader : IAssetLoader<Texture2D>
{
    public Texture2D Load(string path)
    {
        using var stream = File.OpenRead(path);
        return Texture2D.FromStream(GraphicsDevice, stream);
    }

    public bool CanLoad(string extension)
        => extension is ".png" or ".jpg" or ".bmp";
}

public class JsonDataLoader<T> : IAssetLoader<T> where T : class
{
    public T Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json);
    }

    public bool CanLoad(string extension)
        => extension == ".json";
}

// AssetManager uses loaders without knowing implementations
public class AssetManager
{
    private readonly List<object> _loaders = new();

    public void RegisterLoader<T>(IAssetLoader<T> loader)
    {
        _loaders.Add(loader);
    }

    public T LoadAsset<T>(string path)
    {
        var extension = Path.GetExtension(path);
        var loader = _loaders
            .OfType<IAssetLoader<T>>()
            .FirstOrDefault(l => l.CanLoad(extension));

        if (loader == null)
            throw new NotSupportedException($"No loader for {extension}");

        return loader.Load(path);
    }
}
```

### Example: Mod System Extension

```csharp
// Core mod interface (closed for modification)
public interface IMod
{
    ModManifest Manifest { get; }
    void Initialize(IModContext context);
    void Shutdown();
}

// Mods extend behavior (open for extension)
public class CustomBattleMod : IMod
{
    public ModManifest Manifest => new()
    {
        Id = "custom.battle",
        Name = "Custom Battle System"
    };

    public void Initialize(IModContext context)
    {
        // Extend battle system without modifying core
        context.Events.Subscribe<BattleStartEvent>(OnBattleStart);
    }

    private void OnBattleStart(BattleStartEvent evt)
    {
        // Custom battle logic
    }

    public void Shutdown()
    {
        // Cleanup
    }
}
```

### Benefits in PokeNET

- **Mod Support**: Add features without changing base game
- **Maintainability**: Core code stays stable
- **Versioning**: New features don't break old code
- **Safety**: Mods can't accidentally break core systems

---

## Liskov Substitution Principle (LSP)

> "Objects of a superclass should be replaceable with objects of a subclass without breaking the application."

Proper inheritance hierarchies ensure polymorphic behavior works correctly.

### Example: System Hierarchy

```csharp
// Base system contract
public interface ISystem
{
    void Update(World world, float deltaTime);
}

// All implementations are truly substitutable
public class MovementSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        // Movement logic - honors contract
        var query = world.Query<Position, Velocity>();
        foreach (var entity in query)
        {
            // Update positions
        }
    }
}

public class AISystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        // AI logic - honors contract
        var query = world.Query<AIComponent>();
        foreach (var entity in query)
        {
            // Update AI decisions
        }
    }
}

// System manager works with any ISystem
public class SystemManager
{
    private readonly List<ISystem> _systems = new();

    public void AddSystem(ISystem system)
    {
        _systems.Add(system);
    }

    public void UpdateAll(World world, float deltaTime)
    {
        // Any ISystem works here - LSP honored
        foreach (var system in _systems)
        {
            system.Update(world, deltaTime);
        }
    }
}
```

### Example: Event Handlers

```csharp
// Base event interface
public interface IGameEvent
{
    DateTime Timestamp { get; }
}

// All events are substitutable
public class BattleStartEvent : IGameEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string BattleId { get; set; }
}

public class ItemUsedEvent : IGameEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string ItemId { get; set; }
}

// Event system works with any IGameEvent
public class EventBus
{
    public void Publish<T>(T gameEvent) where T : IGameEvent
    {
        // All IGameEvent implementations work correctly
        _logger.Log($"Event at {gameEvent.Timestamp}: {typeof(T).Name}");
        // ... dispatch to handlers
    }
}
```

### Anti-Example: LSP Violation

```csharp
// ❌ BAD: Square violates LSP
public class Rectangle
{
    public virtual int Width { get; set; }
    public virtual int Height { get; set; }

    public int Area => Width * Height;
}

public class Square : Rectangle
{
    // Breaks LSP - changing width affects height!
    public override int Width
    {
        get => base.Width;
        set
        {
            base.Width = value;
            base.Height = value; // Side effect!
        }
    }
}

// ✅ GOOD: Separate types
public interface IShape
{
    int Area { get; }
}

public class Rectangle : IShape
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Area => Width * Height;
}

public class Square : IShape
{
    public int Size { get; set; }
    public int Area => Size * Size;
}
```

### Benefits in PokeNET

- **Polymorphism**: Systems work with abstractions
- **Reliability**: Substitutions don't break code
- **Predictability**: Derived classes behave as expected

---

## Interface Segregation Principle (ISP)

> "No client should be forced to depend on methods it does not use."

PokeNET uses small, focused interfaces instead of large, monolithic ones.

### Example: Focused Interfaces

```csharp
// ✅ GOOD: Small, focused interfaces
public interface IPositionable
{
    float X { get; set; }
    float Y { get; set; }
}

public interface IMovable
{
    void Move(float deltaX, float deltaY);
}

public interface IRenderable
{
    void Draw(SpriteBatch spriteBatch);
}

public interface IDamageable
{
    void TakeDamage(int amount);
    int CurrentHealth { get; }
}

// Entities implement only what they need
public class StaticSprite : IPositionable, IRenderable
{
    public float X { get; set; }
    public float Y { get; set; }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Draw logic
    }
    // No unnecessary methods!
}

public class MovingCreature : IPositionable, IMovable, IRenderable, IDamageable
{
    public float X { get; set; }
    public float Y { get; set; }

    public void Move(float deltaX, float deltaY)
    {
        X += deltaX;
        Y += deltaY;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Draw logic
    }

    public void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
    }

    public int CurrentHealth { get; private set; }
}

// ❌ BAD: Fat interface
public interface IGameEntity
{
    // Position
    float X { get; set; }
    float Y { get; set; }

    // Movement
    void Move(float deltaX, float deltaY);
    float Speed { get; set; }

    // Rendering
    void Draw(SpriteBatch spriteBatch);
    Texture2D Texture { get; set; }

    // Combat
    void TakeDamage(int amount);
    void Attack(IGameEntity target);
    int Health { get; set; }

    // AI
    void UpdateAI(float deltaTime);

    // Not all entities need all of these!
}
```

### Example: ModApi Segregation

```csharp
// ✅ GOOD: Segregated APIs
public interface IModContext
{
    // Each subsystem is separate
    IEntityApi Entities { get; }
    IDataApi Data { get; }
    IAssetApi Assets { get; }
    IEventApi Events { get; }
    IUIApi UI { get; }
    IAudioApi Audio { get; }
}

// Mods use only what they need
public class SimpleDataMod : IMod
{
    public void Initialize(IModContext context)
    {
        // Only uses Data API
        var creature = context.Data.GetCreature("my_creature");
        // Doesn't need to know about rendering, audio, etc.
    }
}

public class AudioMod : IMod
{
    public void Initialize(IModContext context)
    {
        // Only uses Audio API
        context.Audio.PlayMusic("my_track");
        // Doesn't need entity or data APIs
    }
}
```

### Benefits in PokeNET

- **Flexibility**: Implement only what's needed
- **Clarity**: Interfaces communicate intent clearly
- **Maintainability**: Changes don't affect unrelated code
- **Testability**: Mock only required dependencies

---

## Dependency Inversion Principle (DIP)

> "Depend on abstractions, not concretions."

High-level modules in PokeNET depend on interfaces, not concrete implementations.

### Example: Dependency Injection

```csharp
// ✅ GOOD: Depend on abstractions
public class BattleSystem
{
    private readonly ILogger<BattleSystem> _logger;
    private readonly IEventBus _events;
    private readonly IDataProvider _data;

    // Dependencies injected via constructor
    public BattleSystem(
        ILogger<BattleSystem> logger,
        IEventBus events,
        IDataProvider data)
    {
        _logger = logger;
        _events = events;
        _data = data;
    }

    public void StartBattle(string battleId)
    {
        _logger.LogInformation($"Starting battle: {battleId}");

        var battleData = _data.GetBattleData(battleId);

        _events.Publish(new BattleStartEvent
        {
            BattleId = battleId
        });
    }
}

// ❌ BAD: Depend on concretions
public class BattleSystemBad
{
    // Hard dependencies - can't test or swap!
    private readonly ConsoleLogger _logger = new();
    private readonly JsonDataProvider _data = new();

    public void StartBattle(string battleId)
    {
        _logger.Log($"Starting battle: {battleId}");
        var battleData = _data.LoadJson(battleId);
    }
}
```

### Example: Service Registration

```csharp
// Program.cs - Composition Root
var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        // Register abstractions with implementations
        services.AddSingleton<IAssetManager, AssetManager>();
        services.AddSingleton<IAudioManager, AudioManager>();
        services.AddSingleton<IModLoader, ModLoader>();
        services.AddScoped<IBattleSystem, BattleSystem>();

        // Implementations can be swapped without changing consumers
        services.AddSingleton<IDataProvider, JsonDataProvider>();
        // services.AddSingleton<IDataProvider, DatabaseDataProvider>(); // Easy swap!

        services.AddSingleton<PokeNETGame>();
    })
    .Build();
```

### Example: Testability

```csharp
// Easy to test with DIP
public class BattleSystemTests
{
    [Fact]
    public void StartBattle_PublishesEvent()
    {
        // Arrange - inject mocks
        var mockLogger = new Mock<ILogger<BattleSystem>>();
        var mockEvents = new Mock<IEventBus>();
        var mockData = new Mock<IDataProvider>();

        mockData.Setup(d => d.GetBattleData("test123"))
            .Returns(new BattleData { Id = "test123" });

        var system = new BattleSystem(
            mockLogger.Object,
            mockEvents.Object,
            mockData.Object);

        // Act
        system.StartBattle("test123");

        // Assert
        mockEvents.Verify(e => e.Publish(
            It.Is<BattleStartEvent>(evt => evt.BattleId == "test123")),
            Times.Once);
    }
}
```

### Example: Mod System DIP

```csharp
// Mods depend on abstractions
public class CustomMod : IMod
{
    public void Initialize(IModContext context)
    {
        // context is an abstraction
        // Actual implementation could change

        var entities = context.Entities; // IEntityApi
        var data = context.Data;         // IDataApi

        // Mod doesn't care about concrete implementations
    }
}

// Implementation can evolve
public class ModContext : IModContext
{
    public IEntityApi Entities { get; }
    public IDataApi Data { get; }

    public ModContext(
        IEntityApi entities,
        IDataApi data)
    {
        Entities = entities;
        Data = data;
    }
}
```

### Benefits in PokeNET

- **Testability**: Easy to mock dependencies
- **Flexibility**: Swap implementations without changing code
- **Modularity**: Components aren't tightly coupled
- **Maintainability**: Changes are isolated

---

## SOLID in Practice: Complete Example

Here's how all SOLID principles work together in a real PokeNET system:

```csharp
// SRP: Each interface has one responsibility
public interface ILogger { void Log(string message); }
public interface IDataLoader { T Load<T>(string id); }
public interface IEventPublisher { void Publish<T>(T evt); }

// OCP: Open for extension via interface implementation
public interface IAbilityEffect
{
    void Apply(BattleContext context);
}

// ISP: Small, focused interface
public interface IBattleParticipant
{
    string Id { get; }
    int CurrentHP { get; }
}

// DIP: Depend on abstractions
public class BattleEngine
{
    private readonly ILogger _logger;
    private readonly IDataLoader _data;
    private readonly IEventPublisher _events;
    private readonly Dictionary<string, IAbilityEffect> _abilities;

    // All dependencies injected
    public BattleEngine(
        ILogger logger,
        IDataLoader data,
        IEventPublisher events,
        IEnumerable<IAbilityEffect> abilities)
    {
        _logger = logger;
        _data = data;
        _events = events;
        _abilities = abilities.ToDictionary(a => a.GetType().Name);
    }

    // LSP: Works with any IBattleParticipant
    public void ApplyAbility(
        string abilityId,
        IBattleParticipant user,
        IBattleParticipant target)
    {
        _logger.Log($"{user.Id} uses {abilityId}");

        // OCP: New abilities added without modifying this code
        if (_abilities.TryGetValue(abilityId, out var ability))
        {
            var context = new BattleContext { User = user, Target = target };
            ability.Apply(context);

            _events.Publish(new AbilityUsedEvent
            {
                AbilityId = abilityId,
                UserId = user.Id
            });
        }
    }
}
```

## Summary

| Principle | Key Benefit | PokeNET Example |
|-----------|-------------|-----------------|
| **SRP** | Focused, maintainable classes | ECS components, specialized systems |
| **OCP** | Extension without modification | Mod system, asset loaders |
| **LSP** | Reliable polymorphism | System interfaces, event hierarchy |
| **ISP** | Minimal dependencies | Segregated ModApi interfaces |
| **DIP** | Loose coupling, testability | Dependency injection throughout |

## Next Steps

- [Design Patterns](design-patterns.md) - Common patterns used in PokeNET
- [ECS Architecture](ecs-architecture.md) - How SOLID applies to ECS
- [Code Style Guide](../developer/code-style.md) - Coding standards

---

*Last Updated: 2025-10-22*
