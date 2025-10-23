# Scripting Guide for Mod Developers

## Introduction

Welcome to the PokeNET scripting system! This guide will help you create powerful mods using C# scripts that execute at runtime using the Roslyn scripting engine.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Your First Script](#your-first-script)
3. [Script Structure](#script-structure)
4. [Working with Entities](#working-with-entities)
5. [Event-Driven Programming](#event-driven-programming)
6. [Common Patterns](#common-patterns)
7. [Testing and Debugging](#testing-and-debugging)
8. [Deployment](#deployment)

---

## Getting Started

### Prerequisites

- PokeNET installed and running
- Basic knowledge of C# programming
- Text editor (VS Code, Visual Studio, or any text editor)
- Understanding of Pokémon game mechanics

### Script Location

Place your scripts in:
```
Mods/
  YourModName/
    Scripts/
      YourScript.csx
    manifest.json
```

### Mod Manifest

Create a `manifest.json` file:

```json
{
  "id": "your-mod-id",
  "name": "Your Mod Name",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "Brief description of your mod",
  "scripts": [
    "Scripts/YourScript.csx"
  ],
  "dependencies": []
}
```

---

## Your First Script

Let's create a simple script that logs when a battle starts:

**File: `Mods/MyFirstMod/Scripts/BattleLogger.csx`**

```csharp
using PokeNET.ModApi;
using PokeNET.ModApi.Events;

/// <summary>
/// Logs battle events to the console
/// </summary>
public class BattleLogger
{
    private readonly IScriptApi _api;

    public BattleLogger(IScriptApi api)
    {
        _api = api;

        // Subscribe to battle start event
        _api.Events.Subscribe<BattleStartEvent>(OnBattleStart);

        _api.Logger.LogInformation("BattleLogger script loaded!");
    }

    private void OnBattleStart(BattleStartEvent evt)
    {
        _api.Logger.LogInformation($"Battle started: {evt.PlayerTeamSize} vs {evt.OpponentTeamSize}");

        // Log player's team
        _api.Logger.LogInformation("Player team:");
        foreach (var creature in evt.PlayerTeam)
        {
            var name = creature.Get<CreatureName>().Value;
            var level = creature.Get<Level>().Current;
            _api.Logger.LogInformation($"  - {name} (Lv. {level})");
        }
    }
}

// Entry point - return script instance
return new BattleLogger(Api);
```

### Running Your Script

1. Place the script in `Mods/MyFirstMod/Scripts/BattleLogger.csx`
2. Create the manifest.json
3. Launch PokeNET
4. Start a battle to see logs in the console

---

## Script Structure

### Anatomy of a Script

Every script follows this structure:

```csharp
// 1. Using directives
using PokeNET.ModApi;
using PokeNET.ModApi.Events;
using System.Linq;

// 2. XML documentation (optional but recommended)
/// <summary>
/// Script description
/// </summary>

// 3. Script class
public class MyScript
{
    private readonly IScriptApi _api;

    // 4. Constructor - receives IScriptApi
    public MyScript(IScriptApi api)
    {
        _api = api;
        Initialize();
    }

    private void Initialize()
    {
        // Setup code: subscribe to events, load data, etc.
    }

    // 5. Your methods
    private void OnSomeEvent(SomeEvent evt)
    {
        // Event handler logic
    }
}

// 6. Entry point - return instance
return new MyScript(Api);
```

### Available Namespaces

Scripts have access to:

- `System.*` - Core .NET types
- `System.Linq` - LINQ queries
- `System.Collections.Generic` - Collections
- `PokeNET.ModApi` - Script API
- `PokeNET.ModApi.Events` - Event types
- `PokeNET.ModApi.Battle` - Battle system
- `PokeNET.ModApi.Data` - Game data

**Note**: File I/O, networking, and reflection are restricted for security.

---

## Working with Entities

PokeNET uses an Entity Component System (ECS). Entities are containers for components.

### Querying Entities

```csharp
// Get all player-controlled creatures
var playerCreatures = Api.Entities.Query<PlayerControlled, CreatureStats>();

foreach (var entity in playerCreatures)
{
    var name = entity.Get<CreatureName>().Value;
    var stats = entity.Get<CreatureStats>();

    Api.Logger.LogInformation($"{name} - HP: {stats.HP}, Atk: {stats.Attack}");
}
```

### Reading Components

```csharp
// Get a single component (throws if not present)
var health = creature.Get<Health>();
Api.Logger.LogInformation($"HP: {health.Current}/{health.Maximum}");

// Try get (safer)
if (creature.TryGet<StatusCondition>(out var status))
{
    Api.Logger.LogInformation($"Status: {status.Type}");
}

// Check if component exists
if (creature.Has<PlayerControlled>())
{
    Api.Logger.LogInformation("This is a player's creature");
}
```

### Modifying Components

```csharp
// Modify by reference
ref var health = ref creature.Get<Health>();
health.Current -= 50; // Deal 50 damage

// Set entire component
creature.Set(new StatusCondition
{
    Type = StatusType.Paralyzed,
    TurnsRemaining = -1 // Permanent
});

// Add new component
creature.Add(new Marker { Tag = "Custom" });

// Remove component
creature.Remove<StatusCondition>();
```

### Creating Entities

```csharp
// Create from definition
var newCreature = Api.Entities.CreateEntity("pikachu");

// Set level
newCreature.Add(new Level { Current = 50 });

// Calculate stats based on level
Api.Utilities.RecalculateStats(newCreature);

// Add to player's party
newCreature.Add(new PartyMember { Slot = 0 });
newCreature.Add(new PlayerControlled());
```

---

## Event-Driven Programming

Events are the primary way scripts interact with the game.

### Subscribing to Events

```csharp
public MyScript(IScriptApi api)
{
    _api = api;

    // Subscribe to events
    _api.Events.Subscribe<BattleStartEvent>(OnBattleStart);
    _api.Events.Subscribe<MoveUsedEvent>(OnMoveUsed);
    _api.Events.Subscribe<DamageDealtEvent>(OnDamageDealt);
    _api.Events.Subscribe<CreatureFaintedEvent>(OnCreatureFainted);
}
```

### Event Types

#### Battle Events

```csharp
// Battle lifecycle
BattleStartEvent      // Battle begins
BattleTurnStartEvent  // New turn starts
BattleEndEvent        // Battle ends

// Move events
MoveUsedEvent         // Move is used
MoveEffectEvent       // Move effect applies
MoveMissedEvent       // Move misses

// Damage events
DamageDealtEvent      // Damage dealt
HealingEvent          // HP restored
StatusInflictedEvent  // Status condition applied

// Creature events
CreatureFaintedEvent  // Creature faints
CreatureSwitchedEvent // Creature switches
ExperienceGainedEvent // EXP awarded
LevelUpEvent          // Level increased
```

#### World Events

```csharp
// Exploration
ItemPickedUpEvent     // Item collected
AreaEnteredEvent      // New area entered
NPCInteractionEvent   // NPC talked to
WildEncounterEvent    // Wild creature encountered

// Progress
BadgeEarnedEvent      // Gym badge earned
QuestCompletedEvent   // Quest finished
AchievementEvent      // Achievement unlocked
```

### Publishing Events

```csharp
// Publish custom event
_api.Events.Publish(new CustomEvent
{
    Message = "Something happened!",
    Timestamp = DateTime.UtcNow
});
```

### Unsubscribing

```csharp
private Action<BattleStartEvent> _battleHandler;

public MyScript(IScriptApi api)
{
    _battleHandler = OnBattleStart;
    api.Events.Subscribe(_battleHandler);
}

public void Cleanup()
{
    // Unsubscribe when done
    _api.Events.Unsubscribe(_battleHandler);
}
```

---

## Common Patterns

### Pattern 1: Custom Ability

Create a new ability with unique behavior:

```csharp
/// <summary>
/// Regenerator Ability: Heals 33% HP when switching out
/// </summary>
public class RegeneratorAbility
{
    private readonly IScriptApi _api;

    public RegeneratorAbility(IScriptApi api)
    {
        _api = api;
        _api.Events.Subscribe<CreatureSwitchedEvent>(OnSwitch);
    }

    private void OnSwitch(CreatureSwitchedEvent evt)
    {
        // Only trigger when switching OUT
        if (evt.SwitchDirection != SwitchDirection.Out)
            return;

        // Check if creature has Regenerator ability
        if (!HasRegeneratorAbility(evt.Creature))
            return;

        // Heal 33% of max HP
        ref var health = ref evt.Creature.Get<Health>();
        int healAmount = health.Maximum / 3;

        health.Current = Math.Min(health.Maximum, health.Current + healAmount);

        _api.Logger.LogInformation(
            $"{evt.Creature.Get<CreatureName>().Value} restored HP with Regenerator!");
    }

    private bool HasRegeneratorAbility(Entity creature)
    {
        if (creature.TryGet<Ability>(out var ability))
        {
            return ability.Id == "regenerator";
        }
        return false;
    }
}

return new RegeneratorAbility(Api);
```

### Pattern 2: Custom Move

Define a new move with special effects:

```csharp
/// <summary>
/// Volt Tackle: High power, recoil damage, 10% paralyze chance
/// </summary>
public class VoltTackleMove
{
    private readonly IScriptApi _api;

    public VoltTackleMove(IScriptApi api)
    {
        _api = api;
        _api.Events.Subscribe<MoveUsedEvent>(OnMoveUsed);
    }

    private void OnMoveUsed(MoveUsedEvent evt)
    {
        // Only trigger for Volt Tackle
        if (evt.MoveId != "volt-tackle")
            return;

        var attacker = evt.User;
        var defender = evt.Target;

        // Calculate damage (120 base power)
        var damage = Api.Utilities.CalculateMoveDamage(
            attacker, defender,
            Api.Data.GetMove("volt-tackle"));

        // Apply damage
        ref var defenderHealth = ref defender.Get<Health>();
        defenderHealth.Current -= damage;

        // Recoil damage (33% of damage dealt)
        int recoilDamage = damage / 3;
        ref var attackerHealth = ref attacker.Get<Health>();
        attackerHealth.Current -= recoilDamage;

        _api.Logger.LogInformation($"Recoil: {recoilDamage} HP");

        // 10% chance to paralyze
        if (Api.Utilities.RandomChance(0.1f))
        {
            defender.Set(new StatusCondition
            {
                Type = StatusType.Paralyzed,
                TurnsRemaining = -1
            });

            _api.Logger.LogInformation(
                $"{defender.Get<CreatureName>().Value} was paralyzed!");
        }
    }
}

return new VoltTackleMove(Api);
```

### Pattern 3: Item Effect

Create custom item behaviors:

```csharp
/// <summary>
/// Lucky Egg: 1.5x EXP gain when held
/// </summary>
public class LuckyEggItem
{
    private readonly IScriptApi _api;

    public LuckyEggItem(IScriptApi api)
    {
        _api = api;
        _api.Events.Subscribe<ExperienceGainedEvent>(OnExpGain);
    }

    private void OnExpGain(ExperienceGainedEvent evt)
    {
        // Check if creature is holding Lucky Egg
        if (!evt.Creature.TryGet<HeldItem>(out var held))
            return;

        if (held.ItemId != "lucky-egg")
            return;

        // Boost EXP by 50%
        int bonusExp = evt.ExperienceAmount / 2;
        evt.ExperienceAmount += bonusExp;

        _api.Logger.LogInformation($"Lucky Egg bonus: +{bonusExp} EXP");
    }
}

return new LuckyEggItem(Api);
```

### Pattern 4: Battle Modifier

Modify battle mechanics:

```csharp
/// <summary>
/// Hardcore Mode: All battles are Set mode (no switch preview)
/// </summary>
public class HardcoreMode
{
    private readonly IScriptApi _api;

    public HardcoreMode(IScriptApi api)
    {
        _api = api;
        _api.Events.Subscribe<BattleStartEvent>(OnBattleStart);
    }

    private void OnBattleStart(BattleStartEvent evt)
    {
        // Force Set mode
        evt.BattleSettings.SwitchMode = BattleSwitchMode.Set;

        // Increase opponent levels by 5
        foreach (var opponent in evt.OpponentTeam)
        {
            ref var level = ref opponent.Get<Level>();
            level.Current += 5;

            // Recalculate stats for new level
            Api.Utilities.RecalculateStats(opponent);
        }

        _api.Logger.LogInformation("Hardcore Mode: Battle difficulty increased!");
    }
}

return new HardcoreMode(Api);
```

### Pattern 5: Data Modification

Modify game data at runtime:

```csharp
/// <summary>
/// Type Chart Modifier: Makes Dragon type weak to Fairy
/// </summary>
public class TypeChartModifier
{
    private readonly IScriptApi _api;

    public TypeChartModifier(IScriptApi api)
    {
        _api = api;

        // Modify type effectiveness
        ModifyTypeChart();
    }

    private void ModifyTypeChart()
    {
        // Get type chart
        var typeChart = Api.Data.GetTypeChart();

        // Make Dragon weak to Fairy (2x damage)
        typeChart.SetEffectiveness("fairy", "dragon", 2.0f);

        _api.Logger.LogInformation("Type chart modified: Dragon now weak to Fairy");
    }
}

return new TypeChartModifier(Api);
```

---

## Testing and Debugging

### Logging

Use structured logging for debugging:

```csharp
// Different log levels
Api.Logger.LogTrace("Very detailed debug info");
Api.Logger.LogDebug("Debug information");
Api.Logger.LogInformation("General information");
Api.Logger.LogWarning("Warning message");
Api.Logger.LogError("Error occurred");

// Structured logging with parameters
Api.Logger.LogInformation(
    "Creature {Name} dealt {Damage} damage to {Target}",
    attacker.Name, damageAmount, defender.Name);
```

### Error Handling

Always handle potential errors:

```csharp
try
{
    var creature = Api.Data.GetCreature("custom-creature");
    ProcessCreature(creature);
}
catch (KeyNotFoundException ex)
{
    Api.Logger.LogWarning($"Creature not found: {ex.Message}");
    // Fallback behavior
    UseDefaultCreature();
}
catch (Exception ex)
{
    Api.Logger.LogError(ex, "Unexpected error in script");
    // Decide: continue or rethrow
}
```

### Performance Testing

Monitor script performance:

```csharp
var stopwatch = Stopwatch.StartNew();

// ... your code ...

stopwatch.Stop();

if (stopwatch.ElapsedMilliseconds > 100)
{
    Api.Logger.LogWarning(
        $"Script operation took {stopwatch.ElapsedMilliseconds}ms");
}
```

### Hot Reload

Scripts support hot reload during development:

1. Modify your script file
2. Run console command: `/reload-scripts`
3. Script reloads without restarting the game

---

## Deployment

### Packaging Your Mod

Create a distributable package:

```
YourMod/
  manifest.json
  README.md
  LICENSE.txt
  Scripts/
    ability.csx
    move.csx
  Data/
    creatures.json
    moves.json
  Assets/
    sprites/
```

### Manifest Best Practices

```json
{
  "id": "unique-mod-id",
  "name": "Descriptive Mod Name",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "What your mod does",
  "license": "MIT",
  "homepage": "https://github.com/you/your-mod",
  "scripts": [
    "Scripts/main.csx"
  ],
  "dependencies": [
    "required-mod-id:^1.0.0"
  ],
  "loadOrder": 100
}
```

### Versioning

Follow semantic versioning:
- `1.0.0` - Initial release
- `1.0.1` - Bug fix
- `1.1.0` - New feature (backward compatible)
- `2.0.0` - Breaking change

### Distribution

Share your mod:
1. Create a GitHub repository
2. Add clear README with installation instructions
3. Create releases with version tags
4. Submit to PokeNET mod repository

---

## Next Steps

- [Scripting API Reference](../api/scripting.md) - Complete API documentation
- [Script Performance](../performance/script-performance.md) - Optimization guide
- [Script Security](../security/script-security.md) - Security best practices
- [Example Mods](../examples/scripting-examples.md) - More advanced examples

---

**Happy modding!** 🎮

*Last Updated: 2025-10-22*
