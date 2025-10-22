using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;

namespace PokeNET.Tests.Utilities;

/// <summary>
/// Helper utilities for creating test mods and mod-related test data
/// </summary>
public static class ModTestHelpers
{
    /// <summary>
    /// Creates a test mod manifest with the specified properties
    /// </summary>
    public static string CreateTestManifest(
        string id,
        string name,
        string version = "1.0.0",
        string author = "Test Author",
        string description = "Test mod",
        string[]? dependencies = null,
        string[]? loadAfter = null,
        string[]? loadBefore = null)
    {
        var manifest = new
        {
            Id = id,
            Name = name,
            Version = version,
            Author = author,
            Description = description,
            Dependencies = dependencies ?? Array.Empty<string>(),
            LoadAfter = loadAfter ?? Array.Empty<string>(),
            LoadBefore = loadBefore ?? Array.Empty<string>()
        };

        return JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Creates a test mod directory with manifest file
    /// </summary>
    public static string CreateTestModDirectory(
        string baseTestPath,
        string modId,
        string modName,
        string[]? dependencies = null,
        string[]? loadAfter = null,
        string[]? loadBefore = null)
    {
        var modPath = Path.Combine(baseTestPath, modId);
        Directory.CreateDirectory(modPath);

        var manifestPath = Path.Combine(modPath, "modinfo.json");
        var manifestContent = CreateTestManifest(
            modId,
            modName,
            dependencies: dependencies,
            loadAfter: loadAfter,
            loadBefore: loadBefore);

        File.WriteAllText(manifestPath, manifestContent);

        return modPath;
    }

    /// <summary>
    /// Creates multiple test mods with dependency relationships
    /// </summary>
    public static Dictionary<string, string> CreateTestModSet(
        string baseTestPath,
        params (string Id, string Name, string[]? Dependencies)[] mods)
    {
        var modPaths = new Dictionary<string, string>();

        foreach (var (id, name, dependencies) in mods)
        {
            var path = CreateTestModDirectory(baseTestPath, id, name, dependencies);
            modPaths[id] = path;
        }

        return modPaths;
    }

    /// <summary>
    /// Creates a dynamic test assembly for a mod
    /// </summary>
    public static Assembly BuildTestModAssembly(string modName, Type[]? types = null)
    {
        var assemblyName = new AssemblyName($"{modName}.Test");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run);

        var moduleBuilder = assemblyBuilder.DefineDynamicModule($"{modName}Module");

        if (types != null)
        {
            foreach (var type in types)
            {
                var typeBuilder = moduleBuilder.DefineType(
                    type.Name,
                    TypeAttributes.Public | TypeAttributes.Class);
                typeBuilder.CreateType();
            }
        }

        return assemblyBuilder;
    }

    /// <summary>
    /// Creates a test mod data file (JSON)
    /// </summary>
    public static void CreateTestDataFile(string modPath, string relativePath, object data)
    {
        var fullPath = Path.Combine(modPath, relativePath);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(fullPath, json);
    }

    /// <summary>
    /// Creates a test content file (placeholder)
    /// </summary>
    public static void CreateTestContentFile(string modPath, string relativePath)
    {
        var fullPath = Path.Combine(modPath, relativePath);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Create a simple placeholder file
        File.WriteAllBytes(fullPath, new byte[] { 0x50, 0x4E, 0x47 }); // PNG signature
    }

    /// <summary>
    /// Creates a mock logger for testing
    /// </summary>
    public static ILogger<T> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>().Object;
    }

    /// <summary>
    /// Creates an invalid manifest (malformed JSON)
    /// </summary>
    public static string CreateInvalidManifest()
    {
        return "{ invalid json content here ";
    }

    /// <summary>
    /// Creates a manifest with missing required fields
    /// </summary>
    public static string CreateIncompleteManifest()
    {
        return JsonSerializer.Serialize(new
        {
            Id = "test-mod"
            // Missing Name, Version, etc.
        });
    }

    /// <summary>
    /// Cleans up test mod directories
    /// </summary>
    public static void CleanupTestMods(string baseTestPath)
    {
        if (Directory.Exists(baseTestPath))
        {
            Directory.Delete(baseTestPath, recursive: true);
        }
    }

    /// <summary>
    /// Verifies a mod was loaded in the correct order
    /// </summary>
    public static void VerifyLoadOrder(List<string> actualOrder, params string[] expectedOrder)
    {
        actualOrder.Should().Equal(expectedOrder, "mods should load in dependency order");
    }

    /// <summary>
    /// Creates a circular dependency scenario for testing
    /// </summary>
    public static Dictionary<string, string> CreateCircularDependencyMods(string baseTestPath)
    {
        var modA = CreateTestModDirectory(baseTestPath, "ModA", "Mod A", dependencies: new[] { "ModB" });
        var modB = CreateTestModDirectory(baseTestPath, "ModB", "Mod B", dependencies: new[] { "ModC" });
        var modC = CreateTestModDirectory(baseTestPath, "ModC", "Mod C", dependencies: new[] { "ModA" });

        return new Dictionary<string, string>
        {
            ["ModA"] = modA,
            ["ModB"] = modB,
            ["ModC"] = modC
        };
    }

    /// <summary>
    /// Creates version-specific test manifests
    /// </summary>
    public static string CreateVersionedManifest(
        string id,
        string name,
        string version,
        Dictionary<string, string>? versionedDependencies = null)
    {
        var deps = versionedDependencies?.Select(kvp => $"{kvp.Key}@{kvp.Value}").ToArray()
                   ?? Array.Empty<string>();

        return CreateTestManifest(id, name, version, dependencies: deps);
    }
}
