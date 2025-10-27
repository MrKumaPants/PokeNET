# PokeNET Project Status — Current

**Last Updated**: 2025-10-27
**Overall Status**: 🟡 **In Development** - Strong foundations, MVP blockers identified
**Build Status**: ✅ **Compiles** - 0 errors, 0 warnings (Domain merged into Core 2025-10-26)
**Test Status**: 🟡 **Partial** - Audio/Save 90%+, Core systems 10-50%
**Project Structure**: 7 projects (PokeNET.Domain eliminated via merge)
**Target Mechanics**: Generation VI+ (Gen 6+) Pokemon mechanics
**Eventing Standard**: Arch.EventBus (integrated with Arch ECS)

---

## Quick Links

- **📋 All Tasks**: [`ACTIONABLE_TASKS.md`](ACTIONABLE_TASKS.md) - Complete task breakdown (54 tasks, 373h remaining of 405h)
- **🏗️ Architecture**: [`ARCHITECTURE.md`](ARCHITECTURE.md) - System design and patterns
- **🧪 Testing**: [`TESTING_GUIDE.md`](TESTING_GUIDE.md) - Testing strategies
- **📚 Documentation**: [`INDEX.md`](INDEX.md) - Complete documentation index

---

## Implementation Status by Area

### ✅ **Complete & Production-Ready**

| System | Status | Test Coverage | Notes |
|--------|--------|---------------|-------|
| **Audio System** | ✅ Complete | 90%+ | DryWetMIDI v8.0.2, procedural music, reactive audio |
| **Save System** | ✅ Complete | 90%+ | **NOT WIRED** - needs DI registration |
| **Scripting Engine** | ✅ Complete | 50% | Roslyn integration, sandboxing present |
| **Modding System** | ✅ Complete | 30% | Harmony patches, mod loader, APIs defined |
| **Input System** | 🟡 Partial | 20% | Commands defined but execution TODOs |
| **ECS Core** | ✅ Complete | 10% | Arch v2.1.0 + source generators |

### 🟡 **Partially Implemented**

| System | Status | Blockers | Priority |
|--------|--------|----------|----------|
| **Rendering** | 🟡 70% | In wrong layer (Domain), needs move to Core | HIGH |
| **Battle System** | 🟡 40% | Missing type chart, move DB, damage calc | CRITICAL |
| **Pokemon Data** | ✅ 90% | All loaders complete, needs more sample data | HIGH |
| **Party Management** | 🟡 60% | Basic ops exist, no UI | HIGH |
| **Movement System** | 🟡 70% | Tile-based movement OK, collision partial | MEDIUM |

### ❌ **Missing / Stub Only**

| System | Status | Impact | Priority |
|--------|--------|--------|----------|
| **UI Systems** | ❌ Stubs | Battle UI, party screen, menus all TODO | **CRITICAL** |
| **Encounter System** | ❌ None | No wild spawning | **HIGH** |
| **Trainer AI** | ❌ None | No opponent decision-making | **HIGH** |
| **Evolution System** | ❌ None | No evolution triggers | MEDIUM |
| **Status Effects** | ❌ None | Burn/paralyze/etc not implemented | MEDIUM |
| **Weather System** | ❌ None | Weather enum exists, no gameplay | LOW |

---

## Critical Blockers (MVP)

### ~~**#1 - Phase 1: Data Infrastructure**~~ ✅ **COMPLETE**
- **Status**: RESOLVED - All Phase 1 tasks complete (C-4, C-5, C-6, C-7)
- **Delivered**: 
  - `IDataApi` interface + `DataManager` implementation
  - All JSON loaders (species, moves, items, encounters, types)
  - Data-driven type system with `types.json` (Gen 6+ including Fairy)
  - Consolidated stats (`PokemonStats` canonical, `Stats.cs` removed)
- **Tests**: 127 passing (DataManager, TypeData, TypeEffectiveness, StatCalculator)
- **Completion Date**: 2025-10-27
- **Documentation**: `docs/PHASE1_COMPLETION_REPORT.md`, `docs/architecture/Phase1-Summary.md`

### **#1 - UI Systems Are Stubs** 🔴 **CRITICAL**
- **Problem**: `MenuCommand.Execute()` is TODO (line 37), no battle UI, no party screen
- **Impact**: Player cannot interact with game
- **Files**: Need UI components in `PokeNET.Core/UI/`
- **Effort**: 5-7 days for minimal MVP UI
- **Dependencies**: Blocks gameplay loop

### **#2 - Save System Not Wired** 🟡 **HIGH**
- **Problem**: System complete but not registered in DI, no UI to trigger saves
- **Impact**: Cannot save/load games
- **Files**: `PokeNET.DesktopGL/Program.cs` (register), need save menu UI
- **Effort**: 1 day
- **Dependencies**: Blocks persistent gameplay

---

## Architecture Issues

### **Layering Violations**
- ✅ **MonoGame in Domain** [RESOLVED 2025-10-26]
  - **Status**: PokeNET.Domain merged into PokeNET.Core, eliminating layer separation issues
  - **Resolution**: Project structure simplified from 8 to 7 projects
  - **Impact**: CS0436 type conflicts from Arch.System.SourceGenerator resolved

### **API Duplication**
- ❌ **3× Duplicate APIs**: `IEventApi`, `IEntityApi`, `IAssetApi` in Domain, ModAPI, and Scripting
  - **Fix**: Unify in `PokeNET.ModAPI`, migrate events to `Arch.EventBus`
  - **Priority**: MEDIUM

### **Package Versions Inconsistent**
- ⚠️ **Version Drift**: Arch `2.1.0` vs `2.*`, Arch.Relationships `1.0.1` vs `1.0.0`
  - **Fix**: Pin all versions explicitly
  - **Priority**: MEDIUM

### **Extension Files Policy**
- ⚠️ **13 Extension Files**: Per project policy, should be rolled into owning types
  - **Files**: See audit for full list
  - **Priority**: LOW

---

## Test Coverage

| Area | Coverage | Status | Priority |
|------|----------|--------|----------|
| Audio | 90%+ | ✅ Excellent | - |
| Save System | 90%+ | ✅ Excellent | - |
| Scripting Engine | 50% | 🟡 Good | Add security tests |
| Modding Loader | 30% | 🟡 Adequate | Add API tests |
| ECS Systems | 10% | ❌ Poor | **HIGH PRIORITY** |
| Pokemon Mechanics | 0% | ❌ None | **CRITICAL** |
| UI/Menus | 0% | ❌ None | **HIGH PRIORITY** |
| Battle System | 5% | ❌ Minimal | **CRITICAL** |

**Recommendation**: Prioritize Pokemon mechanics (type effectiveness, stat calc, damage formula) and battle system tests.

**Note**: All Pokemon mechanics follow **Generation VI+ (Gen 6+)** standards, including:
- 18 types with Fairy type
- Gen 6+ damage formulas and critical hit rates
- Gen 6+ status effect mechanics
- 1/4096 shiny rate
- Mega Evolution support (future)

---

## Package Status

| Package | Version | Status | Notes |
|---------|---------|--------|-------|
| Arch | 2.1.0 / 2.* | ⚠️ Inconsistent | Pin to 2.1.0 everywhere |
| Arch.System | 1.1.0 | ✅ OK | Source generators working |
| Arch.EventBus | 1.0.2 | ✅ OK | Replace custom EventBus with this |
| Arch.Relationships | 1.0.1 / 1.0.0 | ⚠️ Mismatch | Sync to 1.0.1 |
| MonoGame | 3.8.* | ✅ OK | Wildcard acceptable |
| DryWetMIDI | 8.0.2 | ✅ OK | Latest stable |
| Roslyn Scripting | 4.14.0 | ✅ OK | Latest stable |
| MS Extensions | 9.0.10 / 9.0.* | ⚠️ Mixed | Standardize |

---

## Roadmap to MVP

> **📋 Detailed task breakdown**: See [`ACTIONABLE_TASKS.md`](ACTIONABLE_TASKS.md) for complete implementation phases

### **Phase 1: Data Infrastructure** (Week 1 - 32h) ✅ **COMPLETE**
- [x] C-4: IDataApi + DataManager implemented
- [x] C-5: JSON loaders complete (species, moves, items, encounters, types)
- [x] C-6: TypeChart refactored to data-driven types.json
- [x] C-7: Stats consolidated (Stats.cs deleted, PokemonStats canonical)

### **Phase 2: Fix Architecture** (Week 2 - 24h)
- [x] ~~C-1, C-2~~: ✅ Layering fixes completed (Domain merged into Core 2025-10-26)
- [ ] C-3: Documentation corrections
- [ ] C-8, C-9, C-10: DI registration (save, audio, scripting)
- [ ] C-11: API consolidation
- [ ] C-12: Migrate to Arch.EventBus (8h)
- [ ] C-13: Package version pinning

### **Phase 3: Core Gameplay** (Weeks 3-4 - 86h)
- [ ] H-1, H-2, H-3, H-4: Battle UI, party screen, damage calc, encounters
- [ ] H-14, H-15: Effect scripts creation & integration (28h)
- [ ] H-16, H-17, H-18, H-19, H-20: Asset loaders, system registration, ECS serialization

### **Phase 4: Pokemon Mechanics** (Week 5 - 38h)
- [ ] H-5, H-6, H-7, H-8: AI, status effects, evolution, commands
- [ ] M-1, M-2, M-3: Pokemon-specific components

**Total Estimate**: 148 hours remaining ≈ 5 weeks to playable MVP (32h complete)

**Full Production**: 373 hours remaining ≈ 10 weeks (32h complete, 405h total)

---

## Documentation Status

- ✅ **Task Tracking**: All work tracked in [`ACTIONABLE_TASKS.md`](ACTIONABLE_TASKS.md) (57 tasks)
- ✅ **Architecture Docs**: Consolidated in [`ARCHITECTURE.md`](ARCHITECTURE.md)
- ✅ **Documentation Index**: Complete index in [`INDEX.md`](INDEX.md)
- 🟡 **API Docs**: Mostly current, missing IDataApi/IUIApi
- ✅ **Historical Archive**: 103 phase reports archived in `archive/`

---

## Next Actions (Prioritized)

**✅ Phase 1 Complete** - Data infrastructure is production-ready with 127 passing tests!

**🔄 Current Focus: Phase 2 - Architecture Fixes & DI Registration**

### Immediate Next Steps
1. **C-3: Documentation corrections** (Arch.System references) - 2h
2. **C-8: Wire save system to DI** (register services) - 2h  
3. **C-9: Register audio services in DI** - 2h
4. **C-10: Register scripting context and API** - 4h
5. **C-11: Unify duplicate APIs** (consolidate to ModAPI) - 6h
6. **C-12: Migrate to Arch.EventBus** (replace custom EventBus) - 8h
7. **C-13: Pin package versions** (standardize across projects) - 2h

**Phase 2 Total**: 26 hours (1 week) → Then ready for Phase 3 (Core Gameplay)

### After Phase 2 (Phase 3 Priority)
1. **H-1: Implement minimal battle UI** (move selection, HP bars)
2. **H-2: Implement party screen UI** (view/switch Pokemon)
3. **H-3: Complete battle damage calculations** (Gen 6+ formulas)
4. **H-4: Implement encounter system** (wild Pokemon spawning)
5. **H-5: Add trainer AI** (basic decision-making)
6. **H-6: Implement status effects** (Burn, Poison, Paralysis, etc.)

---

## References

- **📋 Tasks**: [`ACTIONABLE_TASKS.md`](ACTIONABLE_TASKS.md) - All work items (single source of truth)
- **🏗️ Architecture**: [`ARCHITECTURE.md`](ARCHITECTURE.md) - System design and patterns
- **📚 Documentation**: [`INDEX.md`](INDEX.md) - Complete documentation map
- **🧪 Testing**: [`TESTING_GUIDE.md`](TESTING_GUIDE.md) - Testing strategies
- **📖 Main README**: [`README.md`](README.md) - Project overview

---

## Technical Standards

### Eventing Architecture

**All event-driven communication must use `Arch.EventBus`:**
- **Package**: `Arch.EventBus` (part of Arch ECS ecosystem)
- **Rationale**: Tight integration with Arch ECS, optimized performance, built-in features
- **Migration**: Replace custom `IEventBus`/`EventBus` implementation
- **Usage**:
  ```csharp
  // Publishing events
  world.SendEvent(new BattleStartedEvent { ... });
  
  // Subscribing to events
  world.Subscribe<BattleStartedEvent>(OnBattleStarted);
  ```
- **Benefits**:
  - Zero-allocation event dispatch
  - Event buffering and replay
  - Filtered subscriptions
  - Thread-safe by design
  - Integrated with Arch's query system

### Game State Management

**Custom `IEventApi` should wrap `Arch.EventBus`** for mod-facing APIs while maintaining internal consistency.

---

## Pokemon Mechanics Standards

**PokeNET targets Generation VI+ (Gen 6+) mechanics**, which includes:

### Battle Mechanics
- **Damage Formula**: Gen 6+ formula with updated modifiers
- **Critical Hits**: 1/16 base rate (not 1/16.67 from Gen 5)
- **Type Chart**: 18 types including Fairy (introduced Gen 6)
- **Status Effects**: Updated mechanics (e.g., Burn only halves Physical Attack, Confusion 33% self-hit)
- **Experience**: Scaled experience formula from Gen 5+

### Capture & Encounters
- **Shiny Rate**: 1/4096 (down from 1/8192 in Gen 1-5)
- **Critical Capture**: Supported (introduced Gen 5, refined Gen 6)

### Features Supported
- ✅ Fairy type and updated type chart
- ✅ Physical/Special split (Gen 4+)
- ✅ Updated stat formulas (Gen 3+, unchanged in Gen 6)
- 🔄 Mega Evolution (planned, not MVP)
- ❌ Z-Moves (not planned)
- ❌ Dynamax (not planned)

**Rationale**: Gen 6+ provides the most balanced and refined mechanics while remaining true to core Pokemon gameplay.

---

**Last Updated**: 2025-10-27  
**This status report is the single source of truth for current implementation state.**

