using PokeNET.Core.Modding;
using PokeNET.Domain.Modding;
using Xunit;

namespace PokeNET.Tests.Modding;

/// <summary>
/// Tests for Interface Segregation Principle compliance of IModManifest and related interfaces.
/// </summary>
public class ModManifestInterfaceSegregationTests
{
    private static ModManifest CreateTestManifest()
    {
        return new ModManifest
        {
            Id = "com.test.mod",
            Name = "Test Mod",
            Version = new ModVersion { Major = 1, Minor = 0, Patch = 0 },
            ApiVersion = "1.0.0",
            Author = "Test Author",
            Description = "A test mod",
            Tags = new[] { "test", "example" },
            Dependencies = Array.Empty<ModDependency>(),
            LoadAfter = Array.Empty<string>(),
            LoadBefore = Array.Empty<string>(),
            IncompatibleWith = Array.Empty<ModIncompatibility>(),
            ModType = ModType.Code,
            EntryPoint = "TestMod.Entry",
            Assemblies = new[] { "TestMod.dll" },
            TrustLevel = ModTrustLevel.Trusted
        };
    }

    [Fact]
    public void ModManifest_ImplementsAllInterfaces()
    {
        // Arrange & Act
        var manifest = CreateTestManifest();

        // Assert - Verify all interface implementations
        Assert.IsAssignableFrom<IModManifestCore>(manifest);
        Assert.IsAssignableFrom<IModMetadata>(manifest);
        Assert.IsAssignableFrom<IModDependencies>(manifest);
        Assert.IsAssignableFrom<ICodeMod>(manifest);
        Assert.IsAssignableFrom<IContentMod>(manifest);
        Assert.IsAssignableFrom<IModSecurity>(manifest);
        Assert.IsAssignableFrom<IModManifest>(manifest);
    }

    [Fact]
    public void IModManifestCore_ContainsOnlyEssentialProperties()
    {
        // Arrange
        var manifest = CreateTestManifest();
        IModManifestCore core = manifest;

        // Act & Assert - Only these properties should be accessible
        Assert.Equal("com.test.mod", core.Id);
        Assert.Equal("Test Mod", core.Name);
        Assert.Equal(1, core.Version.Major);
        Assert.Equal("1.0.0", core.ApiVersion);

        // Verify interface has exactly 4 properties
        var properties = typeof(IModManifestCore).GetProperties();
        Assert.Equal(4, properties.Length);
    }

    [Fact]
    public void IModMetadata_ContainsOnlyDescriptiveProperties()
    {
        // Arrange
        var manifest = CreateTestManifest();
        IModMetadata metadata = manifest;

        // Act & Assert
        Assert.Equal("Test Author", metadata.Author);
        Assert.Equal("A test mod", metadata.Description);
        Assert.Contains("test", metadata.Tags);
        Assert.NotNull(metadata.Metadata);

        // Verify interface has exactly 7 properties
        var properties = typeof(IModMetadata).GetProperties();
        Assert.Equal(7, properties.Length);
    }

    [Fact]
    public void IModDependencies_ContainsOnlyDependencyProperties()
    {
        // Arrange
        var manifest = CreateTestManifest();
        IModDependencies dependencies = manifest;

        // Act & Assert
        Assert.Empty(dependencies.Dependencies);
        Assert.Empty(dependencies.LoadAfter);
        Assert.Empty(dependencies.LoadBefore);
        Assert.Empty(dependencies.IncompatibleWith);

        // Verify interface has exactly 4 properties
        var properties = typeof(IModDependencies).GetProperties();
        Assert.Equal(4, properties.Length);
    }

    [Fact]
    public void ICodeMod_ContainsOnlyCodeExecutionProperties()
    {
        // Arrange
        var manifest = CreateTestManifest();
        ICodeMod codeMod = manifest;

        // Act & Assert
        Assert.Equal(ModType.Code, codeMod.ModType);
        Assert.Equal("TestMod.Entry", codeMod.EntryPoint);
        Assert.Contains("TestMod.dll", codeMod.Assemblies);

        // Verify interface has exactly 4 properties
        var properties = typeof(ICodeMod).GetProperties();
        Assert.Equal(4, properties.Length);
    }

    [Fact]
    public void IContentMod_ContainsOnlyAssetProperties()
    {
        // Arrange
        var manifest = CreateTestManifest();
        IContentMod contentMod = manifest;

        // Act & Assert
        Assert.NotNull(contentMod.AssetPaths);
        Assert.Empty(contentMod.Preload);

        // Verify interface has exactly 2 properties
        var properties = typeof(IContentMod).GetProperties();
        Assert.Equal(2, properties.Length);
    }

    [Fact]
    public void IModSecurity_ContainsOnlySecurityProperties()
    {
        // Arrange
        var manifest = CreateTestManifest();
        IModSecurity security = manifest;

        // Act & Assert
        Assert.Equal(ModTrustLevel.Trusted, security.TrustLevel);
        Assert.Equal(ContentRating.Everyone, security.ContentRating);

        // Verify interface has exactly 3 properties
        var properties = typeof(IModSecurity).GetProperties();
        Assert.Equal(3, properties.Length);
    }

    [Fact]
    public void ComponentRequiringOnlyCore_ShouldNotDependOnFullInterface()
    {
        // Arrange
        var manifest = CreateTestManifest();

        // Act - Simulate a component that only needs core info
        var displayName = GetModDisplayName(manifest); // Uses only IModManifestCore

        // Assert
        Assert.Equal("Test Mod v1.0.0", displayName);
    }

    [Fact]
    public void ComponentRequiringOnlyMetadata_ShouldNotDependOnFullInterface()
    {
        // Arrange
        var manifest = CreateTestManifest();

        // Act - Simulate a mod browser that only needs metadata
        var description = GetModDescription(manifest, manifest); // Uses Core + Metadata

        // Assert
        Assert.Contains("Test Author", description);
        Assert.Contains("A test mod", description);
    }

    [Fact]
    public void ComponentResolvingLoadOrder_ShouldOnlyUseDependencies()
    {
        // Arrange
        var mods = new List<ModManifest>
        {
            new ModManifest
            {
                Id = "com.test.mod1",
                Name = "Mod 1",
                Version = new ModVersion { Major = 1, Minor = 0, Patch = 0 },
                ApiVersion = "1.0.0"
            },
            new ModManifest
            {
                Id = "com.test.mod2",
                Name = "Mod 2",
                Version = new ModVersion { Major = 1, Minor = 0, Patch = 0 },
                ApiVersion = "1.0.0",
                Dependencies = new[] { new ModDependency { ModId = "com.test.mod1" } }
            }
        };

        // Act - Simulate load order resolution using only dependencies
        var canLoadInOrder = CanResolveLoadOrder(mods.Cast<IModDependencies>().ToList());

        // Assert
        Assert.True(canLoadInOrder);
    }

    [Fact]
    public void BackwardsCompatibility_FullInterfaceStillWorks()
    {
        // Arrange & Act
        var manifest = CreateTestManifest();
        IModManifest fullInterface = manifest;

        // Assert - All properties still accessible through full interface
        Assert.Equal("com.test.mod", fullInterface.Id);
        Assert.Equal("Test Author", fullInterface.Author);
        Assert.Empty(fullInterface.Dependencies);
        Assert.Equal(ModType.Code, fullInterface.ModType);
        Assert.NotNull(fullInterface.AssetPaths);
        Assert.Equal(ModTrustLevel.Trusted, fullInterface.TrustLevel);
        Assert.NotNull(fullInterface.Directory);
    }

    [Fact]
    public void InterfaceHierarchy_IsCorrect()
    {
        // Assert - Verify IModManifest inherits from all focused interfaces
        var interfaceType = typeof(IModManifest);
        var baseInterfaces = interfaceType.GetInterfaces();

        Assert.Contains(typeof(IModManifestCore), baseInterfaces);
        Assert.Contains(typeof(IModMetadata), baseInterfaces);
        Assert.Contains(typeof(IModDependencies), baseInterfaces);
        Assert.Contains(typeof(ICodeMod), baseInterfaces);
        Assert.Contains(typeof(IContentMod), baseInterfaces);
        Assert.Contains(typeof(IModSecurity), baseInterfaces);
    }

    // Helper methods demonstrating ISP-compliant usage

    private static string GetModDisplayName(IModManifestCore core)
    {
        return $"{core.Name} v{core.Version}";
    }

    private static string GetModDescription(IModManifestCore core, IModMetadata metadata)
    {
        return $"{core.Name} by {metadata.Author}: {metadata.Description}";
    }

    private static bool CanResolveLoadOrder(List<IModDependencies> mods)
    {
        // Simplified load order check - just verify we can access dependency info
        return mods.All(m => m.Dependencies != null);
    }
}
