# Benchmark API Mismatches Analysis

**Date:** October 24, 2025
**Analyst:** Benchmark Execution Specialist (Hive Mind Swarm)
**Status:** Awaiting test-fixer resolution

## Executive Summary

The benchmark suite has **40+ compilation errors** due to API mismatches between benchmark code and actual ECS component implementations. All errors are correctable by updating property names and method signatures to match the current codebase.

**Impact:** Blocking all benchmark execution until fixed
**Severity:** High (prevents performance validation)
**Estimated Fix Time:** 30-45 minutes with systematic corrections

---

## Category 1: Component Property Name Mismatches

### 1.1 GridPosition Component

**Benchmark Code (Incorrect):**
```csharp
entity.Set(new GridPosition { X = i, Y = j });
pos.X = (pos.X + 1) % 1000;
pos.Y = (pos.Y + 1) % 1000;
```

**Actual API (Correct):**
```csharp
entity.Set(new GridPosition(tileX: i, tileY: j));
pos.TileX = (pos.TileX + 1) % 1000;
pos.TileY = (pos.TileY + 1) % 1000;
```

**Fix Required:** Replace all `X` → `TileX`, `Y` → `TileY`

**Files Affected:**
- `MemoryAllocationBenchmarks.cs` (lines 38, 67-68)
- `QueryAllocationBenchmarks.cs` (lines 33, 80)
- `SaveLoadBenchmarks.cs` (lines 68, 81, 87)
- `RelationshipQueryBenchmarks.cs` (line 80)

---

### 1.2 MovementState Component

**Benchmark Code (Incorrect):**
```csharp
entity.Set(new MovementState { Speed = 5.0f });
movement.Speed += 0.01f;
```

**Actual API (Correct):**
```csharp
entity.Set(new MovementState { MovementSpeed = 5.0f });
movement.MovementSpeed += 0.01f;
```

**Fix Required:** Replace all `Speed` → `MovementSpeed`

**Files Affected:**
- `MemoryAllocationBenchmarks.cs` (lines 39, 69)
- `QueryAllocationBenchmarks.cs` (line 34)

---

### 1.3 Renderable Component

**Benchmark Code (Incorrect):**
```csharp
entity.Set(new Renderable { IsVisible = true, ZIndex = 1 });
new Renderable { ZIndex = i % 10 }
```

**Actual API (Correct):**
```csharp
entity.Set(new Renderable(isVisible: true, alpha: 1.0f));
new Renderable(isVisible: true, alpha: (float)(i % 10) / 10f)
```

**Fix Required:** Remove `ZIndex` property (doesn't exist), use constructor parameters

**Files Affected:**
- `MemoryAllocationBenchmarks.cs` (line 40)
- `QueryAllocationBenchmarks.cs` (line 35)
- `SaveLoadBenchmarks.cs` (line 89)

---

### 1.4 PokemonStats Component

**Benchmark Code (Incorrect):**
```csharp
new PokemonStats { SpecialAttack = 100, SpecialDefense = 90 }
```

**Actual API (Correct):**
```csharp
new PokemonStats { SpAttack = 100, SpDefense = 90 }
```

**Fix Required:** Replace `SpecialAttack` → `SpAttack`, `SpecialDefense` → `SpDefense`

**Files Affected:**
- `RelationshipQueryBenchmarks.cs` (line 60)
- `SaveLoadBenchmarks.cs` (line 79)

---

### 1.5 PokemonData Component

**Benchmark Code (Incorrect):**
```csharp
new PokemonData { UniqueId = pokemonGuid }
pokemon.UniqueId
```

**Actual API (Correct):**
```csharp
// UniqueId doesn't exist - use Entity.Id directly
// Or use SpeciesId for species identification
new PokemonData { SpeciesId = 25 }  // Pikachu
```

**Fix Required:** Remove `UniqueId` references, use Entity GUID for identification

**Files Affected:**
- `RelationshipQueryBenchmarks.cs` (lines 53, 79)

---

## Category 2: Component Collection Property Mismatches

### 2.1 Party Component

**Benchmark Code (Incorrect):**
```csharp
ref var party = ref trainer.Get<Party>();
foreach (var pokemonGuid in party.PartyMembers) { }
party.PartyMembers.Add(pokemonGuid);
```

**Actual API (Correct):**
```csharp
// Party is a class, not a struct - no ref
var party = trainer.Get<Party>();
foreach (var pokemonGuid in party.GetAllPokemon()) { }
party.AddPokemon(pokemonGuid);
```

**Fix Required:**
- Remove `ref` from Party access
- Replace `PartyMembers` property with `GetAllPokemon()` method
- Use `AddPokemon(guid)` instead of collection manipulation

**Files Affected:**
- `RelationshipQueryBenchmarks.cs` (line 71)
- `SaveLoadBenchmarks.cs` (line 67)

---

### 2.2 Inventory Component

**Benchmark Code (Incorrect):**
```csharp
new Inventory { Items = new List<int> { 1, 2, 3 } }
```

**Actual API (Correct):**
```csharp
// Need to check actual Inventory implementation
// Likely uses methods like AddItem(itemId, quantity)
```

**Fix Required:** Update to use Inventory API methods

**Files Affected:**
- `SaveLoadBenchmarks.cs` (line 69)

---

### 2.3 MoveSet Component

**Benchmark Code (Incorrect):**
```csharp
new MoveSet { Moves = new[] { 1, 2, 3, 4 } }
```

**Actual API (Correct):**
```csharp
// Need to check actual MoveSet implementation
// Likely uses methods or specific properties
```

**Fix Required:** Update to use MoveSet API

**Files Affected:**
- `SaveLoadBenchmarks.cs` (line 82)

---

## Category 3: Constructor Signature Mismatches

### 3.1 Trainer Constructor

**Benchmark Code (Incorrect):**
```csharp
new Trainer { Name = "Test" }
```

**Actual API (Correct):**
```csharp
new Trainer(
    trainerId: Guid.NewGuid(),
    trainerName: "Test",
    trainerClass: "Pokemon Trainer",
    isPlayer: true,
    gender: TrainerGender.Male
)
```

**Fix Required:** Use full constructor with all required parameters

**Files Affected:**
- `RelationshipQueryBenchmarks.cs` (line 39)
- `SaveLoadBenchmarks.cs` (line 66)

---

### 3.2 CommandBuffer Constructor

**Benchmark Code (Incorrect):**
```csharp
_commandBuffer = new CommandBuffer(_world);
```

**Actual API (Correct):**
```csharp
_commandBuffer = new CommandBuffer();
// Must use world.GetCommandBuffer() or similar
```

**Fix Required:** Check Arch.Core.CommandBuffer API for correct instantiation

**Files Affected:**
- `MemoryAllocationBenchmarks.cs` (line 31)

---

## Category 4: Entity Extension Method Mismatches

### 4.1 Entity.Has<T>() Method

**Benchmark Code:**
```csharp
if (pokemon.Has<PokemonStats>()) count++;
```

**Status:** Method might not exist on Entity type directly

**Fix Required:**
```csharp
// Option 1: Use world.Has<T>(entity)
if (_world.Has<PokemonStats>(in pokemon)) count++;

// Option 2: Import extension methods
using Arch.Core.Extensions;
if (pokemon.Has<PokemonStats>()) count++;
```

**Files Affected:**
- `RelationshipQueryBenchmarks.cs` (lines 104, 128)

---

### 4.2 Entity.Get<T>() Method

**Benchmark Code:**
```csharp
ref var party = ref trainer.Get<Party>();
```

**Status:** Method might not exist or return reference

**Fix Required:**
```csharp
// Use world.Get<T>(entity)
var party = _world.Get<Party>(trainer);
```

**Files Affected:**
- `RelationshipQueryBenchmarks.cs` (line 122)

---

### 4.3 Entity.Set<T0, T1>() Inference

**Benchmark Code (Incorrect):**
```csharp
entity.Set(gridPosition, movementState, renderable);
```

**Actual API (Correct):**
```csharp
entity.Set<GridPosition, MovementState, Renderable>(
    in gridPosition,
    in movementState,
    in renderable
);
```

**Fix Required:** Explicitly specify generic type parameters

**Files Affected:**
- `MemoryAllocationBenchmarks.cs` (line 41)
- `QueryAllocationBenchmarks.cs` (line 36)
- `SaveLoadBenchmarks.cs` (lines 74, 88)

---

## Category 5: Relationship API Usage

### 5.1 GetRelationships<T>() Usage

**Benchmark Code:**
```csharp
foreach (var pokemon in _testTrainer.GetRelationships<OwnedBy>())
{
    if (pokemon.Has<PokemonStats>()) count++;
}
```

**Status:** Unclear if GetRelationships returns Entities or KeyValuePairs

**Fix Required:** Check Arch.Relationships API documentation

**Files Affected:**
- `RelationshipQueryBenchmarks.cs` (line 104)

---

## Summary of Required Fixes

| Category | Files Affected | Error Count | Priority |
|----------|----------------|-------------|----------|
| GridPosition (X/Y → TileX/TileY) | 4 | 12 | High |
| MovementState (Speed → MovementSpeed) | 2 | 4 | High |
| Renderable (ZIndex removal) | 3 | 6 | High |
| PokemonStats (Special* → Sp*) | 2 | 4 | High |
| PokemonData (UniqueId removal) | 1 | 2 | High |
| Party API (methods vs properties) | 2 | 3 | High |
| Trainer constructor | 2 | 2 | High |
| CommandBuffer constructor | 1 | 1 | Medium |
| Entity extension methods | 3 | 8 | Medium |
| Inventory/MoveSet API | 1 | 2 | Low |

**Total Errors:** ~44 compilation errors
**Systematic Fix:** Update all benchmarks to match current ECS API

---

## Recommended Action Plan

### Phase 1: Component Property Updates (30 min)
1. Search/replace `X` → `TileX`, `Y` → `TileY` in benchmark files
2. Search/replace `Speed` → `MovementSpeed`
3. Search/replace `SpecialAttack` → `SpAttack`, `SpecialDefense` → `SpDefense`
4. Remove all `ZIndex` references, use Renderable constructor
5. Remove all `UniqueId` references from PokemonData

### Phase 2: Constructor/Method Signature Updates (15 min)
1. Update Trainer instantiation to use full constructor
2. Fix CommandBuffer instantiation
3. Update Party access to use methods instead of properties
4. Fix Entity.Set<T0, T1>() type inference issues

### Phase 3: Extension Method Imports (5 min)
1. Add `using Arch.Core.Extensions;` to all benchmark files
2. Verify Entity.Has<T>() and Entity.Get<T>() work correctly
3. Update relationship API usage if needed

### Phase 4: Verification (5 min)
1. Build benchmarks: `dotnet build benchmarks/PokeNET.Benchmarks.csproj -c Release`
2. Verify 0 compilation errors
3. Run quick smoke test: `dotnet run -c Release --project benchmarks/PokeNET.Benchmarks.csproj --job short --filter "*Query*"`

---

## Test-Fixer Agent Guidance

**Priority Files to Fix (in order):**
1. `MemoryAllocationBenchmarks.cs` - 15 errors
2. `QueryAllocationBenchmarks.cs` - 8 errors
3. `SaveLoadBenchmarks.cs` - 12 errors
4. `RelationshipQueryBenchmarks.cs` - 9 errors

**Verification Command:**
```bash
dotnet build benchmarks/PokeNET.Benchmarks.csproj -c Release 2>&1 | tee benchmark-build.log
```

**Success Criteria:**
- 0 compilation errors
- 0 warnings (ideally, XML comment warnings acceptable)
- Ready for benchmark execution

---

## Post-Fix: Benchmark Execution Plan

Once fixed, execute in this order:

1. **Quick Validation** (5 min):
   ```bash
   dotnet run -c Release --job short --filter "*CachedStaticQuery"
   ```

2. **Query Allocations** (15 min):
   ```bash
   dotnet run -c Release --filter "*QueryAllocation*"
   ```

3. **Relationship Queries** (10 min):
   ```bash
   dotnet run -c Release --filter "*Relationship*"
   ```

4. **Save/Load Performance** (30 min):
   ```bash
   dotnet run -c Release --filter "*SaveLoad*"
   ```

5. **Memory Allocation Suite** (45 min):
   ```bash
   dotnet run -c Release --filter "*MemoryAllocation*"
   ```

**Total Execution Time:** ~90 minutes for full suite

---

**Status:** Ready for test-fixer to begin systematic corrections
**Next Step:** Wait for test-fixer completion, then execute benchmarks
**Coordination:** Memory key `swarm/benchmarks/api-analysis` updated
