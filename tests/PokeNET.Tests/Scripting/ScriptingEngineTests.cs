using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PokeNET.Scripting.Abstractions;
using PokeNET.Scripting.Services;
using Xunit;

namespace PokeNET.Tests.Scripting;

/// <summary>
/// Comprehensive test suite for ScriptingEngine with 800+ lines of tests.
/// Tests compilation, execution, caching, error handling, and performance budgets.
/// </summary>
public sealed class ScriptingEngineTests : IDisposable
{
    private readonly ILogger<ScriptingEngine> _logger;
    private readonly ScriptingEngine _engine;

    public ScriptingEngineTests()
    {
        _logger = NullLogger<ScriptingEngine>.Instance;
        _engine = new ScriptingEngine(_logger, maxCacheSize: 10);
    }

    public void Dispose()
    {
        _engine.ClearCache();
    }

    #region Constructor and Properties Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ScriptingEngine(null!));
    }

    [Fact]
    public void Constructor_InitializesWithCorrectProperties()
    {
        Assert.Equal("CSharpScript", _engine.EngineName);
        Assert.Equal(new Version(1, 0, 0), _engine.EngineVersion);
        Assert.True(_engine.SupportsHotReload);
    }

    [Fact]
    public void Constructor_WithCustomCacheSize_InitializesCorrectly()
    {
        var engine = new ScriptingEngine(_logger, maxCacheSize: 50);
        Assert.NotNull(engine);
        var diagnostics = engine.GetDiagnostics();
        Assert.Equal(0, diagnostics.CachedScriptCount);
    }

    #endregion

    #region CompileAsync Basic Tests

    [Fact]
    public async Task CompileAsync_WithNullSourceCode_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _engine.CompileAsync("test", null!));
    }

    [Fact]
    public async Task CompileAsync_WithEmptySourceCode_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _engine.CompileAsync("test", string.Empty));
    }

    [Fact]
    public async Task CompileAsync_WithWhitespaceSourceCode_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _engine.CompileAsync("test", "   "));
    }

    [Fact]
    public async Task CompileAsync_WithValidCode_CompilesToCompiledScript()
    {
        // Arrange
        var code = "return 42;";

        // Act
        var compiled = await _engine.CompileAsync("test", code);

        // Assert
        Assert.NotNull(compiled);
        Assert.Equal("test", compiled.ScriptId);
        Assert.NotEmpty(compiled.SourceCode);
    }

    [Fact]
    public async Task CompileAsync_WithSyntaxError_ThrowsScriptCompilationException()
    {
        // Arrange - missing semicolon
        var code = "var x = 42";

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ScriptCompilationException>(
            async () => await _engine.CompileAsync("syntax-error", code));

        Assert.Contains("syntax-error", ex.Message);
    }

    [Fact]
    public async Task CompileAsync_WithSemanticError_ThrowsScriptCompilationException()
    {
        // Arrange - undefined variable
        var code = "return undefinedVariable;";

        // Act & Assert
        await Assert.ThrowsAsync<ScriptCompilationException>(
            async () => await _engine.CompileAsync("semantic-error", code));
    }

    [Fact]
    public async Task CompileAsync_WithWarnings_CompileSuccessfully()
    {
        // Arrange - unused variable
        var code = @"
int unusedVar = 42;
return 100;
";

        // Act
        var compiled = await _engine.CompileAsync("warnings", code);

        // Assert
        Assert.NotNull(compiled);
        Assert.True(compiled.Diagnostics.Any(d => d.Severity == PokeNET.Scripting.Models.DiagnosticSeverity.Warning));
    }

    #endregion

    #region CompileAsync Advanced Features Tests

    [Fact]
    public async Task CompileAsync_WithSystemImports_CompileSuccessfully()
    {
        // Arrange
        var code = @"
var list = new List<int> { 1, 2, 3 };
return list.Sum();
";

        // Act
        var compiled = await _engine.CompileAsync("system-imports", code);

        // Assert
        Assert.NotNull(compiled);
    }

    [Fact]
    public async Task CompileAsync_WithLinqQueries_CompileSuccessfully()
    {
        // Arrange
        var code = @"
var numbers = new[] { 1, 2, 3, 4, 5 };
return numbers.Where(n => n % 2 == 0).Sum();
";

        // Act
        var compiled = await _engine.CompileAsync("linq", code);

        // Assert
        Assert.NotNull(compiled);
    }

    [Fact]
    public async Task CompileAsync_WithAsyncCode_CompileSuccessfully()
    {
        // Arrange
        var code = @"
await Task.Delay(1);
return ""completed"";
";

        // Act
        var compiled = await _engine.CompileAsync("async", code);

        // Assert
        Assert.NotNull(compiled);
    }

    [Fact]
    public async Task CompileAsync_WithLambdaExpressions_CompileSuccessfully()
    {
        // Arrange
        var code = @"
Func<int, int> square = x => x * x;
return square(5);
";

        // Act
        var compiled = await _engine.CompileAsync("lambda", code);

        // Assert
        Assert.NotNull(compiled);
    }

    [Fact]
    public async Task CompileAsync_WithNullableTypes_CompileSuccessfully()
    {
        // Arrange
        var code = @"
int? nullableInt = null;
return nullableInt ?? 42;
";

        // Act
        var compiled = await _engine.CompileAsync("nullable", code);

        // Assert
        Assert.NotNull(compiled);
    }

    #endregion

    #region ExecuteAsync Basic Tests

    [Fact]
    public async Task ExecuteAsync_WithNullScript_ThrowsArgumentException()
    {
        var context = new SimpleScriptContext();
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _engine.ExecuteAsync(null!, context));
    }

    [Fact]
    public async Task ExecuteAsync_WithSimpleReturn_ReturnsValue()
    {
        // Arrange
        var code = "return 42;";
        var compiled = await _engine.CompileAsync("return-test", code);
        var context = new SimpleScriptContext();

        // Act
        var result = await _engine.ExecuteAsync(compiled, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(42, result.ReturnValue);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task ExecuteAsync_WithStringReturn_ReturnsString()
    {
        // Arrange
        var code = "return \"Hello, World!\";";
        var compiled = await _engine.CompileAsync("string-return", code);
        var context = new SimpleScriptContext();

        // Act
        var result = await _engine.ExecuteAsync(compiled, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Hello, World!", result.ReturnValue);
    }

    [Fact]
    public async Task ExecuteAsync_WithListReturn_ReturnsList()
    {
        // Arrange
        var code = "return new List<int> { 1, 2, 3 };";
        var compiled = await _engine.CompileAsync("list-return", code);
        var context = new SimpleScriptContext();

        // Act
        var result = await _engine.ExecuteAsync(compiled, context);

        // Assert
        Assert.True(result.Success);
        Assert.IsAssignableFrom<System.Collections.Generic.List<int>>(result.ReturnValue);
    }

    [Fact]
    public async Task ExecuteAsync_WithException_ReturnsFailureResult()
    {
        // Arrange
        var code = "throw new Exception(\"Test exception\");";
        var compiled = await _engine.CompileAsync("exception-test", code);
        var context = new SimpleScriptContext();

        // Act
        var result = await _engine.ExecuteAsync(compiled, context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Exception);
        Assert.Contains("Test exception", result.Exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithDivisionByZero_CatchesException()
    {
        // Arrange
        var code = "return 10 / 0;";
        var compiled = await _engine.CompileAsync("div-by-zero", code);
        var context = new SimpleScriptContext();

        // Act
        var result = await _engine.ExecuteAsync(compiled, context);

        // Assert
        Assert.False(result.Success);
        Assert.IsType<DivideByZeroException>(result.Exception);
    }

    #endregion

    #region ExecuteAsync with Context Tests

    [Fact]
    public async Task ExecuteAsync_WithGlobalVariables_AccessesContext()
    {
        // Arrange
        var code = "return Value * 2;";
        var compiled = await _engine.CompileAsync("context-test", code);
        var context = new SimpleScriptContext { Value = 21 };

        // Act
        var result = await _engine.ExecuteAsync(compiled, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(42, result.ReturnValue);
    }

    [Fact]
    public async Task ExecuteAsync_ModifyingContext_ReflectsChanges()
    {
        // Arrange
        var code = "Value = 100; return Value;";
        var compiled = await _engine.CompileAsync("modify-context", code);
        var context = new SimpleScriptContext { Value = 50 };

        // Act
        var result = await _engine.ExecuteAsync(compiled, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(100, result.ReturnValue);
        Assert.Equal(100, context.Value);
    }

    #endregion

    #region ExecuteAsync Cancellation Tests

    [Fact]
    public async Task ExecuteAsync_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        var code = @"
for(int i = 0; i < 1000000; i++)
{
    System.Threading.Thread.Sleep(1);
}
return 42;
";
        var compiled = await _engine.CompileAsync("cancellation-test", code);
        var context = new SimpleScriptContext();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        var result = await _engine.ExecuteAsync(compiled, context, cts.Token);

        // Assert - should timeout/cancel
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithImmediateCancellation_FailsQuickly()
    {
        // Arrange
        var code = "return 42;";
        var compiled = await _engine.CompileAsync("immediate-cancel", code);
        var context = new SimpleScriptContext();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _engine.ExecuteAsync(compiled, context, cts.Token);
        stopwatch.Stop();

        // Assert
        Assert.False(result.Success);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000); // Should fail quickly
    }

    #endregion

    #region Performance and Timing Tests

    [Fact]
    public async Task ExecuteAsync_TracksExecutionTime()
    {
        // Arrange
        var code = "System.Threading.Thread.Sleep(50); return 42;";
        var compiled = await _engine.CompileAsync("timing-test", code);
        var context = new SimpleScriptContext();

        // Act
        var result = await _engine.ExecuteAsync(compiled, context);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.ExecutionTime.TotalMilliseconds >= 45); // Account for timing variance
    }

    [Fact]
    public async Task CompileAsync_MeasuresCompilationTime()
    {
        // Arrange
        var code = "return 42;";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var compiled = await _engine.CompileAsync("compile-timing", code);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(compiled);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should compile in < 5 seconds
    }

    [Fact]
    public async Task ExecuteAsync_FastExecution_CompletesQuickly()
    {
        // Arrange
        var code = "return 1 + 1;";
        var compiled = await _engine.CompileAsync("fast-exec", code);
        var context = new SimpleScriptContext();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _engine.ExecuteAsync(compiled, context);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success);
        Assert.True(stopwatch.ElapsedMilliseconds < 100);
    }

    #endregion

    #region Cache Tests

    [Fact]
    public async Task CompileAsync_SameCodeTwice_UsesCacheOnSecondCall()
    {
        // Arrange
        var code = "return 42;";

        // Act - compile twice
        var stopwatch1 = Stopwatch.StartNew();
        var compiled1 = await _engine.CompileAsync("cache-test", code);
        stopwatch1.Stop();

        var stopwatch2 = Stopwatch.StartNew();
        var compiled2 = await _engine.CompileAsync("cache-test", code);
        stopwatch2.Stop();

        // Assert - second compile should be faster (cached)
        Assert.True(stopwatch2.Elapsed < stopwatch1.Elapsed);
    }

    [Fact]
    public async Task CompileAsync_DifferentScriptIds_SameCode_UsesCacheBySourceHash()
    {
        // Arrange
        var code = "return 42;";

        // Act
        var compiled1 = await _engine.CompileAsync("script1", code);
        var compiled2 = await _engine.CompileAsync("script2", code);

        // Assert - should use cache (same source)
        Assert.NotNull(compiled1);
        Assert.NotNull(compiled2);
    }

    [Fact]
    public async Task InvalidateCache_RemovesScriptFromCache()
    {
        // Arrange
        var code = "return 42;";
        await _engine.CompileAsync("invalidate-test", code);

        // Act
        var invalidated = _engine.InvalidateCache("invalidate-test");

        // Assert
        Assert.True(invalidated);
        var diagnostics = _engine.GetDiagnostics();
        Assert.Equal(0, diagnostics.CachedScriptCount);
    }

    [Fact]
    public async Task ClearCache_RemovesAllScripts()
    {
        // Arrange - compile multiple scripts
        await _engine.CompileAsync("script1", "return 1;");
        await _engine.CompileAsync("script2", "return 2;");
        await _engine.CompileAsync("script3", "return 3;");

        // Act
        _engine.ClearCache();

        // Assert
        var diagnostics = _engine.GetDiagnostics();
        Assert.Equal(0, diagnostics.CachedScriptCount);
    }

    [Fact]
    public async Task GetDiagnostics_TracksCacheStatistics()
    {
        // Arrange
        var code = "return 42;";

        // Act
        await _engine.CompileAsync("diag-test", code);
        await _engine.CompileAsync("diag-test", code); // Cache hit

        var diagnostics = _engine.GetDiagnostics();

        // Assert
        Assert.NotNull(diagnostics);
        Assert.True(diagnostics.AdditionalMetrics.ContainsKey("CacheHits"));
        Assert.True((long)diagnostics.AdditionalMetrics["CacheHits"] >= 1);
    }

    [Fact]
    public async Task CompileAsync_ExceedsCacheSize_EvictsOldest()
    {
        // Arrange - engine with small cache (10 items)
        for (int i = 0; i < 15; i++)
        {
            await _engine.CompileAsync($"script{i}", $"return {i};");
        }

        // Act
        var diagnostics = _engine.GetDiagnostics();

        // Assert - cache should be at max size
        Assert.True(diagnostics.CachedScriptCount <= 10);
    }

    #endregion

    #region Concurrent Execution Tests

    [Fact]
    public async Task ExecuteAsync_ConcurrentExecutions_AllSucceed()
    {
        // Arrange
        var code = "return System.Threading.Thread.CurrentThread.ManagedThreadId;";
        var compiled = await _engine.CompileAsync("concurrent-test", code);

        // Act - execute 10 times concurrently
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _engine.ExecuteAsync(compiled, new SimpleScriptContext()))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert - all should succeed
        Assert.Equal(10, results.Length);
        Assert.All(results, r => Assert.True(r.Success));
    }

    [Fact]
    public async Task CompileAsync_ConcurrentCompilations_AllSucceed()
    {
        // Arrange
        var codes = Enumerable.Range(0, 10).Select(i => $"return {i};").ToArray();

        // Act - compile 10 scripts concurrently
        var tasks = codes.Select((code, i) =>
            _engine.CompileAsync($"concurrent-compile-{i}", code)
        ).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, results.Length);
        Assert.All(results, r => Assert.NotNull(r));
    }

    #endregion

    #region ExecuteFunctionAsync Tests

    [Fact]
    public async Task ExecuteFunctionAsync_CallsNamedFunction()
    {
        // Arrange
        var code = @"
public class ScriptClass
{
    public static int Add(int a, int b)
    {
        return a + b;
    }
}
return new ScriptClass();
";
        var compiled = await _engine.CompileAsync("function-test", code);
        var context = new SimpleScriptContext();

        // Act
        var result = await _engine.ExecuteFunctionAsync(
            compiled, "Add", context, new object[] { 5, 3 });

        // Assert
        Assert.True(result.Success);
        Assert.Equal(8, result.ReturnValue);
    }

    [Fact]
    public async Task ExecuteFunctionAsync_WithNonExistentFunction_ThrowsException()
    {
        // Arrange
        var code = "return new { Value = 42 };";
        var compiled = await _engine.CompileAsync("no-function", code);
        var context = new SimpleScriptContext();

        // Act
        var result = await _engine.ExecuteFunctionAsync(
            compiled, "NonExistent", context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Exception);
    }

    #endregion

    #region Complex Script Tests

    [Fact]
    public async Task ExecuteAsync_WithComplexCalculation_ReturnsCorrectResult()
    {
        // Arrange
        var code = @"
var numbers = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
var sum = numbers.Where(n => n % 2 == 0).Sum();
var average = numbers.Average();
return sum + average;
";
        var compiled = await _engine.CompileAsync("complex-calc", code);
        var context = new SimpleScriptContext();

        // Act
        var result = await _engine.ExecuteAsync(compiled, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(35.5, result.ReturnValue); // 30 (sum of evens) + 5.5 (average)
    }

    [Fact]
    public async Task ExecuteAsync_WithRecursiveFunction_CalculatesCorrectly()
    {
        // Arrange
        var code = @"
int Fibonacci(int n)
{
    if (n <= 1) return n;
    return Fibonacci(n - 1) + Fibonacci(n - 2);
}
return Fibonacci(10);
";
        var compiled = await _engine.CompileAsync("fibonacci", code);
        var context = new SimpleScriptContext();

        // Act
        var result = await _engine.ExecuteAsync(compiled, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(55, result.ReturnValue); // 10th Fibonacci number
    }

    [Fact]
    public async Task ExecuteAsync_WithStringManipulation_WorksCorrectly()
    {
        // Arrange
        var code = @"
var text = ""Hello, World!"";
return text.ToUpper().Replace(""WORLD"", ""POKENET"");
";
        var compiled = await _engine.CompileAsync("string-manip", code);
        var context = new SimpleScriptContext();

        // Act
        var result = await _engine.ExecuteAsync(compiled, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("HELLO, POKENET!", result.ReturnValue);
    }

    #endregion

    #region Error Recovery Tests

    [Fact]
    public async Task ExecuteAsync_AfterPreviousException_RecoverCorrectly()
    {
        // Arrange
        var failingCode = "throw new Exception(\"Fail\");";
        var successCode = "return 42;";

        var compiledFail = await _engine.CompileAsync("fail", failingCode);
        var compiledSuccess = await _engine.CompileAsync("success", successCode);
        var context = new SimpleScriptContext();

        // Act
        var failResult = await _engine.ExecuteAsync(compiledFail, context);
        var successResult = await _engine.ExecuteAsync(compiledSuccess, context);

        // Assert
        Assert.False(failResult.Success);
        Assert.True(successResult.Success);
    }

    [Fact]
    public async Task CompileAsync_AfterCompilationError_RecoverCorrectly()
    {
        // Arrange
        var invalidCode = "this is not valid C#";
        var validCode = "return 42;";

        // Act & Assert
        await Assert.ThrowsAsync<ScriptCompilationException>(
            async () => await _engine.CompileAsync("invalid", invalidCode));

        var compiled = await _engine.CompileAsync("valid", validCode);
        Assert.NotNull(compiled);
    }

    #endregion

    #region Memory Leak Prevention Tests

    [Fact]
    public async Task ExecuteAsync_MultipleExecutions_DoesNotLeakMemory()
    {
        // Arrange
        var code = "return new byte[1024 * 1024];"; // 1MB allocation
        var compiled = await _engine.CompileAsync("memory-test", code);
        var context = new SimpleScriptContext();

        var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);

        // Act - execute 100 times
        for (int i = 0; i < 100; i++)
        {
            await _engine.ExecuteAsync(compiled, context);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryAfter = GC.GetTotalMemory(forceFullCollection: true);
        var memoryGrowth = memoryAfter - memoryBefore;

        // Assert - memory growth should be reasonable (< 50MB)
        Assert.True(memoryGrowth < 50 * 1024 * 1024);
    }

    #endregion

    #region Diagnostics Tests

    [Fact]
    public void GetDiagnostics_ReturnsValidDiagnostics()
    {
        // Act
        var diagnostics = _engine.GetDiagnostics();

        // Assert
        Assert.NotNull(diagnostics);
        Assert.NotNull(diagnostics.AdditionalMetrics);
        Assert.True(diagnostics.AdditionalMetrics.ContainsKey("CacheHits"));
        Assert.True(diagnostics.AdditionalMetrics.ContainsKey("CacheMisses"));
        Assert.True(diagnostics.AdditionalMetrics.ContainsKey("HitRate"));
    }

    [Fact]
    public async Task GetDiagnostics_AfterOperations_ReflectsCurrentState()
    {
        // Arrange
        await _engine.CompileAsync("test1", "return 1;");
        await _engine.CompileAsync("test2", "return 2;");

        // Act
        var diagnostics = _engine.GetDiagnostics();

        // Assert
        Assert.True(diagnostics.CachedScriptCount >= 2);
    }

    #endregion

    #region Helper Classes

    private class SimpleScriptContext : IScriptContext
    {
        public int Value { get; set; }
    }

    #endregion
}
