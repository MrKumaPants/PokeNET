using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;

namespace PokeNET.Scripting.Security;

/// <summary>
/// Provides a secure sandbox environment for executing untrusted scripts with isolation,
/// resource limits, and permission enforcement.
/// </summary>
/// <remarks>
/// SECURITY ARCHITECTURE:
///
/// 1. ISOLATION LAYERS:
///    - AssemblyLoadContext: Separate assembly loading context
///    - Permission Validation: Pre-execution security checks
///    - Resource Monitoring: Real-time resource tracking
///    - Timeout Enforcement: Automatic termination on timeout
///
/// 2. DEFENSE IN DEPTH:
///    Layer 1: Static analysis (SecurityValidator)
///    Layer 2: Compilation restrictions (limited API surface)
///    Layer 3: Runtime isolation (AssemblyLoadContext)
///    Layer 4: Resource limits (memory, CPU, timeout)
///    Layer 5: Monitoring & logging (security events)
///
/// 3. THREAT MITIGATION:
///    - Code Injection: Static analysis + compilation validation
///    - Resource Exhaustion: Timeout + memory limits + CPU monitoring
///    - Unauthorized Access: Permission validation + namespace restrictions
///    - Privilege Escalation: Permission level enforcement
///    - Information Disclosure: Isolated execution context
///    - Malicious Operations: Sandboxed environment + limited API surface
///
/// 4. LIMITATIONS (continued):
///    - Does NOT provide full process isolation (use containers for that)
///    - CPU time limiting is best-effort (relies on cooperative cancellation)
///    - Memory limits are approximate (GC may not collect immediately)
///    - Advanced attackers may find VM escape vulnerabilities
///
/// RECOMMENDED ADDITIONAL SECURITY (Production):
///    - Run in Docker container with resource limits
///    - Use seccomp profiles to restrict syscalls
///    - Enable AppArmor/SELinux mandatory access control
///    - Monitor system calls and network activity
///    - Implement rate limiting for script execution
///    - Use hardware-based isolation (SGX, TrustZone) for sensitive data
/// </remarks>
public sealed class ScriptSandbox : IDisposable
{
    private readonly ScriptPermissions _permissions;
    private readonly SecurityValidator _validator;
    private readonly ILogger<ScriptSandbox>? _logger;
    private readonly SandboxLoadContext? _loadContext;
    private bool _disposed;

    /// <summary>
    /// Execution result with security context
    /// </summary>
    public sealed class ExecutionResult
    {
        public bool Success { get; init; }
        public object? ReturnValue { get; init; }
        public Exception? Exception { get; init; }
        public TimeSpan ExecutionTime { get; init; }
        public long MemoryUsed { get; init; }
        public IReadOnlyList<string> SecurityEvents { get; init; } = Array.Empty<string>();
        public bool TimedOut { get; init; }
        public bool MemoryLimitExceeded { get; init; }

        public override string ToString()
        {
            if (Success)
                return $"Success: {ReturnValue} (Time: {ExecutionTime.TotalMilliseconds}ms, Memory: {MemoryUsed / 1024}KB)";

            if (TimedOut)
                return $"Failed: Execution timeout after {ExecutionTime.TotalSeconds}s";

            if (MemoryLimitExceeded)
                return $"Failed: Memory limit exceeded ({MemoryUsed / 1024 / 1024}MB)";

            return $"Failed: {Exception?.Message}";
        }
    }

    /// <summary>
    /// Custom AssemblyLoadContext for script isolation
    /// </summary>
    private sealed class SandboxLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;
        private readonly HashSet<string> _allowedAssemblies;

        public SandboxLoadContext(string assemblyPath, IEnumerable<string> allowedAssemblies)
            : base(isCollectible: true) // Allows unloading
        {
            _resolver = new AssemblyDependencyResolver(assemblyPath);
            _allowedAssemblies = new HashSet<string>(allowedAssemblies, StringComparer.OrdinalIgnoreCase);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Only allow explicitly permitted assemblies
            if (!_allowedAssemblies.Contains(assemblyName.Name ?? string.Empty))
            {
                throw new SecurityException($"Assembly '{assemblyName.Name}' is not allowed in this sandbox");
            }

            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            // Deny all unmanaged DLL loading
            throw new SecurityException($"Loading unmanaged DLL '{unmanagedDllName}' is not permitted");
        }
    }

    /// <summary>
    /// Security exception for sandbox violations
    /// </summary>
    public sealed class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }
        public SecurityException(string message, Exception inner) : base(message, inner) { }
    }

    public ScriptSandbox(ScriptPermissions permissions, ILogger<ScriptSandbox>? logger = null)
    {
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _validator = new SecurityValidator(_permissions);
        _logger = logger;

        // Create isolated load context if external assemblies are allowed
        if (_permissions.CanLoadExternalAssemblies)
        {
            var allowedAssemblies = new[]
            {
                "System.Runtime",
                "System.Collections",
                "System.Linq",
                "System.Console",
                "netstandard",
                "PokeNET.Scripting"
            };

            // Use a temporary assembly path for the resolver
            var tempPath = Path.Combine(Path.GetTempPath(), $"sandbox_{Guid.NewGuid()}.dll");
            _loadContext = new SandboxLoadContext(tempPath, allowedAssemblies);
        }

        _logger?.LogInformation("ScriptSandbox created: {Permissions}", _permissions);
    }

    /// <summary>
    /// Compiles and executes a script in the sandbox
    /// </summary>
    public async Task<ExecutionResult> ExecuteAsync(
        string code,
        string? methodName = null,
        object?[]? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ScriptSandbox));

        var securityEvents = new List<string>();
        var stopwatch = Stopwatch.StartNew();
        long startMemory = GC.GetTotalMemory(forceFullCollection: true);

        try
        {
            // Step 1: Validate code security
            _logger?.LogDebug("Validating script security for {ScriptId}", _permissions.ScriptId);
            var validationResult = _validator.Validate(code, _permissions.ScriptId);

            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Violations
                    .Where(v => v.Level >= SecurityValidator.SecurityViolation.Severity.Error)
                    .Select(v => v.Message));

                _logger?.LogWarning("Script validation failed for {ScriptId}: {Errors}",
                    _permissions.ScriptId, errors);

                securityEvents.Add($"Validation failed: {errors}");

                return new ExecutionResult
                {
                    Success = false,
                    Exception = new SecurityException($"Security validation failed: {errors}"),
                    ExecutionTime = stopwatch.Elapsed,
                    SecurityEvents = securityEvents
                };
            }

            securityEvents.Add("Validation passed");

            // Step 2: Compile the script
            _logger?.LogDebug("Compiling script for {ScriptId}", _permissions.ScriptId);
            var assembly = CompileScript(code, securityEvents);

            if (assembly == null)
            {
                return new ExecutionResult
                {
                    Success = false,
                    Exception = new SecurityException("Compilation failed"),
                    ExecutionTime = stopwatch.Elapsed,
                    SecurityEvents = securityEvents
                };
            }

            securityEvents.Add("Compilation successful");

            // Step 3: Execute with timeout and resource monitoring
            _logger?.LogDebug("Executing script {ScriptId} with timeout {Timeout}",
                _permissions.ScriptId, _permissions.MaxExecutionTime);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_permissions.MaxExecutionTime);

            var result = await ExecuteAssemblyAsync(
                assembly,
                methodName ?? "Execute",
                parameters,
                cts.Token,
                securityEvents);

            stopwatch.Stop();
            long endMemory = GC.GetTotalMemory(forceFullCollection: false);
            long memoryUsed = Math.Max(0, endMemory - startMemory);

            // Check resource limits
            bool memoryExceeded = memoryUsed > _permissions.MaxMemoryBytes;
            if (memoryExceeded)
            {
                _logger?.LogWarning("Script {ScriptId} exceeded memory limit: {Used} > {Limit}",
                    _permissions.ScriptId, memoryUsed, _permissions.MaxMemoryBytes);
                securityEvents.Add($"Memory limit exceeded: {memoryUsed} bytes");
            }

            return new ExecutionResult
            {
                Success = result.success && !memoryExceeded,
                ReturnValue = result.value,
                Exception = result.exception,
                ExecutionTime = stopwatch.Elapsed,
                MemoryUsed = memoryUsed,
                SecurityEvents = securityEvents,
                TimedOut = result.exception is OperationCanceledException,
                MemoryLimitExceeded = memoryExceeded
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Sandbox execution failed for {ScriptId}", _permissions.ScriptId);
            stopwatch.Stop();

            return new ExecutionResult
            {
                Success = false,
                Exception = ex,
                ExecutionTime = stopwatch.Elapsed,
                SecurityEvents = securityEvents
            };
        }
    }

    private Assembly? CompileScript(string code, List<string> securityEvents)
    {
        try
        {
            // Parse the code
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            // Get required references
            var references = GetAllowedReferences();

            // Compilation options with security restrictions
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithAllowUnsafe(false) // Disable unsafe code
                .WithPlatform(Platform.AnyCpu)
                .WithOverflowChecks(true); // Enable overflow checking

            // Create compilation
            var compilation = CSharpCompilation.Create(
                $"Script_{_permissions.ScriptId}_{Guid.NewGuid():N}",
                new[] { syntaxTree },
                references,
                compilationOptions);

            // Emit to memory
            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);

            if (!emitResult.Success)
            {
                var errors = string.Join("; ", emitResult.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.GetMessage()));

                _logger?.LogWarning("Compilation failed: {Errors}", errors);
                securityEvents.Add($"Compilation errors: {errors}");
                return null;
            }

            ms.Seek(0, SeekOrigin.Begin);

            // Load assembly in appropriate context
            Assembly assembly;
            if (_loadContext != null)
            {
                assembly = _loadContext.LoadFromStream(ms);
                securityEvents.Add("Assembly loaded in isolated context");
            }
            else
            {
                assembly = Assembly.Load(ms.ToArray());
                securityEvents.Add("Assembly loaded in default context");
            }

            return assembly;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Compilation error");
            securityEvents.Add($"Compilation exception: {ex.Message}");
            return null;
        }
    }

    private MetadataReference[] GetAllowedReferences()
    {
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        };

        // Add System.Runtime
        var systemRuntime = Assembly.Load("System.Runtime");
        references.Add(MetadataReference.CreateFromFile(systemRuntime.Location));

        // Add System.Collections
        if (_permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Collections))
        {
            var systemCollections = Assembly.Load("System.Collections");
            references.Add(MetadataReference.CreateFromFile(systemCollections.Location));
        }

        // Only add references for allowed API categories
        // This limits the attack surface

        return references.ToArray();
    }

    private async Task<(bool success, object? value, Exception? exception)> ExecuteAssemblyAsync(
        Assembly assembly,
        string methodName,
        object?[]? parameters,
        CancellationToken cancellationToken,
        List<string> securityEvents)
    {
        try
        {
            // Find the first type with the specified method
            var type = assembly.GetTypes()
                .FirstOrDefault(t => t.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static) != null);

            if (type == null)
            {
                throw new SecurityException($"No public static method '{methodName}' found in script");
            }

            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                throw new SecurityException($"Method '{methodName}' not found");
            }

            securityEvents.Add($"Invoking method: {type.FullName}.{methodName}");

            // Execute in a separate task to enable timeout
            var task = Task.Run(() =>
            {
                // Check cancellation before execution
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var result = method.Invoke(null, parameters);
                    return (true, result, (Exception?)null);
                }
                catch (Exception ex)
                {
                    return (false, (object?)null, ex);
                }
            }, cancellationToken);

            return await task;
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Script execution timed out for {ScriptId}", _permissions.ScriptId);
            securityEvents.Add("Execution timeout");
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Execution error for {ScriptId}", _permissions.ScriptId);
            securityEvents.Add($"Execution exception: {ex.Message}");
            return (false, null, ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _logger?.LogInformation("Disposing ScriptSandbox for {ScriptId}", _permissions.ScriptId);

        // Unload the assembly context if it was created
        _loadContext?.Unload();

        // Force garbage collection to clean up sandbox resources
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        _disposed = true;
    }
}
