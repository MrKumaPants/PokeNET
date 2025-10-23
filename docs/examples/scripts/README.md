# PokeNET Scripting Examples

This directory contains comprehensive example scripts demonstrating the PokeNET scripting API. These examples cover common use cases and best practices for creating custom game behavior through scripts.

## 📚 Table of Contents

- [Overview](#overview)
- [Script Formats](#script-formats)
- [Example Scripts](#example-scripts)
- [Usage Guide](#usage-guide)
- [Best Practices](#best-practices)
- [API Reference](#api-reference)
- [Troubleshooting](#troubleshooting)

---

## Overview

PokeNET's scripting system uses **Roslyn** (Microsoft.CodeAnalysis.CSharp.Scripting) to execute C# scripts at runtime. Scripts provide a powerful way to add custom behavior, create new content, and modify game mechanics without compiling DLLs.

### Key Features

- ✅ Full C# language support
- ✅ Access to game entities and components
- ✅ Event system integration
- ✅ Safe sandboxed execution
- ✅ Hot-reload support
- ✅ Comprehensive API surface

### Security Model

Scripts run in a **restricted sandbox** with:
- ✅ Access to ScriptApi interfaces
- ✅ Read/modify game data
- ✅ Create/modify entities
- ✅ Subscribe to events
- ❌ No file system access
- ❌ No network operations
- ❌ No unsafe code

---

## Script Formats

PokeNET supports two script formats:

### `.csx` Format (C# Script)

**Use for**: Simple scripts, quick prototypes, single-file solutions

```csharp
// Lightweight script format
using PokeNET.ModApi;

public class MyScript
{
    private readonly IScriptApi _api;

    public MyScript(IScriptApi api)
    {
        _api = api;
    }

    public void Execute()
    {
        // Script logic here
    }
}

return new MyScript(Api); // 'Api' is globally available
```

**Characteristics:**
- No namespace required
- Single file
- Global `Api` variable
- Quick to write and test
- Ideal for move effects, simple modifiers

### `.cs` Format (C# Class)

**Use for**: Complex scripts, multiple classes, production code

```csharp
// Full C# class format
using PokeNET.ModApi;
using System;
using System.Collections.Generic;

namespace MyMod.Scripts
{
    public class ComplexScript
    {
        private readonly IScriptApi _api;

        public ComplexScript(IScriptApi api)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            Initialize();
        }

        private void Initialize()
        {
            // Initialization logic
        }

        // Additional helper classes
        private class HelperClass { }
    }
}

return new MyMod.Scripts.ComplexScript(Api);
```

**Characteristics:**
- Full namespace support
- Multiple classes allowed
- Better IDE support
- Easier to maintain
- Ideal for complex systems, event handlers

---

## Example Scripts

### 1. Move Effect Example (`move-effect-example.csx`)

**Purpose**: Demonstrates creating custom move effects with damage calculation, status effects, and battle mechanics.

**Covers**:
- ✅ Battle context access
- ✅ Damage calculation
- ✅ Critical hit mechanics
- ✅ Status effect application
- ✅ Type effectiveness
- ✅ Event publishing
- ✅ Random mechanics

**Move**: "Thunder Strike"
- Electric-type attack
- 80 base power
- 95% accuracy
- 30% chance to paralyze
- +10% critical hit rate

**Usage**:
```csharp
// In move definition JSON:
{
  "id": "thunder_strike",
  "name": "Thunder Strike",
  "type": "Electric",
  "category": "Special",
  "power": 80,
  "accuracy": 95,
  "pp": 15,
  "scriptPath": "scripts/move-effect-example.csx"
}
```

**Key Learning Points**:
- How to access battle context
- Component manipulation (Health, Stats, Status)
- Using utility functions
- Publishing battle messages
- Proper logging practices

---

### 2. Entity Spawner Example (`entity-spawner-example.cs`)

**Purpose**: Demonstrates advanced entity creation with procedural generation, stat calculation, and customization.

**Covers**:
- ✅ Entity creation from definitions
- ✅ Procedural creature generation
- ✅ IV/EV calculation
- ✅ Moveset generation
- ✅ Nature and ability assignment
- ✅ Shiny determination
- ✅ Location-based encounters

**Features**:
- Wild creature spawning
- Custom creature configuration
- Party generation for trainers
- Encounter table support
- Weighted random selection

**Usage**:

```csharp
// Basic wild encounter
var spawner = new AdvancedEntitySpawner(Api);
var wildCreature = spawner.SpawnWildCreature("route_1", level: 5);

// Custom creature
var config = new CreatureConfig
{
    Level = 50,
    IVs = new StatValues { HP = 31, Attack = 31, Speed = 31 },
    Nature = "Jolly",
    Moves = new List<string> { "thunder_bolt", "quick_attack", "iron_tail" },
    IsShiny = true
};
var customCreature = spawner.SpawnCustomCreature("pikachu", config);

// Trainer party
var partyConfig = new List<PartyMemberConfig>
{
    new() { CreatureId = "charizard", Config = new CreatureConfig { Level = 50 } },
    new() { CreatureId = "blastoise", Config = new CreatureConfig { Level = 50 } },
    new() { CreatureId = "venusaur", Config = new CreatureConfig { Level = 50 } }
};
var party = spawner.SpawnParty(partyConfig);
```

**Key Learning Points**:
- Complex entity creation patterns
- Working with definitions and data
- Procedural content generation
- Error handling in scripts
- Configuration-driven design

---

### 3. Component Modifier Example (`component-modifier-example.csx`)

**Purpose**: Demonstrates dynamic stat modification based on conditions like weather, health, and equipment.

**Covers**:
- ✅ Component access and modification
- ✅ Event-driven modifications
- ✅ Weather system integration
- ✅ Conditional stat changes
- ✅ Temporary modifiers
- ✅ State preservation

**Features**:
- Weather-based stat boosts
- Low-health mechanics
- Equipment modifiers
- Ability-triggered changes
- Modifier stacking

**Usage**:

```csharp
// Apply pre-defined modifier
var modifier = new DynamicStatModifier(Api);
modifier.ApplyModifier(entity, "RainBoost", duration: 5);

// Custom modifier
var customMod = new StatModifierSet
{
    Attack = 2.0f,      // 2x Attack
    Defense = 1.5f,     // 1.5x Defense
    Speed = 0.5f,       // 0.5x Speed (slower)
    Description = "Berserk mode"
};
modifier.ApplyCustomModifier(entity, customMod);

// Reset all modifiers
modifier.ResetModifiers(entity);
```

**Modifier Presets**:
- `RainBoost`: 2x Speed for Water types
- `SunBoost`: 1.5x Attack/SpAttack for Fire types
- `LowHealthBoost`: 1.5x Attack when HP < 25%
- `DefensiveStance`: 1.5x Defense/SpDefense, 0.5x Speed

**Key Learning Points**:
- Safe component modification
- Preserving original state
- Event subscription patterns
- Automatic modifier application
- Cleanup and restoration

---

### 4. Event Handler Example (`event-handler-example.cs`)

**Purpose**: Demonstrates comprehensive event system usage with statistics tracking, achievements, and analytics.

**Covers**:
- ✅ Event subscription/unsubscription
- ✅ Multiple event types
- ✅ Event filtering
- ✅ State management
- ✅ Achievement system
- ✅ Statistics collection
- ✅ Custom event publishing

**Features**:
- Battle event tracking
- Move and damage statistics
- Achievement system
- Win/loss tracking
- Type matchup analysis
- Item usage tracking
- Player behavior analytics

**Usage**:

```csharp
// Create and initialize handler
var handler = new ComprehensiveEventHandler(Api);

// Get statistics
var stats = handler.GetStatistics();
Api.Logger.LogInformation($"Total Battles: {stats.TotalBattles}");
Api.Logger.LogInformation($"Win Rate: {stats.Victories / (float)stats.TotalBattles * 100:F1}%");

// Print full statistics
handler.PrintStatistics();

// Cleanup when done
handler.Cleanup();
```

**Tracked Events**:
- Battle: Start, End, Turn
- Move: Used, Missed, Critical, Super Effective
- Damage: Dealt, Healing
- Status: Inflicted, Cured
- Creature: Fainted, Caught, Evolved, Level Up
- Item: Used, Obtained
- Custom: Player Actions, Quests

**Achievements**:
- `first_ten_wins`: Win 10 battles
- `century_club`: Win 100 battles
- `battle_master`: Win 1000 battles
- `perfect_streak`: Win 10 battles without losing
- `flawless_victory`: Win without taking damage
- `critical_master`: Land 50 critical hits
- `type_master`: Use 50 different type matchups
- `heavy_hitter`: Deal 500+ damage in one hit
- `shiny_hunter`: Catch first shiny
- And many more...

**Key Learning Points**:
- Proper event subscription lifecycle
- Memory management with events
- State tracking across events
- Achievement system design
- Statistics aggregation
- Cleanup patterns

---

## Usage Guide

### 1. Installing Scripts

Place scripts in one of these locations:

```
GameDirectory/
├── Data/
│   └── Scripts/           # Base game scripts
│       ├── Moves/
│       ├── Abilities/
│       └── Events/
└── Mods/
    └── MyMod/
        └── Scripts/       # Mod scripts
            ├── custom-move.csx
            └── custom-system.cs
```

### 2. Referencing Scripts

**In Data Files** (JSON/XML):

```json
{
  "id": "custom_move",
  "name": "Custom Move",
  "scriptPath": "Scripts/Moves/custom-move.csx"
}
```

**Programmatically**:

```csharp
var scriptResult = await scriptingEngine.ExecuteScriptAsync(
    "Scripts/custom-script.csx",
    globals: new ScriptGlobals { Api = scriptApi }
);
```

### 3. Script Lifecycle

1. **Load**: Script file is loaded from disk
2. **Compile**: Roslyn compiles the script
3. **Execute**: Entry point is called with `Api` global
4. **Return**: Script returns instance/result
5. **Cleanup**: Script resources are disposed (if IDisposable)

### 4. Global Variables

Available in all scripts:

```csharp
IScriptApi Api;  // Main scripting API
```

### 5. Error Handling

Always wrap risky operations:

```csharp
try
{
    var creature = Api.Data.GetCreature("unknown");
}
catch (KeyNotFoundException ex)
{
    Api.Logger.LogWarning($"Creature not found: {ex.Message}");
    // Fallback logic
}
catch (Exception ex)
{
    Api.Logger.LogError(ex, "Unexpected error");
    throw; // Re-throw if critical
}
```

---

## Best Practices

### 1. Performance

✅ **DO**:
- Cache expensive lookups
- Use ref for component access
- Limit LINQ in hot paths
- Batch operations when possible

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
```

❌ **DON'T**:
- Lookup same data repeatedly
- Create unnecessary objects
- Use complex LINQ in Update loops

```csharp
// ❌ BAD: Repeated lookups
private CreatureDefinition GetCreature(string id)
{
    return Api.Data.GetCreature(id); // Slow every time!
}
```

### 2. Error Handling

✅ **DO**:
- Handle specific exceptions
- Provide fallback behavior
- Log errors with context
- Use try-finally for cleanup

```csharp
// ✅ GOOD: Specific error handling
try
{
    ProcessCreature(creatureId);
}
catch (KeyNotFoundException)
{
    Api.Logger.LogWarning($"Creature {creatureId} not found");
    ProcessCreature(GetDefaultCreature());
}
```

❌ **DON'T**:
- Catch and ignore exceptions
- Let exceptions crash scripts
- Use empty catch blocks

```csharp
// ❌ BAD: Ignoring errors
try
{
    ProcessCreature(creatureId);
}
catch { } // Silent failure!
```

### 3. Resource Management

✅ **DO**:
- Unsubscribe from events
- Implement cleanup methods
- Dispose resources properly
- Clear references

```csharp
// ✅ GOOD: Proper cleanup
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
        _handler = null;
    }
}
```

❌ **DON'T**:
- Leave event subscriptions active
- Leak memory through references
- Forget to cleanup

### 4. Logging

✅ **DO**:
- Use appropriate log levels
- Include context in messages
- Use structured logging
- Log performance metrics

```csharp
// ✅ GOOD: Structured logging
Api.Logger.LogInformation(
    "Creature {Name} took {Damage} damage from {Source}",
    creature.Name,
    damage,
    source);

// Performance logging
var stopwatch = Stopwatch.StartNew();
// ... work ...
stopwatch.Stop();
Api.Logger.LogDebug($"Operation took {stopwatch.ElapsedMilliseconds}ms");
```

❌ **DON'T**:
- Log everything at Information level
- Include sensitive data
- Log in tight loops
- Use string concatenation

### 5. Script Organization

✅ **DO**:
- One responsibility per script
- Use meaningful names
- Group related scripts
- Document script purpose

```
Scripts/
├── Moves/
│   ├── electric-moves.csx
│   ├── fire-moves.csx
│   └── water-moves.csx
├── Abilities/
│   ├── intimidate.csx
│   └── levitate.csx
└── Systems/
    ├── achievement-system.cs
    └── statistics-tracker.cs
```

---

## API Reference

### Core Interfaces

```csharp
IScriptApi
├── IEntityApi      // Entity creation and queries
├── IDataApi        // Game data access
├── IEventApi       // Event system
├── IAssetApi       // Asset loading
├── IAudioApi       // Audio playback
├── ILogger         // Logging
├── IScriptContext  // Script metadata
└── IScriptUtilities // Helper functions
```

### Common Patterns

**Entity Creation**:
```csharp
var entity = Api.Entities.CreateEntity("pikachu");
```

**Component Access**:
```csharp
ref var health = ref entity.Get<Health>();
health.Current -= damage;
```

**Data Lookup**:
```csharp
var moveData = Api.Data.GetMove("thunder_bolt");
```

**Event Subscription**:
```csharp
Api.Events.Subscribe<BattleStartEvent>(OnBattleStart);
```

**Event Publishing**:
```csharp
Api.Events.Publish(new CustomEvent { Data = "value" });
```

**Logging**:
```csharp
Api.Logger.LogInformation("Message with {Param}", value);
```

**Utilities**:
```csharp
var damage = Api.Utilities.CalculateMoveDamage(attacker, defender, move);
var hit = Api.Utilities.RandomChance(0.95f); // 95% chance
Api.Utilities.RecalculateStats(entity);
```

### Full API Documentation

See [Scripting API Reference](../../api/scripting.md) for complete documentation.

---

## Troubleshooting

### Script Won't Load

**Problem**: Script file not found

**Solution**:
- Check file path is correct
- Ensure file is in Data/Scripts or Mod/Scripts
- Verify file extension (.cs or .csx)
- Check file permissions

### Compilation Errors

**Problem**: Script fails to compile

**Solution**:
- Check syntax errors
- Verify all using statements
- Ensure return statement exists
- Check for missing semicolons
- Review error messages in logs

### Runtime Errors

**Problem**: Script crashes during execution

**Solution**:
- Add try-catch blocks
- Check for null references
- Validate component existence with `.Has<T>()`
- Review entity query results
- Check log files for stack traces

### Performance Issues

**Problem**: Script causes lag or slowdown

**Solution**:
- Profile with logging
- Cache expensive lookups
- Avoid LINQ in hot paths
- Batch operations
- Check for infinite loops
- Review event subscription count

### Memory Leaks

**Problem**: Memory usage grows over time

**Solution**:
- Unsubscribe from events
- Clear cached data periodically
- Implement cleanup methods
- Check for circular references
- Use weak references where appropriate

### Debugging Tips

1. **Enable Debug Logging**:
```csharp
Api.Logger.LogDebug("Variable value: {Value}", myVariable);
```

2. **Track Execution Flow**:
```csharp
Api.Logger.LogInformation("Entering Method()");
// ... code ...
Api.Logger.LogInformation("Exiting Method()");
```

3. **Validate Assumptions**:
```csharp
if (entity == null)
{
    Api.Logger.LogError("Entity is null!");
    return;
}
```

4. **Time Operations**:
```csharp
var sw = Stopwatch.StartNew();
// ... operation ...
sw.Stop();
Api.Logger.LogDebug($"Took {sw.ElapsedMilliseconds}ms");
```

---

## Additional Resources

- [Scripting API Documentation](../../api/scripting.md)
- [Component Reference](../../api/components.md)
- [Event System Guide](../../api/events.md)
- [Modding Guide](../../modding/getting-started.md)
- [Example Mod](../example-mod/README.md)

---

## Contributing

Have a great script example? Consider contributing!

1. Create a well-documented script
2. Add usage examples
3. Include error handling
4. Follow best practices
5. Submit a pull request

---

**Last Updated**: 2025-10-22
**API Version**: 1.0.0
**Engine Version**: PokeNET 1.0
