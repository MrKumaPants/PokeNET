using PokeNET.Scripting.Models;

namespace PokeNET.Scripting.Diagnostics;

/// <summary>
/// Exception thrown when script compilation fails due to syntax or semantic errors.
/// </summary>
public sealed class ScriptCompilationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptCompilationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="diagnostics">The compilation diagnostics.</param>
    public ScriptCompilationException(string message, IReadOnlyList<ScriptDiagnostic> diagnostics)
        : base(message)
    {
        Diagnostics = diagnostics ?? Array.Empty<ScriptDiagnostic>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptCompilationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ScriptCompilationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Diagnostics = Array.Empty<ScriptDiagnostic>();
    }

    /// <summary>
    /// Gets the compilation diagnostics associated with this exception.
    /// </summary>
    public IReadOnlyList<ScriptDiagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets a formatted error summary including all diagnostics.
    /// </summary>
    public string GetDetailedMessage()
    {
        if (!Diagnostics.Any())
            return Message;

        var diagnosticSummary = string.Join(
            Environment.NewLine,
            Diagnostics.Select(d => $"  - {d}")
        );

        return $"{Message}{Environment.NewLine}{diagnosticSummary}";
    }
}
