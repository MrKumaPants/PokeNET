using System;
using System.Threading.Tasks;
using PokeNET.Scripting.Security;

// Quick test for VULN-STACK-001 fix
class StackOverflowTest
{
    static async Task Main()
    {
        Console.WriteLine("Testing VULN-STACK-001 fix: Stack Overflow Protection");
        Console.WriteLine("=====================================================\n");

        var permissions = ScriptPermissions
            .CreateBuilder()
            .WithScriptId("stack-overflow-test")
            .WithTimeout(TimeSpan.FromSeconds(5))
            .WithMaxMemory(100 * 1024 * 1024)
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .AllowNamespace("System.Linq")
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .Build();

        using var sandbox = new ScriptSandbox(permissions);

        var infiniteRecursionScript =
            @"
public class Script
{
    public static int Execute()
    {
        return Recurse(0);
    }

    private static int Recurse(int depth)
    {
        return Recurse(depth + 1);  // Infinite recursion
    }
}
";

        Console.WriteLine("Executing infinite recursion script...");
        var result = await sandbox.ExecuteAsync(infiniteRecursionScript, "Execute");

        Console.WriteLine($"\nResult:");
        Console.WriteLine($"  Success: {result.Success}");
        Console.WriteLine($"  Execution Time: {result.ExecutionTime.TotalMilliseconds}ms");
        Console.WriteLine($"  Exception: {result.Exception?.GetType().Name}");
        Console.WriteLine($"  Message: {result.Exception?.Message}");
        Console.WriteLine($"\nSecurity Events:");
        foreach (var evt in result.SecurityEvents)
        {
            Console.WriteLine($"  - {evt}");
        }

        if (!result.Success)
        {
            Console.WriteLine("\n✅ SUCCESS: Stack overflow was caught and handled gracefully!");
            Console.WriteLine("   Process did NOT crash!");
        }
        else
        {
            Console.WriteLine("\n❌ FAILURE: Script should have been stopped!");
        }
    }
}
