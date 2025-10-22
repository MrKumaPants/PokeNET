# PokeNET Testing Guide

## Overview

This document describes the comprehensive testing strategy for PokeNET, including unit tests, integration tests, and performance benchmarks.

## Test Structure

```
tests/
├── Unit/                    # Unit tests (isolated component testing)
│   ├── Core/               # Core game systems
│   ├── Localization/       # Localization tests
│   └── ...
├── Integration/            # Integration tests (system interactions)
├── Performance/            # Performance benchmarks
└── Utilities/              # Test helpers and utilities
```

## Running Tests

### All Tests
```bash
dotnet test tests/PokeNET.Tests.csproj
```

### Specific Test Category
```bash
dotnet test tests/PokeNET.Tests.csproj --filter "FullyQualifiedName~Unit"
dotnet test tests/PokeNET.Tests.csproj --filter "FullyQualifiedName~Integration"
dotnet test tests/PokeNET.Tests.csproj --filter "FullyQualifiedName~Performance"
```

### With Code Coverage
```bash
dotnet test tests/PokeNET.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Performance Benchmarks
```bash
dotnet run -c Release --project tests/PokeNET.Tests.csproj --filter *Benchmarks*
```

## Test Categories

### 1. Unit Tests

**Purpose**: Test individual components in isolation

**Characteristics**:
- Fast execution (< 100ms per test)
- No external dependencies
- Use mocks for dependencies
- High code coverage target (90%+)

**Example**:
```csharp
[Fact]
public void SetCulture_WithValidCulture_ShouldUpdateCurrentCulture()
{
    // Arrange
    var cultureName = "en-US";

    // Act
    LocalizationManager.SetCulture(cultureName);

    // Assert
    CultureInfo.CurrentCulture.Name.Should().Be(cultureName);
}
```

### 2. Integration Tests

**Purpose**: Test system interactions and data flow

**Characteristics**:
- Test multiple components together
- Verify service registration and DI
- Test asset loading and initialization
- Coverage target (75%+)

**Example**:
```csharp
[Fact]
public void Game_ShouldInitializeWithDefaultSettings()
{
    // Arrange & Act
    using var game = new PokeNETGame();

    // Assert
    game.Content.RootDirectory.Should().Be("Content");
    game.Services.Should().NotBeNull();
}
```

### 3. Performance Benchmarks

**Purpose**: Monitor and optimize performance

**Tools**: BenchmarkDotNet

**Metrics**:
- Execution time
- Memory allocations
- GC pressure

**Example**:
```csharp
[Benchmark]
public void SetCulture_DefaultCulture()
{
    LocalizationManager.SetCulture(LocalizationManager.DEFAULT_CULTURE_CODE);
}
```

## Test Utilities

### TestGameFactory

Provides factory methods for creating test instances:
- `CreateGame()` - Create PokeNETGame instance
- `CreateGameTime()` - Create GameTime for frame simulation
- `CreateFrameSequence()` - Generate multiple frames

### AssertionExtensions

Custom assertions for game-specific scenarios:
- `ShouldBeApproximately()` - Float/double tolerance checking
- `ShouldCompleteWithin()` - Performance assertions
- `ShouldContainExactly()` - Collection count assertions

### MemoryTestHelpers

Memory and performance profiling:
- `MeasureAllocations()` - Track memory allocations
- `MeasureExecutionTime()` - Track execution time
- `BenchmarkAction()` - Run and average multiple iterations

## Coverage Goals

| Layer | Target Coverage |
|-------|----------------|
| Domain Layer | 90%+ |
| Core Systems | 85%+ |
| Integration Points | 75%+ |
| Critical Paths | 100% |

## Best Practices

### 1. Test Naming Convention

```
MethodName_StateUnderTest_ExpectedBehavior
```

Examples:
- `SetCulture_WithValidCulture_ShouldUpdateCurrentCulture`
- `Constructor_ShouldInitializeGraphicsDeviceManager`

### 2. AAA Pattern

Structure tests using Arrange-Act-Assert:

```csharp
[Fact]
public void Example_Test()
{
    // Arrange - Set up test data and dependencies
    var input = "test";

    // Act - Execute the method under test
    var result = MethodUnderTest(input);

    // Assert - Verify the outcome
    result.Should().Be(expected);
}
```

### 3. Use FluentAssertions

Prefer FluentAssertions for readable assertions:

```csharp
// Good
result.Should().NotBeNull();
collection.Should().HaveCount(5);
value.Should().BeInRange(1, 10);

// Avoid
Assert.NotNull(result);
Assert.Equal(5, collection.Count);
```

### 4. Avoid Test Interdependence

Each test should be independent and self-contained:

```csharp
// Bad - Tests depend on execution order
static int counter = 0;

[Fact] public void Test1() { counter++; }
[Fact] public void Test2() { Assert.Equal(1, counter); }

// Good - Each test is independent
[Fact] public void Test1() { var counter = 0; counter++; }
[Fact] public void Test2() { var counter = 0; counter++; }
```

### 5. Use Theory for Parameterized Tests

```csharp
[Theory]
[InlineData("en-US")]
[InlineData("es-ES")]
[InlineData("fr-FR")]
public void SetCulture_WithSupportedCulture_ShouldSucceed(string cultureName)
{
    var act = () => LocalizationManager.SetCulture(cultureName);
    act.Should().NotThrow();
}
```

## Testing Future Components

### ECS Components (When Implemented)

```csharp
[Fact]
public void Position_Component_ShouldInitializeCorrectly()
{
    var position = new Position { X = 10, Y = 20 };
    position.X.Should().Be(10);
    position.Y.Should().Be(20);
}
```

### ECS Systems (When Implemented)

```csharp
[Fact]
public void MovementSystem_ShouldUpdatePositions()
{
    // Arrange
    var world = TestECSFactory.CreateWorld();
    var entity = world.Create<Position, Velocity>();

    // Act
    movementSystem.Update(world, gameTime);

    // Assert
    var position = entity.Get<Position>();
    position.X.Should().BeGreaterThan(0);
}
```

### Asset Loading (When Implemented)

```csharp
[Fact]
public async Task AssetManager_ShouldLoadTexture()
{
    // Arrange
    var assetManager = new AssetManager();

    // Act
    var texture = await assetManager.LoadAsync<Texture2D>("test.png");

    // Assert
    texture.Should().NotBeNull();
}
```

### Mod Loading (When Implemented)

```csharp
[Fact]
public void ModLoader_ShouldRespectDependencyOrder()
{
    // Arrange
    var modLoader = new ModLoader();

    // Act
    var loadedMods = modLoader.LoadMods("TestMods");

    // Assert
    loadedMods.Should().BeInAscendingOrder(m => m.Priority);
}
```

## Continuous Integration

Add to CI/CD pipeline:

```yaml
- name: Run Tests
  run: dotnet test --configuration Release --no-build --verbosity normal

- name: Code Coverage
  run: |
    dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

- name: Upload Coverage
  uses: codecov/codecov-action@v3
```

## Performance Baseline

Establish performance baselines for critical operations:

| Operation | Target Time | Max Memory |
|-----------|-------------|------------|
| Game Initialization | < 500ms | < 10 MB |
| Frame Processing | < 16.6ms | < 1 KB |
| Asset Loading | < 100ms | Varies |
| Culture Switch | < 1ms | < 1 KB |

## Debugging Failed Tests

### Visual Studio
1. Open Test Explorer (Test > Test Explorer)
2. Right-click failed test
3. Select "Debug Selected Tests"

### VS Code
1. Install C# extension
2. Click "Debug Test" above test method
3. Set breakpoints as needed

### Command Line
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Next Steps

As the framework develops, add tests for:

1. **ECS Architecture** (Phase 2)
   - Component creation/deletion
   - System execution order
   - Query performance
   - Entity lifecycle

2. **Asset Management** (Phase 3)
   - Asset loading/caching
   - Mod override resolution
   - Asset unloading
   - Memory management

3. **Mod System** (Phase 4)
   - Mod discovery and loading
   - Dependency resolution
   - Conflict detection
   - Harmony patches

4. **Scripting Engine** (Phase 5)
   - Script compilation
   - API exposure
   - Security sandboxing
   - Error handling

5. **Audio System** (Phase 6)
   - MIDI generation
   - Audio playback
   - Volume management
   - State transitions

## Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/moq/moq4)
- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
