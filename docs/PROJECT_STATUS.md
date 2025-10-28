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

### **Phase 2: Architecture & DI** (Week 2 - 24h)
- [x] ~~C-1, C-2~~: ✅ Layering fixes completed (Domain merged 2025-10-26)
- [ ] C-3, C-8, C-9, C-10, C-11, C-12, C-13: DI registration, API consolidation, EventBus migration

### **Phase 3: Core Battle Mechanics** (Weeks 3-4 - 58h) 🎯 **NO UI**
- [ ] H-3: Battle damage calculations (Gen 6+)
- [ ] H-4: Encounter system
- [ ] H-5: Trainer AI
- [ ] H-6: Status effects
- [ ] H-14, H-15: Effect scripts (28h)

### **Phase 4: Pokemon Systems** (Week 5 - 30h) 🎯 **NO UI**
- [ ] H-7, H-8: Evolution, command execution
- [ ] M-1, M-2, M-3: Tile movement, battle components, trainer/party

### **Phase 5: System Integration** (Week 6 - 41h)
- [ ] H-16, H-17, H-18, H-19, H-20: Asset loaders, ECS systems
- [ ] H-9, H-10: Mouse input, command execution

### **Phase 6: Audio System** (Week 7 - 44h)
- [ ] H-11, H-12, H-13: Audio loading, reactions, track queue
- [ ] M-4, M-5, M-6: Refactor audio engine, async cleanup, procedural music

### **Phase 7: User Interface** (Weeks 8-9 - 42h) 🎨 **UI IMPLEMENTATION**
- [ ] H-1: Battle UI (16h)
- [ ] H-2: Party screen UI (12h)
- [ ] Menu systems for save/load, inventory, etc. (14h)

**Backend MVP**: 197 hours ≈ 6 weeks (Phases 2-6, no UI, fully testable)

**Playable MVP**: 239 hours ≈ 8 weeks (Through Phase 7, with UI)

**Full Production**: 373 hours ≈ 11 weeks (Through Phase 9, 405h total)

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

**🔄 Current Focus: Phase 2 - Architecture & DI (Week 2)**

### Immediate Next Steps (Phase 2 - 24h)
1. **C-3**: Documentation corrections - 2h
2. **C-8**: Wire save system to DI - 2h  
3. **C-9**: Register audio services - 2h
4. **C-10**: Register scripting context - 4h
5. **C-11**: Unify duplicate APIs - 6h
6. **C-12**: Migrate to Arch.EventBus - 8h
7. **C-13**: Pin package versions - 2h

### After Phase 2: Backend First, UI Last

**Phase 3-6 (Backend Only, No UI)** - 173 hours (6 weeks)
- ✅ Battle mechanics fully functional
- ✅ Pokemon systems complete
- ✅ Effect scripts working
- ✅ Save/load operational
- ⚠️ No UI - console/test only

**Then Phase 7 (UI Layer)** - 42 hours (2 weeks)  
- Battle UI, Party UI, Menus
- Makes everything playable

**Why This Order?**
1. Backend can be fully tested without UI
2. UI depends on backend being stable
3. Backend changes won't break UI work
4. Can parallelize UI with testing/polish

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

