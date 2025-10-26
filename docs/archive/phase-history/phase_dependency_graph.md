# Arch.Extended Migration - Phase Dependency Graph

**Generated**: 2025-10-24
**Session**: swarm-1761344090995

---

## Visual Dependency Graph

```
┌─────────────────────────────────────────────────────────────────┐
│                     Phase 1: BaseSystem Migration               │
│                         ✅ COMPLETE                             │
│                                                                  │
│  - All systems use BaseSystem<World, float>                     │
│  - Custom base classes deleted                                  │
│  - 0 build errors, 0 warnings                                   │
│  - DI updated for Arch's ISystem<float>                         │
└────────┬────────────────────────────────────────────────────────┘
         │
         │ (No blocking dependencies - all phases can start)
         │
         ├───────────────────────────┬──────────────────────────────┐
         │                           │                              │
         ▼                           ▼                              ▼
┌─────────────────────┐  ┌─────────────────────────┐  ┌────────────────────────┐
│   Phase 2:          │  │   Phase 3:              │  │   Phase 4:             │
│   Source Generators │  │   Arch.Persistence      │  │   Arch.Relationships   │
│   🔄 PARALLEL       │  │   ⚠️  BLOCKED           │  │   🔄 PARALLEL          │
│                     │  │                         │  │                        │
│  Independent work:  │  │  Critical Path:         │  │  Independent work:     │
│  - Add [Query]      │  │  - Fix API errors       │  │  - Design patterns     │
│    attributes       │  │  - Research v2.0.0      │  │  - Implement PartnerOf │
│  - Verify codegen   │  │  - Update DI            │  │  - Implement InParty   │
│  - Test performance │  │  - Test save/load       │  │  - Test queries        │
│                     │  │                         │  │                        │
│  No DI changes      │  │  REQUIRES:              │  │  Soft depends on       │
│  No blockers        │  │  - API research         │  │  Phase 3 for save/load │
│  Can merge anytime  │  │  - Code fixes           │  │                        │
└─────────┬───────────┘  └──────────┬──────────────┘  └────────────┬───────────┘
          │                         │                              │
          │ (Ready to merge)        │ (Blocks integration)         │ (Develop now,
          │                         │                              │  integrate after P3)
          │                         ▼                              │
          │              ┌──────────────────────┐                  │
          │              │  Phase 3: DI Update  │                  │
          │              │  (Program.cs)        │                  │
          │              │                      │                  │
          │              │  - Register          │                  │
          │              │    WorldPersistence  │                  │
          │              │  - Remove old        │                  │
          │              │    ISaveSystem       │                  │
          │              │  - Wire up services  │                  │
          │              └──────────┬───────────┘                  │
          │                         │                              │
          │                         │ (Phase 3 complete)           │
          │                         │                              │
          └─────────────────────────┴──────────────────────────────┘
                                    │
                                    ▼
                        ┌───────────────────────────┐
                        │   Final Integration       │
                        │   (All Phases Complete)   │
                        │                           │
                        │  - Merge all branches     │
                        │  - End-to-end testing     │
                        │  - Performance benchmarks │
                        │  - Production deployment  │
                        └───────────────────────────┘
```

---

## Execution Order

### Parallel Track A: Phase 2 (No Blockers)

```
Start → Add [Query] attributes → Verify codegen → Test → Merge
        ↓                        ↓                ↓
        2 hours                  1 hour           2 hours
```

**Status**: 🟢 Can complete independently
**ETA**: 5 hours
**Risk**: Low (20%)

### Critical Path: Phase 3 (Blocks Final Integration)

```
Start → Research API → Fix code → Update DI → Test → Ready
        ↓             ↓          ↓            ↓
        4 hours       4 hours    2 hours      4 hours
```

**Status**: 🔴 Blocked by API research
**ETA**: 14 hours
**Risk**: High (60%)

### Parallel Track B: Phase 4 (Soft Dependency)

```
Start → Design patterns → Implement → Test → Wait for Phase 3 → Integrate
        ↓                 ↓            ↓                          ↓
        4 hours           8 hours      4 hours                    4 hours
```

**Status**: 🟡 Can develop, cannot integrate
**ETA**: 20 hours (16 hours dev + 4 hours integration after P3)
**Risk**: Medium (40%)

---

## Dependency Matrix

| Phase | Depends On | Blocks    | DI Changes | Build Impact | Can Merge Alone |
|-------|-----------|-----------|------------|--------------|-----------------|
| 1     | None      | All       | Yes (done) | Complete     | ✅ Done         |
| 2     | Phase 1   | None      | No         | Additive     | ✅ Yes          |
| 3     | Phase 1   | Phase 4   | Yes        | Fixes errors | ❌ Has errors   |
| 4     | Phase 1   | None      | Yes        | Additive     | ⚠️ After Phase 3 |

---

## Critical Path Timeline

### Week 1: Unblock Phase 3

**Monday**: Research Arch.Persistence 2.0.0 API
- Check official GitHub repository
- Review release notes for breaking changes
- Find migration guide
- Document correct API usage

**Tuesday**: Fix WorldPersistenceService
- Update using directives
- Replace WorldSerializer with correct API
- Fix component registration
- Verify builds (0 errors)

**Wednesday**: DI Integration
- Update Program.cs
- Register WorldPersistenceService
- Remove old save system
- Wire up services

**Thursday**: Testing & Verification
- Test save functionality
- Test load functionality
- Performance benchmarks
- Integration testing

**Friday**: Phase 2 Completion
- Add [Query] attributes (done by agent)
- Verify source generators work
- Merge to main

### Week 2: Phase 4 Integration

**Monday-Tuesday**: Relationship Integration
- Merge Phase 4 code
- Update DI registrations
- Test with Phase 3 persistence

**Wednesday-Thursday**: End-to-End Testing
- Full integration tests
- Performance profiling
- Bug fixes

**Friday**: Production Deployment
- Final verification
- Documentation
- Release

---

## Risk Mitigation Strategies

### Phase 3: API Unknown (60% probability)

**Risk**: Arch.Persistence 2.0.0 API significantly different
**Impact**: Complete rewrite of WorldPersistenceService (8-16 hours)

**Mitigation Options**:

1. **Option A: Research Official API** (Recommended)
   - Check Arch.Persistence GitHub
   - Look for v2.0.0 migration guide
   - Use correct API from source code
   - **Time**: 4 hours research + 4 hours fixes

2. **Option B: Downgrade to 1.x**
   - Revert to Arch.Persistence 1.x
   - Keep existing code
   - Research upgrade path later
   - **Time**: 2 hours

3. **Option C: Custom JSON Serialization**
   - Keep existing ISaveSystem
   - Implement custom binary format
   - Don't use Arch.Persistence yet
   - **Time**: 16 hours

**Decision**: Try Option A first (4 hour timebox), fallback to Option B

### Phase 2: SourceGen .NET 9 (20% probability)

**Risk**: Arch.System.SourceGenerator 2.1.0 doesn't support .NET 9
**Impact**: Build warnings or errors

**Mitigation**:
- Check generated files in obj/ folders
- Update to latest SourceGenerator if needed
- Fallback: Manual query optimization without codegen
- **Time**: 2 hours to investigate, 1 hour to fix

### Phase 4: Relationship Serialization (40% probability)

**Risk**: Arch.Persistence doesn't support relationship serialization
**Impact**: Need custom serialization logic

**Mitigation**:
- Research relationship save/load in Arch docs
- Implement custom serializer if needed
- Alternative: Store as components instead
- **Time**: 4 hours to implement custom serializer

---

## Build Status Tracking

### Current Build Status (Pre-Integration)

```bash
Project                   Status    Errors  Warnings
─────────────────────────────────────────────────────
PokeNET.Domain           ✅ Pass    0       0
PokeNET.Core             ❌ FAIL    2       0
  └─ WorldPersistenceService.cs line 3, 23
PokeNET.DesktopGL        ❌ FAIL    (depends on Core)
PokeNET.Tests            ⚠️  SKIP   (legacy tests)
```

### Target Build Status (After Phase 3 Fix)

```bash
Project                   Status    Errors  Warnings
─────────────────────────────────────────────────────
PokeNET.Domain           ✅ Pass    0       0
PokeNET.Core             ✅ Pass    0       0
PokeNET.DesktopGL        ✅ Pass    0       0
PokeNET.Tests            ✅ Pass    0       0
```

---

## Integration Checklist

### Phase 2 Integration Checklist

- [ ] [Query] attributes added to MovementSystem
- [ ] [Query] attributes added to BattleSystem
- [ ] [Query] attributes added to InputSystem
- [ ] [Query] attributes added to RenderSystem
- [ ] Source generator produces code (check obj/Debug/net9.0/generated/)
- [ ] Build succeeds with 0 errors
- [ ] Performance benchmarks show improvement
- [ ] Merge to main branch

### Phase 3 Integration Checklist

- [ ] Arch.Persistence 2.0.0 API researched
- [ ] WorldPersistenceService.cs updated
- [ ] Build succeeds (0 errors)
- [ ] Program.cs updated with DI registration
- [ ] Old ISaveSystem removed (if replaced)
- [ ] Save functionality tested
- [ ] Load functionality tested
- [ ] Performance benchmarks recorded
- [ ] Integration tests passing

### Phase 4 Integration Checklist

- [ ] Relationship patterns implemented (PartnerOf, InParty, HoldsItem)
- [ ] Phase 3 persistence complete (dependency)
- [ ] Relationship serialization tested
- [ ] DI registration in Program.cs
- [ ] Query performance verified
- [ ] End-to-end tests with save/load
- [ ] Documentation updated
- [ ] Merge to main branch

---

## Performance Targets

### Phase 2 Impact

- Query allocations: **-80%** (from ~500KB to ~100KB per frame)
- System update time: **-20%** (from ~10ms to ~8ms)
- Entity capacity: **+50%** (from 10K to 15K at 60 FPS)

### Phase 3 Impact

- Save time: **<500ms** for full world state
- Load time: **<1000ms** for full world state
- File size: **2-5MB** (binary format vs ~10MB JSON)

### Phase 4 Impact

- Relationship queries: **<1ms** typical
- Memory overhead: **+10%** for relationship storage
- No FPS impact with <10K relationships

---

## Conclusion

**Critical Path**: Phase 3 (Arch.Persistence fix) must complete before final integration

**Parallel Work**: Phase 2 and Phase 4 can proceed independently

**Estimated Total Time**: 5-7 working days

**Next Action**: Research Arch.Persistence 2.0.0 API (4 hour timebox)

**Success Criteria**:
- All projects build with 0 errors
- All tests pass
- Performance targets met
- Full integration verified

---

**Dependency Graph Version**: 1.0
**Last Updated**: 2025-10-24
**Status**: Active - Awaiting Phase 3 API research
