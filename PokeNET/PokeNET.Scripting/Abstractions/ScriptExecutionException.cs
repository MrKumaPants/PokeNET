// SOLID Principles Applied:
// - Single Responsibility: Exception only represents runtime script errors
// - Open/Closed: Extensible through inheritance for specific error types

using System;
using System.Collections.Generic;

namespace PokeNET.Scripting.Abstractions;

/// <summary>
/// Exception thrown when a script encounters a runtime error during execution.
/// Includes detailed context for debugging and error reporting.
/// </summary>
/// <remarks>
/// <para><b>Error Context:</b></para>
/// <list type="bullet">
///   <item>Script ID - identifies which script failed</item>
///   <item>Stack trace - shows the call stack within the script</item>
///   <item>Execution context - snapshot of variables and state</item>
///   <item>Line/column information - pinpoints the error location</item>
/// </list>
/// <para><b>Usage:</b></para>
/// <para>
/// This exception should be caught by the script host to prevent
/// script errors from crashing the entire application. Use the
/// detailed information for logging and user feedback.
/// </para>
/// </remarks>
public class ScriptExecutionException : Exception
{
    /// <summary>
    /// Gets the identifier of the script that threw the exception.
    /// </summary>
    public string? ScriptId { get; }

    /// <summary>
    /// Gets the name of the function that was executing when the error occurred.
    /// </summary>
    public string? FunctionName { get; }

    /// <summary>
    /// Gets the line number in the script where the error occurred (if available).
    /// </summary>
    public int? LineNumber { get; }

    /// <summary>
    /// Gets the column number where the error occurred (if available).
    /// </summary>
    public int? ColumnNumber { get; }

    /// <summary>
    /// Gets the script stack trace (different from .NET stack trace).
    /// Shows the call stack within the script itself.
    /// </summary>
    public string? ScriptStackTrace { get; }

    /// <summary>
    /// Gets the execution context at the time of the error.
    /// May include variable values, scope information, etc.
    /// </summary>
    public IReadOnlyDictionary<string, object>? ExecutionContext { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptExecutionException"/> class.
    /// </summary>
    public ScriptExecutionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptExecutionException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ScriptExecutionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptExecutionException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception from the scripting engine.</param>
    public ScriptExecutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptExecutionException"/> class with detailed error information.
    /// </summary>
    /// <param name="scriptId">The script ID that failed.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception from the scripting engine.</param>
    public ScriptExecutionException(string scriptId, string message, Exception innerException)
        : base(message, innerException)
    {
        ScriptId = scriptId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptExecutionException"/> class with full context.
    /// </summary>
    /// <param name="scriptId">The script ID that failed.</param>
    /// <param name="functionName">The function that was executing.</param>
    /// <param name="message">The error message.</param>
    /// <param name="lineNumber">Line number of the error.</param>
    /// <param name="columnNumber">Column number of the error.</param>
    /// <param name="scriptStackTrace">The script's stack trace.</param>
    /// <param name="innerException">The inner exception.</param>
    public ScriptExecutionException(
        string scriptId,
        string? functionName,
        string message,
        int? lineNumber = null,
        int? columnNumber = null,
        string? scriptStackTrace = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ScriptId = scriptId;
        FunctionName = functionName;
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
        ScriptStackTrace = scriptStackTrace;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptExecutionException"/> class with execution context.
    /// </summary>
    /// <param name="scriptId">The script ID that failed.</param>
    /// <param name="functionName">The function that was executing.</param>
    /// <param name="message">The error message.</param>
    /// <param name="executionContext">Snapshot of the execution context.</param>
    /// <param name="lineNumber">Line number of the error.</param>
    /// <param name="columnNumber">Column number of the error.</param>
    /// <param name="scriptStackTrace">The script's stack trace.</param>
    /// <param name="innerException">The inner exception.</param>
    public ScriptExecutionException(
        string scriptId,
        string? functionName,
        string message,
        IReadOnlyDictionary<string, object>? executionContext,
        int? lineNumber = null,
        int? columnNumber = null,
        string? scriptStackTrace = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ScriptId = scriptId;
        FunctionName = functionName;
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
        ScriptStackTrace = scriptStackTrace;
        ExecutionContext = executionContext;
    }

    /// <summary>
    /// Creates a formatted error message including all context information.
    /// </summary>
    /// <returns>A detailed error message for logging.</returns>
    public string GetDetailedMessage()
    {
        var parts = new List<string>();

        parts.Add($"Script execution error: {Message}");

        if (!string.IsNullOrEmpty(ScriptId))
            parts.Add($"Script: {ScriptId}");

        if (!string.IsNullOrEmpty(FunctionName))
            parts.Add($"Function: {FunctionName}");

        if (LineNumber.HasValue)
        {
            var location = $"Line: {LineNumber.Value}";
            if (ColumnNumber.HasValue)
                location += $", Column: {ColumnNumber.Value}";
            parts.Add(location);
        }

        if (!string.IsNullOrEmpty(ScriptStackTrace))
        {
            parts.Add("Script Stack Trace:");
            parts.Add(ScriptStackTrace);
        }

        if (ExecutionContext != null && ExecutionContext.Count > 0)
        {
            parts.Add("Execution Context:");
            foreach (var kvp in ExecutionContext)
            {
                parts.Add($"  {kvp.Key} = {kvp.Value}");
            }
        }

        if (InnerException != null)
        {
            parts.Add($"Inner Exception: {InnerException.GetType().Name}");
            parts.Add($"  {InnerException.Message}");
        }

        return string.Join(Environment.NewLine, parts);
    }

    /// <summary>
    /// Returns a string representation of this exception.
    /// </summary>
    /// <returns>A detailed error message.</returns>
    public override string ToString()
    {
        return GetDetailedMessage();
    }
}

/// <summary>
/// Exception thrown when a script exceeds its execution time limit.
/// </summary>
public class ScriptTimeoutException : ScriptExecutionException
{
    /// <summary>
    /// Gets the timeout limit that was exceeded.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Gets the actual execution time before timeout.
    /// </summary>
    public TimeSpan ExecutionTime { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptTimeoutException"/> class.
    /// </summary>
    /// <param name="scriptId">The script ID that timed out.</param>
    /// <param name="timeout">The timeout limit.</param>
    /// <param name="executionTime">The actual execution time.</param>
    public ScriptTimeoutException(string scriptId, TimeSpan timeout, TimeSpan executionTime)
        : base(scriptId, $"Script execution exceeded timeout of {timeout.TotalMilliseconds}ms (actual: {executionTime.TotalMilliseconds}ms)", null!)
    {
        Timeout = timeout;
        ExecutionTime = executionTime;
    }
}

/// <summary>
/// Exception thrown when a script violates security constraints.
/// Examples: accessing forbidden APIs, resource limit violations, etc.
/// </summary>
public class ScriptSecurityException : ScriptExecutionException
{
    /// <summary>
    /// Gets the security violation type.
    /// </summary>
    public ScriptSecurityViolation ViolationType { get; }

    /// <summary>
    /// Gets details about what was attempted.
    /// </summary>
    public string? ViolationDetails { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptSecurityException"/> class.
    /// </summary>
    /// <param name="scriptId">The script ID that violated security.</param>
    /// <param name="violationType">The type of violation.</param>
    /// <param name="message">The error message.</param>
    /// <param name="violationDetails">Details about the violation.</param>
    public ScriptSecurityException(
        string scriptId,
        ScriptSecurityViolation violationType,
        string message,
        string? violationDetails = null)
        : base(scriptId, message, null!)
    {
        ViolationType = violationType;
        ViolationDetails = violationDetails;
    }
}

/// <summary>
/// Types of security violations that can occur in scripts.
/// </summary>
public enum ScriptSecurityViolation
{
    /// <summary>
    /// Script attempted to access a forbidden API.
    /// </summary>
    ForbiddenApiAccess,

    /// <summary>
    /// Script exceeded memory allocation limits.
    /// </summary>
    MemoryLimitExceeded,

    /// <summary>
    /// Script attempted to access restricted file system paths.
    /// </summary>
    UnauthorizedFileAccess,

    /// <summary>
    /// Script attempted to access restricted network resources.
    /// </summary>
    UnauthorizedNetworkAccess,

    /// <summary>
    /// Script attempted to load or execute external code.
    /// </summary>
    CodeInjectionAttempt,

    /// <summary>
    /// Other security violation.
    /// </summary>
    Other
}
