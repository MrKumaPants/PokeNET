# PokeNET Performance Benchmarks - Quick Reference

## Current Status: ⏸️ AWAITING TEST-FIXER

**Last Updated:** October 24, 2025 01:07 UTC
**Phase:** Pre-execution (API analysis complete)
**Blocker:** 44 compilation errors in benchmark code
**Next Step:** Test-fixer agent must correct API mismatches

---

## Quick Links

- **Status Report:** [benchmark-execution-status.md](./benchmark-execution-status.md)
- **API Mismatches:** [benchmark-api-mismatches.md](./benchmark-api-mismatches.md)
- **Performance Claims:** [phase5-performance-report.md](./phase5-performance-report.md)

---

## For Test-Fixer Agent

### Priority Files (Fix in This Order)

1. **MemoryAllocationBenchmarks.cs** - 15 errors
2. **SaveLoadBenchmarks.cs** - 12 errors
3. **RelationshipQueryBenchmarks.cs** - 9 errors
4. **QueryAllocationBenchmarks.cs** - 8 errors

### Quick Fix Guide

```bash
# Navigate to benchmark directory
cd /mnt/c/Users/nate0/RiderProjects/PokeNET/benchmarks

# Apply systematic fixes (see benchmark-api-mismatches.md for details):
# 1. GridPosition: X → TileX, Y → TileY
# 2. MovementState: Speed → MovementSpeed
# 3. Renderable: Remove ZIndex, use constructor
# 4. PokemonStats: SpecialAttack → SpAttack
# 5. Party: Use methods instead of properties
# 6. Add: using Arch.Core.Extensions;

# Verify after each file:
dotnet build ../benchmarks/PokeNET.Benchmarks.csproj -c Release

# Final verification:
dotnet build ../benchmarks/PokeNET.Benchmarks.csproj -c Release 2>&1 | grep "Build succeeded"
```

**Estimated Fix Time:** 30-45 minutes

---

## For Benchmark Execution Specialist (Post-Fix)

### Execution Sequence

```bash
cd /mnt/c/Users/nate0/RiderProjects/PokeNET

# 1. Smoke test (5 min)
dotnet run -c Release --project benchmarks/PokeNET.Benchmarks.csproj \
  --job short --filter "*CachedStaticQuery"

# 2. Query allocations (15 min)
dotnet run -c Release --project benchmarks/PokeNET.Benchmarks.csproj \
  --filter "*QueryAllocation*"

# 3. Relationship queries (10 min)
dotnet run -c Release --project benchmarks/PokeNET.Benchmarks.csproj \
  --filter "*Relationship*"

# 4. Save/load performance (30 min)
dotnet run -c Release --project benchmarks/PokeNET.Benchmarks.csproj \
  --filter "*SaveLoad*"

# 5. Memory allocations (45 min)
dotnet run -c Release --project benchmarks/PokeNET.Benchmarks.csproj \
  --filter "*MemoryAllocation*"

# 6. Full suite (90 min)
dotnet run -c Release --project benchmarks/PokeNET.Benchmarks.csproj
```

**Total Runtime:** ~90 minutes for complete suite

---

## Performance Claims to Validate

| Claim | Target | Benchmark | Expected Result | Confidence |
|-------|--------|-----------|-----------------|------------|
| Query allocations | 0 bytes/frame | QueryAllocationBenchmarks | 100% reduction | 90% |
| Save/load speed | 3-5x faster | SaveLoadBenchmarks | 3.3x speedup | 85% |
| Relationship queries | <1ms | RelationshipQueryBenchmarks | <1μs (0.8μs) | 95% |
| GC reduction | 50-70% | MemoryAllocationBenchmarks | 67% Gen0, 75% Gen1 | 80% |

---

## Success Criteria

### Build
- ✅ 0 compilation errors
- ✅ 0 warnings (XML warnings OK)

### Execution
- ✅ All 19 benchmarks run successfully
- ✅ No runtime exceptions
- ✅ Complete BenchmarkDotNet reports generated

### Validation
- ✅ Query allocations: 0 bytes ±0
- ✅ Save/load: 3-5x ±10%
- ✅ Relationships: <1ms (target <1μs)
- ✅ GC reduction: 50-70% ±5%

---

## Output Artifacts

After execution, find results in:
```
BenchmarkDotNet.Artifacts/
├── results/
│   ├── *.html           # Interactive HTML reports
│   ├── *.csv            # Raw data for analysis
│   └── *.md             # Markdown summaries
└── logs/
    └── *-*.log          # Detailed execution logs
```

Copy results to docs:
```bash
cp BenchmarkDotNet.Artifacts/results/*.md docs/phase6-performance-validation.md
```

---

## Coordination Keys

Swarm memory keys for coordination:
- `swarm/benchmarks/api-analysis` - API mismatch analysis
- `swarm/benchmarks/build-status` - Build success/failure
- `swarm/benchmarks/results/query-allocations` - Query results
- `swarm/benchmarks/results/save-load` - Serialization results
- `swarm/benchmarks/results/relationships` - Relationship results
- `swarm/benchmarks/results/memory-allocations` - GC results
- `swarm/benchmarks/final-report` - Final validation report

---

## Timeline

| Phase | Duration | Status |
|-------|----------|--------|
| API Analysis | 25 min | ✅ Complete |
| Test Fixes | 45 min | ⏳ Pending |
| Smoke Test | 5 min | 🔴 Blocked |
| Benchmarks | 90 min | 🔴 Blocked |
| Report | 10 min | 🔴 Blocked |
| **Total** | **~3 hours** | - |

---

## Contact

**Primary Agent:** Benchmark Execution Specialist
**Swarm:** Hive Mind (swarm_1761354128168_prcyadna7)
**Queen:** Seraphina
**Priority:** HIGH (v1.0 release blocker)

---

**Status:** Analysis complete, ready to execute upon test-fixer completion
**Confidence:** 90% all claims will validate
**ETA:** Benchmarks can start within 5 minutes of successful build
