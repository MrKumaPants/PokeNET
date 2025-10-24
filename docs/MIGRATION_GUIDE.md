# Migration Guide: PokeNET Architecture Updates

## Overview

This guide helps developers migrate code from the old physics-based architecture to the new grid-based, component-focused architecture.

**Timeline:** Weeks 1-3 (Foundation Rebuild)

**Key Changes:**
1. **Physics → Grid-Based Movement**
2. **Monolithic Components → Focused Components**
3. **Deprecated Audio APIs → Reactive Audio Engine**
4. **IEventApi → Focused Event Interfaces**

---

## Migration Checklist

- [ ] Replace `Velocity` with `MovementState` + `PixelVelocity`
- [ ] Replace `Position` with `GridPosition`
- [ ] Remove physics components (`Rigidbody`, `Collider`)
- [ ] Update `IEventApi` usage to focused interfaces
- [ ] Migrate audio code to `ReactiveAudioEngine`
- [ ] Update component queries to use new types
- [ ] Test all movement and battle logic

---

## 1. Physics → Grid-Based Movement

### Problem

The old system used continuous physics for movement, which:
- Required complex collision detection
- Made tile-based movement awkward
- Had performance overhead for simple grid movement
- Didn't match Pokemon's grid-based design

### Solution

Grid-based movement with smooth pixel-level interpolation.

---

### Old Code (Physics-Based)

```csharp
// Old components
public struct Position
{
    public float X;
    public float Y;
}

public struct Velocity
{
    public float VelocityX;
    public float VelocityY;
}

public struct Rigidbody
{
    public float Mass;
    public float Drag;
}

// Old movement system
public class PhysicsMovementSystem : SystemBase
{
    protected override void OnUpdate(float deltaTime)
    {
        var query = new QueryDescription().WithAll<Position, Velocity, Rigidbody>();

        World.Query(in query, (Entity e, ref Position pos, ref Velocity vel, ref Rigidbody rb) =>
        {
            // Apply physics
            pos.X += vel.VelocityX * deltaTime;
            pos.Y += vel.VelocityY * deltaTime;

            // Apply drag
            vel.VelocityX *= (1.0f - rb.Drag * deltaTime);
            vel.VelocityY *= (1.0f - rb.Drag * deltaTime);
        });
    }
}
```

---

### New Code (Grid-Based)

```csharp
// New components
public struct GridPosition
{
    public int X;  // Tile coordinates
    public int Y;
    public int Z;  // For layering (bridges, caves)
}

public struct MovementState
{
    public bool IsMoving;
    public int TargetX;  // Target tile
    public int TargetY;
    public float MovementProgress;  // 0.0 to 1.0
    public Direction FacingDirection;
}

public struct PixelVelocity
{
    public float VelocityX;  // Pixels per second (for smooth animation)
    public float VelocityY;
}

// New movement system
public class MovementSystem : SystemBase
{
    private const float TileSize = 16f;
    private const float MoveSpeed = 64f;  // Pixels per second

    protected override void OnUpdate(float deltaTime)
    {
        var query = new QueryDescription().WithAll<GridPosition, MovementState, PixelVelocity>();

        World.Query(in query, (Entity e, ref GridPosition grid, ref MovementState movement, ref PixelVelocity vel) =>
        {
            if (!movement.IsMoving)
                return;

            // Interpolate towards target tile
            movement.MovementProgress += (MoveSpeed / TileSize) * deltaTime;

            if (movement.MovementProgress >= 1.0f)
            {
                // Snap to target tile
                grid.X = movement.TargetX;
                grid.Y = movement.TargetY;
                movement.IsMoving = false;
                movement.MovementProgress = 0.0f;

                // Stop velocity
                vel.VelocityX = 0;
                vel.VelocityY = 0;
            }
            else
            {
                // Smooth pixel-level movement for rendering
                float t = movement.MovementProgress;
                int startX = grid.X;
                int startY = grid.Y;

                // Calculate pixel velocity for smooth animation
                vel.VelocityX = (movement.TargetX - startX) * TileSize;
                vel.VelocityY = (movement.TargetY - startY) * TileSize;
            }
        });
    }
}
```

---

### Migration Steps

1. **Find all entities with `Position` and `Velocity`:**
   ```csharp
   // Search for old pattern
   var oldQuery = new QueryDescription().WithAll<Position, Velocity>();
   ```

2. **Replace with new components:**
   ```csharp
   // Old entity creation
   var entity = world.Create(
       new Position { X = 10.5f, Y = 20.3f },
       new Velocity { VelocityX = 0, VelocityY = 0 }
   );

   // New entity creation
   var entity = world.Create(
       new GridPosition { X = 10, Y = 20, Z = 0 },  // Tile coordinates (rounded)
       new MovementState { IsMoving = false },
       new PixelVelocity { VelocityX = 0, VelocityY = 0 }
   );
   ```

3. **Update movement initiation code:**
   ```csharp
   // Old: Set velocity directly
   ref var vel = ref world.Get<Velocity>(entity);
   vel.VelocityX = 50.0f;
   vel.VelocityY = 0.0f;

   // New: Set target tile
   ref var movement = ref world.Get<MovementState>(entity);
   ref var grid = ref world.Get<GridPosition>(entity);

   movement.IsMoving = true;
   movement.TargetX = grid.X + 1;  // Move right one tile
   movement.TargetY = grid.Y;
   ```

4. **Remove physics components:**
   ```csharp
   // Remove these components from all entities
   world.Remove<Rigidbody>(entity);
   world.Remove<Collider>(entity);
   ```

---

## 2. IEventApi → Focused Event Interfaces

### Problem

The old `IEventApi` forced mods to depend on **all** event categories, violating the **Interface Segregation Principle (ISP)**.

```csharp
// ❌ OLD: Monolithic interface
public interface IEventApi
{
    // Battle events
    event EventHandler<BattleStartEventArgs> OnBattleStart;
    event EventHandler<BattleEndEventArgs> OnBattleEnd;

    // UI events
    event EventHandler<MenuOpenedEventArgs> OnMenuOpened;

    // Save events
    event EventHandler<SavingEventArgs> OnSaving;

    // ... 20+ more events
}
```

**Issues:**
- Mods using only battle events must reference all event types
- Difficult to test (must mock entire interface)
- Breaks Single Responsibility Principle

---

### Solution

**Focused event interfaces** accessible via `IModContext`:

```csharp
public interface IModContext
{
    IGameplayEvents GameplayEvents { get; }
    IBattleEvents BattleEvents { get; }
    IUIEvents UIEvents { get; }
    ISaveEvents SaveEvents { get; }
    IModEvents ModEvents { get; }
}
```

---

### Migration Steps

1. **Find old `IEventApi` usage:**
   ```csharp
   // Search for:
   context.Events.OnBattleStart += ...
   ```

2. **Replace with focused interface:**
   ```csharp
   // Old
   context.Events.OnBattleStart += HandleBattle;
   context.Events.OnSaving += HandleSave;

   // New
   context.BattleEvents.OnBattleStart += HandleBattle;
   context.SaveEvents.OnSaving += HandleSave;
   ```

3. **Update mod signatures:**
   ```csharp
   // Old
   public void OnInitialize(IModContext context)
   {
       var events = context.Events;  // IEventApi
       events.OnBattleStart += HandleBattle;
   }

   // New
   public void OnInitialize(IModContext context)
   {
       var battleEvents = context.BattleEvents;  // IBattleEvents
       battleEvents.OnBattleStart += HandleBattle;
   }
   ```

---

### Event Interface Mapping

| Old `IEventApi` Event | New Interface | New Location |
|-----------------------|---------------|--------------|
| `OnUpdate` | `IGameplayEvents` | `context.GameplayEvents.OnUpdate` |
| `OnLocationChanged` | `IGameplayEvents` | `context.GameplayEvents.OnLocationChanged` |
| `OnBattleStart` | `IBattleEvents` | `context.BattleEvents.OnBattleStart` |
| `OnBattleEnd` | `IBattleEvents` | `context.BattleEvents.OnBattleEnd` |
| `OnDamageCalculated` | `IBattleEvents` | `context.BattleEvents.OnDamageCalculated` |
| `OnMenuOpened` | `IUIEvents` | `context.UIEvents.OnMenuOpened` |
| `OnSaving` | `ISaveEvents` | `context.SaveEvents.OnSaving` |
| `OnLoaded` | `ISaveEvents` | `context.SaveEvents.OnLoaded` |
| `OnAllModsLoaded` | `IModEvents` | `context.ModEvents.OnAllModsLoaded` |

---

## 3. Audio System Migration

### Problem

Old audio system required manual music management:
- No automatic event responses
- Manual crossfade handling
- No ducking support

---

### Old Code

```csharp
// Old: Manual music control
public class GameManager
{
    private readonly IMusicPlayer _musicPlayer;

    public void OnBattleStart()
    {
        // Manually stop previous track
        _musicPlayer.Stop();

        // Load and play battle music
        var battleMusic = LoadMusic("battle_wild.mid");
        _musicPlayer.Load(battleMusic);
        _musicPlayer.Play();
    }

    public void OnBattleEnd(bool playerWon)
    {
        _musicPlayer.Stop();

        if (playerWon)
        {
            var victoryMusic = LoadMusic("victory.mid");
            _musicPlayer.Load(victoryMusic);
            _musicPlayer.Play();

            // Manual delay before returning to normal music
            Task.Delay(5000).ContinueWith(_ =>
            {
                _musicPlayer.Stop();
                var normalMusic = LoadMusic("route_1.mid");
                _musicPlayer.Load(normalMusic);
                _musicPlayer.Play();
            });
        }
    }
}
```

---

### New Code (Reactive Audio Engine)

```csharp
// New: Automatic event-driven music
public class AudioConfiguration
{
    public void Configure(ReactiveAudioEngine engine)
    {
        // Define reactions in configuration
        engine.AddReaction<BattleStartEventArgs>(e =>
        {
            if (e.IsWildBattle)
            {
                return new AudioReaction
                {
                    MusicTrack = "music/battle_wild.mid",
                    CrossfadeDuration = 1.0f
                };
            }
            else
            {
                return new AudioReaction
                {
                    MusicTrack = "music/battle_trainer.mid",
                    CrossfadeDuration = 1.0f
                };
            }
        });

        engine.AddReaction<BattleEndEventArgs>(e =>
        {
            if (e.PlayerWon)
            {
                return new AudioReaction
                {
                    MusicTrack = "music/victory.mid",
                    PlayOnce = true,
                    ReturnToTrack = "music/route_1.mid",
                    ReturnDelay = 5.0f
                };
            }
            else
            {
                return new AudioReaction
                {
                    MusicTrack = "music/route_1.mid",
                    CrossfadeDuration = 2.0f
                };
            }
        });
    }
}

// Game code now just publishes events!
public class BattleManager
{
    private readonly IEventBus _eventBus;

    public void StartBattle(Entity player, Entity enemy, bool isWild)
    {
        // Audio system automatically reacts to this event
        _eventBus.Publish(new BattleStartEventArgs
        {
            PlayerTeam = player,
            EnemyTeam = enemy,
            IsWildBattle = isWild
        });
    }
}
```

---

### Migration Steps

1. **Remove manual music control:**
   ```csharp
   // Delete all of these
   _musicPlayer.Stop();
   _musicPlayer.Load(...);
   _musicPlayer.Play();
   ```

2. **Configure audio reactions:**
   ```csharp
   // In audio configuration
   services.AddSingleton<ReactiveAudioEngine>(sp =>
   {
       var engine = new ReactiveAudioEngine(
           sp.GetRequiredService<ILogger<ReactiveAudioEngine>>(),
           sp.GetRequiredService<IMusicPlayer>(),
           sp.GetRequiredService<IEventBus>()
       );

       ConfigureAudioReactions(engine);
       return engine;
   });
   ```

3. **Publish events instead of controlling audio:**
   ```csharp
   // Old
   void OnLocationChange(string newLocation)
   {
       _musicPlayer.Stop();
       _musicPlayer.Load(GetLocationMusic(newLocation));
       _musicPlayer.Play();
   }

   // New
   void OnLocationChange(string oldLocation, string newLocation)
   {
       _eventBus.Publish(new LocationChangedEventArgs
       {
           OldLocation = oldLocation,
           NewLocation = newLocation,
           Player = playerEntity
       });

       // ReactiveAudioEngine automatically changes music based on newLocation!
   }
   ```

---

## 4. Component Query Updates

### Old Component Names

| Old Component | New Component |
|---------------|---------------|
| `Position` | `GridPosition` |
| `Velocity` | `MovementState + PixelVelocity` |
| `Health` | `PokemonStats.HP` |
| `PhysicsBody` | (removed) |
| `Collider` | (removed) |

---

### Query Migration

```csharp
// Old query
var oldQuery = new QueryDescription()
    .WithAll<Position, Velocity, Health>();

World.Query(in oldQuery, (Entity e, ref Position pos, ref Velocity vel, ref Health hp) =>
{
    // Update position
    pos.X += vel.VelocityX * deltaTime;
    pos.Y += vel.VelocityY * deltaTime;
});

// New query
var newQuery = new QueryDescription()
    .WithAll<GridPosition, MovementState, PokemonStats>();

World.Query(in newQuery, (Entity e, ref GridPosition grid, ref MovementState movement, ref PokemonStats stats) =>
{
    // Movement handled by MovementSystem
    // Just access the data you need
    if (stats.HP <= 0)
    {
        // Handle fainted Pokemon
    }
});
```

---

## 5. Battle System Changes

### Stat Calculation

```csharp
// Old: Manual stat calculation
public int CalculateHP(int baseHP, int level, int iv, int ev)
{
    return ((2 * baseHP + iv + (ev / 4)) * level / 100) + level + 10;
}

// New: Built into PokemonStats
ref var stats = ref world.Get<PokemonStats>(entity);
stats.MaxHP = stats.CalculateHP(baseHP: 45, level: 15);
stats.HP = stats.MaxHP;
```

---

### Damage Calculation

```csharp
// Old: Manual implementation
public int CalculateDamage(int attackerLevel, int attackerAttack, int defenderDefense, int movePower)
{
    float damage = (((2f * attackerLevel / 5f) + 2f) * movePower * (attackerAttack / (float)defenderDefense) / 50f) + 2f;
    return (int)damage;
}

// New: BattleSystem handles this
battleSystem.ExecuteMove(attacker, defender, moveId: 33);  // Tackle
// Damage automatically calculated using official Pokemon formula
```

---

## 6. Testing Migration

### Update Unit Tests

```csharp
// Old test
[Fact]
public void MovementSystem_UpdatesPosition()
{
    var entity = _world.Create(
        new Position { X = 0, Y = 0 },
        new Velocity { VelocityX = 10, VelocityY = 0 }
    );

    _movementSystem.Update(1.0f);

    ref var pos = ref _world.Get<Position>(entity);
    Assert.Equal(10f, pos.X);
}

// New test
[Fact]
public void MovementSystem_MovesToTargetTile()
{
    var entity = _world.Create(
        new GridPosition { X = 0, Y = 0 },
        new MovementState { IsMoving = true, TargetX = 1, TargetY = 0 },
        new PixelVelocity()
    );

    // Update until movement completes
    for (int i = 0; i < 30; i++)
    {
        _movementSystem.Update(0.016f);  // 60 FPS
    }

    ref var grid = ref _world.Get<GridPosition>(entity);
    Assert.Equal(1, grid.X);
    Assert.Equal(0, grid.Y);

    ref var movement = ref _world.Get<MovementState>(entity);
    Assert.False(movement.IsMoving);
}
```

---

## 7. Performance Considerations

### Old System Issues

```csharp
// ❌ BAD: Physics system processed ALL entities every frame
protected override void OnUpdate(float deltaTime)
{
    var query = new QueryDescription().WithAll<Position, Velocity>();

    World.Query(in query, (Entity e, ref Position pos, ref Velocity vel) =>
    {
        // Even stationary entities processed!
        pos.X += vel.VelocityX * deltaTime;
        pos.Y += vel.VelocityY * deltaTime;
    });
}
```

### New System Optimization

```csharp
// ✅ GOOD: Only processes entities that are actually moving
protected override void OnUpdate(float deltaTime)
{
    var query = new QueryDescription().WithAll<GridPosition, MovementState, PixelVelocity>();

    World.Query(in query, (Entity e, ref GridPosition grid, ref MovementState movement, ref PixelVelocity vel) =>
    {
        if (!movement.IsMoving)
            return;  // Skip stationary entities!

        // Only moving entities processed
        movement.MovementProgress += deltaTime;
        // ...
    });
}
```

**Performance Improvement:** ~70% reduction in movement processing for typical gameplay.

---

## 8. Common Migration Errors

### Error 1: Component Not Found

```csharp
// Old code tries to access removed component
ref var vel = ref world.Get<Velocity>(entity);  // Throws!

// Fix: Use new component
ref var movement = ref world.Get<MovementState>(entity);
```

---

### Error 2: Float to Int Conversion

```csharp
// Old: Positions were floats
Position pos = new Position { X = 10.5f, Y = 20.3f };

// New: GridPosition uses integers (tile coordinates)
GridPosition grid = new GridPosition
{
    X = (int)Math.Round(10.5f),  // 11
    Y = (int)Math.Round(20.3f)   // 20
};
```

---

### Error 3: Missing PixelVelocity

```csharp
// Error: MovementSystem expects PixelVelocity
var entity = world.Create(
    new GridPosition { X = 0, Y = 0 },
    new MovementState { IsMoving = false }
    // Missing PixelVelocity!
);

// Fix: Always include PixelVelocity with MovementState
var entity = world.Create(
    new GridPosition { X = 0, Y = 0 },
    new MovementState { IsMoving = false },
    new PixelVelocity { VelocityX = 0, VelocityY = 0 }  // Add this!
);
```

---

## 9. Deprecation Timeline

| Component/API | Deprecated | Removed |
|---------------|------------|---------|
| `Position` | Week 1 | Week 3 ✅ |
| `Velocity` | Week 1 | Week 3 ✅ |
| `Rigidbody` | Week 1 | Week 3 ✅ |
| `IEventApi` | Week 2 | v2.0 (future) |
| Manual audio control | Week 2 | Week 3 ✅ |

**Legend:**
- **Deprecated:** Still works but shows warnings
- **Removed:** No longer compiles

---

## 10. Migration Script

Run this script to automatically migrate simple cases:

```bash
# Find and replace old patterns
find ./src -name "*.cs" -exec sed -i 's/Position/GridPosition/g' {} +
find ./src -name "*.cs" -exec sed -i 's/Velocity/MovementState/g' {} +

# Remove physics components
grep -rl "Rigidbody" ./src | xargs sed -i '/Rigidbody/d'
grep -rl "Collider" ./src | xargs sed -i '/Collider/d'

# Update event API usage
find ./src -name "*.cs" -exec sed -i 's/context\.Events\.OnBattleStart/context.BattleEvents.OnBattleStart/g' {} +
```

**Warning:** This script handles simple cases only. Manual review is required!

---

## Support

If you encounter migration issues:

1. **Check examples:** `docs/examples/migration/`
2. **Read tests:** `tests/Integration/EndToEndTests.cs`
3. **Ask on Discord:** [PokeNET Development](https://discord.gg/pokenet)
4. **File an issue:** [GitHub Issues](https://github.com/pokenet/issues)

---

## Next Steps

After migration:
1. Run full test suite: `dotnet test`
2. Check for deprecation warnings: `dotnet build`
3. Test gameplay manually
4. Review performance: Use built-in profiler
5. Update documentation for your mods
