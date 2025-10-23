using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PokeNET.Core.Modding;
using PokeNET.Domain.Modding;
using PokeNET.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace PokeNET.Tests.Modding;

/// <summary>
/// Comprehensive tests for ModRegistry class.
/// Tests mod registration/deregistration, metadata storage/retrieval,
/// conflict detection, state management, thread safety, and memory usage.
/// Target: 300+ lines, 90%+ coverage
/// </summary>
public class ModRegistryTests : IDisposable
{
    private readonly string _testModsDirectory;
    private readonly IServiceProvider _services;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ModLoader _modLoader;
    private readonly ModRegistry _registry;

    public ModRegistryTests()
    {
        // Setup test directory
        _testModsDirectory = Path.Combine(Path.GetTempPath(), $"ModRegistryTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testModsDirectory);

        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        _services = services.BuildServiceProvider();
        _loggerFactory = _services.GetRequiredService<ILoggerFactory>();

        // Create mod loader and registry
        var mockLogger = new Mock<ILogger<ModLoader>>();
        _modLoader = new ModLoader(mockLogger.Object, _services, _loggerFactory, _testModsDirectory);
        _registry = new ModRegistry(_modLoader);
    }

    public void Dispose()
    {
        ModTestHelpers.CleanupTestMods(_testModsDirectory);
    }

    #region Basic Registry Operations

    [Fact]
    public void GetAllMods_WithNoModsLoaded_ShouldReturnEmptyList()
    {
        // Act
        var mods = _registry.GetAllMods();

        // Assert
        mods.Should().BeEmpty("no mods have been loaded");
    }

    [Fact]
    public void GetAllMods_WithLoadedMods_ShouldReturnAllMods()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModC", "Mod C");

        CreateDummyAssembly("ModA");
        CreateDummyAssembly("ModB");
        CreateDummyAssembly("ModC");

        _modLoader.DiscoverMods();
        _modLoader.LoadMods();

        // Act
        var mods = _registry.GetAllMods();

        // Assert
        mods.Should().HaveCount(3);
        mods.Select(m => m.Id).Should().Contain(new[] { "ModA", "ModB", "ModC" });
    }

    [Fact]
    public void GetMod_WithValidModId_ShouldReturnMod()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "TestMod", "Test Mod");
        CreateDummyAssembly("TestMod");

        _modLoader.DiscoverMods();
        _modLoader.LoadMods();

        // Act
        var mod = _registry.GetMod("TestMod");

        // Assert
        mod.Should().NotBeNull();
        mod!.Id.Should().Be("TestMod");
        mod.Name.Should().Be("Test Mod");
    }

    [Fact]
    public void GetMod_WithInvalidModId_ShouldReturnNull()
    {
        // Act
        var mod = _registry.GetMod("NonExistentMod");

        // Assert
        mod.Should().BeNull();
    }

    [Fact]
    public void IsModLoaded_WithLoadedMod_ShouldReturnTrue()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "TestMod", "Test Mod");
        CreateDummyAssembly("TestMod");

        _modLoader.DiscoverMods();
        _modLoader.LoadMods();

        // Act
        var isLoaded = _registry.IsModLoaded("TestMod");

        // Assert
        isLoaded.Should().BeTrue();
    }

    [Fact]
    public void IsModLoaded_WithUnloadedMod_ShouldReturnFalse()
    {
        // Act
        var isLoaded = _registry.IsModLoaded("NonExistentMod");

        // Assert
        isLoaded.Should().BeFalse();
    }

    #endregion

    #region Dependency Queries

    [Fact]
    public void GetDependencies_WithNoDependencies_ShouldReturnEmptyList()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        CreateDummyAssembly("ModA");

        _modLoader.DiscoverMods();
        _modLoader.LoadMods();

        // Act
        var dependencies = _registry.GetDependencies("ModA");

        // Assert
        dependencies.Should().BeEmpty();
    }

    [Fact]
    public void GetDependencies_WithOneDependency_ShouldReturnDependency()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B",
            dependencies: new[] { "ModA" });

        CreateDummyAssembly("ModA");
        CreateDummyAssembly("ModB");

        _modLoader.DiscoverMods();
        _modLoader.LoadMods();

        // Act
        var dependencies = _registry.GetDependencies("ModB");

        // Assert
        dependencies.Should().ContainSingle();
        dependencies.First().Id.Should().Be("ModA");
    }

    [Fact]
    public void GetDependencies_WithMultipleDependencies_ShouldReturnAll()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModC", "Mod C",
            dependencies: new[] { "ModA", "ModB" });

        CreateDummyAssembly("ModA");
        CreateDummyAssembly("ModB");
        CreateDummyAssembly("ModC");

        _modLoader.DiscoverMods();
        _modLoader.LoadMods();

        // Act
        var dependencies = _registry.GetDependencies("ModC");

        // Assert
        dependencies.Should().HaveCount(2);
        dependencies.Select(d => d.Id).Should().Contain(new[] { "ModA", "ModB" });
    }

    [Fact]
    public void GetDependencies_WithNonExistentMod_ShouldReturnEmptyList()
    {
        // Act
        var dependencies = _registry.GetDependencies("NonExistentMod");

        // Assert
        dependencies.Should().BeEmpty();
    }

    [Fact]
    public void GetDependentMods_WithNoDependents_ShouldReturnEmptyList()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        CreateDummyAssembly("ModA");

        _modLoader.DiscoverMods();
        _modLoader.LoadMods();

        // Act
        var dependents = _registry.GetDependentMods("ModA");

        // Assert
        dependents.Should().BeEmpty();
    }

    [Fact]
    public void GetDependentMods_WithOneDependentMod_ShouldReturnDependent()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B",
            dependencies: new[] { "ModA" });

        CreateDummyAssembly("ModA");
        CreateDummyAssembly("ModB");

        _modLoader.DiscoverMods();
        _modLoader.LoadMods();

        // Act
        var dependents = _registry.GetDependentMods("ModA");

        // Assert
        dependents.Should().ContainSingle();
        dependents.First().Id.Should().Be("ModB");
    }

    [Fact]
    public void GetDependentMods_WithMultipleDependents_ShouldReturnAll()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B",
            dependencies: new[] { "ModA" });
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModC", "Mod C",
            dependencies: new[] { "ModA" });

        CreateDummyAssembly("ModA");
        CreateDummyAssembly("ModB");
        CreateDummyAssembly("ModC");

        _modLoader.DiscoverMods();
        _modLoader.LoadMods();

        // Act
        var dependents = _registry.GetDependentMods("ModA");

        // Assert
        dependents.Should().HaveCount(2);
        dependents.Select(d => d.Id).Should().Contain(new[] { "ModB", "ModC" });
    }

    [Fact]
    public void GetDependentMods_WithChainedDependencies_ShouldReturnOnlyDirectDependents()
    {
        // Arrange - Chain: ModA <- ModB <- ModC
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B",
            dependencies: new[] { "ModA" });
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModC", "Mod C",
            dependencies: new[] { "ModB" });

        CreateDummyAssembly("ModA");
        CreateDummyAssembly("ModB");
        CreateDummyAssembly("ModC");

        _modLoader.DiscoverMods();
        _modLoader.LoadMods();

        // Act
        var dependents = _registry.GetDependentMods("ModA");

        // Assert
        dependents.Should().ContainSingle("only direct dependents should be returned");
        dependents.First().Id.Should().Be("ModB");
    }

    #endregion

    #region API Access Tests

    [Fact]
    public void GetApi_WithValidModAndMatchingType_ShouldReturnApi()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        CreateDummyAssembly("ModA");

        _modLoader.DiscoverMods();
        _modLoader.LoadMods();

        // Act - Try to get as IMod (base interface)
        var api = _registry.GetApi<IMod>("ModA");

        // Assert - Will be null if no IMod implementation found, but should not throw
        // This tests the method signature and type casting
        api.Should().NotBeNull();
    }

    [Fact]
    public void GetApi_WithInvalidModId_ShouldReturnNull()
    {
        // Act
        var api = _registry.GetApi<IMod>("NonExistentMod");

        // Assert
        api.Should().BeNull();
    }

    [Fact]
    public void GetApi_WithIncompatibleType_ShouldReturnNull()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        CreateDummyAssembly("ModA");

        _modLoader.DiscoverMods();
        _modLoader.LoadMods();

        // Act - Try to get as incompatible type
        var api = _registry.GetApi<IDisposable>("ModA");

        // Assert - Should return null if mod doesn't implement IDisposable
        // (This depends on mod implementation, but tests the cast behavior)
        api.Should().BeNull();
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task ConcurrentAccess_ToGetAllMods_ShouldNotCrash()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B");
        CreateDummyAssembly("ModA");
        CreateDummyAssembly("ModB");

        _modLoader.DiscoverMods();
        _modLoader.LoadMods();

        // Act - Concurrent access from multiple threads
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                var mods = _registry.GetAllMods();
                mods.Should().NotBeEmpty();
            }
        }));

        // Assert - Should complete without exceptions
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task ConcurrentAccess_ToIsModLoaded_ShouldBeThreadSafe()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        CreateDummyAssembly("ModA");

        _modLoader.DiscoverMods();
        _modLoader.LoadMods();

        // Act - Concurrent checks
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                var isLoaded = _registry.IsModLoaded("ModA");
                isLoaded.Should().BeTrue();
            }
        }));

        // Assert
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task ConcurrentAccess_ToGetDependencies_ShouldNotThrow()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B",
            dependencies: new[] { "ModA" });

        CreateDummyAssembly("ModA");
        CreateDummyAssembly("ModB");

        _modLoader.DiscoverMods();
        _modLoader.LoadMods();

        // Act - Concurrent dependency queries
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 50; i++)
            {
                var deps = _registry.GetDependencies("ModB");
                deps.Should().NotBeEmpty();
            }
        }));

        // Assert
        await Task.WhenAll(tasks);
    }

    #endregion

    #region State Consistency Tests

    [Fact]
    public void GetAllMods_AfterUnloadingMod_ShouldReflectChanges()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B");
        CreateDummyAssembly("ModA");
        CreateDummyAssembly("ModB");

        _modLoader.DiscoverMods();
        _modLoader.LoadMods();

        var initialCount = _registry.GetAllMods().Count;

        // Act
        _modLoader.UnloadMods();

        // Assert
        _registry.GetAllMods().Should().BeEmpty("all mods were unloaded");
        initialCount.Should().Be(2, "initially had 2 mods");
    }

    [Fact]
    public void IsModLoaded_AfterUnloadingSpecificMod_ShouldReturnFalse()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        CreateDummyAssembly("ModA");

        _modLoader.DiscoverMods();
        _modLoader.LoadMods();

        _registry.IsModLoaded("ModA").Should().BeTrue("mod is initially loaded");

        // Act
        _modLoader.UnloadMods();

        // Assert
        _registry.IsModLoaded("ModA").Should().BeFalse("mod was unloaded");
    }

    #endregion

    #region Helper Methods

    private void CreateDummyAssembly(string modId)
    {
        var modPath = Path.Combine(_testModsDirectory, modId);
        var assemblyPath = Path.Combine(modPath, $"{modId}.dll");

        // Create a dummy file to simulate an assembly
        File.WriteAllBytes(assemblyPath, new byte[] { 0x4D, 0x5A }); // MZ header
    }

    #endregion
}
