# Arch ECS Advanced Integration Guide

## Overview

This guide demonstrates advanced integration patterns for Arch ECS 2.x in PokeNET. These examples showcase high-performance techniques, deferred execution patterns, query optimization, and parallel processing.

## Table of Contents

1. [Command Buffer Pattern](#command-buffer-pattern)
2. [Query Optimization](#query-optimization)
3. [Parallel Processing](#parallel-processing)
4. [Advanced System Patterns](#advanced-system-patterns)
5. [Performance Best Practices](#performance-best-practices)

---

## Command Buffer Pattern

### What is it?

Command buffers enable **deferred structural changes** to entities. This pattern is essential for:
- Avoiding modification-during-iteration exceptions
- Batching structural changes for better cache performance
- Thread-safe entity modifications
- Reducing world lock contention

### Example Implementation

```csharp
// Deferred entity creation
var commands = new List<Action>();

_world.Query(in query, (Entity entity, ref Health health) =>
{
    if (health.Current <= 0)
    {
        // Record command instead of immediate execution
        commands.Add(() =>
        {
            var loot = _world.Create(new LootDrop { Position = entity.Get<Position>() });
            _world.Destroy(entity);
        });
    }
});

// Execute all commands after iteration
foreach (var command in commands)
{
    command();
}
```

### Use Cases

1. **Spawning projectiles during combat**
   - Defer entity creation until after query iteration
   - Prevents collection modification exceptions

2. **Batch entity destruction**
   - Collect all dead entities
   - Destroy in single batch for better performance

3. **Component addition/removal**
   - Defer structural changes
   - Apply in batches

### Advanced: Transactional Command Buffer

```csharp
var txBuffer = new TransactionalCommandBuffer();

txBuffer.RecordCommand(
    execute: () => entity.Add(new StatusEffect()),
    undo: () => entity.Remove<StatusEffect>()
);

txBuffer.Execute(); // Apply changes

if (validationFailed)
{
    txBuffer.Rollback(); // Undo everything
}
```

**File**: `examples/ArchExtended/CommandBufferExample.cs`

---

## Query Optimization

### Cache QueryDescription Instances

```csharp
// ✅ GOOD: Reuse query descriptions
private static readonly QueryDescription AliveEntitiesQuery =
    new QueryDescription().WithAll<Health, Stats>().WithNone<Dead>();

// Use cached query
_world.Query(in AliveEntitiesQuery, (ref Health health) => { /* ... */ });
```

```csharp
// ❌ BAD: Creating new query every frame
_world.Query(in new QueryDescription().WithAll<Health>(), /* ... */);
```

### Use WithNone for Exclusion

```csharp
// ✅ EFFICIENT: Structural exclusion
var query = new QueryDescription()
    .WithAll<Health>()
    .WithNone<Dead>();  // Fast archetype filtering

_world.Query(in query, (ref Health health) => { /* Process */ });
```

```csharp
// ❌ INEFFICIENT: Runtime filtering
var query = new QueryDescription().WithAll<Health>();

_world.Query(in query, (Entity entity, ref Health health) =>
{
    if (!entity.Has<Dead>())  // Slower runtime check
    {
        // Process
    }
});
```

### Use WithAny for Optional Components

```csharp
// Query entities with either Attacking OR Defending
var combatQuery = new QueryDescription()
    .WithAll<Health, Stats>()
    .WithAny<Attacking, Defending>();

_world.Query(in combatQuery, (Entity entity, ref Health health) =>
{
    bool isAttacking = entity.Has<Attacking>();
    bool isDefending = entity.Has<Defending>();
    // Handle both cases
});
```

### Performance Metrics

```csharp
var metrics = queryOptimizer.MeasureQueryPerformance();

// Output:
// Simple Query: 10,000 entities in 0.234ms
// Complex Query: 10,000 entities in 0.456ms (1.95x slower)
// Entity Access Query: 10,000 entities in 0.389ms (1.66x slower)
```

**File**: `examples/ArchExtended/QueryOptimizationExample.cs`

---

## Parallel Processing

### PLINQ Component Processing

```csharp
// Collect entities (read-only)
var entities = new List<Entity>();
_world.Query(in query, (Entity entity) => entities.Add(entity));

// Process in parallel
entities.AsParallel()
    .WithDegreeOfParallelism(Environment.ProcessorCount)
    .ForAll(entity =>
    {
        if (entity.TryGet<Health>(out var health))
        {
            // Expensive calculation
            var regen = CalculateRegeneration(health);
            health.Current += regen;
            entity.Set(health);  // Thread-safe component update
        }
    });
```

### Batched Parallel Processing

```csharp
const int batchSize = 1000;
var batches = entities.Chunk(batchSize);

Parallel.ForEach(batches, new ParallelOptions
{
    MaxDegreeOfParallelism = Environment.ProcessorCount
}, batch =>
{
    foreach (var entity in batch)
    {
        // Process batch with better cache locality
    }
});
```

### Thread-Safe Deferred Changes

```csharp
var entitiesToDestroy = new ConcurrentBag<Entity>();
var componentsToAdd = new ConcurrentBag<(Entity, Component)>();

// Parallel processing
Parallel.ForEach(entities, entity =>
{
    if (ShouldDestroy(entity))
    {
        entitiesToDestroy.Add(entity);  // Thread-safe
    }
});

// Main thread applies changes
foreach (var entity in entitiesToDestroy)
{
    _world.Destroy(entity);
}
```

### Parallel Statistics Gathering

```csharp
var stats = entities
    .AsParallel()
    .Select(e => new { Health = e.Get<Health>(), Stats = e.Get<Stats>() })
    .Aggregate((a, b) => new
    {
        TotalHealth = a.TotalHealth + b.TotalHealth,
        Count = a.Count + b.Count
    });

Console.WriteLine($"Average Health: {stats.TotalHealth / stats.Count}");
```

### When to Use Parallelism

✅ **Good candidates:**
- Expensive per-entity calculations (AI, pathfinding)
- Independent component updates
- Statistics aggregation
- Large entity counts (>1000)

❌ **Poor candidates:**
- Simple component updates (overhead > benefit)
- Small entity counts (<100)
- Operations requiring world locks
- Structural changes

**File**: `examples/ArchExtended/ParallelProcessingExample.cs`

---

## Advanced System Patterns

### 1. Reactive Event-Driven Systems

```csharp
public class ReactiveDamageSystem : ISystem
{
    private readonly List<DamageEvent> _pendingDamage = new();

    public ReactiveDamageSystem(IEventBus eventBus)
    {
        // Subscribe to events
        eventBus.Subscribe<DamageEvent>(OnDamageEvent);
    }

    private void OnDamageEvent(DamageEvent evt)
    {
        // Queue for processing in Update
        _pendingDamage.Add(evt);
    }

    public void Update(float deltaTime)
    {
        // Process all pending damage
        foreach (var damage in _pendingDamage)
        {
            ApplyDamage(damage);
        }
        _pendingDamage.Clear();
    }
}
```

**Benefits:**
- Decoupled system communication
- Event-driven architecture
- Clear separation of concerns
- Easy to debug event flow

### 2. Multi-Phase Systems

```csharp
public class MultiPhaseCombatSystem : ISystem
{
    public void Update(float deltaTime)
    {
        // Phase 1: Gather and validate
        BeforeUpdate();

        // Phase 2: Execute combat
        ExecuteCombat();

        // Phase 3: Cleanup
        AfterUpdate();
    }

    private void BeforeUpdate() { /* Preparation */ }
    private void ExecuteCombat() { /* Main logic */ }
    private void AfterUpdate() { /* Cleanup */ }
}
```

**Use cases:**
- Complex multi-stage operations
- Dependency management between stages
- Clear execution order

### 3. Performance-Budgeted Systems

```csharp
public class BudgetedAISystem : ISystem
{
    private readonly TimeSpan _timeBudget = TimeSpan.FromMilliseconds(2);

    public void Update(float deltaTime)
    {
        var stopwatch = Stopwatch.StartNew();
        int processed = 0;

        _world.Query(in _aiQuery, (Entity entity) =>
        {
            if (stopwatch.Elapsed >= _timeBudget)
            {
                // Exit early to stay within budget
                return;
            }

            ProcessAI(entity);
            processed++;
        });

        if (stopwatch.Elapsed > _timeBudget)
        {
            _logger.LogWarning("AI exceeded budget!");
        }
    }
}
```

**Benefits:**
- Guaranteed frame time
- Graceful degradation under load
- Performance monitoring

**File**: `examples/ArchExtended/AdvancedSystemExample.cs`

---

## Performance Best Practices

### 1. Component Design

```csharp
// ✅ GOOD: Small value types
public struct Position
{
    public float X;
    public float Y;
}

// ✅ GOOD: Reference for large data
public struct Sprite
{
    public int TextureId;  // Reference, not full texture
}

// ❌ BAD: Large value types
public struct BadComponent
{
    public float[] LargeArray;  // Heap allocation
}
```

### 2. Query Performance

| Pattern | Performance | Use Case |
|---------|-------------|----------|
| `WithAll<T>` | ⭐⭐⭐⭐⭐ | Required components |
| `WithNone<T>` | ⭐⭐⭐⭐⭐ | Exclude entities |
| `WithAny<T>` | ⭐⭐⭐⭐ | Optional components |
| Runtime `Has<T>` check | ⭐⭐⭐ | Conditional logic |
| Creating new `QueryDescription` | ⭐⭐ | One-off queries |

### 3. Batch Processing

```csharp
// ✅ GOOD: Batch structural changes
var toDestroy = new List<Entity>();
_world.Query(/* ... */, entity => toDestroy.Add(entity));
foreach (var e in toDestroy) _world.Destroy(e);

// ❌ BAD: Immediate destruction during iteration
_world.Query(/* ... */, entity => _world.Destroy(entity)); // May cause issues
```

### 4. Memory Management

```csharp
// ✅ GOOD: Reuse collections
private readonly List<Entity> _entityCache = new(1024);

public void Update()
{
    _entityCache.Clear();  // Reuse, don't recreate
    _world.Query(/* ... */, entity => _entityCache.Add(entity));
}

// ❌ BAD: Allocate every frame
public void Update()
{
    var entities = new List<Entity>();  // GC pressure
}
```

### 5. Parallel Processing Guidelines

```csharp
// ✅ GOOD: Worth parallelizing
if (entities.Count > 1000 && IsExpensiveOperation())
{
    ProcessInParallel(entities);
}

// ❌ BAD: Overhead > benefit
if (entities.Count < 100 && IsSimpleOperation())
{
    ProcessInParallel(entities);  // Don't parallelize!
}
```

---

## Integration Checklist

- [ ] Replace manual command buffering with structured pattern
- [ ] Cache all frequently-used QueryDescription instances
- [ ] Use `WithNone` instead of runtime filtering
- [ ] Profile before adding parallelization
- [ ] Implement performance budgets for expensive systems
- [ ] Use event-driven architecture for system communication
- [ ] Batch structural changes (create/destroy/add/remove)
- [ ] Keep components small and value-based
- [ ] Reuse collections to reduce GC pressure
- [ ] Monitor system execution times

---

## Example Integration into PokeNET

### Before: Manual Deferred Operations

```csharp
public class BattleSystem : ISystem
{
    public void Update(float deltaTime)
    {
        _world.Query(/* ... */, (Entity entity) =>
        {
            // Manual collection, error-prone
            if (ShouldDie(entity))
            {
                // Can't destroy here safely!
            }
        });
    }
}
```

### After: Command Buffer Pattern

```csharp
public class BattleSystem : ISystem
{
    private readonly List<Action> _commands = new();

    public void Update(float deltaTime)
    {
        _commands.Clear();

        _world.Query(/* ... */, (Entity entity) =>
        {
            if (ShouldDie(entity))
            {
                _commands.Add(() => _world.Destroy(entity));
            }
        });

        // Execute safely
        foreach (var cmd in _commands) cmd();
    }
}
```

---

## Resources

- **Examples**: `/examples/ArchExtended/`
- **Tests**: `/tests/ArchExtended/`
- **Arch Documentation**: https://github.com/genaray/Arch
- **Performance Profiling**: Use BenchmarkDotNet or built-in metrics

---

## Summary

These advanced Arch ECS patterns provide:

1. ⚡ **Better Performance**: Query optimization, parallelization, batching
2. 🛡️ **Safety**: Deferred structural changes, thread-safe operations
3. 🏗️ **Architecture**: Event-driven systems, multi-phase execution
4. 📊 **Monitoring**: Performance budgets, metrics gathering
5. 🔄 **Maintainability**: Clear patterns, reusable code

Integrate these patterns incrementally, profile results, and adjust based on your specific performance requirements.
