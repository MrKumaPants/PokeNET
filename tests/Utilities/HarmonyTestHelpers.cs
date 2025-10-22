using HarmonyLib;
using System.Reflection;

namespace PokeNET.Tests.Utilities;

/// <summary>
/// Helper utilities for testing Harmony patches
/// </summary>
public static class HarmonyTestHelpers
{
    private static int _harmonyIdCounter = 0;

    /// <summary>
    /// Creates a unique Harmony instance for testing
    /// </summary>
    public static Harmony CreateTestHarmonyInstance(string? modId = null)
    {
        var id = modId ?? $"test.harmony.{Interlocked.Increment(ref _harmonyIdCounter)}";
        return new Harmony(id);
    }

    /// <summary>
    /// Verifies that a method has been patched by Harmony
    /// </summary>
    public static bool VerifyPatchApplied(MethodBase originalMethod, Harmony harmony)
    {
        var patches = Harmony.GetPatchInfo(originalMethod);
        return patches != null &&
               (patches.Prefixes.Any(p => p.owner == harmony.Id) ||
                patches.Postfixes.Any(p => p.owner == harmony.Id) ||
                patches.Transpilers.Any(p => p.owner == harmony.Id));
    }

    /// <summary>
    /// Creates a simple test prefix patch
    /// </summary>
    public static HarmonyMethod CreateTestPrefix(MethodInfo method)
    {
        return new HarmonyMethod(method);
    }

    /// <summary>
    /// Creates a simple test postfix patch
    /// </summary>
    public static HarmonyMethod CreateTestPostfix(MethodInfo method)
    {
        return new HarmonyMethod(method);
    }

    /// <summary>
    /// Unpatch all methods patched by a Harmony instance
    /// </summary>
    public static void UnpatchAll(Harmony harmony)
    {
        harmony.UnpatchAll(harmony.Id);
    }

    /// <summary>
    /// Verifies that multiple mods' patches coexist correctly
    /// </summary>
    public static void VerifyMultiplePatchesCoexist(MethodBase originalMethod, params Harmony[] harmonies)
    {
        var patches = Harmony.GetPatchInfo(originalMethod);
        patches.Should().NotBeNull("method should have patches");

        foreach (var harmony in harmonies)
        {
            var hasPatch = patches!.Prefixes.Any(p => p.owner == harmony.Id) ||
                          patches.Postfixes.Any(p => p.owner == harmony.Id) ||
                          patches.Transpilers.Any(p => p.owner == harmony.Id);

            hasPatch.Should().BeTrue($"harmony instance {harmony.Id} should have a patch on the method");
        }
    }

    /// <summary>
    /// Counts the number of patches on a method
    /// </summary>
    public static int GetPatchCount(MethodBase originalMethod)
    {
        var patches = Harmony.GetPatchInfo(originalMethod);
        if (patches == null) return 0;

        return patches.Prefixes.Count +
               patches.Postfixes.Count +
               patches.Transpilers.Count +
               patches.Finalizers.Count;
    }

    /// <summary>
    /// Creates a test class with patchable methods
    /// </summary>
    public static class TestPatchTarget
    {
        public static int CallCount { get; set; } = 0;
        public static string? LastValue { get; set; }

        public static void ResetCounters()
        {
            CallCount = 0;
            LastValue = null;
        }

        public static string TestMethod(string input)
        {
            CallCount++;
            LastValue = input;
            return $"Original: {input}";
        }

        public static int TestMethodWithReturn(int value)
        {
            CallCount++;
            return value * 2;
        }

        public virtual string VirtualTestMethod(string input)
        {
            CallCount++;
            return $"Virtual: {input}";
        }
    }

    /// <summary>
    /// Sample prefix patch that can block execution
    /// </summary>
    public static class SamplePrefixPatch
    {
        public static bool Applied { get; set; } = false;

        public static bool Prefix(string input)
        {
            Applied = true;
            return input != "block"; // Return false to skip original method
        }
    }

    /// <summary>
    /// Sample postfix patch that modifies return value
    /// </summary>
    public static class SamplePostfixPatch
    {
        public static bool Applied { get; set; } = false;

        public static void Postfix(ref string __result)
        {
            Applied = true;
            __result = __result + " [Modified]";
        }
    }

    /// <summary>
    /// Verifies that a patch was rolled back successfully
    /// </summary>
    public static void VerifyPatchRolledBack(MethodBase originalMethod, string harmonyId)
    {
        var patches = Harmony.GetPatchInfo(originalMethod);

        if (patches == null)
        {
            return; // No patches at all - successfully rolled back
        }

        var hasPatch = patches.Prefixes.Any(p => p.owner == harmonyId) ||
                      patches.Postfixes.Any(p => p.owner == harmonyId) ||
                      patches.Transpilers.Any(p => p.owner == harmonyId);

        hasPatch.Should().BeFalse($"harmony instance {harmonyId} should not have any patches after rollback");
    }

    /// <summary>
    /// Simulates a patch conflict by applying incompatible patches
    /// </summary>
    public static (Harmony first, Harmony second) CreatePatchConflict(MethodBase targetMethod)
    {
        var harmony1 = CreateTestHarmonyInstance("conflict.mod1");
        var harmony2 = CreateTestHarmonyInstance("conflict.mod2");

        // Apply conflicting patches
        var prefix1 = typeof(SamplePrefixPatch).GetMethod(nameof(SamplePrefixPatch.Prefix));
        var prefix2 = typeof(SamplePrefixPatch).GetMethod(nameof(SamplePrefixPatch.Prefix));

        harmony1.Patch(targetMethod, prefix: new HarmonyMethod(prefix1));
        harmony2.Patch(targetMethod, prefix: new HarmonyMethod(prefix2));

        return (harmony1, harmony2);
    }
}
