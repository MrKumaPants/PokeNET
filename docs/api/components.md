# ECS Component Reference

## Introduction

Components in PokeNET are pure data structures that represent specific aspects of game entities. They follow the Entity-Component-System (ECS) pattern using the Arch library.

## Component Design Principles

1. **Data Only**: Components contain only data, no behavior
2. **Single Responsibility**: Each component represents one aspect
3. **Struct-Based**: Use structs for performance (value types)
4. **Minimal Size**: Keep components small and focused
5. **Serializable**: Support save/load functionality

## Core Components

### Position
**Purpose**: Spatial location in the game world

```csharp
public struct Position
{
    /// <summary>
    /// X coordinate in world space
    /// </summary>
    public float X;

    /// <summary>
    /// Y coordinate in world space
    /// </summary>
    public float Y;

    /// <summary>
    /// Z layer for rendering order (optional)
    /// </summary>
    public float Z;
}
```

**Common Queries**:
```csharp
// All positioned entities
world.Query<Position>();

// Positioned and moving entities
world.Query<Position, Velocity>();
```

---

### Velocity
**Purpose**: Movement speed and direction

```csharp
public struct Velocity
{
    /// <summary>
    /// Horizontal velocity (units per second)
    /// </summary>
    public float VX;

    /// <summary>
    /// Vertical velocity (units per second)
    /// </summary>
    public float VY;

    /// <summary>
    /// Maximum speed (optional constraint)
    /// </summary>
    public float MaxSpeed;
}
```

**Usage**:
```csharp
// Set velocity
entity.Set(new Velocity { VX = 100f, VY = 50f, MaxSpeed = 150f });

// Update position based on velocity (in MovementSystem)
ref var pos = ref entity.Get<Position>();
ref var vel = ref entity.Get<Velocity>();
pos.X += vel.VX * deltaTime;
pos.Y += vel.VY * deltaTime;
```

---

### Sprite
**Purpose**: Visual representation

```csharp
public struct Sprite
{
    /// <summary>
    /// Texture to render
    /// </summary>
    public Texture2D Texture;

    /// <summary>
    /// Source rectangle in texture (for sprite sheets)
    /// </summary>
    public Rectangle? SourceRect;

    /// <summary>
    /// Tint color (default: White)
    /// </summary>
    public Color Tint;

    /// <summary>
    /// Scale factor
    /// </summary>
    public Vector2 Scale;

    /// <summary>
    /// Rotation in radians
    /// </summary>
    public float Rotation;

    /// <summary>
    /// Origin point for rotation/scale
    /// </summary>
    public Vector2 Origin;

    /// <summary>
    /// Render layer for sorting
    /// </summary>
    public int Layer;
}
```

**Example**:
```csharp
entity.Set(new Sprite
{
    Texture = assetManager.LoadTexture("creature.png"),
    Tint = Color.White,
    Scale = new Vector2(1.0f, 1.0f),
    Rotation = 0f,
    Layer = 10
});
```

---

## Creature Components

### CreatureStats
**Purpose**: Core creature statistics

```csharp
public struct CreatureStats
{
    // Base stats (unchanging)
    public int BaseHP;
    public int BaseAttack;
    public int BaseDefense;
    public int BaseSpAttack;
    public int BaseSpDefense;
    public int BaseSpeed;

    // Current stats (modified by level, EVs, IVs)
    public int HP;
    public int Attack;
    public int Defense;
    public int SpAttack;
    public int SpDefense;
    public int Speed;

    // Individual Values (0-31)
    public byte IVHP;
    public byte IVAttack;
    public byte IVDefense;
    public byte IVSpAttack;
    public byte IVSpDefense;
    public byte IVSpeed;

    // Effort Values (0-252, max 510 total)
    public byte EVHP;
    public byte EVAttack;
    public byte EVDefense;
    public byte EVSpAttack;
    public byte EVSpDefense;
    public byte EVSpeed;
}
```

**Stat Calculation**:
```csharp
public static int CalculateStat(
    int baseStat, int iv, int ev, int level, bool isHP)
{
    if (isHP)
    {
        return ((2 * baseStat + iv + (ev / 4)) * level / 100) + level + 10;
    }
    else
    {
        return ((2 * baseStat + iv + (ev / 4)) * level / 100) + 5;
    }
}
```

---

### Health
**Purpose**: Current health state

```csharp
public struct Health
{
    /// <summary>
    /// Current HP
    /// </summary>
    public int Current;

    /// <summary>
    /// Maximum HP
    /// </summary>
    public int Maximum;

    /// <summary>
    /// Is creature fainted?
    /// </summary>
    public bool IsFainted => Current <= 0;

    /// <summary>
    /// Health percentage (0-1)
    /// </summary>
    public float Percentage => (float)Current / Maximum;
}
```

**Common Operations**:
```csharp
// Damage
ref var health = ref entity.Get<Health>();
health.Current = Math.Max(0, health.Current - damage);

// Heal
health.Current = Math.Min(health.Maximum, health.Current + healAmount);

// Full restore
health.Current = health.Maximum;
```

---

### CreatureType
**Purpose**: Creature type information

```csharp
public struct CreatureType
{
    /// <summary>
    /// Primary type
    /// </summary>
    public string PrimaryType;

    /// <summary>
    /// Secondary type (null if mono-type)
    /// </summary>
    public string? SecondaryType;

    /// <summary>
    /// Check if creature has a specific type
    /// </summary>
    public bool HasType(string type)
    {
        return PrimaryType == type || SecondaryType == type;
    }
}
```

---

### Ability
**Purpose**: Creature ability

```csharp
public struct Ability
{
    /// <summary>
    /// Ability ID
    /// </summary>
    public string AbilityId;

    /// <summary>
    /// Is this ability active?
    /// </summary>
    public bool IsActive;

    /// <summary>
    /// Ability-specific data
    /// </summary>
    public Dictionary<string, object> Data;
}
```

---

### Moveset
**Purpose**: Learned moves

```csharp
public struct Moveset
{
    /// <summary>
    /// Currently learned moves (max 4)
    /// </summary>
    public FixedArray4<Move> Moves;

    /// <summary>
    /// Number of moves learned
    /// </summary>
    public int Count;
}

public struct Move
{
    public string MoveId;
    public int CurrentPP;
    public int MaxPP;
}

// Fixed-size array to avoid allocations
public struct FixedArray4<T>
{
    public T Item0;
    public T Item1;
    public T Item2;
    public T Item3;

    public T this[int index]
    {
        get => index switch
        {
            0 => Item0,
            1 => Item1,
            2 => Item2,
            3 => Item3,
            _ => throw new IndexOutOfRangeException()
        };
        set
        {
            switch (index)
            {
                case 0: Item0 = value; break;
                case 1: Item1 = value; break;
                case 2: Item2 = value; break;
                case 3: Item3 = value; break;
                default: throw new IndexOutOfRangeException();
            }
        }
    }
}
```

---

## Battle Components

### BattleState
**Purpose**: Creature's state in battle

```csharp
public struct BattleState
{
    /// <summary>
    /// Current status condition (none, poison, burn, etc.)
    /// </summary>
    public StatusCondition Status;

    /// <summary>
    /// Stat stage modifications (-6 to +6)
    /// </summary>
    public StatStages StatStages;

    /// <summary>
    /// Volatile conditions (confusion, flinch, etc.)
    /// </summary>
    public VolatileConditions Volatile;

    /// <summary>
    /// Has creature moved this turn?
    /// </summary>
    public bool HasMoved;
}

public struct StatStages
{
    public sbyte Attack;     // -6 to +6
    public sbyte Defense;
    public sbyte SpAttack;
    public sbyte SpDefense;
    public sbyte Speed;
    public sbyte Accuracy;
    public sbyte Evasion;
}

[Flags]
public enum VolatileConditions
{
    None = 0,
    Confusion = 1 << 0,
    Flinch = 1 << 1,
    Trapped = 1 << 2,
    Infatuated = 1 << 3,
    Taunted = 1 << 4
}
```

---

### TurnAction
**Purpose**: Action to perform this turn

```csharp
public struct TurnAction
{
    public ActionType Type;
    public string? MoveId;
    public int? TargetSlot;
    public string? ItemId;
    public int? SwitchToSlot;
}

public enum ActionType
{
    None,
    UseMove,
    UseItem,
    Switch,
    Run
}
```

---

## AI Components

### AIController
**Purpose**: AI behavior control

```csharp
public struct AIController
{
    /// <summary>
    /// AI strategy type
    /// </summary>
    public AIStrategy Strategy;

    /// <summary>
    /// AI difficulty/skill level (0-100)
    /// </summary>
    public int SkillLevel;

    /// <summary>
    /// Custom AI parameters
    /// </summary>
    public Dictionary<string, float> Parameters;
}

public enum AIStrategy
{
    Random,
    Aggressive,
    Defensive,
    Smart,
    Custom
}
```

---

### Pathfinding
**Purpose**: Navigation data

```csharp
public struct Pathfinding
{
    /// <summary>
    /// Current path to follow
    /// </summary>
    public List<Vector2> Path;

    /// <summary>
    /// Current waypoint index
    /// </summary>
    public int CurrentWaypoint;

    /// <summary>
    /// Target position
    /// </summary>
    public Vector2? Target;

    /// <summary>
    /// Movement speed
    /// </summary>
    public float Speed;
}
```

---

## Player Components

### PlayerControlled
**Purpose**: Marks entity as player-controlled

```csharp
public struct PlayerControlled
{
    /// <summary>
    /// Player ID (for multiplayer)
    /// </summary>
    public int PlayerId;

    /// <summary>
    /// Input enabled?
    /// </summary>
    public bool InputEnabled;
}
```

---

### Inventory
**Purpose**: Player's items

```csharp
public struct Inventory
{
    /// <summary>
    /// Items and quantities
    /// </summary>
    public Dictionary<string, int> Items;

    /// <summary>
    /// Maximum item capacity
    /// </summary>
    public int Capacity;
}
```

---

## Tag Components

Tag components have no data - they just mark entities with a property.

```csharp
// Mark entity as wild creature
public struct WildCreature { }

// Mark entity as trainer
public struct Trainer { }

// Mark entity as important/cannot delete
public struct Persistent { }

// Mark for destruction next frame
public struct DestroyTag { }

// Mark as belonging to player's party
public struct PartyMember
{
    public int SlotIndex; // 0-5
}
```

**Usage**:
```csharp
// Add tag
entity.Add<WildCreature>();

// Check for tag
if (entity.Has<WildCreature>())
{
    // It's a wild creature
}

// Query tagged entities
var wildCreatures = world.Query<WildCreature, Position, Health>();
```

---

## Component Archetypes

Common component combinations:

### Basic Creature
```csharp
entity.Add<Position>();
entity.Add<Sprite>();
entity.Add<CreatureStats>();
entity.Add<Health>();
entity.Add<CreatureType>();
entity.Add<Ability>();
entity.Add<Moveset>();
```

### Wild Encounter
```csharp
// Basic creature +
entity.Add<WildCreature>();
entity.Add<AIController>();
```

### Player's Creature
```csharp
// Basic creature +
entity.Add<PartyMember>();
entity.Add<PlayerControlled>();
```

### Battle Participant
```csharp
// Creature components +
entity.Add<BattleState>();
entity.Add<TurnAction>();
```

## Best Practices

### 1. Keep Components Small
```csharp
// ✅ GOOD: Focused components
public struct Position { public float X, Y; }
public struct Velocity { public float VX, VY; }

// ❌ BAD: Kitchen sink component
public struct CreatureData
{
    public float X, Y, VX, VY;
    public int HP, Attack, Defense;
    public string Name, Type1, Type2;
    // Too much!
}
```

### 2. Use Structs, Not Classes
```csharp
// ✅ GOOD: Struct (value type)
public struct Health
{
    public int Current;
    public int Maximum;
}

// ❌ BAD: Class (reference type, causes allocations)
public class Health
{
    public int Current { get; set; }
    public int Maximum { get; set; }
}
```

### 3. Avoid References to Other Entities
```csharp
// ❌ BAD: Storing entity references
public struct Target
{
    public Entity TargetEntity; // Don't do this!
}

// ✅ GOOD: Store ID or position
public struct Target
{
    public Vector2 TargetPosition;
    // Or use a lookup system
}
```

### 4. Make Components Serializable
```csharp
[Serializable]
public struct Position
{
    public float X;
    public float Y;

    // Explicit serialization if needed
    public void Serialize(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
    }

    public static Position Deserialize(BinaryReader reader)
    {
        return new Position
        {
            X = reader.ReadSingle(),
            Y = reader.ReadSingle()
        };
    }
}
```

## Component Lifecycle

```csharp
// Create entity with components
var entity = world.Create();
entity.Add<Position>();
entity.Add<Velocity>();

// Add component later
entity.Add(new Health { Current = 100, Maximum = 100 });

// Get component (read-only)
var health = entity.Get<Health>();

// Get component reference (for modification)
ref var healthRef = ref entity.Get<Health>();
healthRef.Current -= 10;

// Check if entity has component
if (entity.Has<PlayerControlled>())
{
    // ...
}

// Remove component
entity.Remove<Velocity>();

// Destroy entity (removes all components)
world.Destroy(entity);
```

## Next Steps

- [System Reference](systems.md) - Learn about systems that process components
- [ECS Architecture](../architecture/ecs-architecture.md) - Deep dive into ECS design
- [Creating Custom Components](../developer/custom-components.md) - Add your own

---

*Last Updated: 2025-10-22*
