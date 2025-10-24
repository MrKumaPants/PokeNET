using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using PokeNET.Scripting.Abstractions;
using PokeNET.Scripting.Services;
using Xunit;

namespace PokeNET.Tests.RegressionTests.CodeQualityFixes;

/// <summary>
/// Regression tests for Issue #3: Race condition in ScriptCompilationCache.
/// </summary>
public class ScriptCompilationCacheThreadSafetyTests
{
    [Fact]
    public async Task Add_ConcurrentAccess_ShouldNotExceedMaxSize()
    {
        // Arrange
        var logger = Mock.Of<ILogger<ScriptCompilationCache>>();
        var maxCacheSize = 10;
        var cache = new ScriptCompilationCache(logger, maxCacheSize);

        // Create mock compiled scripts
        var mockScript = Mock.Of<ICompiledScript>();

        // Act - Add items concurrently from multiple threads
        var tasks = Enumerable.Range(0, 100).Select(i =>
            Task.Run(() =>
            {
                var hash = $"script-hash-{i}";
                cache.Add(hash, mockScript);
            }));

        await Task.WhenAll(tasks);

        // Assert - Cache should never exceed max size
        var stats = cache.GetStatistics();
        Assert.True(stats.CurrentSize <= maxCacheSize,
            $"Cache size {stats.CurrentSize} exceeded max size {maxCacheSize}");
    }

    [Fact]
    public async Task TryGet_And_Add_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var logger = Mock.Of<ILogger<ScriptCompilationCache>>();
        var cache = new ScriptCompilationCache(logger, 100);
        var mockScript = Mock.Of<ICompiledScript>();
        var errors = new ConcurrentBag<Exception>();

        // Act - Concurrent read and write operations
        var tasks = Enumerable.Range(0, 1000).Select(i =>
            Task.Run(() =>
            {
                try
                {
                    var hash = $"script-{i % 50}"; // Reuse some hashes

                    if (i % 2 == 0)
                    {
                        // Write operation
                        cache.Add(hash, mockScript);
                    }
                    else
                    {
                        // Read operation
                        cache.TryGet(hash, out _);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }));

        await Task.WhenAll(tasks);

        // Assert - No exceptions should occur during concurrent access
        Assert.Empty(errors);
    }

    [Fact]
    public void Add_WithSameHash_ShouldUpdateExisting()
    {
        // Arrange
        var logger = Mock.Of<ILogger<ScriptCompilationCache>>();
        var cache = new ScriptCompilationCache(logger, 10);
        var mockScript1 = Mock.Of<ICompiledScript>();
        var mockScript2 = Mock.Of<ICompiledScript>();
        var hash = "test-hash";

        // Act
        cache.Add(hash, mockScript1);
        cache.Add(hash, mockScript2); // Same hash, should update

        // Assert
        var found = cache.TryGet(hash, out var retrieved);
        Assert.True(found);
        Assert.Same(mockScript2, retrieved); // Should get the updated script
    }

    [Fact]
    public void Add_BeyondMaxSize_ShouldEvictOldest()
    {
        // Arrange
        var logger = Mock.Of<ILogger<ScriptCompilationCache>>();
        var maxSize = 3;
        var cache = new ScriptCompilationCache(logger, maxSize);
        var mockScript = Mock.Of<ICompiledScript>();

        // Act - Add more items than max size
        cache.Add("hash-1", mockScript);
        System.Threading.Thread.Sleep(10); // Ensure different timestamps
        cache.Add("hash-2", mockScript);
        System.Threading.Thread.Sleep(10);
        cache.Add("hash-3", mockScript);
        System.Threading.Thread.Sleep(10);
        cache.Add("hash-4", mockScript); // Should evict hash-1

        // Assert
        Assert.False(cache.TryGet("hash-1", out _), "Oldest entry should be evicted");
        Assert.True(cache.TryGet("hash-2", out _), "Recent entry should exist");
        Assert.True(cache.TryGet("hash-3", out _), "Recent entry should exist");
        Assert.True(cache.TryGet("hash-4", out _), "Newest entry should exist");
    }
}
