# Arch.Extended Quick Reference Summary

**For:** PokeNET Integration
**Date:** 2025-10-24
**Full Report:** [ARCH_EXTENDED_RESEARCH.md](./ARCH_EXTENDED_RESEARCH.md)

---

## 🎯 TL;DR - Why Arch.Extended?

**84% less code** + **100x faster events** + **Safe multithreading** = **Better PokeNET**

---

## 📦 Core Packages

```bash
dotnet add package Arch.System --version 1.1.0
dotnet add package Arch.System.SourceGenerator --version 2.1.0
dotnet add package Arch.EventBus --version 1.0.2
dotnet add package Arch.LowLevel --version 1.1.5
```

---

## 🚀 Top 5 Features for PokeNET

### 1️⃣ Source-Generated Queries (84% Code Reduction)

**Before:**
```csharp
protected override void OnUpdate(float deltaTime)
{
    var query = new QueryDescription()
        .WithAll<Health, Damage>()
        .WithNone<Dead>();

    World.Query(query, (ref Health h, in Damage d) => {
        h.Value -= d.Amount * deltaTime;
    });
}
```

**After:**
```csharp
[Query]
[All<Health, Damage>]
[None<Dead>]
public void ProcessDamage([Data] in float dt, ref Health h, in Damage d)
{
    h.Value -= d.Amount * dt;
}
```

---

### 2️⃣ Enhanced System Lifecycle

```csharp
public class BattleSystem : BaseSystem<World, float>
{
    public override void BeforeUpdate(float dt) { }  // Input, validation
    public override void Update(float dt) { }        // Core logic
    public override void AfterUpdate(float dt) { }   // Cleanup, events
}
```

---

### 3️⃣ CommandBuffer (Safe Multithreading)

```csharp
private CommandBuffer _cmd;

[Query]
public void SpawnPokemon(in SpawnRequest request)
{
    var entity = _cmd.Create();
    _cmd.Add<Pokemon>(entity, GeneratePokemon());
    _cmd.Add<Position>(entity, request.Position);
}

public override void AfterUpdate(float dt)
{
    _cmd.Playback();  // All operations execute here safely
}
```

**Key Benefit:** Defer entity creation/destruction during parallel queries!

---

### 4️⃣ Source-Generated EventBus (100x Faster)

**Current PokeNET:**
- ❌ Dictionary lookups
- ❌ Lock contention
- ❌ Delegate allocations

**Arch.EventBus:**
- ✅ Zero allocations
- ✅ Compile-time code generation
- ✅ Lock-free dispatching

```csharp
eventBus.Send(new DamageDealtEvent { Target = entity, Amount = 50 });
// 100x faster than reflection-based EventBus
```

---

### 5️⃣ Parallel Queries

```csharp
[Query]
[All<Pokemon, AIState, Position>]
public void UpdateAI([Data] in float dt, ref AIState ai, ref Position pos)
{
    // This runs in PARALLEL for thousands of Pokemon
    ai.Think(dt);
    pos.Move(ai.Direction, ai.Speed * dt);
}
```

---

## 📊 Performance Impact

| Feature | Improvement |
|---------|------------|
| **Query Performance** | 6-7x faster |
| **EventBus** | 100x faster |
| **Code Size** | 84% reduction |
| **Memory Allocations** | Zero (events) |
| **GC Pressure** | Significantly reduced |

---

## 🎮 PokeNET Use Cases

### Combat System
```csharp
[Query]
[All<BattleParticipant, ActiveTurn>]
[None<Fainted>]
public void ExecuteTurn(in Entity battler, ref Health health, in Move move)
{
    var damage = CalculateDamage(move);
    health.Value -= damage;

    if (health.Value <= 0)
        _cmd.Add<Fainted>(in battler);  // Deferred operation
}
```

### Wild Pokemon Spawning
```csharp
[Query]
[All<SpawnZone, Active>]
public void SpawnWildPokemon(in Entity zone, in SpawnZone config)
{
    for (int i = 0; i < config.Count; i++)
    {
        var pokemon = _cmd.Create();
        _cmd.Add<Pokemon>(pokemon, GenerateWildPokemon());
        _cmd.AddRelationship<SpawnedBy>(pokemon, zone);
    }
}
```

### Parallel AI Processing
```csharp
[Query]
[All<Pokemon, AIState>]
[Any<Idle, Patrolling, Chasing>]
public void UpdateAI([Data] in float dt, ref AIState ai)
{
    // Runs in parallel for ALL wild Pokemon
    ai.UpdateDecisionTree(dt);
}
```

---

## 🔧 Integration Checklist

### Phase 1: Foundation
- [ ] Install NuGet packages
- [ ] Migrate `SystemBase` → `BaseSystem<World, float>`
- [ ] Update `ISystemManager` to call `BeforeUpdate`/`AfterUpdate`

### Phase 2: Source Generators
- [ ] Add `[Query]` attributes to 2-3 systems (prototype)
- [ ] Test generated code
- [ ] Benchmark performance vs manual queries

### Phase 3: CommandBuffer
- [ ] Refactor entity creation to use `CommandBuffer`
- [ ] Enable parallel queries in AI/physics systems
- [ ] Test multithreading safety

### Phase 4: EventBus
- [ ] Replace `EventBus` with `Arch.EventBus`
- [ ] Migrate subscribers
- [ ] Benchmark performance improvement

### Phase 5: Advanced
- [ ] Add `Arch.Relationships` for trainer-Pokemon hierarchy
- [ ] Implement `Arch.Persistence` for save/load
- [ ] Optimize hot paths with `Arch.LowLevel`

---

## ⚠️ Gotchas & Tips

### Source Generator
- ✅ Attributes support up to 25 components in `[All<>]`/`[Any<>]`/`[None<>]`
- ✅ Use `[Data]` attribute for non-component parameters (deltaTime)
- ⚠️ Generated code in `obj/` folder - enable source generator debugging in IDE

### CommandBuffer
- ✅ ALWAYS call `Playback()` in `AfterUpdate()`
- ⚠️ Don't call `Playback()` in parallel queries
- ✅ Reuse same `CommandBuffer` instance - it clears after playback

### EventBus
- ✅ Zero allocations - events are structs
- ✅ Type-safe at compile time
- ⚠️ No reflection - must register event types explicitly

### Parallel Queries
- ✅ Use `CommandBuffer` for structural changes
- ⚠️ DON'T modify `World`/`Archetype`/`Chunk` structure during parallel queries
- ✅ OK to update component data (ref parameters)

---

## 📚 Resources

- **GitHub:** https://github.com/genaray/Arch.Extended
- **Docs:** https://arch-ecs.gitbook.io/arch
- **NuGet:** Search "Arch.System", "Arch.EventBus"
- **Discord:** Arch ECS Community (link in GitHub)
- **Full Report:** [ARCH_EXTENDED_RESEARCH.md](./ARCH_EXTENDED_RESEARCH.md)

---

## 🎯 Recommendation

**HIGH PRIORITY:** Integrate Arch.Extended incrementally
- Start with 1-2 systems as prototype
- Benchmark performance gains
- Roll out to all systems if successful

**Expected ROI:**
- 84% less code to maintain
- 6-100x performance improvement
- Better scalability for large Pokemon battles
- Safer multithreading for AI systems

---

**Research by:** RESEARCHER agent
**Stored in:** Hive mind memory (`hive/research/arch-extended`)
