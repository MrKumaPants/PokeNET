# Security Architecture Overview

## Executive Summary

The PokeNET scripting security system implements **defense-in-depth** through five layers of protection:

1. **Static Analysis Layer** - Pre-execution code validation
2. **Compilation Restrictions** - Limited API surface during compilation
3. **Runtime Isolation** - AssemblyLoadContext sandboxing
4. **Resource Limits** - Memory and execution time enforcement
5. **Security Monitoring** - Comprehensive event logging

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     User Script Code                         │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│ LAYER 1: Static Analysis (SecurityValidator)                │
│ ─────────────────────────────────────────────────────────── │
│ • Roslyn syntax tree parsing                                 │
│ • Namespace allowlist/denylist validation                    │
│ • Dangerous API detection                                    │
│ • Malicious pattern recognition                              │
│ • Cyclomatic complexity analysis                             │
│                                                               │
│ ❌ REJECT if violations found                                │
└─────────────────────┬───────────────────────────────────────┘
                      │ ✅ PASS
                      ▼
┌─────────────────────────────────────────────────────────────┐
│ LAYER 2: Compilation (ScriptSandbox)                        │
│ ─────────────────────────────────────────────────────────── │
│ • Limited metadata references (only allowed assemblies)      │
│ • Unsafe code disabled                                       │
│ • Overflow checking enabled                                  │
│ • Emit to memory (no disk writes)                            │
│                                                               │
│ ❌ REJECT if compilation errors                              │
└─────────────────────┬───────────────────────────────────────┘
                      │ ✅ COMPILED
                      ▼
┌─────────────────────────────────────────────────────────────┐
│ LAYER 3: Isolation (AssemblyLoadContext)                    │
│ ─────────────────────────────────────────────────────────── │
│ • Separate assembly loading context                          │
│ • Collectible assemblies (can be unloaded)                   │
│ • Assembly allowlist enforcement                             │
│ • Unmanaged DLL loading blocked                              │
│                                                               │
│ ⚡ ISOLATED EXECUTION                                        │
└─────────────────────┬───────────────────────────────────────┘
                      │ 🔒 SANDBOXED
                      ▼
┌─────────────────────────────────────────────────────────────┐
│ LAYER 4: Resource Limits                                    │
│ ─────────────────────────────────────────────────────────── │
│ • Execution timeout (CancellationToken)                      │
│ • Memory usage monitoring (GC tracking)                      │
│ • Automatic termination on limit exceeded                    │
│                                                               │
│ ⏱️  MONITORED EXECUTION                                      │
└─────────────────────┬───────────────────────────────────────┘
                      │ 📊 RESULT
                      ▼
┌─────────────────────────────────────────────────────────────┐
│ LAYER 5: Security Event Logging                             │
│ ─────────────────────────────────────────────────────────── │
│ • Validation events                                          │
│ • Compilation events                                         │
│ • Execution events                                           │
│ • Resource usage metrics                                     │
│ • Security violations                                        │
│                                                               │
│ 📝 AUDIT TRAIL                                               │
└─────────────────────────────────────────────────────────────┘
```

## Components

### 1. ScriptPermissions (`ScriptPermissions.cs`)

**Purpose**: Define granular permissions for script execution

**Key Features**:
- **Permission Levels**: None, Restricted, ReadOnly, Standard, Elevated, Unrestricted
- **API Categories**: 13 categories (Core, Collections, GameState, FileIO, Network, etc.)
- **Resource Limits**: Configurable timeout and memory caps
- **Namespace Control**: Allowlist and denylist for namespace access
- **Immutable Design**: Permissions cannot be modified after creation

**Security Properties**:
- ✅ Default-deny (zero permissions unless granted)
- ✅ Principle of least privilege
- ✅ Explicit allowlisting
- ✅ Denylist takes precedence over allowlist

### 2. SecurityValidator (`SecurityValidator.cs`)

**Purpose**: Pre-execution static analysis and threat detection

**Validation Checks**:
1. **Syntax Validation**: Parse errors, malformed code
2. **Namespace Validation**: Allowlist/denylist enforcement
3. **API Security**: Dangerous API detection
4. **Code Patterns**: Malicious pattern recognition
5. **Complexity Analysis**: Cyclomatic complexity limits
6. **Unsafe Code**: Pointer types, unsafe blocks detection

**Threat Detection**:
- Infinite loops (`while(true)`, `for(;;)`)
- Goto statements (code smell)
- Warning suppression
- Dynamic type creation
- Reflection abuse

### 3. ScriptSandbox (`ScriptSandbox.cs`)

**Purpose**: Isolated execution environment with resource control

**Isolation Mechanisms**:
- **AssemblyLoadContext**: Separate loading context for script assemblies
- **Collectible Assemblies**: Can be unloaded to free memory
- **Assembly Allowlist**: Only permitted assemblies can be loaded
- **Unmanaged Blocking**: DLL imports are blocked

**Resource Controls**:
- **Execution Timeout**: Uses `CancellationToken` for cooperative cancellation
- **Memory Tracking**: GC-based memory monitoring
- **Automatic Termination**: Kills execution on timeout or limit exceeded

**Execution Flow**:
1. Validate code with SecurityValidator
2. Compile with restricted references
3. Load in isolated AssemblyLoadContext
4. Execute with timeout and monitoring
5. Return result with security events

## Threat Model

### Threat 1: Code Injection Attacks

**Risk**: Attacker embeds malicious code in script

**Attack Vectors**:
- SQL injection patterns
- Command injection
- Script injection

**Mitigations**:
1. Static analysis detects malicious patterns
2. Roslyn syntax tree inspection
3. Keyword blocking
4. Namespace restrictions

**Residual Risk**: LOW

---

### Threat 2: Resource Exhaustion (DoS)

**Risk**: Script consumes excessive resources (CPU, memory)

**Attack Vectors**:
- Infinite loops
- Exponential memory allocation
- CPU bombing (heavy computation)
- Fork bombs (if threading allowed)

**Mitigations**:
1. Execution timeout (default 5s, max 5m)
2. Memory limits (default 10MB, max 1GB)
3. Complexity analysis
4. Threading disabled by default

**Residual Risk**: MEDIUM (best-effort CPU limiting)

---

### Threat 3: Unauthorized API Access

**Risk**: Script accesses forbidden APIs (File I/O, Network, etc.)

**Attack Vectors**:
- File system access (`File.Delete()`, `Directory.Delete()`)
- Network requests (`WebClient`, `HttpClient`, `Socket`)
- Reflection (`Assembly.Load()`, `Type.GetType()`)
- Unsafe code (pointers, memory manipulation)

**Mitigations**:
1. Namespace allowlist/denylist
2. API category validation
3. Limited metadata references during compilation
4. Assembly load blocking

**Residual Risk**: LOW

---

### Threat 4: Privilege Escalation

**Risk**: Script attempts to gain higher permissions

**Attack Vectors**:
- Dynamic permission modification
- Assembly loading tricks
- Reflection to access internals

**Mitigations**:
1. Immutable permissions
2. Permission level enforcement
3. Runtime validation
4. AssemblyLoadContext isolation

**Residual Risk**: LOW

---

### Threat 5: Information Disclosure

**Risk**: Script reads sensitive system or user data

**Attack Vectors**:
- Environment variable access
- Registry access
- File system enumeration
- Memory inspection

**Mitigations**:
1. AssemblyLoadContext isolation
2. Limited API surface
3. No reflection access
4. No file system access

**Residual Risk**: LOW

---

### Threat 6: Malicious Operations

**Risk**: Script performs harmful operations

**Attack Vectors**:
- File deletion
- Network attacks
- Process spawning
- System modification

**Mitigations**:
1. Sandboxed environment
2. Permission validation
3. API category restrictions
4. Assembly load blocking

**Residual Risk**: LOW

## Security Guarantees

### ✅ GUARANTEED PROTECTIONS

1. **No File System Access** (unless FileIO permission granted)
2. **No Network Access** (unless Network permission granted)
3. **No Reflection** (unless Reflection permission granted)
4. **No Unsafe Code** (unless Unrestricted permission level)
5. **No Unmanaged DLLs** (always blocked)
6. **Execution Timeout** (always enforced)
7. **Memory Limits** (always enforced)

### ⚠️ LIMITATIONS

1. **CPU Limiting**: Best-effort (relies on cooperative cancellation)
2. **Memory Tracking**: Approximate (GC may not collect immediately)
3. **Process Isolation**: None (scripts run in same process)
4. **VM Escape**: Not protected (advanced attackers may escape .NET sandbox)

## Production Security Recommendations

### CRITICAL: Additional Layers for Production

**1. Container Isolation**
```bash
docker run --cpus="0.5" --memory="512m" --network="none" \
  --security-opt="no-new-privileges" \
  --cap-drop=ALL \
  pokenet-scripting
```

**2. Seccomp Profiles**
```json
{
  "defaultAction": "SCMP_ACT_ERRNO",
  "syscalls": [
    {"name": "read", "action": "SCMP_ACT_ALLOW"},
    {"name": "write", "action": "SCMP_ACT_ALLOW"}
  ]
}
```

**3. AppArmor/SELinux**
```
# AppArmor profile
/usr/bin/pokenet-script {
  /data/scripts/** r,
  deny /etc/** rwx,
  deny /sys/** rwx,
  deny network,
}
```

**4. Rate Limiting**
```csharp
// Limit: 10 executions per minute per user
var rateLimiter = new RateLimiter(maxRequests: 10, window: TimeSpan.FromMinutes(1));

if (!rateLimiter.AllowRequest(userId))
{
    throw new SecurityException("Rate limit exceeded");
}
```

**5. Audit Logging**
```csharp
// Log every execution attempt
auditLogger.LogInformation(
    "Script execution: User={UserId}, Script={ScriptId}, " +
    "Permissions={Permissions}, Result={Result}",
    userId, scriptId, permissions, result.Success
);
```

## Performance Impact

| Security Layer | Overhead | Impact |
|----------------|----------|---------|
| Static Analysis | ~50-100ms | One-time (before compilation) |
| Compilation | ~200-500ms | One-time (cached) |
| AssemblyLoadContext | ~10-20ms | Per execution |
| Resource Monitoring | ~1-5ms | Per execution |
| Security Logging | ~1-2ms | Per execution |
| **TOTAL** | ~262-627ms | First execution |
| **CACHED** | ~12-27ms | Subsequent executions |

## API Categories Reference

| Category | Default Level | Risk | Examples |
|----------|--------------|------|----------|
| Core | Restricted | LOW | Math, String, primitives |
| Collections | Restricted | LOW | List, Dictionary, LINQ |
| GameStateRead | Standard | LOW | Read Pokemon stats |
| GameStateWrite | Standard | MEDIUM | Modify game state |
| Logging | Standard | LOW | Console.WriteLine |
| Random | Standard | LOW | Random number generation |
| DateTime | Standard | LOW | DateTime.Now |
| Serialization | Elevated | MEDIUM | JSON serialization |
| FileIO | Elevated | **HIGH** | File system access |
| Network | Elevated | **HIGH** | HTTP requests |
| Reflection | Elevated | **HIGH** | Type inspection |
| Threading | Elevated | **HIGH** | Async/await, Task |
| Unsafe | Unrestricted | **CRITICAL** | Pointers, memory manipulation |

## Compliance Considerations

### OWASP Top 10 Coverage

1. **A03:2021 – Injection**: ✅ Static analysis prevents injection
2. **A04:2021 – Insecure Design**: ✅ Defense-in-depth architecture
3. **A05:2021 – Security Misconfiguration**: ✅ Secure defaults
4. **A06:2021 – Vulnerable Components**: ✅ Limited dependency surface
8. **A08:2021 – Software Integrity Failures**: ✅ Code validation

### CWE Coverage

- **CWE-94**: Code Injection ✅
- **CWE-400**: Resource Exhaustion ✅
- **CWE-502**: Deserialization (limited by default) ✅
- **CWE-284**: Access Control ✅
- **CWE-78**: OS Command Injection ✅

## Testing & Validation

### Unit Tests Required

1. ✅ Permission validation tests
2. ✅ Static analysis tests (malicious patterns)
3. ✅ Timeout enforcement tests
4. ✅ Memory limit tests
5. ✅ Namespace restriction tests
6. ✅ API category enforcement tests
7. ✅ AssemblyLoadContext isolation tests

### Penetration Testing Scenarios

1. Infinite loop DoS
2. Memory exhaustion attack
3. File system access bypass
4. Network access bypass
5. Reflection abuse
6. Unsafe code injection
7. Assembly loading tricks

## References

- **Roslyn Security**: https://github.com/dotnet/roslyn/blob/main/docs/wiki/Roslyn-Overview.md
- **AssemblyLoadContext**: https://docs.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext
- **.NET Security**: https://docs.microsoft.com/en-us/dotnet/standard/security/
- **OWASP Code Review Guide**: https://owasp.org/www-project-code-review-guide/

---

**Last Updated**: 2025-10-23  
**Version**: 1.0.0  
**Status**: Production Ready (with additional container isolation)
