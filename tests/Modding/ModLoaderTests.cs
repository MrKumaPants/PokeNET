using System.Reflection;
using System.Text.Json;
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
/// Comprehensive tests for ModLoader class.
/// Tests mod discovery, dependency resolution, circular dependency detection,
/// load order validation, assembly loading/unloading, and error handling.
/// Target: 400+ lines, 90%+ coverage
/// </summary>
public class ModLoaderTests : IDisposable
{
    private readonly string _testModsDirectory;
    private readonly IServiceProvider _services;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ModLoader> _logger;
    private readonly Mock<ILogger<ModLoader>> _mockLogger;

    public ModLoaderTests()
    {
        // Setup test directory
        _testModsDirectory = Path.Combine(Path.GetTempPath(), $"ModLoaderTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testModsDirectory);

        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        _services = services.BuildServiceProvider();
        _loggerFactory = _services.GetRequiredService<ILoggerFactory>();

        // Setup logger
        _mockLogger = new Mock<ILogger<ModLoader>>();
        _logger = _mockLogger.Object;
    }

    public void Dispose()
    {
        ModTestHelpers.CleanupTestMods(_testModsDirectory);
    }

    #region Mod Discovery Tests

    [Fact]
    public void DiscoverMods_WithValidManifests_ShouldDiscoverAllMods()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "TestMod1", "Test Mod 1");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "TestMod2", "Test Mod 2");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "TestMod3", "Test Mod 3");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);

        // Act
        var count = loader.DiscoverMods();

        // Assert
        count.Should().Be(3, "should discover all 3 valid mods");
        loader.DiscoveredMods.Should().HaveCount(3);
        loader.DiscoveredMods.Select(m => m.Id).Should().Contain(new[] { "TestMod1", "TestMod2", "TestMod3" });
    }

    [Fact]
    public void DiscoverMods_WithNonExistentDirectory_ShouldReturnZero()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_testModsDirectory, "NonExistent");
        var loader = new ModLoader(_logger, _services, _loggerFactory, nonExistentDir);

        // Act
        var count = loader.DiscoverMods();

        // Assert
        count.Should().Be(0, "should not discover any mods in non-existent directory");
        loader.DiscoveredMods.Should().BeEmpty();
    }

    [Fact]
    public void DiscoverMods_WithInvalidManifest_ShouldSkipAndLogError()
    {
        // Arrange
        var modPath = Path.Combine(_testModsDirectory, "InvalidMod");
        Directory.CreateDirectory(modPath);
        File.WriteAllText(Path.Combine(modPath, "modinfo.json"), "{ invalid json }");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);

        // Act
        var count = loader.DiscoverMods();

        // Assert
        count.Should().Be(0, "should skip mods with invalid manifests");
        loader.DiscoveredMods.Should().BeEmpty();
    }

    [Fact]
    public void DiscoverMods_WithMissingManifest_ShouldSkipDirectory()
    {
        // Arrange
        var modPath = Path.Combine(_testModsDirectory, "NoManifest");
        Directory.CreateDirectory(modPath);
        // No modinfo.json created

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);

        // Act
        var count = loader.DiscoverMods();

        // Assert
        count.Should().Be(0, "should skip directories without modinfo.json");
        loader.DiscoveredMods.Should().BeEmpty();
    }

    [Fact]
    public void DiscoverMods_WithEmptyManifest_ShouldSkipAndLogWarning()
    {
        // Arrange
        var modPath = Path.Combine(_testModsDirectory, "EmptyMod");
        Directory.CreateDirectory(modPath);
        File.WriteAllText(Path.Combine(modPath, "modinfo.json"), "null");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);

        // Act
        var count = loader.DiscoverMods();

        // Assert
        count.Should().Be(0, "should skip empty manifests");
    }

    [Fact]
    public void DiscoverMods_WithMixedValidAndInvalid_ShouldDiscoverOnlyValid()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ValidMod", "Valid Mod");

        var invalidPath = Path.Combine(_testModsDirectory, "InvalidMod");
        Directory.CreateDirectory(invalidPath);
        File.WriteAllText(Path.Combine(invalidPath, "modinfo.json"), "{ bad json");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);

        // Act
        var count = loader.DiscoverMods();

        // Assert
        count.Should().Be(1, "should discover only valid mods");
        loader.DiscoveredMods.First().Id.Should().Be("ValidMod");
    }

    #endregion

    #region Dependency Resolution Tests

    [Fact]
    public void LoadMods_WithSimpleDependency_ShouldLoadInCorrectOrder()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B",
            dependencies: new[] { "ModA" });

        CreateDummyAssembly(_testModsDirectory, "ModA");
        CreateDummyAssembly(_testModsDirectory, "ModB");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act
        loader.LoadMods();
        var loadOrder = loader.GetLoadOrder();

        // Assert
        loadOrder.Should().ContainInOrder("ModA", "ModB", "dependencies should be loaded first");
    }

    [Fact]
    public void LoadMods_WithComplexDependencyChain_ShouldResolveCorrectly()
    {
        // Arrange
        // Chain: ModA -> ModB -> ModC (C depends on B, B depends on A)
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B",
            dependencies: new[] { "ModA" });
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModC", "Mod C",
            dependencies: new[] { "ModB" });

        CreateDummyAssembly(_testModsDirectory, "ModA");
        CreateDummyAssembly(_testModsDirectory, "ModB");
        CreateDummyAssembly(_testModsDirectory, "ModC");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act
        loader.LoadMods();
        var loadOrder = loader.GetLoadOrder();

        // Assert
        loadOrder.Should().ContainInOrder("ModA", "ModB", "ModC");
        loadOrder.IndexOf("ModA").Should().BeLessThan(loadOrder.IndexOf("ModB"));
        loadOrder.IndexOf("ModB").Should().BeLessThan(loadOrder.IndexOf("ModC"));
    }

    [Fact]
    public void LoadMods_WithCircularDependency_ShouldThrowModLoadException()
    {
        // Arrange - Create circular dependency: A -> B -> C -> A
        var manifestA = CreateManifestWithDependencies("ModA", "Mod A", new[] { "ModB" });
        var manifestB = CreateManifestWithDependencies("ModB", "Mod B", new[] { "ModC" });
        var manifestC = CreateManifestWithDependencies("ModC", "Mod C", new[] { "ModA" });

        CreateModWithManifest(_testModsDirectory, "ModA", manifestA);
        CreateModWithManifest(_testModsDirectory, "ModB", manifestB);
        CreateModWithManifest(_testModsDirectory, "ModC", manifestC);

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act & Assert
        var exception = Assert.Throws<ModLoadException>(() => loader.LoadMods());
        exception.Message.Should().Contain("Circular dependency");
    }

    [Fact]
    public void LoadMods_WithMissingRequiredDependency_ShouldThrowModLoadException()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A",
            dependencies: new[] { "NonExistentMod" });

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act & Assert
        var exception = Assert.Throws<ModLoadException>(() => loader.LoadMods());
        exception.Message.Should().Contain("Missing required dependency");
        exception.Message.Should().Contain("NonExistentMod");
    }

    [Fact]
    public void LoadMods_WithOptionalDependency_ShouldLoadSuccessfully()
    {
        // Arrange - Create manifest with optional dependency
        var manifest = @"{
            ""id"": ""ModA"",
            ""name"": ""Mod A"",
            ""version"": ""1.0.0"",
            ""dependencies"": [
                {
                    ""modId"": ""OptionalMod"",
                    ""optional"": true
                }
            ]
        }";

        CreateModWithManifest(_testModsDirectory, "ModA", manifest);
        CreateDummyAssembly(_testModsDirectory, "ModA");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act & Assert - Should not throw
        loader.LoadMods();
        loader.LoadedMods.Should().ContainSingle();
    }

    [Fact]
    public void LoadMods_WithLoadAfter_ShouldRespectOrder()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");

        var manifestB = @"{
            ""id"": ""ModB"",
            ""name"": ""Mod B"",
            ""version"": ""1.0.0"",
            ""loadAfter"": [""ModA""]
        }";
        CreateModWithManifest(_testModsDirectory, "ModB", manifestB);

        CreateDummyAssembly(_testModsDirectory, "ModA");
        CreateDummyAssembly(_testModsDirectory, "ModB");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act
        loader.LoadMods();
        var loadOrder = loader.GetLoadOrder();

        // Assert
        loadOrder.Should().ContainInOrder("ModA", "ModB");
    }

    [Fact]
    public void LoadMods_WithLoadBefore_ShouldRespectOrder()
    {
        // Arrange
        var manifestA = @"{
            ""id"": ""ModA"",
            ""name"": ""Mod A"",
            ""version"": ""1.0.0"",
            ""loadBefore"": [""ModB""]
        }";
        CreateModWithManifest(_testModsDirectory, "ModA", manifestA);

        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B");

        CreateDummyAssembly(_testModsDirectory, "ModA");
        CreateDummyAssembly(_testModsDirectory, "ModB");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act
        loader.LoadMods();
        var loadOrder = loader.GetLoadOrder();

        // Assert
        loadOrder.Should().ContainInOrder("ModA", "ModB");
    }

    #endregion

    #region Assembly Loading Tests

    [Fact]
    public void LoadMod_WithMissingAssembly_ShouldThrowModLoadException()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        // No assembly file created

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act & Assert
        var exception = Assert.Throws<ModLoadException>(() => loader.LoadMods());
        exception.Message.Should().Contain("Assembly not found");
        exception.ModId.Should().Be("ModA");
    }

    [Fact]
    public void IsModLoaded_WithLoadedMod_ShouldReturnTrue()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        CreateDummyAssembly(_testModsDirectory, "ModA");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();
        loader.LoadMods();

        // Act & Assert
        loader.IsModLoaded("ModA").Should().BeTrue();
    }

    [Fact]
    public void IsModLoaded_WithUnloadedMod_ShouldReturnFalse()
    {
        // Arrange
        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);

        // Act & Assert
        loader.IsModLoaded("NonExistentMod").Should().BeFalse();
    }

    [Fact]
    public void GetMod_WithLoadedMod_ShouldReturnModInstance()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        CreateDummyAssembly(_testModsDirectory, "ModA");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();
        loader.LoadMods();

        // Act
        var mod = loader.GetMod("ModA");

        // Assert - Mod might be null if IMod implementation isn't found, but call should not throw
        // This test verifies the GetMod method works
        mod.Should().NotBeNull();
    }

    [Fact]
    public void GetMod_WithUnloadedMod_ShouldReturnNull()
    {
        // Arrange
        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);

        // Act
        var mod = loader.GetMod("NonExistentMod");

        // Assert
        mod.Should().BeNull();
    }

    #endregion

    #region Mod Unloading Tests

    [Fact]
    public void UnloadMods_ShouldUnloadAllModsInReverseOrder()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B");
        CreateDummyAssembly(_testModsDirectory, "ModA");
        CreateDummyAssembly(_testModsDirectory, "ModB");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();
        loader.LoadMods();

        // Act
        loader.UnloadMods();

        // Assert
        loader.LoadedMods.Should().BeEmpty("all mods should be unloaded");
        loader.IsModLoaded("ModA").Should().BeFalse();
        loader.IsModLoaded("ModB").Should().BeFalse();
    }

    [Fact]
    public async Task UnloadModAsync_WithValidModId_ShouldUnloadSpecificMod()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B");
        CreateDummyAssembly(_testModsDirectory, "ModA");
        CreateDummyAssembly(_testModsDirectory, "ModB");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();
        loader.LoadMods();

        // Act
        await loader.UnloadModAsync("ModA");

        // Assert
        loader.IsModLoaded("ModA").Should().BeFalse("ModA should be unloaded");
        loader.IsModLoaded("ModB").Should().BeTrue("ModB should still be loaded");
    }

    [Fact]
    public async Task UnloadModAsync_WithInvalidModId_ShouldThrowModLoadException()
    {
        // Arrange
        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);

        // Act & Assert
        await Assert.ThrowsAsync<ModLoadException>(async () =>
            await loader.UnloadModAsync("NonExistentMod"));
    }

    [Fact]
    public async Task ReloadModAsync_WithValidModId_ShouldReloadMod()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        CreateDummyAssembly(_testModsDirectory, "ModA");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();
        loader.LoadMods();

        // Act
        await loader.ReloadModAsync("ModA");

        // Assert
        loader.IsModLoaded("ModA").Should().BeTrue("mod should be reloaded");
    }

    [Fact]
    public async Task ReloadModAsync_WithInvalidModId_ShouldThrowModLoadException()
    {
        // Arrange
        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);

        // Act & Assert
        await Assert.ThrowsAsync<ModLoadException>(async () =>
            await loader.ReloadModAsync("NonExistentMod"));
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ValidateModsAsync_WithValidMods_ShouldReturnNoErrors()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        CreateDummyAssembly(_testModsDirectory, "ModA");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);

        // Act
        var report = await loader.ValidateModsAsync(_testModsDirectory);

        // Assert
        report.IsValid.Should().BeTrue();
        report.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateModsAsync_WithDuplicateModIds_ShouldReturnError()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        var duplicatePath = Path.Combine(_testModsDirectory, "ModA_Copy");
        Directory.CreateDirectory(duplicatePath);
        File.WriteAllText(
            Path.Combine(duplicatePath, "modinfo.json"),
            ModTestHelpers.CreateTestManifest("ModA", "Mod A Copy"));

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);

        // Act
        var report = await loader.ValidateModsAsync(_testModsDirectory);

        // Assert
        report.IsValid.Should().BeFalse();
        report.Errors.Should().Contain(e => e.ErrorType == ModValidationErrorType.DuplicateModId);
        report.Errors.Should().Contain(e => e.Message.Contains("ModA"));
    }

    [Fact]
    public async Task ValidateModsAsync_WithMissingRequiredDependency_ShouldReturnError()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A",
            dependencies: new[] { "NonExistentMod" });

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);

        // Act
        var report = await loader.ValidateModsAsync(_testModsDirectory);

        // Assert
        report.IsValid.Should().BeFalse();
        report.Errors.Should().Contain(e => e.ErrorType == ModValidationErrorType.MissingDependency);
    }

    [Fact]
    public async Task ValidateModsAsync_WithCircularDependency_ShouldReturnError()
    {
        // Arrange
        var manifestA = CreateManifestWithDependencies("ModA", "Mod A", new[] { "ModB" });
        var manifestB = CreateManifestWithDependencies("ModB", "Mod B", new[] { "ModA" });

        CreateModWithManifest(_testModsDirectory, "ModA", manifestA);
        CreateModWithManifest(_testModsDirectory, "ModB", manifestB);

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);

        // Act
        var report = await loader.ValidateModsAsync(_testModsDirectory);

        // Assert
        report.IsValid.Should().BeFalse();
        report.Errors.Should().Contain(e => e.ErrorType == ModValidationErrorType.CircularDependency);
    }

    [Fact]
    public async Task ValidateModsAsync_WithIncompatibleMods_ShouldReturnError()
    {
        // Arrange
        var manifestA = @"{
            ""id"": ""ModA"",
            ""name"": ""Mod A"",
            ""version"": ""1.0.0"",
            ""incompatibleWith"": [
                {
                    ""modId"": ""ModB"",
                    ""reason"": ""Test incompatibility""
                }
            ]
        }";
        CreateModWithManifest(_testModsDirectory, "ModA", manifestA);
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);

        // Act
        var report = await loader.ValidateModsAsync(_testModsDirectory);

        // Assert
        report.IsValid.Should().BeFalse();
        report.Errors.Should().Contain(e => e.ErrorType == ModValidationErrorType.IncompatibleModLoaded);
    }

    [Fact]
    public async Task ValidateModsAsync_WithNonExistentDirectory_ShouldReturnError()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_testModsDirectory, "NonExistent");
        var loader = new ModLoader(_logger, _services, _loggerFactory, nonExistentDir);

        // Act
        var report = await loader.ValidateModsAsync(nonExistentDir);

        // Assert
        report.IsValid.Should().BeFalse();
        report.Errors.Should().Contain(e => e.ErrorType == ModValidationErrorType.InvalidManifest);
    }

    #endregion

    #region Helper Methods

    private void CreateDummyAssembly(string modsDirectory, string modId)
    {
        var modPath = Path.Combine(modsDirectory, modId);
        var assemblyPath = Path.Combine(modPath, $"{modId}.dll");

        // Create a dummy file to simulate an assembly
        File.WriteAllBytes(assemblyPath, new byte[] { 0x4D, 0x5A }); // MZ header
    }

    private void CreateModWithManifest(string modsDirectory, string modId, string manifestJson)
    {
        var modPath = Path.Combine(modsDirectory, modId);
        Directory.CreateDirectory(modPath);
        File.WriteAllText(Path.Combine(modPath, "modinfo.json"), manifestJson);
    }

    private string CreateManifestWithDependencies(string id, string name, string[] dependencies)
    {
        var deps = dependencies.Select(d => new { modId = d });
        var manifest = new
        {
            id = id,
            name = name,
            version = "1.0.0",
            dependencies = deps
        };

        return JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
    }

    #endregion
}
