# PokeNET Performance Optimization Roadmap

**Analyst**: Performance Analysis Agent
**Date**: 2025-10-22
**Version**: 1.0
**Status**: Planning Phase

## Executive Summary

This roadmap provides a prioritized, phased approach to performance optimization for the PokeNET game framework. It consolidates recommendations from memory analysis, ECS profiling, and asset loading studies into a coherent implementation plan.

### Strategic Goals

1. **60 FPS Minimum**: Maintain 60 FPS with 50,000+ active entities
2. **Fast Loading**: < 3 second initial load, < 100ms asset loads
3. **Low Memory**: < 1 GB total memory usage on desktop
4. **Mod Support**: Zero-overhead mod asset system
5. **Scalability**: Support 100,000+ entities for simple systems

---

## Optimization Phases

### Phase 1: Project Foundation (Current)
**Timeline**: Completed
**Status**: ✅ Done

- [x] MonoGame project scaffolding
- [x] Basic game loop (Update/Draw)
- [x] Localization infrastructure
- [x] Platform detection (Desktop/Mobile)

**Performance Baseline**: N/A (minimal implementation)

---

### Phase 2: ECS Foundation & Core Systems
**Timeline**: Weeks 1-3
**Priority**: 🔴 CRITICAL

#### 2.1 Arch ECS Integration
**Week 1**

**Tasks**:
1. Add Arch NuGet package
2. Initialize ECS World in PokeNETGame
3. Implement basic component types (Position, Velocity, Sprite)
4. Create first systems (MovementSystem, RenderSystem)

**Code Template**:
```csharp
// Components.cs
public struct Position { public float X, Y, Z; }
public struct Velocity { public float X, Y, Z; }
public struct Sprite { public int TextureId; public Rectangle SourceRect; }

// MovementSystem.cs
public class MovementSystem
{
    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update(ref Position pos, in Velocity vel, float deltaTime)
    {
        pos.X += vel.X * deltaTime;
        pos.Y += vel.Y * deltaTime;
        pos.Z += vel.Z * deltaTime;
    }
}
```

**Performance Targets**:
- 100,000 entities (Pos + Vel): < 2ms update time
- Zero allocations per frame
- Memory: < 10 MB for ECS world

**Validation**:
```csharp
[Benchmark]
public void UpdateMovement_100K_Entities()
{
    world.Query(new QueryDescription().WithAll<Position, Velocity>(),
        (ref Position pos, in Velocity vel) =>
        {
            pos.X += vel.X * 0.016f;
            pos.Y += vel.Y * 0.016f;
            pos.Z += vel.Z * 0.016f;
        });
}
// Target: < 2ms
```

#### 2.2 Component Design Optimization
**Week 2**

**Tasks**:
1. Review all planned components
2. Ensure components are < 16 bytes where possible
3. Use value types (structs) for simple data
4. Design component hierarchy

**Component Size Guidelines**:
| Component | Size | Type | Justification |
|-----------|------|------|---------------|
| Position | 12 bytes | struct | 3 floats, hot data |
| Velocity | 12 bytes | struct | 3 floats, hot data |
| Health | 8 bytes | struct | 2 ints, frequently accessed |
| Stats | 28 bytes | struct | 7 ints, acceptable |
| Sprite | 8 bytes | struct | Texture ID + flags |
| Animation | 16 bytes | struct | Frame + timing |
| AI | 8 bytes | struct | State + target ID |

**Total per Full Entity**: ~92 bytes
**100K entities**: ~9.2 MB

#### 2.3 System Execution Order
**Week 2-3**

**Tasks**:
1. Define system dependencies
2. Implement system scheduler
3. Group systems by data access patterns

**System Schedule**:
```csharp
public class SystemScheduler
{
    public void Update(float deltaTime)
    {
        // Phase 1: Input & Logic
        InputSystem.Update();
        AISystem.Update(deltaTime);

        // Phase 2: Physics & Movement
        MovementSystem.Update(deltaTime);
        PhysicsSystem.Update(deltaTime);
        CollisionSystem.Update();

        // Phase 3: Animation & Effects
        AnimationSystem.Update(deltaTime);
        ParticleSystem.Update(deltaTime);

        // Phase 4: Rendering (deferred to Draw)
        // RenderSystem.Execute() called in Draw()
    }
}
```

**Performance Targets**:
- Total system time: < 14ms (60 FPS budget: 16.67ms)
- Memory allocations: 0 bytes/frame
- Cache miss rate: < 5%

**Deliverables**:
- ✅ Working ECS with 3-5 core systems
- ✅ Component design document
- ✅ System execution order diagram
- ✅ Benchmark suite for ECS operations

---

### Phase 3: Memory Management & Pooling
**Timeline**: Weeks 4-5
**Priority**: 🔴 CRITICAL

#### 3.1 Component Pooling
**Week 4**

**Tasks**:
1. Implement ComponentPool<T>
2. Integrate with entity creation
3. Profile allocation reduction

**Implementation**:
```csharp
public class ComponentPool<T> where T : struct
{
    private T[] pool;
    private int nextFree = 0;
    private const int CHUNK_SIZE = 1024;

    public ComponentPool(int initialCapacity = 4096)
    {
        pool = new T[initialCapacity];
    }

    public Span<T> Rent(int count)
    {
        if (nextFree + count > pool.Length)
            Array.Resize(ref pool, pool.Length * 2);

        var span = new Span<T>(pool, nextFree, count);
        nextFree += count;
        return span;
    }

    public void Return(int count)
    {
        nextFree -= count;
        if (nextFree < 0) nextFree = 0;
    }

    public void Clear() => nextFree = 0;
}
```

**Performance Targets**:
- 40-60% reduction in Gen 0 collections
- Entity creation: < 100ns (vs 300ns without pooling)

#### 3.2 Entity Pooling
**Week 4**

**Tasks**:
1. Implement entity pool for common archetypes
2. Pre-allocate 1000 entities per common type
3. Spawn/despawn using pool

**Implementation**:
```csharp
public class EntityPool
{
    private Dictionary<Archetype, Stack<Entity>> pools = new();
    private World world;

    public void Initialize()
    {
        // Pre-allocate common archetypes
        var commonArchetypes = new[]
        {
            typeof(Position, Velocity, Sprite),
            typeof(Position, Health, AI),
            typeof(Position, Sprite, Animation)
        };

        foreach (var archetype in commonArchetypes)
        {
            pools[archetype] = new Stack<Entity>(1000);

            for (int i = 0; i < 1000; i++)
            {
                var entity = CreateEntityForArchetype(archetype);
                world.Add<Inactive>(entity);
                pools[archetype].Push(entity);
            }
        }
    }

    public Entity Spawn(Archetype archetype)
    {
        if (pools.TryGetValue(archetype, out var pool) && pool.Count > 0)
        {
            var entity = pool.Pop();
            world.Remove<Inactive>(entity);
            return entity;
        }

        return CreateEntityForArchetype(archetype);
    }

    public void Despawn(Entity entity, Archetype archetype)
    {
        // Reset components
        ResetComponents(entity);
        world.Add<Inactive>(entity);

        if (pools.TryGetValue(archetype, out var pool))
        {
            pool.Push(entity);
        }
    }
}
```

**Performance Targets**:
- 3-6x faster entity spawn/despawn
- Zero archetype migrations for pooled entities

#### 3.3 Asset Caching
**Week 5**

**Tasks**:
1. Implement multi-tier asset cache
2. Add memory budget enforcement
3. Implement LRU eviction

**Implementation**:
```csharp
public class AssetCache
{
    // Hot cache: Always loaded
    private Dictionary<string, Texture2D> hotCache = new();

    // Warm cache: WeakReference, can be GC'd
    private Dictionary<string, WeakReference<Texture2D>> warmCache = new();

    // Memory budget: 512 MB default
    private long maxMemoryBytes = 512 * 1024 * 1024;
    private long currentMemoryBytes = 0;

    public void AddToHotCache(string path, Texture2D texture)
    {
        hotCache[path] = texture;
        currentMemoryBytes += EstimateTextureSize(texture);
    }

    public Texture2D Get(string path)
    {
        // Check hot cache
        if (hotCache.TryGetValue(path, out var texture))
            return texture;

        // Check warm cache
        if (warmCache.TryGetValue(path, out var weakRef) &&
            weakRef.TryGetTarget(out texture))
            return texture;

        // Load from disk
        texture = LoadTexture(path);
        warmCache[path] = new WeakReference<Texture2D>(texture);

        return texture;
    }

    private long EstimateTextureSize(Texture2D texture)
    {
        return texture.Width * texture.Height * 4; // RGBA
    }
}
```

**Performance Targets**:
- Cache hit rate: > 90%
- Cache miss penalty: < 10ms
- Memory savings: 30-40% vs loading everything

**Deliverables**:
- ✅ Component pooling system
- ✅ Entity pooling system
- ✅ Asset caching infrastructure
- ✅ Memory profiling report

---

### Phase 4: Asset Loading Optimization
**Timeline**: Weeks 6-7
**Priority**: 🟠 HIGH

#### 4.1 Async Asset Loading
**Week 6**

**Tasks**:
1. Implement AsyncAssetLoader
2. Create loading screen with progress
3. Support background loading

**Implementation**:
```csharp
public class AsyncAssetLoader
{
    private ConcurrentBag<Task<object>> loadingTasks = new();
    private int totalAssets = 0;
    private int loadedAssets = 0;

    public async Task<T> LoadAsync<T>(string path)
    {
        Interlocked.Increment(ref totalAssets);

        var task = Task.Run(async () =>
        {
            var asset = await LoadAssetFromDisk<T>(path);
            Interlocked.Increment(ref loadedAssets);
            return (object)asset;
        });

        loadingTasks.Add(task);
        return await task as T;
    }

    public float Progress => totalAssets > 0 ? (float)loadedAssets / totalAssets : 0;

    public async Task WaitForAll()
    {
        await Task.WhenAll(loadingTasks);
        loadingTasks.Clear();
    }
}
```

**Performance Targets**:
- 50-70% reduction in perceived load time
- Initial load: < 3 seconds
- Parallel loading: 4-8 concurrent loads

#### 4.2 Moddable Asset Manager
**Week 6-7**

**Tasks**:
1. Implement search path system
2. Support mod asset overrides
3. Detect and log asset conflicts

**Implementation**:
```csharp
public class ModdableAssetManager
{
    private List<string> searchPaths = new();
    private AssetCache cache = new();

    public void Initialize(List<ModInfo> mods)
    {
        searchPaths.Clear();

        // Reverse order: Last mod wins
        for (int i = mods.Count - 1; i >= 0; i--)
        {
            searchPaths.Add(mods[i].AssetPath);
        }

        // Base game (lowest priority)
        searchPaths.Add("Content");
    }

    public T Load<T>(string relativePath)
    {
        foreach (var basePath in searchPaths)
        {
            var fullPath = Path.Combine(basePath, relativePath);
            if (File.Exists(fullPath))
            {
                return cache.Get<T>(fullPath);
            }
        }

        throw new FileNotFoundException($"Asset not found: {relativePath}");
    }
}
```

**Performance Targets**:
- Asset override: Zero overhead
- Conflict detection: < 100ms at startup

**Deliverables**:
- ✅ Async asset loading system
- ✅ Moddable asset manager
- ✅ Loading screen UI
- ✅ Asset load time profiling

---

### Phase 5: Advanced ECS Optimization
**Timeline**: Weeks 8-10
**Priority**: 🟠 HIGH

#### 5.1 Parallel Query Execution
**Week 8**

**Tasks**:
1. Identify parallelizable systems
2. Use Arch's ParallelQuery
3. Profile speedup vs overhead

**Implementation**:
```csharp
// Before: Sequential
world.Query(in query, (ref Position pos, in Velocity vel) =>
{
    pos.X += vel.X * deltaTime;
    pos.Y += vel.Y * deltaTime;
    pos.Z += vel.Z * deltaTime;
});

// After: Parallel
world.ParallelQuery(in query, (ref Position pos, in Velocity vel) =>
{
    pos.X += vel.X * deltaTime;
    pos.Y += vel.Y * deltaTime;
    pos.Z += vel.Z * deltaTime;
});
```

**Performance Targets**:
- 2-4x speedup on quad-core
- 5-6x speedup on octa-core

#### 5.2 SIMD Optimization
**Week 9**

**Tasks**:
1. Identify SIMD-friendly operations
2. Optimize vector math with Vector<T>
3. Profile performance gains

**Implementation**:
```csharp
using System.Numerics;

public static void UpdatePositionsSIMD(Span<Position> positions, Span<Velocity> velocities, float deltaTime)
{
    for (int i = 0; i < positions.Length; i++)
    {
        // Compiler auto-SIMD optimization
        positions[i].X += velocities[i].X * deltaTime;
        positions[i].Y += velocities[i].Y * deltaTime;
        positions[i].Z += velocities[i].Z * deltaTime;
    }

    // Manual SIMD (if auto-SIMD insufficient):
    // Use Vector<float> for 4-8 simultaneous operations
}
```

**Performance Targets**:
- 2-4x speedup for vector math
- Maintain zero allocations

#### 5.3 Archetype Stability
**Week 10**

**Tasks**:
1. Pre-allocate common archetypes
2. Minimize add/remove component operations
3. Use flags instead of optional components

**Strategy**:
```csharp
// ❌ BAD: Frequent archetype changes
if (isPoisoned)
    world.Add<Poison>(entity);
else
    world.Remove<Poison>(entity);

// ✅ GOOD: Stable archetype with flags
public struct StatusEffects
{
    public bool IsPoisoned;
    public bool IsFrozen;
    public bool IsStunned;
}

statusEffects.IsPoisoned = isPoisoned; // No migration
```

**Performance Targets**:
- < 100 archetype migrations per frame
- Entity spawn: < 100ns

**Deliverables**:
- ✅ Parallel system execution
- ✅ SIMD-optimized hot paths
- ✅ Archetype stability guidelines
- ✅ ECS performance benchmarks

---

### Phase 6: Texture & Audio Optimization
**Timeline**: Weeks 11-12
**Priority**: 🟡 MEDIUM

#### 6.1 Texture Compression
**Week 11**

**Tasks**:
1. Implement DXT compression for Windows
2. Add ETC2 support for mobile (future)
3. Profile memory savings

**Performance Targets**:
- 4-8x smaller texture sizes
- Memory savings: 200-300 MB

#### 6.2 Texture Atlas System
**Week 11**

**Tasks**:
1. Create texture packing tool
2. Load atlases instead of individual sprites
3. Batch rendering with atlases

**Performance Targets**:
- 5-10x faster sprite loading
- Reduced draw calls

#### 6.3 Audio Streaming
**Week 12**

**Tasks**:
1. Stream music from disk (OGG)
2. Load SFX to memory (WAV)
3. Implement audio cache with size limit

**Performance Targets**:
- Music: Zero memory overhead (streamed)
- SFX cache: < 50 MB

**Deliverables**:
- ✅ Texture compression system
- ✅ Texture atlas support
- ✅ Audio streaming implementation

---

### Phase 7: Performance Monitoring & Profiling
**Timeline**: Weeks 13-14
**Priority**: 🟡 MEDIUM

#### 7.1 Custom Performance Tracking
**Week 13**

**Tasks**:
1. Implement system timing profiler
2. Add memory usage tracking
3. Create performance HUD (dev mode)

**Implementation**:
```csharp
public class PerformanceProfiler
{
    private Dictionary<string, List<double>> systemTimes = new();

    public void ProfileSystem(string name, Action system)
    {
        var sw = Stopwatch.StartNew();
        system();
        sw.Stop();

        if (!systemTimes.ContainsKey(name))
            systemTimes[name] = new List<double>();

        systemTimes[name].Add(sw.Elapsed.TotalMilliseconds);

        // Log if over budget
        if (sw.Elapsed.TotalMilliseconds > 5.0)
        {
            Logger.LogWarning($"{name} took {sw.Elapsed.TotalMilliseconds:F2}ms (over budget)");
        }
    }

    public void GenerateReport()
    {
        foreach (var kvp in systemTimes)
        {
            var avg = kvp.Value.Average();
            var max = kvp.Value.Max();
            Console.WriteLine($"{kvp.Key}: Avg={avg:F2}ms, Max={max:F2}ms");
        }
    }
}
```

#### 7.2 Benchmark Suite
**Week 14**

**Tasks**:
1. Create comprehensive benchmark suite
2. Track performance over time
3. Set up CI/CD performance regression tests

**Benchmarks**:
- Entity creation/destruction
- Query iteration (various sizes)
- System execution time
- Asset loading time
- Memory allocations

**Deliverables**:
- ✅ Performance profiling tools
- ✅ Benchmark suite
- ✅ Performance dashboard (dev mode)
- ✅ CI/CD integration

---

## Performance Targets Summary

### Overall Targets

| Metric | Phase 2 | Phase 5 | Phase 7 (Final) |
|--------|---------|---------|-----------------|
| Entity Count (60 FPS) | 10,000 | 50,000 | 100,000 |
| System Update Time | < 10ms | < 8ms | < 5ms |
| Initial Load Time | N/A | < 5s | < 3s |
| Memory Usage | < 100 MB | < 500 MB | < 1 GB |
| GC Collections/sec | < 20 | < 10 | < 5 |

### Subsystem Targets

**ECS Performance**:
- Query iteration (100K entities): < 2ms
- Entity creation: < 100ns
- Component access: < 1ns (cache hit)
- Archetype migration: < 500ns

**Asset Loading**:
- Texture load (sync): < 50ms
- Texture load (async): < 30ms
- Texture load (cached): < 0.01ms
- Audio stream startup: < 20ms

**Memory**:
- ECS World: < 100 MB (100K entities)
- Texture Cache: < 400 MB
- Audio Cache: < 50 MB
- System Overhead: < 100 MB

---

## Risk Assessment

### High-Risk Items

| Risk | Impact | Mitigation |
|------|--------|------------|
| GC pauses during gameplay | HIGH | Implement pooling, reduce allocations |
| Slow asset loading | MEDIUM | Async loading, caching, compression |
| ECS archetype churn | MEDIUM | Stable component design, pre-allocation |
| Memory leaks in mods | HIGH | WeakReferences, mod sandboxing |

### Performance Regression Prevention

1. **Continuous Benchmarking**: Run benchmarks on every commit
2. **Performance Budgets**: Enforce time/memory budgets per system
3. **Profiling Reviews**: Profile before merging large features
4. **Automated Testing**: CI/CD performance regression tests

---

## Implementation Checklist

### Phase 2: ECS Foundation
- [ ] Add Arch ECS package
- [ ] Define core components (< 16 bytes)
- [ ] Implement 3-5 core systems
- [ ] Set up system execution order
- [ ] Create ECS benchmark suite
- [ ] Profile ECS performance
- [ ] Document component design guidelines

### Phase 3: Memory Management
- [ ] Implement ComponentPool<T>
- [ ] Implement EntityPool
- [ ] Create AssetCache with LRU eviction
- [ ] Add memory budget enforcement
- [ ] Profile memory allocations
- [ ] Fix LocalizationManager allocations
- [ ] Document pooling patterns

### Phase 4: Asset Loading
- [ ] Implement AsyncAssetLoader
- [ ] Create loading screen UI
- [ ] Build ModdableAssetManager
- [ ] Add asset conflict detection
- [ ] Profile asset load times
- [ ] Implement priority loading queue
- [ ] Document asset system

### Phase 5: Advanced ECS
- [ ] Enable parallel query execution
- [ ] Optimize vector math with SIMD
- [ ] Pre-allocate common archetypes
- [ ] Minimize archetype migrations
- [ ] Profile parallel speedup
- [ ] Benchmark SIMD gains
- [ ] Document optimization techniques

### Phase 6: Texture & Audio
- [ ] Implement DXT texture compression
- [ ] Create texture atlas system
- [ ] Build texture packing tool
- [ ] Implement audio streaming
- [ ] Add audio cache with limits
- [ ] Profile memory savings
- [ ] Document asset formats

### Phase 7: Monitoring
- [ ] Create PerformanceProfiler
- [ ] Build performance HUD
- [ ] Implement memory tracking
- [ ] Create comprehensive benchmark suite
- [ ] Set up CI/CD performance tests
- [ ] Generate performance reports
- [ ] Document profiling tools

---

## Success Criteria

### Performance Metrics

**Must Have** (Launch Blockers):
- ✅ 60 FPS with 50,000 entities
- ✅ < 3 second initial load
- ✅ < 1 GB memory usage
- ✅ Zero allocations per frame (ECS)
- ✅ < 5ms total system update time

**Should Have** (Quality Goals):
- 🎯 60 FPS with 100,000 entities
- 🎯 < 2 second initial load
- 🎯 < 800 MB memory usage
- 🎯 > 90% asset cache hit rate
- 🎯 < 3ms total system update time

**Could Have** (Stretch Goals):
- 🌟 120 FPS support
- 🌟 1,000,000 entity support (simple systems)
- 🌟 < 1 second initial load
- 🌟 < 500 MB memory usage

---

## Conclusion

This roadmap provides a clear, phased approach to optimizing PokeNET's performance. By following this plan:

1. **Phase 2** establishes the ECS foundation
2. **Phase 3** eliminates memory allocations and GC pressure
3. **Phase 4** enables fast, mod-friendly asset loading
4. **Phase 5** maximizes ECS performance with parallelism and SIMD
5. **Phase 6** reduces asset memory footprint
6. **Phase 7** ensures performance is maintained long-term

**Expected Results** (after all phases):
- **10-50x faster** than naive implementation
- **60-80% reduction** in memory usage
- **2-4x speedup** from parallelism
- **< 3 second** initial load time
- **100,000+ entities** at 60 FPS

This roadmap should be treated as a living document, updated as new optimization opportunities are discovered and priorities shift based on project needs.

---

**Next Steps**:
1. Review roadmap with development team
2. Prioritize Phase 2 tasks for immediate implementation
3. Set up performance tracking infrastructure
4. Begin ECS foundation work
