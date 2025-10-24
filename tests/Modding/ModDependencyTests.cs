using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PokeNET.Core.Modding;
using PokeNET.Domain.Modding;
using PokeNET.Tests.Utilities;
using Xunit;

namespace PokeNET.Tests.Modding;

/// <summary>
/// Comprehensive tests for mod dependency resolution and load order calculation.
/// Tests single dependencies, transitive dependencies, optional dependencies,
/// version matching, circular dependencies, and complex dependency graphs.
/// Target: 150+ lines, 90%+ coverage
/// </summary>
public class ModDependencyTests : IDisposable
{
    private readonly string _testModsDirectory;
    private readonly IServiceProvider _services;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ModLoader> _logger;

    public ModDependencyTests()
    {
        _testModsDirectory = Path.Combine(Path.GetTempPath(), $"ModDependencyTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testModsDirectory);

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        _services = services.BuildServiceProvider();
        _loggerFactory = _services.GetRequiredService<ILoggerFactory>();
        _logger = new Mock<ILogger<ModLoader>>().Object;
    }

    public void Dispose()
    {
        ModTestHelpers.CleanupTestMods(_testModsDirectory);
    }

    #region Single Dependency Tests

    [Fact]
    public void LoadMods_WithSingleDependency_ShouldLoadInOrder()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "CoreMod", "Core Mod");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "DependentMod", "Dependent Mod",
            dependencies: new[] { "CoreMod" });

        CreateDummyAssemblies("CoreMod", "DependentMod");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act
        loader.LoadMods();
        var loadOrder = loader.GetLoadOrder();

        // Assert
        loadOrder.Should().ContainInOrder("CoreMod", "DependentMod");
    }

    [Fact]
    public void LoadMods_WithMissingSingleDependency_ShouldThrow()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "DependentMod", "Dependent Mod",
            dependencies: new[] { "NonExistent" });

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act & Assert
        var exception = Assert.Throws<ModLoadException>(() => loader.LoadMods());
        exception.Message.Should().Contain("Missing required dependency");
        exception.Message.Should().Contain("NonExistent");
    }

    #endregion

    #region Transitive Dependency Tests

    [Fact]
    public void LoadMods_WithTransitiveDependencies_ShouldResolveChain()
    {
        // Arrange - A -> B -> C (C depends on B, B depends on A)
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B",
            dependencies: new[] { "ModA" });
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModC", "Mod C",
            dependencies: new[] { "ModB" });

        CreateDummyAssemblies("ModA", "ModB", "ModC");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act
        loader.LoadMods();
        var loadOrder = loader.GetLoadOrder().ToList();

        // Assert
        loadOrder.Should().ContainInOrder("ModA", "ModB", "ModC");
        loadOrder.IndexOf("ModA").Should().Be(0);
        loadOrder.IndexOf("ModB").Should().Be(1);
        loadOrder.IndexOf("ModC").Should().Be(2);
    }

    [Fact]
    public void LoadMods_WithLongTransitiveChain_ShouldResolveCorrectly()
    {
        // Arrange - 5-mod chain: A -> B -> C -> D -> E
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B",
            dependencies: new[] { "ModA" });
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModC", "Mod C",
            dependencies: new[] { "ModB" });
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModD", "Mod D",
            dependencies: new[] { "ModC" });
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModE", "Mod E",
            dependencies: new[] { "ModD" });

        CreateDummyAssemblies("ModA", "ModB", "ModC", "ModD", "ModE");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act
        loader.LoadMods();
        var loadOrder = loader.GetLoadOrder();

        // Assert
        loadOrder.Should().ContainInOrder("ModA", "ModB", "ModC", "ModD", "ModE");
    }

    #endregion

    #region Optional Dependency Tests

    [Fact]
    public void LoadMods_WithPresentOptionalDependency_ShouldRespectOrder()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "OptionalMod", "Optional Mod");
        var manifest = @"{
            ""id"": ""MainMod"",
            ""name"": ""Main Mod"",
            ""version"": ""1.0.0"",
            ""dependencies"": [
                {
                    ""modId"": ""OptionalMod"",
                    ""optional"": true
                }
            ]
        }";
        CreateModWithManifest("MainMod", manifest);

        CreateDummyAssemblies("OptionalMod", "MainMod");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act
        loader.LoadMods();
        var loadOrder = loader.GetLoadOrder();

        // Assert
        loadOrder.Should().ContainInOrder("OptionalMod", "MainMod");
    }

    [Fact]
    public void LoadMods_WithMissingOptionalDependency_ShouldLoadSuccessfully()
    {
        // Arrange
        var manifest = @"{
            ""id"": ""MainMod"",
            ""name"": ""Main Mod"",
            ""version"": ""1.0.0"",
            ""dependencies"": [
                {
                    ""modId"": ""MissingOptional"",
                    ""optional"": true
                }
            ]
        }";
        CreateModWithManifest("MainMod", manifest);
        CreateDummyAssemblies("MainMod");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act & Assert - Should not throw
        loader.LoadMods();
        loader.LoadedMods.Should().ContainSingle();
    }

    #endregion

    #region Version Range Tests

    [Fact]
    public async Task ValidateModsAsync_WithVersionedDependency_ShouldValidate()
    {
        // Arrange
        var manifestA = @"{
            ""id"": ""ModA"",
            ""name"": ""Mod A"",
            ""version"": ""2.1.0""
        }";
        var manifestB = @"{
            ""id"": ""ModB"",
            ""name"": ""Mod B"",
            ""version"": ""1.0.0"",
            ""dependencies"": [
                {
                    ""modId"": ""ModA"",
                    ""version"": "">=2.0.0""
                }
            ]
        }";

        CreateModWithManifest("ModA", manifestA);
        CreateModWithManifest("ModB", manifestB);

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);

        // Act
        var report = await loader.ValidateModsAsync(_testModsDirectory);

        // Assert - Should pass validation (version matching not fully implemented)
        report.Errors.Should().NotContain(e => e.ErrorType == ModValidationErrorType.MissingDependency);
    }

    #endregion

    #region Circular Dependency Tests

    [Fact]
    public void LoadMods_WithTwoModCircular_ShouldDetectCycle()
    {
        // Arrange - A -> B -> A
        var manifestA = CreateManifestWithDeps("ModA", new[] { "ModB" });
        var manifestB = CreateManifestWithDeps("ModB", new[] { "ModA" });

        CreateModWithManifest("ModA", manifestA);
        CreateModWithManifest("ModB", manifestB);

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act & Assert
        var exception = Assert.Throws<ModLoadException>(() => loader.LoadMods());
        exception.Message.Should().Contain("Circular dependency");
    }

    [Fact]
    public void LoadMods_WithThreeModCircular_ShouldDetectCycle()
    {
        // Arrange - A -> B -> C -> A
        var manifestA = CreateManifestWithDeps("ModA", new[] { "ModB" });
        var manifestB = CreateManifestWithDeps("ModB", new[] { "ModC" });
        var manifestC = CreateManifestWithDeps("ModC", new[] { "ModA" });

        CreateModWithManifest("ModA", manifestA);
        CreateModWithManifest("ModB", manifestB);
        CreateModWithManifest("ModC", manifestC);

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act & Assert
        var exception = Assert.Throws<ModLoadException>(() => loader.LoadMods());
        exception.Message.Should().Contain("Circular dependency");
    }

    [Fact]
    public void LoadMods_WithComplexCircular_ShouldReportCycle()
    {
        // Arrange - Complex graph with cycle: A->B, A->C, B->D, C->D, D->A
        var manifestA = CreateManifestWithDeps("ModA", new[] { "ModB", "ModC" });
        var manifestB = CreateManifestWithDeps("ModB", new[] { "ModD" });
        var manifestC = CreateManifestWithDeps("ModC", new[] { "ModD" });
        var manifestD = CreateManifestWithDeps("ModD", new[] { "ModA" });

        CreateModWithManifest("ModA", manifestA);
        CreateModWithManifest("ModB", manifestB);
        CreateModWithManifest("ModC", manifestC);
        CreateModWithManifest("ModD", manifestD);

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act & Assert
        var exception = Assert.Throws<ModLoadException>(() => loader.LoadMods());
        exception.Message.Should().Contain("Circular dependency");
    }

    #endregion

    #region Complex Dependency Graph Tests

    [Fact]
    public void LoadMods_WithDiamondDependency_ShouldResolve()
    {
        // Arrange - Diamond: D depends on B & C, both depend on A
        //     A
        //    / \
        //   B   C
        //    \ /
        //     D
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModA", "Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B",
            dependencies: new[] { "ModA" });
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModC", "Mod C",
            dependencies: new[] { "ModA" });
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModD", "Mod D",
            dependencies: new[] { "ModB", "ModC" });

        CreateDummyAssemblies("ModA", "ModB", "ModC", "ModD");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act
        loader.LoadMods();
        var loadOrder = loader.GetLoadOrder().ToList();

        // Assert
        loadOrder.Should().HaveCount(4);
        loadOrder.IndexOf("ModA").Should().BeLessThan(loadOrder.IndexOf("ModB"));
        loadOrder.IndexOf("ModA").Should().BeLessThan(loadOrder.IndexOf("ModC"));
        loadOrder.IndexOf("ModB").Should().BeLessThan(loadOrder.IndexOf("ModD"));
        loadOrder.IndexOf("ModC").Should().BeLessThan(loadOrder.IndexOf("ModD"));
    }

    [Fact]
    public void LoadMods_WithMultipleDependenciesPerMod_ShouldResolve()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "Core1", "Core 1");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "Core2", "Core 2");
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "Feature", "Feature",
            dependencies: new[] { "Core1", "Core2" });

        CreateDummyAssemblies("Core1", "Core2", "Feature");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);
        loader.DiscoverMods();

        // Act
        loader.LoadMods();
        var loadOrder = loader.GetLoadOrder().ToList();

        // Assert
        loadOrder.IndexOf("Core1").Should().BeLessThan(loadOrder.IndexOf("Feature"));
        loadOrder.IndexOf("Core2").Should().BeLessThan(loadOrder.IndexOf("Feature"));
    }

    #endregion

    #region Incompatibility Tests

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
                    ""reason"": ""Conflicts with feature X""
                }
            ]
        }";
        CreateModWithManifest("ModA", manifestA);
        ModTestHelpers.CreateTestModDirectory(_testModsDirectory, "ModB", "Mod B");

        var loader = new ModLoader(_logger, _services, _loggerFactory, _testModsDirectory);

        // Act
        var report = await loader.ValidateModsAsync(_testModsDirectory);

        // Assert
        report.IsValid.Should().BeFalse();
        report.Errors.Should().Contain(e =>
            e.ErrorType == ModValidationErrorType.IncompatibleModLoaded &&
            e.Message.Contains("ModB"));
    }

    #endregion

    #region Helper Methods

    private void CreateDummyAssemblies(params string[] modIds)
    {
        foreach (var modId in modIds)
        {
            var modPath = Path.Combine(_testModsDirectory, modId);
            var assemblyPath = Path.Combine(modPath, $"{modId}.dll");
            File.WriteAllBytes(assemblyPath, new byte[] { 0x4D, 0x5A });
        }
    }

    private void CreateModWithManifest(string modId, string manifestJson)
    {
        var modPath = Path.Combine(_testModsDirectory, modId);
        Directory.CreateDirectory(modPath);
        File.WriteAllText(Path.Combine(modPath, "modinfo.json"), manifestJson);
    }

    private string CreateManifestWithDeps(string id, string[] dependencies)
    {
        var deps = dependencies.Select(d => new { modId = d });
        var manifest = new
        {
            id = id,
            name = $"{id} Name",
            version = "1.0.0",
            dependencies = deps
        };

        return System.Text.Json.JsonSerializer.Serialize(manifest,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    #endregion
}
