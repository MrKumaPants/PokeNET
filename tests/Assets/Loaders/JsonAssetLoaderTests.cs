using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using PokeNET.Core.Assets.Loaders;
using PokeNET.Domain.Assets;
using Xunit;

namespace PokeNET.Tests.Assets.Loaders;

/// <summary>
/// Comprehensive tests for JsonAssetLoader covering valid loading, error handling,
/// streaming, caching, and concurrent access scenarios.
/// </summary>
public class JsonAssetLoaderTests : IDisposable
{
    private readonly Mock<ILogger<JsonAssetLoader<TestData>>> _mockLogger;
    private readonly Mock<ILogger<JsonAssetLoader<List<TestData>>>> _mockListLogger;
    private readonly Mock<ILogger<JsonAssetLoader<NestedTestData>>> _mockNestedLogger;
    private readonly string _testDirectory;
    private readonly List<string> _testFiles;

    public JsonAssetLoaderTests()
    {
        _mockLogger = new Mock<ILogger<JsonAssetLoader<TestData>>>();
        _mockListLogger = new Mock<ILogger<JsonAssetLoader<List<TestData>>>>();
        _mockNestedLogger = new Mock<ILogger<JsonAssetLoader<NestedTestData>>>();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"JsonAssetLoaderTests_{Guid.NewGuid()}");
        _testFiles = new List<string>();
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        // Clean up test files
        foreach (var file in _testFiles)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    #region Valid JSON Loading Tests

    [Fact]
    public void Load_ValidSimpleJson_ReturnsDeserializedObject()
    {
        // Arrange
        var loader = new JsonAssetLoader<TestData>(_mockLogger.Object);
        var testData = new TestData { Id = 1, Name = "Test", Value = 42.5 };
        var jsonPath = CreateTestJsonFile(testData);

        // Act
        var result = loader.Load(jsonPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testData.Id, result.Id);
        Assert.Equal(testData.Name, result.Name);
        Assert.Equal(testData.Value, result.Value);
    }

    [Fact]
    public void Load_JsonWithComments_SuccessfullyDeserializes()
    {
        // Arrange
        var loader = new JsonAssetLoader<TestData>(_mockLogger.Object);
        var json = @"{
            // This is a comment
            ""id"": 1,
            ""name"": ""Test"", // Inline comment
            ""value"": 42.5
        }";
        var jsonPath = CreateTestFile("test_with_comments.json", json);

        // Act
        var result = loader.Load(jsonPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void Load_JsonWithTrailingCommas_SuccessfullyDeserializes()
    {
        // Arrange
        var loader = new JsonAssetLoader<TestData>(_mockLogger.Object);
        var json = @"{
            ""id"": 1,
            ""name"": ""Test"",
            ""value"": 42.5,
        }";
        var jsonPath = CreateTestFile("test_trailing_comma.json", json);

        // Act
        var result = loader.Load(jsonPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void Load_CaseInsensitiveProperties_SuccessfullyDeserializes()
    {
        // Arrange
        var loader = new JsonAssetLoader<TestData>(_mockLogger.Object);
        var json = @"{
            ""ID"": 1,
            ""NAME"": ""Test"",
            ""VALUE"": 42.5
        }";
        var jsonPath = CreateTestFile("test_case_insensitive.json", json);

        // Act
        var result = loader.Load(jsonPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void Load_JsonArray_SuccessfullyDeserializes()
    {
        // Arrange
        var loader = new JsonAssetLoader<List<TestData>>(_mockListLogger.Object);
        var json = @"[
            {""id"": 1, ""name"": ""Test1"", ""value"": 10.0},
            {""id"": 2, ""name"": ""Test2"", ""value"": 20.0}
        ]";
        var jsonPath = CreateTestFile("test_array.json", json);

        // Act
        var result = loader.Load(jsonPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
    }

    [Fact]
    public void Load_NestedObjects_SuccessfullyDeserializes()
    {
        // Arrange
        var loader = new JsonAssetLoader<NestedTestData>(_mockNestedLogger.Object);
        var json = @"{
            ""id"": 1,
            ""nested"": {
                ""id"": 2,
                ""name"": ""Nested"",
                ""value"": 99.9
            }
        }";
        var jsonPath = CreateTestFile("test_nested.json", json);

        // Act
        var result = loader.Load(jsonPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.NotNull(result.Nested);
        Assert.Equal(2, result.Nested.Id);
        Assert.Equal("Nested", result.Nested.Name);
    }

    #endregion

    #region Caching Tests

    [Fact]
    public void Load_SameFileTwice_ReturnsCachedInstance()
    {
        // Arrange
        var loader = new JsonAssetLoader<TestData>(_mockLogger.Object);
        var testData = new TestData { Id = 1, Name = "Test", Value = 42.5 };
        var jsonPath = CreateTestJsonFile(testData);

        // Act
        var result1 = loader.Load(jsonPath);
        var result2 = loader.Load(jsonPath);

        // Assert
        Assert.Same(result1, result2); // Should be the same instance due to caching
        Assert.Equal(1, loader.CacheSize);
    }

    [Fact]
    public void ClearCache_RemovesAllCachedEntries()
    {
        // Arrange
        var loader = new JsonAssetLoader<TestData>(_mockLogger.Object);
        var jsonPath1 = CreateTestJsonFile(new TestData { Id = 1, Name = "Test1" });
        var jsonPath2 = CreateTestJsonFile(new TestData { Id = 2, Name = "Test2" });

        loader.Load(jsonPath1);
        loader.Load(jsonPath2);

        // Act
        loader.ClearCache();

        // Assert
        Assert.Equal(0, loader.CacheSize);
        Assert.False(loader.IsCached(jsonPath1));
        Assert.False(loader.IsCached(jsonPath2));
    }

    [Fact]
    public void IsCached_LoadedFile_ReturnsTrue()
    {
        // Arrange
        var loader = new JsonAssetLoader<TestData>(_mockLogger.Object);
        var jsonPath = CreateTestJsonFile(new TestData { Id = 1 });

        // Act
        loader.Load(jsonPath);

        // Assert
        Assert.True(loader.IsCached(jsonPath));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void Load_FileNotFound_ThrowsAssetLoadException()
    {
        // Arrange
        var loader = new JsonAssetLoader<TestData>(_mockLogger.Object);
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");

        // Act & Assert
        var exception = Assert.Throws<AssetLoadException>(() => loader.Load(nonExistentPath));
        Assert.Contains("not found", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(nonExistentPath, exception.AssetPath);
    }

    [Fact]
    public void Load_MalformedJson_ThrowsAssetLoadExceptionWithLineNumber()
    {
        // Arrange
        var loader = new JsonAssetLoader<TestData>(_mockLogger.Object);
        var json = @"{
            ""id"": 1,
            ""name"": ""Test"",
            ""value"": INVALID
        }";
        var jsonPath = CreateTestFile("malformed.json", json);

        // Act & Assert
        var exception = Assert.Throws<AssetLoadException>(() => loader.Load(jsonPath));
        Assert.Contains("Malformed JSON", exception.Message);
        Assert.Contains("line", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(exception.InnerException);
        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Fact]
    public void Load_InvalidJsonSyntax_ThrowsAssetLoadException()
    {
        // Arrange
        var loader = new JsonAssetLoader<TestData>(_mockLogger.Object);
        var json = @"{
            ""id"": 1,
            ""name"": ""Test""
            ""value"": 42.5
        }"; // Missing comma
        var jsonPath = CreateTestFile("invalid_syntax.json", json);

        // Act & Assert
        var exception = Assert.Throws<AssetLoadException>(() => loader.Load(jsonPath));
        Assert.Contains("Malformed JSON", exception.Message);
    }

    [Fact]
    public void Load_NullOrWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var loader = new JsonAssetLoader<TestData>(_mockLogger.Object);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => loader.Load(null!));
        Assert.Throws<ArgumentException>(() => loader.Load(""));
        Assert.Throws<ArgumentException>(() => loader.Load("   "));
    }

    [Fact]
    public void Load_TypeMismatch_ThrowsAssetLoadException()
    {
        // Arrange
        var loader = new JsonAssetLoader<List<TestData>>(_mockListLogger.Object);
        var json = @"{""id"": 1, ""name"": ""Test""}"; // Object instead of array
        var jsonPath = CreateTestFile("type_mismatch.json", json);

        // Act & Assert
        var exception = Assert.Throws<AssetLoadException>(() => loader.Load(jsonPath));
        Assert.Contains("Type mismatch", exception.Message);
    }

    [Fact]
    public void Load_EmptyJson_ThrowsAssetLoadException()
    {
        // Arrange
        var loader = new JsonAssetLoader<TestData>(_mockLogger.Object);
        var jsonPath = CreateTestFile("empty.json", "");

        // Act & Assert
        var exception = Assert.Throws<AssetLoadException>(() => loader.Load(jsonPath));
        Assert.NotNull(exception.InnerException);
    }

    #endregion

    #region Streaming Tests

    [Fact]
    public void Load_LargeFile_UsesStreamingDeserialization()
    {
        // Arrange
        var streamingThreshold = 100; // Low threshold to trigger streaming
        var loader = new JsonAssetLoader<List<TestData>>(_mockListLogger.Object, streamingThreshold);

        // Create a JSON file larger than threshold
        var largeData = new List<TestData>();
        for (int i = 0; i < 100; i++)
        {
            largeData.Add(new TestData { Id = i, Name = $"Test{i}", Value = i * 1.5 });
        }
        var jsonPath = CreateTestJsonFile(largeData);

        // Verify file is larger than threshold
        var fileInfo = new FileInfo(jsonPath);
        Assert.True(fileInfo.Length > streamingThreshold);

        // Act
        var result = loader.Load(jsonPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.Count);
        Assert.Equal(0, result[0].Id);
        Assert.Equal(99, result[99].Id);
    }

    [Fact]
    public void Load_SmallFile_UsesSynchronousDeserialization()
    {
        // Arrange
        var streamingThreshold = 1_000_000; // High threshold
        var loader = new JsonAssetLoader<TestData>(_mockLogger.Object, streamingThreshold);
        var testData = new TestData { Id = 1, Name = "Small", Value = 1.0 };
        var jsonPath = CreateTestJsonFile(testData);

        // Verify file is smaller than threshold
        var fileInfo = new FileInfo(jsonPath);
        Assert.True(fileInfo.Length < streamingThreshold);

        // Act
        var result = loader.Load(jsonPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    #endregion

    #region CanHandle Tests

    [Theory]
    [InlineData(".json", true)]
    [InlineData("json", true)]
    [InlineData(".JSON", true)]
    [InlineData("JSON", true)]
    [InlineData(".txt", false)]
    [InlineData(".xml", false)]
    [InlineData(".png", false)]
    public void CanHandle_VariousExtensions_ReturnsExpectedResult(string extension, bool expected)
    {
        // Arrange
        var loader = new JsonAssetLoader<TestData>(_mockLogger.Object);

        // Act
        var result = loader.CanHandle(extension);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CanHandle_EmptyString_ThrowsArgumentException()
    {
        // Arrange
        var loader = new JsonAssetLoader<TestData>(_mockLogger.Object);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => loader.CanHandle(""));
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public void Load_ConcurrentAccess_ThreadSafe()
    {
        // Arrange
        var loader = new JsonAssetLoader<TestData>(_mockLogger.Object);
        var testFiles = new List<string>();

        // Create multiple test files
        for (int i = 0; i < 10; i++)
        {
            var testData = new TestData { Id = i, Name = $"Test{i}", Value = i * 10.0 };
            testFiles.Add(CreateTestJsonFile(testData));
        }

        var results = new System.Collections.Concurrent.ConcurrentBag<TestData>();
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Act - Load files concurrently
        Parallel.ForEach(testFiles, file =>
        {
            try
            {
                var result = loader.Load(file);
                results.Add(result);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(10, results.Count);
        Assert.Equal(10, loader.CacheSize);
    }

    [Fact]
    public void Load_ConcurrentAccessSameFile_ReturnsSameInstance()
    {
        // Arrange
        var loader = new JsonAssetLoader<TestData>(_mockLogger.Object);
        var testData = new TestData { Id = 1, Name = "Concurrent", Value = 99.9 };
        var jsonPath = CreateTestJsonFile(testData);

        var results = new System.Collections.Concurrent.ConcurrentBag<TestData>();

        // Act - Load same file concurrently
        Parallel.For(0, 10, _ =>
        {
            var result = loader.Load(jsonPath);
            results.Add(result);
        });

        // Assert
        Assert.Equal(10, results.Count);
        var firstResult = results.First();
        Assert.All(results, r => Assert.Same(firstResult, r)); // All should be same instance
    }

    #endregion

    #region Helper Methods and Test Data Classes

    private string CreateTestJsonFile<T>(T data)
    {
        var fileName = $"test_{Guid.NewGuid()}.json";
        var filePath = Path.Combine(_testDirectory, fileName);
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
        _testFiles.Add(filePath);
        return filePath;
    }

    private string CreateTestFile(string fileName, string content)
    {
        var filePath = Path.Combine(_testDirectory, fileName);
        File.WriteAllText(filePath, content);
        _testFiles.Add(filePath);
        return filePath;
    }

    #endregion

    #region Test Data Classes

    public class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
    }

    public class NestedTestData
    {
        public int Id { get; set; }
        public TestData? Nested { get; set; }
    }

    #endregion
}
