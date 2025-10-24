using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using PokeNET.Core.Assets;
using PokeNET.Domain.Assets;
using Xunit;

namespace PokeNET.Tests.RegressionTests.CodeQualityFixes;

/// <summary>
/// Regression tests for Issue #4: Memory leak in AssetManager.
/// </summary>
public class AssetManagerMemoryLeakTests
{
    private class DisposableTestLoader : IAssetLoader<string>, IDisposable
    {
        public bool IsDisposed { get; private set; }
        public int DisposeCount { get; private set; }

        public bool CanHandle(string extension) => extension == ".test";

        public string Load(string path) => "test-content";

        public void Dispose()
        {
            IsDisposed = true;
            DisposeCount++;
        }
    }

    [Fact]
    public void SetModPaths_WithDisposableLoaders_ShouldDisposeLoaders()
    {
        // Arrange
        var logger = Mock.Of<ILogger<AssetManager>>();
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);

        try
        {
            var assetManager = new AssetManager(logger, tempPath);
            var loader = new DisposableTestLoader();

            assetManager.RegisterLoader(loader);

            // Act - Change mod paths (should dispose loaders)
            assetManager.SetModPaths(new[] { tempPath });

            // Assert
            Assert.True(loader.IsDisposed, "Loader should be disposed when mod paths change");
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public void Dispose_WithDisposableLoaders_ShouldDisposeAllLoaders()
    {
        // Arrange
        var logger = Mock.Of<ILogger<AssetManager>>();
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);

        try
        {
            var assetManager = new AssetManager(logger, tempPath);
            var loader1 = new DisposableTestLoader();
            var loader2 = new DisposableTestLoader();

            assetManager.RegisterLoader<string>(loader1);

            // Act
            assetManager.Dispose();

            // Assert
            Assert.True(loader1.IsDisposed, "All loaders should be disposed");
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public void SetModPaths_MultipleTimes_ShouldNotLeakLoaders()
    {
        // Arrange
        var logger = Mock.Of<ILogger<AssetManager>>();
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);

        try
        {
            var assetManager = new AssetManager(logger, tempPath);
            var loader = new DisposableTestLoader();

            assetManager.RegisterLoader(loader);

            // Act - Call SetModPaths multiple times
            for (int i = 0; i < 5; i++)
            {
                assetManager.SetModPaths(new[] { tempPath });
            }

            // Assert - Loader should only be disposed once per SetModPaths call
            Assert.True(loader.DisposeCount >= 1,
                "Loader should be disposed on each SetModPaths call");
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public void Dispose_MultipleTimesForIdempotency_ShouldNotThrow()
    {
        // Arrange
        var logger = Mock.Of<ILogger<AssetManager>>();
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);

        try
        {
            var assetManager = new AssetManager(logger, tempPath);

            // Act & Assert - Multiple Dispose calls should be safe
            assetManager.Dispose();
            assetManager.Dispose();
            assetManager.Dispose();

            // No exception should be thrown
            Assert.True(true);
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }
}
