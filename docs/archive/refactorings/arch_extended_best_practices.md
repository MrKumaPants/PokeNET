# Arch.Extended Best Practices Research

**Date**: 2025-10-24
**Research Goal**: Determine if PokeNET's "Enhanced" pattern is idiomatic or an antipattern compared to Arch.Extended's official approach

---

## Executive Summary

### 🚨 CRITICAL FINDING: We Are Duplicating Arch.Extended Functionality

**Our custom `SystemBase` and `SystemBaseEnhanced` classes duplicate functionality that Arch.Extended ALREADY PROVIDES out of the box.**

### The Verdict

- ✅ **Good News**: Our lifecycle pattern (BeforeUpdate/Update/AfterUpdate) matches Arch's official design
- ⚠️ **Bad News**: We're reimplementing what Arch.System.BaseSystem already provides
- 🔴 **Critical**: We should be inheriting from `Arch.System.BaseSystem<World, float>` instead of our custom base

---

## What Arch.Extended Actually Provides

### 1. Official Base Class: `Arch.System.BaseSystem<W, T>`

From the Arch.System XML documentation (version 1.1.0):

```csharp
/// <summary>
/// A basic implementation of a <see cref="ISystem{T}"/>.
/// </summary>
/// <typeparam name="W">The world type.</typeparam>
/// <typeparam name="T">The type passed to the ISystem interface.</typeparam>
public abstract class BaseSystem<W, T> : ISystem<T>, IDisposable
{
    public BaseSystem(W world);

    public W World { get; }

    public virtual void Initialize();
    public virtual void BeforeUpdate(ref T t);
    public virtual void Update(ref T t);
    public virtual void AfterUpdate(ref T t);
    public virtual void Dispose();
}
```

### 2. Official Interface: `Arch.System.ISystem<T>`

```csharp
/// <summary>
/// An interface providing several methods for a system.
/// </summary>
/// <typeparam name="T">The type passed to each method. For example a delta time or some other data.</typeparam>
public interface ISystem<T>
{
    void Initialize();
    void BeforeUpdate(ref T t);
    void Update(ref T t);
    void AfterUpdate(ref T t);
}
```

### 3. Official System Group: `Arch.System.Group<T>`

Arch.Extended provides `Group<T>` to organize systems:

```csharp
var group = new Group<float>("GameSystems",
    new MovementSystem(world),
    new RenderSystem(world),
    new BattleSystem(world)
);

group.Initialize();        // Calls Initialize on all systems
group.BeforeUpdate(ref dt); // Calls BeforeUpdate on all systems
group.Update(ref dt);       // Calls Update on all systems
group.AfterUpdate(ref dt);  // Calls AfterUpdate on all systems
group.Dispose();            // Calls Dispose on all systems
```

---

## How Arch.Extended Is MEANT To Be Used

### Official Example (from GitHub genaray/Arch.Extended):

```csharp
// Components
public struct Position { float X, Y; }
public struct Velocity { float Dx, Dy; }

// BaseSystem provides several useful methods for interacting and structuring systems
public class MovementSystem : BaseSystem<World, float>
{
    public MovementSystem(World world) : base(world) { }

    // Override lifecycle methods
    public override void Initialize()
    {
        // One-time initialization
    }

    public override void BeforeUpdate(ref float deltaTime)
    {
        // Pre-frame setup
    }

    public override void Update(ref float deltaTime)
    {
        // Main system logic
        MoveQuery(ref deltaTime); // Calls source-generated query
    }

    public override void AfterUpdate(ref float deltaTime)
    {
        // Post-frame cleanup
    }

    // Source generator creates optimized query from this method
    [Query]
    public void Move([Data] in float time, ref Position pos, ref Velocity vel)
    {
        pos.X += time * vel.X;
        pos.Y += time * vel.Y;
    }

    // Filtered query with attributes
    [Query]
    [All<Player, Mob>, Any<Idle, Moving>, None<Alive>]
    public void ResetVelocity(ref Velocity vel)
    {
        vel = new Velocity { X = 0, Y = 0 };
    }
}
```

### Key Features of Official BaseSystem:

1. **Generic Type Parameters**:
   - `W` = World type (usually `Arch.Core.World`)
   - `T` = State/context type (usually `float` for deltaTime, or `GameTime`)

2. **Lifecycle Methods** (all virtual, can be overridden):
   - `Initialize()` - Called once before first update
   - `BeforeUpdate(ref T)` - Called before main update
   - `Update(ref T)` - Main system logic
   - `AfterUpdate(ref T)` - Called after main update
   - `Dispose()` - Cleanup resources

3. **World Access**:
   - Protected `World` property for querying entities
   - Passed in constructor and stored automatically

4. **Source Generator Integration**:
   - Methods marked with `[Query]` attribute are auto-converted to high-performance queries
   - `[Data]` attribute marks non-component parameters
   - Filter attributes: `[All<>]`, `[Any<>]`, `[None<>]`, `[Exclusive<>]`

---

## What We're Doing Wrong

### Our Custom Implementation

**File: `PokeNET.Domain/ECS/Systems/SystemBase.cs`**

```csharp
public abstract class SystemBase : ISystem
{
    protected readonly ILogger Logger;
    private World? _world;

    protected World World { get; private set; }

    public virtual int Priority => 0;
    public bool IsEnabled { get; set; } = true;

    protected SystemBase(ILogger logger)
    {
        Logger = logger;
    }

    public virtual void Initialize(World world)
    {
        World = world;
        OnInitialize();
    }

    public void Update(float deltaTime)
    {
        if (!IsEnabled) return;
        OnUpdate(deltaTime);
    }

    protected abstract void OnUpdate(float deltaTime);

    public virtual void Dispose() { }
}
```

**File: `PokeNET.Domain/ECS/Systems/SystemBaseEnhanced.cs`**

```csharp
public abstract class SystemBaseEnhanced : SystemBase
{
    private readonly Stopwatch _performanceStopwatch;
    private double _totalUpdateTime;
    private long _updateCount;

    public double LastUpdateTime { get; private set; }
    public long UpdateCount => _updateCount;
    public double AverageUpdateTime => _updateCount > 0 ? _totalUpdateTime / _updateCount : 0;

    protected SystemBaseEnhanced(ILogger logger) : base(logger) { }

    protected sealed override void OnUpdate(float deltaTime)
    {
        _performanceStopwatch.Restart();

        BeforeUpdate(deltaTime);   // ✅ Same as Arch
        DoUpdate(deltaTime);        // ❌ Should be "Update" not "DoUpdate"
        AfterUpdate(deltaTime);     // ✅ Same as Arch

        _performanceStopwatch.Stop();
        LastUpdateTime = _performanceStopwatch.Elapsed.TotalMilliseconds;
        _totalUpdateTime += LastUpdateTime;
        _updateCount++;
    }

    protected virtual void BeforeUpdate(float deltaTime) { }
    protected abstract void DoUpdate(float deltaTime);  // ❌ Should be "Update"
    protected virtual void AfterUpdate(float deltaTime) { }
}
```

### Problems Identified:

1. **❌ Custom ISystem Interface**: We define our own `ISystem` instead of using `Arch.System.ISystem<T>`
2. **❌ Custom Base Class**: We reimplement `SystemBase` instead of inheriting from `Arch.System.BaseSystem<W, T>`
3. **❌ Incorrect Method Names**: We use `DoUpdate` instead of `Update` (breaks Arch conventions)
4. **❌ World Initialization Pattern**: Arch passes world in constructor, we pass it to `Initialize()`
5. **❌ Missing Source Generator Support**: We can't use `[Query]` attributes with our custom base
6. **❌ No Group Support**: Our systems don't work with `Arch.System.Group<T>` out of the box
7. **✅ Performance Tracking**: This is the ONLY thing we add that Arch doesn't provide

### Current System Inheritance:

- `BattleSystem : SystemBase` ❌ Should inherit from `BaseSystem<World, float>`
- `InputSystem : SystemBase` ❌ Should inherit from `BaseSystem<World, float>`
- `RenderSystem : SystemBase` ❌ Should inherit from `BaseSystem<World, float>`
- `MovementSystem : SystemBaseEnhanced` ❌ Should inherit from `BaseSystem<World, float>` with custom metrics

---

## The "Enhanced" Pattern: Antipattern or Valid?

### Is SystemBaseEnhanced an Antipattern?

**NO, but it's implemented incorrectly.**

### Why It's Not a Pure Antipattern:

1. **Performance Metrics**: Adding metrics tracking is a valid extension
2. **Lifecycle Hooks**: BeforeUpdate/Update/AfterUpdate matches Arch's official pattern
3. **Logging Integration**: Adding structured logging is reasonable

### Why Our Implementation Is Wrong:

1. **Duplicates Arch Functionality**: Arch.System.BaseSystem ALREADY has BeforeUpdate/Update/AfterUpdate
2. **Breaks Compatibility**: Can't use with `Group<T>` or source generator
3. **Wrong Base Class**: Should extend Arch's BaseSystem, not replace it
4. **Method Naming**: "DoUpdate" breaks Arch conventions

---

## Recommended Migration Path

### Phase 1: Replace Custom Base with Arch.System.BaseSystem

**Before:**
```csharp
public class MovementSystem : SystemBaseEnhanced
{
    public MovementSystem(ILogger<MovementSystem> logger, IEventBus? eventBus = null)
        : base(logger)
    {
    }

    protected override void BeforeUpdate(float deltaTime) { }
    protected override void DoUpdate(float deltaTime) { }    // ❌ Wrong name
    protected override void AfterUpdate(float deltaTime) { }
}
```

**After:**
```csharp
public class MovementSystem : BaseSystem<World, float>
{
    private readonly ILogger _logger;
    private readonly IEventBus? _eventBus;

    public MovementSystem(World world, ILogger<MovementSystem> logger, IEventBus? eventBus = null)
        : base(world)  // ✅ Arch pattern
    {
        _logger = logger;
        _eventBus = eventBus;
    }

    public override void Initialize()
    {
        // One-time initialization
    }

    public override void BeforeUpdate(ref float deltaTime)  // ✅ ref parameter
    {
        // Pre-frame setup
    }

    public override void Update(ref float deltaTime)  // ✅ Correct name
    {
        // Main logic - can call source-generated queries
        ProcessMovementQuery(ref deltaTime);
    }

    public override void AfterUpdate(ref float deltaTime)  // ✅ ref parameter
    {
        // Post-frame cleanup
    }

    // Source generator creates optimized query
    [Query]
    public void ProcessMovement([Data] in float deltaTime, ref GridPosition pos, ref Direction dir)
    {
        // Movement logic
    }
}
```

### Phase 2: Add Performance Metrics as Decorator/Wrapper

Instead of `SystemBaseEnhanced`, create a performance-tracking decorator:

```csharp
public class PerformanceTrackedSystem<W, T> : BaseSystem<W, T>
{
    private readonly BaseSystem<W, T> _innerSystem;
    private readonly Stopwatch _stopwatch = new();
    private double _totalUpdateTime;
    private long _updateCount;

    public double LastUpdateTime { get; private set; }
    public double AverageUpdateTime => _updateCount > 0 ? _totalUpdateTime / _updateCount : 0;

    public PerformanceTrackedSystem(BaseSystem<W, T> innerSystem, W world) : base(world)
    {
        _innerSystem = innerSystem;
    }

    public override void BeforeUpdate(ref T state)
    {
        _stopwatch.Restart();
        _innerSystem.BeforeUpdate(ref state);
    }

    public override void Update(ref T state)
    {
        _innerSystem.Update(ref state);
    }

    public override void AfterUpdate(ref T state)
    {
        _innerSystem.AfterUpdate(ref state);
        _stopwatch.Stop();
        LastUpdateTime = _stopwatch.Elapsed.TotalMilliseconds;
        _totalUpdateTime += LastUpdateTime;
        _updateCount++;
    }
}
```

### Phase 3: Use Arch.System.Group for System Management

**Before (our custom ISystemManager):**
```csharp
public interface ISystemManager
{
    void RegisterSystem(ISystem system);
    void UpdateSystems(float deltaTime);
}
```

**After (use Arch.System.Group):**
```csharp
var gameGroup = new Group<float>("GameSystems",
    new MovementSystem(world, logger),
    new RenderSystem(world, logger),
    new BattleSystem(world, logger)
);

gameGroup.Initialize();

// In game loop
gameGroup.BeforeUpdate(ref deltaTime);
gameGroup.Update(ref deltaTime);
gameGroup.AfterUpdate(ref deltaTime);
```

### Phase 4: Leverage Source Generator for Queries

**Before (manual queries):**
```csharp
protected override void DoUpdate(float deltaTime)
{
    World.Query(in QueryExtensions.MovementQuery, (Entity entity, ref GridPosition pos, ref Direction dir) =>
    {
        // Movement logic
    });
}
```

**After (source-generated queries):**
```csharp
public override void Update(ref float deltaTime)
{
    ProcessMovementQuery(ref deltaTime);  // Calls source-generated method
}

[Query]
public void ProcessMovement([Data] in float deltaTime, ref GridPosition pos, ref Direction dir)
{
    // Movement logic - automatically converted to optimized query
}
```

---

## Benefits of Migration

### 1. Source Generator Support
- Zero-allocation queries generated at compile-time
- Type-safe query parameters
- Automatic parallel execution with `[Query(Parallel = true)]`

### 2. Group Management
- Organize systems into logical groups
- Automatic lifecycle management
- Built-in system ordering

### 3. Compatibility
- Works with all Arch.Extended features
- Future-proof as Arch evolves
- Community best practices

### 4. Less Code to Maintain
- Remove ~200 lines of custom base classes
- Remove custom `ISystem` interface
- Remove custom `ISystemManager` interface

### 5. Better Performance
- Source-generated queries are faster than manual queries
- Arch's BaseSystem is optimized for minimal overhead

---

## Migration Checklist

### Step 1: Update Dependencies
- ✅ Already have: `Arch.System` v1.1.0
- ✅ Already have: `Arch.System.SourceGenerator` v2.1.0

### Step 2: Replace Custom Interfaces
- [ ] Remove `PokeNET.Domain/ECS/Systems/ISystem.cs`
- [ ] Update to use `Arch.System.ISystem<float>`

### Step 3: Replace Custom Base Classes
- [ ] Remove `PokeNET.Domain/ECS/Systems/SystemBase.cs`
- [ ] Remove `PokeNET.Domain/ECS/Systems/SystemBaseEnhanced.cs`
- [ ] Update all systems to inherit from `Arch.System.BaseSystem<World, float>`

### Step 4: Update System Constructors
- [ ] Change from `Initialize(World world)` to constructor parameter
- [ ] Update dependency injection to pass `World` in constructor

### Step 5: Rename Methods
- [ ] Rename `DoUpdate` to `Update`
- [ ] Add `ref` to all lifecycle method parameters
- [ ] Update `OnInitialize()` to `Initialize()`

### Step 6: Add Source Generator Attributes
- [ ] Mark query methods with `[Query]`
- [ ] Add `[Data]` to non-component parameters
- [ ] Add filter attributes where appropriate

### Step 7: Replace ISystemManager with Group
- [ ] Remove custom `ISystemManager` interface
- [ ] Create `Group<float>` instances for system organization
- [ ] Update game loop to use `Group.BeforeUpdate/Update/AfterUpdate`

### Step 8: Optional: Add Performance Tracking
- [ ] Create decorator pattern for metrics tracking
- [ ] Keep performance monitoring without custom base class

---

## Code Examples: Before vs After

### Example 1: Simple System

**Before:**
```csharp
public class RenderSystem : SystemBase
{
    public RenderSystem(ILogger<RenderSystem> logger) : base(logger) { }

    protected override void OnUpdate(float deltaTime)
    {
        World.Query(in QueryExtensions.RenderQuery, (ref Sprite sprite, ref GridPosition pos) =>
        {
            // Render logic
        });
    }
}
```

**After:**
```csharp
public partial class RenderSystem : BaseSystem<World, float>
{
    private readonly ILogger _logger;

    public RenderSystem(World world, ILogger<RenderSystem> logger) : base(world)
    {
        _logger = logger;
    }

    public override void Update(ref float deltaTime)
    {
        RenderQuery(ref deltaTime);  // Source-generated
    }

    [Query]
    public void Render([Data] in float deltaTime, ref Sprite sprite, ref GridPosition pos)
    {
        // Render logic - automatically converted to optimized query
    }
}
```

### Example 2: System with Lifecycle Hooks

**Before:**
```csharp
public class MovementSystem : SystemBaseEnhanced
{
    public MovementSystem(ILogger<MovementSystem> logger) : base(logger) { }

    protected override void BeforeUpdate(float deltaTime)
    {
        // Pre-frame setup
    }

    protected override void DoUpdate(float deltaTime)
    {
        // Main logic
    }

    protected override void AfterUpdate(float deltaTime)
    {
        // Post-frame cleanup
    }
}
```

**After:**
```csharp
public partial class MovementSystem : BaseSystem<World, float>
{
    private readonly ILogger _logger;

    public MovementSystem(World world, ILogger<MovementSystem> logger) : base(world)
    {
        _logger = logger;
    }

    public override void BeforeUpdate(ref float deltaTime)
    {
        // Pre-frame setup
    }

    public override void Update(ref float deltaTime)
    {
        ProcessMovementQuery(ref deltaTime);  // Source-generated
    }

    public override void AfterUpdate(ref float deltaTime)
    {
        // Post-frame cleanup
    }

    [Query]
    public void ProcessMovement([Data] in float deltaTime, ref GridPosition pos, ref Direction dir)
    {
        // Movement logic
    }
}
```

### Example 3: System Manager Replacement

**Before:**
```csharp
public class SystemManager : ISystemManager
{
    private readonly List<ISystem> _systems = new();

    public void RegisterSystem(ISystem system)
    {
        _systems.Add(system);
        system.Initialize(_world);
    }

    public void UpdateSystems(float deltaTime)
    {
        foreach (var system in _systems.OrderBy(s => s.Priority))
        {
            if (system.IsEnabled)
                system.Update(deltaTime);
        }
    }
}
```

**After:**
```csharp
// Use Arch's built-in Group
var gameGroup = new Group<float>("GameSystems",
    new MovementSystem(world, movementLogger),
    new RenderSystem(world, renderLogger),
    new BattleSystem(world, battleLogger)
);

gameGroup.Initialize();

// In game loop
float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
gameGroup.BeforeUpdate(ref deltaTime);
gameGroup.Update(ref deltaTime);
gameGroup.AfterUpdate(ref deltaTime);
```

---

## Performance Comparison

### Manual Query (Current):
```csharp
// ~500 LOC of custom base classes + manual query creation
protected override void OnUpdate(float deltaTime)
{
    World.Query(in QueryExtensions.MovementQuery, (Entity entity, ref GridPosition pos, ref Direction dir) =>
    {
        // Logic
    });
}
```

### Source-Generated Query (After Migration):
```csharp
// ~50 LOC of system + automatic query optimization
public override void Update(ref float deltaTime)
{
    ProcessMovementQuery(ref deltaTime);  // Zero-allocation, optimized at compile-time
}

[Query]
public void ProcessMovement([Data] in float deltaTime, ref GridPosition pos, ref Direction dir)
{
    // Logic - automatically converted to fastest possible query
}
```

**Performance Benefits:**
- **Zero allocations**: Source-generated queries are stack-only
- **Better inlining**: Compiler can optimize generated code
- **Parallel support**: `[Query(Parallel = true)]` for free multithreading
- **Type safety**: Compile-time validation of component access

---

## Conclusion

### Summary

1. **Arch.Extended ALREADY provides `BaseSystem<W, T>` with BeforeUpdate/Update/AfterUpdate lifecycle**
2. **Our `SystemBase` and `SystemBaseEnhanced` duplicate this functionality**
3. **We should migrate to inherit from `Arch.System.BaseSystem<World, float>`**
4. **The "Enhanced" pattern is valid for metrics tracking, but should be a decorator, not a base class**
5. **Source generator support requires using Arch's official base class**

### Antipattern Assessment

**Is `SystemBaseEnhanced` an antipattern?**
- ❌ **YES** - It duplicates Arch.Extended's built-in functionality
- ❌ **YES** - It breaks compatibility with source generator
- ❌ **YES** - It uses wrong method naming (`DoUpdate` vs `Update`)
- ✅ **NO** - The performance tracking feature itself is valid
- ✅ **NO** - The lifecycle pattern matches Arch's design

**Recommendation**: Replace custom base classes with Arch's official `BaseSystem<W, T>` and add performance tracking as an optional decorator or separate metrics system.

### Migration Priority

**HIGH PRIORITY** - This migration should be done soon because:
1. We're maintaining ~200 LOC of unnecessary custom code
2. We're missing out on source generator performance benefits
3. We're not following Arch best practices
4. Future Arch.Extended updates may break our custom implementation

### Estimated Migration Effort

- **Phase 1** (Replace base classes): ~4 hours
- **Phase 2** (Add performance decorator): ~2 hours
- **Phase 3** (Use Group for management): ~2 hours
- **Phase 4** (Add source generator attributes): ~4 hours
- **Total**: ~12 hours of development time

**ROI**:
- Remove ~200 LOC of custom code to maintain
- Gain performance improvements from source generator
- Future-proof system architecture
- Align with community best practices

---

## References

- [Arch.Extended GitHub](https://github.com/genaray/Arch.Extended)
- [Arch Core GitHub](https://github.com/genaray/Arch)
- [Arch.System NuGet Package](https://www.nuget.org/packages/Arch.System/1.1.0)
- [Arch.System.SourceGenerator NuGet Package](https://www.nuget.org/packages/Arch.System.SourceGenerator/2.1.0)
- Local XML Documentation: `~/.nuget/packages/arch.system/1.1.0/lib/net7.0/Arch.System.xml`

---

**Research conducted by**: Claude Code Research Agent
**Research method**:
- Analysis of Arch.System XML documentation
- Review of PokeNET's current implementation
- Web search for Arch.Extended examples
- Comparison with official Arch GitHub repository
