using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using PokeNET.Core.Assets;
using PokeNET.Domain.Assets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PokeNET.Tests.Assets
{
    /// <summary>
    /// Comprehensive tests for AssetManager
    /// Tests loading, caching, mod support, thread safety, and memory management
    /// Target: 85%+ code coverage with 300 lines of tests
    /// </summary>
    public class AssetManagerTests : IDisposable
    {
        private readonly Mock<ILogger<AssetManager>> _mockLogger;
        private readonly string _testBasePath;
        private readonly string _testModPath;
        private AssetManager? _assetManager;

        public AssetManagerTests()
        {
            _mockLogger = new Mock<ILogger<AssetManager>>();
            _testBasePath = Path.Combine(Path.GetTempPath(), "PokeNET_Tests", Guid.NewGuid().ToString());
            _testModPath = Path.Combine(_testBasePath, "Mods");

            // Create test directories
            Directory.CreateDirectory(_testBasePath);
            Directory.CreateDirectory(_testModPath);
        }

        #region Initialization Tests

        [Fact]
        public void Constructor_ShouldCreateBasePathIfNotExists()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testBasePath, "NonExistent");

            // Act
            _assetManager = new AssetManager(_mockLogger.Object, nonExistentPath);

            // Assert
            Directory.Exists(nonExistentPath).Should().BeTrue();
            _assetManager.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act
            Action act = () => new AssetManager(null!, _testBasePath);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void Constructor_WithValidBasePath_ShouldInitialize()
        {
            // Act
            _assetManager = new AssetManager(_mockLogger.Object, _testBasePath);

            // Assert
            _assetManager.Should().NotBeNull();
        }

        #endregion

        #region Loader Registration Tests

        [Fact]
        public void RegisterLoader_WithValidLoader_ShouldRegisterSuccessfully()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            var mockLoader = new Mock<IAssetLoader<TestAsset>>();
            mockLoader.Setup(l => l.CanHandle(It.IsAny<string>())).Returns(true);

            // Act
            _assetManager.RegisterLoader(mockLoader.Object);

            // Assert - should not throw
            _assetManager.Should().NotBeNull();
        }

        [Fact]
        public void RegisterLoader_WithNullLoader_ShouldThrowArgumentNullException()
        {
            // Arrange
            _assetManager = CreateAssetManager();

            // Act
            Action act = () => _assetManager.RegisterLoader<TestAsset>(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void RegisterLoader_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            _assetManager.Dispose();
            var mockLoader = new Mock<IAssetLoader<TestAsset>>();

            // Act
            Action act = () => _assetManager.RegisterLoader(mockLoader.Object);

            // Assert
            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void RegisterLoader_MultipleTypes_ShouldRegisterAll()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            var mockLoader1 = new Mock<IAssetLoader<TestAsset>>();
            var mockLoader2 = new Mock<IAssetLoader<TestAsset2>>();

            // Act
            _assetManager.RegisterLoader(mockLoader1.Object);
            _assetManager.RegisterLoader(mockLoader2.Object);

            // Assert - should not throw
            _assetManager.Should().NotBeNull();
        }

        #endregion

        #region Asset Loading Tests

        [Fact]
        public void Load_WithValidAsset_ShouldLoadSuccessfully()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            var testFile = CreateTestFile("test.json", "{\"data\":\"value\"}");
            var mockLoader = CreateMockLoader<TestAsset>(".json", new TestAsset { Data = "value" });
            _assetManager.RegisterLoader(mockLoader.Object);

            // Act
            var asset = _assetManager.Load<TestAsset>("test.json");

            // Assert
            asset.Should().NotBeNull();
            asset.Data.Should().Be("value");
        }

        [Fact]
        public void Load_WithNullPath_ShouldThrowArgumentException()
        {
            // Arrange
            _assetManager = CreateAssetManager();

            // Act
            Action act = () => _assetManager.Load<TestAsset>(null!);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Load_WithEmptyPath_ShouldThrowArgumentException()
        {
            // Arrange
            _assetManager = CreateAssetManager();

            // Act
            Action act = () => _assetManager.Load<TestAsset>(string.Empty);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Load_WithNonExistentAsset_ShouldThrowAssetLoadException()
        {
            // Arrange
            _assetManager = CreateAssetManager();

            // Act
            Action act = () => _assetManager.Load<TestAsset>("nonexistent.json");

            // Assert
            act.Should().Throw<AssetLoadException>()
                .WithMessage("*Asset not found*");
        }

        [Fact]
        public void Load_WithoutRegisteredLoader_ShouldThrowAssetLoadException()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            CreateTestFile("test.json", "{}");

            // Act
            Action act = () => _assetManager.Load<TestAsset>("test.json");

            // Assert
            act.Should().Throw<AssetLoadException>()
                .WithMessage("*No asset loader registered*");
        }

        [Fact]
        public void Load_WithIncompatibleExtension_ShouldThrowAssetLoadException()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            CreateTestFile("test.txt", "data");
            var mockLoader = CreateMockLoader<TestAsset>(".json", new TestAsset());
            _assetManager.RegisterLoader(mockLoader.Object);

            // Act
            Action act = () => _assetManager.Load<TestAsset>("test.txt");

            // Assert
            act.Should().Throw<AssetLoadException>()
                .WithMessage("*cannot handle extension*");
        }

        [Fact]
        public void Load_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            _assetManager.Dispose();

            // Act
            Action act = () => _assetManager.Load<TestAsset>("test.json");

            // Assert
            act.Should().Throw<ObjectDisposedException>();
        }

        #endregion

        #region Caching Tests

        [Fact]
        public void Load_SameAssetTwice_ShouldReturnCachedInstance()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            CreateTestFile("cached.json", "{}");
            var mockLoader = CreateMockLoader<TestAsset>(".json", new TestAsset());
            _assetManager.RegisterLoader(mockLoader.Object);

            // Act
            var asset1 = _assetManager.Load<TestAsset>("cached.json");
            var asset2 = _assetManager.Load<TestAsset>("cached.json");

            // Assert
            asset1.Should().BeSameAs(asset2);
            mockLoader.Verify(l => l.Load(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void IsLoaded_WithCachedAsset_ShouldReturnTrue()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            CreateTestFile("test.json", "{}");
            var mockLoader = CreateMockLoader<TestAsset>(".json", new TestAsset());
            _assetManager.RegisterLoader(mockLoader.Object);
            _assetManager.Load<TestAsset>("test.json");

            // Act
            var isLoaded = _assetManager.IsLoaded("test.json");

            // Assert
            isLoaded.Should().BeTrue();
        }

        [Fact]
        public void IsLoaded_WithoutLoadedAsset_ShouldReturnFalse()
        {
            // Arrange
            _assetManager = CreateAssetManager();

            // Act
            var isLoaded = _assetManager.IsLoaded("test.json");

            // Assert
            isLoaded.Should().BeFalse();
        }

        [Fact]
        public void IsLoaded_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            _assetManager.Dispose();

            // Act
            Action act = () => _assetManager.IsLoaded("test.json");

            // Assert
            act.Should().Throw<ObjectDisposedException>();
        }

        #endregion

        #region Unloading Tests

        [Fact]
        public void Unload_WithLoadedAsset_ShouldRemoveFromCache()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            CreateTestFile("test.json", "{}");
            var mockLoader = CreateMockLoader<TestAsset>(".json", new TestAsset());
            _assetManager.RegisterLoader(mockLoader.Object);
            _assetManager.Load<TestAsset>("test.json");

            // Act
            _assetManager.Unload("test.json");

            // Assert
            _assetManager.IsLoaded("test.json").Should().BeFalse();
        }

        [Fact]
        public void Unload_WithDisposableAsset_ShouldDisposeAsset()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            CreateTestFile("test.json", "{}");
            var disposableAsset = new DisposableTestAsset();
            var mockLoader = CreateMockLoader<DisposableTestAsset>(".json", disposableAsset);
            _assetManager.RegisterLoader(mockLoader.Object);
            _assetManager.Load<DisposableTestAsset>("test.json");

            // Act
            _assetManager.Unload("test.json");

            // Assert
            disposableAsset.IsDisposed.Should().BeTrue();
        }

        [Fact]
        public void Unload_WithNonExistentAsset_ShouldNotThrow()
        {
            // Arrange
            _assetManager = CreateAssetManager();

            // Act
            Action act = () => _assetManager.Unload("nonexistent.json");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void UnloadAll_ShouldClearAllCachedAssets()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            CreateTestFile("asset1.json", "{}");
            CreateTestFile("asset2.json", "{}");
            var mockLoader = CreateMockLoader<TestAsset>(".json", new TestAsset());
            _assetManager.RegisterLoader(mockLoader.Object);
            _assetManager.Load<TestAsset>("asset1.json");
            _assetManager.Load<TestAsset>("asset2.json");

            // Act
            _assetManager.UnloadAll();

            // Assert
            _assetManager.IsLoaded("asset1.json").Should().BeFalse();
            _assetManager.IsLoaded("asset2.json").Should().BeFalse();
        }

        [Fact]
        public void UnloadAll_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            _assetManager.Dispose();

            // Act
            Action act = () => _assetManager.UnloadAll();

            // Assert
            act.Should().Throw<ObjectDisposedException>();
        }

        #endregion

        #region Mod Path Tests

        [Fact]
        public void SetModPaths_WithValidPaths_ShouldSetSuccessfully()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            var modPath1 = Path.Combine(_testModPath, "Mod1");
            var modPath2 = Path.Combine(_testModPath, "Mod2");
            Directory.CreateDirectory(modPath1);
            Directory.CreateDirectory(modPath2);

            // Act
            _assetManager.SetModPaths(new[] { modPath1, modPath2 });

            // Assert - should not throw
            _assetManager.Should().NotBeNull();
        }

        [Fact]
        public void SetModPaths_WithNullPaths_ShouldThrowArgumentNullException()
        {
            // Arrange
            _assetManager = CreateAssetManager();

            // Act
            Action act = () => _assetManager.SetModPaths(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Load_WithModOverride_ShouldLoadFromModPath()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            var modPath = Path.Combine(_testModPath, "TestMod");
            Directory.CreateDirectory(modPath);

            CreateTestFile("base.json", "{\"data\":\"base\"}");
            File.WriteAllText(Path.Combine(modPath, "base.json"), "{\"data\":\"modded\"}");

            var mockLoader = new Mock<IAssetLoader<TestAsset>>();
            mockLoader.Setup(l => l.CanHandle(".json")).Returns(true);
            mockLoader.Setup(l => l.Load(It.IsAny<string>()))
                .Returns<string>(path => new TestAsset { Data = File.ReadAllText(path).Contains("modded") ? "modded" : "base" });

            _assetManager.RegisterLoader(mockLoader.Object);
            _assetManager.SetModPaths(new[] { modPath });

            // Act
            var asset = _assetManager.Load<TestAsset>("base.json");

            // Assert
            asset.Data.Should().Be("modded");
        }

        [Fact]
        public void SetModPaths_ShouldClearExistingCache()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            CreateTestFile("test.json", "{}");
            var mockLoader = CreateMockLoader<TestAsset>(".json", new TestAsset());
            _assetManager.RegisterLoader(mockLoader.Object);
            _assetManager.Load<TestAsset>("test.json");
            var modPath = Path.Combine(_testModPath, "Mod1");
            Directory.CreateDirectory(modPath);

            // Act
            _assetManager.SetModPaths(new[] { modPath });

            // Assert
            _assetManager.IsLoaded("test.json").Should().BeFalse();
        }

        #endregion

        #region TryLoad Tests

        [Fact]
        public void TryLoad_WithValidAsset_ShouldReturnAsset()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            CreateTestFile("test.json", "{}");
            var mockLoader = CreateMockLoader<TestAsset>(".json", new TestAsset { Data = "value" });
            _assetManager.RegisterLoader(mockLoader.Object);

            // Act
            var asset = _assetManager.TryLoad<TestAsset>("test.json");

            // Assert
            asset.Should().NotBeNull();
            asset!.Data.Should().Be("value");
        }

        [Fact]
        public void TryLoad_WithNonExistentAsset_ShouldReturnNull()
        {
            // Arrange
            _assetManager = CreateAssetManager();

            // Act
            var asset = _assetManager.TryLoad<TestAsset>("nonexistent.json");

            // Assert
            asset.Should().BeNull();
        }

        [Fact]
        public void TryLoad_WithLoadError_ShouldReturnNull()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            CreateTestFile("test.json", "{}");
            var mockLoader = new Mock<IAssetLoader<TestAsset>>();
            mockLoader.Setup(l => l.CanHandle(".json")).Returns(true);
            mockLoader.Setup(l => l.Load(It.IsAny<string>())).Throws<IOException>();
            _assetManager.RegisterLoader(mockLoader.Object);

            // Act
            var asset = _assetManager.TryLoad<TestAsset>("test.json");

            // Assert
            asset.Should().BeNull();
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public async Task Load_ConcurrentSameAsset_ShouldHandleThreadSafely()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            CreateTestFile("concurrent.json", "{}");
            var loadCount = 0;
            var mockLoader = new Mock<IAssetLoader<TestAsset>>();
            mockLoader.Setup(l => l.CanHandle(".json")).Returns(true);
            mockLoader.Setup(l => l.Load(It.IsAny<string>()))
                .Returns(() =>
                {
                    Interlocked.Increment(ref loadCount);
                    Thread.Sleep(10); // Simulate loading time
                    return new TestAsset();
                });
            _assetManager.RegisterLoader(mockLoader.Object);

            // Act
            var tasks = Enumerable.Range(0, 10)
                .Select(_ => Task.Run(() => _assetManager.Load<TestAsset>("concurrent.json")))
                .ToArray();
            await Task.WhenAll(tasks);

            // Assert - should load only once due to caching
            tasks.Select(t => t.Result).Distinct().Should().HaveCount(1);
        }

        [Fact]
        public async Task Load_ConcurrentDifferentAssets_ShouldLoadAllSuccessfully()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            var fileCount = 5;
            for (int i = 0; i < fileCount; i++)
            {
                CreateTestFile($"asset{i}.json", "{}");
            }
            var mockLoader = CreateMockLoader<TestAsset>(".json", new TestAsset());
            _assetManager.RegisterLoader(mockLoader.Object);

            // Act
            var tasks = Enumerable.Range(0, fileCount)
                .Select(i => Task.Run(() => _assetManager.Load<TestAsset>($"asset{i}.json")))
                .ToArray();
            await Task.WhenAll(tasks);

            // Assert
            tasks.Should().AllSatisfy(t => t.Result.Should().NotBeNull());
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_ShouldUnloadAllAssets()
        {
            // Arrange
            _assetManager = CreateAssetManager();
            CreateTestFile("test.json", "{}");
            var disposableAsset = new DisposableTestAsset();
            var mockLoader = CreateMockLoader<DisposableTestAsset>(".json", disposableAsset);
            _assetManager.RegisterLoader(mockLoader.Object);
            _assetManager.Load<DisposableTestAsset>("test.json");

            // Act
            _assetManager.Dispose();

            // Assert
            disposableAsset.IsDisposed.Should().BeTrue();
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldBeIdempotent()
        {
            // Arrange
            _assetManager = CreateAssetManager();

            // Act
            _assetManager.Dispose();
            Action act = () => _assetManager.Dispose();

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region Helper Methods

        private AssetManager CreateAssetManager()
        {
            return new AssetManager(_mockLogger.Object, _testBasePath);
        }

        private string CreateTestFile(string relativePath, string content)
        {
            var fullPath = Path.Combine(_testBasePath, relativePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(fullPath, content);
            return fullPath;
        }

        private Mock<IAssetLoader<T>> CreateMockLoader<T>(string extension, T asset) where T : class
        {
            var mockLoader = new Mock<IAssetLoader<T>>();
            mockLoader.Setup(l => l.CanHandle(extension)).Returns(true);
            mockLoader.Setup(l => l.Load(It.IsAny<string>())).Returns(asset);
            return mockLoader;
        }

        #endregion

        #region Test Asset Classes

        private class TestAsset
        {
            public string Data { get; set; } = string.Empty;
        }

        private class TestAsset2
        {
            public string Value { get; set; } = string.Empty;
        }

        private class DisposableTestAsset : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        #endregion

        public void Dispose()
        {
            _assetManager?.Dispose();

            // Clean up test directories
            try
            {
                if (Directory.Exists(_testBasePath))
                {
                    Directory.Delete(_testBasePath, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
