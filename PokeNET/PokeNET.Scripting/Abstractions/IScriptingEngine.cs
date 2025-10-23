// SOLID Principles Applied:
// - Single Responsibility: Engine only handles script lifecycle (load, compile, execute)
// - Open/Closed: Extensible through implementation without modifying interface
// - Liskov Substitution: Any implementation can replace another
// - Interface Segregation: Focused on script execution concerns only
// - Dependency Inversion: Depends on abstractions (IScriptContext, IScriptLoader)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PokeNET.Scripting.Abstractions;

/// <summary>
/// Core scripting engine interface responsible for the complete script lifecycle.
/// Handles loading, compilation, and execution of scripts with proper isolation and error handling.
/// </summary>
/// <remarks>
/// <para><b>Architectural Design:</b></para>
/// <list type="bullet">
///   <item>Stateless design - each script execution is independent</item>
///   <item>Supports async execution for non-blocking gameplay</item>
///   <item>Provides compilation caching for performance</item>
///   <item>Enables hot-reloading through versioning</item>
/// </list>
/// <para><b>SOLID Alignment:</b></para>
/// <list type="bullet">
///   <item><b>SRP:</b> Only manages script lifecycle, not content or discovery</item>
///   <item><b>OCP:</b> New script languages can be added via new implementations</item>
///   <item><b>ISP:</b> Clients only depend on execution methods they need</item>
/// </list>
/// </remarks>
public interface IScriptingEngine
{
    /// <summary>
    /// Gets the name of the scripting engine (e.g., "CSharpScript", "Roslyn").
    /// Used for logging, diagnostics, and multi-engine coordination.
    /// </summary>
    string EngineName { get; }

    /// <summary>
    /// Gets the version of the scripting engine implementation.
    /// Follows semantic versioning (major.minor.patch).
    /// </summary>
    Version EngineVersion { get; }

    /// <summary>
    /// Gets a value indicating whether the engine supports hot-reloading of scripts
    /// without restarting the application.
    /// </summary>
    bool SupportsHotReload { get; }

    /// <summary>
    /// Loads and compiles a script from source code.
    /// </summary>
    /// <param name="scriptId">Unique identifier for the script (used for caching and error reporting).</param>
    /// <param name="sourceCode">The complete source code of the script.</param>
    /// <param name="options">Optional compilation options (optimization level, debug symbols, etc.).</param>
    /// <param name="cancellationToken">Cancellation token to abort long-running compilations.</param>
    /// <returns>A compiled script ready for execution, or throws if compilation fails.</returns>
    /// <exception cref="ScriptCompilationException">Thrown when the script contains syntax or semantic errors.</exception>
    /// <exception cref="ArgumentNullException">Thrown when scriptId or sourceCode is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <remarks>
    /// <para>Compilation results may be cached internally for performance.</para>
    /// <para>The returned <see cref="ICompiledScript"/> is thread-safe and reusable.</para>
    /// </remarks>
    Task<ICompiledScript> CompileAsync(
        string scriptId,
        string sourceCode,
        ScriptCompilationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a script from a file path using the configured script loader.
    /// </summary>
    /// <param name="scriptPath">Relative or absolute path to the script file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A compiled script ready for execution.</returns>
    /// <exception cref="ScriptLoadException">Thrown when the file cannot be found or read.</exception>
    /// <exception cref="ScriptCompilationException">Thrown when compilation fails.</exception>
    Task<ICompiledScript> LoadScriptAsync(
        string scriptPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a compiled script within the provided context.
    /// </summary>
    /// <param name="script">The compiled script to execute.</param>
    /// <param name="context">Execution context providing API access and dependency injection.</param>
    /// <param name="cancellationToken">Cancellation token to abort long-running scripts.</param>
    /// <returns>A result object containing return values, execution time, and success status.</returns>
    /// <exception cref="ScriptExecutionException">Thrown when runtime errors occur during execution.</exception>
    /// <exception cref="ArgumentNullException">Thrown when script or context is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when execution is cancelled.</exception>
    /// <remarks>
    /// <para>Scripts execute in isolation - they cannot directly access each other's state.</para>
    /// <para>Execution is sandboxed with resource limits (memory, CPU time).</para>
    /// <para>All ECS world access must go through the provided <see cref="IScriptContext"/>.</para>
    /// </remarks>
    Task<ScriptExecutionResult> ExecuteAsync(
        ICompiledScript script,
        IScriptContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a script function/method by name with arguments.
    /// Useful for calling specific event handlers or callbacks.
    /// </summary>
    /// <param name="script">The compiled script containing the function.</param>
    /// <param name="functionName">Name of the function to invoke.</param>
    /// <param name="context">Execution context.</param>
    /// <param name="arguments">Arguments to pass to the function.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution result with function return value.</returns>
    /// <exception cref="ScriptExecutionException">Thrown when the function doesn't exist or throws an error.</exception>
    Task<ScriptExecutionResult> ExecuteFunctionAsync(
        ICompiledScript script,
        string functionName,
        IScriptContext context,
        object?[]? arguments = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the compilation cache, forcing scripts to be recompiled on next load.
    /// Useful for hot-reloading during development.
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Removes a specific script from the compilation cache.
    /// </summary>
    /// <param name="scriptId">Identifier of the script to invalidate.</param>
    /// <returns>True if the script was found and removed; otherwise false.</returns>
    bool InvalidateCache(string scriptId);

    /// <summary>
    /// Gets diagnostic information about the engine's current state.
    /// Includes cache statistics, memory usage, and execution metrics.
    /// </summary>
    /// <returns>Diagnostic information for monitoring and debugging.</returns>
    ScriptEngineDiagnostics GetDiagnostics();
}

/// <summary>
/// Options for script compilation.
/// </summary>
public sealed class ScriptCompilationOptions
{
    /// <summary>
    /// Gets or sets the optimization level (0 = none, 1 = basic, 2 = full).
    /// Higher levels improve runtime performance but increase compilation time.
    /// </summary>
    public int OptimizationLevel { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether to include debug symbols for better error messages.
    /// Should be enabled in development, disabled in production.
    /// </summary>
    public bool IncludeDebugSymbols { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum allowed compilation time in milliseconds.
    /// Prevents malicious scripts from causing indefinite compilation.
    /// </summary>
    public int CompilationTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets additional metadata to attach to the compiled script.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Result of script execution containing return values and diagnostics.
/// </summary>
public sealed class ScriptExecutionResult
{
    /// <summary>
    /// Gets or sets whether the script executed successfully without errors.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the return value from the script (if any).
    /// Null if the script returns void or throws an exception.
    /// </summary>
    public object? ReturnValue { get; set; }

    /// <summary>
    /// Gets or sets the exception that occurred during execution (if any).
    /// Null if execution was successful.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets or sets the total execution time.
    /// Useful for performance profiling and timeout enforcement.
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets any output/log messages produced by the script.
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Gets or sets custom diagnostic data (memory usage, API calls made, etc.).
    /// </summary>
    public Dictionary<string, object>? Diagnostics { get; set; }
}

/// <summary>
/// Diagnostic information about the scripting engine's state.
/// </summary>
public sealed class ScriptEngineDiagnostics
{
    /// <summary>
    /// Gets or sets the number of scripts currently in the compilation cache.
    /// </summary>
    public int CachedScriptCount { get; set; }

    /// <summary>
    /// Gets or sets the total memory used by cached scripts in bytes.
    /// </summary>
    public long CacheMemoryBytes { get; set; }

    /// <summary>
    /// Gets or sets the total number of script executions since engine creation.
    /// </summary>
    public long TotalExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of currently executing scripts.
    /// </summary>
    public int ActiveExecutions { get; set; }

    /// <summary>
    /// Gets or sets the average execution time across all runs.
    /// </summary>
    public TimeSpan AverageExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets additional engine-specific metrics.
    /// </summary>
    public Dictionary<string, object>? AdditionalMetrics { get; set; }
}
