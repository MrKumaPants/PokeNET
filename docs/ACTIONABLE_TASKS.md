# PokeNET: Consolidated Actionable Tasks

> **Single Source of Truth for All Outstanding Work**
> 
> **Last Updated:** October 27, 2025
> **Target Mechanics:** Generation VI+ (Gen 6+) Pokemon mechanics
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
| **🔴 CRITICAL** | 6 | 1-2 weeks | Blockers for MVP and architectural violations |
| **🟠 HIGH** | 20 | 3-4 weeks | Core functionality gaps (includes script creation) |
| **🟡 MEDIUM** | 16 | 2-3 weeks | Quality and polish (includes EventBus migration) |
| **🔵 LOW** | 12 | 1-2 weeks | Nice-to-haves and optimizations |
| **TOTAL** | **54** | **7-10 weeks** | Full implementation (4 tasks complete) |

---

## 🔴 CRITICAL PRIORITY (Blockers & Architecture)

### Eventing Standard

**All event communication must use `Arch.EventBus`** (from Arch ECS ecosystem):
- **Package**: Already installed (`Arch.EventBus` v1.0.2)
- **Migration**: Replace custom `EventBus` implementation with `Arch.EventBus`
- **Usage**: `World.SendEvent<T>()` and `World.Subscribe<T>()` for event communication
- **Benefits**: Tight integration with ECS systems, optimized performance, event replay, buffering, and filtering
- **Priority**: MEDIUM - Custom EventBus works but Arch integration preferred

### Architecture Violations

#### ✅ C-1. Move RenderSystem from Domain to Core [COMPLETED 2025-10-26]
- **Status:** Obsolete - RenderSystem already in Core (PokeNET.Domain merged into PokeNET.Core)
- **Resolution:** Project merge eliminated Domain layer entirely, all systems now in Core
- **Completed:** 2025-10-26

#### ✅ C-2. Fix PokeNET.Domain MonoGame Reference [COMPLETED 2025-10-26]
- **Status:** Resolved - PokeNET.Domain project merged into PokeNET.Core
- **Resolution:** MonoGame references properly isolated in Core project, no layering violations
- **Impact:** Project structure simplified from 8 to 7 projects
- **Completed:** 2025-10-26

#### C-3. Correct "Arch.Extended" References in Documentation
- **Issue:** Documentation claims usage of "Arch.Extended" which doesn't exist
- **Reality:** Project uses `Arch.System v1.1.0` (source generators)
- **Files Affected:** 
  - `docs/ARCHITECTURE.md` ✅ Already fixed
  - `docs/codebase-audit-2025-10-26.md` ✅ Already fixed
  - System implementations need to use `BaseSystem<World, float>`
- **Action:** Update remaining code examples and system base classes
- **Est:** 2 hours

### Data Infrastructure (Phase 1: Week 1) ✅ **COMPLETE**

#### ✅ C-4. Implement IDataApi + DataManager [COMPLETED 2025-10-27]
- **Status:** COMPLETE
- **Delivered:**
  - `IDataApi` interface in `PokeNET.Core/Data/IDataApi.cs`
  - `DataManager` implementation with full caching and mod support
  - JSON data loading for all game data types
  - Registered in DI container via `AddDataServices()`
- **Files Created:** `IDataApi.cs`, `DataManager.cs`
- **Tests:** 127 passing (DataManagerTests, TypeDataTests, TypeEffectivenessTests)
- **Completed:** 2025-10-27

#### ✅ C-5. Create JSON Loaders for Game Data [COMPLETED 2025-10-27]
- **Status:** COMPLETE
- **Delivered Loaders:**
  - `SpeciesDataLoader` - Pokemon species with stats, types, evolutions
  - `MoveDataLoader` - Move database with power, accuracy, PP, effects
  - `ItemDataLoader` - Item database with effects
  - `EncounterDataLoader` - Wild encounter tables by route/area
  - `TypeDataLoader` - Type data with matchups (Gen 6+ including Fairy)
- **Files Created:** `BaseDataLoader.cs`, `JsonDataLoader.cs`, `SpeciesDataLoader.cs`, `MoveDataLoader.cs`, `ItemDataLoader.cs`, `EncounterDataLoader.cs`
- **Sample Data:** species.json, moves.json, items.json, encounters.json, types.json
- **Completed:** 2025-10-27

#### ✅ C-6. Implement TypeChart System [COMPLETED 2025-10-27]
- **Status:** COMPLETE - Refactored to data-driven approach
- **Delivered:**
  - String-based type system (no hardcoded `PokemonType` enum)
  - `TypeData` model for individual types with matchups
  - `types.json` with all 18 types including Fairy (Gen 6+)
  - Type effectiveness calculations (0x, 0.25x, 0.5x, 1x, 2x, 4x)
  - Dual-type Pokemon support
  - Fully moddable via JSON
- **Files Created:** `TypeData.cs`, `types.json`
- **Files Deleted:** `PokemonType.cs` (enum), `TypeChart.cs` (hardcoded)
- **Tests:** 27 passing (TypeDataTests + TypeEffectivenessTests)
- **Completed:** 2025-10-27

#### ✅ C-7. Consolidate Battle Stats Models [COMPLETED 2025-10-27]
- **Status:** COMPLETE
- **Delivered:**
  - Single canonical `PokemonStats` component with HP, Attack, Defense, SpAttack, SpDefense, Speed
  - Full IV/EV support (0-31 IVs, 0-252 EVs)
  - `StatCalculator` utility for Gen 3+ formulas
  - Removed deprecated `Stats` component
  - Removed deprecated methods from `PokemonStats`
- **Files Deleted:** `Stats.cs`
- **Files Modified:** `PokemonStats.cs`, `ComponentBuilders.cs`, `QueryExtensionsLegacy.cs`, `BattleSystem.cs`
- **Tests:** 23 passing (StatCalculatorTests)
- **Completed:** 2025-10-27

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
  - Replace custom `IEventApi` with `Arch.EventBus` wrappers
  - Version all interfaces
- **Est:** 6 hours

#### C-12. Migrate to Arch.EventBus
- **Status:** `Arch.EventBus` v1.0.2 already installed
- **Issue:** Custom `EventBus` implementation exists alongside Arch.EventBus
- **Files:**
  - `PokeNET/PokeNET.Core/ECS/EventBus.cs` (custom implementation)
  - `PokeNET/PokeNET.Core/ECS/Events/IEventBus.cs` (custom interface)
- **Action:**
  - Migrate all event subscriptions to use `Arch.EventBus`
  - Update `World` to use `world.SendEvent<T>()` and `world.Subscribe<T>()`
  - Deprecate custom `EventBus` and `IEventBus`
  - Update audio reactions to subscribe via Arch.EventBus
  - Update command system to publish via Arch.EventBus
- **Benefits:** Zero-allocation dispatch, event buffering, tight ECS integration
- **Priority:** MEDIUM
- **Est:** 8 hours

#### C-13. Pin Package Versions Consistently
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
  - Gen 6+ damage formula
  - Type effectiveness (requires C-6)
  - Critical hit calculation (Gen 6+ rates: 1/16 base, 1/8 high crit)
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
  - Shiny Pokemon (1/4096 chance in Gen 6+)
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
- **Required (Gen 6+ mechanics):**
  - Burn (halve Attack for Physical moves only, 1/16 HP damage per turn)
  - Poison (1/8 HP damage per turn)
  - Badly Poisoned (1/16 HP damage, increases each turn)
  - Paralysis (25% chance to not move, Speed quartered)
  - Sleep (1-3 turns in Gen 6+, cannot move)
  - Freeze (20% thaw chance per turn, thaws immediately if hit by Fire move)
  - Confusion (1-4 turns, 33% chance to hit self in Gen 6+)
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
  - Wire to Arch.EventBus (subscribe to game events)
  - Test battle music transitions
  - Migrate from custom EventBus to Arch.EventBus
- **Est:** 4 hours

#### H-13. Implement Track Queue in MusicPlayer
- **Status:** Mentioned in refactoring docs, not implemented
- **Required:**
  - `NextTrack` property
  - `QueueTrack(AudioTrack)` method
  - `ClearQueue()` method
  - Automatic transition to next track
- **Est:** 6 hours

### Scripting & Effects

#### H-14. Create Effect Scripts
- **Status:** JSON references scripts that don't exist
- **Issue:** Items and moves reference `.csx` scripts that aren't implemented
- **Required Scripts:**
  - **Item Effects:**
    - `scripts/items/heal.csx` - HP restoration (Potion family)
    - `scripts/items/status-heal.csx` - Cure status conditions
    - `scripts/items/catch.csx` - Pokeball catch logic
    - `scripts/items/stat-boost.csx` - X Attack, X Defense, etc.
  - **Move Effects:**
    - `scripts/moves/damage.csx` - Basic damage (most moves)
    - `scripts/moves/status-inflict.csx` - Burn, Poison, Paralyze
    - `scripts/moves/stat-change.csx` - Growl, Swords Dance, etc.
    - `scripts/moves/priority.csx` - Quick Attack, etc.
- **Location:** `Content/scripts/` directory
- **Examples:** See `docs/examples/effect-scripts/` for templates
- **Est:** 16 hours (20+ scripts)

#### H-15. Implement Script Loading in Battle/Item Systems
- **Status:** Scripts defined, but not loaded/executed
- **Issue:** Need to integrate script execution into battle and item use
- **Required:**
  - Load and compile scripts via `IScriptEngine`
  - Cache compiled scripts for performance
  - Execute item effects when items are used
  - Execute move effects during battle
  - Handle script errors gracefully
- **Dependencies:** H-14 (scripts must exist)
- **Est:** 12 hours

### Asset Loading

#### H-16. Implement Asset Loaders
- **Status:** `IAssetLoader<T>` interface exists, no implementations
- **Required:**
  - `TextureLoader` - PNG sprites
  - `AudioLoader` - WAV/OGG files (see H-11)
  - `JsonLoader` - Data files (see C-5)
  - `FontLoader` - Text rendering
- **Est:** 12 hours

#### H-17. Create MonoGame Content Pipeline Integration
- **Status:** No `.mgcb` files
- **Required:**
  - `Content.mgcb` file
  - Asset build pipeline
  - `Texture2D`, `SoundEffect` converters
- **Est:** 4 hours

### System Registration

#### H-18. Register ECS Systems
- **File:** `PokeNET/PokeNET.DesktopGL/Program.cs` lines 131-134
- **Status:** TODO comments
- **Required:**
  - Register `RenderSystem` (after C-1)
  - Register `MovementSystem`
  - Register `BattleSystem`
  - Register `InputSystem`
  - Register `AISystem`
- **Est:** 3 hours

#### H-19. Register Asset Loaders
- **File:** `PokeNET/PokeNET.DesktopGL/Program.cs` lines 150-160
- **Status:** TODO comments
- **Required:**
  - Register `JsonLoader`
  - Register `TextureLoader`
  - Register `AudioLoader`
  - Register `FontLoader`
- **Est:** 2 hours

#### H-20. Implement ECS Serialization
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
  - Migrate to `Arch.EventBus` for event subscriptions
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

### Phase 1: Data Infrastructure (Week 1) ✅ **COMPLETE**
**Time:** 32 hours (COMPLETED)
- ~~C-4~~ ✅ IDataApi + DataManager (2025-10-27)
- ~~C-5~~ ✅ JSON Loaders (2025-10-27)
- ~~C-6~~ ✅ TypeChart/TypeData System (2025-10-27)
- ~~C-7~~ ✅ Stats Consolidation (2025-10-27)
- **Status:** All deliverables complete, 127 tests passing
- **See:** `docs/PHASE1_COMPLETION_REPORT.md`

### Phase 2: Architecture & DI (Week 2)
**Time:** 24 hours
**Focus:** Fix layering, register services, consolidate APIs
- ~~C-1~~ ✅ Complete (Domain merged)
- ~~C-2~~ ✅ Complete (Domain merged)
- C-3: Documentation corrections (2h)
- C-8: Wire save system to DI (2h)
- C-9: Register audio services (2h)
- C-10: Register scripting context (4h)
- C-11: Unify duplicate APIs (6h)
- C-12: Migrate to Arch.EventBus (8h)
- C-13: Pin package versions (2h)

### Phase 3: Core Battle Mechanics (Weeks 3-4)
**Time:** 58 hours
**Focus:** Backend battle logic, no UI
- H-3: Complete battle damage calculations (8h)
- H-4: Implement encounter system (10h)
- H-5: Add trainer AI (12h)
- H-6: Implement status effect system (10h)
- H-14: Create effect scripts (16h)
- H-15: Implement script loading (12h)

### Phase 4: Pokemon Systems (Week 5)
**Time:** 30 hours
**Focus:** Pokemon-specific functionality, no UI
- H-7: Implement evolution system (12h)
- H-8: Complete command execution logic (4h)
- M-1: Replace physics with tile-based movement (12h)
- M-2: Create Pokemon battle components (6h)
- M-3: Create trainer/party components (8h)
- **Note:** M-3 reduced from 8h to 6h (UI portion moved to Phase 7)

### Phase 5: System Integration (Week 6)
**Time:** 41 hours
**Focus:** ECS systems, asset loading, serialization
- H-16: Implement asset loaders (12h)
- H-17: MonoGame content pipeline (4h)
- H-18: Register ECS systems (3h)
- H-19: Register asset loaders (2h)
- H-20: Implement ECS serialization (16h)
- H-9: Implement mouse input (6h)
- H-10: Implement command pattern execution (8h)

### Phase 6: Audio System (Week 7)
**Time:** 44 hours
**Focus:** Audio integration and reactivity
- H-11: Implement audio file loading (10h)
- H-12: Register audio reactions (4h)
- H-13: Implement track queue in MusicPlayer (6h)
- M-4: Refactor reactive audio engine (14h)
- M-5: Remove sync-over-async anti-patterns (4h)
- M-6: Connect procedural music to game state (10h)

### Phase 7: User Interface (Weeks 8-9)
**Time:** 42 hours
**Focus:** All UI implementation
- H-1: Implement battle UI (16h)
- H-2: Implement party screen UI (12h)
- M-3 (UI portion): Party screen integration (2h)
- UI menus for save/load (6h)
- Input/command UI integration (6h)

### Phase 8: Testing & Quality (Week 10)
**Time:** 54 hours
**Focus:** Test coverage and code quality
- M-7: Create unit test suite (24h)
- M-8: Implement integration tests (12h)
- M-9: Enable nullable reference types (8h)
- M-10: Add architecture tests (4h)
- M-11: Remove dead code (2h)
- M-12: Fix exception types (1h)
- L-7: Add logging infrastructure (6h)

### Phase 9: Documentation & Polish (Week 11)
**Time:** 33 hours
**Focus:** Documentation and code modernization
- M-13: Create modding tutorial (8h)
- M-14: Generate API documentation (6h)
- M-15: Create developer quick start guide (4h)
- L-8: Modernize C# syntax (4h)
- L-1: Finalize benchmark baselines (4h)
- Performance tuning (7h)

### Phase 10: Optional Features (Weeks 12+)
**Time:** 94 hours
**Focus:** Nice-to-have features
- L-2: Asset cache eviction (6h)
- L-3: Delta compression for saves (12h)
- L-4: Hot reload for assets (10h)
- L-5: Auto-save system (6h)
- L-6: Weather system (8h)
- L-9: Network/multiplayer APIs design (8h)
- L-10: Complete localization system (10h)
- L-11: Advanced persistence support (12h)
- L-12: Enforce performance budgets (8h)
- Additional polish (14h)

---

## Success Criteria

### Backend MVP (Phases 1-6)
- ✅ All CRITICAL tasks complete
- ✅ All HIGH backend tasks complete (no UI)
- ✅ Battle mechanics functional (damage, types, status effects)
- ✅ Pokemon systems working (evolution, stats, moves, items)
- ✅ Scripts executable (item effects, move effects)
- ✅ Save/load functional
- ⚠️ **No UI** - can only be tested via unit tests/console

### Playable MVP (Through Phase 7)
- ✅ All backend MVP criteria met
- ✅ Battle UI implemented
- ✅ Party screen UI implemented
- ✅ Menu system functional
- ✅ One complete battle playable end-to-end
- ✅ Example content created

### Production Ready (Through Phase 9)
- ✅ All Playable MVP criteria met
- ✅ 60%+ test coverage
- ✅ All documentation complete
- ✅ Performance benchmarks met
- ✅ Zero high-severity bugs
- ✅ Modding guide available

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

- **Total Estimated Effort:** 373 hours (~7-10 weeks for 1 developer) - 32 hours completed
- **Project Structure:** 7 projects (PokeNET.Domain merged into PokeNET.Core on 2025-10-26)
- **Target Mechanics:** Generation VI+ (Gen 6+) - see PROJECT_STATUS.md for details
- **Critical Path:** Phases 1-6 must be completed before UI (Phase 7)
- **Parallelizable:** Testing (Phase 8) can start alongside UI work
- **MVP Target (Backend):** End of Phase 6 (6 weeks remaining) - fully functional, no UI
- **MVP Target (Playable):** End of Phase 7 (8 weeks remaining) - with UI
- **Production Ready:** End of Phase 9 (11 weeks remaining)
- **Full Feature Set:** Phase 10+ (12+ weeks)
- **Recent Completions:** 
  - ✅ Phase 1 (C-4, C-5, C-6, C-7) - Data Infrastructure complete (2025-10-27)
  - ✅ C-1, C-2 - Domain/Core merge (2025-10-26)

**Last Updated:** October 27, 2025

