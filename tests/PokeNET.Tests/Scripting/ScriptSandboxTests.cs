using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PokeNET.Scripting.Security;
using Xunit;

namespace PokeNET.Tests.Scripting;

/// <summary>
/// Comprehensive security test suite for ScriptSandbox with 800+ lines of tests.
/// Tests CPU timeout enforcement, memory limits, permission violations, sandbox escape attempts,
/// and resource exhaustion prevention. CRITICAL for security.
/// </summary>
public sealed class ScriptSandboxTests : IDisposable
{
    private readonly ILogger<ScriptSandbox> _logger;

    public ScriptSandboxTests()
    {
        _logger = NullLogger<ScriptSandbox>.Instance;
    }

    public void Dispose()
    {
        // Cleanup
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullPermissions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ScriptSandbox(null!));
    }

    [Fact]
    public void Constructor_WithValidPermissions_Initializes()
    {
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions, _logger);
        Assert.NotNull(sandbox);
    }

    [Fact]
    public void Constructor_WithLogger_Initializes()
    {
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions, _logger);
        Assert.NotNull(sandbox);
    }

    #endregion

    #region Basic Execution Tests

    [Fact]
    public async Task ExecuteAsync_WithSimpleReturn_Succeeds()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
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
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(42, result.ReturnValue);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task ExecuteAsync_WithStringReturn_Succeeds()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static string Execute()
    {
        return ""Hello, Sandbox!"";
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Hello, Sandbox!", result.ReturnValue);
    }

    [Fact]
    public async Task ExecuteAsync_WithParameters_PassesCorrectly()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static int Execute(int a, int b)
    {
        return a + b;
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute", new object[] { 5, 3 });

        // Assert
        Assert.True(result.Success);
        Assert.Equal(8, result.ReturnValue);
    }

    #endregion

    #region Security Validation Tests

    [Fact]
    public async Task ExecuteAsync_WithForbiddenNamespace_FailsValidation()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
using System.IO;
public class Script
{
    public static void Execute()
    {
        File.Delete(""test.txt"");
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Exception);
        Assert.IsType<ScriptSandbox.SecurityException>(result.Exception);
        Assert.Contains("File I/O", result.Exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnsafeCode_FailsValidation()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static unsafe int Execute()
    {
        int value = 42;
        int* ptr = &value;
        return *ptr;
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task ExecuteAsync_WithReflection_FailsValidation()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
using System.Reflection;
public class Script
{
    public static void Execute()
    {
        var assembly = Assembly.Load(""System.IO"");
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Reflection", result.Exception?.Message ?? result.SecurityEvents.FirstOrDefault() ?? "");
    }

    #endregion

    #region CPU Timeout Enforcement Tests (CRITICAL)

    [Fact]
    public async Task ExecuteAsync_ExceedsTimeout_Terminates()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("test-script")
            .WithTimeout(TimeSpan.FromMilliseconds(500))
            .WithMaxMemory(100 * 1024 * 1024)
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .AllowNamespace("System.Linq")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();
        using var sandbox = new ScriptSandbox(permissions);
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
        Assert.False(result.Success);
        Assert.True(result.TimedOut || result.Exception is OperationCanceledException);
        Assert.True(stopwatch.ElapsedMilliseconds < 2000); // Should timeout well before completion
    }

    [Fact]
    public async Task ExecuteAsync_InfiniteLoop_Terminates()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("test-script")
            .WithTimeout(TimeSpan.FromMilliseconds(500))
            .WithMaxMemory(100 * 1024 * 1024)
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .AllowNamespace("System.Linq")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        while(true)
        {
            // Infinite loop - should be terminated
        }
    }
}
";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await sandbox.ExecuteAsync(code, "Execute");
        stopwatch.Stop();

        // Assert
        Assert.False(result.Success);
        Assert.True(stopwatch.ElapsedMilliseconds < 2000);
    }

    [Fact]
    public async Task ExecuteAsync_CPUBomb_Terminates()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("test-script")
            .WithTimeout(TimeSpan.FromMilliseconds(500))
            .WithMaxMemory(100 * 1024 * 1024)
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .AllowNamespace("System.Linq")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        // CPU bomb - compute intensive loop
        for(long i = 0; i < long.MaxValue; i++)
        {
            Math.Sqrt(i);
        }
    }
}
";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await sandbox.ExecuteAsync(code, "Execute");
        stopwatch.Stop();

        // Assert
        Assert.False(result.Success);
        Assert.True(stopwatch.ElapsedMilliseconds < 2000);
    }

    [Fact]
    public async Task ExecuteAsync_NestedLoops_Terminates()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("test-script")
            .WithTimeout(TimeSpan.FromMilliseconds(500))
            .WithMaxMemory(100 * 1024 * 1024)
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .AllowNamespace("System.Linq")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        for(int i = 0; i < 1000000; i++)
        {
            for(int j = 0; j < 1000000; j++)
            {
                var x = i * j;
            }
        }
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.False(result.Success);
        Assert.True(result.TimedOut || result.Exception is OperationCanceledException);
    }

    [Fact]
    public async Task ExecuteAsync_TimeoutBypass_StillTerminates()
    {
        // Arrange - attempt to bypass cancellation token
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("test-script")
            .WithTimeout(TimeSpan.FromMilliseconds(500))
            .WithMaxMemory(100 * 1024 * 1024)
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .AllowNamespace("System.Linq")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        try
        {
            while(true)
            {
                // Try to catch and ignore cancellation
            }
        }
        catch
        {
            // Attempt to continue after cancellation
            while(true) { }
        }
    }
}
";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await sandbox.ExecuteAsync(code, "Execute");
        stopwatch.Stop();

        // Assert
        Assert.False(result.Success);
        Assert.True(stopwatch.ElapsedMilliseconds < 2000);
    }

    #endregion

    #region Memory Limit Enforcement Tests (CRITICAL)

    [Fact]
    public async Task ExecuteAsync_ExceedsMemoryLimit_DetectsViolation()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("test-script")
            .WithTimeout(TimeSpan.FromSeconds(5))
            .WithMaxMemory(10 * 1024 * 1024) // 10MB limit
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .AllowNamespace("System.Linq")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static byte[] Execute()
    {
        // Allocate 50MB - should exceed limit
        return new byte[50 * 1024 * 1024];
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.False(result.Success);
        Assert.True(result.MemoryLimitExceeded);
        Assert.True(result.MemoryUsed > permissions.MaxMemoryBytes);
    }

    [Fact]
    public async Task ExecuteAsync_MemoryBomb_DetectsExcessiveAllocation()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("test-script")
            .WithTimeout(TimeSpan.FromSeconds(2))
            .WithMaxMemory(10 * 1024 * 1024) // 10MB limit
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .AllowNamespace("System.Linq")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        var list = new System.Collections.Generic.List<byte[]>();
        for(int i = 0; i < 1000; i++)
        {
            list.Add(new byte[1024 * 1024]); // 1MB per iteration
        }
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.False(result.Success);
        // Either timeout or memory exceeded
        Assert.True(result.MemoryLimitExceeded || result.TimedOut);
    }

    [Fact]
    public async Task ExecuteAsync_GCEvasion_StillDetected()
    {
        // Arrange - attempt to evade GC by holding references
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("test-script")
            .WithTimeout(TimeSpan.FromSeconds(2))
            .WithMaxMemory(10 * 1024 * 1024) // 10MB limit
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .AllowNamespace("System.Linq")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        var hold = new System.Collections.Generic.List<byte[]>();
        while(true)
        {
            hold.Add(new byte[1024 * 1024]);
            // Don't allow GC to collect
        }
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.False(result.Success);
    }

    #endregion

    #region Permission Violation Tests

    [Fact]
    public async Task ExecuteAsync_FileSystemAccess_Blocked()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
using System.IO;
public class Script
{
    public static void Execute()
    {
        File.WriteAllText(""/tmp/malicious.txt"", ""pwned"");
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("File I/O", result.Exception?.Message ?? string.Join(", ", result.SecurityEvents));
    }

    [Fact]
    public async Task ExecuteAsync_NetworkAccess_Blocked()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
using System.Net;
using System.Net.Sockets;
public class Script
{
    public static void Execute()
    {
        var client = new TcpClient(""evil.com"", 1337);
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Network", result.Exception?.Message ?? string.Join(", ", result.SecurityEvents));
    }

    [Fact]
    public async Task ExecuteAsync_ProcessSpawning_Blocked()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
using System.Diagnostics;
public class Script
{
    public static void Execute()
    {
        Process.Start(""cmd.exe"", ""/c echo pwned"");
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Process", result.Exception?.Message ?? string.Join(", ", result.SecurityEvents));
    }

    #endregion

    #region Sandbox Escape Attempt Tests (CRITICAL)

    [Fact]
    public async Task ExecuteAsync_ReflectionEscape_Blocked()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
using System.Reflection;
public class Script
{
    public static void Execute()
    {
        // Attempt to use reflection to access forbidden APIs
        var fileType = Type.GetType(""System.IO.File"");
        var deleteMethod = fileType.GetMethod(""Delete"");
        deleteMethod.Invoke(null, new object[] { ""important.txt"" });
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_TypeSpoofing_Blocked()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        // Attempt to create types that look like system types
        var fakeFile = Activator.CreateInstance(typeof(System.IO.FileInfo), ""/etc/passwd"");
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_AssemblyLoading_Blocked()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
using System.Reflection;
public class Script
{
    public static void Execute()
    {
        // Attempt to load malicious assembly
        Assembly.Load(""MaliciousLibrary"");
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_DynamicCodeGeneration_Blocked()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
using System.Reflection.Emit;
public class Script
{
    public static void Execute()
    {
        var method = new DynamicMethod(""Malicious"", typeof(void), null);
        var il = method.GetILGenerator();
        // Attempt to generate malicious code at runtime
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.False(result.Success);
    }

    #endregion

    #region Resource Exhaustion Tests

    [Fact]
    public async Task ExecuteAsync_ExcessiveAllocations_Handled()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("test-script")
            .WithTimeout(TimeSpan.FromSeconds(3))
            .WithMaxMemory(50 * 1024 * 1024) // 50MB
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .AllowNamespace("System.Linq")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        var list = new System.Collections.Generic.List<object>();
        for(int i = 0; i < 10000000; i++)
        {
            list.Add(new object());
        }
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert - should either timeout or exceed memory
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_StackOverflow_Caught()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("test-script")
            .WithTimeout(TimeSpan.FromSeconds(2))
            .WithMaxMemory(100 * 1024 * 1024)
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .AllowNamespace("System.Linq")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static int Execute()
    {
        return Recurse(0);
    }

    private static int Recurse(int depth)
    {
        return Recurse(depth + 1);
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_StringConcatenationBomb_Handled()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("test-script")
            .WithTimeout(TimeSpan.FromSeconds(3))
            .WithMaxMemory(20 * 1024 * 1024) // 20MB
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .AllowNamespace("System.Linq")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static string Execute()
    {
        string result = """";
        for(int i = 0; i < 1000000; i++)
        {
            result += ""x""; // Inefficient string concatenation
        }
        return result;
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert - should timeout or exceed memory
        Assert.False(result.Success);
    }

    #endregion

    #region Script Isolation Tests

    [Fact]
    public async Task ExecuteAsync_MultipleScripts_IsolatedFromEachOther()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code1 = @"
public class Script
{
    private static int sharedState = 42;
    public static int Execute() { return sharedState; }
}
";
        var code2 = @"
public class Script
{
    private static int sharedState = 100;
    public static int Execute() { return sharedState; }
}
";

        // Act
        var result1 = await sandbox.ExecuteAsync(code1, "Execute");
        var result2 = await sandbox.ExecuteAsync(code2, "Execute");

        // Assert - each script should have its own state
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(42, result1.ReturnValue);
        Assert.Equal(100, result2.ReturnValue);
    }

    [Fact]
    public async Task ExecuteAsync_StaticFields_IsolatedBetweenExecutions()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    private static int counter = 0;
    public static int Execute()
    {
        counter++;
        return counter;
    }
}
";

        // Act - execute twice
        var result1 = await sandbox.ExecuteAsync(code, "Execute");
        var result2 = await sandbox.ExecuteAsync(code, "Execute");

        // Assert - counter should be isolated (both return 1)
        Assert.Equal(1, result1.ReturnValue);
        Assert.Equal(1, result2.ReturnValue);
    }

    #endregion

    #region Concurrent Execution Tests

    [Fact]
    public async Task ExecuteAsync_ConcurrentExecutions_AllSucceed()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static int Execute(int value)
    {
        System.Threading.Thread.Sleep(50);
        return value * 2;
    }
}
";

        // Act - execute 10 scripts concurrently
        var tasks = Enumerable.Range(1, 10)
            .Select(i => sandbox.ExecuteAsync(code, "Execute", new object[] { i }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, results.Length);
        Assert.All(results, r => Assert.True(r.Success));
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal((i + 1) * 2, results[i].ReturnValue);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ConcurrentWithTimeout_HandlesCorrectly()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("test-script")
            .WithTimeout(TimeSpan.FromMilliseconds(500))
            .WithMaxMemory(100 * 1024 * 1024)
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .AllowNamespace("System.Linq")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();
        using var sandbox = new ScriptSandbox(permissions);
        var fastCode = @"
public class Script { public static int Execute() { return 42; } }
";
        var slowCode = @"
public class Script { public static void Execute() { System.Threading.Thread.Sleep(5000); } }
";

        // Act
        var task1 = sandbox.ExecuteAsync(fastCode, "Execute");
        var task2 = sandbox.ExecuteAsync(slowCode, "Execute");
        var results = await Task.WhenAll(task1, task2);

        // Assert
        Assert.True(results[0].Success); // Fast script succeeds
        Assert.False(results[1].Success); // Slow script times out
    }

    #endregion

    #region Disposal and Cleanup Tests

    [Fact]
    public async Task Dispose_AfterExecution_CleansUpResources()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static int Execute() { return 42; }
}
";

        // Act
        await sandbox.ExecuteAsync(code, "Execute");
        sandbox.Dispose();

        // Assert - disposed sandbox should throw
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await sandbox.ExecuteAsync(code, "Execute"));
    }

    [Fact]
    public void Dispose_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        var sandbox = new ScriptSandbox(permissions);

        // Act & Assert
        sandbox.Dispose();
        sandbox.Dispose(); // Should not throw
    }

    #endregion

    #region Execution Result Tests

    [Fact]
    public async Task ExecuteAsync_TracksExecutionTime()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static void Execute()
    {
        System.Threading.Thread.Sleep(100);
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.True(result.ExecutionTime.TotalMilliseconds >= 90);
    }

    [Fact]
    public async Task ExecuteAsync_TracksMemoryUsage()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static byte[] Execute()
    {
        return new byte[1024 * 1024]; // 1MB
    }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.True(result.MemoryUsed > 0);
    }

    [Fact]
    public async Task ExecuteAsync_LogsSecurityEvents()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static int Execute() { return 42; }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");

        // Assert
        Assert.NotNull(result.SecurityEvents);
        Assert.Contains(result.SecurityEvents, e => e.Contains("Validation"));
        Assert.Contains(result.SecurityEvents, e => e.Contains("Compilation"));
    }

    [Fact]
    public async Task ExecuteAsync_ResultToString_FormatsCorrectly()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var code = @"
public class Script
{
    public static int Execute() { return 42; }
}
";

        // Act
        var result = await sandbox.ExecuteAsync(code, "Execute");
        var resultString = result.ToString();

        // Assert
        Assert.Contains("Success", resultString);
        Assert.Contains("42", resultString);
    }

    #endregion

    #region Error Recovery Tests

    [Fact]
    public async Task ExecuteAsync_AfterFailedExecution_RecoverCorrectly()
    {
        // Arrange
        var permissions = CreateBasicPermissions();
        using var sandbox = new ScriptSandbox(permissions);
        var failingCode = @"
public class Script
{
    public static void Execute() { throw new System.Exception(""Fail""); }
}
";
        var successCode = @"
public class Script
{
    public static int Execute() { return 42; }
}
";

        // Act
        var failResult = await sandbox.ExecuteAsync(failingCode, "Execute");
        var successResult = await sandbox.ExecuteAsync(successCode, "Execute");

        // Assert
        Assert.False(failResult.Success);
        Assert.True(successResult.Success);
    }

    [Fact]
    public async Task ExecuteAsync_AfterTimeout_RecoverCorrectly()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("test-script")
            .WithTimeout(TimeSpan.FromMilliseconds(200))
            .WithMaxMemory(100 * 1024 * 1024)
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .AllowNamespace("System.Linq")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();
        using var sandbox = new ScriptSandbox(permissions);
        var slowCode = @"
public class Script
{
    public static void Execute() { System.Threading.Thread.Sleep(5000); }
}
";
        var fastCode = @"
public class Script
{
    public static int Execute() { return 42; }
}
";

        // Act
        var timeoutResult = await sandbox.ExecuteAsync(slowCode, "Execute");
        var successResult = await sandbox.ExecuteAsync(fastCode, "Execute");

        // Assert
        Assert.False(timeoutResult.Success);
        Assert.True(successResult.Success);
    }

    #endregion

    #region Helper Methods

    private ScriptPermissions CreateBasicPermissions()
    {
        return ScriptPermissions.CreateBuilder()
            .WithScriptId("test-script")
            .WithTimeout(TimeSpan.FromSeconds(5))
            .WithMaxMemory(100 * 1024 * 1024) // 100MB
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .AllowNamespace("System.Linq")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();
    }

    #endregion
}
