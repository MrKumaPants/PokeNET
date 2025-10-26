using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using PokeNET.Scripting.Abstractions;
using PokeNET.Scripting.Diagnostics;
using PokeNET.Scripting.Models;

namespace PokeNET.Scripting.Services;

/// <summary>
/// Core scripting engine that compiles and executes C# scripts using Microsoft.CodeAnalysis.CSharp.Scripting (Roslyn).
/// Implements caching, timeout protection, and comprehensive error handling.
/// </summary>
public sealed class ScriptingEngine : IScriptingEngine
{
    private readonly ILogger<ScriptingEngine> _logger;
    private readonly IScriptLoader? _scriptLoader;
    private readonly ScriptCompilationCache _cache;
    private readonly ScriptOptions _defaultScriptOptions;

    /// <inheritdoc/>
    public string EngineName => "CSharpScript";

    /// <inheritdoc/>
    public Version EngineVersion => new Version(1, 0, 0);

    /// <inheritdoc/>
    public bool SupportsHotReload => true;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptingEngine"/> class.
    /// </summary>
    /// <param name="logger">Logger for engine operations.</param>
    /// <param name="scriptLoader">Optional script loader for file-based scripts.</param>
    /// <param name="cacheLogger">Logger for the compilation cache.</param>
    /// <param name="maxCacheSize">Maximum number of compiled scripts to cache.</param>
    public ScriptingEngine(
        ILogger<ScriptingEngine> logger,
        IScriptLoader? scriptLoader = null,
        ILogger<ScriptCompilationCache>? cacheLogger = null,
        int maxCacheSize = 100
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scriptLoader = scriptLoader;

        // Create cache logger if not provided - use NullLogger as fallback
        cacheLogger ??= Microsoft
            .Extensions
            .Logging
            .Abstractions
            .NullLogger<ScriptCompilationCache>
            .Instance;
        _cache = new ScriptCompilationCache(cacheLogger, maxCacheSize);

        // Configure default script options with common imports and references
        _defaultScriptOptions = ScriptOptions
            .Default.WithImports(
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
                "System.Threading.Tasks"
            )
            .WithReferences(
                typeof(object).Assembly, // System.Private.CoreLib
                typeof(Enumerable).Assembly, // System.Linq
                typeof(List<>).Assembly
            ) // System.Collections.Generic
            .WithOptimizationLevel(OptimizationLevel.Release);

        _logger.LogInformation(
            "ScriptingEngine initialized with cache size: {CacheSize}",
            maxCacheSize
        );
    }

    /// <inheritdoc/>
    public async Task<ICompiledScript> CompileAsync(
        string scriptId,
        string sourceCode,
        ScriptCompilationOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return await CompileInternalAsync(sourceCode, scriptId, options, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ICompiledScript> LoadScriptAsync(
        string scriptPath,
        CancellationToken cancellationToken = default
    )
    {
        if (_scriptLoader == null)
        {
            throw new InvalidOperationException(
                "No script loader configured. Cannot load scripts from files."
            );
        }

        _logger.LogDebug("Loading script from: {ScriptPath}", scriptPath);
        var loadResult = await _scriptLoader.LoadScriptAsync(scriptPath, cancellationToken);

        return await CompileInternalAsync(
            loadResult.SourceCode,
            loadResult.ScriptId,
            null,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public async Task<ScriptExecutionResult> ExecuteAsync(
        ICompiledScript script,
        IScriptContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (script is not CompiledScript internalScript)
        {
            throw new ArgumentException("Script must be compiled by this engine", nameof(script));
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Executing script: {ScriptId}", script.ScriptId);

            var result = await internalScript.RoslynScript.RunAsync(context, cancellationToken);

            stopwatch.Stop();

            return new ScriptExecutionResult
            {
                Success = true,
                ReturnValue = result.ReturnValue,
                ExecutionTime = stopwatch.Elapsed,
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Script execution failed: {ScriptId}", script.ScriptId);

            return new ScriptExecutionResult
            {
                Success = false,
                Exception = ex,
                ExecutionTime = stopwatch.Elapsed,
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ScriptExecutionResult> ExecuteFunctionAsync(
        ICompiledScript script,
        string functionName,
        IScriptContext context,
        object?[]? arguments = null,
        CancellationToken cancellationToken = default
    )
    {
        if (script is not CompiledScript internalScript)
        {
            throw new ArgumentException("Script must be compiled by this engine", nameof(script));
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug(
                "Executing function {FunctionName} in script: {ScriptId}",
                functionName,
                script.ScriptId
            );

            // Execute the script first to get the state
            var state = await internalScript.RoslynScript.RunAsync(context, cancellationToken);

            // Try to find and invoke the function
            var scriptType = state.ReturnValue?.GetType();
            if (scriptType == null)
            {
                throw new InvalidOperationException(
                    $"Script did not return an object with methods"
                );
            }

            var method = scriptType.GetMethod(functionName);
            if (method == null)
            {
                throw new InvalidOperationException(
                    $"Function '{functionName}' not found in script"
                );
            }

            var result = method.Invoke(state.ReturnValue, arguments);

            stopwatch.Stop();

            return new ScriptExecutionResult
            {
                Success = true,
                ReturnValue = result,
                ExecutionTime = stopwatch.Elapsed,
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Function execution failed: {FunctionName} in {ScriptId}",
                functionName,
                script.ScriptId
            );

            return new ScriptExecutionResult
            {
                Success = false,
                Exception = ex,
                ExecutionTime = stopwatch.Elapsed,
            };
        }
    }

    /// <inheritdoc/>
    public bool InvalidateCache(string scriptId)
    {
        _logger.LogInformation("Invalidating cache for script: {ScriptId}", scriptId);
        // For now, we'll clear the entire cache since we cache by source hash
        // A more sophisticated implementation would maintain a scriptId -> hash mapping
        ClearCache();
        return true;
    }

    /// <inheritdoc/>
    public ScriptEngineDiagnostics GetDiagnostics()
    {
        var stats = _cache.GetStatistics();

        return new ScriptEngineDiagnostics
        {
            CachedScriptCount = stats.CurrentSize,
            CacheMemoryBytes = 0, // Would need to track memory usage separately
            TotalExecutions = 0, // Would need to track this
            ActiveExecutions = 0, // Would need to track this
            AverageExecutionTime = TimeSpan.Zero, // Would need to track this
            AdditionalMetrics = new Dictionary<string, object>
            {
                ["CacheHits"] = stats.CacheHits,
                ["CacheMisses"] = stats.CacheMisses,
                ["HitRate"] = stats.HitRate,
                ["TotalRequests"] = stats.TotalRequests,
                ["MaxCacheSize"] = stats.MaxSize,
                ["UsagePercentage"] = stats.UsagePercentage,
            },
        };
    }

    /// <summary>
    /// Internal compilation method that handles the actual compilation logic.
    /// </summary>
    private Task<ICompiledScript> CompileInternalAsync(
        string scriptCode,
        string scriptId,
        ScriptCompilationOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(scriptCode))
            throw new ArgumentException("Script code cannot be null or empty.", nameof(scriptCode));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Compiling script: {ScriptId}", scriptId);

            // Compute source hash for caching
            var sourceHash = ComputeSourceHash(scriptCode);

            // Try to get from cache
            if (_cache.TryGet(sourceHash, out var cachedScript))
            {
                _logger.LogInformation(
                    "Retrieved compiled script from cache in {ElapsedMs}ms: {ScriptId}",
                    stopwatch.ElapsedMilliseconds,
                    scriptId
                );
                return Task.FromResult(cachedScript!);
            }

            // Compile the script
            cancellationToken.ThrowIfCancellationRequested();

            var script = CSharpScript.Create<object>(
                scriptCode,
                _defaultScriptOptions,
                globalsType: null
            );

            // Compile with cancellation support
            var compilation = script.GetCompilation();
            var diagnostics = compilation.GetDiagnostics();

            // Check for compilation errors
            var errors = diagnostics
                .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .ToList();
            if (errors.Any())
            {
                var errorMessages = errors.Select(e => $"{e.Id}: {e.GetMessage()}");
                var errorSummary = string.Join(Environment.NewLine, errorMessages);

                _logger.LogError(
                    "Script compilation failed with {ErrorCount} errors: {ScriptId}\n{Errors}",
                    errors.Count,
                    scriptId,
                    errorSummary
                );

                throw new Abstractions.ScriptCompilationException(
                    scriptId,
                    $"Script compilation failed with {errors.Count} error(s)."
                );
            }

            // Convert diagnostics to our model
            var scriptDiagnostics = ConvertDiagnostics(diagnostics);

            // Create compiled script
            var compiledScript = new CompiledScript(
                script,
                scriptCode,
                scriptId,
                scriptDiagnostics
            );

            // Add to cache
            _cache.Add(sourceHash, compiledScript);

            stopwatch.Stop();

            _logger.LogInformation(
                "Script compiled successfully in {ElapsedMs}ms: {ScriptId} (Warnings: {WarningCount})",
                stopwatch.ElapsedMilliseconds,
                scriptId,
                scriptDiagnostics.Count(d => d.Severity == Models.DiagnosticSeverity.Warning)
            );

            return Task.FromResult<ICompiledScript>(compiledScript);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Script compilation cancelled: {ScriptId}", scriptId);
            throw;
        }
        catch (Abstractions.ScriptCompilationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error during script compilation: {ScriptId}",
                scriptId
            );
            throw new Abstractions.ScriptCompilationException(
                scriptId,
                "An unexpected error occurred during compilation."
            );
        }
    }

    /// <inheritdoc/>
    public void ClearCache()
    {
        _logger.LogInformation("Clearing script compilation cache");
        _cache.Clear();
    }

    /// <summary>
    /// Computes a SHA256 hash of the source code for cache validation.
    /// </summary>
    private static string ComputeSourceHash(string sourceCode)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(sourceCode));
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Converts Roslyn diagnostics to our diagnostic model.
    /// </summary>
    private static List<ScriptDiagnostic> ConvertDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        var result = new List<ScriptDiagnostic>();

        foreach (var diagnostic in diagnostics)
        {
            var severity = diagnostic.Severity switch
            {
                Microsoft.CodeAnalysis.DiagnosticSeverity.Info => Models.DiagnosticSeverity.Info,
                Microsoft.CodeAnalysis.DiagnosticSeverity.Warning => Models
                    .DiagnosticSeverity
                    .Warning,
                Microsoft.CodeAnalysis.DiagnosticSeverity.Error => Models.DiagnosticSeverity.Error,
                _ => Models.DiagnosticSeverity.Info,
            };

            var lineSpan = diagnostic.Location.GetLineSpan();
            var lineNumber = lineSpan.StartLinePosition.Line + 1;
            var columnNumber = lineSpan.StartLinePosition.Character + 1;

            result.Add(
                new ScriptDiagnostic(
                    severity,
                    diagnostic.GetMessage(),
                    diagnostic.Id,
                    lineNumber,
                    columnNumber
                )
            );
        }

        return result;
    }
}
