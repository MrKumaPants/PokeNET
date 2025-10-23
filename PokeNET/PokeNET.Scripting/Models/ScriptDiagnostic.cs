namespace PokeNET.Scripting.Models;

/// <summary>
/// Represents a diagnostic message from script compilation or execution.
/// </summary>
public sealed class ScriptDiagnostic
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptDiagnostic"/> class.
    /// </summary>
    /// <param name="severity">The severity of the diagnostic.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="lineNumber">The line number where the diagnostic occurred.</param>
    /// <param name="columnNumber">The column number where the diagnostic occurred.</param>
    public ScriptDiagnostic(
        DiagnosticSeverity severity,
        string message,
        string code,
        int lineNumber = 0,
        int columnNumber = 0)
    {
        Severity = severity;
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Code = code ?? throw new ArgumentNullException(nameof(code));
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
    }

    /// <summary>
    /// Gets the severity of the diagnostic.
    /// </summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>
    /// Gets the diagnostic message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the diagnostic code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the line number where the diagnostic occurred.
    /// </summary>
    public int LineNumber { get; }

    /// <summary>
    /// Gets the column number where the diagnostic occurred.
    /// </summary>
    public int ColumnNumber { get; }

    /// <summary>
    /// Gets a formatted string representation of the diagnostic.
    /// </summary>
    public override string ToString()
    {
        var location = LineNumber > 0 ? $"({LineNumber},{ColumnNumber})" : "";
        return $"{Severity} {Code}{location}: {Message}";
    }
}

/// <summary>
/// Defines the severity levels for script diagnostics.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    /// Informational message.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning message.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error message.
    /// </summary>
    Error = 2
}
