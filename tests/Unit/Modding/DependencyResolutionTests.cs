using PokeNET.Tests.Utilities;

namespace PokeNET.Tests.Unit.Modding;

/// <summary>
/// Tests for mod dependency resolution and load ordering
/// </summary>
public class DependencyResolutionTests : IDisposable
{
    private readonly string _testModsPath;
    private readonly ILogger<ModLoader> _logger;

    public DependencyResolutionTests()
    {
        _testModsPath = Path.Combine(Path.GetTempPath(), $"PokeNET_Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testModsPath);
        _logger = ModTestHelpers.CreateMockLogger<ModLoader>();
    }

    public void Dispose()
    {
        ModTestHelpers.CleanupTestMods(_testModsPath);
    }

    [Fact]
    public void ModLoader_Should_ResolveSimpleDependencyChain()
    {
        // Arrange: A → B → C
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModC", "Mod C");
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModB", "Mod B",
            dependencies: new[] { "ModC" });
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModA", "Mod A",
            dependencies: new[] { "ModB" });

        var loader = new ModLoader(_logger, _testModsPath);

        // Act
        var loadOrder = loader.ResolveLoadOrder();

        // Assert
        loadOrder.Should().HaveCount(3);
        var ids = loadOrder.Select(m => m.Id).ToList();

        ids.IndexOf("ModC").Should().BeLessThan(ids.IndexOf("ModB"));
        ids.IndexOf("ModB").Should().BeLessThan(ids.IndexOf("ModA"));
    }

    [Fact]
    public void ModLoader_Should_ResolveComplexDependencyGraph()
    {
        // Arrange: Complex graph with multiple dependencies
        //   A → B, C
        //   B → D
        //   C → D
        //   D → (none)
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModD", "Mod D");
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModC", "Mod C",
            dependencies: new[] { "ModD" });
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModB", "Mod B",
            dependencies: new[] { "ModD" });
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModA", "Mod A",
            dependencies: new[] { "ModB", "ModC" });

        var loader = new ModLoader(_logger, _testModsPath);

        // Act
        var loadOrder = loader.ResolveLoadOrder();

        // Assert
        loadOrder.Should().HaveCount(4);
        var ids = loadOrder.Select(m => m.Id).ToList();

        // D should load first
        ids[0].Should().Be("ModD");

        // B and C should load after D but before A
        ids.IndexOf("ModB").Should().BeLessThan(ids.IndexOf("ModA"));
        ids.IndexOf("ModC").Should().BeLessThan(ids.IndexOf("ModA"));
    }

    [Fact]
    public void ModLoader_Should_DetectCircularDependencies()
    {
        // Arrange: A → B → C → A (circular)
        ModTestHelpers.CreateCircularDependencyMods(_testModsPath);

        var loader = new ModLoader(_logger, _testModsPath);

        // Act & Assert
        var act = () => loader.ResolveLoadOrder();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*circular*");
    }

    [Fact]
    public void ModLoader_Should_HandleMissingDependency()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModA", "Mod A",
            dependencies: new[] { "NonExistentMod" });

        var loader = new ModLoader(_logger, _testModsPath);

        // Act & Assert
        var act = () => loader.ResolveLoadOrder();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*missing*dependency*");
    }

    [Fact]
    public void ModLoader_Should_RespectLoadAfterHints()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModA", "Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModB", "Mod B",
            loadAfter: new[] { "ModA" });

        var loader = new ModLoader(_logger, _testModsPath);

        // Act
        var loadOrder = loader.ResolveLoadOrder();

        // Assert
        var ids = loadOrder.Select(m => m.Id).ToList();
        ids.IndexOf("ModA").Should().BeLessThan(ids.IndexOf("ModB"));
    }

    [Fact]
    public void ModLoader_Should_RespectLoadBeforeHints()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModA", "Mod A",
            loadBefore: new[] { "ModB" });
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModB", "Mod B");

        var loader = new ModLoader(_logger, _testModsPath);

        // Act
        var loadOrder = loader.ResolveLoadOrder();

        // Assert
        var ids = loadOrder.Select(m => m.Id).ToList();
        ids.IndexOf("ModA").Should().BeLessThan(ids.IndexOf("ModB"));
    }

    [Fact]
    public void ModLoader_Should_HandleIndependentMods()
    {
        // Arrange: Three mods with no dependencies
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModA", "Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModB", "Mod B");
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModC", "Mod C");

        var loader = new ModLoader(_logger, _testModsPath);

        // Act
        var loadOrder = loader.ResolveLoadOrder();

        // Assert
        loadOrder.Should().HaveCount(3);
        // Order can be arbitrary but all should be present
        loadOrder.Should().Contain(m => m.Id == "ModA");
        loadOrder.Should().Contain(m => m.Id == "ModB");
        loadOrder.Should().Contain(m => m.Id == "ModC");
    }

    [Theory]
    [InlineData("CoreMod@1.0.0", "1.0.0", true)]
    [InlineData("CoreMod@1.0.0", "0.9.0", false)]
    [InlineData("CoreMod@1.0.0", "2.0.0", true)]
    public void ModLoader_Should_CheckVersionCompatibility(
        string dependency,
        string actualVersion,
        bool shouldBeCompatible)
    {
        // Arrange
        var corePath = Path.Combine(_testModsPath, "CoreMod");
        Directory.CreateDirectory(corePath);
        var coreManifest = ModTestHelpers.CreateTestManifest(
            "CoreMod",
            "Core Mod",
            version: actualVersion);
        File.WriteAllText(Path.Combine(corePath, "modinfo.json"), coreManifest);

        ModTestHelpers.CreateTestModDirectory(_testModsPath, "DependentMod", "Dependent Mod",
            dependencies: new[] { dependency });

        var loader = new ModLoader(_logger, _testModsPath);

        // Act & Assert
        if (shouldBeCompatible)
        {
            var loadOrder = loader.ResolveLoadOrder();
            loadOrder.Should().HaveCount(2);
        }
        else
        {
            var act = () => loader.ResolveLoadOrder();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*version*");
        }
    }

    [Fact]
    public void ModLoader_Should_HandleDiamondDependency()
    {
        // Arrange: Diamond pattern
        //     A
        //    / \
        //   B   C
        //    \ /
        //     D
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModD", "Mod D");
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModB", "Mod B",
            dependencies: new[] { "ModD" });
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModC", "Mod C",
            dependencies: new[] { "ModD" });
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModA", "Mod A",
            dependencies: new[] { "ModB", "ModC" });

        var loader = new ModLoader(_logger, _testModsPath);

        // Act
        var loadOrder = loader.ResolveLoadOrder();

        // Assert
        loadOrder.Should().HaveCount(4);
        var ids = loadOrder.Select(m => m.Id).ToList();

        // D must load first
        ids[0].Should().Be("ModD");

        // A must load last
        ids[3].Should().Be("ModA");

        // B and C in middle (order doesn't matter)
        ids.Should().Contain("ModB");
        ids.Should().Contain("ModC");
    }

    [Fact]
    public void ModLoader_Should_DetectSelfDependency()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModA", "Mod A",
            dependencies: new[] { "ModA" });

        var loader = new ModLoader(_logger, _testModsPath);

        // Act & Assert
        var act = () => loader.ResolveLoadOrder();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*circular*");
    }
}
