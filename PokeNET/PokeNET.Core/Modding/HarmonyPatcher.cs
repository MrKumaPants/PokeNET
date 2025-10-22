using HarmonyLib;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.Modding;

namespace PokeNET.Core.Modding;

/// <summary>
/// Manages Harmony patches for mods.
/// Each mod gets its own Harmony instance to allow isolated patching and rollback.
/// </summary>
public sealed class HarmonyPatcher : IDisposable
{
    private readonly ILogger<HarmonyPatcher> _logger;
    private readonly Dictionary<string, Harmony> _harmonyInstances = new();
    private readonly Dictionary<string, List<PatchInfo>> _appliedPatches = new();

    public HarmonyPatcher(ILogger<HarmonyPatcher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Applies Harmony patches from a mod's assembly.
    /// Creates a dedicated Harmony instance for the mod.
    /// </summary>
    /// <param name="modId">Unique mod identifier.</param>
    /// <param name="assembly">Assembly containing Harmony patch classes.</param>
    public void ApplyPatches(string modId, System.Reflection.Assembly assembly)
    {
        if (string.IsNullOrEmpty(modId))
            throw new ArgumentException("Mod ID cannot be null or empty", nameof(modId));
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        _logger.LogInformation("Applying Harmony patches for mod: {ModId}", modId);

        try
        {
            // Create Harmony instance for this mod
            var harmonyId = $"pokenet.mod.{modId}";
            var harmony = new Harmony(harmonyId);
            _harmonyInstances[modId] = harmony;

            // Apply all patches from the assembly
            var patchesBefore = Harmony.GetAllPatchedMethods().Count();
            harmony.PatchAll(assembly);
            var patchesAfter = Harmony.GetAllPatchedMethods().Count();

            // Track applied patches for this mod
            var patches = new List<PatchInfo>();
            foreach (var method in Harmony.GetAllPatchedMethods())
            {
                var patchInfo = Harmony.GetPatchInfo(method);
                if (patchInfo != null)
                {
                    // Check if any patches belong to this mod
                    var modPatches = new[] { patchInfo.Prefixes, patchInfo.Postfixes, patchInfo.Transpilers, patchInfo.Finalizers }
                        .SelectMany(p => p)
                        .Where(p => p.owner == harmonyId);

                    foreach (var patch in modPatches)
                    {
                        patches.Add(new PatchInfo(method, patch.PatchMethod, patch.index));
                        _logger.LogDebug("Applied {PatchType} patch to {Method}",
                            patch.PatchMethod.Name, method.FullDescription());
                    }
                }
            }

            _appliedPatches[modId] = patches;
            _logger.LogInformation("Applied {Count} Harmony patches for mod: {ModId}",
                patches.Count, modId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply Harmony patches for mod: {ModId}", modId);
            throw new ModLoadException(modId, "Failed to apply Harmony patches", ex);
        }
    }

    /// <summary>
    /// Removes all Harmony patches applied by a specific mod.
    /// </summary>
    /// <param name="modId">Unique mod identifier.</param>
    public void RemovePatches(string modId)
    {
        if (!_harmonyInstances.TryGetValue(modId, out var harmony))
        {
            _logger.LogWarning("No Harmony instance found for mod: {ModId}", modId);
            return;
        }

        try
        {
            _logger.LogInformation("Removing Harmony patches for mod: {ModId}", modId);
            harmony.UnpatchAll(harmony.Id);

            _harmonyInstances.Remove(modId);
            _appliedPatches.Remove(modId);

            _logger.LogInformation("Removed all patches for mod: {ModId}", modId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing patches for mod: {ModId}", modId);
        }
    }

    /// <summary>
    /// Gets information about all patches applied by a mod.
    /// </summary>
    /// <param name="modId">Unique mod identifier.</param>
    /// <returns>List of applied patches or empty list if none found.</returns>
    public IReadOnlyList<PatchInfo> GetAppliedPatches(string modId)
    {
        return _appliedPatches.TryGetValue(modId, out var patches)
            ? patches
            : Array.Empty<PatchInfo>();
    }

    /// <summary>
    /// Checks if there are any patch conflicts between mods.
    /// Returns a list of methods that are patched by multiple mods.
    /// </summary>
    public IReadOnlyList<PatchConflict> DetectConflicts()
    {
        var conflicts = new List<PatchConflict>();
        var methodPatches = new Dictionary<System.Reflection.MethodBase, List<string>>();

        // Group patches by target method
        foreach (var (modId, patches) in _appliedPatches)
        {
            foreach (var patch in patches)
            {
                if (!methodPatches.ContainsKey(patch.TargetMethod))
                {
                    methodPatches[patch.TargetMethod] = new List<string>();
                }
                methodPatches[patch.TargetMethod].Add(modId);
            }
        }

        // Find methods patched by multiple mods
        foreach (var (method, modIds) in methodPatches)
        {
            if (modIds.Count > 1)
            {
                conflicts.Add(new PatchConflict(method, modIds));
                _logger.LogWarning("Patch conflict detected on {Method}: {Mods}",
                    method.FullDescription(), string.Join(", ", modIds));
            }
        }

        return conflicts;
    }

    public void Dispose()
    {
        // Remove all patches
        foreach (var modId in _harmonyInstances.Keys.ToList())
        {
            RemovePatches(modId);
        }
    }
}

/// <summary>
/// Information about a single applied Harmony patch.
/// </summary>
public sealed record PatchInfo(
    System.Reflection.MethodBase TargetMethod,
    System.Reflection.MethodInfo PatchMethod,
    int Priority
);

/// <summary>
/// Represents a conflict where multiple mods patch the same method.
/// </summary>
public sealed record PatchConflict(
    System.Reflection.MethodBase TargetMethod,
    IReadOnlyList<string> ConflictingMods
);
