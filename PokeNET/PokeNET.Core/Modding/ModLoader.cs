using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using PokeNET.Domain.Modding;

namespace PokeNET.Core.Modding;

/// <summary>
/// Manages mod discovery, loading, and lifecycle.
/// Implements topological sorting for dependency resolution.
/// </summary>
public sealed class ModLoader : IModLoader
{
    private readonly ILogger<ModLoader> _logger;
    private readonly IServiceProvider _services;
    private readonly ILoggerFactory _loggerFactory;
    private readonly List<ModManifest> _discoveredManifests = new();
    private readonly List<LoadedMod> _loadedMods = new();
    private readonly Dictionary<string, IMod> _modInstances = new();

    public string ModsDirectory { get; }
    public IReadOnlyList<IModManifest> LoadedMods => _loadedMods.Select(m => (IModManifest)m.Manifest).ToList();
    public IReadOnlyList<IModManifest> DiscoveredMods => _discoveredManifests;

    /// <summary>
    /// Creates a new mod loader instance.
    /// </summary>
    /// <param name="logger">Logger for mod loading operations.</param>
    /// <param name="services">Service provider for dependency injection.</param>
    /// <param name="loggerFactory">Factory for creating mod-specific loggers.</param>
    /// <param name="modsDirectory">Directory containing mod subdirectories.</param>
    public ModLoader(
        ILogger<ModLoader> logger,
        IServiceProvider services,
        ILoggerFactory loggerFactory,
        string modsDirectory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        ModsDirectory = modsDirectory ?? throw new ArgumentNullException(nameof(modsDirectory));
    }

    public int DiscoverMods()
    {
        _discoveredManifests.Clear();

        if (!Directory.Exists(ModsDirectory))
        {
            _logger.LogWarning("Mods directory does not exist: {ModsDirectory}", ModsDirectory);
            return 0;
        }

        var modDirectories = Directory.GetDirectories(ModsDirectory);
        _logger.LogInformation("Scanning {Count} directories for mods", modDirectories.Length);

        foreach (var modDir in modDirectories)
        {
            // SECURITY FIX VULN-006: Validate mod directory is within ModsDirectory
            try
            {
                var fullModDir = Path.GetFullPath(modDir);
                var fullModsDir = Path.GetFullPath(ModsDirectory);

                if (!fullModDir.StartsWith(fullModsDir, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Skipping directory outside mod path: {Directory}", modDir);
                    continue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating mod directory: {Directory}", modDir);
                continue;
            }

            var manifestPath = Path.Combine(modDir, "modinfo.json");
            if (!File.Exists(manifestPath))
            {
                _logger.LogDebug("Skipping {Directory} - no modinfo.json found", modDir);
                continue;
            }

            try
            {
                var json = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<ModManifest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });

                if (manifest == null)
                {
                    _logger.LogWarning("Failed to deserialize manifest: {Path}", manifestPath);
                    continue;
                }

                _discoveredManifests.Add(manifest);
                _logger.LogInformation("Discovered mod: {Name} ({Id}) v{Version}",
                    manifest.Name, manifest.Id, manifest.Version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading manifest from {Path}", manifestPath);
            }
        }

        return _discoveredManifests.Count;
    }

    public void LoadMods()
    {
        if (_discoveredManifests.Count == 0)
        {
            _logger.LogInformation("No mods to load");
            return;
        }

        _logger.LogInformation("Resolving load order for {Count} mods", _discoveredManifests.Count);
        var loadOrder = ResolveLoadOrder(_discoveredManifests);

        _logger.LogInformation("Loading mods in order: {Order}",
            string.Join(", ", loadOrder.Select(m => m.Id)));

        foreach (var manifest in loadOrder)
        {
            try
            {
                LoadMod(manifest);
            }
            catch (Exception ex)
            {
                throw new ModLoadException(manifest.Id,
                    $"Failed to load mod '{manifest.Name}' ({manifest.Id})", ex);
            }
        }

        _logger.LogInformation("Successfully loaded {Count} mods", _loadedMods.Count);
    }

    private void LoadMod(ModManifest manifest)
    {
        // SECURITY FIX VULN-006: Sanitize and validate mod path to prevent directory traversal
        var modDir = SanitizeModPath(manifest.Id, ModsDirectory);
        var assemblyPath = Path.Combine(modDir, manifest.GetAssemblyFileName());

        // Additional validation for assembly path
        var sanitizedAssemblyPath = ValidateModFilePath(assemblyPath, modDir);

        if (!File.Exists(sanitizedAssemblyPath))
        {
            throw new ModLoadException(manifest.Id,
                $"Assembly not found: {sanitizedAssemblyPath}");
        }

        _logger.LogDebug("Loading assembly: {Path}", sanitizedAssemblyPath);
        var assembly = Assembly.LoadFrom(sanitizedAssemblyPath);

        // Find the IMod implementation
        var modType = assembly.GetTypes()
            .FirstOrDefault(t => typeof(IMod).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        if (modType == null)
        {
            throw new ModLoadException(manifest.Id,
                $"No IMod implementation found in assembly: {assembly.FullName}");
        }

        _logger.LogDebug("Instantiating mod type: {Type}", modType.FullName);
        var modInstance = (IMod?)Activator.CreateInstance(modType);

        if (modInstance == null)
        {
            throw new ModLoadException(manifest.Id,
                $"Failed to instantiate mod type: {modType.FullName}");
        }

        // Create context and initialize mod
        var context = new ModContext(manifest, modDir, _services, _loggerFactory, this);

        _logger.LogInformation("Initializing mod: {Name} ({Id}) v{Version}",
            manifest.Name, manifest.Id, manifest.Version);

        // Initialize the mod asynchronously with ConfigureAwait to prevent deadlocks
        modInstance.InitializeAsync(context, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

        var loadedMod = new LoadedMod(manifest, modInstance, context, assembly);
        _loadedMods.Add(loadedMod);
        _modInstances[manifest.Id] = modInstance;

        _logger.LogInformation("Loaded mod: {Name} ({Id})", manifest.Name, manifest.Id);
    }

    public void UnloadMods()
    {
        _logger.LogInformation("Unloading {Count} mods", _loadedMods.Count);

        // Unload in reverse order
        for (int i = _loadedMods.Count - 1; i >= 0; i--)
        {
            var mod = _loadedMods[i];
            try
            {
                _logger.LogDebug("Unloading mod: {Id}", mod.Manifest.Id);
                mod.Instance.ShutdownAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading mod: {Id}", mod.Manifest.Id);
            }
        }

        _loadedMods.Clear();
        _modInstances.Clear();
        _logger.LogInformation("All mods unloaded");
    }

    public IMod? GetMod(string modId)
    {
        _modInstances.TryGetValue(modId, out var mod);
        return mod;
    }

    public bool IsModLoaded(string modId)
    {
        return _modInstances.ContainsKey(modId);
    }

    public async Task LoadModsAsync(string modsDirectory, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            DiscoverMods();

            cancellationToken.ThrowIfCancellationRequested();
            LoadMods();
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task ReloadModAsync(string modId, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var mod = _loadedMods.FirstOrDefault(m => m.Manifest.Id == modId);
            if (mod == null)
            {
                throw new ModLoadException($"Mod not found: {modId}", modId);
            }

            // Unload the mod
            _logger.LogInformation("Reloading mod: {ModId}", modId);
            mod.Instance.ShutdownAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
            _loadedMods.Remove(mod);
            _modInstances.Remove(modId);

            cancellationToken.ThrowIfCancellationRequested();

            // Reload it
            LoadMod(mod.Manifest);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task UnloadModAsync(string modId, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var mod = _loadedMods.FirstOrDefault(m => m.Manifest.Id == modId);
            if (mod == null)
            {
                throw new ModLoadException($"Mod not found: {modId}", modId);
            }

            _logger.LogInformation("Unloading mod: {ModId}", modId);
            mod.Instance.ShutdownAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
            _loadedMods.Remove(mod);
            _modInstances.Remove(modId);
        }, cancellationToken).ConfigureAwait(false);
    }

    public IReadOnlyList<string> GetLoadOrder()
    {
        return _loadedMods.Select(m => m.Manifest.Id).ToList();
    }

    public async Task<ModValidationReport> ValidateModsAsync(string modsDirectory)
    {
        return await Task.Run(() =>
        {
            var report = new ModValidationReport();

            if (!Directory.Exists(modsDirectory))
            {
                report.Errors.Add(new ModValidationError
                {
                    Message = $"Mods directory does not exist: {modsDirectory}",
                    ErrorType = ModValidationErrorType.InvalidManifest
                });
                return report;
            }

            var modDirectories = Directory.GetDirectories(modsDirectory);
            var manifests = new List<ModManifest>();

            // Validate individual manifests
            foreach (var modDir in modDirectories)
            {
                var manifestPath = Path.Combine(modDir, "modinfo.json");
                if (!File.Exists(manifestPath))
                {
                    continue;
                }

                try
                {
                    var json = File.ReadAllText(manifestPath);
                    var manifest = JsonSerializer.Deserialize<ModManifest>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    });

                    if (manifest == null)
                    {
                        report.Errors.Add(new ModValidationError
                        {
                            Message = $"Failed to deserialize manifest: {manifestPath}",
                            ErrorType = ModValidationErrorType.InvalidManifest
                        });
                        continue;
                    }

                    manifests.Add(manifest);

                    // Check for assembly file
                    var assemblyPath = Path.Combine(modDir, manifest.GetAssemblyFileName());
                    if (!File.Exists(assemblyPath) && manifest.ModType == ModType.Code)
                    {
                        report.Errors.Add(new ModValidationError
                        {
                            ModId = manifest.Id,
                            Message = $"Assembly not found: {assemblyPath}",
                            ErrorType = ModValidationErrorType.MissingAssembly
                        });
                    }
                }
                catch (Exception ex)
                {
                    report.Errors.Add(new ModValidationError
                    {
                        Message = $"Error reading manifest from {manifestPath}: {ex.Message}",
                        ErrorType = ModValidationErrorType.InvalidManifest
                    });
                }
            }

            // Check for duplicate IDs
            var duplicateIds = manifests.GroupBy(m => m.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicateId in duplicateIds)
            {
                report.Errors.Add(new ModValidationError
                {
                    ModId = duplicateId,
                    Message = $"Duplicate mod ID detected: {duplicateId}",
                    ErrorType = ModValidationErrorType.DuplicateModId
                });
            }

            // Validate dependencies
            var modMap = manifests.ToDictionary(m => m.Id);
            foreach (var manifest in manifests)
            {
                foreach (var dep in manifest.Dependencies)
                {
                    if (!modMap.ContainsKey(dep.ModId) && !dep.Optional)
                    {
                        report.Errors.Add(new ModValidationError
                        {
                            ModId = manifest.Id,
                            Message = $"Missing required dependency: {dep.ModId}",
                            ErrorType = ModValidationErrorType.MissingDependency
                        });
                    }
                    else if (!modMap.ContainsKey(dep.ModId) && dep.Optional)
                    {
                        report.Warnings.Add(new ModValidationWarning
                        {
                            ModId = manifest.Id,
                            Message = $"Optional dependency not found: {dep.ModId}",
                            WarningType = ModValidationWarningType.MissingOptionalDependency
                        });
                    }
                }

                // Check incompatibilities
                foreach (var incomp in manifest.IncompatibleWith)
                {
                    if (modMap.ContainsKey(incomp.ModId))
                    {
                        report.Errors.Add(new ModValidationError
                        {
                            ModId = manifest.Id,
                            Message = $"Incompatible mod present: {incomp.ModId}" +
                                     (incomp.Reason != null ? $" - {incomp.Reason}" : ""),
                            ErrorType = ModValidationErrorType.IncompatibleModLoaded
                        });
                    }
                }
            }

            // Try to resolve load order to detect circular dependencies
            try
            {
                ResolveLoadOrder(manifests);
            }
            catch (ModLoadException ex) when (ex.Message.Contains("Circular dependency"))
            {
                report.Errors.Add(new ModValidationError
                {
                    Message = ex.Message,
                    ErrorType = ModValidationErrorType.CircularDependency
                });
            }

            return report;
        });
    }

    /// <summary>
    /// Resolves the load order using topological sort (Kahn's algorithm).
    /// Handles dependencies, loadAfter, and loadBefore constraints.
    /// </summary>
    private List<ModManifest> ResolveLoadOrder(List<ModManifest> mods)
    {
        var modMap = mods.ToDictionary(m => m.Id);
        var result = new List<ModManifest>();
        var inDegree = new Dictionary<string, int>();
        var adjacency = new Dictionary<string, List<string>>();

        // Initialize graph
        foreach (var mod in mods)
        {
            inDegree[mod.Id] = 0;
            adjacency[mod.Id] = new List<string>();
        }

        // Build dependency graph
        foreach (var mod in mods)
        {
            // Hard dependencies (must be loaded first)
            foreach (var dep in mod.Dependencies)
            {
                if (!modMap.ContainsKey(dep.ModId))
                {
                    throw new ModLoadException(
                        $"Missing required dependency: {dep.ModId}" +
                        (dep.Version != null ? $" {dep.Version}" : ""),
                        mod.Id);
                }

                adjacency[dep.ModId].Add(mod.Id);
                inDegree[mod.Id]++;
            }

            // LoadAfter (soft dependency)
            foreach (var afterId in mod.LoadAfter)
            {
                if (modMap.ContainsKey(afterId))
                {
                    adjacency[afterId].Add(mod.Id);
                    inDegree[mod.Id]++;
                }
            }

            // LoadBefore (this mod should load before others)
            foreach (var beforeId in mod.LoadBefore)
            {
                if (modMap.ContainsKey(beforeId))
                {
                    adjacency[mod.Id].Add(beforeId);
                    inDegree[beforeId]++;
                }
            }
        }

        // Kahn's algorithm
        var queue = new Queue<string>(inDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));

        while (queue.Count > 0)
        {
            var modId = queue.Dequeue();
            result.Add(modMap[modId]);

            foreach (var dependent in adjacency[modId])
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        // Check for circular dependencies
        if (result.Count != mods.Count)
        {
            var unresolved = mods.Where(m => !result.Contains(m)).ToList();
            var unresolvedIds = unresolved.Select(m => m.Id).ToList();

            // Try to detect the actual circular dependency chain
            var cycles = DetectDependencyCycles(unresolved, modMap, adjacency);

            throw new ModLoadException(
                $"Circular dependency detected: {cycles}. Unresolved mods: {string.Join(", ", unresolvedIds)}");
        }

        return result;
    }

    /// <summary>
    /// Detects and reports the actual circular dependency chain for better error messages.
    /// </summary>
    private string DetectDependencyCycles(
        List<ModManifest> unresolved,
        Dictionary<string, ModManifest> modMap,
        Dictionary<string, List<string>> adjacency)
    {
        // Use depth-first search to find cycles
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        var cyclePath = new List<string>();

        foreach (var mod in unresolved)
        {
            if (FindCycle(mod.Id, visited, recursionStack, cyclePath, adjacency))
            {
                // Found a cycle, return the chain
                return string.Join(" -> ", cyclePath) + " -> " + cyclePath[0];
            }
        }

        // No specific cycle found, return generic message
        return "Unable to determine exact cycle path";
    }

    private bool FindCycle(
        string modId,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        List<string> path,
        Dictionary<string, List<string>> adjacency)
    {
        if (recursionStack.Contains(modId))
        {
            // Found a cycle, build the path
            path.Add(modId);
            return true;
        }

        if (visited.Contains(modId))
        {
            return false;
        }

        visited.Add(modId);
        recursionStack.Add(modId);
        path.Add(modId);

        if (adjacency.TryGetValue(modId, out var dependencies))
        {
            foreach (var dep in dependencies)
            {
                if (FindCycle(dep, visited, recursionStack, path, adjacency))
                {
                    return true;
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        recursionStack.Remove(modId);
        return false;
    }

    /// <summary>
    /// SECURITY: Sanitizes mod path to prevent directory traversal attacks
    /// </summary>
    private static string SanitizeModPath(string modId, string modsDirectory)
    {
        if (string.IsNullOrWhiteSpace(modId))
        {
            throw new System.Security.SecurityException("Mod ID cannot be null or empty");
        }

        // Remove any path traversal characters
        if (modId.Contains("..") || modId.Contains('/') || modId.Contains('\\'))
        {
            throw new System.Security.SecurityException($"Mod path traversal detected in ID: {modId}");
        }

        var modPath = Path.Combine(modsDirectory, modId);
        var fullPath = Path.GetFullPath(modPath);
        var modsFullPath = Path.GetFullPath(modsDirectory);

        if (!fullPath.StartsWith(modsFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new System.Security.SecurityException($"Mod path traversal detected: {modId}");
        }

        return fullPath;
    }

    /// <summary>
    /// SECURITY: Validates that a mod file path is within the mod's directory
    /// </summary>
    private static string ValidateModFilePath(string filePath, string modDirectory)
    {
        var fullPath = Path.GetFullPath(filePath);
        var modFullPath = Path.GetFullPath(modDirectory);

        if (!fullPath.StartsWith(modFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new System.Security.SecurityException($"File path traversal detected: {filePath}");
        }

        return fullPath;
    }

    private sealed record LoadedMod(
        ModManifest Manifest,
        IMod Instance,
        ModContext Context,
        Assembly Assembly
    );
}
