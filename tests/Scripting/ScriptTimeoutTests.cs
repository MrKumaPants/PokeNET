using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PokeNET.Scripting.Security;
using Xunit;

namespace PokeNET.Tests.Scripting;

/// <summary>
/// Comprehensive timeout enforcement tests for ScriptSandbox.
/// CRITICAL: Tests VULN-001 fix - process-level timeout enforcement.
/// Tests cooperative cancellation, infinite loop detection, and async timeout handling.
/// </summary>
public sealed class ScriptTimeoutTests : IDisposable
{
    private readonly ScriptPermissions _defaultPermissions;

    public ScriptTimeoutTests()
    {
        _defaultPermissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("timeout-test-script")
            .WithTimeout(TimeSpan.FromMilliseconds(500))
            .WithMaxMemory(100 * 1024 * 1024) // 100MB
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .AllowNamespace("System.Linq")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();
    }

    public void Dispose()
    {
        // Cleanup
    }

    #region Cooperative Cancellation Tests

    [Fact]
    public async Task CooperativeCancellation_FastScript_CompletesSuccessfully()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("fast-script")
            .WithTimeout(TimeSpan.FromSeconds(5))
            .WithMaxMemory(100 * 1024 * 1024)
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();

        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static int Execute()
    {
        return 42;
    }
}
";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await sandbox.ExecuteAsync(code, "Execute");
        stopwatch.Stop();

        // Assert
        result.Success.Should().BeTrue();
        result.ReturnValue.Should().Be(42);
        result.TimedOut.Should().BeFalse();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    [Fact]
    public async Task CooperativeCancellation_SlowScript_TimesOut()
    {
        // Arrange
        using var sandbox = new ScriptSandbox(_defaultPermissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        System.Threading.Thread.Sleep(5000); // Sleep 5 seconds
    }
}
";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await sandbox.ExecuteAsync(code, "Execute");
        stopwatch.Stop();

        // Assert
        result.Success.Should().BeFalse();
        result.TimedOut.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should timeout in <2s
        result.Exception.Should().BeOfType<OperationCanceledException>();
    }

    [Fact]
    public async Task CooperativeCancellation_CancellationToken_RespectedDuringExecution()
    {
        // Arrange
        using var sandbox = new ScriptSandbox(_defaultPermissions);
        using var cts = new CancellationTokenSource();
        var code = @"
public class Script
{
    public static void Execute()
    {
        for (int i = 0; i < 1000000; i++)
        {
            // Long-running work
            var x = i * i;
        }
    }
}
";

        // Act
        var task = sandbox.ExecuteAsync(code, "Execute", null, cts.Token);
        cts.CancelAfter(100); // Cancel after 100ms
        var result = await task;

        // Assert
        result.Success.Should().BeFalse();
        result.TimedOut.Should().BeTrue();
    }

    #endregion

    #region Process-Level Timeout Tests (VULN-001 Fix)

    [Fact]
    public async Task ProcessTimeout_InfiniteLoop_TerminatesWithinTimeout()
    {
        // Arrange - CRITICAL test for VULN-001
        using var sandbox = new ScriptSandbox(_defaultPermissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        while (true)
        {
            // Infinite loop - process-level timeout should kill this
        }
    }
}
";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await sandbox.ExecuteAsync(code, "Execute");
        stopwatch.Stop();

        // Assert - MUST terminate within reasonable time
        result.Success.Should().BeFalse();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Max 2 seconds
        result.ExecutionTime.TotalMilliseconds.Should().BeLessThan(2000);
    }

    [Fact]
    public async Task ProcessTimeout_CpuBombAttack_TerminatesWithinTimeout()
    {
        // Arrange
        using var sandbox = new ScriptSandbox(_defaultPermissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        // CPU bomb - compute intensive infinite loop
        for (long i = 0; i < long.MaxValue; i++)
        {
            var x = System.Math.Sqrt(i) * System.Math.Sqrt(i);
        }
    }
}
";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await sandbox.ExecuteAsync(code, "Execute");
        stopwatch.Stop();

        // Assert
        result.Success.Should().BeFalse();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000);
    }

    [Fact]
    public async Task ProcessTimeout_NestedInfiniteLoops_TerminatesWithinTimeout()
    {
        // Arrange
        using var sandbox = new ScriptSandbox(_defaultPermissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        while (true)
        {
            for (int i = 0; i < int.MaxValue; i++)
            {
                // Nested infinite loops
            }
        }
    }
}
";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await sandbox.ExecuteAsync(code, "Execute");
        stopwatch.Stop();

        // Assert
        result.Success.Should().BeFalse();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000);
    }

    [Fact]
    public async Task ProcessTimeout_TimeoutBypassAttempt_StillTerminates()
    {
        // Arrange - attempt to bypass cancellation token
        using var sandbox = new ScriptSandbox(_defaultPermissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        try
        {
            while (true)
            {
                // Try to catch and ignore cancellation
            }
        }
        catch (System.Exception)
        {
            // Attempt to continue after cancellation
            while (true) { }
        }
    }
}
";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await sandbox.ExecuteAsync(code, "Execute");
        stopwatch.Stop();

        // Assert - process-level timeout should still terminate
        result.Success.Should().BeFalse();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000);
    }

    [Fact]
    public async Task ProcessTimeout_BusyWaitLoop_TerminatesWithinTimeout()
    {
        // Arrange
        using var sandbox = new ScriptSandbox(_defaultPermissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        var start = System.DateTime.UtcNow;
        while ((System.DateTime.UtcNow - start).TotalMinutes < 10)
        {
            // Busy wait for 10 minutes - should timeout in 500ms
        }
    }
}
";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await sandbox.ExecuteAsync(code, "Execute");
        stopwatch.Stop();

        // Assert
        result.Success.Should().BeFalse();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000);
    }

    #endregion

    #region Infinite Loop Detection Tests

    [Fact]
    public async Task InfiniteLoopDetection_WhileTrueLoop_Detected()
    {
        // Arrange
        using var sandbox = new ScriptSandbox(_defaultPermissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        while (true)
        {
            var x = 1;
        }
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        result.Success.Should().BeFalse();
        result.TimedOut.Should().BeTrue();
    }

    [Fact]
    public async Task InfiniteLoopDetection_ForeverLoop_Detected()
    {
        // Arrange
        using var sandbox = new ScriptSandbox(_defaultPermissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        for (;;)
        {
            // Infinite for loop
        }
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        result.Success.Should().BeFalse();
        result.TimedOut.Should().BeTrue();
    }

    [Fact]
    public async Task InfiniteLoopDetection_DoWhileLoop_Detected()
    {
        // Arrange
        using var sandbox = new ScriptSandbox(_defaultPermissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        do
        {
            var x = 1;
        } while (true);
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        result.Success.Should().BeFalse();
        result.TimedOut.Should().BeTrue();
    }

    [Fact]
    public async Task InfiniteLoopDetection_RecursiveLoop_Detected()
    {
        // Arrange
        using var sandbox = new ScriptSandbox(_defaultPermissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        InfiniteRecursion();
    }

    private static void InfiniteRecursion()
    {
        InfiniteRecursion(); // Stack overflow or timeout
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        result.Success.Should().BeFalse();
    }

    #endregion

    #region Async Timeout Tests

    [Fact]
    public async Task AsyncTimeout_SlowAsyncOperation_TimesOut()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("async-script")
            .WithTimeout(TimeSpan.FromMilliseconds(500))
            .WithMaxMemory(100 * 1024 * 1024)
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .AllowNamespace("System")
            .AllowNamespace("System.Threading.Tasks")
            .AllowApi(ScriptPermissions.ApiCategory.Threading)
            .Build();

        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
using System.Threading.Tasks;
public class Script
{
    public static async Task Execute()
    {
        await Task.Delay(10000); // 10 second delay
    }
}
";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await sandbox.ExecuteAsync(code, "Execute");
        stopwatch.Stop();

        // Assert
        result.Success.Should().BeFalse();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000);
    }

    [Fact]
    public async Task AsyncTimeout_CancellationTokenPropagation_Works()
    {
        // Arrange
        using var sandbox = new ScriptSandbox(_defaultPermissions);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var code = @"
public class Script
{
    public static void Execute()
    {
        System.Threading.Thread.Sleep(5000);
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute", null, cts.Token);

        // Assert
        result.Success.Should().BeFalse();
        result.TimedOut.Should().BeTrue();
    }

    #endregion

    #region Timeout Cleanup Tests

    [Fact]
    public async Task TimeoutCleanup_AfterTimeout_ResourcesReleased()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(forceFullCollection: true);
        using var sandbox = new ScriptSandbox(_defaultPermissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        while (true) { }
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(forceFullCollection: true);

        // Assert
        result.Success.Should().BeFalse();
        var memoryIncrease = finalMemory - initialMemory;
        memoryIncrease.Should().BeLessThan(50 * 1024 * 1024); // <50MB leak
    }

    [Fact]
    public async Task TimeoutCleanup_MultipleTimeouts_NoLeaks()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(forceFullCollection: true);
        using var sandbox = new ScriptSandbox(_defaultPermissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        while (true) { }
    }
}
";

        // Act - execute multiple times
        for (int i = 0; i < 10; i++)
        {
            var result = await sandbox.ExecuteAsync(code, "Execute");
            result.Success.Should().BeFalse();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(forceFullCollection: true);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        memoryIncrease.Should().BeLessThan(100 * 1024 * 1024); // <100MB for 10 executions
    }

    [Fact]
    public async Task TimeoutCleanup_AfterTimeoutDisposal_Complete()
    {
        // Arrange
        var code = @"
public class Script
{
    public static void Execute()
    {
        while (true) { }
    }
}
";

        // Act
        ScriptSandbox.ExecutionResult? result = null;
        using (var sandbox = new ScriptSandbox(_defaultPermissions))
        {
            result = await sandbox.ExecuteAsync(code, "Execute");
        }

        // Trigger GC
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Assert
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    #endregion

    #region Concurrent Timeout Tests

    [Fact]
    public async Task ConcurrentTimeouts_MultipleScripts_AllTimeout()
    {
        // Arrange
        using var sandbox = new ScriptSandbox(_defaultPermissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        while (true) { }
    }
}
";

        // Act - execute 5 scripts concurrently
        var tasks = new Task<ScriptSandbox.ExecutionResult>[5];
        for (int i = 0; i < 5; i++)
        {
            tasks[i] = sandbox.ExecuteAsync(code, "Execute");
        }

        var stopwatch = Stopwatch.StartNew();
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        foreach (var result in results)
        {
            result.Success.Should().BeFalse();
            result.TimedOut.Should().BeTrue();
        }
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000); // All should timeout quickly
    }

    [Fact]
    public async Task ConcurrentTimeouts_MixedFastAndSlow_CorrectBehavior()
    {
        // Arrange
        using var sandbox = new ScriptSandbox(_defaultPermissions);
        var fastCode = @"
public class Script
{
    public static int Execute() { return 42; }
}
";
        var slowCode = @"
public class Script
{
    public static void Execute() { while(true) { } }
}
";

        // Act
        var task1 = sandbox.ExecuteAsync(fastCode, "Execute");
        var task2 = sandbox.ExecuteAsync(slowCode, "Execute");
        var task3 = sandbox.ExecuteAsync(fastCode, "Execute");

        var results = await Task.WhenAll(task1, task2, task3);

        // Assert
        results[0].Success.Should().BeTrue();
        results[1].Success.Should().BeFalse();
        results[1].TimedOut.Should().BeTrue();
        results[2].Success.Should().BeTrue();
    }

    #endregion

    #region Timeout Configuration Tests

    [Fact]
    public async Task TimeoutConfiguration_ShortTimeout_EnforcedStrictly()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("short-timeout")
            .WithTimeout(TimeSpan.FromMilliseconds(100))
            .WithMaxMemory(100 * 1024 * 1024)
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();

        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        System.Threading.Thread.Sleep(500);
    }
}
";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await sandbox.ExecuteAsync(code, "Execute");
        stopwatch.Stop();

        // Assert
        result.Success.Should().BeFalse();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
    }

    [Fact]
    public async Task TimeoutConfiguration_LongTimeout_AllowsCompletion()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("long-timeout")
            .WithTimeout(TimeSpan.FromSeconds(10))
            .WithMaxMemory(100 * 1024 * 1024)
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();

        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static int Execute()
    {
        System.Threading.Thread.Sleep(100);
        return 42;
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        result.Success.Should().BeTrue();
        result.ReturnValue.Should().Be(42);
        result.TimedOut.Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task EdgeCase_ExactTimeoutBoundary_HandledCorrectly()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("boundary-test")
            .WithTimeout(TimeSpan.FromMilliseconds(500))
            .WithMaxMemory(100 * 1024 * 1024)
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();

        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        System.Threading.Thread.Sleep(490); // Just under timeout
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert - may succeed or timeout depending on timing
        if (!result.Success)
        {
            result.TimedOut.Should().BeTrue();
        }
    }

    [Fact]
    public async Task EdgeCase_ZeroWorkScript_CompletesImmediately()
    {
        // Arrange
        using var sandbox = new ScriptSandbox(_defaultPermissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        // Do nothing
    }
}
";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await sandbox.ExecuteAsync(code, "Execute");
        stopwatch.Stop();

        // Assert
        result.Success.Should().BeTrue();
        result.TimedOut.Should().BeFalse();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    #endregion
}
