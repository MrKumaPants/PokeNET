# Phase 4: RimWorld-Style Modding System Architecture

## Executive Summary

This document defines the complete architecture for PokeNET's modding framework, inspired by RimWorld's powerful and extensible modding system. The design emphasizes SOLID principles, semantic versioning, backward compatibility, and security while enabling deep gameplay modifications through Harmony runtime patching.

## Architecture Decision Records

### ADR-001: Modding System Architecture Pattern

**Status**: Accepted
**Context**: Need a flexible, extensible modding system that supports data, content, and code mods with minimal coupling to core systems.
**Decision**: Implement a plugin-style architecture with:
- Separate `PokeNET.ModApi` interface project (stable API surface)
- Dependency injection-based mod lifecycle management
- Topological sort for dependency resolution
- Harmony integration for runtime code patching

**Consequences**:
- ✅ Clear separation of concerns between core and mod API
- ✅ Easy to version and maintain backward compatibility
- ✅ Mods can be tested independently
- ⚠️ Requires careful API design to avoid breaking changes
- ⚠️ Harmony patches can conflict; need conflict detection

### ADR-002: Mod Loading Strategy

**Status**: Accepted
**Context**: Mods must be loaded in correct order to respect dependencies, with clear lifecycle phases.
**Decision**: Multi-phase loading process:
1. **Discovery**: Scan `Mods/` directory for subdirectories with `modinfo.json`
2. **Validation**: Parse manifest, verify schema, check version compatibility
3. **Resolution**: Build dependency graph, detect cycles, perform topological sort
4. **Loading**: Load in dependency order (data → content → code)
5. **Initialization**: Execute mod entry points, apply Harmony patches

**Consequences**:
- ✅ Deterministic load order
- ✅ Clear error reporting for dependency issues
- ✅ Graceful handling of optional dependencies
- ⚠️ Longer startup time with many mods
- ⚠️ Need comprehensive logging for troubleshooting

### ADR-003: Asset Override Strategy

**Status**: Accepted
**Context**: Mods need to override base game assets and each other's assets predictably.
**Decision**: Last-loaded-wins strategy with explicit override chains:
- Asset resolution order: Last loaded mod → ... → First loaded mod → Base game
- Mods can declare "loadAfter" to ensure they override specific mods
- Asset manager tracks override chains for debugging
- Warnings logged when mods override the same asset

**Consequences**:
- ✅ Predictable override behavior
- ✅ Mods can explicitly control load order
- ✅ Easy to debug asset conflicts
- ⚠️ Load order becomes critical for visual consistency
- ⚠️ Need UI to show active overrides

### ADR-004: Harmony Integration Strategy

**Status**: Accepted
**Context**: Enable deep code modifications while maintaining stability and conflict detection.
**Decision**: Managed Harmony integration:
- One global `Harmony` instance per mod (mod ID as Harmony ID)
- Patches applied during mod initialization phase
- Patch conflict detection via reflection analysis
- Automatic rollback on patch application failure
- Patch inspection API for debugging

**Consequences**:
- ✅ Isolated patch namespaces per mod
- ✅ Clear ownership of patches
- ✅ Automatic conflict detection
- ✅ Safe failure recovery
- ⚠️ Performance impact from extensive patches
- ⚠️ Requires validation of patch targets

### ADR-005: Mod API Versioning

**Status**: Accepted
**Context**: Mods compiled against older API versions must continue working.
**Decision**: Semantic versioning with compatibility guarantees:
- Major version: Breaking changes (require mod recompilation)
- Minor version: Additive changes (backward compatible)
- Patch version: Bug fixes (fully compatible)
- Runtime version checking with clear error messages
- Deprecated API marked with `[Obsolete]` attributes
- Support for N-1 major versions

**Consequences**:
- ✅ Clear expectations for mod authors
- ✅ Gradual migration path for breaking changes
- ✅ Long-term mod stability
- ⚠️ Requires careful API design
- ⚠️ Need to maintain legacy adapters

## System Architecture

### Component Diagram (C4 Level 2)

```
┌─────────────────────────────────────────────────────────────────┐
│                         PokeNET Game                             │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │                    PokeNET.Core                             │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │ │
│  │  │  ModLoader   │  │ AssetManager │  │  ECS World   │     │ │
│  │  │              │  │              │  │              │     │ │
│  │  │ - Discovery  │  │ - Override   │  │ - Systems    │     │ │
│  │  │ - Validation │  │   Resolution │  │ - Entities   │     │ │
│  │  │ - Loading    │  │ - Caching    │  │              │     │ │
│  │  └──────┬───────┘  └──────┬───────┘  └──────────────┘     │ │
│  │         │                  │                                │ │
│  └─────────┼──────────────────┼────────────────────────────────┘ │
│            │                  │                                  │
│  ┌─────────▼──────────────────▼────────────────────────────────┐ │
│  │                    PokeNET.ModApi                            │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │ │
│  │  │     IMod     │  │ IModManifest │  │ IModContext  │     │ │
│  │  │              │  │              │  │              │     │ │
│  │  │ + Initialize │  │ + Id         │  │ + Logger     │     │ │
│  │  │ + Shutdown   │  │ + Version    │  │ + World      │     │ │
│  │  │              │  │ + Deps       │  │ + Assets     │     │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘     │ │
│  │                                                              │ │
│  │  ┌──────────────┐  ┌──────────────┐                        │ │
│  │  │ IModLoader   │  │ IAssetApi    │                        │ │
│  │  └──────────────┘  └──────────────┘                        │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                              │                                    │
└──────────────────────────────┼────────────────────────────────────┘
                               │
                    ┌──────────▼──────────┐
                    │   Mod Ecosystem     │
                    │                     │
                    │  ┌──────────────┐  │
                    │  │  Data Mod    │  │
                    │  │  (JSON/XML)  │  │
                    │  └──────────────┘  │
                    │                     │
                    │  ┌──────────────┐  │
                    │  │ Content Mod  │  │
                    │  │  (Assets)    │  │
                    │  └──────────────┘  │
                    │                     │
                    │  ┌──────────────┐  │
                    │  │  Code Mod    │  │
                    │  │ (.dll+Harmony)│ │
                    │  └──────────────┘  │
                    └─────────────────────┘
```

### Mod Loading Sequence Diagram

```
┌──────┐  ┌──────────┐  ┌────────────┐  ┌─────────┐  ┌──────┐
│ Game │  │ModLoader │  │ Validation │  │Dependency│  │ Mod  │
│      │  │          │  │  Engine    │  │ Resolver │  │      │
└──┬───┘  └────┬─────┘  └─────┬──────┘  └────┬─────┘  └───┬──┘
   │           │               │              │            │
   │ StartGame │               │              │            │
   ├──────────>│               │              │            │
   │           │               │              │            │
   │           │ Discover Mods │              │            │
   │           ├──────────────>│              │            │
   │           │               │              │            │
   │           │ Validate Each │              │            │
   │           ├──────────────>│              │            │
   │           │               │              │            │
   │           │ Parse Dependencies          │            │
   │           ├──────────────────────────────>            │
   │           │               │              │            │
   │           │ Topological Sort             │            │
   │           │<─────────────────────────────┤            │
   │           │               │              │            │
   │           │ Load Mod Assembly            │            │
   │           ├──────────────────────────────────────────>│
   │           │               │              │            │
   │           │ Create Mod Instance          │            │
   │           │<──────────────────────────────────────────┤
   │           │               │              │            │
   │           │ Initialize(context)          │            │
   │           ├──────────────────────────────────────────>│
   │           │               │              │            │
   │           │ Apply Harmony Patches        │            │
   │           │<──────────────────────────────────────────┤
   │           │               │              │            │
   │ All Mods Loaded           │              │            │
   │<──────────┤               │              │            │
   │           │               │              │            │
```

### Data Flow: Asset Resolution

```
┌───────────────────┐
│  Game requests    │
│  asset "logo.png" │
└─────────┬─────────┘
          │
          ▼
┌─────────────────────────────────────────────┐
│        AssetManager.Load<T>("logo")         │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│  Check asset override chain (reverse order) │
│  1. Mod C (last loaded)                     │
│  2. Mod B                                   │
│  3. Mod A (first loaded)                    │
│  4. Base game                               │
└─────────────────┬───────────────────────────┘
                  │
     ┌────────────┼────────────┐
     │            │            │
     ▼            ▼            ▼
┌─────────┐  ┌─────────┐  ┌─────────┐
│ Mod C   │  │ Mod B   │  │ Base    │
│ Assets/ │  │ Assets/ │  │ Assets/ │
│ logo.png│  │ logo.png│  │ logo.png│
└────┬────┘  └─────────┘  └─────────┘
     │
     │ Found!
     │
     ▼
┌─────────────────────────────────────────────┐
│  Load file from Mod C                       │
│  Cache with key: "logo.png"                 │
│  Track override: Mod C → Base               │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│  Return Texture2D to caller                 │
└─────────────────────────────────────────────┘
```

## Implementation Design

### 1. PokeNET.ModApi Project Structure

```
PokeNET.ModApi/
├── PokeNET.ModApi.csproj
├── IMod.cs                      # Core mod interface
├── IModManifest.cs              # Manifest data contract
├── IModContext.cs               # Runtime context for mods
├── IModLoader.cs                # Loader interface
├── IAssetApi.cs                 # Asset access for mods
├── IEntityApi.cs                # ECS entity access
├── ILoggerApi.cs                # Logging facade
├── Attributes/
│   ├── ModEntryPointAttribute.cs
│   └── ModDependencyAttribute.cs
├── Data/
│   ├── ModDependency.cs
│   ├── ModVersion.cs
│   └── ModLoadOrder.cs
└── Exceptions/
    ├── ModLoadException.cs
    ├── ModValidationException.cs
    └── ModDependencyException.cs
```

### 2. Mod Loading Algorithm (Kahn's Topological Sort)

**Pseudocode**:
```
function LoadMods(modsDirectory):
    # Phase 1: Discovery
    modDirectories = Directory.GetDirectories(modsDirectory)
    manifests = []

    for dir in modDirectories:
        manifestPath = Path.Combine(dir, "modinfo.json")
        if File.Exists(manifestPath):
            manifest = ParseManifest(manifestPath)
            ValidateManifest(manifest)
            manifests.Add(manifest)

    # Phase 2: Dependency Resolution
    graph = BuildDependencyGraph(manifests)

    if HasCycles(graph):
        throw ModDependencyException("Circular dependencies detected")

    loadOrder = TopologicalSort(graph)

    # Phase 3: Loading
    loadedMods = []

    for modId in loadOrder:
        manifest = manifests.Find(m => m.Id == modId)

        try:
            # Load data assets (JSON/XML)
            LoadDataAssets(manifest.Directory)

            # Load content assets (textures, audio)
            LoadContentAssets(manifest.Directory)

            # Load code mod (if present)
            if manifest.HasCodeMod:
                mod = LoadCodeMod(manifest)
                mod.Initialize(CreateModContext(manifest))
                loadedMods.Add(mod)
        catch Exception e:
            LogError($"Failed to load mod {modId}: {e.Message}")
            if manifest.Required:
                throw

    return loadedMods

function TopologicalSort(graph):
    # Kahn's algorithm
    inDegree = {}
    adjList = {}

    for node in graph.Nodes:
        inDegree[node] = 0
        adjList[node] = []

    for edge in graph.Edges:
        adjList[edge.From].Add(edge.To)
        inDegree[edge.To] += 1

    queue = Queue()
    for node, degree in inDegree:
        if degree == 0:
            queue.Enqueue(node)

    result = []

    while queue.Count > 0:
        node = queue.Dequeue()
        result.Add(node)

        for neighbor in adjList[node]:
            inDegree[neighbor] -= 1
            if inDegree[neighbor] == 0:
                queue.Enqueue(neighbor)

    if result.Count != graph.Nodes.Count:
        throw ModDependencyException("Cycle detected")

    return result
```

### 3. Harmony Integration Design

**Harmony Instance Management**:
```csharp
public class ModHarmonyManager
{
    private readonly Dictionary<string, Harmony> _harmonyInstances = new();
    private readonly Dictionary<string, List<MethodBase>> _patchedMethods = new();

    public void ApplyPatches(IMod mod, IModManifest manifest)
    {
        var harmonyId = $"pokenet.mod.{manifest.Id}";
        var harmony = new Harmony(harmonyId);

        try
        {
            // Scan mod assembly for [HarmonyPatch] attributes
            var modAssembly = mod.GetType().Assembly;
            harmony.PatchAll(modAssembly);

            // Track patches for conflict detection
            var patches = harmony.GetPatchedMethods().ToList();
            _patchedMethods[manifest.Id] = patches;
            _harmonyInstances[manifest.Id] = harmony;

            // Check for conflicts
            DetectPatchConflicts(manifest.Id, patches);

            _logger.LogInformation(
                "Applied {PatchCount} Harmony patches for mod {ModId}",
                patches.Count, manifest.Id);
        }
        catch (Exception ex)
        {
            // Rollback on failure
            RollbackPatches(harmonyId);
            throw new ModLoadException(
                $"Failed to apply Harmony patches for mod {manifest.Id}", ex);
        }
    }

    private void DetectPatchConflicts(string modId, List<MethodBase> patches)
    {
        foreach (var method in patches)
        {
            var allPatches = Harmony.GetPatchInfo(method);
            if (allPatches == null) continue;

            // Check if multiple mods patch the same method
            var prefixes = allPatches.Prefixes.Select(p => p.owner).Distinct().ToList();
            var postfixes = allPatches.Postfixes.Select(p => p.owner).Distinct().ToList();
            var transpilers = allPatches.Transpilers.Select(p => p.owner).Distinct().ToList();

            if (prefixes.Count > 1 || postfixes.Count > 1 || transpilers.Count > 1)
            {
                _logger.LogWarning(
                    "Potential patch conflict on {Method}: {Owners}",
                    method.Name,
                    string.Join(", ", prefixes.Concat(postfixes).Concat(transpilers)));
            }
        }
    }
}
```

### 4. Asset Override System

**Integration with AssetManager**:
```csharp
public class AssetManager
{
    private readonly List<string> _assetSearchPaths = new();
    private readonly Dictionary<string, string> _assetOverrides = new();

    public void RegisterModAssetPath(string modId, string path, int priority)
    {
        // Insert at position based on load order (higher priority = searched first)
        _assetSearchPaths.Insert(priority, path);
        _logger.LogDebug("Registered asset path for mod {ModId}: {Path}", modId, path);
    }

    public T Load<T>(string assetName) where T : class
    {
        // Try each search path in order (mods first, base game last)
        foreach (var searchPath in _assetSearchPaths)
        {
            var fullPath = Path.Combine(searchPath, assetName);

            if (File.Exists(fullPath))
            {
                // Track override chain for debugging
                if (_assetOverrides.TryGetValue(assetName, out var existingSource))
                {
                    _logger.LogDebug(
                        "Asset {AssetName} overridden: {NewSource} → {OldSource}",
                        assetName, searchPath, existingSource);
                }

                _assetOverrides[assetName] = searchPath;

                return LoadAsset<T>(fullPath);
            }
        }

        throw new AssetNotFoundException($"Asset not found: {assetName}");
    }
}
```

## Security Considerations

### 1. Mod Validation

**Required Checks**:
- Manifest schema validation
- Version compatibility verification
- Dependency existence validation
- File path sanitization (prevent directory traversal)
- Assembly signature verification (optional, for trusted mods)

### 2. Sandboxing Strategy

**Current Limitations**:
- Harmony patches have full access to game internals
- Mod code runs in same AppDomain as game
- No CPU/memory limits enforced

**Mitigation**:
- Clear trust level indicators in UI ("This mod can modify game code")
- User consent required for code mods
- Future: Investigate AssemblyLoadContext isolation
- Future: Scripting API with limited capabilities

### 3. Safe Harmony Patching

**Best Practices**:
- Validate patch targets exist before applying
- Use try-catch in patch methods to prevent crashes
- Log all patch applications for debugging
- Automatic rollback on patch failure
- Conflict detection and warnings

## Performance Considerations

### 1. Startup Performance

**Optimization Strategies**:
- Parallel manifest parsing (async I/O)
- Lazy loading of mod assemblies (only when needed)
- Asset preloading hints in manifest
- Cached dependency resolution (hash-based)

### 2. Runtime Performance

**Considerations**:
- Harmony patches add overhead to patched methods (~10-50ns per patch)
- Asset override resolution cached after first lookup
- Mod data merged into main data structures (no lookup overhead)

### 3. Memory Management

**Strategies**:
- Unload unused mod assemblies (if using AssemblyLoadContext)
- Share asset instances between mods and base game
- Mod-specific resource pools

## Testing Strategy

### 1. Unit Tests

**Coverage Areas**:
- Manifest parsing and validation
- Dependency resolution (including cycle detection)
- Version compatibility checking
- Asset override resolution
- Harmony patch application and rollback

### 2. Integration Tests

**Scenarios**:
- Load multiple mods with complex dependencies
- Override assets from multiple mods
- Apply conflicting Harmony patches
- Handle mod load failures gracefully
- Hot reload mods during development

### 3. Example Test Cases

```csharp
[Fact]
public void ModLoader_DetectsCyclicDependencies()
{
    // Mod A depends on B, B depends on C, C depends on A
    var mods = CreateCyclicDependencyMods();

    var exception = Assert.Throws<ModDependencyException>(
        () => modLoader.LoadMods(mods));

    Assert.Contains("Circular dependency", exception.Message);
}

[Fact]
public void AssetManager_RespectsLoadOrder()
{
    // Base game has logo.png
    // Mod A overrides logo.png
    // Mod B loads after A and also overrides logo.png

    assetManager.RegisterModAssetPath("base", "/base/assets", priority: 2);
    assetManager.RegisterModAssetPath("modA", "/mods/A/assets", priority: 1);
    assetManager.RegisterModAssetPath("modB", "/mods/B/assets", priority: 0);

    var logo = assetManager.Load<Texture2D>("logo.png");

    // Should load from Mod B (highest priority)
    Assert.Equal("/mods/B/assets/logo.png", logo.SourcePath);
}
```

## Backward Compatibility Plan

### Version Migration Strategy

**Supported Transitions**:
- Mods compiled against API v1.0 work with runtime v1.x (minor updates)
- Mods compiled against API v1.x require recompile for v2.0 (major updates)
- Deprecated APIs marked with `[Obsolete("Use X instead", error: false)]`
- Breaking changes documented in migration guide

**Example Migration**:
```csharp
// API v1.0
public interface IMod
{
    void Initialize(IModContext context);
}

// API v2.0 (breaking change)
public interface IMod
{
    Task InitializeAsync(IModContext context, CancellationToken ct);

    // Legacy support via adapter
    void Initialize(IModContext context) =>
        InitializeAsync(context, CancellationToken.None).GetAwaiter().GetResult();
}
```

## Future Enhancements

### Phase 4.1: Enhanced Mod Management
- In-game mod browser UI
- One-click mod installation
- Automatic dependency downloads
- Mod update notifications

### Phase 4.2: Advanced Harmony Features
- Patch priority system
- Patch compatibility database
- Automatic conflict resolution suggestions

### Phase 4.3: Mod Development Tools
- Visual Studio project template
- Mod debugging support (attach debugger to game)
- Hot reload during development
- Mod validation tool (pre-publish checks)

### Phase 4.4: Community Features
- Mod workshop integration
- Mod ratings and reviews
- Automatic crash reporting (mod-specific)
- Mod compatibility matrix

## References

### External Documentation
- [Harmony Documentation](https://harmony.pardeike.net/)
- [RimWorld Modding Guide](https://rimworldwiki.com/wiki/Modding)
- [Semantic Versioning](https://semver.org/)

### Internal Documentation
- `docs/architecture/mod-manifest-schema.json`
- `docs/architecture/harmony-integration.md`
- `GAME_FRAMEWORK_PLAN.md` - Phase 4 requirements

## Appendix A: Glossary

- **Mod**: User-created extension that adds or modifies game functionality
- **Manifest**: Metadata file describing a mod's identity, dependencies, and capabilities
- **Harmony**: Runtime patching library for .NET using IL manipulation
- **Topological Sort**: Algorithm for ordering nodes in a directed acyclic graph
- **Asset Override**: Mechanism where mods replace base game assets
- **Code Mod**: Mod that includes compiled .NET assemblies
- **Data Mod**: Mod that only includes JSON/XML configuration files
- **Content Mod**: Mod that only includes art, audio, or other media assets

---

**Document Version**: 1.0
**Last Updated**: 2025-10-22
**Authors**: System Architect Agent (PokeNET Hive Mind)
**Status**: Final Design - Ready for Implementation
