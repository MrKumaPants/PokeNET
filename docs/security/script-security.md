# Script Security Model

## Overview

PokeNET's scripting system uses **Roslyn** (Microsoft.CodeAnalysis.CSharp.Scripting) to execute C# scripts at runtime. This document details the comprehensive security model that protects the game and player systems from malicious or poorly-written scripts.

## Table of Contents

1. [Security Philosophy](#security-philosophy)
2. [Sandboxing Architecture](#sandboxing-architecture)
3. [Execution Boundaries](#execution-boundaries)
4. [API Surface Restrictions](#api-surface-restrictions)
5. [Resource Limits](#resource-limits)
6. [Validation and Verification](#validation-and-verification)
7. [Best Practices](#best-practices)
8. [Threat Model](#threat-model)

---

## Security Philosophy

### Defense in Depth

PokeNET employs multiple layers of security:

1. **Code Analysis** - Static analysis before execution
2. **Sandboxing** - Restricted execution environment
3. **Resource Limits** - Memory, CPU, and time constraints
4. **API Restrictions** - Limited access to system resources
5. **Runtime Monitoring** - Continuous execution tracking

### Trust Model

- **Game Core**: Fully trusted, full system access
- **Official Mods**: Reviewed, trusted with extended permissions
- **Community Mods**: Sandboxed, restricted permissions
- **User Scripts**: Untrusted, maximum restrictions

---

## Sandboxing Architecture

### Execution Context Isolation

Scripts execute in an isolated context with no direct access to:

- File system
- Network sockets
- Process management
- Registry/system configuration
- Native code interop
- Reflection on internal types

### Security Boundaries

```csharp
public class ScriptSecurityContext
{
    // Allowed namespaces
    public static readonly HashSet<string> AllowedNamespaces = new()
    {
        "System",
        "System.Linq",
        "System.Collections.Generic",
        "System.Text",
        "System.Text.RegularExpressions",
        "PokeNET.ModApi",
        "PokeNET.ModApi.Events",
        "PokeNET.ModApi.Battle",
        "PokeNET.ModApi.Data"
    };

    // Blocked types
    public static readonly HashSet<string> BlockedTypes = new()
    {
        "System.IO.File",
        "System.IO.Directory",
        "System.Net.Http.HttpClient",
        "System.Net.Sockets.Socket",
        "System.Diagnostics.Process",
        "System.Reflection.Assembly",
        "System.Runtime.InteropServices.Marshal"
    };

    // Blocked namespaces
    public static readonly HashSet<string> BlockedNamespaces = new()
    {
        "System.IO",
        "System.Net",
        "System.Net.Http",
        "System.Net.Sockets",
        "System.Diagnostics",
        "System.Reflection",
        "System.Runtime.InteropServices",
        "System.Security",
        "Microsoft.Win32"
    };
}
```

### Compilation Options

Scripts are compiled with restricted options:

```csharp
var scriptOptions = ScriptOptions.Default
    .WithReferences(allowedAssemblies)
    .WithImports(allowedNamespaces)
    .WithEmitDebugInformation(false)
    .WithOptimizationLevel(OptimizationLevel.Release)
    .WithAllowUnsafe(false); // No unsafe code
```

---

## Execution Boundaries

### Time Limits

Scripts have strict execution time limits to prevent infinite loops:

```csharp
public class ExecutionLimits
{
    // Maximum execution time per script call
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    // Maximum execution time per event handler
    public TimeSpan EventHandlerTimeout { get; set; } = TimeSpan.FromSeconds(2);

    // Maximum initialization time
    public TimeSpan InitializationTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
```

**Enforcement:**
- Cancellation tokens for async operations
- Thread pool timeout guards
- Watchdog timer for long-running operations

### Memory Limits

Memory usage is strictly controlled:

```csharp
public class MemoryLimits
{
    // Maximum memory allocation per script
    public long MaxMemoryBytes { get; set; } = 50 * 1024 * 1024; // 50 MB

    // Maximum object graph depth
    public int MaxObjectDepth { get; set; } = 100;

    // Maximum string length
    public int MaxStringLength { get; set; } = 1_000_000; // 1 MB

    // Maximum collection size
    public int MaxCollectionSize { get; set; } = 100_000;
}
```

**Monitoring:**
- Periodic memory usage checks
- Garbage collection pressure monitoring
- Allocation tracking per script

### Operation Limits

Scripts are limited in operations per time period:

```csharp
public class OperationLimits
{
    // Maximum entity queries per second
    public int MaxQueriesPerSecond { get; set; } = 1000;

    // Maximum events published per second
    public int MaxEventsPerSecond { get; set; } = 100;

    // Maximum API calls per second
    public int MaxApiCallsPerSecond { get; set; } = 10_000;

    // Maximum log entries per second
    public int MaxLogsPerSecond { get; set; } = 100;
}
```

---

## API Surface Restrictions

### Allowed Operations

Scripts can:

✅ **Read game data** (creatures, moves, abilities, items)
```csharp
var creature = Api.Data.GetCreature("pikachu");
var move = Api.Data.GetMove("thunderbolt");
```

✅ **Query entities** (read-only by default)
```csharp
var entities = Api.Entities.Query<Health, PlayerControlled>();
```

✅ **Modify entities through API** (controlled mutations)
```csharp
ref var health = ref entity.Get<Health>();
health.Current -= damage; // Validated
```

✅ **Subscribe to events** (sandboxed event handlers)
```csharp
Api.Events.Subscribe<BattleStartEvent>(OnBattleStart);
```

✅ **Log information** (rate-limited)
```csharp
Api.Logger.LogInformation("Script executed");
```

### Prohibited Operations

Scripts **cannot**:

❌ **Access file system**
```csharp
// BLOCKED - will throw SecurityException
File.ReadAllText("data.txt");
Directory.GetFiles("C:\\");
```

❌ **Make network calls**
```csharp
// BLOCKED - HttpClient not available
var client = new HttpClient();
await client.GetAsync("http://evil.com");
```

❌ **Execute processes**
```csharp
// BLOCKED - Process type not available
Process.Start("cmd.exe");
```

❌ **Use reflection on game internals**
```csharp
// BLOCKED - restricted reflection
var assembly = Assembly.Load("PokeNET.Core");
var type = assembly.GetType("InternalClass");
```

❌ **Execute unsafe code**
```csharp
// BLOCKED - unsafe not allowed
unsafe
{
    int* ptr = stackalloc int[100];
}
```

❌ **Create threads directly**
```csharp
// BLOCKED - Thread type restricted
new Thread(() => { }).Start();
```

### API Validation

All API calls go through validation layers:

```csharp
public class ApiValidator
{
    public void ValidateEntityAccess(Entity entity, AccessType access)
    {
        // Check if script has permission to access entity
        if (access == AccessType.Write && !CanWrite(entity))
        {
            throw new SecurityException(
                "Script does not have write permission for this entity");
        }
    }

    public void ValidateDataAccess(string dataId)
    {
        // Validate data ID doesn't contain path traversal
        if (dataId.Contains("..") || dataId.Contains("/") || dataId.Contains("\\"))
        {
            throw new SecurityException("Invalid data identifier");
        }
    }

    public void ValidateEventPublish(object evt)
    {
        // Check if script can publish this event type
        if (evt is SystemEvent)
        {
            throw new SecurityException(
                "Scripts cannot publish system events");
        }
    }
}
```

---

## Resource Limits

### Rate Limiting

Operations are rate-limited using token bucket algorithm:

```csharp
public class RateLimiter
{
    private readonly int _maxTokens;
    private readonly TimeSpan _refillInterval;
    private int _tokens;
    private DateTime _lastRefill;

    public bool TryConsume(int cost = 1)
    {
        RefillTokens();

        if (_tokens >= cost)
        {
            _tokens -= cost;
            return true;
        }

        return false;
    }

    private void RefillTokens()
    {
        var now = DateTime.UtcNow;
        var elapsed = now - _lastRefill;

        if (elapsed >= _refillInterval)
        {
            _tokens = _maxTokens;
            _lastRefill = now;
        }
    }
}

// Usage
if (!_apiRateLimiter.TryConsume())
{
    throw new RateLimitException("API call rate limit exceeded");
}
```

### Execution Quotas

Scripts have execution quotas per time window:

```csharp
public class ExecutionQuota
{
    // Maximum CPU time per minute
    public TimeSpan CpuTimePerMinute { get; set; } = TimeSpan.FromSeconds(30);

    // Maximum event handler invocations per minute
    public int MaxEventHandlerCalls { get; set; } = 1000;

    // Maximum entity modifications per minute
    public int MaxEntityWrites { get; set; } = 500;

    // Maximum log entries per minute
    public int MaxLogEntries { get; set; } = 1000;
}
```

### Circuit Breaker

Scripts that repeatedly fail or exceed limits are disabled:

```csharp
public class CircuitBreaker
{
    private int _failureCount;
    private DateTime _lastFailure;
    private CircuitState _state = CircuitState.Closed;

    public bool IsOpen => _state == CircuitState.Open;

    public void RecordSuccess()
    {
        _failureCount = 0;
        _state = CircuitState.Closed;
    }

    public void RecordFailure()
    {
        _failureCount++;
        _lastFailure = DateTime.UtcNow;

        if (_failureCount >= 5)
        {
            _state = CircuitState.Open;
            // Script is disabled for 5 minutes
        }
    }
}
```

---

## Validation and Verification

### Static Analysis

Scripts undergo static analysis before execution:

```csharp
public class ScriptAnalyzer
{
    public ValidationResult Analyze(string scriptCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(scriptCode);
        var root = syntaxTree.GetRoot();

        var issues = new List<SecurityIssue>();

        // Check for blocked types
        foreach (var identifier in root.DescendantNodes()
            .OfType<IdentifierNameSyntax>())
        {
            if (BlockedTypes.Contains(identifier.Identifier.Text))
            {
                issues.Add(new SecurityIssue
                {
                    Severity = Severity.Error,
                    Message = $"Use of blocked type: {identifier.Identifier.Text}",
                    Location = identifier.GetLocation()
                });
            }
        }

        // Check for infinite loops
        foreach (var whileLoop in root.DescendantNodes()
            .OfType<WhileStatementSyntax>())
        {
            if (IsPotentialInfiniteLoop(whileLoop))
            {
                issues.Add(new SecurityIssue
                {
                    Severity = Severity.Warning,
                    Message = "Potential infinite loop detected",
                    Location = whileLoop.GetLocation()
                });
            }
        }

        // Check for excessive recursion
        foreach (var method in root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>())
        {
            if (HasDeepRecursion(method))
            {
                issues.Add(new SecurityIssue
                {
                    Severity = Severity.Warning,
                    Message = "Excessive recursion detected",
                    Location = method.GetLocation()
                });
            }
        }

        return new ValidationResult(issues);
    }
}
```

### Runtime Monitoring

Scripts are monitored during execution:

```csharp
public class ScriptMonitor
{
    public void MonitorExecution(Script script)
    {
        // CPU usage
        var cpuUsage = GetCpuUsage(script);
        if (cpuUsage > 80)
        {
            _logger.LogWarning("Script {ScriptId} using {Cpu}% CPU",
                script.Id, cpuUsage);
        }

        // Memory usage
        var memoryUsage = GetMemoryUsage(script);
        if (memoryUsage > MemoryLimits.MaxMemoryBytes)
        {
            throw new OutOfMemoryException(
                $"Script exceeded memory limit: {memoryUsage} bytes");
        }

        // Exception rate
        if (script.ExceptionRate > 0.1) // 10% failure rate
        {
            _logger.LogWarning("Script {ScriptId} has high failure rate: {Rate}%",
                script.Id, script.ExceptionRate * 100);
        }
    }
}
```

---

## Best Practices

### For Mod Developers

1. **Handle errors gracefully**
```csharp
try
{
    var data = Api.Data.GetCreature(id);
}
catch (KeyNotFoundException)
{
    // Provide fallback
}
```

2. **Avoid infinite loops**
```csharp
int iterations = 0;
while (condition && iterations < 1000)
{
    // Work
    iterations++;
}
```

3. **Cache expensive lookups**
```csharp
private Dictionary<string, CreatureDefinition> _cache = new();

private CreatureDefinition GetCachedCreature(string id)
{
    if (!_cache.TryGetValue(id, out var def))
    {
        def = Api.Data.GetCreature(id);
        _cache[id] = def;
    }
    return def;
}
```

4. **Unsubscribe from events**
```csharp
public void Cleanup()
{
    Api.Events.Unsubscribe(_handler);
}
```

### For Script Reviewers

1. **Check for resource leaks**
   - Event subscriptions without unsubscribe
   - Large collections that grow unbounded
   - Cached data that never expires

2. **Verify error handling**
   - All API calls wrapped in try-catch
   - Fallback behavior on failures
   - No exceptions thrown to game engine

3. **Review performance**
   - No O(n²) or worse algorithms
   - Appropriate use of caching
   - Minimal allocations in hot paths

4. **Security review**
   - No path traversal attempts
   - No reflection usage
   - No dangerous regex patterns

---

## Threat Model

### Identified Threats

1. **Denial of Service**
   - Infinite loops
   - Memory exhaustion
   - CPU saturation
   - **Mitigation**: Time limits, memory limits, rate limiting

2. **Data Corruption**
   - Invalid entity modifications
   - Race conditions
   - State inconsistencies
   - **Mitigation**: API validation, transaction boundaries

3. **Information Disclosure**
   - Reading sensitive game state
   - Accessing other players' data
   - **Mitigation**: Access control, data isolation

4. **Privilege Escalation**
   - Reflection to access internals
   - Breaking out of sandbox
   - **Mitigation**: Restricted APIs, sandboxing

### Security Boundaries

```
┌─────────────────────────────────────┐
│         Game Engine (Trusted)        │
│  - Full system access                │
│  - Unrestricted APIs                 │
└──────────────┬──────────────────────┘
               │
          ┌────▼─────┐
          │   IScriptApi  │  <-- Security Boundary
          └────┬─────┘
               │
┌──────────────▼──────────────────────┐
│      Script Sandbox (Untrusted)     │
│  - Restricted namespace access       │
│  - No file I/O                       │
│  - No network access                 │
│  - No reflection                     │
│  - Memory limited                    │
│  - Time limited                      │
└─────────────────────────────────────┘
```

---

## Incident Response

### Detecting Malicious Scripts

Signs of malicious or problematic scripts:

- Excessive CPU usage (>50% for >5 seconds)
- Rapid memory growth (>10 MB/second)
- High API call rate (>1000 calls/second)
- Frequent exceptions (>10% failure rate)
- Suspicious code patterns (reflection, path traversal)

### Automatic Response

```csharp
public class SecurityIncidentHandler
{
    public void HandleIncident(Script script, SecurityIncident incident)
    {
        // Log incident
        _logger.LogWarning(
            "Security incident: {Type} from script {ScriptId}",
            incident.Type, script.Id);

        // Record in database
        _incidentRepository.Record(incident);

        // Take action based on severity
        switch (incident.Severity)
        {
            case Severity.High:
                // Immediately disable script
                script.Disable();
                NotifyAdministrators(incident);
                break;

            case Severity.Medium:
                // Increase monitoring
                script.MonitoringLevel = MonitoringLevel.Detailed;
                break;

            case Severity.Low:
                // Log and continue
                break;
        }
    }
}
```

---

## Compliance and Auditing

### Audit Logging

All script security events are logged:

```csharp
public class SecurityAuditLog
{
    public void LogSecurityEvent(SecurityEvent evt)
    {
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            ScriptId = evt.ScriptId,
            EventType = evt.Type,
            Severity = evt.Severity,
            Details = evt.Details,
            UserImpact = evt.UserImpact
        };

        _repository.Save(entry);
    }
}
```

### Review Process

Scripts submitted to the official mod repository undergo:

1. Automated security scanning
2. Manual code review by maintainers
3. Sandbox testing with monitoring
4. Community testing period

---

## References

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [.NET Security Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/security/)
- [Roslyn Scripting Security](https://github.com/dotnet/roslyn/wiki/Scripting-API-Samples#security)

---

*Last Updated: 2025-10-22*
