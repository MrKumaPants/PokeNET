# IModManifest Interface Segregation Migration Guide

## Overview

The `IModManifest` interface has been refactored to follow the **Interface Segregation Principle (ISP)**, splitting 25 properties into 6 focused interfaces. This improves code maintainability, testability, and allows components to depend only on the capabilities they need.

## What Changed?

### Before (ISP Violation)
```csharp
// ❌ Single interface with 25 properties - forces all consumers to depend on everything
public interface IModManifest
{
    string Id { get; }
    string Name { get; }
    // ... 23 more properties
}
```

### After (ISP Compliant)
```csharp
// ✅ Focused interfaces with specific responsibilities
public interface IModManifestCore { /* 4 core properties */ }
public interface IModMetadata { /* 7 metadata properties */ }
public interface IModDependencies { /* 4 dependency properties */ }
public interface ICodeMod { /* 4 code execution properties */ }
public interface IContentMod { /* 2 asset properties */ }
public interface IModSecurity { /* 3 security properties */ }

// ✅ Combined interface for backwards compatibility
public interface IModManifest : IModManifestCore, IModMetadata,
    IModDependencies, ICodeMod, IContentMod, IModSecurity
{
    string Directory { get; } // Runtime property
}
```

## The 6 Interfaces

### 1. IModManifestCore (Core Identity)
**Use when:** You need basic mod identification.

```csharp
public interface IModManifestCore
{
    string Id { get; }           // Unique identifier
    string Name { get; }         // Display name
    ModVersion Version { get; }  // Semantic version
    string ApiVersion { get; }   // Required API version
}
```

**Example usage:**
```csharp
// Display mod name
void LogModInfo(IModManifestCore core)
{
    Console.WriteLine($"Loading {core.Name} v{core.Version}");
}
```

### 2. IModMetadata (Descriptive Information)
**Use when:** You need to display mod information in UI or documentation.

```csharp
public interface IModMetadata
{
    string? Author { get; }
    string? Description { get; }
    Uri? Homepage { get; }
    string? License { get; }
    IReadOnlyList<string> Tags { get; }
    LocalizationConfiguration? Localization { get; }
    IReadOnlyDictionary<string, object> Metadata { get; }
}
```

**Example usage:**
```csharp
// Mod browser UI
void DisplayInModBrowser(IModManifestCore core, IModMetadata metadata)
{
    UI.ShowModCard(
        title: core.Name,
        author: metadata.Author,
        description: metadata.Description,
        tags: metadata.Tags
    );
}
```

### 3. IModDependencies (Load Order Management)
**Use when:** You need to resolve dependencies or validate load order.

```csharp
public interface IModDependencies
{
    IReadOnlyList<ModDependency> Dependencies { get; }
    IReadOnlyList<string> LoadAfter { get; }
    IReadOnlyList<string> LoadBefore { get; }
    IReadOnlyList<ModIncompatibility> IncompatibleWith { get; }
}
```

**Example usage:**
```csharp
// Topological sort for load order
List<string> ResolveLoadOrder(IEnumerable<IModManifestCore> cores,
                               IEnumerable<IModDependencies> deps)
{
    // Build dependency graph
    var graph = BuildDependencyGraph(cores, deps);
    return TopologicalSort(graph);
}
```

### 4. ICodeMod (Code Execution)
**Use when:** You need to load assemblies or execute mod code.

```csharp
public interface ICodeMod
{
    ModType ModType { get; }
    string? EntryPoint { get; }
    IReadOnlyList<string> Assemblies { get; }
    string? HarmonyId { get; }
}
```

**Example usage:**
```csharp
// Assembly loader
void LoadModAssembly(IModManifestCore core, ICodeMod codeMod)
{
    if (codeMod.ModType == ModType.Code || codeMod.ModType == ModType.Hybrid)
    {
        foreach (var assembly in codeMod.Assemblies)
        {
            LoadAssembly(core.Id, assembly);
        }
    }
}
```

### 5. IContentMod (Asset Management)
**Use when:** You need to load textures, audio, or other game assets.

```csharp
public interface IContentMod
{
    AssetPathConfiguration AssetPaths { get; }
    IReadOnlyList<string> Preload { get; }
}
```

**Example usage:**
```csharp
// Asset preloader
void PreloadAssets(IModManifestCore core, IContentMod contentMod)
{
    foreach (var asset in contentMod.Preload)
    {
        var path = ResolveAssetPath(core.Id, asset, contentMod.AssetPaths);
        AssetLoader.PreloadAsync(path);
    }
}
```

### 6. IModSecurity (Trust Verification)
**Use when:** You need to verify mod integrity or enforce security policies.

```csharp
public interface IModSecurity
{
    ModTrustLevel TrustLevel { get; }
    string? Checksum { get; }
    ContentRating ContentRating { get; }
}
```

**Example usage:**
```csharp
// Security validator
bool ValidateModSecurity(IModManifestCore core, IModSecurity security)
{
    if (security.TrustLevel == ModTrustLevel.Untrusted)
    {
        return RequestUserConsent(core.Name);
    }

    if (security.Checksum != null)
    {
        return VerifyChecksum(core.Id, security.Checksum);
    }

    return true;
}
```

## Migration Guide

### For Mod Authors

**Good news:** No changes required! The `ModManifest` class still implements `IModManifest` with all properties.

Your `modinfo.json` files remain unchanged:
```json
{
  "id": "com.example.mymod",
  "name": "My Awesome Mod",
  "version": "1.0.0",
  "apiVersion": "1.0.0",
  "author": "Your Name",
  "description": "Does cool things"
}
```

### For Framework/Tool Developers

**Update your code to use focused interfaces:**

#### Before (ISP Violation)
```csharp
// ❌ Bad: Depends on entire interface
void DisplayModCard(IModManifest manifest)
{
    UI.Title = manifest.Name;
    UI.Author = manifest.Author;
    UI.Description = manifest.Description;
    // Only uses 3 properties but depends on all 25!
}

void ResolveLoadOrder(List<IModManifest> mods)
{
    // Only needs Dependencies but depends on all properties
    return TopologicalSort(mods);
}
```

#### After (ISP Compliant)
```csharp
// ✅ Good: Depends only on what's needed
void DisplayModCard(IModManifestCore core, IModMetadata metadata)
{
    UI.Title = core.Name;
    UI.Author = metadata.Author;
    UI.Description = metadata.Description;
    // Clear interface dependencies!
}

void ResolveLoadOrder(IEnumerable<IModManifestCore> cores,
                      IEnumerable<IModDependencies> deps)
{
    // Only depends on core identity and dependencies
    return TopologicalSort(cores.Zip(deps));
}
```

### Common Patterns

#### Pattern 1: Display Mod Information
```csharp
// Use: IModManifestCore + IModMetadata
string FormatModInfo(IModManifestCore core, IModMetadata metadata)
{
    return $"{core.Name} v{core.Version} by {metadata.Author}\n" +
           $"{metadata.Description}\n" +
           $"Tags: {string.Join(", ", metadata.Tags)}";
}
```

#### Pattern 2: Load Order Resolution
```csharp
// Use: IModManifestCore + IModDependencies
List<string> ResolveLoadOrder(
    IEnumerable<(IModManifestCore Core, IModDependencies Deps)> mods)
{
    // Build dependency graph and sort
    var graph = new Dictionary<string, List<string>>();

    foreach (var (core, deps) in mods)
    {
        graph[core.Id] = deps.Dependencies
            .Select(d => d.ModId)
            .ToList();
    }

    return TopologicalSort(graph);
}
```

#### Pattern 3: Security Validation
```csharp
// Use: IModManifestCore + ICodeMod + IModSecurity
bool ValidateMod(IModManifestCore core, ICodeMod code, IModSecurity security)
{
    // Check trust level
    if (security.TrustLevel == ModTrustLevel.Untrusted &&
        code.ModType == ModType.Code)
    {
        Logger.Warn($"Untrusted code mod: {core.Name}");
        return RequestUserConsent();
    }

    // Verify checksum
    if (security.Checksum != null)
    {
        return VerifyChecksum(core.Id, security.Checksum);
    }

    return true;
}
```

#### Pattern 4: Complete Mod Loading
```csharp
// Use: IModManifest when you truly need everything
void LoadMod(IModManifest manifest)
{
    // 1. Validate security
    ValidateSecurity(manifest, manifest);

    // 2. Resolve dependencies
    CheckDependencies(manifest, manifest);

    // 3. Load code
    if (manifest.ModType == ModType.Code || manifest.ModType == ModType.Hybrid)
    {
        LoadAssembly(manifest, manifest);
    }

    // 4. Preload assets
    PreloadAssets(manifest, manifest);
}
```

## Benefits

### 1. Better Testability
```csharp
// Mock only what you need
var mockCore = new Mock<IModManifestCore>();
mockCore.Setup(m => m.Id).Returns("test.mod");
mockCore.Setup(m => m.Name).Returns("Test");

// Test display logic without creating full manifest
var display = GetModDisplayName(mockCore.Object);
```

### 2. Clear Dependencies
```csharp
// Method signature clearly shows what it needs
void ValidateDependencies(
    IModManifestCore core,           // Need ID for error messages
    IModDependencies dependencies)   // Need dependency list
{
    // No confusion about what's used
}
```

### 3. Easier Refactoring
```csharp
// If IModMetadata changes, only components using metadata are affected
// Components using only IModManifestCore or IModDependencies are unaffected
```

### 4. Flexible Implementations
```csharp
// Can create lightweight implementations for specific use cases
public class ModManifestCoreOnly : IModManifestCore
{
    public string Id { get; set; }
    public string Name { get; set; }
    public ModVersion Version { get; set; }
    public string ApiVersion { get; set; }
}

// Use in scenarios where only core info is needed (e.g., mod listing)
```

## Backwards Compatibility

**100% backwards compatible!**

- `ModManifest` class implements `IModManifest` (which inherits all interfaces)
- Existing code using `IModManifest` continues to work
- `modinfo.json` format unchanged
- No breaking changes to mod loading or API

## Best Practices

### DO ✅

1. **Use focused interfaces in method signatures:**
   ```csharp
   void DisplayMod(IModManifestCore core, IModMetadata metadata)
   ```

2. **Use IModManifest when you truly need all properties:**
   ```csharp
   void LoadCompleteMod(IModManifest manifest)
   ```

3. **Document interface requirements:**
   ```csharp
   /// <param name="core">Core mod identity</param>
   /// <param name="deps">Dependency information</param>
   ```

### DON'T ❌

1. **Don't use IModManifest when focused interfaces suffice:**
   ```csharp
   // ❌ Bad
   void ShowModName(IModManifest manifest) => Console.WriteLine(manifest.Name);

   // ✅ Good
   void ShowModName(IModManifestCore core) => Console.WriteLine(core.Name);
   ```

2. **Don't create tight coupling:**
   ```csharp
   // ❌ Bad - UI component depends on security interface
   class ModCard
   {
       public ModCard(IModManifest manifest)
       {
           // Only uses Name and Author but depends on everything
       }
   }
   ```

3. **Don't bypass interfaces with concrete types:**
   ```csharp
   // ❌ Bad
   void ProcessMod(ModManifest manifest)

   // ✅ Good
   void ProcessMod(IModManifest manifest)
   ```

## Testing

See `ModManifestInterfaceSegregationTests.cs` for comprehensive test examples demonstrating:

- Interface implementation validation
- Property count verification per interface
- ISP-compliant usage patterns
- Backwards compatibility tests
- Mock-based unit testing

## Questions?

- **Q: Do I need to update my mods?**
  - A: No! Existing mods work without changes.

- **Q: Should I always use focused interfaces?**
  - A: Use the most specific interface that meets your needs. If you need everything, use `IModManifest`.

- **Q: Can I implement these interfaces myself?**
  - A: Yes! They're public interfaces designed for flexibility.

- **Q: Is this a breaking change?**
  - A: No! `IModManifest` still exists and includes all properties.

## Related Documentation

- [Interface Segregation Principle (ISP)](https://en.wikipedia.org/wiki/Interface_segregation_principle)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Mod Manifest Specification](../modding/manifest-specification.md)
- [Mod Loading Architecture](../architecture/mod-loading.md)

---

**Summary:** The IModManifest interface now follows ISP by splitting into 6 focused interfaces. This improves code quality while maintaining 100% backwards compatibility. Use focused interfaces in new code for better maintainability and testability.
