using PokeNET.Tests.Utilities;
using HarmonyLib;

namespace PokeNET.Tests.Integration.Modding;

/// <summary>
/// End-to-end integration tests for the complete mod system
/// </summary>
public class ModSystemIntegrationTests : IDisposable
{
    private readonly string _testModsPath;
    private readonly string _baseGamePath;
    private readonly ILogger<ModLoader> _logger;
    private readonly ILogger<AssetManager> _assetLogger;

    public ModSystemIntegrationTests()
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
    public void ModSystem_Should_LoadCompleteModWorkflow()
    {
        // Arrange: Create a complete mod with dependencies
        var corePath = ModTestHelpers.CreateTestModDirectory(
            _testModsPath, "CoreMod", "Core Mod");

        var expansionPath = ModTestHelpers.CreateTestModDirectory(
            _testModsPath, "ExpansionMod", "Expansion Mod",
            dependencies: new[] { "CoreMod" });

        ModTestHelpers.CreateTestDataFile(corePath, "Data/creatures.json",
            new[] { new { Id = "pikachu", Name = "Pikachu" } });

        ModTestHelpers.CreateTestDataFile(expansionPath, "Data/moves.json",
            new[] { new { Id = "thunderbolt", Name = "Thunderbolt" } });

        var assetManager = new AssetManager(_assetLogger, _baseGamePath);
        var modLoader = new ModLoader(_logger, _testModsPath, assetManager);

        // Act
        modLoader.LoadMods();

        // Assert
        var loadedMods = modLoader.GetLoadedMods();
        loadedMods.Should().HaveCount(2);

        var creatures = assetManager.LoadJson<dynamic[]>("Data/creatures.json");
        var moves = assetManager.LoadJson<dynamic[]>("Data/moves.json");

        creatures.Should().NotBeEmpty();
        moves.Should().NotBeEmpty();
    }

    [Fact]
    public void ModSystem_Should_HandleMultiModScenarioWithDependencies()
    {
        // Arrange: Complex scenario with 5 mods
        //   BaseMod (no deps)
        //   GraphicsMod → BaseMod
        //   SoundMod → BaseMod
        //   GameplayMod → BaseMod, GraphicsMod
        //   BalanceMod → GameplayMod

        ModTestHelpers.CreateTestModDirectory(_testModsPath, "BaseMod", "Base Mod");

        ModTestHelpers.CreateTestModDirectory(_testModsPath, "GraphicsMod", "Graphics Mod",
            dependencies: new[] { "BaseMod" });

        ModTestHelpers.CreateTestModDirectory(_testModsPath, "SoundMod", "Sound Mod",
            dependencies: new[] { "BaseMod" });

        ModTestHelpers.CreateTestModDirectory(_testModsPath, "GameplayMod", "Gameplay Mod",
            dependencies: new[] { "BaseMod", "GraphicsMod" });

        ModTestHelpers.CreateTestModDirectory(_testModsPath, "BalanceMod", "Balance Mod",
            dependencies: new[] { "GameplayMod" });

        var assetManager = new AssetManager(_assetLogger, _baseGamePath);
        var modLoader = new ModLoader(_logger, _testModsPath, assetManager);

        // Act
        modLoader.LoadMods();

        // Assert
        var loadedMods = modLoader.GetLoadedMods();
        loadedMods.Should().HaveCount(5);

        var ids = loadedMods.Select(m => m.Id).ToList();

        // Verify load order
        ids.IndexOf("BaseMod").Should().BeLessThan(ids.IndexOf("GraphicsMod"));
        ids.IndexOf("GraphicsMod").Should().BeLessThan(ids.IndexOf("GameplayMod"));
        ids.IndexOf("GameplayMod").Should().BeLessThan(ids.IndexOf("BalanceMod"));
    }

    [Fact]
    public void ModSystem_Should_HandleDataContentCodeMod()
    {
        // Arrange: A mod that includes all three types
        var modPath = ModTestHelpers.CreateTestModDirectory(
            _testModsPath, "CompleteMod", "Complete Mod");

        // Add data
        ModTestHelpers.CreateTestDataFile(modPath, "Data/config.json",
            new { Setting = "value" });

        // Add content
        ModTestHelpers.CreateTestContentFile(modPath, "Content/sprite.png");

        // Add code (assembly)
        var assemblyPath = Path.Combine(modPath, "CompleteMod.dll");
        // In real scenario, this would be an actual compiled assembly

        var assetManager = new AssetManager(_assetLogger, _baseGamePath);
        var modLoader = new ModLoader(_logger, _testModsPath, assetManager);

        // Act
        modLoader.LoadMods();

        // Assert
        var loadedMods = modLoader.GetLoadedMods();
        loadedMods.Should().ContainSingle();

        var config = assetManager.LoadJson<dynamic>("Data/config.json");
        config.Should().NotBeNull();

        var spritePath = assetManager.ResolvePath("Content/sprite.png");
        File.Exists(spritePath).Should().BeTrue();
    }

    [Fact]
    public void ModSystem_Should_HandleConflictsGracefully()
    {
        // Arrange: Two mods that conflict
        var modAPath = ModTestHelpers.CreateTestModDirectory(
            _testModsPath, "ConflictA", "Conflict Mod A");

        var modBPath = ModTestHelpers.CreateTestModDirectory(
            _testModsPath, "ConflictB", "Conflict Mod B");

        // Both try to override the same data
        ModTestHelpers.CreateTestDataFile(modAPath, "Data/shared.json",
            new { Value = "From Mod A", Priority = 1 });

        ModTestHelpers.CreateTestDataFile(modBPath, "Data/shared.json",
            new { Value = "From Mod B", Priority = 2 });

        var assetManager = new AssetManager(_assetLogger, _baseGamePath);
        var modLoader = new ModLoader(_logger, _testModsPath, assetManager);

        // Act
        modLoader.LoadMods();

        // Assert - Should not crash, one mod wins
        var data = assetManager.LoadJson<dynamic>("Data/shared.json");
        data.Should().NotBeNull();
        // Last loaded mod should win (alphabetical order: ConflictB)
        ((string)data.Value).Should().Be("From Mod B");
    }

    [Fact]
    public void ModSystem_Should_RecoverFromModLoadFailure()
    {
        // Arrange
        var goodModPath = ModTestHelpers.CreateTestModDirectory(
            _testModsPath, "GoodMod", "Good Mod");

        var badModPath = Path.Combine(_testModsPath, "BadMod");
        Directory.CreateDirectory(badModPath);
        File.WriteAllText(Path.Combine(badModPath, "modinfo.json"),
            "{ invalid json ");

        var anotherGoodPath = ModTestHelpers.CreateTestModDirectory(
            _testModsPath, "AnotherGoodMod", "Another Good Mod");

        ModTestHelpers.CreateTestDataFile(goodModPath, "Data/test1.json",
            new { Id = 1 });
        ModTestHelpers.CreateTestDataFile(anotherGoodPath, "Data/test2.json",
            new { Id = 2 });

        var assetManager = new AssetManager(_assetLogger, _baseGamePath);
        var modLoader = new ModLoader(_logger, _testModsPath, assetManager);

        // Act
        modLoader.LoadMods();

        // Assert - Good mods should still load
        var loadedMods = modLoader.GetLoadedMods();
        loadedMods.Should().Contain(m => m.Id == "GoodMod");
        loadedMods.Should().Contain(m => m.Id == "AnotherGoodMod");
        loadedMods.Should().NotContain(m => m.Id == "BadMod");
    }

    [Fact]
    public void ModSystem_Should_IntegrateWithHarmony()
    {
        // Arrange
        var modPath = ModTestHelpers.CreateTestModDirectory(
            _testModsPath, "HarmonyMod", "Harmony Integration Mod");

        var assetManager = new AssetManager(_assetLogger, _baseGamePath);
        var modLoader = new ModLoader(_logger, _testModsPath, assetManager);

        var harmony = HarmonyTestHelpers.CreateTestHarmonyInstance("integration-test");

        // Simulate mod applying Harmony patches
        var targetMethod = typeof(HarmonyTestHelpers.TestPatchTarget)
            .GetMethod(nameof(HarmonyTestHelpers.TestPatchTarget.TestMethod))!;

        var prefixMethod = typeof(HarmonyTestHelpers.SamplePrefixPatch)
            .GetMethod(nameof(HarmonyTestHelpers.SamplePrefixPatch.Prefix))!;

        // Act
        modLoader.LoadMods();
        harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefixMethod));

        var result = HarmonyTestHelpers.TestPatchTarget.TestMethod("test");

        // Assert
        HarmonyTestHelpers.SamplePrefixPatch.Applied.Should().BeTrue();
        result.Should().NotBeNull();

        // Cleanup
        HarmonyTestHelpers.UnpatchAll(harmony);
    }

    [Fact]
    public void ModSystem_Should_UnloadAllModsCleanly()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            ModTestHelpers.CreateTestModDirectory(
                _testModsPath, $"Mod{i}", $"Mod {i}");
        }

        var assetManager = new AssetManager(_assetLogger, _baseGamePath);
        var modLoader = new ModLoader(_logger, _testModsPath, assetManager);

        modLoader.LoadMods();

        // Act
        modLoader.UnloadAllMods();

        // Assert
        var loadedMods = modLoader.GetLoadedMods();
        loadedMods.Should().BeEmpty();
        assetManager.GetCachedAssetCount().Should().Be(0);
    }

    [Fact]
    public void ModSystem_Should_ReloadModsAfterChanges()
    {
        // Arrange
        var modPath = ModTestHelpers.CreateTestModDirectory(
            _testModsPath, "ReloadMod", "Reload Test Mod");

        ModTestHelpers.CreateTestDataFile(modPath, "Data/version.json",
            new { Version = 1 });

        var assetManager = new AssetManager(_assetLogger, _baseGamePath);
        var modLoader = new ModLoader(_logger, _testModsPath, assetManager);

        modLoader.LoadMods();
        var firstVersion = assetManager.LoadJson<dynamic>("Data/version.json");

        // Act - Modify mod data
        ModTestHelpers.CreateTestDataFile(modPath, "Data/version.json",
            new { Version = 2 });

        modLoader.ReloadMod("ReloadMod");
        var secondVersion = assetManager.LoadJson<dynamic>("Data/version.json");

        // Assert
        ((int)firstVersion.Version).Should().Be(1);
        ((int)secondVersion.Version).Should().Be(2);
    }

    [Fact]
    public void ModSystem_Should_ProvideModMetrics()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            var modPath = ModTestHelpers.CreateTestModDirectory(
                _testModsPath, $"Mod{i}", $"Mod {i}");

            ModTestHelpers.CreateTestDataFile(modPath, $"Data/data{i}.json",
                new { Id = i });
        }

        var assetManager = new AssetManager(_assetLogger, _baseGamePath);
        var modLoader = new ModLoader(_logger, _testModsPath, assetManager);

        // Act
        var loadStartTime = DateTime.UtcNow;
        modLoader.LoadMods();
        var loadDuration = DateTime.UtcNow - loadStartTime;

        var metrics = modLoader.GetLoadMetrics();

        // Assert
        metrics.TotalModsDiscovered.Should().Be(10);
        metrics.TotalModsLoaded.Should().Be(10);
        metrics.LoadDuration.Should().BeLessThan(TimeSpan.FromSeconds(5));
        metrics.FailedMods.Should().BeEmpty();
    }
}
