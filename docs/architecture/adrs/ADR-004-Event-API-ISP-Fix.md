f# ADR-004: Fix Interface Segregation Principle Violation in Event API

**Status:** Accepted

**Date:** 2025-10-23

**Context:**

The original `IEventApi` interface violated the Interface Segregation Principle (ISP) by forcing all mods to depend on all event categories, even when they only needed access to a specific subset.

## Problem

The previous implementation exposed events through a single monolithic interface:

```csharp
public interface IModContext
{
    IEventApi Events { get; }
}

public interface IEventApi
{
    IGameplayEvents Gameplay { get; }
    IBattleEvents Battle { get; }
    IUIEvents UI { get; }
    ISaveEvents Save { get; }
    IModEvents Mod { get; }
}
```

**ISP Violations:**

1. **Unnecessary Dependencies:** A UI-only mod that only needs `IUIEvents` was forced to depend on `IBattleEvents`, `IGameplayEvents`, etc.

2. **Compilation Coupling:** Changes to `IBattleEvents` could force recompilation of UI mods that never used battle events.

3. **Fat Interface Anti-Pattern:** `IEventApi` served as a facade but created unnecessary coupling between unrelated concerns.

4. **Modularity Issues:** Mods couldn't declare precise dependencies - they had to take "all events or nothing."

## Decision

We refactored the event API to expose event categories independently through `IModContext`:

```csharp
public interface IModContext
{
    // Backwards compatible (obsolete)
    [Obsolete("Use specific event properties instead")]
    IEventApi Events { get; }

    // New ISP-compliant properties
    IGameplayEvents GameplayEvents { get; }
    IBattleEvents BattleEvents { get; }
    IUIEvents UIEvents { get; }
    ISaveEvents SaveEvents { get; }
    IModEvents ModEvents { get; }
}
```

**Key Design Decisions:**

1. **Independent Access:** Each event category is accessible directly without depending on other categories.

2. **Backwards Compatibility:** The old `IEventApi Events` property remains available but marked obsolete. It will be removed in v2.0.

3. **Shared Implementation:** All event properties share the same underlying implementation (EventBus) to maintain consistency.

4. **No Breaking Changes:** Existing mods continue to work unchanged - they just get deprecation warnings.

## Architecture

### Before (ISP Violation)
```
┌──────────────┐
│   UI Mod     │
└──────┬───────┘
       │ depends on
       ▼
┌──────────────────────────────┐
│       IEventApi              │
│  - Gameplay { get; }         │
│  - Battle { get; }           │ ◄─── Forces dependency
│  - UI { get; }               │      on ALL categories
│  - Save { get; }             │
│  - Mod { get; }              │
└──────────────────────────────┘
```

### After (ISP Compliant)
```
┌──────────────┐
│   UI Mod     │
└──────┬───────┘
       │ depends only on
       ▼
┌──────────────┐
│  IUIEvents   │  ◄─── Only what it needs
└──────────────┘

┌──────────────┐
│ Battle Mod   │
└──────┬───────┘
       │ depends only on
       ▼
┌──────────────┐
│IBattleEvents │  ◄─── Only what it needs
└──────────────┘
```

## Implementation Details

### 1. Updated IModContext

Added focused event properties to `IModContext`:

```csharp
public interface IModContext
{
    IGameplayEvents GameplayEvents { get; }
    IBattleEvents BattleEvents { get; }
    IUIEvents UIEvents { get; }
    ISaveEvents SaveEvents { get; }
    IModEvents ModEvents { get; }
}
```

### 2. Obsoleted IEventApi

Marked the monolithic interface as obsolete:

```csharp
[Obsolete("Use IModContext.GameplayEvents, BattleEvents, etc. instead")]
public interface IEventApi
{
    // Kept for backwards compatibility
}
```

### 3. Updated ModContext Implementation

Modified `ModContext` to expose event categories independently:

```csharp
public sealed class ModContext : IModContext
{
    public IGameplayEvents GameplayEvents { get; }
    public IBattleEvents BattleEvents { get; }
    // ... other event categories

    public ModContext(...)
    {
        var eventApiStub = new EventApiStub();
        GameplayEvents = eventApiStub.Gameplay;
        BattleEvents = eventApiStub.Battle;
        // ... initialize other categories
    }
}
```

## Migration Path

### Old Usage (Deprecated)
```csharp
public class MyMod : IMod
{
    public Task InitializeAsync(IModContext context, CancellationToken ct)
    {
        // OLD: Through IEventApi
        context.Events.UI.OnMenuOpened += HandleMenuOpened;
        return Task.CompletedTask;
    }
}
```

### New Usage (Recommended)
```csharp
public class MyMod : IMod
{
    public Task InitializeAsync(IModContext context, CancellationToken ct)
    {
        // NEW: Direct access
        context.UIEvents.OnMenuOpened += HandleMenuOpened;
        return Task.CompletedTask;
    }
}
```

## Consequences

### Positive

1. **Better Modularity:** Mods depend only on the event categories they actually use.

2. **Reduced Coupling:** Changes to battle events don't affect UI mods.

3. **Clearer Intent:** Code explicitly shows which event domains are used.

4. **SOLID Compliance:** Properly follows the Interface Segregation Principle.

5. **Backwards Compatible:** Existing mods continue to work without changes.

### Negative

1. **More Properties:** `IModContext` has more properties (6 instead of 1 for events).

2. **Migration Overhead:** Existing mods need to update their code to remove deprecation warnings.

3. **Documentation Updates:** All examples and tutorials need updating.

### Neutral

1. **Implementation Complexity:** Similar complexity - just organized differently.

2. **Runtime Performance:** No performance impact - same underlying implementation.

## Related Decisions

- **ADR-001:** Scripting System Interfaces (established interface design patterns)
- **ADR-002:** (If exists) ECS event system architecture
- **Phase 4 Architecture:** Original modding API design

## References

- SOLID Principles: Interface Segregation Principle
- Martin, Robert C. "Agile Software Development, Principles, Patterns, and Practices"
- Event-Driven Architecture patterns
- Facade pattern vs. ISP trade-offs

## Future Considerations

1. **V2.0 Release:** Remove obsolete `IEventApi` entirely.

2. **EventBus Integration:** When EventBus is fully implemented, wire real event publishing through these interfaces.

3. **Additional Categories:** New event categories (e.g., `INetworkEvents`, `IAudioEvents`) can be added independently.

4. **Event Filtering:** Consider adding event filtering/transformation capabilities to each category.

## Status

- ✅ Design approved
- ✅ IModContext updated
- ✅ IEventApi marked obsolete
- ✅ ModContext implementation updated
- ✅ Documentation written
- ⏳ Migration guide created
- ⏳ Example code updated
- ⏳ V2.0 removal planned
