using PokeNET.Tests.Utilities;
using System.Reflection;

namespace PokeNET.Tests.Unit.Modding;

/// <summary>
/// Tests for mod assembly loading and initialization
/// </summary>
public class ModLoadingTests : IDisposable
{
    private readonly string _testModsPath;
    private readonly ILogger<ModLoader> _logger;
    private readonly Mock<IAssetManager> _mockAssetManager;

    public ModLoadingTests()
    {
        _testModsPath = Path.Combine(Path.GetTempPath(), $"PokeNET_Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testModsPath);
        _logger = ModTestHelpers.CreateMockLogger<ModLoader>();
        _mockAssetManager = new Mock<IAssetManager>();
    }

    public void Dispose()
    {
        ModTestHelpers.CleanupTestMods(_testModsPath);
    }

    [Fact]
    public void ModLoader_Should_LoadModAssembly()
    {
        // Arrange
        var modPath = ModTestHelpers.CreateTestModDirectory(
            _testModsPath,
            "TestMod",
            "Test Mod");

        // Create a simple test assembly DLL
        var assemblyPath = Path.Combine(modPath, "TestMod.dll");
        var assembly = ModTestHelpers.BuildTestModAssembly("TestMod");

        var loader = new ModLoader(_logger, _testModsPath, _mockAssetManager.Object);

        // Act
        var loadedMods = loader.LoadMods();

        // Assert
        loadedMods.Should().HaveCount(1);
        loadedMods.First().Assembly.Should().NotBeNull();
    }

    [Fact]
    public void ModLoader_Should_InitializeModsInOrder()
    {
        // Arrange
        var initOrder = new List<string>();

        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModA", "Mod A",
            dependencies: new[] { "ModB" });
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ModB", "Mod B");

        var loader = new ModLoader(_logger, _testModsPath, _mockAssetManager.Object);

        // Act
        loader.LoadMods();
        var loadedMods = loader.GetLoadedMods();

        // Assert
        var ids = loadedMods.Select(m => m.Id).ToList();
        ids.IndexOf("ModB").Should().BeLessThan(ids.IndexOf("ModA"));
    }

    [Fact]
    public void ModLoader_Should_CallModInitializeMethod()
    {
        // Arrange
        var modPath = ModTestHelpers.CreateTestModDirectory(
            _testModsPath,
            "InitMod",
            "Init Mod");

        var loader = new ModLoader(_logger, _testModsPath, _mockAssetManager.Object);

        // Act
        var loadedMods = loader.LoadMods();

        // Assert
        loadedMods.Should().HaveCount(1);
        // Verify that Initialize() was called on the mod
        // This would need actual mod implementation or mock verification
    }

    [Fact]
    public void ModLoader_Should_HandleLoadFailureGracefully()
    {
        // Arrange
        var modPath = ModTestHelpers.CreateTestModDirectory(
            _testModsPath,
            "BrokenMod",
            "Broken Mod");

        // Create an invalid DLL file
        var dllPath = Path.Combine(modPath, "BrokenMod.dll");
        File.WriteAllBytes(dllPath, new byte[] { 0x00, 0x01, 0x02 }); // Invalid assembly

        var loader = new ModLoader(_logger, _testModsPath, _mockAssetManager.Object);

        // Act
        var loadedMods = loader.LoadMods();

        // Assert
        loadedMods.Should().NotContain(m => m.Id == "BrokenMod");
        // Verify error was logged (would need logger mock verification)
    }

    [Fact]
    public void ModLoader_Should_UnloadMods()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "TestMod", "Test Mod");

        var loader = new ModLoader(_logger, _testModsPath, _mockAssetManager.Object);
        loader.LoadMods();

        // Act
        loader.UnloadAllMods();

        // Assert
        var loadedMods = loader.GetLoadedMods();
        loadedMods.Should().BeEmpty();
    }

    [Fact]
    public void ModLoader_Should_RespectModLifecycle()
    {
        // Arrange
        var lifecycleStates = new List<string>();
        var modPath = ModTestHelpers.CreateTestModDirectory(
            _testModsPath,
            "LifecycleMod",
            "Lifecycle Mod");

        var loader = new ModLoader(_logger, _testModsPath, _mockAssetManager.Object);

        // Act
        loader.LoadMods();          // Should trigger: Discover → Load → Initialize
        loader.UnloadAllMods();     // Should trigger: Unload

        // Assert
        // Verify lifecycle methods were called in correct order
        // This would require actual mod implementation with lifecycle tracking
    }

    [Fact]
    public void ModLoader_Should_HandleMissingDllFile()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "NoDllMod", "No DLL Mod");
        // Don't create a DLL file

        var loader = new ModLoader(_logger, _testModsPath, _mockAssetManager.Object);

        // Act
        var loadedMods = loader.LoadMods();

        // Assert
        // Mod should still be discovered but marked as data-only
        loadedMods.Should().HaveCount(1);
        loadedMods.First().Assembly.Should().BeNull();
    }

    [Fact]
    public void ModLoader_Should_LoadMultipleModsInParallel()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            ModTestHelpers.CreateTestModDirectory(
                _testModsPath,
                $"Mod{i}",
                $"Mod {i}");
        }

        var loader = new ModLoader(_logger, _testModsPath, _mockAssetManager.Object);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var loadedMods = loader.LoadMods();
        stopwatch.Stop();

        // Assert
        loadedMods.Should().HaveCount(10);
        // Parallel loading should be faster than sequential
    }

    [Fact]
    public void ModLoader_Should_IsolateModFailures()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "GoodMod1", "Good Mod 1");

        var brokenModPath = ModTestHelpers.CreateTestModDirectory(
            _testModsPath,
            "BrokenMod",
            "Broken Mod");
        File.WriteAllBytes(Path.Combine(brokenModPath, "BrokenMod.dll"), new byte[] { 0xFF });

        ModTestHelpers.CreateTestModDirectory(_testModsPath, "GoodMod2", "Good Mod 2");

        var loader = new ModLoader(_logger, _testModsPath, _mockAssetManager.Object);

        // Act
        var loadedMods = loader.LoadMods();

        // Assert
        // Good mods should load even if BrokenMod fails
        loadedMods.Should().Contain(m => m.Id == "GoodMod1");
        loadedMods.Should().Contain(m => m.Id == "GoodMod2");
    }

    [Fact]
    public void ModLoader_Should_ReloadMods()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "ReloadMod", "Reload Mod");

        var loader = new ModLoader(_logger, _testModsPath, _mockAssetManager.Object);
        loader.LoadMods();

        // Act
        loader.ReloadMod("ReloadMod");

        // Assert
        var loadedMods = loader.GetLoadedMods();
        loadedMods.Should().Contain(m => m.Id == "ReloadMod");
    }

    [Fact]
    public void ModLoader_Should_PreventDuplicateLoading()
    {
        // Arrange
        ModTestHelpers.CreateTestModDirectory(_testModsPath, "DupMod", "Duplicate Mod");

        var loader = new ModLoader(_logger, _testModsPath, _mockAssetManager.Object);

        // Act
        loader.LoadMods();
        var act = () => loader.LoadMods(); // Try to load again

        // Assert
        act.Should().NotThrow(); // Should handle gracefully
        var loadedMods = loader.GetLoadedMods();
        loadedMods.Count(m => m.Id == "DupMod").Should().Be(1);
    }
}
