# Entity Factory Pattern Implementation

## Overview

The Entity Factory pattern provides a structured, maintainable way to create entities in the PokeNET ECS architecture. It follows the **Open/Closed Principle** - the system is open for extension (create new factories) but closed for modification (existing code remains stable).

## Architecture

### Layer Organization

```
Domain Layer (Interfaces & Contracts)
├── IEntityFactory                 # Factory contract
├── EntityDefinition               # Immutable entity template
└── EntityCreatedEvent             # Creation notification

Core Layer (Implementations)
├── EntityFactory                  # Base implementation
├── PlayerEntityFactory            # Player-specific factory
├── EnemyEntityFactory             # Enemy-specific factory
├── ItemEntityFactory              # Item-specific factory
├── ProjectileEntityFactory        # Projectile-specific factory
└── TemplateLoader                 # JSON template support
```

### Component Diagram

```
┌─────────────────────────────────────────────────────┐
│                 IEntityFactory                      │
│  + Create(world, definition)                       │
│  + CreateFromTemplate(world, name)                 │
│  + RegisterTemplate(name, definition)              │
└─────────────────────────────────────────────────────┘
                         △
                         │ implements
                         │
┌─────────────────────────────────────────────────────┐
│              EntityFactory (Base)                   │
│  - templates: Dictionary<string, EntityDefinition>  │
│  - eventBus: IEventBus                             │
│  # ValidateComponents(components)                   │
└─────────────────────────────────────────────────────┘
                         △
                         │ extends
         ┌───────────────┼───────────────┬───────────┐
         │               │               │           │
┌────────────────┐ ┌──────────────┐ ┌─────────┐ ┌──────────┐
│ PlayerFactory  │ │ EnemyFactory │ │ItemFac..│ │Projectile│
│ + CreateBasic  │ │ + CreateWeak │ │+ Create │ │+ Create  │
│ + CreateFast   │ │ + CreateElite│ │  Potion │ │  Bullet  │
│ + CreateTank   │ │ + CreateBoss │ │+ Create │ │+ Create  │
└────────────────┘ └──────────────┘ │  Coin   │ │  Arrow   │
                                    └─────────┘ └──────────┘
```

## Key Benefits

### 1. **Separation of Concerns**
- Entity creation logic separated from game logic
- Easy to modify entity configurations without touching systems
- Centralized validation and error handling

### 2. **Open/Closed Principle**
- Add new entity types by creating new factories
- Existing factories remain unchanged
- No risk of breaking existing entity creation

### 3. **Reusability**
- Templates can be registered once and reused
- Consistent entity creation across the codebase
- Easy to share configurations between systems

### 4. **Testability**
- Factories can be easily mocked
- Entity creation logic isolated for unit testing
- Validation logic testable independently

### 5. **Maintainability**
- Entity configurations in one place
- Easy to understand entity structure
- Clear documentation of component requirements

## Usage Examples

### Basic Entity Creation

```csharp
// Using EntityFactory directly
var factory = new EntityFactory(logger, eventBus);

var definition = new EntityDefinition(
    "CustomEntity",
    new object[]
    {
        new Position(100, 200),
        new Velocity(50, 0),
        new Health(100)
    }
);

var entity = factory.Create(world, definition);
```

### Using Specialized Factories

```csharp
// Player creation
var playerFactory = new PlayerEntityFactory(logger, eventBus);
var player = playerFactory.CreateBasicPlayer(world, new Vector2(100, 100));
var fastPlayer = playerFactory.CreateFastPlayer(world, new Vector2(200, 200));
var tankPlayer = playerFactory.CreateTankPlayer(world, new Vector2(300, 300));

// Enemy creation
var enemyFactory = new EnemyEntityFactory(logger, eventBus);
var weakEnemy = enemyFactory.CreateWeakEnemy(world, new Vector2(500, 100));
var eliteEnemy = enemyFactory.CreateEliteEnemy(world, new Vector2(600, 200));
var boss = enemyFactory.CreateBossEnemy(world, new Vector2(700, 300), "DragonKing");

// Item creation
var itemFactory = new ItemEntityFactory(logger, eventBus);
var potion = itemFactory.CreateHealthPotion(world, new Vector2(50, 50), healAmount: 50);
var coin = itemFactory.CreateCoin(world, new Vector2(60, 60), value: 10);
var speedBoost = itemFactory.CreateSpeedBoost(world, new Vector2(70, 70), duration: 5f);

// Projectile creation
var projectileFactory = new ProjectileEntityFactory(logger, eventBus);
var bullet = projectileFactory.CreateBullet(world, playerPos, direction, speed: 400f);
var arrow = projectileFactory.CreateArrow(world, playerPos, direction, speed: 300f);
var fireball = projectileFactory.CreateFireball(world, playerPos, direction, speed: 250f);
```

### Template Management

```csharp
// Register custom template
factory.RegisterTemplate("player_archer", new EntityDefinition(
    "ArcherPlayer",
    new object[]
    {
        new Position(0, 0),
        new Velocity(0, 0),
        new Health(90),
        new Sprite("sprites/player_archer.png", 32, 32),
        new Renderable(true),
        new MovementConstraint(maxVelocity: 180f)
    },
    new Dictionary<string, object>
    {
        ["PlayerType"] = "Archer",
        ["AttackType"] = "Ranged"
    }
));

// Create from template
var archer = factory.CreateFromTemplate(world, "player_archer");

// Check available templates
var templates = factory.GetTemplateNames();
foreach (var name in templates)
{
    Console.WriteLine($"Available: {name}");
}

// Remove template
factory.UnregisterTemplate("player_archer");
```

### Extending with Custom Factories

```csharp
public sealed class NPCEntityFactory : EntityFactory
{
    protected override string FactoryName => "NPCFactory";

    public NPCEntityFactory(ILogger<NPCEntityFactory> logger, IEventBus? eventBus = null)
        : base(logger, eventBus)
    {
        RegisterNPCTemplates();
    }

    public Entity CreateVendor(World world, Vector2 position, string vendorName)
    {
        var definition = new EntityDefinition(
            $"Vendor_{vendorName}",
            new object[]
            {
                new Position(position.X, position.Y),
                new Sprite("sprites/npcs/vendor.png", 32, 48),
                new Renderable(true),
                new Health(100) // NPCs have health but don't move
            },
            new Dictionary<string, object>
            {
                ["NPCType"] = "Vendor",
                ["VendorName"] = vendorName,
                ["ShopInventory"] = new List<string> { "HealthPotion", "Coin" }
            }
        );

        return Create(world, definition);
    }

    public Entity CreateQuestGiver(World world, Vector2 position, string questId)
    {
        var definition = new EntityDefinition(
            "QuestGiver",
            new object[]
            {
                new Position(position.X, position.Y),
                new Sprite("sprites/npcs/quest_giver.png", 32, 48),
                new Renderable(true),
                new Health(100)
            },
            new Dictionary<string, object>
            {
                ["NPCType"] = "QuestGiver",
                ["QuestId"] = questId,
                ["DialogueTree"] = "quest_intro"
            }
        );

        return Create(world, definition);
    }

    private void RegisterNPCTemplates()
    {
        RegisterTemplate("npc_vendor", new EntityDefinition(
            "Vendor",
            new object[]
            {
                new Position(0, 0),
                new Sprite("sprites/npcs/vendor.png", 32, 48),
                new Renderable(true),
                new Health(100)
            }
        ));

        RegisterTemplate("npc_quest_giver", new EntityDefinition(
            "QuestGiver",
            new object[]
            {
                new Position(0, 0),
                new Sprite("sprites/npcs/quest_giver.png", 32, 48),
                new Renderable(true),
                new Health(100)
            }
        ));
    }
}
```

## Dependency Injection Setup

### Service Registration

```csharp
// In your DI container setup (e.g., Startup.cs or Program.cs)
services.AddLogging();

// Register IEventBus if available
services.AddSingleton<IEventBus, EventBus>();

// Register factories as singletons (templates persist)
services.AddSingleton<EntityFactory>();
services.AddSingleton<PlayerEntityFactory>();
services.AddSingleton<EnemyEntityFactory>();
services.AddSingleton<ItemEntityFactory>();
services.AddSingleton<ProjectileEntityFactory>();

// Or register as IEntityFactory if you need abstraction
services.AddSingleton<IEntityFactory, EntityFactory>();

// Register custom factories
services.AddSingleton<NPCEntityFactory>();

// Register template loader for JSON support
services.AddSingleton<TemplateLoader>();
```

### Constructor Injection

```csharp
public class GameWorld
{
    private readonly World _world;
    private readonly PlayerEntityFactory _playerFactory;
    private readonly EnemyEntityFactory _enemyFactory;
    private readonly ILogger<GameWorld> _logger;

    public GameWorld(
        PlayerEntityFactory playerFactory,
        EnemyEntityFactory enemyFactory,
        ILogger<GameWorld> logger)
    {
        _world = World.Create();
        _playerFactory = playerFactory;
        _enemyFactory = enemyFactory;
        _logger = logger;
    }

    public void Initialize()
    {
        // Create player at spawn point
        var player = _playerFactory.CreateBasicPlayer(_world, new Vector2(400, 300));

        // Spawn enemies
        for (int i = 0; i < 5; i++)
        {
            var position = new Vector2(Random.Shared.Next(100, 700),
                                      Random.Shared.Next(100, 500));
            _enemyFactory.CreateWeakEnemy(_world, position);
        }

        _logger.LogInformation("Game world initialized with entities");
    }
}
```

## Migration Path

### Backward Compatibility

The factory pattern is **fully backward compatible**. Existing code using `world.Create()` continues to work:

```csharp
// Old code (still works)
var entity = world.Create(new Position(0, 0), new Velocity(5, 5));

// New code (using factory)
var entity = factory.Create(world, definition);
```

### Gradual Migration

1. **Phase 1**: Add factories alongside existing code
2. **Phase 2**: Use factories for new entity types
3. **Phase 3**: Gradually refactor existing creation code
4. **Phase 4**: Deprecate direct `world.Create()` calls (optional)

### Migration Example

```csharp
// Before: Direct creation scattered in code
public void SpawnPlayer()
{
    var player = world.Create(
        new Position(100, 100),
        new Velocity(0, 0),
        new Health(100),
        new Sprite("player.png", 32, 32),
        new Renderable(true),
        new MovementConstraint(maxVelocity: 200f)
    );
}

// After: Factory-based creation
public void SpawnPlayer()
{
    var player = _playerFactory.CreateBasicPlayer(world, new Vector2(100, 100));
}
```

## Event Integration

Factories publish `EntityCreatedEvent` when creating entities:

```csharp
// Subscribe to entity creation events
eventBus.Subscribe<EntityCreatedEvent>(OnEntityCreated);

void OnEntityCreated(EntityCreatedEvent evt)
{
    _logger.LogInformation(
        "Entity created: Type={Type}, Components={Count}, Factory={Factory}",
        evt.EntityType,
        evt.ComponentCount,
        evt.FactoryName
    );

    // Track entity statistics
    _entityStats.IncrementCount(evt.EntityType);

    // Initialize entity-specific systems
    if (evt.EntityType.StartsWith("Player"))
    {
        _cameraSystem.FollowEntity(evt.Entity);
    }
}
```

## Performance Considerations

### 1. **Template Caching**
Templates are registered once and reused, avoiding repeated component instantiation.

### 2. **Efficient Arch ECS Creation**
Factories create entities with all components at once, which is optimal for Arch's archetype system.

```csharp
// ✅ GOOD: Single creation call (efficient)
var entity = world.Create(comp1, comp2, comp3);

// ❌ BAD: Multiple Add calls (triggers archetype changes)
var entity = world.Create();
world.Add(entity, comp1);
world.Add(entity, comp2);
world.Add(entity, comp3);
```

### 3. **Thread Safety**
Template registration is thread-safe using locks, but entity creation should happen on the main thread (Arch requirement).

## Testing

### Unit Testing Factories

```csharp
[Fact]
public void Factory_ShouldCreateValidEntity()
{
    // Arrange
    var world = World.Create();
    var logger = new Mock<ILogger<EntityFactory>>();
    var factory = new EntityFactory(logger.Object);

    var definition = new EntityDefinition(
        "TestEntity",
        new object[] { new Position(10, 20), new Velocity(5, 5) }
    );

    // Act
    var entity = factory.Create(world, definition);

    // Assert
    Assert.True(world.IsAlive(entity));
    Assert.True(world.Has<Position>(entity));
    Assert.True(world.Has<Velocity>(entity));
}
```

### Integration Testing

```csharp
[Fact]
public void GameWorld_ShouldInitializeWithFactories()
{
    // Arrange
    var serviceProvider = BuildServiceProvider();
    var gameWorld = serviceProvider.GetRequiredService<GameWorld>();

    // Act
    gameWorld.Initialize();

    // Assert
    Assert.True(gameWorld.PlayerCount > 0);
    Assert.True(gameWorld.EnemyCount > 0);
}
```

## Best Practices

### 1. **Use Specialized Factories for Domain Entities**
```csharp
// ✅ GOOD: Specialized factory with domain logic
playerFactory.CreateTankPlayer(world, position);

// ❌ AVOID: Generic factory for domain-specific entities
factory.Create(world, complexTankDefinition);
```

### 2. **Register Templates at Startup**
```csharp
// ✅ GOOD: Register templates once
public void Initialize()
{
    RegisterDefaultTemplates();
}

// ❌ AVOID: Registering templates repeatedly
public Entity Create()
{
    RegisterTemplate(...); // Don't do this every time
    return CreateFromTemplate(...);
}
```

### 3. **Validate Components in Custom Factories**
```csharp
protected override void ValidateComponents(IReadOnlyList<object> components)
{
    base.ValidateComponents(components); // Call base validation

    // Add custom validation
    if (!components.Any(c => c is Position))
    {
        throw new ArgumentException("Player entities must have Position component");
    }
}
```

### 4. **Use Metadata for Factory-Specific Data**
```csharp
var definition = new EntityDefinition(
    "Enemy",
    components,
    new Dictionary<string, object>
    {
        ["Difficulty"] = 5,
        ["XPReward"] = 100,
        ["AIPattern"] = "Aggressive"
    }
);
```

## Related Patterns

- **Builder Pattern**: For complex, step-by-step entity construction
- **Prototype Pattern**: For cloning existing entities
- **Object Pool Pattern**: For reusing frequently created entities
- **Service Locator**: For accessing factories without DI

## Conclusion

The Entity Factory pattern provides a robust, maintainable solution for entity creation in PokeNET. It follows SOLID principles, enables easy extension, and integrates seamlessly with the existing ECS architecture while maintaining backward compatibility.
