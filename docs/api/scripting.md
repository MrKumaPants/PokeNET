# Scripting API Reference

## Introduction

PokeNET's scripting system uses **Roslyn** (Microsoft.CodeAnalysis.CSharp.Scripting) to execute C# scripts at runtime. Scripts provide a powerful way to add custom behavior without compiling DLLs.

## Security Model

### Sandboxing
Scripts run in a **restricted environment** with limited access to system resources.

**Allowed**:
- ✅ Access to ScriptApi interfaces
- ✅ Read game data
- ✅ Create/modify entities
- ✅ Subscribe to events
- ✅ Logging and debugging

**Prohibited**:
- ❌ Direct file system access
- ❌ Network operations
- ❌ Process creation
- ❌ Reflection on internal types
- ❌ Unsafe code
- ❌ Native interop

### Execution Boundaries

```csharp
// Scripts execute in isolated context
public class ScriptExecutionContext
{
    // Maximum execution time (prevents infinite loops)
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    // Memory limit for script execution
    public long MemoryLimit { get; set; } = 50 * 1024 * 1024; // 50 MB

    // Allowed namespaces
    public HashSet<string> AllowedNamespaces { get; set; } = new()
    {
        "System",
        "System.Linq",
        "System.Collections.Generic",
        "PokeNET.ModApi"
    };
}
```

## Script Structure

### Basic Script Format

```csharp
using PokeNET.ModApi;
using PokeNET.ModApi.Events;

/// <summary>
/// Script description
/// </summary>
public class MyScript
{
    private readonly IScriptApi _api;

    public MyScript(IScriptApi api)
    {
        _api = api;
        Initialize();
    }

    private void Initialize()
    {
        // Setup code
    }

    // Your methods here
}

// Entry point - return script instance
return new MyScript(Api);
```

### Script Entry Point

Every script must:
1. Accept `IScriptApi` named `Api` (provided globally)
2. Return an instance or result
3. Complete within timeout period

```csharp
// Simple script - return value
var result = Api.Data.GetCreature("pikachu");
return result;

// Complex script - return instance
public class MyAbility
{
    public void OnDamage(DamageEvent evt)
    {
        // ...
    }
}

return new MyAbility();
```

## IScriptApi Interface

The primary interface exposed to scripts:

```csharp
public interface IScriptApi
{
    /// <summary>
    /// Entity management API
    /// </summary>
    IEntityApi Entities { get; }

    /// <summary>
    /// Game data access
    /// </summary>
    IDataApi Data { get; }

    /// <summary>
    /// Event system
    /// </summary>
    IEventApi Events { get; }

    /// <summary>
    /// Asset loading
    /// </summary>
    IAssetApi Assets { get; }

    /// <summary>
    /// Audio playback
    /// </summary>
    IAudioApi Audio { get; }

    /// <summary>
    /// Logging facility
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Script context information
    /// </summary>
    IScriptContext Context { get; }

    /// <summary>
    /// Utility functions
    /// </summary>
    IScriptUtilities Utilities { get; }
}
```

## Common Scripting Patterns

### 1. Ability Effect Script

Create custom ability behaviors:

```csharp
using PokeNET.ModApi;
using PokeNET.ModApi.Events;

/// <summary>
/// Example: "Pressure" ability
/// Opponent's moves cost 1 extra PP
/// </summary>
public class PressureAbility
{
    private readonly IScriptApi _api;

    public PressureAbility(IScriptApi api)
    {
        _api = api;
        _api.Events.Subscribe<MoveUsedEvent>(OnMoveUsed);
        _api.Logger.LogInformation("Pressure ability script loaded");
    }

    private void OnMoveUsed(MoveUsedEvent evt)
    {
        // Check if opponent is using move
        if (IsOpponentMove(evt))
        {
            // Deduct extra PP
            ref var moveset = ref evt.User.Get<Moveset>();
            var move = moveset.Moves[evt.MoveIndex];

            if (move.CurrentPP > 0)
            {
                move.CurrentPP -= 1; // Extra PP cost
                _api.Logger.LogDebug($"Pressure: Extra PP deducted from {evt.MoveId}");
            }
        }
    }

    private bool IsOpponentMove(MoveUsedEvent evt)
    {
        // Check if move user is opponent
        return !evt.User.Has<PlayerControlled>();
    }
}

return new PressureAbility(Api);
```

### 2. Move Effect Script

Define custom move behaviors:

```csharp
using PokeNET.ModApi;
using PokeNET.ModApi.Battle;

/// <summary>
/// Custom move: "Soul Drain"
/// Deals damage and heals user for 50% of damage dealt
/// </summary>
public class SoulDrainEffect
{
    private readonly IScriptApi _api;

    public SoulDrainEffect(IScriptApi api)
    {
        _api = api;
    }

    public MoveResult Execute(MoveContext context)
    {
        var result = new MoveResult();

        // Calculate damage using standard formula
        var damage = CalculateDamage(context);

        // Apply damage to target
        ref var targetHealth = ref context.Target.Get<Health>();
        targetHealth.Current -= damage;

        result.DamageDealt = damage;

        // Heal user for 50% of damage
        var healAmount = damage / 2;
        ref var userHealth = ref context.User.Get<Health>();
        userHealth.Current = Math.Min(
            userHealth.Maximum,
            userHealth.Current + healAmount);

        result.HealingDone = healAmount;

        // Battle message
        _api.Events.Publish(new BattleMessageEvent
        {
            Message = $"{context.User.Name} drained health from {context.Target.Name}!",
            Priority = MessagePriority.MoveEffect
        });

        _api.Logger.LogInformation(
            $"Soul Drain: Dealt {damage} damage, healed {healAmount} HP");

        return result;
    }

    private int CalculateDamage(MoveContext context)
    {
        // Use standard damage calculation
        return Api.Utilities.CalculateMoveDamage(
            context.User,
            context.Target,
            context.Move);
    }
}

return new SoulDrainEffect(Api);
```

### 3. Event Listener Script

React to game events:

```csharp
using PokeNET.ModApi;
using PokeNET.ModApi.Events;

/// <summary>
/// Achievement tracker - monitors battle events
/// </summary>
public class AchievementTracker
{
    private readonly IScriptApi _api;
    private int _battleVictories = 0;
    private int _criticalHits = 0;

    public AchievementTracker(IScriptApi api)
    {
        _api = api;

        // Subscribe to multiple events
        _api.Events.Subscribe<BattleEndEvent>(OnBattleEnd);
        _api.Events.Subscribe<CriticalHitEvent>(OnCriticalHit);
        _api.Events.Subscribe<CreatureFaintedEvent>(OnCreatureFainted);
    }

    private void OnBattleEnd(BattleEndEvent evt)
    {
        if (evt.Winner == BattleWinner.Player)
        {
            _battleVictories++;
            _api.Logger.LogInformation($"Battle victories: {_battleVictories}");

            // Check for achievement
            if (_battleVictories == 10)
            {
                UnlockAchievement("first_ten_wins");
            }
        }
    }

    private void OnCriticalHit(CriticalHitEvent evt)
    {
        if (evt.Attacker.Has<PlayerControlled>())
        {
            _criticalHits++;

            if (_criticalHits == 50)
            {
                UnlockAchievement("critical_master");
            }
        }
    }

    private void OnCreatureFainted(CreatureFaintedEvent evt)
    {
        // Check for perfect victory (no damage taken)
        if (evt.Victim.Has<WildCreature>())
        {
            var playerCreature = GetPlayerCreature();
            var health = playerCreature.Get<Health>();

            if (health.Current == health.Maximum)
            {
                UnlockAchievement("flawless_victory");
            }
        }
    }

    private void UnlockAchievement(string achievementId)
    {
        _api.Logger.LogInformation($"Achievement unlocked: {achievementId}");

        _api.Events.Publish(new AchievementUnlockedEvent
        {
            AchievementId = achievementId,
            UnlockedAt = DateTime.UtcNow
        });
    }

    private Entity GetPlayerCreature()
    {
        var query = Api.Entities.Query<PlayerControlled, PartyMember>();
        return query.First();
    }
}

return new AchievementTracker(Api);
```

### 4. Procedural Content Script

Generate content dynamically:

```csharp
using PokeNET.ModApi;
using System.Linq;

/// <summary>
/// Procedural creature generator
/// </summary>
public class ProceduralCreatureGenerator
{
    private readonly IScriptApi _api;
    private readonly Random _random = new();

    public ProceduralCreatureGenerator(IScriptApi api)
    {
        _api = api;
    }

    public Entity GenerateRandomCreature(int level)
    {
        _api.Logger.LogInformation($"Generating random creature at level {level}");

        // Get random base creature
        var allCreatures = _api.Data.GetAllDefinitions<CreatureDefinition>();
        var baseDef = allCreatures.ElementAt(_random.Next(allCreatures.Count()));

        // Create entity
        var entity = _api.Entities.CreateEntity(baseDef.Id);

        // Randomize IVs
        ref var stats = ref entity.Get<CreatureStats>();
        stats.IVHP = (byte)_random.Next(32);
        stats.IVAttack = (byte)_random.Next(32);
        stats.IVDefense = (byte)_random.Next(32);
        stats.IVSpAttack = (byte)_random.Next(32);
        stats.IVSpDefense = (byte)_random.Next(32);
        stats.IVSpeed = (byte)_random.Next(32);

        // Set level and calculate stats
        SetLevel(entity, level);

        // Random nature
        ApplyRandomNature(entity);

        // Generate random moveset from level-up moves
        GenerateMoveset(entity, level);

        _api.Logger.LogDebug(
            $"Generated {baseDef.Name} (Lv.{level}) with IVs: " +
            $"HP={stats.IVHP}, Atk={stats.IVAttack}, Def={stats.IVDefense}");

        return entity;
    }

    private void SetLevel(Entity entity, int level)
    {
        // Level-up logic
        var levelComponent = new Level { Current = level };
        entity.Add(levelComponent);

        // Recalculate stats based on level
        Api.Utilities.RecalculateStats(entity);
    }

    private void ApplyRandomNature(Entity entity)
    {
        var natures = new[]
        {
            "Hardy", "Lonely", "Brave", "Adamant", "Naughty",
            "Bold", "Docile", "Relaxed", "Impish", "Lax",
            "Timid", "Hasty", "Serious", "Jolly", "Naive",
            "Modest", "Mild", "Quiet", "Bashful", "Rash",
            "Calm", "Gentle", "Sassy", "Careful", "Quirky"
        };

        var nature = natures[_random.Next(natures.Length)];
        entity.Add(new Nature { Name = nature });

        _api.Logger.LogDebug($"Applied nature: {nature}");
    }

    private void GenerateMoveset(Entity entity, int level)
    {
        // Get creature definition
        var def = GetCreatureDefinition(entity);

        // Get moves learnable at this level
        var learnableMoves = def.Learnset
            .Where(m => m.Level <= level)
            .OrderByDescending(m => m.Level)
            .Take(4)
            .ToList();

        // Set moveset
        var moveset = new Moveset { Count = learnableMoves.Count };

        for (int i = 0; i < learnableMoves.Count; i++)
        {
            var moveData = _api.Data.GetMove(learnableMoves[i].MoveId);
            moveset.Moves[i] = new Move
            {
                MoveId = moveData.Id,
                CurrentPP = moveData.PP,
                MaxPP = moveData.PP
            };
        }

        entity.Set(moveset);
    }

    private CreatureDefinition GetCreatureDefinition(Entity entity)
    {
        // Get definition from entity data
        var defId = entity.Get<DefinitionReference>().DefinitionId;
        return _api.Data.GetCreature(defId);
    }
}

return new ProceduralCreatureGenerator(Api);
```

## Script Utilities

### IScriptUtilities Interface

```csharp
public interface IScriptUtilities
{
    /// <summary>
    /// Calculate move damage
    /// </summary>
    int CalculateMoveDamage(Entity attacker, Entity defender, MoveDefinition move);

    /// <summary>
    /// Calculate type effectiveness
    /// </summary>
    float GetTypeEffectiveness(string attackType, CreatureType defenderTypes);

    /// <summary>
    /// Recalculate creature stats
    /// </summary>
    void RecalculateStats(Entity creature);

    /// <summary>
    /// Get random number
    /// </summary>
    int Random(int min, int max);

    /// <summary>
    /// Get random float
    /// </summary>
    float RandomFloat(float min = 0f, float max = 1f);

    /// <summary>
    /// Check if random chance succeeds
    /// </summary>
    bool RandomChance(float probability);

    /// <summary>
    /// Clamp value between min and max
    /// </summary>
    T Clamp<T>(T value, T min, T max) where T : IComparable<T>;

    /// <summary>
    /// Linear interpolation
    /// </summary>
    float Lerp(float a, float b, float t);
}
```

### Usage Examples

```csharp
// Random chance (30% probability)
if (Api.Utilities.RandomChance(0.3f))
{
    // 30% of the time
}

// Random damage variance (85% - 100%)
var damageMultiplier = Api.Utilities.RandomFloat(0.85f, 1.0f);
var finalDamage = baseDamage * damageMultiplier;

// Clamp health
var newHealth = Api.Utilities.Clamp(health.Current + healing, 0, health.Maximum);
```

## Debugging Scripts

### Logging

```csharp
// Different log levels
Api.Logger.LogTrace("Very detailed information");
Api.Logger.LogDebug("Debug information");
Api.Logger.LogInformation("General information");
Api.Logger.LogWarning("Warning - something unexpected");
Api.Logger.LogError("Error occurred");
Api.Logger.LogCritical("Critical error!");

// Structured logging
Api.Logger.LogInformation(
    "Creature {CreatureName} took {Damage} damage",
    creature.Name,
    damageAmount);
```

### Error Handling

```csharp
try
{
    var creature = Api.Data.GetCreature("unknown_id");
}
catch (KeyNotFoundException ex)
{
    Api.Logger.LogWarning($"Creature not found: {ex.Message}");
    // Fallback behavior
}
catch (Exception ex)
{
    Api.Logger.LogError(ex, "Unexpected error in script");
    throw; // Re-throw if critical
}
```

### Performance Monitoring

```csharp
// Time script execution
var stopwatch = Stopwatch.StartNew();

// ... your code ...

stopwatch.Stop();
Api.Logger.LogDebug($"Script execution time: {stopwatch.ElapsedMilliseconds}ms");
```

## Best Practices

### 1. Handle Exceptions Gracefully

```csharp
// ✅ GOOD: Handle errors
try
{
    var data = Api.Data.GetCreature(creatureId);
    ProcessCreature(data);
}
catch (KeyNotFoundException)
{
    Api.Logger.LogWarning($"Creature {creatureId} not found, using default");
    ProcessCreature(GetDefaultCreature());
}

// ❌ BAD: Let exceptions crash
var data = Api.Data.GetCreature(creatureId); // Might throw!
```

### 2. Avoid Infinite Loops

```csharp
// ✅ GOOD: Limited iterations
int maxIterations = 1000;
int count = 0;

while (condition && count < maxIterations)
{
    // Work
    count++;
}

// ❌ BAD: Potential infinite loop
while (condition) // Could run forever!
{
    // Work
}
```

### 3. Clean Up Resources

```csharp
// ✅ GOOD: Unsubscribe from events
public class MyScript
{
    private Action<BattleEvent> _handler;

    public MyScript(IScriptApi api)
    {
        _handler = OnBattle;
        api.Events.Subscribe(_handler);
    }

    public void Cleanup()
    {
        Api.Events.Unsubscribe(_handler);
    }
}
```

### 4. Cache Expensive Lookups

```csharp
// ✅ GOOD: Cache definitions
private readonly Dictionary<string, CreatureDefinition> _cache = new();

private CreatureDefinition GetCreature(string id)
{
    if (!_cache.TryGetValue(id, out var def))
    {
        def = Api.Data.GetCreature(id);
        _cache[id] = def;
    }
    return def;
}

// ❌ BAD: Lookup every time
private CreatureDefinition GetCreature(string id)
{
    return Api.Data.GetCreature(id); // Slow!
}
```

## Limitations

### Memory Limits
- Scripts have a 50 MB memory limit
- Exceeding limit terminates script

### Execution Time
- Default timeout: 5 seconds
- Can be configured per-script
- Exceeding timeout terminates script

### API Restrictions
- Cannot access internal game types
- Cannot use reflection on game assemblies
- Cannot execute unsafe code
- Cannot create threads directly

## Troubleshooting

### Common Issues

#### Script Fails to Load

**Symptom**: Script doesn't appear in loaded scripts list

**Solutions**:
1. Check `manifest.json` syntax is valid JSON
2. Verify script path in manifest matches actual file location
3. Check for compilation errors in script syntax
4. Review logs: `/logs/scripts/[script-name].log`

```csharp
// Check for common syntax errors
// ❌ Missing return statement
public class MyScript { }
// Script must return something!

// ✅ Correct
public class MyScript { }
return new MyScript(Api);
```

#### SecurityException During Execution

**Symptom**: Script throws `SecurityException`

**Cause**: Attempting to access blocked APIs

**Solutions**:
- Remove file I/O operations (`File`, `Directory`)
- Remove network operations (`HttpClient`, `Socket`)
- Remove reflection on game internals
- Use only allowed APIs from `IScriptApi`

```csharp
// ❌ BAD - Will throw SecurityException
File.ReadAllText("data.txt");

// ✅ GOOD - Use script API
var data = Api.Data.GetCreature("pikachu");
```

#### TimeoutException

**Symptom**: Script terminated with `TimeoutException`

**Cause**: Script execution exceeded 5-second timeout

**Solutions**:
1. Break long operations into smaller chunks
2. Remove infinite loops
3. Add iteration limits to while loops
4. Use early returns

```csharp
// ❌ BAD - Potential infinite loop
while (condition)
{
    // No exit condition!
}

// ✅ GOOD - Limited iterations
int iterations = 0;
while (condition && iterations < 1000)
{
    // Work
    iterations++;
}
```

#### OutOfMemoryException

**Symptom**: Script terminated due to excessive memory usage

**Cause**: Script exceeded 50 MB memory limit

**Solutions**:
1. Clear collections when done: `list.Clear()`
2. Avoid unbounded caching
3. Use object pooling for temporary objects
4. Dispose resources properly

```csharp
// ❌ BAD - Unbounded cache growth
private Dictionary<string, Data> _cache = new();

public void OnEvent(Event evt)
{
    _cache[evt.Id] = new Data(); // Grows forever!
}

// ✅ GOOD - Bounded cache with LRU
private LRUCache<string, Data> _cache = new(maxSize: 1000);
```

#### Components Not Found

**Symptom**: `KeyNotFoundException` when accessing components

**Cause**: Entity doesn't have the requested component

**Solutions**:
- Use `TryGet` instead of `Get`
- Use `Has` to check before accessing
- Verify query includes correct component types

```csharp
// ❌ BAD - Will throw if component missing
var health = entity.Get<Health>();

// ✅ GOOD - Safe access
if (entity.TryGet<Health>(out var health))
{
    // Use health
}

// ✅ ALSO GOOD - Check first
if (entity.Has<Health>())
{
    var health = entity.Get<Health>();
}
```

#### Events Not Firing

**Symptom**: Event handlers never called

**Cause**: Subscription issues or wrong event type

**Solutions**:
1. Verify event subscription in constructor
2. Check event type matches published events
3. Ensure handler method signature is correct
4. Check if script is enabled

```csharp
// ❌ BAD - Wrong event type
_api.Events.Subscribe<WrongEvent>(OnBattle);

// ✅ GOOD - Correct event type
_api.Events.Subscribe<BattleStartEvent>(OnBattle);

// ✅ Verify handler signature
private void OnBattle(BattleStartEvent evt) // Must match event type
{
    // Handler code
}
```

#### Performance Degradation

**Symptom**: Game runs slowly when script is enabled

**Cause**: Script is too slow or allocates too much

**Solutions**:
1. Profile script with `/script-profile [id]`
2. Review [Performance Guide](../performance/script-performance.md)
3. Cache expensive lookups
4. Avoid LINQ in hot paths
5. Reduce event handler complexity

```csharp
// Check execution time
var sw = Stopwatch.StartNew();
// Your code
sw.Stop();
if (sw.ElapsedMilliseconds > 1)
{
    Api.Logger.LogWarning("Slow handler: {Time}ms", sw.ElapsedMilliseconds);
}
```

### Debug Commands

```bash
# List all loaded scripts
/scripts list

# Reload specific script
/script reload <script-id>

# Reload all scripts
/reload-scripts

# View script logs
/script logs <script-id>

# View script performance stats
/script stats <script-id>

# Enable detailed profiling
/script profile <script-id>

# Test script in isolation
/script test <script-id>
```

### Logging Best Practices

Use appropriate log levels:

```csharp
// Trace: Very detailed, rarely needed
Api.Logger.LogTrace("Variable value: {Value}", value);

// Debug: Debugging information during development
Api.Logger.LogDebug("Processing entity {EntityId}", entity.Id);

// Information: General informational messages
Api.Logger.LogInformation("Script initialized with {Count} handlers", handlerCount);

// Warning: Something unexpected but handled
Api.Logger.LogWarning("Entity missing optional component {Component}", "StatusCondition");

// Error: Error occurred but script continues
Api.Logger.LogError(ex, "Failed to process event {EventType}", evt.GetType().Name);

// Critical: Critical error, script may be disabled
Api.Logger.LogCritical("Script failed initialization");
```

### Getting Help

1. **Documentation**: Review this documentation and guides
2. **Examples**: Check example scripts in `/Examples/Scripts/`
3. **Logs**: Check script logs in `/logs/scripts/`
4. **Community**: Ask in PokeNET Discord `#modding` channel
5. **Issues**: Report bugs on GitHub: https://github.com/you/pokenet/issues

---

## Quick Reference Card

### Essential API Methods

```csharp
// Entity Queries
Api.Entities.Query<Component1, Component2>()
Api.Entities.CreateEntity(definitionId)

// Component Access
entity.Get<Component>()
entity.TryGet<Component>(out var component)
entity.Has<Component>()
entity.Set(component)
entity.Add(component)
entity.Remove<Component>()

// Data Access
Api.Data.GetCreature(id)
Api.Data.GetMove(id)
Api.Data.GetAbility(id)
Api.Data.GetItem(id)

// Events
Api.Events.Subscribe<EventType>(handler)
Api.Events.Unsubscribe<EventType>(handler)
Api.Events.Publish(eventObject)

// Utilities
Api.Utilities.CalculateMoveDamage(attacker, defender, move)
Api.Utilities.GetTypeEffectiveness(attackType, defenderTypes)
Api.Utilities.Random(min, max)
Api.Utilities.RandomChance(probability)

// Logging
Api.Logger.LogInformation(message, ...args)
Api.Logger.LogWarning(message, ...args)
Api.Logger.LogError(exception, message, ...args)
```

### Performance Limits

| Resource | Limit |
|----------|-------|
| Execution Timeout | 5 seconds |
| Event Handler Timeout | 2 seconds |
| Memory Limit | 50 MB |
| API Calls/Second | 10,000 |
| Log Entries/Second | 100 |

---

## Next Steps

- **[Scripting Guide](../modding/scripting-guide.md)** - Comprehensive guide for mod developers
- **[Security Model](../security/script-security.md)** - Security and sandboxing details
- **[Performance Guide](../performance/script-performance.md)** - Optimization best practices
- [Complete Scripting Examples](../examples/scripting-examples.md) - Advanced examples
- [Event Reference](events.md) - Complete event documentation

---

*Last Updated: 2025-10-22*
