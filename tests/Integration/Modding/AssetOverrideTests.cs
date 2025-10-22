using PokeNET.Tests.Utilities;

namespace PokeNET.Tests.Integration.Modding;

/// <summary>
/// Integration tests for mod asset loading and override system
/// </summary>
public class AssetOverrideTests : IDisposable
{
    private readonly string _testModsPath;
    private readonly string _baseGamePath;
    private readonly ILogger<ModLoader> _logger;
    private readonly ILogger<AssetManager> _assetLogger;

    public AssetOverrideTests()
    {
        _testModsPath = Path.Combine(Path.GetTempPath(), $"PokeNET_Tests_{Guid.NewGuid()}");
        _baseGamePath = Path.Combine(Path.GetTempPath(), $"PokeNET_BaseGame_{Guid.NewGuid()}");

        Directory.CreateDirectory(_testModsPath);
        Directory.CreateDirectory(_baseGamePath);

        _logger = ModTestHelpers.CreateMockLogger<ModLoader>();
        _assetLogger = ModTestHelpers.CreateMockLogger<AssetManager>();
    }

    public void Dispose()
    {
        ModTestHelpers.CleanupTestMods(_testModsPath);
        ModTestHelpers.CleanupTestMods(_baseGamePath);
    }

    [Fact]
    public void AssetManager_Should_LoadModAssetOverBaseGame()
    {
        // Arrange
        var baseCreatureData = new { Id = "pikachu", Name = "Pikachu", Type = "electric" };
        ModTestHelpers.CreateTestDataFile(_baseGamePath, "Data/creatures.json",
            new[] { baseCreatureData });

        var modPath = ModTestHelpers.CreateTestModDirectory(_testModsPath, "ElectricMod", "Electric Mod");
        var modCreatureData = new { Id = "pikachu", Name = "Super Pikachu", Type = "electric", Power = 100 };
        ModTestHelpers.CreateTestDataFile(modPath, "Data/creatures.json",
            new[] { modCreatureData });

        var assetManager = new AssetManager(_assetLogger, _baseGamePath);
        var modLoader = new ModLoader(_logger, _testModsPath, assetManager);

        modLoader.LoadMods();

        // Act
        var creatureData = assetManager.LoadJson<dynamic>("Data/creatures.json");

        // Assert
        creatureData.Should().NotBeNull();
        ((string)creatureData[0].Name).Should().Be("Super Pikachu");
        ((int)creatureData[0].Power).Should().Be(100);
    }

    [Fact]
    public void AssetManager_Should_PrioritizeModsByLoadOrder()
    {
        // Arrange
        var modAPath = ModTestHelpers.CreateTestModDirectory(
            _testModsPath, "ModA", "Mod A",
            dependencies: new[] { "ModB" });

        var modBPath = ModTestHelpers.CreateTestModDirectory(
            _testModsPath, "ModB", "Mod B");

        ModTestHelpers.CreateTestDataFile(modAPath, "Data/test.json",
            new { Value = "From Mod A" });
        ModTestHelpers.CreateTestDataFile(modBPath, "Data/test.json",
            new { Value = "From Mod B" });

        var assetManager = new AssetManager(_assetLogger, _baseGamePath);
        var modLoader = new ModLoader(_logger, _testModsPath, assetManager);

        modLoader.LoadMods();

        // Act
        var data = assetManager.LoadJson<dynamic>("Data/test.json");

        // Assert
        // ModA loads after ModB, so it should override
        ((string)data.Value).Should().Be("From Mod A");
    }

    [Fact]
    public void AssetManager_Should_FallbackToBaseGame()
    {
        // Arrange
        var baseData = new { Id = "baseAsset", Value = "Base Game" };
        ModTestHelpers.CreateTestDataFile(_baseGamePath, "Data/base.json",
            new[] { baseData });

        var modPath = ModTestHelpers.CreateTestModDirectory(_testModsPath, "TestMod", "Test Mod");
        // Mod doesn't override this asset

        var assetManager = new AssetManager(_assetLogger, _baseGamePath);
        var modLoader = new ModLoader(_logger, _testModsPath, assetManager);

        modLoader.LoadMods();

        // Act
        var data = assetManager.LoadJson<dynamic>("Data/base.json");

        // Assert
        data.Should().NotBeNull();
        ((string)data[0].Value).Should().Be("Base Game");
    }

    [Fact]
    public void AssetManager_Should_HandleMultipleModAssets()
    {
        // Arrange
        var modAPath = ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModA", "Mod A");
        var modBPath = ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModB", "Mod B");

        ModTestHelpers.CreateTestDataFile(modAPath, "Data/modAOnly.json",
            new { Value = "Mod A Asset" });
        ModTestHelpers.CreateTestDataFile(modBPath, "Data/modBOnly.json",
            new { Value = "Mod B Asset" });

        var assetManager = new AssetManager(_assetLogger, _baseGamePath);
        var modLoader = new ModLoader(_logger, _testModsPath, assetManager);

        modLoader.LoadMods();

        // Act
        var assetA = assetManager.LoadJson<dynamic>("Data/modAOnly.json");
        var assetB = assetManager.LoadJson<dynamic>("Data/modBOnly.json");

        // Assert
        assetA.Should().NotBeNull();
        assetB.Should().NotBeNull();
        ((string)assetA.Value).Should().Be("Mod A Asset");
        ((string)assetB.Value).Should().Be("Mod B Asset");
    }

    [Fact]
    public void AssetManager_Should_OverrideContentFiles()
    {
        // Arrange
        var baseSpritePath = Path.Combine(_baseGamePath, "Content/sprites/pikachu.png");
        Directory.CreateDirectory(Path.GetDirectoryName(baseSpritePath)!);
        File.WriteAllBytes(baseSpritePath, new byte[] { 0x01, 0x02, 0x03 }); // Base sprite

        var modPath = ModTestHelpers.CreateTestModDirectory(_testModsPath, "SpriteMod", "Sprite Mod");
        ModTestHelpers.CreateTestContentFile(modPath, "Content/sprites/pikachu.png");

        var assetManager = new AssetManager(_assetLogger, _baseGamePath);
        var modLoader = new ModLoader(_logger, _testModsPath, assetManager);

        modLoader.LoadMods();

        // Act
        var spritePath = assetManager.ResolvePath("Content/sprites/pikachu.png");
        var spriteBytes = File.ReadAllBytes(spritePath);

        // Assert
        spriteBytes.Should().NotEqual(new byte[] { 0x01, 0x02, 0x03 });
        spriteBytes.Should().StartWith(new byte[] { 0x50, 0x4E, 0x47 }); // PNG signature from mod
    }

    [Fact]
    public void AssetManager_Should_CacheAssets()
    {
        // Arrange
        var modPath = ModTestHelpers.CreateTestModDirectory(_testModsPath, "CacheMod", "Cache Mod");
        ModTestHelpers.CreateTestDataFile(modPath, "Data/cached.json",
            new { Value = "Cached Data" });

        var assetManager = new AssetManager(_assetLogger, _baseGamePath);
        var modLoader = new ModLoader(_logger, _testModsPath, assetManager);

        modLoader.LoadMods();

        // Act
        var firstLoad = assetManager.LoadJson<dynamic>("Data/cached.json");
        var secondLoad = assetManager.LoadJson<dynamic>("Data/cached.json");

        // Assert
        ReferenceEquals(firstLoad, secondLoad).Should().BeTrue("asset should be cached");
    }

    [Fact]
    public void AssetManager_Should_HandleMissingAsset()
    {
        // Arrange
        var assetManager = new AssetManager(_assetLogger, _baseGamePath);
        var modLoader = new ModLoader(_logger, _testModsPath, assetManager);

        modLoader.LoadMods();

        // Act & Assert
        var act = () => assetManager.LoadJson<dynamic>("Data/nonexistent.json");

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void AssetManager_Should_MergeDataFromMultipleMods()
    {
        // Arrange
        var modAPath = ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModA", "Mod A");
        var modBPath = ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModB", "Mod B");

        ModTestHelpers.CreateTestDataFile(modAPath, "Data/creatures.json",
            new[] { new { Id = "creature1", Name = "Creature A" } });

        ModTestHelpers.CreateTestDataFile(modBPath, "Data/creatures.json",
            new[] { new { Id = "creature2", Name = "Creature B" } });

        var assetManager = new AssetManager(_assetLogger, _baseGamePath, mergeMode: AssetMergeMode.Combine);
        var modLoader = new ModLoader(_logger, _testModsPath, assetManager);

        modLoader.LoadMods();

        // Act
        var creatures = assetManager.LoadJson<dynamic[]>("Data/creatures.json");

        // Assert
        creatures.Should().HaveCount(2);
        creatures.Should().Contain(c => c.Id == "creature1");
        creatures.Should().Contain(c => c.Id == "creature2");
    }

    [Fact]
    public void AssetManager_Should_ReloadModifiedAsset()
    {
        // Arrange
        var modPath = ModTestHelpers.CreateTestModDirectory(_testModsPath, "ReloadMod", "Reload Mod");
        var assetPath = Path.Combine(modPath, "Data/reload.json");

        ModTestHelpers.CreateTestDataFile(modPath, "Data/reload.json",
            new { Value = "Original" });

        var assetManager = new AssetManager(_assetLogger, _baseGamePath);
        var modLoader = new ModLoader(_logger, _testModsPath, assetManager);

        modLoader.LoadMods();

        var firstLoad = assetManager.LoadJson<dynamic>("Data/reload.json");

        // Act - Modify asset and reload
        ModTestHelpers.CreateTestDataFile(modPath, "Data/reload.json",
            new { Value = "Modified" });

        assetManager.ReloadAsset("Data/reload.json");
        var secondLoad = assetManager.LoadJson<dynamic>("Data/reload.json");

        // Assert
        ((string)firstLoad.Value).Should().Be("Original");
        ((string)secondLoad.Value).Should().Be("Modified");
    }
}
