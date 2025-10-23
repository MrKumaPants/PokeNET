namespace PokeNET.Scripting.Diagnostics;

/// <summary>
/// Exception thrown when script execution fails at runtime.
/// </summary>
public sealed class ScriptExecutionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptExecutionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="scriptId">The identifier of the script that failed.</param>
    public ScriptExecutionException(string message, string scriptId)
        : base(message)
    {
        ScriptId = scriptId ?? throw new ArgumentNullException(nameof(scriptId));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptExecutionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="scriptId">The identifier of the script that failed.</param>
    /// <param name="innerException">The inner exception that caused the failure.</param>
    public ScriptExecutionException(string message, string scriptId, Exception innerException)
        : base(message, innerException)
    {
        ScriptId = scriptId ?? throw new ArgumentNullException(nameof(scriptId));
    }

    /// <summary>
    /// Gets the identifier of the script that failed execution.
    /// </summary>
    public string ScriptId { get; }
}
