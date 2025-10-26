# Arch.Extended Quick Reference

## Installation

```xml
<!-- PokeNET.Core.csproj -->
<ItemGroup>
    <PackageReference Include="Arch" Version="2.1.0" />
    <PackageReference Include="Arch.System" Version="1.1.0" />
    <PackageReference Include="Arch.System.SourceGenerator" Version="2.1.0" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
    <PackageReference Include="Arch.EventBus" Version="1.0.2" />
    <PackageReference Include="Arch.Relationships" Version="1.0.0" />
    <PackageReference Include="Arch.Persistence" Version="2.0.0" />
    <PackageReference Include="Arch.LowLevel" Version="1.1.5" />
</ItemGroup>
```

```bash
dotnet restore
dotnet build
```

---

## System Basics

### Creating a System

```csharp
using Arch.System;

public partial class MySystem : BaseSystem<World, float>
{
    private readonly ILogger _logger;

    public MySystem(World world, ILogger logger) : base(world)
    {
        _logger = logger;
    }

    public override void Initialize()
    {
        _logger.LogInformation("System initialized");
    }

    public override void Update(in float deltaTime)
    {
        // Queries run automatically
    }
}
```

### System Lifecycle

```csharp
public override void Initialize() { }         // Once at start
public override void BeforeUpdate(in T) { }  // Before queries
public override void Update(in T) { }        // Main update
public override void AfterUpdate(in T) { }   // After queries
public override void Dispose() { }           // Cleanup
```

---

## Query Attributes

### Basic Query

```csharp
[Query]
[All<Position, Velocity>]
public void Move(ref Position pos, ref Velocity vel)
{
    pos.X += vel.X;
    pos.Y += vel.Y;
}
```

### Query with Entity

```csharp
[Query]
[All<Health>]
public void CheckDeath(Entity entity, ref Health health)
{
    if (health.HP <= 0)
    {
        World.Destroy(entity);
    }
}
```

### Query with Data Parameter

```csharp
[Query]
[All<Position, Velocity>]
public void Move(ref Position pos, ref Velocity vel, [Data] float deltaTime)
{
    pos.X += vel.X * deltaTime;
    pos.Y += vel.Y * deltaTime;
}

// In Update:
public override void Update(in float deltaTime)
{
    // Query methods automatically receive deltaTime
}
```

### Query Filters

```csharp
// All: Must have ALL components
[All<Position, Velocity, Sprite>]

// Any: Must have AT LEAST ONE
[Any<Player, Enemy, NPC>]

// None: Must NOT have any
[None<Dead, Disabled>]

// Exclusive: Must have EXACTLY these (no more)
[Exclusive<Position, Velocity>]
```

### Combined Filters

```csharp
[Query]
[All<Position, Velocity>]
[Any<Player, Enemy>]
[None<Dead, Frozen>]
public void ProcessMovement(ref Position pos, ref Velocity vel)
{
    // Entities with Position AND Velocity
    // AND (Player OR Enemy)
    // AND NOT Dead AND NOT Frozen
}
```

---

## System Groups

```csharp
using Arch.System;

var world = World.Create();

var gameplaySystems = new Group<float>(
    "Gameplay",
    new InputSystem(world, logger),
    new MovementSystem(world, logger),
    new BattleSystem(world, logger)
);

var renderSystems = new Group<float>(
    "Rendering",
    new RenderSystem(world, logger),
    new UISystem(world, logger)
);

// Lifecycle
gameplaySystems.Initialize();
renderSystems.Initialize();

// Update (each frame)
gameplaySystems.Update(in deltaTime);
renderSystems.Update(in deltaTime);

// Cleanup
gameplaySystems.Dispose();
renderSystems.Dispose();
```

---

## EventBus

### Setup

```csharp
using Arch.EventBus;

// Create event bus setup (once per project)
public partial class EventBusSetup
{
    [EventBus]
    public static partial void RegisterEvents();
}

// Initialize (in game startup)
EventBusSetup.RegisterEvents();
```

### Define Events

```csharp
// Events are structs
public struct PlayerDamagedEvent
{
    public Entity Player;
    public int Damage;
    public Entity Attacker;
}

public struct LevelUpEvent
{
    public Entity Entity;
    public int NewLevel;
}
```

### Subscribe to Events

```csharp
public partial class AudioSystem : BaseSystem<World, float>
{
    [EventHandler]
    public void OnPlayerDamaged(ref PlayerDamagedEvent evt)
    {
        PlaySound("hit.wav");
    }

    [EventHandler]
    public void OnLevelUp(ref LevelUpEvent evt)
    {
        PlaySound("levelup.wav");
    }
}
```

### Send Events

```csharp
EventBus.Send(new PlayerDamagedEvent
{
    Player = playerEntity,
    Damage = 10,
    Attacker = enemyEntity
});

EventBus.Send(new LevelUpEvent
{
    Entity = pokemonEntity,
    NewLevel = 25
});
```

---

## Relationships

### Define Relationship Types

```csharp
public struct ChildOf { }
public struct OwnedBy { }
public struct EquippedBy { }
```

### Add Relationships

```csharp
using Arch.Relationships;

// Player owns Pokemon
world.AddRelationship<ChildOf>(pokemonEntity, playerEntity);

// Item equipped by player
world.AddRelationship<EquippedBy>(itemEntity, playerEntity);
```

### Query Relationships

```csharp
// Get all children
var children = world.GetRelationships<ChildOf>(parentEntity);
foreach (var child in children)
{
    Console.WriteLine($"Child: {child}");
}

// Check if relationship exists
bool hasChild = world.HasRelationship<ChildOf>(childEntity, parentEntity);
```

### Remove Relationships

```csharp
world.RemoveRelationship<ChildOf>(childEntity, parentEntity);

// Remove all relationships of a type
world.RemoveAllRelationships<ChildOf>(parentEntity);
```

---

## Persistence

### JSON Serialization

```csharp
using Arch.Persistence;
using Arch.Persistence.Json;

// Save
var serializer = new JsonWorldSerializer();
string json = serializer.Serialize(world);
File.WriteAllText("save.json", json);

// Load
string json = File.ReadAllText("save.json");
var world = serializer.Deserialize(json);
```

### Binary Serialization

```csharp
using Arch.Persistence.Binary;

// Save
var serializer = new BinaryWorldSerializer();
byte[] data = serializer.Serialize(world);
File.WriteAllBytes("save.dat", data);

// Load
byte[] data = File.ReadAllBytes("save.dat");
var world = serializer.Deserialize(data);
```

### Partial Serialization

```csharp
// Save only specific entities
var playerQuery = new QueryDescription().WithAll<PlayerData>();

var entitiesToSave = new List<Entity>();
world.Query(in playerQuery, (Entity e) => entitiesToSave.Add(e));

string json = serializer.SerializeEntities(world, entitiesToSave);
```

---

## LowLevel Utilities

### UnsafeList

```csharp
using Arch.LowLevel;

public partial class MySystem : BaseSystem<World, float>
{
    private UnsafeList<int> _numbers = new(capacity: 100);

    public override void Update(in float deltaTime)
    {
        _numbers.Clear();
        _numbers.Add(42);
        _numbers.Add(100);

        for (int i = 0; i < _numbers.Length; i++)
        {
            Console.WriteLine(_numbers[i]);
        }
    }

    public override void Dispose()
    {
        _numbers.Dispose(); // REQUIRED
        base.Dispose();
    }
}
```

### UnsafeHashMap

```csharp
private UnsafeHashMap<int, Entity> _entityMap = new(capacity: 1000);

public override void Initialize()
{
    _entityMap.Add(123, someEntity);
}

public override void Update(in float deltaTime)
{
    if (_entityMap.TryGetValue(123, out var entity))
    {
        // Use entity
    }
}

public override void Dispose()
{
    _entityMap.Dispose(); // REQUIRED
}
```

---

## Common Patterns

### Movement System

```csharp
public partial class MovementSystem : BaseSystem<World, float>
{
    [Query]
    [All<Position, Velocity>]
    [None<Frozen>]
    public void Move(ref Position pos, ref Velocity vel, [Data] float dt)
    {
        pos.X += vel.X * dt;
        pos.Y += vel.Y * dt;
    }
}
```

### Damage System

```csharp
public partial class DamageSystem : BaseSystem<World, float>
{
    [Query]
    [All<Health, DamageQueue>]
    public void ApplyDamage(Entity entity, ref Health health, ref DamageQueue dmg)
    {
        health.HP -= dmg.TotalDamage;

        if (health.HP <= 0)
        {
            EventBus.Send(new EntityDiedEvent { Entity = entity });
        }

        dmg.Clear();
    }

    [EventHandler]
    public void OnEntityDied(ref EntityDiedEvent evt)
    {
        World.Destroy(evt.Entity);
    }
}
```

### Render System

```csharp
public partial class RenderSystem : BaseSystem<World, float>
{
    private readonly SpriteBatch _spriteBatch;

    [Query]
    [All<Position, Sprite>]
    public void Render(ref Position pos, ref Sprite sprite)
    {
        _spriteBatch.Draw(
            sprite.Texture,
            new Vector2(pos.X, pos.Y),
            Color.White
        );
    }
}
```

### Cleanup System

```csharp
public partial class CleanupSystem : BaseSystem<World, float>
{
    private UnsafeList<Entity> _toDestroy = new(capacity: 100);

    [Query]
    [All<DestroyTag>]
    public void MarkForDestruction(Entity entity)
    {
        _toDestroy.Add(entity);
    }

    public override void AfterUpdate(in float deltaTime)
    {
        for (int i = 0; i < _toDestroy.Length; i++)
        {
            World.Destroy(_toDestroy[i]);
        }
        _toDestroy.Clear();
    }

    public override void Dispose()
    {
        _toDestroy.Dispose();
        base.Dispose();
    }
}
```

---

## Performance Tips

### DO:
- ✅ Use `ref` for component parameters
- ✅ Cache queries with `[Query]` attributes
- ✅ Use `UnsafeList` for temporary collections
- ✅ Use `struct` for events and components
- ✅ Use `[None<T>]` to filter out unwanted entities

### DON'T:
- ❌ Create `QueryDescription` in Update loop
- ❌ Use `class` for components or events
- ❌ Allocate collections every frame
- ❌ Forget to `Dispose()` LowLevel collections
- ❌ Access `World` in constructor

---

## Debugging

### Enable Logging

```csharp
public MySystem(World world, ILogger<MySystem> logger) : base(world)
{
    _logger = logger;
}

[Query]
[All<Position>]
public void ProcessPosition(Entity entity, ref Position pos)
{
    _logger.LogDebug($"Entity {entity}: Position ({pos.X}, {pos.Y})");
}
```

### Check Generated Code

```bash
# Build with generated file output
dotnet build /p:EmitCompilerGeneratedFiles=true

# Check generated files
ls obj/Debug/net9.0/generated/Arch.System.SourceGenerator/
```

### Profiling

```csharp
using System.Diagnostics;

public override void Update(in float deltaTime)
{
    var sw = Stopwatch.StartNew();
    base.Update(in deltaTime);
    sw.Stop();

    if (sw.ElapsedMilliseconds > 16)
    {
        _logger.LogWarning($"{GetType().Name} took {sw.ElapsedMilliseconds}ms");
    }
}
```

---

## Cheat Sheet

| Feature | Package | Key Type/Attribute |
|---------|---------|-------------------|
| Systems | Arch.System | `BaseSystem<W,T>` |
| Queries | Arch.System.SourceGenerator | `[Query]`, `[All<T>]` |
| Events | Arch.EventBus | `[EventBus]`, `[EventHandler]` |
| Relationships | Arch.Relationships | `AddRelationship<T>()` |
| Persistence | Arch.Persistence | `JsonWorldSerializer` |
| LowLevel | Arch.LowLevel | `UnsafeList<T>` |

---

**Quick Reference Version:** 1.0.0
**For Full Documentation:** See `arch-extended-integration-guide.md`
