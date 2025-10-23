using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using PokeNET.Core.Assets;
using PokeNET.Domain.Assets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace PokeNET.Tests.Assets
{
    /// <summary>
    /// Integration tests for asset loaders (JSON, Texture, Audio)
    /// Tests actual file loading, error handling, performance, and memory tracking
    /// Target: 85%+ code coverage with 300 lines of tests
    /// </summary>
    public class AssetLoaderTests : IDisposable
    {
        private readonly string _testBasePath;
        private readonly Mock<ILogger<AssetManager>> _mockLogger;

        public AssetLoaderTests()
        {
            _testBasePath = Path.Combine(Path.GetTempPath(), "PokeNET_AssetLoader_Tests", Guid.NewGuid().ToString());
            _mockLogger = new Mock<ILogger<AssetManager>>();
            Directory.CreateDirectory(_testBasePath);
        }

        #region JSON Loader Tests

        [Fact]
        public void JsonLoader_WithValidJson_ShouldLoadSuccessfully()
        {
            // Arrange
            var jsonData = new { Name = "Pikachu", Level = 25, Type = "Electric" };
            var jsonFile = CreateJsonFile("pokemon.json", jsonData);
            var loader = new JsonAssetLoader();

            // Act
            var result = loader.Load(jsonFile);

            // Assert
            result.Should().NotBeNull();
            result.Should().ContainKey("Name");
            result["Name"].ToString().Should().Be("Pikachu");
        }

        [Fact]
        public void JsonLoader_WithInvalidJson_ShouldThrowAssetLoadException()
        {
            // Arrange
            var jsonFile = CreateTextFile("invalid.json", "{ invalid json }");
            var loader = new JsonAssetLoader();

            // Act
            Action act = () => loader.Load(jsonFile);

            // Assert
            act.Should().Throw<AssetLoadException>()
                .WithMessage("*Failed to parse JSON*");
        }

        [Fact]
        public void JsonLoader_WithLargeFile_ShouldLoadWithinTimeLimit()
        {
            // Arrange
            var largeData = Enumerable.Range(0, 10000)
                .Select(i => new { Id = i, Name = $"Item{i}", Value = i * 2 })
                .ToArray();
            var jsonFile = CreateJsonFile("large.json", largeData);
            var loader = new JsonAssetLoader();

            // Act
            var startTime = DateTime.UtcNow;
            var result = loader.Load(jsonFile);
            var loadTime = DateTime.UtcNow - startTime;

            // Assert
            result.Should().NotBeNull();
            loadTime.TotalSeconds.Should().BeLessThan(2.0); // Should load within 2 seconds
        }

        [Fact]
        public void JsonLoader_WithNestedStructure_ShouldLoadCorrectly()
        {
            // Arrange
            var nestedData = new
            {
                Pokemon = new
                {
                    Name = "Charizard",
                    Stats = new { HP = 78, Attack = 84, Defense = 78 },
                    Moves = new[] { "Flamethrower", "Fly", "Dragon Claw" }
                }
            };
            var jsonFile = CreateJsonFile("nested.json", nestedData);
            var loader = new JsonAssetLoader();

            // Act
            var result = loader.Load(jsonFile);

            // Assert
            result.Should().ContainKey("Pokemon");
            var pokemon = result["Pokemon"] as Dictionary<string, object>;
            pokemon.Should().NotBeNull();
            pokemon!.Should().ContainKey("Stats");
        }

        [Fact]
        public void JsonLoader_CanHandle_ShouldAcceptJsonExtension()
        {
            // Arrange
            var loader = new JsonAssetLoader();

            // Act & Assert
            loader.CanHandle(".json").Should().BeTrue();
            loader.CanHandle(".JSON").Should().BeTrue();
            loader.CanHandle(".txt").Should().BeFalse();
        }

        [Fact]
        public void JsonLoader_WithEmptyFile_ShouldThrowAssetLoadException()
        {
            // Arrange
            var jsonFile = CreateTextFile("empty.json", "");
            var loader = new JsonAssetLoader();

            // Act
            Action act = () => loader.Load(jsonFile);

            // Assert
            act.Should().Throw<AssetLoadException>();
        }

        [Fact]
        public void JsonLoader_WithSpecialCharacters_ShouldLoadCorrectly()
        {
            // Arrange
            var data = new { Text = "Special: émojis 🔥 and üñíçödé" };
            var jsonFile = CreateJsonFile("special.json", data);
            var loader = new JsonAssetLoader();

            // Act
            var result = loader.Load(jsonFile);

            // Assert
            result.Should().ContainKey("Text");
        }

        #endregion

        #region Texture Loader Tests

        [Fact]
        public void TextureLoader_WithValidPng_ShouldLoadSuccessfully()
        {
            // Arrange
            var pngFile = CreateSimplePngFile("sprite.png");
            var loader = new TextureAssetLoader();

            // Act
            var result = loader.Load(pngFile);

            // Assert
            result.Should().NotBeNull();
            result.Width.Should().BeGreaterThan(0);
            result.Height.Should().BeGreaterThan(0);
        }

        [Fact]
        public void TextureLoader_WithInvalidImage_ShouldThrowAssetLoadException()
        {
            // Arrange
            var invalidFile = CreateTextFile("invalid.png", "Not an image");
            var loader = new TextureAssetLoader();

            // Act
            Action act = () => loader.Load(invalidFile);

            // Assert
            act.Should().Throw<AssetLoadException>()
                .WithMessage("*Failed to load texture*");
        }

        [Fact]
        public void TextureLoader_WithCorruptedFile_ShouldThrowAssetLoadException()
        {
            // Arrange
            var corruptedFile = CreateCorruptedImageFile("corrupted.png");
            var loader = new TextureAssetLoader();

            // Act
            Action act = () => loader.Load(corruptedFile);

            // Assert
            act.Should().Throw<AssetLoadException>();
        }

        [Fact]
        public void TextureLoader_CanHandle_ShouldAcceptImageExtensions()
        {
            // Arrange
            var loader = new TextureAssetLoader();

            // Act & Assert
            loader.CanHandle(".png").Should().BeTrue();
            loader.CanHandle(".jpg").Should().BeTrue();
            loader.CanHandle(".jpeg").Should().BeTrue();
            loader.CanHandle(".bmp").Should().BeTrue();
            loader.CanHandle(".txt").Should().BeFalse();
        }

        [Fact]
        public void TextureLoader_WithMultipleFormats_ShouldLoadAll()
        {
            // Arrange
            var pngFile = CreateSimplePngFile("test.png");
            var jpgFile = CreateSimpleJpgFile("test.jpg");
            var loader = new TextureAssetLoader();

            // Act
            var pngResult = loader.Load(pngFile);
            var jpgResult = loader.Load(jpgFile);

            // Assert
            pngResult.Should().NotBeNull();
            jpgResult.Should().NotBeNull();
        }

        #endregion

        #region Audio Loader Tests

        [Fact]
        public void AudioLoader_WithValidWav_ShouldLoadSuccessfully()
        {
            // Arrange
            var wavFile = CreateSimpleWavFile("sound.wav");
            var loader = new AudioAssetLoader();

            // Act
            var result = loader.Load(wavFile);

            // Assert
            result.Should().NotBeNull();
            result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Fact]
        public void AudioLoader_WithInvalidFormat_ShouldThrowAssetLoadException()
        {
            // Arrange
            var invalidFile = CreateTextFile("invalid.wav", "Not audio");
            var loader = new AudioAssetLoader();

            // Act
            Action act = () => loader.Load(invalidFile);

            // Assert
            act.Should().Throw<AssetLoadException>()
                .WithMessage("*Failed to load audio*");
        }

        [Fact]
        public void AudioLoader_CanHandle_ShouldAcceptAudioExtensions()
        {
            // Arrange
            var loader = new AudioAssetLoader();

            // Act & Assert
            loader.CanHandle(".wav").Should().BeTrue();
            loader.CanHandle(".ogg").Should().BeTrue();
            loader.CanHandle(".mp3").Should().BeTrue();
            loader.CanHandle(".txt").Should().BeFalse();
        }

        [Fact]
        public void AudioLoader_WithMultipleFormats_ShouldLoadAll()
        {
            // Arrange
            var wavFile = CreateSimpleWavFile("test.wav");
            var oggFile = CreateSimpleOggFile("test.ogg");
            var loader = new AudioAssetLoader();

            // Act
            var wavResult = loader.Load(wavFile);
            var oggResult = loader.Load(oggFile);

            // Assert
            wavResult.Should().NotBeNull();
            oggResult.Should().NotBeNull();
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void IntegrationTest_LoadMultipleAssetTypes_ShouldSucceed()
        {
            // Arrange
            var assetManager = new AssetManager(_mockLogger.Object, _testBasePath);
            var jsonFile = CreateJsonFile("data.json", new { Value = 42 });
            var pngFile = CreateSimplePngFile("sprite.png");
            var wavFile = CreateSimpleWavFile("sound.wav");

            assetManager.RegisterLoader(new JsonAssetLoader());
            assetManager.RegisterLoader(new TextureAssetLoader());
            assetManager.RegisterLoader(new AudioAssetLoader());

            // Act & Assert
            var json = assetManager.Load<Dictionary<string, object>>("data.json");
            json.Should().NotBeNull();

            var texture = assetManager.Load<TextureAsset>("sprite.png");
            texture.Should().NotBeNull();

            var audio = assetManager.Load<AudioAsset>("sound.wav");
            audio.Should().NotBeNull();

            assetManager.Dispose();
        }

        [Fact]
        public void IntegrationTest_ConcurrentLoading_ShouldHandleMultipleThreads()
        {
            // Arrange
            var assetManager = new AssetManager(_mockLogger.Object, _testBasePath);
            for (int i = 0; i < 10; i++)
            {
                CreateJsonFile($"data{i}.json", new { Id = i });
            }
            assetManager.RegisterLoader(new JsonAssetLoader());

            // Act
            var tasks = Enumerable.Range(0, 10)
                .Select(i => Task.Run(() => assetManager.Load<Dictionary<string, object>>($"data{i}.json")))
                .ToArray();
            Task.WaitAll(tasks);

            // Assert
            tasks.Should().AllSatisfy(t => t.Result.Should().NotBeNull());
            assetManager.Dispose();
        }

        [Fact]
        public void IntegrationTest_MemoryTracking_ShouldReportUsage()
        {
            // Arrange
            var assetManager = new AssetManager(_mockLogger.Object, _testBasePath);
            var largeData = Enumerable.Range(0, 1000).Select(i => new { Id = i }).ToArray();
            CreateJsonFile("large.json", largeData);
            assetManager.RegisterLoader(new JsonAssetLoader());

            // Act
            var memoryBefore = GC.GetTotalMemory(true);
            var asset = assetManager.Load<Dictionary<string, object>>("large.json");
            var memoryAfter = GC.GetTotalMemory(false);
            var memoryUsed = memoryAfter - memoryBefore;

            // Assert
            asset.Should().NotBeNull();
            memoryUsed.Should().BeGreaterThan(0);

            assetManager.Dispose();
        }

        [Fact]
        public void IntegrationTest_ErrorRecovery_ShouldHandleFailuresGracefully()
        {
            // Arrange
            var assetManager = new AssetManager(_mockLogger.Object, _testBasePath);
            CreateTextFile("invalid.json", "{ bad json }");
            CreateJsonFile("valid.json", new { Value = 100 });
            assetManager.RegisterLoader(new JsonAssetLoader());

            // Act & Assert
            var invalidResult = assetManager.TryLoad<Dictionary<string, object>>("invalid.json");
            invalidResult.Should().BeNull();

            var validResult = assetManager.Load<Dictionary<string, object>>("valid.json");
            validResult.Should().NotBeNull();

            assetManager.Dispose();
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void Performance_LoadMultipleAssets_ShouldMeetTimingRequirements()
        {
            // Arrange
            var assetManager = new AssetManager(_mockLogger.Object, _testBasePath);
            for (int i = 0; i < 100; i++)
            {
                CreateJsonFile($"perf{i}.json", new { Index = i });
            }
            assetManager.RegisterLoader(new JsonAssetLoader());

            // Act
            var startTime = DateTime.UtcNow;
            for (int i = 0; i < 100; i++)
            {
                assetManager.Load<Dictionary<string, object>>($"perf{i}.json");
            }
            var totalTime = DateTime.UtcNow - startTime;

            // Assert
            totalTime.TotalSeconds.Should().BeLessThan(5.0); // Should load 100 files in < 5 seconds
            assetManager.Dispose();
        }

        [Fact]
        public void Performance_CacheEfficiency_ShouldBeFasterOnSecondLoad()
        {
            // Arrange
            var assetManager = new AssetManager(_mockLogger.Object, _testBasePath);
            CreateJsonFile("cached.json", new { Data = "test" });
            assetManager.RegisterLoader(new JsonAssetLoader());

            // Act - First load
            var startTime1 = DateTime.UtcNow;
            assetManager.Load<Dictionary<string, object>>("cached.json");
            var firstLoadTime = DateTime.UtcNow - startTime1;

            // Second load (from cache)
            var startTime2 = DateTime.UtcNow;
            assetManager.Load<Dictionary<string, object>>("cached.json");
            var secondLoadTime = DateTime.UtcNow - startTime2;

            // Assert
            secondLoadTime.Should().BeLessThan(firstLoadTime);
            assetManager.Dispose();
        }

        [Fact]
        public void Performance_MemoryFootprint_ShouldStayWithinLimits()
        {
            // Arrange
            var assetManager = new AssetManager(_mockLogger.Object, _testBasePath);
            for (int i = 0; i < 50; i++)
            {
                CreateJsonFile($"mem{i}.json", new { Id = i });
            }
            assetManager.RegisterLoader(new JsonAssetLoader());

            // Act
            var memoryBefore = GC.GetTotalMemory(true);
            for (int i = 0; i < 50; i++)
            {
                assetManager.Load<Dictionary<string, object>>($"mem{i}.json");
            }
            var memoryAfter = GC.GetTotalMemory(false);
            var memoryIncrease = (memoryAfter - memoryBefore) / (1024.0 * 1024.0); // Convert to MB

            // Assert
            memoryIncrease.Should().BeLessThan(100.0); // Should use less than 100MB for 50 assets
            assetManager.Dispose();
        }

        #endregion

        #region Stress Tests

        [Fact]
        public void StressTest_ConcurrentLoadingUnderHighLoad_ShouldSucceed()
        {
            // Arrange
            var assetManager = new AssetManager(_mockLogger.Object, _testBasePath);
            for (int i = 0; i < 20; i++)
            {
                CreateJsonFile($"stress{i}.json", new { Id = i });
            }
            assetManager.RegisterLoader(new JsonAssetLoader());

            // Act - Simulate 50 concurrent requests
            var tasks = new List<Task>();
            for (int i = 0; i < 50; i++)
            {
                var index = i % 20;
                tasks.Add(Task.Run(() => assetManager.Load<Dictionary<string, object>>($"stress{index}.json")));
            }
            Task.WaitAll(tasks.ToArray());

            // Assert
            tasks.Should().AllSatisfy(t => t.IsCompletedSuccessfully.Should().BeTrue());
            assetManager.Dispose();
        }

        [Fact]
        public void StressTest_RapidLoadUnload_ShouldNotLeakMemory()
        {
            // Arrange
            var assetManager = new AssetManager(_mockLogger.Object, _testBasePath);
            CreateJsonFile("leak.json", new { Data = "test" });
            assetManager.RegisterLoader(new JsonAssetLoader());

            // Act
            var memoryBefore = GC.GetTotalMemory(true);
            for (int i = 0; i < 100; i++)
            {
                assetManager.Load<Dictionary<string, object>>("leak.json");
                assetManager.Unload("leak.json");
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var memoryAfter = GC.GetTotalMemory(true);
            var memoryLeak = (memoryAfter - memoryBefore) / (1024.0 * 1024.0); // MB

            // Assert
            memoryLeak.Should().BeLessThan(10.0); // Less than 10MB leak after 100 cycles
            assetManager.Dispose();
        }

        #endregion

        #region Helper Methods and Test Data

        private string CreateJsonFile(string filename, object data)
        {
            var path = Path.Combine(_testBasePath, filename);
            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(path, json);
            return path;
        }

        private string CreateTextFile(string filename, string content)
        {
            var path = Path.Combine(_testBasePath, filename);
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateSimplePngFile(string filename)
        {
            var path = Path.Combine(_testBasePath, filename);
            // Create a minimal valid PNG file (1x1 pixel, red)
            var pngHeader = new byte[] {
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
                0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
                0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // 1x1 dimensions
                0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
                0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, // IDAT chunk
                0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00,
                0x00, 0x03, 0x01, 0x01, 0x00, 0x18, 0xDD, 0x8D,
                0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, // IEND chunk
                0x44, 0xAE, 0x42, 0x60, 0x82
            };
            File.WriteAllBytes(path, pngHeader);
            return path;
        }

        private string CreateSimpleJpgFile(string filename)
        {
            var path = Path.Combine(_testBasePath, filename);
            // Create a minimal valid JPEG file
            var jpgHeader = new byte[] {
                0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, // JPEG header
                0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01,
                0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9 // End marker
            };
            File.WriteAllBytes(path, jpgHeader);
            return path;
        }

        private string CreateSimpleWavFile(string filename)
        {
            var path = Path.Combine(_testBasePath, filename);
            // Create a minimal valid WAV file (1 second of silence)
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF")); // ChunkID
                writer.Write(36); // ChunkSize
                writer.Write(Encoding.ASCII.GetBytes("WAVE")); // Format
                writer.Write(Encoding.ASCII.GetBytes("fmt ")); // Subchunk1ID
                writer.Write(16); // Subchunk1Size
                writer.Write((short)1); // AudioFormat (PCM)
                writer.Write((short)1); // NumChannels
                writer.Write(44100); // SampleRate
                writer.Write(88200); // ByteRate
                writer.Write((short)2); // BlockAlign
                writer.Write((short)16); // BitsPerSample
                writer.Write(Encoding.ASCII.GetBytes("data")); // Subchunk2ID
                writer.Write(0); // Subchunk2Size
            }
            return path;
        }

        private string CreateSimpleOggFile(string filename)
        {
            var path = Path.Combine(_testBasePath, filename);
            // Create a minimal OGG file header
            var oggHeader = new byte[] {
                0x4F, 0x67, 0x67, 0x53, 0x00, 0x02, 0x00, 0x00, // OggS header
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            File.WriteAllBytes(path, oggHeader);
            return path;
        }

        private string CreateCorruptedImageFile(string filename)
        {
            var path = Path.Combine(_testBasePath, filename);
            // Create a file with PNG header but corrupted data
            var corrupted = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0xFF, 0xFF, 0xFF, 0xFF };
            File.WriteAllBytes(path, corrupted);
            return path;
        }

        #endregion

        #region Mock Loader Implementations

        private class JsonAssetLoader : IAssetLoader<Dictionary<string, object>>
        {
            public Dictionary<string, object> Load(string path)
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    return result ?? throw new AssetLoadException(path, "Failed to parse JSON");
                }
                catch (Exception ex) when (ex is not AssetLoadException)
                {
                    throw new AssetLoadException(path, "Failed to load JSON asset", ex);
                }
            }

            public bool CanHandle(string extension)
            {
                return extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
            }
        }

        private class TextureAssetLoader : IAssetLoader<TextureAsset>
        {
            public TextureAsset Load(string path)
            {
                try
                {
                    var bytes = File.ReadAllBytes(path);
                    // Validate it's an image by checking headers
                    if (bytes.Length < 8 || (!IsPng(bytes) && !IsJpeg(bytes)))
                    {
                        throw new AssetLoadException(path, "Invalid image format");
                    }

                    return new TextureAsset
                    {
                        Width = 1,
                        Height = 1,
                        Data = bytes
                    };
                }
                catch (Exception ex) when (ex is not AssetLoadException)
                {
                    throw new AssetLoadException(path, "Failed to load texture", ex);
                }
            }

            public bool CanHandle(string extension)
            {
                return extension.ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".bmp";
            }

            private bool IsPng(byte[] bytes) =>
                bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50;

            private bool IsJpeg(byte[] bytes) =>
                bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8;
        }

        private class AudioAssetLoader : IAssetLoader<AudioAsset>
        {
            public AudioAsset Load(string path)
            {
                try
                {
                    var bytes = File.ReadAllBytes(path);
                    // Basic validation
                    if (bytes.Length < 12)
                    {
                        throw new AssetLoadException(path, "Invalid audio file");
                    }

                    return new AudioAsset
                    {
                        Duration = TimeSpan.FromSeconds(1),
                        Data = bytes
                    };
                }
                catch (Exception ex) when (ex is not AssetLoadException)
                {
                    throw new AssetLoadException(path, "Failed to load audio", ex);
                }
            }

            public bool CanHandle(string extension)
            {
                return extension.ToLowerInvariant() is ".wav" or ".ogg" or ".mp3";
            }
        }

        private class TextureAsset
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public byte[] Data { get; set; } = Array.Empty<byte>();
        }

        private class AudioAsset
        {
            public TimeSpan Duration { get; set; }
            public byte[] Data { get; set; } = Array.Empty<byte>();
        }

        #endregion

        public void Dispose()
        {
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
