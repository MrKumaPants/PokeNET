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
    private bool _disposed;

    // SECURITY FIX VULN-009: Allowlist of types that mods are permitted to patch
    private static readonly HashSet<string> AllowedPatchTargets = new()
    {
        "PokeNET.Domain.ECS.Systems.BattleSystem",
        "PokeNET.Domain.ECS.Systems.MovementSystem",
        "PokeNET.Domain.ECS.Systems.RenderSystem",
        "PokeNET.Domain.ECS.Systems.ItemSystem",
        "PokeNET.Domain.ECS.Systems.InteractionSystem",
        "PokeNET.Domain.Pokemon.PokemonStats",
        "PokeNET.Domain.Pokemon.MoveEffects",
        "PokeNET.Domain.Items.ItemEffects",
        "PokeNET.Domain.Combat.DamageCalculator",
        "PokeNET.Domain.Combat.StatusEffects"
    };

    // SECURITY: Critical types that must NEVER be patched
    private static readonly HashSet<string> BlockedPatchTargets = new()
    {
        "PokeNET.Core.Modding.ModLoader",
        "PokeNET.Core.Modding.HarmonyPatcher",
        "PokeNET.Scripting.Security.ScriptSandbox",
        "PokeNET.Scripting.Security.SecurityValidator",
        "PokeNET.Core.Assets.AssetManager",
        "System.Reflection.Assembly",
        "System.Security",
        "System.IO.File",
        "System.IO.Directory"
    };

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
        if (_disposed)
            throw new ObjectDisposedException(nameof(HarmonyPatcher));

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

            // SECURITY FIX VULN-009: Validate all patches before applying
            ValidatePatchesInAssembly(assembly);

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
        if (_disposed)
            throw new ObjectDisposedException(nameof(HarmonyPatcher));

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

    /// <summary>
    /// SECURITY: Validates that all patches in an assembly target allowed types
    /// </summary>
    private void ValidatePatchesInAssembly(System.Reflection.Assembly assembly)
    {
        var patchTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0);

        foreach (var patchType in patchTypes)
        {
            var patchAttributes = patchType.GetCustomAttributes(typeof(HarmonyPatch), false)
                .Cast<HarmonyPatch>();

            foreach (var attr in patchAttributes)
            {
                // Extract target type from HarmonyPatch attribute
                var targetType = attr.info?.declaringType?.FullName;

                if (targetType != null)
                {
                    ValidatePatchTarget(targetType, patchType.FullName ?? "Unknown");
                }
            }
        }

        _logger.LogDebug("Patch validation passed for assembly: {Assembly}", assembly.FullName);
    }

    /// <summary>
    /// SECURITY: Validates a single patch target against allowlist and blocklist
    /// </summary>
    private void ValidatePatchTarget(string targetTypeName, string patchTypeName)
    {
        // First check blocklist - these can NEVER be patched
        if (BlockedPatchTargets.Any(blocked => targetTypeName.StartsWith(blocked, StringComparison.OrdinalIgnoreCase)))
        {
            throw new System.Security.SecurityException(
                $"Patch '{patchTypeName}' attempts to modify security-critical type '{targetTypeName}' which is blocked");
        }

        // Then check allowlist - only these can be patched
        if (!AllowedPatchTargets.Any(allowed => targetTypeName.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)))
        {
            throw new System.Security.SecurityException(
                $"Patch '{patchTypeName}' targets '{targetTypeName}' which is not in the allowlist. " +
                $"Allowed targets: {string.Join(", ", AllowedPatchTargets)}");
        }

        _logger.LogDebug("Patch target validated: {TargetType}", targetTypeName);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogInformation("Disposing HarmonyPatcher");

        // Remove all patches
        foreach (var modId in _harmonyInstances.Keys.ToList())
        {
            try
            {
                var harmony = _harmonyInstances[modId];
                harmony.UnpatchAll(harmony.Id);
                _logger.LogDebug("Removed patches for mod: {ModId}", modId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing patches during disposal for mod: {ModId}", modId);
            }
        }

        _harmonyInstances.Clear();
        _appliedPatches.Clear();
        _disposed = true;

        GC.SuppressFinalize(this);
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
