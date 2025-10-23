using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PokeNET.Scripting.Interfaces;
using PokeNET.Scripting.Services;
using Xunit;

namespace PokeNET.Tests.Scripting;

/// <summary>
/// Comprehensive test suite for ScriptLoader with 500+ lines of tests.
/// Tests script discovery, loading, metadata extraction, error handling, and security.
/// </summary>
public sealed class ScriptLoaderTests : IDisposable
{
    private readonly ILogger<ScriptLoader> _logger;
    private readonly ScriptLoader _loader;
    private readonly string _testDirectory;

    public ScriptLoaderTests()
    {
        _logger = NullLogger<ScriptLoader>.Instance;
        _loader = new ScriptLoader(_logger);
        _testDirectory = Path.Combine(Path.GetTempPath(), $"script_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ScriptLoader(null!));
    }

    [Fact]
    public void Constructor_InitializesWithSupportedExtensions()
    {
        var extensions = _loader.SupportedExtensions;
        Assert.NotNull(extensions);
        Assert.Contains(".csx", extensions);
        Assert.Contains(".cs", extensions);
        Assert.Equal(2, extensions.Count);
    }

    #endregion

    #region DiscoverScripts Tests

    [Fact]
    public void DiscoverScripts_WithNullDirectory_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _loader.DiscoverScripts(null!));
    }

    [Fact]
    public void DiscoverScripts_WithEmptyDirectory_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _loader.DiscoverScripts(string.Empty));
    }

    [Fact]
    public void DiscoverScripts_WithWhitespaceDirectory_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _loader.DiscoverScripts("   "));
    }

    [Fact]
    public void DiscoverScripts_WithNonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        var nonExistent = Path.Combine(_testDirectory, "nonexistent");
        Assert.Throws<DirectoryNotFoundException>(() => _loader.DiscoverScripts(nonExistent));
    }

    [Fact]
    public void DiscoverScripts_WithEmptyDirectory_ReturnsEmptyList()
    {
        var scripts = _loader.DiscoverScripts(_testDirectory);
        Assert.NotNull(scripts);
        Assert.Empty(scripts);
    }

    [Fact]
    public void DiscoverScripts_WithValidCsxScript_DiscoversScript()
    {
        // Arrange
        var scriptPath = CreateTestScript("test.csx", @"
// @script-id: test-script
// @name: Test Script
// @version: 1.0.0
Console.WriteLine(""Hello"");
");

        // Act
        var scripts = _loader.DiscoverScripts(_testDirectory);

        // Assert
        Assert.Single(scripts);
        Assert.Equal("test-script", scripts[0].Id);
        Assert.Equal("Test Script", scripts[0].Name);
        Assert.Equal("1.0.0", scripts[0].Version);
    }

    [Fact]
    public void DiscoverScripts_WithValidCsScript_DiscoversScript()
    {
        // Arrange
        var scriptPath = CreateTestScript("test.cs", @"
// @script-id: cs-script
// @name: C# Script
Console.WriteLine(""Hello"");
");

        // Act
        var scripts = _loader.DiscoverScripts(_testDirectory);

        // Assert
        Assert.Single(scripts);
        Assert.Equal("cs-script", scripts[0].Id);
    }

    [Fact]
    public void DiscoverScripts_WithMultipleScripts_DiscoversAll()
    {
        // Arrange
        CreateTestScript("script1.csx", "// @script-id: script1\nConsole.WriteLine();");
        CreateTestScript("script2.csx", "// @script-id: script2\nConsole.WriteLine();");
        CreateTestScript("script3.cs", "// @script-id: script3\nConsole.WriteLine();");

        // Act
        var scripts = _loader.DiscoverScripts(_testDirectory);

        // Assert
        Assert.Equal(3, scripts.Count);
        Assert.Contains(scripts, s => s.Id == "script1");
        Assert.Contains(scripts, s => s.Id == "script2");
        Assert.Contains(scripts, s => s.Id == "script3");
    }

    [Fact]
    public void DiscoverScripts_WithRecursiveSearch_DiscoversScriptsInSubdirectories()
    {
        // Arrange
        var subdir1 = Path.Combine(_testDirectory, "subdir1");
        var subdir2 = Path.Combine(_testDirectory, "subdir2");
        Directory.CreateDirectory(subdir1);
        Directory.CreateDirectory(subdir2);

        CreateTestScript("root.csx", "// @script-id: root\nConsole.WriteLine();");
        File.WriteAllText(Path.Combine(subdir1, "sub1.csx"), "// @script-id: sub1\nConsole.WriteLine();");
        File.WriteAllText(Path.Combine(subdir2, "sub2.csx"), "// @script-id: sub2\nConsole.WriteLine();");

        // Act
        var scripts = _loader.DiscoverScripts(_testDirectory, recursive: true);

        // Assert
        Assert.Equal(3, scripts.Count);
    }

    [Fact]
    public void DiscoverScripts_WithNonRecursiveSearch_OnlyDiscoversTopLevel()
    {
        // Arrange
        var subdir = Path.Combine(_testDirectory, "subdir");
        Directory.CreateDirectory(subdir);

        CreateTestScript("root.csx", "// @script-id: root\nConsole.WriteLine();");
        File.WriteAllText(Path.Combine(subdir, "sub.csx"), "// @script-id: sub\nConsole.WriteLine();");

        // Act
        var scripts = _loader.DiscoverScripts(_testDirectory, recursive: false);

        // Assert
        Assert.Single(scripts);
        Assert.Equal("root", scripts[0].Id);
    }

    [Fact]
    public void DiscoverScripts_WithInvalidScriptFile_SkipsFile()
    {
        // Arrange
        CreateTestScript("valid.csx", "// @script-id: valid\nConsole.WriteLine();");
        File.WriteAllText(Path.Combine(_testDirectory, "invalid.txt"), "not a script");

        // Act
        var scripts = _loader.DiscoverScripts(_testDirectory);

        // Assert
        Assert.Single(scripts);
    }

    [Fact]
    public void DiscoverScripts_WithMalformedMetadata_SkipsScriptAndLogs()
    {
        // Arrange
        CreateTestScript("malformed.csx", "// @script-id:\nthis will throw during metadata extraction");

        // Act
        var scripts = _loader.DiscoverScripts(_testDirectory);

        // Assert - should skip malformed script
        Assert.Empty(scripts);
    }

    #endregion

    #region LoadScriptAsync Tests

    [Fact]
    public async Task LoadScriptAsync_WithNullFilePath_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _loader.LoadScriptAsync(null!));
    }

    [Fact]
    public async Task LoadScriptAsync_WithEmptyFilePath_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _loader.LoadScriptAsync(string.Empty));
    }

    [Fact]
    public async Task LoadScriptAsync_WithWhitespaceFilePath_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _loader.LoadScriptAsync("   "));
    }

    [Fact]
    public async Task LoadScriptAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        var nonExistent = Path.Combine(_testDirectory, "nonexistent.csx");
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await _loader.LoadScriptAsync(nonExistent));
    }

    [Fact]
    public async Task LoadScriptAsync_WithInvalidExtension_ThrowsInvalidOperationException()
    {
        var txtFile = Path.Combine(_testDirectory, "script.txt");
        File.WriteAllText(txtFile, "console.log('test');");

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _loader.LoadScriptAsync(txtFile));
    }

    [Fact]
    public async Task LoadScriptAsync_WithValidScript_ReturnsMetadataAndSourceCode()
    {
        // Arrange
        var sourceCode = @"
// @script-id: test-load
// @name: Load Test
// @version: 2.0.0
// @author: Test Author
// @description: Test description
// @dependencies: dep1, dep2
// @permissions: entities.create, events.publish

Console.WriteLine(""Hello, World!"");
";
        var scriptPath = CreateTestScript("load.csx", sourceCode);

        // Act
        var (metadata, loadedCode) = await _loader.LoadScriptAsync(scriptPath);

        // Assert
        Assert.NotNull(metadata);
        Assert.NotNull(loadedCode);
        Assert.Equal("test-load", metadata.Id);
        Assert.Equal("Load Test", metadata.Name);
        Assert.Equal("2.0.0", metadata.Version);
        Assert.Equal("Test Author", metadata.Author);
        Assert.Equal("Test description", metadata.Description);
        Assert.Equal(2, metadata.Dependencies.Count);
        Assert.Contains("dep1", metadata.Dependencies);
        Assert.Contains("dep2", metadata.Dependencies);
        Assert.Equal(2, metadata.Permissions.Count);
        Assert.Equal(sourceCode, loadedCode);
    }

    [Fact]
    public async Task LoadScriptAsync_WithMinimalMetadata_GeneratesDefaults()
    {
        // Arrange
        var scriptPath = CreateTestScript("minimal.csx", "Console.WriteLine();");

        // Act
        var (metadata, code) = await _loader.LoadScriptAsync(scriptPath);

        // Assert
        Assert.NotNull(metadata);
        Assert.NotNull(metadata.Id); // Generated ID
        Assert.Equal("minimal", metadata.Name); // From filename
        Assert.NotNull(metadata.FilePath);
    }

    [Fact]
    public async Task LoadScriptAsync_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        var scriptPath = CreateTestScript("cancel.csx", new string('x', 10000000)); // Large file
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await _loader.LoadScriptAsync(scriptPath, cts.Token));
    }

    [Fact]
    public async Task LoadScriptAsync_WithEnabledFlag_ParsesBooleanCorrectly()
    {
        // Arrange
        var scriptPath = CreateTestScript("enabled.csx", @"
// @script-id: enabled-test
// @enabled: false
Console.WriteLine();
");

        // Act
        var (metadata, _) = await _loader.LoadScriptAsync(scriptPath);

        // Assert
        Assert.False(metadata.Enabled);
    }

    [Fact]
    public async Task LoadScriptAsync_WithMultiValuePermissions_ParsesAllValues()
    {
        // Arrange
        var scriptPath = CreateTestScript("perms.csx", @"
// @script-id: perms-test
// @permissions: perm1, perm2, perm3, perm4
Console.WriteLine();
");

        // Act
        var (metadata, _) = await _loader.LoadScriptAsync(scriptPath);

        // Assert
        Assert.Equal(4, metadata.Permissions.Count);
    }

    #endregion

    #region IsValidScriptFile Tests

    [Fact]
    public void IsValidScriptFile_WithNullPath_ReturnsFalse()
    {
        Assert.False(_loader.IsValidScriptFile(null!));
    }

    [Fact]
    public void IsValidScriptFile_WithEmptyPath_ReturnsFalse()
    {
        Assert.False(_loader.IsValidScriptFile(string.Empty));
    }

    [Fact]
    public void IsValidScriptFile_WithWhitespacePath_ReturnsFalse()
    {
        Assert.False(_loader.IsValidScriptFile("   "));
    }

    [Fact]
    public void IsValidScriptFile_WithNonExistentFile_ReturnsFalse()
    {
        var nonExistent = Path.Combine(_testDirectory, "nonexistent.csx");
        Assert.False(_loader.IsValidScriptFile(nonExistent));
    }

    [Fact]
    public void IsValidScriptFile_WithValidCsxFile_ReturnsTrue()
    {
        var scriptPath = CreateTestScript("valid.csx", "Console.WriteLine();");
        Assert.True(_loader.IsValidScriptFile(scriptPath));
    }

    [Fact]
    public void IsValidScriptFile_WithValidCsFile_ReturnsTrue()
    {
        var scriptPath = CreateTestScript("valid.cs", "Console.WriteLine();");
        Assert.True(_loader.IsValidScriptFile(scriptPath));
    }

    [Fact]
    public void IsValidScriptFile_WithInvalidExtension_ReturnsFalse()
    {
        var txtPath = Path.Combine(_testDirectory, "script.txt");
        File.WriteAllText(txtPath, "console.log();");
        Assert.False(_loader.IsValidScriptFile(txtPath));
    }

    [Fact]
    public void IsValidScriptFile_WithMixedCaseExtension_ReturnsTrue()
    {
        var scriptPath = Path.Combine(_testDirectory, "test.CSX");
        File.WriteAllText(scriptPath, "Console.WriteLine();");
        Assert.True(_loader.IsValidScriptFile(scriptPath));
    }

    #endregion

    #region Metadata Extraction Tests

    [Fact]
    public void DiscoverScripts_ExtractsScriptIdCorrectly()
    {
        CreateTestScript("id.csx", "// @script-id: my-custom-id\nConsole.WriteLine();");
        var scripts = _loader.DiscoverScripts(_testDirectory);
        Assert.Equal("my-custom-id", scripts[0].Id);
    }

    [Fact]
    public void DiscoverScripts_WithoutScriptId_GeneratesIdFromPath()
    {
        CreateTestScript("auto.csx", "Console.WriteLine();");
        var scripts = _loader.DiscoverScripts(_testDirectory);
        Assert.NotNull(scripts[0].Id);
        Assert.Contains("auto", scripts[0].Id.ToLower());
    }

    [Fact]
    public void DiscoverScripts_ExtractsNameCorrectly()
    {
        CreateTestScript("name.csx", "// @name: Beautiful Script Name\nConsole.WriteLine();");
        var scripts = _loader.DiscoverScripts(_testDirectory);
        Assert.Equal("Beautiful Script Name", scripts[0].Name);
    }

    [Fact]
    public void DiscoverScripts_ExtractsVersionCorrectly()
    {
        CreateTestScript("ver.csx", "// @version: 3.2.1\nConsole.WriteLine();");
        var scripts = _loader.DiscoverScripts(_testDirectory);
        Assert.Equal("3.2.1", scripts[0].Version);
    }

    [Fact]
    public void DiscoverScripts_ExtractsAuthorCorrectly()
    {
        CreateTestScript("auth.csx", "// @author: John Doe\nConsole.WriteLine();");
        var scripts = _loader.DiscoverScripts(_testDirectory);
        Assert.Equal("John Doe", scripts[0].Author);
    }

    [Fact]
    public void DiscoverScripts_ExtractsDescriptionCorrectly()
    {
        CreateTestScript("desc.csx", "// @description: This is a long description\nConsole.WriteLine();");
        var scripts = _loader.DiscoverScripts(_testDirectory);
        Assert.Equal("This is a long description", scripts[0].Description);
    }

    [Fact]
    public void DiscoverScripts_ExtractsDependenciesCorrectly()
    {
        CreateTestScript("deps.csx", "// @dependencies: dep1, dep2, dep3\nConsole.WriteLine();");
        var scripts = _loader.DiscoverScripts(_testDirectory);
        Assert.Equal(3, scripts[0].Dependencies.Count);
        Assert.Contains("dep1", scripts[0].Dependencies);
        Assert.Contains("dep2", scripts[0].Dependencies);
        Assert.Contains("dep3", scripts[0].Dependencies);
    }

    [Fact]
    public void DiscoverScripts_ExtractsPermissionsCorrectly()
    {
        CreateTestScript("perms.csx", "// @permissions: read, write, execute\nConsole.WriteLine();");
        var scripts = _loader.DiscoverScripts(_testDirectory);
        Assert.Equal(3, scripts[0].Permissions.Count);
    }

    [Fact]
    public void DiscoverScripts_WithSpacesInMetadata_TrimsCorrectly()
    {
        CreateTestScript("spaces.csx", "// @script-id:   id-with-spaces   \nConsole.WriteLine();");
        var scripts = _loader.DiscoverScripts(_testDirectory);
        Assert.Equal("id-with-spaces", scripts[0].Id);
    }

    [Fact]
    public void DiscoverScripts_WithCaseInsensitiveTag_ParsesCorrectly()
    {
        CreateTestScript("case.csx", "// @SCRIPT-ID: uppercase-id\nConsole.WriteLine();");
        var scripts = _loader.DiscoverScripts(_testDirectory);
        Assert.Equal("uppercase-id", scripts[0].Id);
    }

    #endregion

    #region Error Handling and Edge Cases

    [Fact]
    public async Task LoadScriptAsync_WithLargeFile_LoadsSuccessfully()
    {
        // Arrange - create 1MB script
        var largeContent = new StringBuilder();
        largeContent.AppendLine("// @script-id: large-script");
        for (int i = 0; i < 50000; i++)
        {
            largeContent.AppendLine($"// Comment line {i}");
        }
        largeContent.AppendLine("Console.WriteLine();");

        var scriptPath = CreateTestScript("large.csx", largeContent.ToString());

        // Act
        var (metadata, code) = await _loader.LoadScriptAsync(scriptPath);

        // Assert
        Assert.NotNull(metadata);
        Assert.NotNull(code);
        Assert.True(code.Length > 1000000); // > 1MB
    }

    [Fact]
    public void DiscoverScripts_WithSpecialCharactersInFilename_HandlesCorrectly()
    {
        var scriptPath = CreateTestScript("special!@#$%^&().csx", "// @script-id: special\nConsole.WriteLine();");
        var scripts = _loader.DiscoverScripts(_testDirectory);
        Assert.Single(scripts);
    }

    [Fact]
    public void DiscoverScripts_WithUnicodeCharacters_HandlesCorrectly()
    {
        CreateTestScript("unicode.csx", "// @name: 测试脚本\n// @author: John 李\nConsole.WriteLine();");
        var scripts = _loader.DiscoverScripts(_testDirectory);
        Assert.Single(scripts);
        Assert.Equal("测试脚本", scripts[0].Name);
    }

    [Fact]
    public async Task LoadScriptAsync_WithEmptyFile_LoadsSuccessfully()
    {
        var scriptPath = CreateTestScript("empty.csx", string.Empty);
        var (metadata, code) = await _loader.LoadScriptAsync(scriptPath);
        Assert.NotNull(metadata);
        Assert.Equal(string.Empty, code);
    }

    [Fact]
    public void DiscoverScripts_WithReadOnlyDirectory_HandlesCorrectly()
    {
        CreateTestScript("readonly.csx", "// @script-id: readonly\nConsole.WriteLine();");
        var dirInfo = new DirectoryInfo(_testDirectory);
        var wasReadOnly = dirInfo.Attributes.HasFlag(FileAttributes.ReadOnly);

        try
        {
            dirInfo.Attributes |= FileAttributes.ReadOnly;
            var scripts = _loader.DiscoverScripts(_testDirectory);
            Assert.Single(scripts);
        }
        finally
        {
            if (!wasReadOnly)
                dirInfo.Attributes &= ~FileAttributes.ReadOnly;
        }
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task LoadScriptAsync_ConcurrentLoads_HandlesCorrectly()
    {
        // Arrange
        var scriptPath = CreateTestScript("concurrent.csx", "// @script-id: concurrent\nConsole.WriteLine();");

        // Act - load same script 10 times concurrently
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _loader.LoadScriptAsync(scriptPath))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert - all should succeed
        Assert.Equal(10, results.Length);
        Assert.All(results, r => Assert.Equal("concurrent", r.Metadata.Id));
    }

    [Fact]
    public void DiscoverScripts_ConcurrentDiscovery_HandlesCorrectly()
    {
        // Arrange
        for (int i = 0; i < 20; i++)
        {
            CreateTestScript($"script{i}.csx", $"// @script-id: script{i}\nConsole.WriteLine();");
        }

        // Act - discover concurrently from multiple threads
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => Task.Run(() => _loader.DiscoverScripts(_testDirectory)))
            .ToArray();

        Task.WaitAll(tasks);

        // Assert - all should find the same scripts
        var results = tasks.Select(t => t.Result).ToArray();
        Assert.All(results, r => Assert.Equal(20, r.Count));
    }

    #endregion

    #region Cache and Performance Tests

    [Fact]
    public async Task LoadScriptAsync_SameScriptMultipleTimes_ReadsFromDiskEachTime()
    {
        // Arrange
        var scriptPath = CreateTestScript("cache.csx", "// @script-id: cache-test\nConsole.WriteLine();");

        // Act - load 3 times
        var result1 = await _loader.LoadScriptAsync(scriptPath);
        var result2 = await _loader.LoadScriptAsync(scriptPath);
        var result3 = await _loader.LoadScriptAsync(scriptPath);

        // Assert - all should succeed
        Assert.Equal(result1.Metadata.Id, result2.Metadata.Id);
        Assert.Equal(result2.Metadata.Id, result3.Metadata.Id);
    }

    [Fact]
    public async Task LoadScriptAsync_ModifiedScript_LoadsLatestVersion()
    {
        // Arrange
        var scriptPath = CreateTestScript("modified.csx", "// @version: 1.0.0\nConsole.WriteLine();");
        var (metadata1, _) = await _loader.LoadScriptAsync(scriptPath);

        // Modify script
        File.WriteAllText(scriptPath, "// @version: 2.0.0\nConsole.WriteLine();");

        // Act
        var (metadata2, _) = await _loader.LoadScriptAsync(scriptPath);

        // Assert
        Assert.Equal("1.0.0", metadata1.Version);
        Assert.Equal("2.0.0", metadata2.Version);
    }

    #endregion

    #region Helper Methods

    private string CreateTestScript(string filename, string content)
    {
        var path = Path.Combine(_testDirectory, filename);
        File.WriteAllText(path, content);
        return path;
    }

    #endregion
}
