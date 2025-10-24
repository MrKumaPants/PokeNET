# Migration Guide: IEventApi to Focused Event APIs

**Version:** 1.0 → 2.0
**Date:** 2025-10-23
**Impact:** All mods using `context.Events.*`

## Overview

The `IEventApi` interface has been deprecated in favor of focused, domain-specific event APIs that better follow the Interface Segregation Principle (ISP).

**Why This Change?**

The old design forced all mods to depend on all event categories, even if they only needed one. This created unnecessary coupling and violated SOLID principles.

## Quick Migration

### Before (Deprecated ⚠️)
```csharp
public class MyMod : IMod
{
    public Task InitializeAsync(IModContext context, CancellationToken ct)
    {
        // OLD: Access through IEventApi facade
        context.Events.Battle.OnBattleStart += OnBattleStart;
        context.Events.UI.OnMenuOpened += OnMenuOpened;
        return Task.CompletedTask;
    }
}
```

### After (Recommended ✅)
```csharp
public class MyMod : IMod
{
    public Task InitializeAsync(IModContext context, CancellationToken ct)
    {
        // NEW: Direct access to focused APIs
        context.BattleEvents.OnBattleStart += OnBattleStart;
        context.UIEvents.OnMenuOpened += OnMenuOpened;
        return Task.CompletedTask;
    }
}
```

## Migration Steps

### 1. Find and Replace

Use your IDE's find-and-replace feature:

| Old Pattern | New Pattern |
|------------|-------------|
| `context.Events.Gameplay` | `context.GameplayEvents` |
| `context.Events.Battle` | `context.BattleEvents` |
| `context.Events.UI` | `context.UIEvents` |
| `context.Events.Save` | `context.SaveEvents` |
| `context.Events.Mod` | `context.ModEvents` |

### 2. Update Event Subscriptions

**Old Code:**
```csharp
public Task InitializeAsync(IModContext context, CancellationToken ct)
{
    context.Events.Gameplay.OnUpdate += OnUpdate;
    context.Events.Battle.OnBattleStart += OnBattleStart;
    context.Events.Battle.OnDamageCalculated += OnDamageCalculated;
    context.Events.UI.OnMenuOpened += OnMenuOpened;
    context.Events.Save.OnSaving += OnSaving;
    return Task.CompletedTask;
}
```

**New Code:**
```csharp
public Task InitializeAsync(IModContext context, CancellationToken ct)
{
    context.GameplayEvents.OnUpdate += OnUpdate;
    context.BattleEvents.OnBattleStart += OnBattleStart;
    context.BattleEvents.OnDamageCalculated += OnDamageCalculated;
    context.UIEvents.OnMenuOpened += OnMenuOpened;
    context.SaveEvents.OnSaving += OnSaving;
    return Task.CompletedTask;
}
```

### 3. Update Unsubscriptions

Don't forget to update cleanup code:

**Old Code:**
```csharp
public Task ShutdownAsync(CancellationToken ct)
{
    context.Events.Battle.OnBattleStart -= OnBattleStart;
    return Task.CompletedTask;
}
```

**New Code:**
```csharp
public Task ShutdownAsync(CancellationToken ct)
{
    context.BattleEvents.OnBattleStart -= OnBattleStart;
    return Task.CompletedTask;
}
```

## Event Category Reference

### IGameplayEvents
**Old:** `context.Events.Gameplay`
**New:** `context.GameplayEvents`

Available events:
- `OnUpdate` - Frame updates
- `OnNewGameStarted` - New game started
- `OnLocationChanged` - Player moved locations
- `OnItemPickedUp` - Item picked up
- `OnItemUsed` - Item used

**Example:**
```csharp
context.GameplayEvents.OnLocationChanged += (sender, args) =>
{
    Logger.LogInformation($"Moved to {args.NewLocation}");
};
```

### IBattleEvents
**Old:** `context.Events.Battle`
**New:** `context.BattleEvents`

Available events:
- `OnBattleStart` - Battle started
- `OnBattleEnd` - Battle ended
- `OnTurnStart` - Turn started
- `OnMoveUsed` - Move used
- `OnDamageCalculated` - Damage calculated (mutable)
- `OnCreatureFainted` - Creature fainted
- `OnCreatureCaught` - Creature caught

**Example:**
```csharp
context.BattleEvents.OnDamageCalculated += (sender, args) =>
{
    // Modify damage (this is mutable!)
    args.Damage = (int)(args.Damage * 1.5);
};
```

### IUIEvents
**Old:** `context.Events.UI`
**New:** `context.UIEvents`

Available events:
- `OnMenuOpened` - Menu opened
- `OnMenuClosed` - Menu closed
- `OnDialogShown` - Dialog shown

**Example:**
```csharp
context.UIEvents.OnMenuOpened += (sender, args) =>
{
    Logger.LogDebug($"Menu opened: {args.MenuName}");
};
```

### ISaveEvents
**Old:** `context.Events.Save`
**New:** `context.SaveEvents`

Available events:
- `OnSaving` - Before save (can add mod data)
- `OnSaved` - After save
- `OnLoading` - Before load
- `OnLoaded` - After load (can read mod data)

**Example:**
```csharp
context.SaveEvents.OnSaving += (sender, args) =>
{
    args.ModData["mymod.data"] = myCustomData;
};

context.SaveEvents.OnLoaded += (sender, args) =>
{
    if (args.ModData.TryGetValue("mymod.data", out var data))
    {
        myCustomData = data;
    }
};
```

### IModEvents
**Old:** `context.Events.Mod`
**New:** `context.ModEvents`

Available events:
- `OnAllModsLoaded` - All mods finished loading
- `OnModUnloaded` - Mod unloaded (hot reload)

**Example:**
```csharp
context.ModEvents.OnAllModsLoaded += (sender, args) =>
{
    Logger.LogInformation($"All {args.ModCount} mods loaded!");
};
```

## Common Patterns

### Pattern 1: Multiple Event Categories

**Before:**
```csharp
var events = context.Events;
events.Battle.OnBattleStart += OnBattleStart;
events.Battle.OnBattleEnd += OnBattleEnd;
events.UI.OnMenuOpened += OnMenuOpened;
```

**After:**
```csharp
context.BattleEvents.OnBattleStart += OnBattleStart;
context.BattleEvents.OnBattleEnd += OnBattleEnd;
context.UIEvents.OnMenuOpened += OnMenuOpened;
```

### Pattern 2: Storing Event References

**Before:**
```csharp
private readonly IEventApi _events;

public void Initialize(IModContext context)
{
    _events = context.Events;
}
```

**After:**
```csharp
private readonly IBattleEvents _battleEvents;
private readonly IUIEvents _uiEvents;

public void Initialize(IModContext context)
{
    _battleEvents = context.BattleEvents;
    _uiEvents = context.UIEvents;
}
```

### Pattern 3: Helper Methods

**Before:**
```csharp
private void SubscribeToEvents(IEventApi events)
{
    events.Battle.OnBattleStart += OnBattleStart;
}
```

**After:**
```csharp
private void SubscribeToBattleEvents(IBattleEvents battleEvents)
{
    battleEvents.OnBattleStart += OnBattleStart;
}

// Or keep single method with multiple parameters
private void SubscribeToEvents(IModContext context)
{
    context.BattleEvents.OnBattleStart += OnBattleStart;
    context.UIEvents.OnMenuOpened += OnMenuOpened;
}
```

## Benefits of Migration

### 1. Clearer Dependencies
Your mod's dependencies are now explicit:

```csharp
// It's immediately clear this mod only uses battle events
public Task InitializeAsync(IModContext context, CancellationToken ct)
{
    context.BattleEvents.OnBattleStart += OnBattleStart;
    return Task.CompletedTask;
}
```

### 2. Better Modularity
Changes to unrelated event systems won't affect your mod:

- Battle system changes won't force recompilation of UI-only mods
- Event categories can evolve independently

### 3. Improved Testability
Easier to mock only the events you need:

```csharp
[Fact]
public void TestBattleLogic()
{
    var mockBattleEvents = new Mock<IBattleEvents>();
    var mod = new MyMod(mockBattleEvents.Object);
    // Only need to mock battle events, not entire IEventApi
}
```

## Timeline

- **v1.0 (Current):** Both old and new APIs available
  - `context.Events.*` works but shows deprecation warnings
  - `context.BattleEvents` etc. recommended

- **v1.5 (Q2 2026):** Migration period
  - Deprecation warnings become more prominent
  - All official examples updated

- **v2.0 (Q4 2026):** Breaking change
  - `IEventApi` removed entirely
  - `context.Events` property removed
  - Only focused APIs available

## Troubleshooting

### Issue: "context.Events is obsolete"

**Solution:** Replace with focused APIs:
```csharp
// OLD: context.Events.Battle
// NEW: context.BattleEvents
```

### Issue: "Cannot resolve IEventApi"

**Solution:** Update using directives and use focused interfaces:
```csharp
// Remove this if present:
// using IEventApi = PokeNET.Domain.Modding.IEventApi;

// Use this instead:
using PokeNET.Domain.Modding;

// Then use:
context.BattleEvents // instead of context.Events.Battle
```

### Issue: "Stored IEventApi in field"

**Solution:** Store specific event interfaces instead:
```csharp
// OLD
private IEventApi _events;

// NEW
private IBattleEvents _battleEvents;
private IUIEvents _uiEvents;
```

## Getting Help

- **Documentation:** `/docs/api/modapi-overview.md`
- **Examples:** `/docs/examples/mods/`
- **Issues:** GitHub Issues with `[migration]` tag
- **Discord:** #mod-development channel

## Automated Migration Tool

We provide a script to help automate migration:

```bash
# Run from your mod directory
dotnet tool install -g PokeNET.ModMigrationTool
pokenet-migrate ieventapi
```

The tool will:
1. Scan your code for `context.Events.*` usage
2. Generate a migration report
3. Optionally auto-fix simple cases
4. Highlight manual changes needed

## Example: Complete Mod Migration

### Before (v1.0)
```csharp
using PokeNET.Domain.Modding;

public class CombatLoggerMod : IMod
{
    private IEventApi? _events;

    public Task InitializeAsync(IModContext context, CancellationToken ct)
    {
        _events = context.Events;

        _events.Battle.OnBattleStart += (s, e) =>
            context.Logger.LogInformation("Battle started!");

        _events.Battle.OnDamageCalculated += (s, e) =>
            context.Logger.LogInformation($"Damage: {e.Damage}");

        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct)
    {
        if (_events != null)
        {
            _events.Battle.OnBattleStart -= OnBattleStart;
        }
        return Task.CompletedTask;
    }
}
```

### After (v2.0 Ready)
```csharp
using PokeNET.Domain.Modding;

public class CombatLoggerMod : IMod
{
    private IBattleEvents? _battleEvents;
    private ILogger? _logger;

    public Task InitializeAsync(IModContext context, CancellationToken ct)
    {
        _battleEvents = context.BattleEvents;
        _logger = context.Logger;

        _battleEvents.OnBattleStart += (s, e) =>
            _logger.LogInformation("Battle started!");

        _battleEvents.OnDamageCalculated += (s, e) =>
            _logger.LogInformation($"Damage: {e.Damage}");

        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct)
    {
        if (_battleEvents != null)
        {
            _battleEvents.OnBattleStart -= OnBattleStart;
        }
        return Task.CompletedTask;
    }
}
```

**Changes Made:**
1. ✅ Changed `IEventApi` to `IBattleEvents`
2. ✅ Changed `context.Events` to `context.BattleEvents`
3. ✅ Updated cleanup code
4. ✅ Only depends on what it uses (ISP compliant)

## Summary

| Aspect | Old Approach | New Approach |
|--------|-------------|--------------|
| **Access Pattern** | `context.Events.Battle` | `context.BattleEvents` |
| **Dependencies** | All event categories | Only what you use |
| **ISP Compliance** | ❌ Violates | ✅ Follows |
| **Coupling** | High | Low |
| **Clarity** | Moderate | High |
| **Migration Effort** | N/A | Simple find/replace |

**Bottom Line:** Simple find-and-replace migration with significant architectural benefits.
