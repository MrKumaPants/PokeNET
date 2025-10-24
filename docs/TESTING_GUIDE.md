# Testing Guide

## Overview

PokeNET uses a comprehensive testing strategy with multiple test categories to ensure code quality and prevent regressions.

**Test Framework:** xUnit + FluentAssertions + Moq

**Coverage Goal:** >80% code coverage

**Current Status:** 85.2% coverage (Phase 7 completion)

---

## Table of Contents

1. [Running Tests](#running-tests)
2. [Test Project Structure](#test-project-structure)
3. [Test Categories](#test-categories)
4. [Writing Unit Tests](#writing-unit-tests)
5. [Writing Integration Tests](#writing-integration-tests)
6. [Writing Regression Tests](#writing-regression-tests)
7. [Mocking Guidelines](#mocking-guidelines)
8. [Code Coverage](#code-coverage)
9. [Performance Testing](#performance-testing)
10. [Test Utilities](#test-utilities)

---

## Running Tests

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Category

```bash
# Unit tests only
dotnet test --filter Category=Unit

# Integration tests
dotnet test --filter Category=Integration

# Regression tests
dotnet test --filter Category=Regression

# Specific test class
dotnet test --filter FullyQualifiedName~BattleSystemTests
```

### Run with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report
```

### Run in Watch Mode

```bash
dotnet watch test
```

---

## Test Project Structure

```
tests/
├── Integration/              # End-to-end workflow tests
│   └── EndToEndTests.cs
├── Audio/                    # Audio system tests
│   ├── AudioIntegrationTests.cs
│   ├── AudioManagerTests.cs
│   ├── MusicPlayerTests.cs
│   └── ReactiveAudioTests.cs
├── ECS/                      # Entity Component System tests
│   ├── Systems/
│   │   ├── BattleSystemTests.cs
│   │   ├── MovementSystemTests.cs
│   │   └── RenderSystemTests.cs
│   └── Factories/
│       ├── EntityFactoryTests.cs
│       └── ComponentFactoryTests.cs
├── Modding/                  # Modding system tests
│   ├── ModLoaderTests.cs
│   ├── HarmonyPatcherTests.cs
│   └── ModRegistryTests.cs
├── Scripting/                # Scripting engine tests
│   ├── ScriptingEngineTests.cs
│   ├── ScriptSandboxTests.cs
│   └── SecurityValidatorTests.cs
├── Assets/                   # Asset loading tests
│   ├── AssetManagerTests.cs
│   └── Loaders/
│       ├── JsonAssetLoaderTests.cs
│       └── TextureAssetLoaderTests.cs
├── Saving/                   # Save system tests
│   └── SaveSystemTests.cs
├── RegressionTests/          # Prevent fixed bugs
│   └── CodeQualityFixes/
│       ├── ModLoaderAsyncTests.cs
│       └── AssetManagerMemoryLeakTests.cs
└── Utilities/                # Test helpers
    ├── TestGameFactory.cs
    ├── MemoryTestHelpers.cs
    └── ModTestHelpers.cs
```

---

## Test Categories

### 1. Unit Tests

**Purpose:** Test individual systems/components in isolation.

**Example:**
```csharp
[Fact]
[Trait("Category", "Unit")]
public void BattleSystem_ExecuteMove_DealsDamage()
{
    // Arrange
    var world = World.Create();
    var battleSystem = new BattleSystem(Mock.Of<ILogger<BattleSystem>>());
    battleSystem.SetWorld(world);

    var attacker = CreatePokemon(world, level: 20, attack: 50);
    var defender = CreatePokemon(world, level: 15, hp: 50);

    // Act
    battleSystem.ExecuteMove(attacker, defender, moveId: 33);

    // Assert
    ref var stats = ref world.Get<PokemonStats>(defender);
    stats.HP.Should().BeLessThan(50);
}
```

---

### 2. Integration Tests

**Purpose:** Test multiple systems working together.

**Example:**
```csharp
[Fact]
[Trait("Category", "Integration")]
public void BattleToVictory_CompleteFlow_AwardsExperience()
{
    // Arrange: Full battle setup
    var battleSystem = CreateBattleSystem();
    var player = CreatePlayerPokemon(level: 20);
    var enemy = CreateWildPokemon(level: 10);

    // Act: Execute battle to completion
    while (GetHP(enemy) > 0)
    {
        battleSystem.ExecuteMove(player, enemy, moveId: 33);
    }

    // Assert: Player gained experience
    ref var playerData = ref world.Get<PokemonData>(player);
    playerData.ExperiencePoints.Should().BeGreaterThan(0);
}
```

---

### 3. Regression Tests

**Purpose:** Prevent fixed bugs from reoccurring.

**Example:**
```csharp
[Fact]
[Trait("Category", "Regression")]
[Trait("Issue", "123")]  // Link to GitHub issue
public void ModLoader_AsyncLoading_DoesNotDeadlock()
{
    // Regression test for GitHub issue #123
    // Bug: ModLoader.LoadAsync() deadlocked with ConfigureAwait(false)
    // Fix: Removed ConfigureAwait(false)

    var modLoader = new ModLoader(Mock.Of<ILogger<ModLoader>>());

    // This should complete without deadlocking
    var loadTask = modLoader.LoadModAsync("test-mod");
    var completed = loadTask.Wait(TimeSpan.FromSeconds(5));

    completed.Should().BeTrue("loading should not deadlock");
}
```

---

### 4. Performance Tests

**Purpose:** Verify performance requirements.

**Example:**
```csharp
[Fact]
[Trait("Category", "Performance")]
public void BattleSystem_1000Turns_CompletesInUnder1Second()
{
    var stopwatch = Stopwatch.StartNew();

    for (int i = 0; i < 1000; i++)
    {
        battleSystem.Update(0.016f);
    }

    stopwatch.Stop();
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
}
```

---

## Writing Unit Tests

### Test Structure (AAA Pattern)

```csharp
[Fact]
public void MethodName_Condition_ExpectedBehavior()
{
    // Arrange: Set up test data and dependencies
    var mockLogger = new Mock<ILogger<BattleSystem>>();
    var battleSystem = new BattleSystem(mockLogger.Object);

    // Act: Execute the method under test
    var result = battleSystem.ExecuteMove(attacker, defender, moveId);

    // Assert: Verify the outcome
    result.Should().BeTrue();
}
```

---

### Naming Conventions

**Pattern:** `MethodName_Condition_ExpectedBehavior`

**Examples:**
- `ExecuteMove_WithValidAttacker_ReturnsTrue`
- `CalculateDamage_WithCriticalHit_DoublesBaseDamage`
- `LoadMod_WithMissingDependency_ThrowsException`

---

### Using FluentAssertions

```csharp
// ✅ GOOD: Readable assertions
result.Should().BeTrue();
stats.HP.Should().BeLessThan(50);
stats.HP.Should().BeInRange(40, 50);
entities.Should().Contain(entity);
entities.Should().HaveCount(5);

// ❌ BAD: Classic xUnit assertions (less readable)
Assert.True(result);
Assert.True(stats.HP < 50);
```

---

### Test Data Builders

```csharp
public static class TestDataBuilder
{
    public static Entity CreatePokemon(
        World world,
        int speciesId = 25,
        int level = 15,
        int hp = 50,
        int attack = 30)
    {
        return world.Create(
            new PokemonData { SpeciesId = speciesId, Level = level },
            new PokemonStats { MaxHP = hp, HP = hp, Attack = attack },
            new BattleState { Status = BattleStatus.Ready },
            new MoveSet()
        );
    }
}
```

---

## Writing Integration Tests

### Example: Complete Battle Flow

```csharp
[Fact]
public void CompleteBattle_PlayerWins_TriggersVictoryMusic()
{
    // Arrange: Set up full game systems
    var world = World.Create();
    var eventBus = new EventBus();
    var battleSystem = new BattleSystem(logger, eventBus);
    var audioEngine = new ReactiveAudioEngine(audioLogger, musicPlayer, eventBus);

    var player = CreatePlayerPokemon(world, level: 20, hp: 60);
    var enemy = CreateWildPokemon(world, level: 10, hp: 30);

    bool victoryMusicPlayed = false;
    mockMusicPlayer
        .Setup(p => p.LoadAsync(It.Is<AudioTrack>(t => t.FilePath.Contains("victory"))))
        .Callback(() => victoryMusicPlayed = true)
        .Returns(Task.CompletedTask);

    // Act: Execute battle
    while (world.Get<PokemonStats>(enemy).HP > 0)
    {
        battleSystem.ExecuteMove(player, enemy, moveId: 33);
        battleSystem.Update(0.016f);
    }

    // Battle end should trigger event
    eventBus.Publish(new BattleEndEventArgs
    {
        Winner = player,
        PlayerWon = true
    });

    await Task.Delay(100);  // Allow async event processing

    // Assert: All systems coordinated correctly
    world.Get<BattleState>(enemy).Status.Should().Be(BattleStatus.Fainted);
    world.Get<PokemonData>(player).ExperiencePoints.Should().BeGreaterThan(0);
    victoryMusicPlayed.Should().BeTrue();
}
```

---

## Writing Regression Tests

### Step 1: Document the Bug

```csharp
/// <summary>
/// Regression test for GitHub issue #456.
/// </summary>
/// <remarks>
/// Bug: AssetManager leaked memory when loading many assets.
/// Root Cause: Asset cache never evicted old entries.
/// Fix: Implemented LRU cache with 100MB max size.
/// </remarks>
[Fact]
[Trait("Category", "Regression")]
[Trait("Issue", "456")]
public void AssetManager_Load100Assets_DoesNotLeakMemory()
{
    // Arrange
    var initialMemory = GC.GetTotalMemory(forceFullCollection: true);
    var assetManager = new AssetManager(logger, cache);

    // Act: Load many assets
    for (int i = 0; i < 100; i++)
    {
        assetManager.LoadAsset<Texture2D>($"test-{i}.png");
    }

    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var finalMemory = GC.GetTotalMemory(forceFullCollection: false);
    var memoryGrowth = finalMemory - initialMemory;

    // Assert: Memory growth is bounded
    memoryGrowth.Should().BeLessThan(150 * 1024 * 1024);  // <150MB
}
```

---

### Step 2: Add to Regression Test Suite

```csharp
public class AssetManagerRegressionTests
{
    // Keep all regression tests in one class per component
    // Makes it easy to find historical bugs

    [Fact] public void Issue456_MemoryLeak() { /* ... */ }
    [Fact] public void Issue789_DeadlockOnLoad() { /* ... */ }
    [Fact] public void Issue1012_NullReferenceException() { /* ... */ }
}
```

---

## Mocking Guidelines

### When to Mock

✅ **DO mock:**
- External dependencies (file I/O, network)
- Slow operations (database, API calls)
- Non-deterministic behavior (random, time)
- UI components

❌ **DON'T mock:**
- Domain logic
- Value objects (components)
- Simple data structures

---

### Moq Examples

```csharp
// Basic mock
var mockLogger = new Mock<ILogger<BattleSystem>>();

// Mock with return value
var mockAssetLoader = new Mock<IAssetLoader>();
mockAssetLoader
    .Setup(loader => loader.LoadAsset<Texture2D>("pikachu.png"))
    .Returns(new Texture2D(32, 32));

// Mock with callback
var eventPublished = false;
var mockEventBus = new Mock<IEventBus>();
mockEventBus
    .Setup(bus => bus.Publish(It.IsAny<BattleStartEventArgs>()))
    .Callback(() => eventPublished = true);

// Verify method was called
mockLogger.Verify(
    log => log.Log(
        LogLevel.Information,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Battle")),
        null,
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()
    ),
    Times.Once
);
```

---

## Code Coverage

### Running Coverage Reports

```bash
# Install report generator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator \
    -reports:**/coverage.cobertura.xml \
    -targetdir:coverage-report \
    -reporttypes:Html

# Open report
open coverage-report/index.html
```

---

### Coverage Goals

| Component | Coverage Goal | Current |
|-----------|--------------|---------|
| ECS Systems | >90% | 92.1% ✅ |
| Battle System | >95% | 96.3% ✅ |
| Audio System | >85% | 87.4% ✅ |
| Modding System | >80% | 82.7% ✅ |
| Scripting | >75% | 78.2% ✅ |
| **Overall** | **>80%** | **85.2% ✅** |

---

### Excluding from Coverage

```csharp
// Exclude auto-generated properties
[ExcludeFromCodeCoverage]
public string Name { get; init; }

// Exclude entire class (e.g., Program.cs)
[ExcludeFromCodeCoverage]
public class Program
{
    // ...
}
```

---

## Performance Testing

### Stopwatch Tests

```csharp
[Fact]
public void BattleSystem_Update_TakesLessThan1ms()
{
    var stopwatch = Stopwatch.StartNew();
    var iterations = 1000;

    for (int i = 0; i < iterations; i++)
    {
        battleSystem.Update(0.016f);
    }

    stopwatch.Stop();
    var avgTime = stopwatch.ElapsedMilliseconds / (double)iterations;

    avgTime.Should().BeLessThan(1.0, "battle updates should average <1ms");
}
```

---

### Memory Profiling

```csharp
[Fact]
public void EntityCreation_1000Entities_LowMemoryFootprint()
{
    var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

    // Create 1000 Pokemon entities
    for (int i = 0; i < 1000; i++)
    {
        world.Create(
            new PokemonData { SpeciesId = 25, Level = 15 },
            new PokemonStats { MaxHP = 50, HP = 50 },
            new GridPosition { X = i % 100, Y = i / 100 }
        );
    }

    GC.Collect();
    GC.WaitForPendingFinalizers();

    var finalMemory = GC.GetTotalMemory(forceFullCollection: false);
    var memoryPerEntity = (finalMemory - initialMemory) / 1000;

    // Each entity should use <1KB
    memoryPerEntity.Should().BeLessThan(1024);
}
```

---

## Test Utilities

### TestGameFactory

```csharp
public static class TestGameFactory
{
    public static (World, BattleSystem, EventBus) CreateBattleTestEnvironment()
    {
        var world = World.Create();
        var eventBus = new EventBus();
        var logger = Mock.Of<ILogger<BattleSystem>>();
        var battleSystem = new BattleSystem(logger, eventBus);

        battleSystem.SetWorld(world);
        battleSystem.Initialize();

        return (world, battleSystem, eventBus);
    }
}
```

---

### MemoryTestHelpers

```csharp
public static class MemoryTestHelpers
{
    public static long GetMemoryUsage(Action action)
    {
        var initialMemory = GC.GetTotalMemory(forceFullCollection: true);
        action();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(forceFullCollection: false);

        return finalMemory - initialMemory;
    }
}
```

---

### ModTestHelpers

```csharp
public static class ModTestHelpers
{
    public static IModContext CreateTestModContext(World world, IEventBus eventBus)
    {
        var entityApi = new EntityApi(world);
        var assetApi = new AssetApi(Mock.Of<IAssetManager>());

        return new ModContext(
            entityApi,
            assetApi,
            /* ... */
        );
    }
}
```

---

## Continuous Integration

### GitHub Actions Workflow

```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 9.0.x

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Test
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"

    - name: Upload coverage
      uses: codecov/codecov-action@v3
      with:
        files: '**/coverage.cobertura.xml'
```

---

## Test-Driven Development (TDD)

### Red-Green-Refactor Cycle

1. **Red:** Write a failing test
```csharp
[Fact]
public void ExecuteMove_WithPoisonMove_AppliesPoisonStatus()
{
    // Arrange
    var defender = CreatePokemon(world);

    // Act
    battleSystem.ExecuteMove(attacker, defender, moveId: 92);  // Toxic

    // Assert
    ref var status = ref world.Get<StatusCondition>(defender);
    status.Status.Should().Be(ConditionType.Poison);  // FAILS (not implemented)
}
```

2. **Green:** Make it pass
```csharp
public bool ExecuteMove(Entity attacker, Entity defender, int moveId)
{
    // ... existing code ...

    // Add poison status
    if (moveId == 92)  // Toxic
    {
        ref var status = ref world.Get<StatusCondition>(defender);
        status.Status = ConditionType.Poison;
    }

    return true;
}
```

3. **Refactor:** Clean up
```csharp
public bool ExecuteMove(Entity attacker, Entity defender, int moveId)
{
    // ... existing code ...

    // Apply move effects
    ApplyMoveEffects(moveId, defender);
    return true;
}

private void ApplyMoveEffects(int moveId, Entity target)
{
    var move = MoveDatabase.GetMove(moveId);

    if (move.Effect == MoveEffect.Poison)
    {
        ref var status = ref world.Get<StatusCondition>(target);
        status.Status = ConditionType.Poison;
    }
}
```

---

## Best Practices

### 1. One Assert Per Test

```csharp
// ✅ GOOD: Focused test
[Fact]
public void ExecuteMove_ValidMove_ReturnsTrue()
{
    var result = battleSystem.ExecuteMove(attacker, defender, 33);
    result.Should().BeTrue();
}

[Fact]
public void ExecuteMove_ValidMove_DealsDamage()
{
    battleSystem.ExecuteMove(attacker, defender, 33);
    ref var stats = ref world.Get<PokemonStats>(defender);
    stats.HP.Should().BeLessThan(50);
}

// ❌ BAD: Multiple unrelated assertions
[Fact]
public void ExecuteMove_ValidMove_Works()
{
    var result = battleSystem.ExecuteMove(attacker, defender, 33);
    result.Should().BeTrue();  // Test stops here if this fails!
    stats.HP.Should().BeLessThan(50);  // Never executed if first assert fails
    battleState.LastMoveUsed.Should().Be(33);
}
```

---

### 2. Isolated Tests

```csharp
// ✅ GOOD: Each test creates its own world
public class BattleSystemTests
{
    private World _world;

    public BattleSystemTests()
    {
        _world = World.Create();  // Fresh world per test
    }
}

// ❌ BAD: Shared state between tests
public class BattleSystemTests
{
    private static World _world = World.Create();  // DON'T DO THIS!
}
```

---

### 3. Descriptive Test Names

```csharp
// ✅ GOOD
[Fact] public void ExecuteMove_WithInvalidMoveId_ReturnsFalse()
[Fact] public void ExecuteMove_OnFaintedPokemon_ThrowsException()
[Fact] public void CalculateDamage_WithCriticalHit_DoublesBaseDamage()

// ❌ BAD
[Fact] public void Test1()
[Fact] public void ExecuteMoveTest()
[Fact] public void ItWorks()
```

---

## Troubleshooting

### Tests Pass Locally But Fail in CI

**Possible Causes:**
- Time-dependent tests (use mocked time)
- File path differences (use `Path.Combine`)
- Missing test data files (ensure they're copied to output)

**Solution:**
```csharp
// ❌ BAD: Hardcoded path
var path = "C:\\Data\\test.json";

// ✅ GOOD: Portable path
var path = Path.Combine(AppContext.BaseDirectory, "TestData", "test.json");
```

---

### Flaky Tests

**Symptoms:** Test sometimes passes, sometimes fails.

**Common Causes:**
- Async operations without proper waiting
- Random number generation
- Shared mutable state

**Solutions:**
```csharp
// ❌ BAD: Race condition
await Task.Run(() => battleSystem.Update(0.016f));
Assert.True(battleState.Status == BattleStatus.Fainted);  // May fail!

// ✅ GOOD: Proper synchronization
var task = Task.Run(() => battleSystem.Update(0.016f));
await task;
Assert.True(battleState.Status == BattleStatus.Fainted);
```

---

## Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Quick Start](https://github.com/moq/moq4/wiki/Quickstart)
- [Code Coverage Tools](https://github.com/coverlet-coverage/coverlet)
- [Test-Driven Development](https://martinfowler.com/bliki/TestDrivenDevelopment.html)
