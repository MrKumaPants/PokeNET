# PokeNET: Consolidated Actionable Tasks

> **Single Source of Truth for All Outstanding Work**
> 
> **Last Updated:** October 26, 2025
> 
> This document consolidates all TODOs, implementation gaps, and refactoring tasks from across the codebase documentation. All other implementation summary documents have been archived.

---

## Quick Links

- [Project Status](PROJECT_STATUS.md) - Current implementation status and MVP roadmap
- [Architecture](ARCHITECTURE.md) - System architecture and design
- [Audit Findings](codebase-audit-2025-10-26.md) - Comprehensive codebase audit results

---

## Priority Overview

| Priority | Count | Est. Time | Description |
|----------|-------|-----------|-------------|
| **🔴 CRITICAL** | 12 | 3-4 weeks | Blockers for MVP and architectural violations |
| **🟠 HIGH** | 18 | 2-3 weeks | Core functionality gaps |
| **🟡 MEDIUM** | 15 | 2-3 weeks | Quality and polish |
| **🔵 LOW** | 12 | 1-2 weeks | Nice-to-haves and optimizations |
| **TOTAL** | **57** | **8-12 weeks** | Full implementation |

---

## 🔴 CRITICAL PRIORITY (Blockers & Architecture)

### Architecture Violations

#### C-1. Move RenderSystem from Domain to Core
- **File:** `PokeNET/PokeNET.Domain/ECS/Systems/RenderSystem.cs`
- **Issue:** Contains MonoGame types (`GraphicsDevice`, `SpriteBatch`, `Texture2D`) violating Domain layer purity
- **Impact:** Severe architectural violation
- **Action:** Move to `PokeNET.Core/ECS/Systems/RenderSystem.cs`
- **Est:** 1 hour

#### C-2. Fix PokeNET.Domain MonoGame Reference
- **File:** `PokeNET/PokeNET.Domain/PokeNET.Domain.csproj`
- **Issue:** References `MonoGame.Framework.DesktopGL` (platform-specific)
- **Impact:** Violates layering (Domain should be pure C#)
- **Action:** Remove MonoGame reference entirely from Domain
- **Est:** 2 hours

#### C-3. Correct "Arch.Extended" References in Documentation
- **Issue:** Documentation claims usage of "Arch.Extended" which doesn't exist
- **Reality:** Project uses `Arch.System v1.1.0` (source generators)
- **Files Affected:** 
  - `docs/ARCHITECTURE.md` ✅ Already fixed
  - `docs/codebase-audit-2025-10-26.md` ✅ Already fixed
  - System implementations need to use `BaseSystem<World, float>`
- **Action:** Update remaining code examples and system base classes
- **Est:** 2 hours

### Data Infrastructure (Phase 1: Week 1)

#### C-4. Implement IDataApi + DataManager
- **Status:** Not implemented
- **Impact:** Cannot load Pokemon data (species, moves, items, encounters)
- **Required For:** All game mechanics
- **Tasks:**
  - Create `IDataApi` interface in Domain
  - Implement `DataManager` in Core
  - Support JSON data loading
  - Register in DI container
- **Est:** 8 hours

#### C-5. Create JSON Loaders for Game Data
- **Status:** No implementations
- **Required Loaders:**
  - `SpeciesDataLoader` - Pokemon species stats, types, evolutions
  - `MoveDataLoader` - Move database with power, accuracy, PP, effects
  - `ItemDataLoader` - Item database with effects
  - `EncounterDataLoader` - Wild encounter tables by route/area
- **Format:** JSON schemas following Pokemon standard
- **Est:** 12 hours

#### C-6. Implement TypeChart System
- **Status:** Not implemented
- **Details:** 18×18 effectiveness matrix for type matchups
- **Requirements:**
  - Type effectiveness calculations (0x, 0.25x, 0.5x, 1x, 2x, 4x)
  - Support for dual-type Pokemon
  - Configurable via JSON for mods
- **Est:** 6 hours

#### C-7. Consolidate Battle Stats Models
- **Issue:** Multiple overlapping models violate DRY
- **Problem Files:**
  - `PokeNET.Domain/ECS/Components/Stats.cs` (generic)
  - References to `PokemonStats` in documentation
- **Action:** 
  - Create single `PokemonStats` component
  - Include HP, Attack, Defense, SpAttack, SpDefense, Speed
  - Add IV/EV support
  - Remove generic `Stats` component
- **Est:** 4 hours

### Critical Missing Systems

#### C-8. Wire Save System to DI Container
- **Status:** Exists but not registered
- **Issue:** `ISaveSystem`, `ISaveSerializer`, `IGameStateManager` implementations exist but unused
- **Action:** Register in `Program.cs`:
  ```csharp
  services.AddSingleton<ISaveSystem, SaveSystem>();
  services.AddSingleton<ISaveSerializer, JsonSaveSerializer>();
  services.AddSingleton<ISaveFileProvider, FileSystemSaveFileProvider>();
  services.AddSingleton<ISaveValidator, SaveValidator>();
  services.AddSingleton<IGameStateManager, GameStateManager>();
  ```
- **Est:** 2 hours

#### C-9. Register Audio Services in DI
- **Status:** Exists but not wired
- **Issue:** Complete audio system not connected to DI container
- **Action:** Register audio services:
  ```csharp
  services.AddSingleton<IAudioManager, AudioManager>();
  services.AddSingleton<IMusicPlayer, MusicPlayer>();
  services.AddSingleton<ISoundEffectPlayer, SoundEffectPlayer>();
  services.AddSingleton<IAudioCache, AudioCache>();
  services.AddSingleton<IProceduralMusicGenerator, ProceduralMusicGenerator>();
  services.AddSingleton<IAudioMixer, AudioMixer>();
  ```
- **Est:** 2 hours

#### C-10. Register Scripting Context and API
- **Status:** Commented out in `Program.cs` (lines 247-249)
- **Issue:** Scripts cannot interact with game world
- **Action:** 
  - Uncomment and register `IScriptContext`, `ScriptContext`
  - Uncomment and register `IScriptApi`, `ScriptApi`
  - Test script execution with game API access
- **Est:** 4 hours

#### C-11. Unify Duplicate APIs
- **Issue:** Multiple overlapping API interfaces for mods
- **Problem:** `IEntityApi`, `IAssetApi`, `IEventApi` defined in multiple projects
- **Action:**
  - Consolidate into `PokeNET.ModAPI` project (create if missing)
  - Single source of truth for mod-facing APIs
  - Version all interfaces
- **Est:** 6 hours

#### C-12. Pin Package Versions Consistently
- **Issue:** Mismatched package versions across projects
- **Examples:**
  - `DryWetMidi` - some projects use v7, some v8
  - `Arch` versions inconsistent
- **Action:**
  - Audit all `.csproj` files
  - Pin exact versions in `Directory.Build.props`
  - Document version choices
- **Est:** 2 hours

---

## 🟠 HIGH PRIORITY (Core Gameplay)

### Battle System (Phase 3: Week 3)

#### H-1. Implement Battle UI
- **Status:** Placeholder/stub
- **Required:**
  - Move selection interface (4 moves + PP display)
  - HP bars with smooth animation
  - Battle messages/log
  - Pokemon sprites (player & opponent)
  - Switch Pokemon UI
- **Est:** 16 hours

#### H-2. Implement Party Screen UI
- **Status:** Not implemented
- **Required:**
  - 6-slot party display
  - Pokemon status indicators (HP, status conditions)
  - Move detail view
  - Item usage
  - Pokemon switching
- **Est:** 12 hours

#### H-3. Complete Battle Damage Calculations
- **Status:** Partial (formulas defined, not fully implemented)
- **Required:**
  - Gen 3-5 damage formula
  - Type effectiveness (requires C-6)
  - Critical hit calculation
  - STAB (Same Type Attack Bonus)
  - Random damage variance (0.85-1.0)
- **Est:** 8 hours

#### H-4. Implement Encounter System
- **Status:** Not implemented
- **Required:**
  - Wild encounter triggering (grass, water, caves)
  - Encounter rate calculation
  - Species selection from tables (requires C-5)
  - Level variance
  - Shiny Pokemon (1/8192 chance)
- **Est:** 10 hours

### Pokemon Mechanics (Phase 4: Week 4)

#### H-5. Add Trainer AI
- **Status:** Stub (TODO at `MenuCommand.Execute()` line 37)
- **Required:**
  - Basic decision tree for move selection
  - Type advantage consideration
  - Switch logic when disadvantaged
  - Item usage (Potion, etc.)
- **Est:** 12 hours

#### H-6. Implement Status Effect System
- **Status:** Models defined, logic not implemented
- **Required:**
  - Burn (halve Attack, 1/16 HP damage per turn)
  - Poison (1/8 HP damage per turn)
  - Paralysis (25% chance to not move, Speed quartered)
  - Sleep (2-4 turns, cannot move)
  - Freeze (cannot move until thawed)
  - Confusion (1-4 turns, 50% chance to hit self)
- **Est:** 10 hours

#### H-7. Implement Evolution System
- **Status:** Mentioned in docs, not implemented
- **Required:**
  - Level-based evolution
  - Stone-based evolution
  - Trade evolution
  - Happiness evolution
  - Evolution cancellation (B button)
  - Stat recalculation after evolution
- **Est:** 12 hours

#### H-8. Complete Command Execution Logic
- **File:** `PokeNET/PokeNET.Domain/Input/Commands/MenuCommand.cs` line 37
- **Issue:** `Execute()` method is TODO stub
- **Required:**
  - Menu navigation
  - Action handling
  - Input validation
- **Est:** 4 hours

### Input System Completion

#### H-9. Implement Mouse Input
- **File:** `PokeNET/PokeNET.Domain/Input/InputSystem.cs` line 213
- **Status:** TODO marker
- **Required:**
  - Mouse click detection
  - Hover states
  - Drag support
  - UI interaction
- **Est:** 6 hours

#### H-10. Implement Command Pattern Execution
- **Status:** Command interfaces defined, execution incomplete
- **Required:**
  - Command queue
  - Command validation
  - Undo/redo support (optional)
  - Command batching
- **Est:** 8 hours

### Audio Integration

#### H-11. Implement Audio File Loading
- **File:** `PokeNET.Audio/Services/AudioManager.cs` lines 242, 283, 323, 419, 572
- **Status:** TODO placeholders
- **Required:**
  - WAV file loading
  - OGG file loading
  - MP3 file loading (optional)
  - Integration with `AssetManager`
- **Est:** 10 hours

#### H-12. Register Audio Reactions
- **File:** `PokeNET/PokeNET.DesktopGL/Program.cs` line 333
- **Status:** TODO for registration
- **Required:**
  - Register all audio reaction handlers
  - Wire to event bus
  - Test battle music transitions
- **Est:** 4 hours

#### H-13. Implement Track Queue in MusicPlayer
- **Status:** Mentioned in refactoring docs, not implemented
- **Required:**
  - `NextTrack` property
  - `QueueTrack(AudioTrack)` method
  - `ClearQueue()` method
  - Automatic transition to next track
- **Est:** 6 hours

### Asset Loading

#### H-14. Implement Asset Loaders
- **Status:** `IAssetLoader<T>` interface exists, no implementations
- **Required:**
  - `TextureLoader` - PNG sprites
  - `AudioLoader` - WAV/OGG files (see H-11)
  - `JsonLoader` - Data files (see C-5)
  - `FontLoader` - Text rendering
- **Est:** 12 hours

#### H-15. Create MonoGame Content Pipeline Integration
- **Status:** No `.mgcb` files
- **Required:**
  - `Content.mgcb` file
  - Asset build pipeline
  - `Texture2D`, `SoundEffect` converters
- **Est:** 4 hours

### System Registration

#### H-16. Register ECS Systems
- **File:** `PokeNET/PokeNET.DesktopGL/Program.cs` lines 131-134
- **Status:** TODO comments
- **Required:**
  - Register `RenderSystem` (after C-1)
  - Register `MovementSystem`
  - Register `BattleSystem`
  - Register `InputSystem`
  - Register `AISystem`
- **Est:** 3 hours

#### H-17. Register Asset Loaders
- **File:** `PokeNET/PokeNET.DesktopGL/Program.cs` lines 150-160
- **Status:** TODO comments
- **Required:**
  - Register `JsonLoader`
  - Register `TextureLoader`
  - Register `AudioLoader`
  - Register `FontLoader`
- **Est:** 2 hours

#### H-18. Implement ECS Serialization
- **Status:** Save system exists but can't save entities
- **Impact:** Cannot save game state
- **Required:**
  - Arch World serialization
  - Component serialization strategy
  - Entity ID mapping
  - Archetype serialization
- **Est:** 16 hours

---

## 🟡 MEDIUM PRIORITY (Quality & Polish)

> **Note**: The SystemBase/SystemBaseEnhanced refactoring (originally M-1) is **NOT APPLICABLE** - systems already use `Arch.System.BaseSystem<World, float>` with lifecycle hooks and source generators. See `tmp/refactoring_plan.md` for historical context only.

### Pokemon-Specific Components

#### M-1. Replace Physics Components with Tile-Based Movement
- **Issue:** `Acceleration`, `Friction`, `Velocity` are wrong abstraction for Pokemon
- **Required Components:**
  - `GridPosition` - Tile-based position (TileX, TileY, MapId)
  - `Direction` enum - 8-way discrete direction
  - `MovementState` - Movement speed, facing, CanMove
  - `TileCollider` - Collision layer, surf/cut requirements
- **Action:** Mark old components `[Obsolete]`, migrate systems
- **Est:** 12 hours

#### M-2. Create Pokemon Battle Components
- **Required:**
  - `PokemonData` - Species, level, type, trainer ID, shiny, gender
  - `MoveSet` - 4 move slots with PP
  - `StatusCondition` - Burn, poison, sleep, etc.
- **Est:** 6 hours

#### M-3. Create Trainer/Party Components
- **Required:**
  - `Trainer` - Trainer ID, name, class, money
  - `Party` - 6 Pokemon slots
  - `Inventory` - Items, key items, TMs, berries
  - `PlayerProgress` - Badges, flags, Pokedex
- **Est:** 8 hours

### Audio Refactoring

#### M-4. Refactor Reactive Audio Engine
- **Issue:** Hard-coded event subscriptions violate Open/Closed Principle
- **File:** `PokeNET.Audio/Reactive/ReactiveAudioEngine.cs` lines 73-81
- **Action:**
  - Implement strategy pattern with `IAudioReaction`
  - Load reactions from JSON configuration
  - Support mod-provided reactions
- **Est:** 14 hours

#### M-5. Remove Sync-Over-Async Anti-Patterns
- **Issue:** `.GetAwaiter().GetResult()` blocks threads
- **File:** `PokeNET.Audio/Reactive/ReactiveAudioEngine.cs` lines 190-212
- **Action:** Convert to proper async/await throughout
- **Est:** 4 hours

#### M-6. Connect Procedural Music to Game State
- **Status:** Generator exists but not integrated
- **Required:**
  - Map game state → music parameters
  - Real-time adaptation hooks
  - Example: battle tension → music intensity
- **Est:** 10 hours

### Testing Infrastructure

#### M-7. Create Unit Test Suite
- **Status:** `PokeNET.Tests` project empty
- **Required:**
  - Mod loading tests
  - Asset loading tests
  - Serialization tests
  - Script execution tests
  - ECS system tests
- **Target:** 60%+ coverage
- **Est:** 24 hours

#### M-8. Implement Integration Tests
- **Required:**
  - Full battle sequence test
  - Save/load cycle test
  - Mod loading end-to-end test
  - Audio playback test
- **Est:** 12 hours

### Code Quality

#### M-9. Enable Nullable Reference Types
- **Status:** Not enabled project-wide
- **Action:** Add to `Directory.Build.props`:
  ```xml
  <Nullable>enable</Nullable>
  ```
- **Est:** 8 hours (includes fixing warnings)

#### M-10. Add Architecture Tests
- **Package:** NetArchTest.Rules
- **Required Rules:**
  - Domain cannot reference MonoGame
  - Core can only reference Domain
  - Mods can only reference ModApi
- **Est:** 4 hours

#### M-11. Remove Dead Code
- **Issues:**
  - Unused using statement in `PokeNETGame.cs` line 8
  - Unused `languages` list lines 60-65
  - Unused TODO comments throughout
- **Est:** 2 hours

#### M-12. Fix Exception Types
- **File:** `LocalizationManager.cs` line 78-79
- **Issue:** Uses `ArgumentNullException` for empty string
- **Action:** Use `ArgumentException` or `ArgumentException.ThrowIfNullOrWhiteSpace()`
- **Est:** 1 hour

### Documentation

#### M-13. Create Modding Tutorial
- **Status:** API reference exists, no step-by-step guide
- **Required:**
  - "Your First Mod" tutorial
  - Harmony patching guide
  - Script API examples
  - Asset override examples
- **Est:** 8 hours

#### M-14. Generate API Documentation
- **Tool:** DocFX or similar
- **Required:**
  - Automated API reference
  - Code examples
  - Integration with website
- **Est:** 6 hours

#### M-15. Create Developer Quick Start Guide
- **Required:**
  - Setup instructions
  - Build process
  - Running tests
  - Debugging tips
- **Est:** 4 hours

---

## 🔵 LOW PRIORITY (Nice-to-Have)

### Performance & Optimization

#### L-1. Finalize Benchmark Baselines
- **Issue:** Benchmarks reference "TBD" values
- **Action:** Run benchmarks, document baselines, set performance budgets
- **Est:** 4 hours

#### L-2. Implement Asset Cache Eviction
- **Status:** Basic cache exists, no LRU eviction
- **Required:**
  - Cache size limits
  - LRU eviction policy
  - Cache statistics
- **Est:** 6 hours

#### L-3. Add Delta Compression for Saves
- **Status:** Full save only
- **Required:**
  - Snapshot diffing
  - Incremental saves
  - Compressed format
- **Est:** 12 hours

### Advanced Features

#### L-4. Implement Hot Reload for Assets
- **Status:** Not implemented (Phase 11 feature)
- **Required:**
  - File watchers for assets and JSON
  - Reload mechanism
  - Cache invalidation
- **Est:** 10 hours

#### L-5. Add Auto-Save System
- **Status:** Manual save only
- **Required:**
  - Periodic auto-save
  - Quick save hotkey
  - Save slot management
- **Est:** 6 hours

#### L-6. Create Weather System
- **Status:** `WeatherChangedEvent` exists, no implementation
- **Required:**
  - Weather types (Sun, Rain, Sandstorm, Hail)
  - Battle effects (boost Fire in Sun, etc.)
  - Overworld effects (visual)
- **Est:** 8 hours

### Polish

#### L-7. Add Logging Infrastructure
- **Status:** Some logging exists, not comprehensive
- **Action:** Add structured logging throughout with ILogger<T>
- **Est:** 6 hours

#### L-8. Modernize C# Syntax
- **Changes:**
  - File-scoped namespaces
  - Target-typed `new()`
  - Primary constructors (C# 12)
  - Collection expressions
- **Est:** 4 hours

### Future-Proofing

#### L-9. Design Network/Multiplayer APIs
- **Status:** No network consideration
- **Issue:** Pokemon games traditionally support trading, battles, Wonder Trade
- **Required:**
  - Design `INetworkApi` interface
  - Pokemon serialization format for exchange
  - Save encryption option (anti-cheating)
  - Local link battle simulation
- **Note:** Design only, not full implementation
- **Est:** 8 hours

#### L-10. Complete Localization System
- **Status:** Basic resx files exist, incomplete
- **Issue:** No `ILocalizationApi` for mods, hardcoded strings
- **Required:**
  - Create `ILocalizationApi` for ModAPI
  - Wrap all UI strings in localization calls
  - Support mod-provided translations
  - Language selection UI
- **Est:** 10 hours

#### L-11. Advanced Persistence Support
- **Status:** Basic save system works, complex types not handled
- **Issue:** Missing serialization for:
  - `Pokedex` state (BitArray custom serialization)
  - `Inventory` (nested collections)
  - Battle state mid-combat
  - Audio state for seamless restore
- **Required:**
  - Custom JSON formatters for complex types
  - Battle state snapshot/restore
  - Audio state persistence
- **Est:** 12 hours

#### L-12. Enforce Performance Budgets
- **Status:** Monitoring exists, no enforcement
- **Issue:** No production-level performance guarantees
- **Required:**
  - Frame-time budget (16ms for 60fps)
  - Memory limits for scripts/assets
  - System execution time tracking
  - Performance warnings/errors in dev builds
- **Est:** 8 hours

---

## Example Content Creation

### Required for Phase 8 POC

#### Example Creature JSON
```json
{
  "speciesId": 1,
  "name": "Bulbasaur",
  "types": ["Grass", "Poison"],
  "baseStats": {
    "hp": 45,
    "attack": 49,
    "defense": 49,
    "spAttack": 65,
    "spDefense": 65,
    "speed": 45
  },
  "evolutions": [
    { "species": 2, "method": "level", "level": 16 }
  ],
  "learnset": [
    { "level": 1, "move": "Tackle" },
    { "level": 7, "move": "Vine Whip" }
  ]
}
```

#### Example Ability Script
```csharp
// scripts/abilities/overgrow.csx
#r "PokeNET.ModApi.dll"

using PokeNET.ModApi;

public class Overgrow : IAbility
{
    public string Name => "Overgrow";
    public string Description => "Powers up Grass-type moves when HP is low.";
    
    public float ModifyDamage(IBattleContext context, IMove move)
    {
        if (move.Type == PokemonType.Grass && context.Attacker.HP <= context.Attacker.MaxHP / 3)
        {
            return 1.5f; // 50% boost
        }
        return 1.0f;
    }
}
```

#### Example Harmony Patch Mod
```csharp
using HarmonyLib;
using PokeNET.Domain.Battle;

[HarmonyPatch(typeof(DamageCalculator), "CalculateDamage")]
public class CriticalHitBoostPatch
{
    static void Postfix(ref int __result, bool isCritical)
    {
        if (isCritical)
        {
            __result = (int)(__result * 2.5f); // Buff critical hits
        }
    }
}
```

---

## Implementation Phases

### Phase 1: Data Infrastructure (Week 1)
**Time:** 32 hours
- C-4, C-5, C-6, C-7

### Phase 2: Fix Architecture (Week 2)
**Time:** 19 hours
- C-1, C-2, C-3, C-8, C-9, C-10, C-11, C-12

### Phase 3: Core Gameplay (Weeks 3-4)
**Time:** 58 hours
- H-1, H-2, H-3, H-4, H-16, H-17, H-18

### Phase 4: Pokemon Mechanics (Week 5)
**Time:** 38 hours
- H-5, H-6, H-7, H-8, M-1, M-2, M-3

### Phase 5: Audio & Input (Week 6)
**Time:** 44 hours
- H-9, H-10, H-11, H-12, H-13, M-4, M-5, M-6

### Phase 6: Asset Loading & Testing (Week 7)
**Time:** 54 hours
- H-14, H-15, M-7, M-8, M-9, M-10

### Phase 7: Polish & Documentation (Week 8)
**Time:** 33 hours
- M-11, M-12, M-13, M-14, M-15, L-7, L-8

### Phase 8: Optional Features (Weeks 9-12)
**Time:** 94 hours
- L-1, L-2, L-3, L-4, L-5, L-6, L-9, L-10, L-11, L-12

---

## Success Criteria

### Minimum Viable Product (MVP)
- ✅ All CRITICAL tasks complete
- ✅ All HIGH tasks complete
- ✅ 50%+ of MEDIUM tasks complete
- ✅ Example content created
- ✅ One complete battle playable

### Production Ready
- ✅ All CRITICAL, HIGH, MEDIUM tasks complete
- ✅ 60%+ test coverage
- ✅ All documentation complete
- ✅ Performance benchmarks met
- ✅ Zero high-severity bugs

---

## Tracking Progress

Use this document as the single source of truth for all implementation work. As tasks are completed:

1. Mark with ✅ checkbox
2. Add completion date
3. Link to PR or commit
4. Update PROJECT_STATUS.md

**Do not create new TODO documents** - add new tasks here instead.

---

## Notes

- **Total Estimated Effort:** 372 hours (~8-12 weeks for 1 developer)
- **Critical Path:** Phases 1-3 must be completed in order
- **Parallelizable:** Audio, asset loading, and testing can overlap
- **MVP Target:** End of Phase 4 (5 weeks)
- **Production Target:** End of Phase 7 (8 weeks)
- **Full Feature Set:** End of Phase 8 (12 weeks)

**Last Updated:** October 26, 2025

