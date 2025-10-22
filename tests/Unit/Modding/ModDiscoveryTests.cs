using PokeNET.Tests.Utilities;
using System.Text.Json;

namespace PokeNET.Tests.Unit.Modding;

/// <summary>
/// Tests for mod discovery and manifest parsing
/// </summary>
public class ModDiscoveryTests : IDisposable
{
    private readonly string _testModsPath;
    private readonly ILogger<ModLoader> _logger;

    public ModDiscoveryTests()
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
    public void ModLoader_Should_DiscoverModsInDirectory()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModA", "Test Mod A");
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModB", "Test Mod B");
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModC", "Test Mod C");

        var loader = new ModLoader(_logger, _testModsPath);

        // Act
        var discoveredMods = loader.DiscoverMods();

        // Assert
        discoveredMods.Should().HaveCount(3);
        discoveredMods.Should().Contain(m => m.Id == "ModA");
        discoveredMods.Should().Contain(m => m.Id == "ModB");
        discoveredMods.Should().Contain(m => m.Id == "ModC");
    }

    [Fact]
    public void ModLoader_Should_ParseValidManifest()
    {
        // Arrange
        var modPath = ModTestHelpers.CreateTestModDirectory(
            _testModsPath,
            "TestMod",
            "Test Mod",
            dependencies: new[] { "CoreMod" });

        var loader = new ModLoader(_logger, _testModsPath);

        // Act
        var discoveredMods = loader.DiscoverMods();
        var testMod = discoveredMods.FirstOrDefault(m => m.Id == "TestMod");

        // Assert
        testMod.Should().NotBeNull();
        testMod!.Name.Should().Be("Test Mod");
        testMod.Version.Should().Be("1.0.0");
        testMod.Author.Should().Be("Test Author");
        testMod.Dependencies.Should().Contain("CoreMod");
    }

    [Fact]
    public void ModLoader_Should_HandleInvalidJSON()
    {
        // Arrange
        var modPath = Path.Combine(_testModsPath, "InvalidMod");
        Directory.CreateDirectory(modPath);
        File.WriteAllText(
            Path.Combine(modPath, "modinfo.json"),
            ModTestHelpers.CreateInvalidManifest());

        var loader = new ModLoader(_logger, _testModsPath);

        // Act
        var discoveredMods = loader.DiscoverMods();

        // Assert
        discoveredMods.Should().NotContain(m => m.Id == "InvalidMod");
    }

    [Fact]
    public void ModLoader_Should_HandleMissingManifest()
    {
        // Arrange
        var modPath = Path.Combine(_testModsPath, "NoManifest");
        Directory.CreateDirectory(modPath);
        // Don't create modinfo.json

        var loader = new ModLoader(_logger, _testModsPath);

        // Act
        var discoveredMods = loader.DiscoverMods();

        // Assert
        discoveredMods.Should().NotContain(m => m.Id == "NoManifest");
    }

    [Fact]
    public void ModLoader_Should_ValidateRequiredFields()
    {
        // Arrange
        var modPath = Path.Combine(_testModsPath, "IncompleteMod");
        Directory.CreateDirectory(modPath);
        File.WriteAllText(
            Path.Combine(modPath, "modinfo.json"),
            ModTestHelpers.CreateIncompleteManifest());

        var loader = new ModLoader(_logger, _testModsPath);

        // Act
        var discoveredMods = loader.DiscoverMods();

        // Assert
        discoveredMods.Should().NotContain(m => m.Id == "IncompleteMod");
    }

    [Fact]
    public void ModLoader_Should_SkipEmptyDirectories()
    {
        // Arrange
        var emptyPath = Path.Combine(_testModsPath, "EmptyDir");
        Directory.CreateDirectory(emptyPath);

        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ValidMod", "Valid Mod");

        var loader = new ModLoader(_logger, _testModsPath);

        // Act
        var discoveredMods = loader.DiscoverMods();

        // Assert
        discoveredMods.Should().HaveCount(1);
        discoveredMods.Should().Contain(m => m.Id == "ValidMod");
    }

    [Fact]
    public void ModLoader_Should_ParseLoadAfterAndLoadBefore()
    {
        // Arrange
        var modPath = ModTestHelpers.CreateTestModDirectory(
            _testModsPath,
            "OrderedMod",
            "Ordered Mod",
            loadAfter: new[] { "ModA" },
            loadBefore: new[] { "ModB" });

        var loader = new ModLoader(_logger, _testModsPath);

        // Act
        var discoveredMods = loader.DiscoverMods();
        var orderedMod = discoveredMods.FirstOrDefault(m => m.Id == "OrderedMod");

        // Assert
        orderedMod.Should().NotBeNull();
        orderedMod!.LoadAfter.Should().Contain("ModA");
        orderedMod.LoadBefore.Should().Contain("ModB");
    }

    [Fact]
    public void ModLoader_Should_HandleNonExistentModsDirectory()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var loader = new ModLoader(_logger, nonExistentPath);

        // Act
        var discoveredMods = loader.DiscoverMods();

        // Assert
        discoveredMods.Should().BeEmpty();
    }

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("2.5.3")]
    [InlineData("10.20.30")]
    public void ModLoader_Should_ParseVersionNumbers(string version)
    {
        // Arrange
        var manifestPath = Path.Combine(_testModsPath, "VersionTest");
        Directory.CreateDirectory(manifestPath);

        var manifest = ModTestHelpers.CreateTestManifest(
            "VersionTest",
            "Version Test",
            version: version);

        File.WriteAllText(Path.Combine(manifestPath, "modinfo.json"), manifest);

        var loader = new ModLoader(_logger, _testModsPath);

        // Act
        var discoveredMods = loader.DiscoverMods();
        var versionMod = discoveredMods.FirstOrDefault(m => m.Id == "VersionTest");

        // Assert
        versionMod.Should().NotBeNull();
        versionMod!.Version.Should().Be(version);
    }
}
