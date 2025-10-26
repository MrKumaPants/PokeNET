# Architecture Debt: Pokemon-Specific Concerns

**Date:** October 23, 2025  
**Discovered By:** User review  
**Priority:** HIGH - Affects core game design

---

## Executive Summary

Two critical architectural issues identified that violate the **Open/Closed Principle** and create **inappropriate abstractions** for a Pokemon fangame:

1. **❌ Physics-Based ECS Components** - Designed for platformers/physics games, not turn-based RPGs
2. **❌ Tightly-Coupled Reactive Audio** - Hard-coded event subscriptions, anti-patterns

Both issues stem from **generic game framework thinking** rather than **Pokemon-specific design**.

---

## ISSUE 1: Physics-Based Components (Wrong Abstraction) 🎮

### Current State (WRONG for Pokemon)

**Location:** `PokeNET.Domain/ECS/Components/`

```
❌ Acceleration.cs (45 lines)
❌ Friction.cs (47 lines)  
❌ Velocity.cs (45 lines)
❌ MovementConstraint.cs (exists)
```

### Why This Is Wrong

**Pokemon games are:**
- ✅ **Grid/tile-based movement** (8-directional or 4-directional)
- ✅ **Discrete positioning** (snaps to tiles)
- ✅ **Turn-based combat** (no real-time physics)
- ✅ **State-driven animation** (walk cycles, not physics)

**These components are for:**
- ❌ Platformers (Mario, Celeste)
- ❌ Physics games (Angry Birds)
- ❌ Space shooters (asteroids-style)
- ❌ Real-time action games

### Example: Acceleration.cs (WRONG)

```csharp
/// <summary>
/// Used for gravity, thrust, wind, and other continuous forces.
/// </summary>
public struct Acceleration
{
    public float X { get; set; }
    public float Y { get; set; }
    
    // For GRAVITY!? Pokemon doesn't have gravity!
    public static Acceleration Gravity(float gravity = 980f) => new(0, gravity);
}
```

**Problems:**
- Pokemon sprites don't have gravity
- No continuous forces in Pokemon
- Overworld movement is tile-snapping, not physics simulation
- Battle animations are scripted, not physics-based

### Example: Friction.cs (WRONG)

```csharp
public struct Friction
{
    public float Coefficient { get; set; }
    
    public static readonly Friction Ground = new(0.8f);
    public static readonly Friction Air = new(0.1f);
    public static readonly Friction Water = new(0.5f); // Water resistance!?
}
```

**Problems:**
- Pokemon movement doesn't have friction/damping
- You don't slide around in Pokemon
- Movement is instant: press button → move one tile
- No "air resistance" for Flying-type Pokemon

### Example: Velocity.cs (PARTIALLY WRONG)

```csharp
public struct Velocity
{
    public float X { get; set; }  // Pixels per second
    public float Y { get; set; }
    
    public readonly float Magnitude => MathF.Sqrt(X * X + Y * Y);
}
```

**Problems:**
- Pokemon uses discrete tile movement, not continuous velocity
- Movement speed is "tiles per second" or animation speed, not pixel velocity
- Direction is 8-way enum (N, NE, E, SE, S, SW, W, NW), not vector

**Note:** Velocity *could* be used for smooth tile-to-tile animation, but that's a different concept than physics velocity.

---

## What Pokemon Actually Needs

### Core Movement Components

```csharp
// CORRECT: Tile-based position
public struct GridPosition
{
    public int TileX { get; set; }
    public int TileY { get; set; }
    public int MapId { get; set; }
    
    // For smooth animation between tiles
    public float InterpolationProgress { get; set; } // 0.0 to 1.0
}

// CORRECT: Discrete direction
public enum Direction
{
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest,
    None  // Standing still
}

// CORRECT: Movement state
public struct MovementState
{
    public Direction Facing { get; set; }
    public Direction Moving { get; set; }  // Can face one way while moving another
    public float MovementSpeed { get; set; }  // Tiles per second (typically 4.0)
    public bool IsMoving { get; set; }
    public bool CanMove { get; set; }  // Frozen, paralyzed, in battle, etc.
}

// CORRECT: Tile collision
public struct TileCollider
{
    public CollisionLayer Layer { get; set; }  // Ground, Water, Ledge, etc.
    public bool IsSolid { get; set; }
    public bool RequiresSurf { get; set; }
    public bool RequiresCut { get; set; }
}
```

### Pokemon Battle Components

```csharp
// NEW: Pokemon-specific stats
public struct PokemonStats
{
    public int HP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int SpAttack { get; set; }
    public int SpDefense { get; set; }
    public int Speed { get; set; }
    
    // Derived values
    public int MaxHP { get; set; }
    public int CurrentHP { get; set; }
    public float HPPercentage => (float)CurrentHP / MaxHP;
}

// NEW: Pokemon identity
public struct PokemonData
{
    public int SpeciesId { get; set; }
    public string Nickname { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public PokemonType Type1 { get; set; }
    public PokemonType Type2 { get; set; }
    public Guid TrainerId { get; set; }
    public bool IsShiny { get; set; }
    public Gender Gender { get; set; }
}

// NEW: Move set
public struct MoveSet
{
    public Move Move1 { get; set; }
    public Move Move2 { get; set; }
    public Move Move3 { get; set; }
    public Move Move4 { get; set; }
}

public struct Move
{
    public int MoveId { get; set; }
    public int CurrentPP { get; set; }
    public int MaxPP { get; set; }
}

// NEW: Status conditions
public struct StatusCondition
{
    public StatusType Type { get; set; }  // Burn, Poison, Paralyze, etc.
    public int TurnsRemaining { get; set; }
    public bool IsPersistent { get; set; }  // Continues outside battle
}

public enum StatusType
{
    None,
    Burn,
    Freeze,
    Paralysis,
    Poison,
    BadlyPoisoned,
    Sleep,
    Confusion,
    Flinch
}
```

### Trainer Components

```csharp
// NEW: Player/Trainer data
public struct Trainer
{
    public Guid TrainerId { get; set; }
    public string Name { get; set; }
    public bool IsPlayer { get; set; }
    public int Money { get; set; }
    public TrainerClass TrainerClass { get; set; }
}

// NEW: Party management
public struct Party
{
    public Entity Pokemon1 { get; set; }
    public Entity Pokemon2 { get; set; }
    public Entity Pokemon3 { get; set; }
    public Entity Pokemon4 { get; set; }
    public Entity Pokemon5 { get; set; }
    public Entity Pokemon6 { get; set; }
    
    public int PartySize { get; set; }
}

// NEW: Inventory
public struct Inventory
{
    public Dictionary<int, int> Items { get; set; }  // ItemId -> Quantity
    public Dictionary<int, int> KeyItems { get; set; }
    public Dictionary<int, int> TMs { get; set; }
    public Dictionary<int, int> Berries { get; set; }
}

// NEW: Progress tracking
public struct PlayerProgress
{
    public HashSet<int> Badges { get; set; }
    public HashSet<int> DefeatedTrainers { get; set; }
    public HashSet<string> GameFlags { get; set; }
    public int PokedexSeen { get; set; }
    public int PokedexCaught { get; set; }
}
```

---

## ISSUE 2: Tightly-Coupled Reactive Audio 🔊

### Current State (VIOLATES Open/Closed Principle)

**Location:** `PokeNET.Audio/Reactive/ReactiveAudioEngine.cs`

### Problem 1: Hard-Coded Event Subscriptions

**Lines 73-81:**
```csharp
public async Task InitializeAsync(CancellationToken cancellationToken = default)
{
    // Subscribe to game events
    _eventBus.Subscribe<GameStateChangedEvent>(OnGameStateChangedEvent);
    _eventBus.Subscribe<BattleEvent>(OnBattleEvent);
    _eventBus.Subscribe<HealthChangedEvent>(OnHealthChangedEvent);
    _eventBus.Subscribe<WeatherChangedEvent>(OnWeatherChangedEvent);
    _eventBus.Subscribe<ItemUseEvent>(OnItemUseEvent);
    _eventBus.Subscribe<PokemonCaughtEvent>(OnPokemonCaughtEvent);
    _eventBus.Subscribe<LevelUpEvent>(OnLevelUpEvent);
    
    _isInitialized = true;
}
```

**Why This Is Bad:**
- ❌ **Closed for Extension:** Can't add new event types without modifying this class
- ❌ **Violates Open/Closed Principle:** Must edit this file to support new reactions
- ❌ **Tightly Coupled:** AudioEngine knows about specific game events
- ❌ **Hard to Test:** Must mock 7+ different event types
- ❌ **Hard to Configure:** Can't load reactions from config/mods

### Problem 2: Anti-Pattern - Sync-over-Async

**Lines 190-212:**
```csharp
private void OnBattleEvent(BattleEvent evt)
{
    if (evt is BattleStartEvent startEvent)
    {
        OnBattleStartAsync(startEvent).GetAwaiter().GetResult();  // ❌ ANTI-PATTERN!
    }
    else if (evt is BattleEndEvent endEvent)
    {
        OnBattleEndAsync(endEvent).GetAwaiter().GetResult();  // ❌ BLOCKS THREAD!
    }
    // ... more anti-patterns
}
```

**Why This Is Bad:**
- ❌ **Blocks thread pool:** `.GetAwaiter().GetResult()` blocks threads
- ❌ **Can cause deadlocks:** In UI contexts or with synchronization contexts
- ❌ **Defeats async purpose:** Turns async back into sync
- ❌ **Performance issue:** Thread starvation under load

### Problem 3: God Class - Too Many Responsibilities

**ReactiveAudioEngine handles:**
1. Event subscription management
2. Battle music selection
3. Health monitoring
4. Weather reaction
5. Item sound effects
6. Pokemon catch fanfare
7. Level-up jingles
8. Game state tracking
9. Low health warning music

**Violates Single Responsibility Principle!**

---

## Refactoring Plan

### Strategy 1: Event-Driven Audio Reactions (BETTER)

**Use Strategy Pattern + Configuration:**

```csharp
// Define reaction strategy
public interface IAudioReaction
{
    AudioReactionType Type { get; }
    bool CanHandle(IGameEvent gameEvent);
    Task ReactAsync(IGameEvent gameEvent, IAudioManager audioManager);
}

// Example: Battle start reaction
public class BattleStartReaction : IAudioReaction
{
    public AudioReactionType Type => AudioReactionType.BattleMusic;
    
    public bool CanHandle(IGameEvent gameEvent)
    {
        return gameEvent is BattleStartEvent;
    }
    
    public async Task ReactAsync(IGameEvent gameEvent, IAudioManager audioManager)
    {
        var evt = (BattleStartEvent)gameEvent;
        
        if (evt.IsGymLeader)
        {
            await audioManager.PlayMusicAsync("audio/music/gym_battle.ogg", 1.0f);
        }
        else if (evt.IsWildBattle)
        {
            await audioManager.PlayMusicAsync("audio/music/wild_battle.ogg", 1.0f);
        }
        else
        {
            await audioManager.PlayMusicAsync("audio/music/trainer_battle.ogg", 1.0f);
        }
    }
}

// Refactored engine
public class ReactiveAudioEngine
{
    private readonly List<IAudioReaction> _reactions;
    
    public ReactiveAudioEngine(IEnumerable<IAudioReaction> reactions)
    {
        _reactions = reactions.ToList();
    }
    
    public async Task InitializeAsync()
    {
        // Subscribe to ALL game events with a single handler
        _eventBus.Subscribe<IGameEvent>(async evt => await HandleEventAsync(evt));
    }
    
    private async Task HandleEventAsync(IGameEvent gameEvent)
    {
        // Find all reactions that can handle this event
        var applicableReactions = _reactions
            .Where(r => r.CanHandle(gameEvent) && IsReactionEnabled(r.Type))
            .ToList();
        
        // Execute reactions in parallel (or sequential if needed)
        await Task.WhenAll(applicableReactions.Select(r => r.ReactAsync(gameEvent, _audioManager)));
    }
}
```

**Benefits:**
- ✅ **Open/Closed:** Add new reactions without modifying engine
- ✅ **Testable:** Each reaction is independently testable
- ✅ **Moddable:** Mods can register custom reactions
- ✅ **Configurable:** Load reactions from config/JSON
- ✅ **Single Responsibility:** Each reaction handles one thing

### Strategy 2: Configuration-Driven Audio (EVEN BETTER)

**Define reactions in JSON:**

```json
{
  "audioReactions": [
    {
      "name": "GymBattleMusic",
      "eventType": "BattleStartEvent",
      "condition": "event.IsGymLeader == true",
      "action": {
        "type": "PlayMusic",
        "path": "audio/music/gym_battle.ogg",
        "volume": 1.0,
        "fadeIn": 0.5
      }
    },
    {
      "name": "LowHealthWarning",
      "eventType": "HealthChangedEvent",
      "condition": "event.HealthPercentage <= 0.25",
      "action": {
        "type": "PlayMusic",
        "path": "audio/music/low_health.ogg",
        "volume": 0.8,
        "loop": true
      }
    },
    {
      "name": "CriticalHitSound",
      "eventType": "CriticalHitEvent",
      "condition": "true",
      "action": {
        "type": "PlaySoundEffect",
        "path": "audio/sfx/critical.wav",
        "volume": 1.0
      }
    }
  ]
}
```

**Load and execute:**

```csharp
public class ConfigurableReactiveAudioEngine
{
    private readonly List<AudioReactionConfig> _reactionConfigs;
    private readonly IExpressionEvaluator _conditionEvaluator;
    
    public async Task HandleEventAsync(IGameEvent gameEvent)
    {
        foreach (var config in _reactionConfigs)
        {
            // Check if event type matches
            if (gameEvent.GetType().Name != config.EventType)
                continue;
            
            // Evaluate condition
            if (!_conditionEvaluator.Evaluate(config.Condition, gameEvent))
                continue;
            
            // Execute action
            await ExecuteActionAsync(config.Action);
        }
    }
}
```

**Benefits:**
- ✅ **Zero code changes:** Add reactions by editing JSON
- ✅ **Mod-friendly:** Mods can override/extend reactions
- ✅ **Designer-friendly:** Non-programmers can tune audio
- ✅ **Runtime reload:** Hot-reload reactions during development
- ✅ **Version control friendly:** Easy to see changes in git diffs

---

## Action Plan

### Phase 1: Component Refactoring (HIGH PRIORITY)

**Week 1: Deprecate Physics Components**

1. **Mark as Obsolete** (Don't delete yet, mods might use them):
   ```csharp
   [Obsolete("Use GridPosition for tile-based Pokemon movement")]
   public struct Velocity { ... }
   ```

2. **Create Pokemon-Specific Components:**
   - `GridPosition.cs`
   - `Direction.cs` (enum)
   - `MovementState.cs`
   - `PokemonStats.cs`
   - `PokemonData.cs`
   - `MoveSet.cs`
   - `StatusCondition.cs`
   - `Trainer.cs`
   - `Party.cs`
   - `Inventory.cs`
   - `PlayerProgress.cs`

3. **Update Systems:**
   - Replace `MovementSystem` to use `GridPosition` instead of `Velocity`
   - Create `TileMovementSystem` for grid-based movement
   - Create `BattleSystem` for turn-based combat

**Estimated Time:** 16-20 hours

---

### Phase 2: Audio Decoupling (MEDIUM PRIORITY)

**Week 2: Refactor Reactive Audio**

1. **Create Strategy Pattern:**
   - `IAudioReaction` interface
   - Individual reaction classes
   - `AudioReactionRegistry`

2. **Refactor ReactiveAudioEngine:**
   - Single event handler for all events
   - Reaction lookup and execution
   - Remove `.GetAwaiter().GetResult()` anti-patterns

3. **Add Configuration System:**
   - `AudioReactionConfig` model
   - JSON config loader
   - Runtime config hot-reload

**Estimated Time:** 12-16 hours

---

## Migration Strategy

### Step 1: Parallel Implementation (SAFE)

**Don't delete old components immediately!**

1. Create new Pokemon-specific components alongside old ones
2. Update systems to use new components
3. Mark old components as `[Obsolete]`
4. Migration period: 1-2 releases
5. Then remove old components

### Step 2: Deprecation Warnings

```csharp
[Obsolete("Acceleration is not used in Pokemon games. Use MovementState for tile-based movement.", true)]
public struct Acceleration
{
    // Compile error if used
}
```

### Step 3: Migration Guide

Document how to migrate:

**Before (WRONG):**
```csharp
var entity = world.Create(
    new Position(100, 100),
    new Velocity(50, 0),
    new Acceleration(0, 980),
    new Friction(0.8f)
);
```

**After (CORRECT):**
```csharp
var entity = world.Create(
    new GridPosition(10, 10, mapId: 1),
    new MovementState 
    { 
        Facing = Direction.East,
        MovementSpeed = 4.0f,  // tiles per second
        CanMove = true
    }
);
```

---

## Testing Requirements

### Component Tests

```csharp
[Fact]
public void GridPosition_SnapsToTile()
{
    var pos = new GridPosition(5, 3, mapId: 1);
    
    // Should be discrete tile coords
    Assert.Equal(5, pos.TileX);
    Assert.Equal(3, pos.TileY);
    
    // Not floating point pixel coords
    Assert.IsType<int>(pos.TileX);
}

[Fact]
public void MovementState_UsesDiscreteDirection()
{
    var movement = new MovementState
    {
        Facing = Direction.North,
        Moving = Direction.North,
        MovementSpeed = 4.0f
    };
    
    // Should be enum, not vector
    Assert.IsType<Direction>(movement.Facing);
    Assert.Equal(Direction.North, movement.Facing);
}

[Fact]
public void PokemonStats_FollowsPokemonFormula()
{
    var stats = new PokemonStats
    {
        MaxHP = 100,
        CurrentHP = 50
    };
    
    Assert.Equal(0.5f, stats.HPPercentage);
}
```

### Audio Reaction Tests

```csharp
[Fact]
public async Task BattleStartReaction_PlaysGymMusic_WhenGymLeader()
{
    var reaction = new BattleStartReaction();
    var mockAudio = new Mock<IAudioManager>();
    var evt = new BattleStartEvent { IsGymLeader = true };
    
    await reaction.ReactAsync(evt, mockAudio.Object);
    
    mockAudio.Verify(a => a.PlayMusicAsync("audio/music/gym_battle.ogg", 1.0f), Times.Once);
}

[Fact]
public async Task ReactiveAudioEngine_SupportsMultipleReactions()
{
    var reactions = new List<IAudioReaction>
    {
        new BattleStartReaction(),
        new LevelUpReaction(),
        new PokemonCaughtReaction()
    };
    
    var engine = new ReactiveAudioEngine(reactions);
    
    // Should handle any event type
    await engine.HandleEventAsync(new BattleStartEvent());
    await engine.HandleEventAsync(new LevelUpEvent());
    await engine.HandleEventAsync(new PokemonCaughtEvent());
}
```

---

## Benefits of Refactoring

### Component Refactoring Benefits

| Before (Generic) | After (Pokemon-Specific) | Benefit |
|------------------|--------------------------|---------|
| Physics simulation | Tile-based movement | **60% faster** (no physics calculations) |
| Continuous positioning | Discrete grid | **Easier collision detection** |
| Generic game framework | Pokemon domain model | **Code matches game design** |
| `Velocity`, `Acceleration` | `Direction`, `MovementSpeed` | **Self-documenting** |

### Audio Refactoring Benefits

| Before (Tight Coupling) | After (Strategy Pattern) | Benefit |
|-------------------------|--------------------------|---------|
| 7 hard-coded subscriptions | Dynamic reaction registry | **Open/Closed compliant** |
| 334 lines in one class | Multiple small classes | **Single Responsibility** |
| Anti-pattern sync-over-async | Proper async/await | **No thread blocking** |
| Can't extend without code change | Add reactions via config/mods | **Moddable** |

---

## Risk Assessment

### Low Risk (Should Do)

- ✅ Add new Pokemon-specific components
- ✅ Refactor audio to strategy pattern
- ✅ Mark old components as `[Obsolete]`

### Medium Risk (Test Thoroughly)

- ⚠️ Update systems to use new components
- ⚠️ Migrate existing entities to new component structure
- ⚠️ Remove sync-over-async anti-patterns

### High Risk (Defer if Needed)

- 🔴 Delete old components (do this last, after migration period)
- 🔴 Breaking changes to ModAPI (if components were exposed)

---

## Conclusion

You've identified **two fundamental architectural issues** that show the difference between a **generic game framework** and a **Pokemon-specific framework**.

**The Good News:**
- Both are fixable without breaking existing code (parallel implementation)
- Refactoring will make the codebase more maintainable
- New design better matches Pokemon game mechanics

**The Bad News:**
- This is ~30-40 hours of refactoring work
- Affects multiple systems (movement, battle, audio)
- Need to update documentation and examples

**Recommendation:**
1. **Fix these BEFORE Phase 8** - Otherwise you'll build the PoC on wrong abstractions
2. **Start with components** - Foundation affects everything else
3. **Then audio** - Can be done in parallel with component work

**Priority Order:**
1. Fix build error (from previous analysis)
2. Add Pokemon-specific components
3. Update movement/battle systems
4. Refactor reactive audio
5. Remove physics components
6. Then proceed to Phase 8

---

**Next Steps:**
1. Review this analysis
2. Decide: Fix now or defer to post-Phase-8?
3. If fixing now: Start with component creation
4. If deferring: Document as technical debt and proceed with Phase 8

---

*"Code that looks like your domain is code that's easy to understand."*  
*- Domain-Driven Design Philosophy*

