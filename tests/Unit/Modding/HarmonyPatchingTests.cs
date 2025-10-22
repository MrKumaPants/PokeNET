using PokeNET.Tests.Utilities;
using HarmonyLib;
using System.Reflection;

namespace PokeNET.Tests.Unit.Modding;

/// <summary>
/// Tests for Harmony patching functionality
/// </summary>
public class HarmonyPatchingTests : IDisposable
{
    private readonly List<Harmony> _harmonyInstances = new();

    public void Dispose()
    {
        // Cleanup all harmony patches
        foreach (var harmony in _harmonyInstances)
        {
            HarmonyTestHelpers.UnpatchAll(harmony);
        }
        _harmonyInstances.Clear();

        // Reset test targets
        HarmonyTestHelpers.TestPatchTarget.ResetCounters();
        HarmonyTestHelpers.SamplePrefixPatch.Applied = false;
        HarmonyTestHelpers.SamplePostfixPatch.Applied = false;
    }

    [Fact]
    public void Harmony_Should_ApplySimplePatch()
    {
        // Arrange
        var harmony = HarmonyTestHelpers.CreateTestHarmonyInstance();
        _harmonyInstances.Add(harmony);

        var originalMethod = typeof(HarmonyTestHelpers.TestPatchTarget)
            .GetMethod(nameof(HarmonyTestHelpers.TestPatchTarget.TestMethod))!;

        var prefixMethod = typeof(HarmonyTestHelpers.SamplePrefixPatch)
            .GetMethod(nameof(HarmonyTestHelpers.SamplePrefixPatch.Prefix))!;

        // Act
        harmony.Patch(originalMethod, prefix: new HarmonyMethod(prefixMethod));

        // Assert
        HarmonyTestHelpers.VerifyPatchApplied(originalMethod, harmony).Should().BeTrue();
    }

    [Fact]
    public void Harmony_Should_ExecutePrefixPatch()
    {
        // Arrange
        var harmony = HarmonyTestHelpers.CreateTestHarmonyInstance();
        _harmonyInstances.Add(harmony);

        var originalMethod = typeof(HarmonyTestHelpers.TestPatchTarget)
            .GetMethod(nameof(HarmonyTestHelpers.TestPatchTarget.TestMethod))!;

        var prefixMethod = typeof(HarmonyTestHelpers.SamplePrefixPatch)
            .GetMethod(nameof(HarmonyTestHelpers.SamplePrefixPatch.Prefix))!;

        harmony.Patch(originalMethod, prefix: new HarmonyMethod(prefixMethod));

        // Act
        var result = HarmonyTestHelpers.TestPatchTarget.TestMethod("test");

        // Assert
        HarmonyTestHelpers.SamplePrefixPatch.Applied.Should().BeTrue();
        result.Should().Be("Original: test");
    }

    [Fact]
    public void Harmony_Should_BlockExecutionWithPrefixReturnFalse()
    {
        // Arrange
        var harmony = HarmonyTestHelpers.CreateTestHarmonyInstance();
        _harmonyInstances.Add(harmony);

        var originalMethod = typeof(HarmonyTestHelpers.TestPatchTarget)
            .GetMethod(nameof(HarmonyTestHelpers.TestPatchTarget.TestMethod))!;

        var prefixMethod = typeof(HarmonyTestHelpers.SamplePrefixPatch)
            .GetMethod(nameof(HarmonyTestHelpers.SamplePrefixPatch.Prefix))!;

        harmony.Patch(originalMethod, prefix: new HarmonyMethod(prefixMethod));

        // Act
        var result = HarmonyTestHelpers.TestPatchTarget.TestMethod("block");

        // Assert
        HarmonyTestHelpers.SamplePrefixPatch.Applied.Should().BeTrue();
        result.Should().BeNull(); // Original method was blocked
    }

    [Fact]
    public void Harmony_Should_ExecutePostfixPatch()
    {
        // Arrange
        var harmony = HarmonyTestHelpers.CreateTestHarmonyInstance();
        _harmonyInstances.Add(harmony);

        var originalMethod = typeof(HarmonyTestHelpers.TestPatchTarget)
            .GetMethod(nameof(HarmonyTestHelpers.TestPatchTarget.TestMethod))!;

        var postfixMethod = typeof(HarmonyTestHelpers.SamplePostfixPatch)
            .GetMethod(nameof(HarmonyTestHelpers.SamplePostfixPatch.Postfix))!;

        harmony.Patch(originalMethod, postfix: new HarmonyMethod(postfixMethod));

        // Act
        var result = HarmonyTestHelpers.TestPatchTarget.TestMethod("test");

        // Assert
        HarmonyTestHelpers.SamplePostfixPatch.Applied.Should().BeTrue();
        result.Should().Contain("[Modified]");
    }

    [Fact]
    public void Harmony_Should_AllowMultipleModsToCoexist()
    {
        // Arrange
        var harmony1 = HarmonyTestHelpers.CreateTestHarmonyInstance("mod1");
        var harmony2 = HarmonyTestHelpers.CreateTestHarmonyInstance("mod2");
        _harmonyInstances.Add(harmony1);
        _harmonyInstances.Add(harmony2);

        var originalMethod = typeof(HarmonyTestHelpers.TestPatchTarget)
            .GetMethod(nameof(HarmonyTestHelpers.TestPatchTarget.TestMethod))!;

        var prefixMethod = typeof(HarmonyTestHelpers.SamplePrefixPatch)
            .GetMethod(nameof(HarmonyTestHelpers.SamplePrefixPatch.Prefix))!;

        // Act
        harmony1.Patch(originalMethod, prefix: new HarmonyMethod(prefixMethod));
        harmony2.Patch(originalMethod, prefix: new HarmonyMethod(prefixMethod));

        // Assert
        HarmonyTestHelpers.VerifyMultiplePatchesCoexist(originalMethod, harmony1, harmony2);
    }

    [Fact]
    public void Harmony_Should_DetectPatchConflicts()
    {
        // Arrange
        var originalMethod = typeof(HarmonyTestHelpers.TestPatchTarget)
            .GetMethod(nameof(HarmonyTestHelpers.TestPatchTarget.TestMethod))!;

        // Act
        var (harmony1, harmony2) = HarmonyTestHelpers.CreatePatchConflict(originalMethod);
        _harmonyInstances.Add(harmony1);
        _harmonyInstances.Add(harmony2);

        var patchCount = HarmonyTestHelpers.GetPatchCount(originalMethod);

        // Assert
        patchCount.Should().BeGreaterThan(1, "multiple patches should be applied");
    }

    [Fact]
    public void Harmony_Should_RollbackPatchOnError()
    {
        // Arrange
        var harmony = HarmonyTestHelpers.CreateTestHarmonyInstance("rollback-test");
        _harmonyInstances.Add(harmony);

        var originalMethod = typeof(HarmonyTestHelpers.TestPatchTarget)
            .GetMethod(nameof(HarmonyTestHelpers.TestPatchTarget.TestMethod))!;

        var prefixMethod = typeof(HarmonyTestHelpers.SamplePrefixPatch)
            .GetMethod(nameof(HarmonyTestHelpers.SamplePrefixPatch.Prefix))!;

        harmony.Patch(originalMethod, prefix: new HarmonyMethod(prefixMethod));

        // Act
        HarmonyTestHelpers.UnpatchAll(harmony);

        // Assert
        HarmonyTestHelpers.VerifyPatchRolledBack(originalMethod, harmony.Id);
    }

    [Fact]
    public void Harmony_Should_PatchMultipleMethods()
    {
        // Arrange
        var harmony = HarmonyTestHelpers.CreateTestHarmonyInstance();
        _harmonyInstances.Add(harmony);

        var method1 = typeof(HarmonyTestHelpers.TestPatchTarget)
            .GetMethod(nameof(HarmonyTestHelpers.TestPatchTarget.TestMethod))!;
        var method2 = typeof(HarmonyTestHelpers.TestPatchTarget)
            .GetMethod(nameof(HarmonyTestHelpers.TestPatchTarget.TestMethodWithReturn))!;

        var prefixMethod = typeof(HarmonyTestHelpers.SamplePrefixPatch)
            .GetMethod(nameof(HarmonyTestHelpers.SamplePrefixPatch.Prefix))!;

        // Act
        harmony.Patch(method1, prefix: new HarmonyMethod(prefixMethod));
        harmony.Patch(method2, prefix: new HarmonyMethod(prefixMethod));

        // Assert
        HarmonyTestHelpers.VerifyPatchApplied(method1, harmony).Should().BeTrue();
        HarmonyTestHelpers.VerifyPatchApplied(method2, harmony).Should().BeTrue();
    }

    [Fact]
    public void Harmony_Should_HandleVirtualMethods()
    {
        // Arrange
        var harmony = HarmonyTestHelpers.CreateTestHarmonyInstance();
        _harmonyInstances.Add(harmony);

        var virtualMethod = typeof(HarmonyTestHelpers.TestPatchTarget)
            .GetMethod(nameof(HarmonyTestHelpers.TestPatchTarget.VirtualTestMethod))!;

        var postfixMethod = typeof(HarmonyTestHelpers.SamplePostfixPatch)
            .GetMethod(nameof(HarmonyTestHelpers.SamplePostfixPatch.Postfix))!;

        // Act
        harmony.Patch(virtualMethod, postfix: new HarmonyMethod(postfixMethod));

        var target = new HarmonyTestHelpers.TestPatchTarget();
        var result = target.VirtualTestMethod("test");

        // Assert
        result.Should().Contain("[Modified]");
    }

    [Fact]
    public void Harmony_Should_MaintainPatchPriority()
    {
        // Arrange
        var harmony1 = HarmonyTestHelpers.CreateTestHarmonyInstance("priority1");
        var harmony2 = HarmonyTestHelpers.CreateTestHarmonyInstance("priority2");
        _harmonyInstances.Add(harmony1);
        _harmonyInstances.Add(harmony2);

        var originalMethod = typeof(HarmonyTestHelpers.TestPatchTarget)
            .GetMethod(nameof(HarmonyTestHelpers.TestPatchTarget.TestMethod))!;

        var prefixMethod = typeof(HarmonyTestHelpers.SamplePrefixPatch)
            .GetMethod(nameof(HarmonyTestHelpers.SamplePrefixPatch.Prefix))!;

        // Act - Apply patches with different priorities
        harmony1.Patch(originalMethod,
            prefix: new HarmonyMethod(prefixMethod) { priority = Priority.High });
        harmony2.Patch(originalMethod,
            prefix: new HarmonyMethod(prefixMethod) { priority = Priority.Low });

        var patches = Harmony.GetPatchInfo(originalMethod);

        // Assert
        patches.Should().NotBeNull();
        patches!.Prefixes.Should().HaveCount(2);
        // Verify high priority patch is first
        patches.Prefixes[0].priority.Should().BeGreaterThan(patches.Prefixes[1].priority);
    }

    [Fact]
    public void Harmony_Should_UnpatchSpecificMod()
    {
        // Arrange
        var harmony1 = HarmonyTestHelpers.CreateTestHarmonyInstance("mod1");
        var harmony2 = HarmonyTestHelpers.CreateTestHarmonyInstance("mod2");
        _harmonyInstances.Add(harmony1);
        _harmonyInstances.Add(harmony2);

        var originalMethod = typeof(HarmonyTestHelpers.TestPatchTarget)
            .GetMethod(nameof(HarmonyTestHelpers.TestPatchTarget.TestMethod))!;

        var prefixMethod = typeof(HarmonyTestHelpers.SamplePrefixPatch)
            .GetMethod(nameof(HarmonyTestHelpers.SamplePrefixPatch.Prefix))!;

        harmony1.Patch(originalMethod, prefix: new HarmonyMethod(prefixMethod));
        harmony2.Patch(originalMethod, prefix: new HarmonyMethod(prefixMethod));

        // Act
        HarmonyTestHelpers.UnpatchAll(harmony1);

        // Assert
        HarmonyTestHelpers.VerifyPatchRolledBack(originalMethod, harmony1.Id);
        HarmonyTestHelpers.VerifyPatchApplied(originalMethod, harmony2).Should().BeTrue();
    }
}
