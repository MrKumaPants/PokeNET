using PokeNET.Scripting.Abstractions;

namespace PokeNET.Scripting.Models;

/// <summary>
/// Represents the result of script execution with return value and diagnostics.
/// </summary>
/// <typeparam name="T">The type of the return value.</typeparam>
public sealed class ScriptExecutionResult<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptExecutionResult{T}"/> class.
    /// </summary>
    /// <param name="success">Whether the execution was successful.</param>
    /// <param name="returnValue">The return value of the script.</param>
    /// <param name="compiledScript">The compiled script that was executed.</param>
    /// <param name="executionTime">The time taken to execute the script.</param>
    /// <param name="exception">The exception that occurred during execution, if any.</param>
    public ScriptExecutionResult(
        bool success,
        T? returnValue,
        ICompiledScript compiledScript,
        TimeSpan executionTime,
        Exception? exception = null)
    {
        Success = success;
        ReturnValue = returnValue;
        CompiledScript = compiledScript ?? throw new ArgumentNullException(nameof(compiledScript));
        ExecutionTime = executionTime;
        Exception = exception;
        ExecutedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets a value indicating whether the script execution was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the return value of the script execution.
    /// </summary>
    public T? ReturnValue { get; }

    /// <summary>
    /// Gets the compiled script that was executed.
    /// </summary>
    public ICompiledScript CompiledScript { get; }

    /// <summary>
    /// Gets the time taken to execute the script.
    /// </summary>
    public TimeSpan ExecutionTime { get; }

    /// <summary>
    /// Gets the exception that occurred during execution, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the timestamp when the script was executed.
    /// </summary>
    public DateTime ExecutedAt { get; }

    /// <summary>
    /// Gets the compilation diagnostics from the compiled script.
    /// </summary>
    public IReadOnlyList<ScriptDiagnostic> Diagnostics => CompiledScript.Diagnostics;

    /// <summary>
    /// Gets a value indicating whether the script has any warnings.
    /// </summary>
    public bool HasWarnings => CompiledScript.HasWarnings;

    /// <summary>
    /// Gets a value indicating whether the execution failed.
    /// </summary>
    public bool Failed => !Success;

    /// <summary>
    /// Creates a successful execution result.
    /// </summary>
    public static ScriptExecutionResult<T> Successful(
        T? returnValue,
        ICompiledScript compiledScript,
        TimeSpan executionTime)
    {
        return new ScriptExecutionResult<T>(true, returnValue, compiledScript, executionTime);
    }

    /// <summary>
    /// Creates a failed execution result.
    /// </summary>
    public static ScriptExecutionResult<T> Failure(
        Exception exception,
        ICompiledScript compiledScript,
        TimeSpan executionTime)
    {
        return new ScriptExecutionResult<T>(false, default, compiledScript, executionTime, exception);
    }
}
