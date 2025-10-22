# Harmony Integration Strategy for PokeNET

## Overview

This document defines the strategy for integrating Lib.Harmony into PokeNET's modding framework, enabling RimWorld-style runtime code patching for deep gameplay modifications.

## What is Harmony?

Harmony is a .NET library that enables runtime method patching using IL (Intermediate Language) manipulation. It allows mods to:
- Insert code before/after existing methods (Prefix/Postfix)
- Replace method implementations entirely (Transpiler)
- Modify method behavior without changing source code

## Integration Architecture

### 1. Harmony Instance Management

**Design Pattern**: One Harmony instance per mod
- Instance ID: `pokenet.mod.{modId}`
- Isolated patch namespaces prevent conflicts
- Easy rollback on mod unload

```csharp
namespace PokeNET.Core.Modding
{
    public class ModHarmonyManager : IModHarmonyManager
    {
        private readonly ILogger<ModHarmonyManager> _logger;
        private readonly Dictionary<string, Harmony> _harmonyInstances;
        private readonly Dictionary<string, List<MethodBase>> _patchRegistry;
        private readonly ConcurrentDictionary<MethodBase, List<string>> _patchConflicts;

        public ModHarmonyManager(ILogger<ModHarmonyManager> logger)
        {
            _logger = logger;
            _harmonyInstances = new Dictionary<string, Harmony>();
            _patchRegistry = new Dictionary<string, List<MethodBase>>();
            _patchConflicts = new ConcurrentDictionary<MethodBase, List<string>>();
        }

        public void ApplyPatches(IMod mod, IModManifest manifest)
        {
            var harmonyId = manifest.HarmonyId ?? $"pokenet.mod.{manifest.Id}";

            _logger.LogInformation(
                "Applying Harmony patches for mod {ModId} (Harmony ID: {HarmonyId})",
                manifest.Id, harmonyId);

            try
            {
                var harmony = new Harmony(harmonyId);
                var modAssembly = mod.GetType().Assembly;

                // Apply all patches marked with [HarmonyPatch]
                harmony.PatchAll(modAssembly);

                // Track patches
                var patchedMethods = harmony.GetPatchedMethods().ToList();
                _harmonyInstances[manifest.Id] = harmony;
                _patchRegistry[manifest.Id] = patchedMethods;

                _logger.LogInformation(
                    "Applied {PatchCount} Harmony patches for mod {ModId}",
                    patchedMethods.Count, manifest.Id);

                // Analyze for conflicts
                AnalyzePatchConflicts(manifest.Id, patchedMethods);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to apply Harmony patches for mod {ModId}",
                    manifest.Id);

                // Attempt rollback
                RollbackPatches(manifest.Id);
                throw new ModLoadException(
                    $"Harmony patch application failed for mod {manifest.Id}", ex);
            }
        }

        public void RollbackPatches(string modId)
        {
            if (!_harmonyInstances.TryGetValue(modId, out var harmony))
                return;

            try
            {
                _logger.LogInformation("Rolling back Harmony patches for mod {ModId}", modId);
                harmony.UnpatchAll(harmony.Id);

                _harmonyInstances.Remove(modId);
                _patchRegistry.Remove(modId);

                _logger.LogInformation("Rolled back patches for mod {ModId}", modId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to rollback Harmony patches for mod {ModId}", modId);
            }
        }

        private void AnalyzePatchConflicts(string modId, List<MethodBase> patchedMethods)
        {
            foreach (var method in patchedMethods)
            {
                var patches = Harmony.GetPatchInfo(method);
                if (patches == null) continue;

                var allOwners = new HashSet<string>();
                allOwners.UnionWith(patches.Prefixes.Select(p => p.owner));
                allOwners.UnionWith(patches.Postfixes.Select(p => p.owner));
                allOwners.UnionWith(patches.Transpilers.Select(p => p.owner));
                allOwners.UnionWith(patches.Finalizers.Select(p => p.owner));

                if (allOwners.Count > 1)
                {
                    _patchConflicts.AddOrUpdate(
                        method,
                        allOwners.ToList(),
                        (_, existing) =>
                        {
                            existing.AddRange(allOwners.Except(existing));
                            return existing;
                        });

                    _logger.LogWarning(
                        "Patch conflict detected on {DeclaringType}.{MethodName}: " +
                        "Multiple mods patching ({Owners})",
                        method.DeclaringType?.Name ?? "Unknown",
                        method.Name,
                        string.Join(", ", allOwners));
                }
            }
        }

        public IReadOnlyDictionary<MethodBase, List<string>> GetPatchConflicts()
        {
            return _patchConflicts;
        }

        public List<PatchInfo> GetModPatches(string modId)
        {
            if (!_patchRegistry.TryGetValue(modId, out var methods))
                return new List<PatchInfo>();

            return methods.Select(m => new PatchInfo
            {
                MethodName = m.Name,
                DeclaringType = m.DeclaringType?.FullName ?? "Unknown",
                Patches = Harmony.GetPatchInfo(m)
            }).ToList();
        }
    }
}
```

### 2. Patch Application Lifecycle

```
Mod Loading Phase
       │
       ▼
┌──────────────────────┐
│ Load Mod Assembly    │
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ Create Harmony       │
│ Instance             │
│ ID: pokenet.mod.{id} │
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ Scan Assembly for    │
│ [HarmonyPatch]       │
│ Attributes           │
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ Validate Patch       │
│ Targets Exist        │
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ Apply Patches        │
│ (PatchAll)           │
└──────┬───────────────┘
       │
       ├─Success─────────┐
       │                 ▼
       │         ┌───────────────┐
       │         │ Track Patches │
       │         │ Detect        │
       │         │ Conflicts     │
       │         └───────────────┘
       │
       └─Failure─────────┐
                         ▼
                 ┌───────────────┐
                 │ Rollback      │
                 │ UnpatchAll    │
                 │ Throw         │
                 │ Exception     │
                 └───────────────┘
```

### 3. Patch Types and Use Cases

#### Prefix Patches
**Purpose**: Run code before the original method
**Use Cases**:
- Input validation
- Conditional method skipping
- Logging/debugging

**Example**:
```csharp
[HarmonyPatch(typeof(BattleSystem), nameof(BattleSystem.CalculateDamage))]
public class DamageCalculationPatch
{
    // Return false to skip original method
    static bool Prefix(Pokemon attacker, Pokemon defender, Move move, ref int __result)
    {
        // Custom damage calculation for special moves
        if (move.IsSpecial)
        {
            __result = CustomDamageCalculation(attacker, defender, move);
            return false; // Skip original
        }
        return true; // Run original
    }
}
```

#### Postfix Patches
**Purpose**: Run code after the original method
**Use Cases**:
- Result modification
- Side effects
- Logging

**Example**:
```csharp
[HarmonyPatch(typeof(CaptureSystem), nameof(CaptureSystem.AttemptCapture))]
public class CaptureRatePatch
{
    static void Postfix(Pokemon target, ref bool __result)
    {
        // Increase capture rate for endangered species
        if (!__result && target.IsEndangered)
        {
            if (Random.Shared.NextDouble() < 0.2) // 20% bonus chance
            {
                __result = true;
            }
        }
    }
}
```

#### Transpiler Patches
**Purpose**: Modify the IL code of the method
**Use Cases**:
- Performance optimizations
- Deep logic changes
- Complex modifications

**Example**:
```csharp
[HarmonyPatch(typeof(ExperienceSystem), nameof(ExperienceSystem.CalculateExpGain))]
public class ExpMultiplierPatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        // Find multiplication operation and double it
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Mul)
            {
                // Insert: ldc.i4.2, mul (multiply by 2)
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldc_I4_2));
                codes.Insert(i + 2, new CodeInstruction(OpCodes.Mul));
                break;
            }
        }

        return codes;
    }
}
```

#### Finalizer Patches
**Purpose**: Catch exceptions from original method
**Use Cases**:
- Error handling
- Graceful degradation
- Logging exceptions

**Example**:
```csharp
[HarmonyPatch(typeof(SaveSystem), nameof(SaveSystem.SaveGame))]
public class SaveErrorHandlerPatch
{
    static Exception Finalizer(Exception __exception)
    {
        if (__exception != null)
        {
            Logger.LogError($"Save failed: {__exception.Message}");
            NotificationManager.Show("Save failed. Please try again.");
            return null; // Suppress exception
        }
        return __exception;
    }
}
```

### 4. Conflict Detection System

```csharp
public class PatchConflictAnalyzer
{
    public ConflictReport AnalyzeConflicts(List<MethodBase> patchedMethods)
    {
        var report = new ConflictReport();

        foreach (var method in patchedMethods)
        {
            var patches = Harmony.GetPatchInfo(method);
            if (patches == null) continue;

            // Check prefix conflicts
            if (patches.Prefixes.Count > 1)
            {
                var conflict = new PatchConflict
                {
                    MethodName = $"{method.DeclaringType?.Name}.{method.Name}",
                    PatchType = "Prefix",
                    ConflictingMods = patches.Prefixes.Select(p => p.owner).ToList(),
                    Severity = DetermineSeverity(patches.Prefixes)
                };
                report.Conflicts.Add(conflict);
            }

            // Check postfix conflicts
            if (patches.Postfixes.Count > 1)
            {
                var conflict = new PatchConflict
                {
                    MethodName = $"{method.DeclaringType?.Name}.{method.Name}",
                    PatchType = "Postfix",
                    ConflictingMods = patches.Postfixes.Select(p => p.owner).ToList(),
                    Severity = ConflictSeverity.Low // Postfixes usually safe
                };
                report.Conflicts.Add(conflict);
            }

            // Check transpiler conflicts (HIGH severity)
            if (patches.Transpilers.Count > 1)
            {
                var conflict = new PatchConflict
                {
                    MethodName = $"{method.DeclaringType?.Name}.{method.Name}",
                    PatchType = "Transpiler",
                    ConflictingMods = patches.Transpilers.Select(p => p.owner).ToList(),
                    Severity = ConflictSeverity.High // Multiple IL mods risky
                };
                report.Conflicts.Add(conflict);
            }
        }

        return report;
    }

    private ConflictSeverity DetermineSeverity(ReadOnlyCollection<Patch> prefixes)
    {
        // High severity if any prefix might skip original
        // (heuristic: check if patch has bool return type)
        foreach (var patch in prefixes)
        {
            if (patch.PatchMethod.ReturnType == typeof(bool))
                return ConflictSeverity.High;
        }
        return ConflictSeverity.Medium;
    }
}

public enum ConflictSeverity
{
    Low,      // Multiple postfixes (usually safe)
    Medium,   // Multiple prefixes (may interfere)
    High      // Multiple transpilers or skip-capable prefixes
}
```

### 5. Safe Patch Practices

**Guidelines for Mod Developers**:

1. **Always Check Nulls**:
```csharp
static void Postfix(Pokemon pokemon)
{
    if (pokemon == null) return; // Safety check
    // ... modification logic
}
```

2. **Use Try-Catch**:
```csharp
static bool Prefix(BattleState state)
{
    try
    {
        // Your logic
        return true;
    }
    catch (Exception ex)
    {
        Logger.LogError($"Patch error: {ex}");
        return true; // Don't break the game
    }
}
```

3. **Document Patch Behavior**:
```csharp
/// <summary>
/// Patches BattleSystem.CalculateDamage to apply weather effects.
/// Returns false for weather-boosted moves to use custom calculation.
/// </summary>
[HarmonyPatch(typeof(BattleSystem), nameof(BattleSystem.CalculateDamage))]
public class WeatherDamagePatch { ... }
```

4. **Use Patch Priority** (for known conflicts):
```csharp
[HarmonyPatch(typeof(TargetClass), "MethodName")]
[HarmonyPriority(Priority.High)] // Runs before normal priority
public class MyPatch { ... }
```

5. **Validate Patch Targets**:
```csharp
static void ApplyPatches()
{
    var targetMethod = typeof(BattleSystem).GetMethod("CalculateDamage");
    if (targetMethod == null)
    {
        Logger.LogError("Patch target not found: BattleSystem.CalculateDamage");
        return;
    }
    // Apply patch...
}
```

### 6. Performance Considerations

**Overhead per Patch**:
- Prefix: ~10-20ns per call
- Postfix: ~10-20ns per call
- Transpiler: ~5-10ns per call (IL is modified, minimal runtime cost)
- Finalizer: ~30-50ns per call (exception handling overhead)

**Optimization Tips**:
1. Minimize patch count (prefer fewer complex patches over many simple ones)
2. Cache reflection results in static fields
3. Use transpilers for hot paths (more complex but faster at runtime)
4. Avoid allocations in frequently-called patches

**Example - Optimized Patch**:
```csharp
[HarmonyPatch(typeof(HotPathClass), "FrequentMethod")]
public class OptimizedPatch
{
    // Cache reflection results
    private static readonly FieldInfo _cachedField =
        typeof(HotPathClass).GetField("_internalState", BindingFlags.NonPublic);

    static void Postfix(HotPathClass __instance)
    {
        // Use cached field instead of GetField every call
        var state = _cachedField.GetValue(__instance);
        // ... logic
    }
}
```

### 7. Debugging Harmony Patches

**In-Game Debug Tools**:

```csharp
public class HarmonyDebugger
{
    public static void DumpPatchInfo(MethodBase method)
    {
        var patches = Harmony.GetPatchInfo(method);
        if (patches == null)
        {
            Console.WriteLine($"No patches on {method.Name}");
            return;
        }

        Console.WriteLine($"Patches for {method.DeclaringType?.Name}.{method.Name}:");
        Console.WriteLine($"  Prefixes: {patches.Prefixes.Count}");
        foreach (var p in patches.Prefixes)
            Console.WriteLine($"    - {p.owner} ({p.priority})");

        Console.WriteLine($"  Postfixes: {patches.Postfixes.Count}");
        foreach (var p in patches.Postfixes)
            Console.WriteLine($"    - {p.owner} ({p.priority})");

        Console.WriteLine($"  Transpilers: {patches.Transpilers.Count}");
        foreach (var p in patches.Transpilers)
            Console.WriteLine($"    - {p.owner}");
    }

    public static void DumpAllPatches()
    {
        var allPatched = Harmony.GetAllPatchedMethods();
        foreach (var method in allPatched)
        {
            DumpPatchInfo(method);
        }
    }
}
```

**Developer Console Commands**:
- `/harmony list` - List all patched methods
- `/harmony dump <TypeName.MethodName>` - Show patches on specific method
- `/harmony conflicts` - Show detected conflicts
- `/harmony reload <modId>` - Reload mod's patches

### 8. Security & Safety

**Validation During Load**:
```csharp
public void ValidatePatchSafety(Assembly modAssembly)
{
    var harmonyPatchTypes = modAssembly.GetTypes()
        .Where(t => t.GetCustomAttributes<HarmonyPatch>().Any());

    foreach (var type in harmonyPatchTypes)
    {
        var patchAttrs = type.GetCustomAttributes<HarmonyPatch>();
        foreach (var attr in patchAttrs)
        {
            // Check if target class/method exists
            var targetType = attr.info.declaringType;
            var targetMethod = attr.info.methodName;

            if (targetType == null)
            {
                Logger.LogWarning($"Patch target type is null in {type.Name}");
                continue;
            }

            var method = targetType.GetMethod(targetMethod,
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static);

            if (method == null)
            {
                throw new ModValidationException(
                    $"Patch target not found: {targetType.Name}.{targetMethod}");
            }

            // Check if patching sensitive methods
            if (IsSensitiveMethod(method))
            {
                Logger.LogWarning(
                    $"Mod {modAssembly.GetName().Name} patches sensitive method: " +
                    $"{targetType.Name}.{targetMethod}");
            }
        }
    }
}

private bool IsSensitiveMethod(MethodInfo method)
{
    // Flag patches to save system, networking, etc.
    var sensitiveNamespaces = new[]
    {
        "System.IO",
        "System.Net",
        "System.Security",
        "PokeNET.Core.SaveSystem",
        "PokeNET.Core.Networking"
    };

    return sensitiveNamespaces.Any(ns =>
        method.DeclaringType?.Namespace?.StartsWith(ns) == true);
}
```

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void Harmony_PrefixPatch_SkipsOriginalMethod()
{
    var harmony = new Harmony("test.harmony");
    var original = typeof(TestClass).GetMethod(nameof(TestClass.TestMethod));
    var prefix = typeof(TestPatch).GetMethod(nameof(TestPatch.Prefix));

    harmony.Patch(original, prefix: new HarmonyMethod(prefix));

    var result = TestClass.TestMethod();

    Assert.Equal(42, result); // Prefix sets result to 42

    harmony.UnpatchAll("test.harmony");
}

[Fact]
public void Harmony_DetectsConflicts_WhenMultipleModsPatchSameMethod()
{
    var manager = new ModHarmonyManager(logger);

    // Load two mods that patch the same method
    manager.ApplyPatches(mod1, manifest1);
    manager.ApplyPatches(mod2, manifest2);

    var conflicts = manager.GetPatchConflicts();

    Assert.NotEmpty(conflicts);
    Assert.Contains(conflicts.Keys,
        m => m.Name == "CalculateDamage");
}
```

### Integration Tests

```csharp
[Fact]
public async Task ModLoader_AppliesHarmonyPatches_InCorrectOrder()
{
    var modLoader = CreateModLoader();
    var mods = new[]
    {
        CreateMod("base.mod"),
        CreateMod("patch.mod", dependencies: ["base.mod"])
    };

    await modLoader.LoadModsAsync(mods);

    // Verify patches applied in dependency order
    var patchInfo = Harmony.GetPatchInfo(typeof(TestClass).GetMethod("Test"));
    var owners = patchInfo.Prefixes.Select(p => p.owner).ToList();

    Assert.Equal("pokenet.mod.base.mod", owners[0]);
    Assert.Equal("pokenet.mod.patch.mod", owners[1]);
}
```

## Best Practices Summary

1. ✅ One Harmony instance per mod (isolated namespaces)
2. ✅ Validate patch targets before applying
3. ✅ Use try-catch in patch methods
4. ✅ Document patch behavior clearly
5. ✅ Cache reflection results for performance
6. ✅ Detect and warn about conflicts
7. ✅ Automatic rollback on patch failure
8. ✅ Provide debugging tools for developers
9. ✅ Flag sensitive method patches
10. ✅ Comprehensive logging for troubleshooting

## Future Enhancements

### Phase 4.2: Advanced Harmony Features
- **Patch Priority System**: Allow mods to declare patch execution order
- **Compatibility Database**: Known-good patch combinations
- **Auto-Resolution**: Suggest load order for conflicting mods
- **IL Analyzer**: Static analysis of transpiler conflicts

### Phase 4.3: Developer Tools
- **Visual Patch Inspector**: UI showing all patches and their effects
- **Patch Profiler**: Measure overhead of each patch
- **Hot Reload**: Recompile and re-apply patches without restart
- **Patch Templates**: Code snippets for common patch patterns

## References

- [Harmony Documentation](https://harmony.pardeike.net/)
- [Harmony GitHub](https://github.com/pardeike/Harmony)
- [RimWorld Harmony Examples](https://github.com/pardeike/Harmony/wiki/Examples)
- [IL Basics for Transpilers](https://harmony.pardeike.net/articles/patching-transpiler.html)

---

**Document Version**: 1.0
**Last Updated**: 2025-10-22
**Author**: System Architect Agent (PokeNET Hive Mind)
**Status**: Final Design - Ready for Implementation
