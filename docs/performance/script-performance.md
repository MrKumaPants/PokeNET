# Script Performance Guide

## Introduction

This guide provides best practices and optimization techniques for writing high-performance scripts in PokeNET. Well-optimized scripts ensure smooth gameplay and prevent performance degradation.

## Table of Contents

1. [Performance Fundamentals](#performance-fundamentals)
2. [Profiling and Measurement](#profiling-and-measurement)
3. [Memory Optimization](#memory-optimization)
4. [CPU Optimization](#cpu-optimization)
5. [Entity Query Optimization](#entity-query-optimization)
6. [Event Handler Optimization](#event-handler-optimization)
7. [Caching Strategies](#caching-strategies)
8. [Anti-Patterns](#anti-patterns)
9. [Performance Checklist](#performance-checklist)

---

## Performance Fundamentals

### Performance Budget

Scripts operate within strict performance budgets:

- **Event Handler**: Must complete in <2ms for 60 FPS gameplay
- **Initialization**: Should complete in <100ms
- **Memory**: Limited to 50 MB per script
- **API Calls**: Rate-limited to 10,000 calls/second

### Performance Goals

| Operation | Target | Maximum |
|-----------|--------|---------|
| Event handler | <1ms | 2ms |
| Entity query | <0.5ms | 1ms |
| Data lookup | <0.1ms | 0.5ms |
| Component access | <0.01ms | 0.1ms |

### The 80/20 Rule

Focus optimization efforts on:
- Event handlers (called frequently)
- Entity queries (potentially expensive)
- Initialization code (one-time but impacts startup)

---

## Profiling and Measurement

### Built-in Profiling

Use the script profiler to measure performance:

```csharp
using System.Diagnostics;

public class MyScript
{
    private readonly IScriptApi _api;
    private readonly Stopwatch _stopwatch = new();

    private void OnEvent(BattleEvent evt)
    {
        _stopwatch.Restart();

        // Your code here
        ProcessEvent(evt);

        _stopwatch.Stop();

        if (_stopwatch.ElapsedMilliseconds > 1)
        {
            _api.Logger.LogWarning(
                "Event handler took {Time}ms (target: <1ms)",
                _stopwatch.ElapsedMilliseconds);
        }
    }
}
```

### Performance Metrics

Track key metrics:

```csharp
public class PerformanceMetrics
{
    private int _eventHandlerCalls;
    private int _queriesMade;
    private TimeSpan _totalExecutionTime;
    private long _peakMemoryUsage;

    public void RecordEventHandler(TimeSpan duration)
    {
        _eventHandlerCalls++;
        _totalExecutionTime += duration;
    }

    public void RecordQuery(int resultCount, TimeSpan duration)
    {
        _queriesMade++;

        if (duration.TotalMilliseconds > 1)
        {
            _api.Logger.LogWarning(
                "Slow query: {Count} results in {Time}ms",
                resultCount, duration.TotalMilliseconds);
        }
    }

    public void LogStatistics()
    {
        _api.Logger.LogInformation(
            "Performance Stats: {Calls} event calls, {Queries} queries, " +
            "Avg: {AvgMs}ms, Peak Memory: {PeakMB} MB",
            _eventHandlerCalls,
            _queriesMade,
            _totalExecutionTime.TotalMilliseconds / _eventHandlerCalls,
            _peakMemoryUsage / 1024 / 1024);
    }
}
```

### Benchmarking

Create benchmarks for critical code paths:

```csharp
public class Benchmarks
{
    public void BenchmarkCreatureQuery()
    {
        var stopwatch = Stopwatch.StartNew();
        int iterations = 1000;

        for (int i = 0; i < iterations; i++)
        {
            var creatures = Api.Entities.Query<CreatureStats, Health>();
            // Force enumeration
            var count = creatures.Count();
        }

        stopwatch.Stop();

        var avgMs = stopwatch.ElapsedMilliseconds / (double)iterations;
        _api.Logger.LogInformation(
            "Creature query benchmark: {AvgMs}ms average ({Iterations} iterations)",
            avgMs, iterations);
    }
}
```

---

## Memory Optimization

### Avoid Allocations in Hot Paths

**❌ BAD: Allocates on every call**
```csharp
private void OnBattleTurn(BattleTurnEvent evt)
{
    // Allocates new list every turn
    var creatures = new List<Entity>();
    foreach (var entity in Api.Entities.Query<PlayerControlled>())
    {
        creatures.Add(entity);
    }
}
```

**✅ GOOD: Reuses collection**
```csharp
private readonly List<Entity> _creatureBuffer = new(10);

private void OnBattleTurn(BattleTurnEvent evt)
{
    _creatureBuffer.Clear();
    foreach (var entity in Api.Entities.Query<PlayerControlled>())
    {
        _creatureBuffer.Add(entity);
    }
}
```

### Use Span<T> for Temporary Data

**❌ BAD: Allocates array**
```csharp
private int[] CalculateStats(Entity creature)
{
    var stats = new int[6]; // Heap allocation
    stats[0] = creature.Get<CreatureStats>().HP;
    // ...
    return stats;
}
```

**✅ GOOD: Stack allocation**
```csharp
private void CalculateStats(Entity creature, Span<int> stats)
{
    stats[0] = creature.Get<CreatureStats>().HP;
    stats[1] = creature.Get<CreatureStats>().Attack;
    // ... no heap allocation
}

// Usage
Span<int> stats = stackalloc int[6];
CalculateStats(creature, stats);
```

### String Optimization

**❌ BAD: String concatenation**
```csharp
var message = "Creature " + name + " dealt " + damage + " damage";
```

**✅ GOOD: String interpolation with caching**
```csharp
// For frequently used strings, cache them
private readonly Dictionary<string, string> _messageCache = new();

private string GetDamageMessage(string name, int damage)
{
    var key = $"{name}:{damage}";
    if (!_messageCache.TryGetValue(key, out var message))
    {
        message = $"Creature {name} dealt {damage} damage";
        _messageCache[key] = message;
    }
    return message;
}
```

**✅ BETTER: Use StringBuilder for complex strings**
```csharp
private readonly StringBuilder _sb = new(256);

private string BuildComplexMessage(params object[] parts)
{
    _sb.Clear();
    foreach (var part in parts)
    {
        _sb.Append(part);
    }
    return _sb.ToString();
}
```

### Object Pooling

For frequently created/destroyed objects:

```csharp
public class ObjectPool<T> where T : class, new()
{
    private readonly Stack<T> _pool = new();
    private readonly int _maxSize;

    public ObjectPool(int maxSize = 100)
    {
        _maxSize = maxSize;
    }

    public T Rent()
    {
        return _pool.Count > 0 ? _pool.Pop() : new T();
    }

    public void Return(T obj)
    {
        if (_pool.Count < _maxSize)
        {
            _pool.Push(obj);
        }
    }
}

// Usage
private readonly ObjectPool<List<Entity>> _listPool = new();

private void ProcessEntities()
{
    var list = _listPool.Rent();
    try
    {
        // Use list
        foreach (var entity in Api.Entities.Query<Health>())
        {
            list.Add(entity);
        }

        ProcessList(list);
    }
    finally
    {
        list.Clear();
        _listPool.Return(list);
    }
}
```

---

## CPU Optimization

### Minimize Loops

**❌ BAD: Nested loops (O(n²))**
```csharp
foreach (var attacker in attackers)
{
    foreach (var defender in defenders)
    {
        CalculateDamage(attacker, defender); // O(n²)
    }
}
```

**✅ GOOD: Single pass (O(n))**
```csharp
// Pre-calculate values
var attackerStats = attackers.Select(a => a.Get<CreatureStats>()).ToArray();
var defenderStats = defenders.Select(d => d.Get<CreatureStats>()).ToArray();

// Process in parallel if possible
Parallel.For(0, attackers.Count, i =>
{
    for (int j = 0; j < defenders.Count; j++)
    {
        CalculateDamage(attackerStats[i], defenderStats[j]);
    }
});
```

### Early Returns

**❌ BAD: Unnecessary processing**
```csharp
private void OnDamage(DamageEvent evt)
{
    var attacker = evt.Attacker;
    var defender = evt.Defender;

    // Process everything even if not needed
    var attackerStats = attacker.Get<CreatureStats>();
    var defenderStats = defender.Get<CreatureStats>();
    var typeEffectiveness = CalculateTypeEffectiveness();

    if (!defender.Has<StatusCondition>())
    {
        return; // Wasted work!
    }

    // ...
}
```

**✅ GOOD: Early exit**
```csharp
private void OnDamage(DamageEvent evt)
{
    // Check condition first
    if (!evt.Defender.Has<StatusCondition>())
    {
        return; // Exit early, no wasted work
    }

    // Only process if needed
    var attackerStats = evt.Attacker.Get<CreatureStats>();
    var defenderStats = evt.Defender.Get<CreatureStats>();
    var typeEffectiveness = CalculateTypeEffectiveness();

    // ...
}
```

### Avoid Redundant Calculations

**❌ BAD: Recalculates every time**
```csharp
private void OnTurn(TurnEvent evt)
{
    var effectiveness = CalculateTypeEffectiveness("fire", "water");
    // ... later in same method
    var effectiveness2 = CalculateTypeEffectiveness("fire", "water"); // Same!
}
```

**✅ GOOD: Calculate once**
```csharp
private void OnTurn(TurnEvent evt)
{
    var effectiveness = CalculateTypeEffectiveness("fire", "water");
    // Reuse the variable
    ProcessAttack(effectiveness);
    ProcessDefense(effectiveness);
}
```

### Lazy Initialization

**❌ BAD: Eager initialization**
```csharp
public class MyScript
{
    private readonly Dictionary<string, Data> _data = LoadAllData(); // Slow startup

    public MyScript(IScriptApi api)
    {
        _api = api;
    }
}
```

**✅ GOOD: Lazy initialization**
```csharp
public class MyScript
{
    private Dictionary<string, Data>? _data;

    private Dictionary<string, Data> Data =>
        _data ??= LoadAllData(); // Only load when needed

    public MyScript(IScriptApi api)
    {
        _api = api;
    }
}
```

---

## Entity Query Optimization

### Filter Early

**❌ BAD: Query all, filter later**
```csharp
var allCreatures = Api.Entities.Query<CreatureStats>();
var playerCreatures = allCreatures.Where(c => c.Has<PlayerControlled>());
```

**✅ GOOD: Filter in query**
```csharp
var playerCreatures = Api.Entities.Query<CreatureStats, PlayerControlled>();
```

### Query Only What You Need

**❌ BAD: Large queries**
```csharp
// Queries ALL creatures every frame
private void Update()
{
    var allCreatures = Api.Entities.Query<
        CreatureStats,
        Health,
        Moveset,
        StatusCondition,
        Ability>();

    // Only uses one creature
    var player = allCreatures.First(c => c.Has<PlayerControlled>());
}
```

**✅ GOOD: Targeted queries**
```csharp
private Entity? _playerCreature;

private void Update()
{
    // Cache player creature
    _playerCreature ??= Api.Entities
        .Query<PlayerControlled, Health>()
        .FirstOrDefault();
}
```

### Batch Component Access

**❌ BAD: Access components individually**
```csharp
foreach (var entity in entities)
{
    var health = entity.Get<Health>(); // Lookup
    var stats = entity.Get<CreatureStats>(); // Lookup
    var moveset = entity.Get<Moveset>(); // Lookup

    Process(health, stats, moveset);
}
```

**✅ GOOD: Batch component access**
```csharp
// The query already has these components
var query = Api.Entities.Query<Health, CreatureStats, Moveset>();

foreach (var entity in query)
{
    // Components are already retrieved by the query
    ref var health = ref entity.Get<Health>();
    ref var stats = ref entity.Get<CreatureStats>();
    ref var moveset = ref entity.Get<Moveset>();

    Process(ref health, ref stats, ref moveset);
}
```

### Cache Query Results

**❌ BAD: Query every frame**
```csharp
private void OnUpdate()
{
    var enemies = Api.Entities.Query<Enemy, Health>().ToList();
    // Process enemies
}
```

**✅ GOOD: Cache and invalidate**
```csharp
private List<Entity>? _cachedEnemies;
private DateTime _enemyCacheExpiry;

private List<Entity> GetEnemies()
{
    if (_cachedEnemies == null || DateTime.UtcNow > _enemyCacheExpiry)
    {
        _cachedEnemies = Api.Entities.Query<Enemy, Health>().ToList();
        _enemyCacheExpiry = DateTime.UtcNow.AddSeconds(1);
    }

    return _cachedEnemies;
}
```

---

## Event Handler Optimization

### Minimize Event Subscriptions

**❌ BAD: Subscribe to everything**
```csharp
public MyScript(IScriptApi api)
{
    api.Events.Subscribe<BattleStartEvent>(OnBattle);
    api.Events.Subscribe<BattleTurnEvent>(OnBattle);
    api.Events.Subscribe<MoveUsedEvent>(OnBattle);
    api.Events.Subscribe<DamageDealtEvent>(OnBattle);
    // Too many subscriptions!
}
```

**✅ GOOD: Subscribe only to what you need**
```csharp
public MyScript(IScriptApi api)
{
    // Only subscribe to the specific event you care about
    api.Events.Subscribe<BattleEndEvent>(OnBattleEnd);
}
```

### Fast Event Handlers

**❌ BAD: Heavy processing in event handler**
```csharp
private void OnMoveUsed(MoveUsedEvent evt)
{
    // Complex calculations in event handler
    var damage = CalculateComplexDamage(evt);
    var effectiveness = CalculateTypeChart();
    var criticalChance = CalculateCritical();

    // Database lookup
    var moveData = Api.Data.GetMove(evt.MoveId);

    // More processing...
}
```

**✅ GOOD: Defer heavy work**
```csharp
private readonly Queue<MoveUsedEvent> _pendingEvents = new();

private void OnMoveUsed(MoveUsedEvent evt)
{
    // Just queue the event
    _pendingEvents.Enqueue(evt);
}

private void ProcessPendingEvents()
{
    // Process in batches during a less critical time
    while (_pendingEvents.TryDequeue(out var evt))
    {
        ProcessMoveEvent(evt);
    }
}
```

### Conditional Event Handling

**❌ BAD: Process all events**
```csharp
private void OnDamage(DamageEvent evt)
{
    // Always processes, even when not relevant
    var attacker = evt.Attacker;
    var defender = evt.Defender;

    // ... lots of processing ...

    if (defender.Has<PlayerControlled>())
    {
        // Only care about player damage
    }
}
```

**✅ GOOD: Early filtering**
```csharp
private void OnDamage(DamageEvent evt)
{
    // Filter early
    if (!evt.Defender.Has<PlayerControlled>())
    {
        return; // Don't care about non-player damage
    }

    // Only process relevant events
    ProcessPlayerDamage(evt);
}
```

---

## Caching Strategies

### Data Caching

Cache frequently accessed data:

```csharp
public class DataCache
{
    private readonly Dictionary<string, CreatureDefinition> _creatures = new();
    private readonly Dictionary<string, MoveDefinition> _moves = new();

    public CreatureDefinition GetCreature(string id)
    {
        if (!_creatures.TryGetValue(id, out var def))
        {
            def = Api.Data.GetCreature(id);
            _creatures[id] = def;
        }
        return def;
    }

    public MoveDefinition GetMove(string id)
    {
        if (!_moves.TryGetValue(id, out var def))
        {
            def = Api.Data.GetMove(id);
            _moves[id] = def;
        }
        return def;
    }
}
```

### Computed Value Caching

Cache expensive calculations:

```csharp
public class StatsCalculator
{
    private readonly Dictionary<(Entity, int), int> _statCache = new();

    public int GetAttackStat(Entity creature)
    {
        var level = creature.Get<Level>().Current;
        var key = (creature, level);

        if (!_statCache.TryGetValue(key, out var attack))
        {
            attack = CalculateAttack(creature);
            _statCache[key] = attack;
        }

        return attack;
    }

    private int CalculateAttack(Entity creature)
    {
        // Expensive calculation
        var stats = creature.Get<CreatureStats>();
        var level = creature.Get<Level>().Current;

        return ((stats.BaseAttack + stats.IVAttack) * 2 + stats.EVAttack / 4) *
               level / 100 + 5;
    }
}
```

### Cache Invalidation

Invalidate caches when data changes:

```csharp
public class CacheManager
{
    private Dictionary<string, object> _cache = new();

    public void InvalidateOnLevelUp(LevelUpEvent evt)
    {
        // Stats changed, clear cache for this creature
        var creatureId = evt.Creature.Id;
        _cache.Remove($"stats:{creatureId}");
    }

    public void InvalidateAll()
    {
        _cache.Clear();
    }
}
```

### LRU Cache

Implement Least Recently Used cache for bounded memory:

```csharp
public class LRUCache<TKey, TValue>
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<(TKey, TValue)>> _cache;
    private readonly LinkedList<(TKey, TValue)> _lruList;

    public LRUCache(int capacity)
    {
        _capacity = capacity;
        _cache = new(capacity);
        _lruList = new();
    }

    public bool TryGet(TKey key, out TValue value)
    {
        if (_cache.TryGetValue(key, out var node))
        {
            // Move to front (most recently used)
            _lruList.Remove(node);
            _lruList.AddFirst(node);

            value = node.Value.Item2;
            return true;
        }

        value = default!;
        return false;
    }

    public void Add(TKey key, TValue value)
    {
        if (_cache.TryGetValue(key, out var existingNode))
        {
            _lruList.Remove(existingNode);
        }
        else if (_cache.Count >= _capacity)
        {
            // Remove least recently used
            var last = _lruList.Last!;
            _lruList.RemoveLast();
            _cache.Remove(last.Value.Item1);
        }

        var node = new LinkedListNode<(TKey, TValue)>((key, value));
        _lruList.AddFirst(node);
        _cache[key] = node;
    }
}
```

---

## Anti-Patterns

### ❌ LINQ in Hot Paths

**BAD:**
```csharp
private void OnUpdate()
{
    var enemies = Api.Entities.Query<Enemy>()
        .Where(e => e.Has<Health>())
        .OrderBy(e => e.Get<Health>().Current)
        .Select(e => e.Get<Health>())
        .ToList(); // Multiple allocations!
}
```

**GOOD:**
```csharp
private void OnUpdate()
{
    var enemies = Api.Entities.Query<Enemy, Health>();

    // Manual iteration, no allocations
    Entity? lowestHealthEnemy = null;
    int lowestHealth = int.MaxValue;

    foreach (var enemy in enemies)
    {
        var health = enemy.Get<Health>().Current;
        if (health < lowestHealth)
        {
            lowestHealth = health;
            lowestHealthEnemy = enemy;
        }
    }
}
```

### ❌ Exception-Driven Flow Control

**BAD:**
```csharp
try
{
    var creature = Api.Data.GetCreature(id);
}
catch (KeyNotFoundException)
{
    // Using exceptions for flow control is SLOW
    return null;
}
```

**GOOD:**
```csharp
if (Api.Data.TryGetCreature(id, out var creature))
{
    return creature;
}
return null;
```

### ❌ Excessive Logging

**BAD:**
```csharp
private void OnUpdate()
{
    Api.Logger.LogDebug("Update called"); // Every frame!

    foreach (var entity in entities)
    {
        Api.Logger.LogDebug($"Processing {entity.Id}"); // Hundreds per frame!
    }
}
```

**GOOD:**
```csharp
private int _frameCount = 0;

private void OnUpdate()
{
    _frameCount++;

    // Log every 60 frames (1 second at 60 FPS)
    if (_frameCount % 60 == 0)
    {
        Api.Logger.LogDebug($"Processed {entities.Count} entities");
    }
}
```

### ❌ Premature Optimization

**BAD:**
```csharp
// Over-optimized initialization that's called once
public MyScript(IScriptApi api)
{
    // Micro-optimization of one-time code
    var creatures = stackalloc int[6];
    unsafe
    {
        fixed (int* ptr = creatures)
        {
            // ... complex pointer arithmetic
        }
    }
}
```

**GOOD:**
```csharp
// Simple, readable code for one-time initialization
public MyScript(IScriptApi api)
{
    var creatures = new int[6]; // Fine for initialization
    // ... simple, clear code
}
```

---

## Performance Checklist

### Before Submitting Your Script

- [ ] Event handlers complete in <1ms
- [ ] No LINQ in hot paths (called >100 times/second)
- [ ] Queries filter early and request only needed components
- [ ] Frequently accessed data is cached
- [ ] No allocations in hot paths
- [ ] No exceptions used for flow control
- [ ] Logging is rate-limited
- [ ] Memory usage stays under 50 MB
- [ ] No infinite loops or deep recursion
- [ ] Profiled with realistic game scenarios

### Optimization Priority

1. **Profile first** - Measure before optimizing
2. **Optimize hot paths** - Focus on code called frequently
3. **Cache expensive operations** - Don't recalculate unnecessarily
4. **Reduce allocations** - Reuse objects when possible
5. **Early returns** - Exit fast when conditions not met
6. **Batch operations** - Process in groups when possible

---

## Tools and Resources

### Performance Monitoring Commands

```
/script-stats <script-id>     - View script performance statistics
/script-profile <script-id>   - Enable detailed profiling
/script-memory                - View memory usage by script
/script-benchmark             - Run performance benchmarks
```

### Recommended Reading

- [.NET Performance Tips](https://docs.microsoft.com/en-us/dotnet/framework/performance/)
- [Memory Management in .NET](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/)
- [Span<T> Performance](https://docs.microsoft.com/en-us/archive/msdn-magazine/2018/january/csharp-all-about-span-exploring-a-new-net-mainstay)

---

*Last Updated: 2025-10-22*
