using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using PokeNET.Scripting.Abstractions;

namespace PokeNET.Scripting.Models;

/// <summary>
/// Represents a compiled C# script with metadata and diagnostics.
/// </summary>
public sealed class CompiledScript : ICompiledScript
{
    /// <summary>
    /// Gets the internal Roslyn script instance.
    /// </summary>
    internal Script<object> RoslynScript { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompiledScript"/> class.
    /// </summary>
    /// <param name="roslynScript">The compiled Roslyn script.</param>
    /// <param name="sourceCode">The original source code.</param>
    /// <param name="scriptPath">Optional path to the script file.</param>
    /// <param name="diagnostics">Compilation diagnostics.</param>
    /// <param name="metadata">Optional metadata about the script.</param>
    /// <param name="exposedFunctions">Optional list of exposed function names.</param>
    public CompiledScript(
        Script<object> roslynScript,
        string sourceCode,
        string? scriptPath,
        IReadOnlyList<ScriptDiagnostic> diagnostics,
        IReadOnlyDictionary<string, object>? metadata = null,
        IReadOnlyList<string>? exposedFunctions = null
    )
    {
        RoslynScript = roslynScript ?? throw new ArgumentNullException(nameof(roslynScript));
        SourceCode = sourceCode ?? throw new ArgumentNullException(nameof(sourceCode));
        ScriptPath = scriptPath;
        Diagnostics = diagnostics ?? Array.Empty<ScriptDiagnostic>();
        Metadata = metadata ?? new Dictionary<string, object>();
        ExposedFunctions = exposedFunctions ?? Array.Empty<string>();
        ScriptId = GenerateScriptId(sourceCode, scriptPath);
        SourceHash = ComputeSourceHash(sourceCode);
        CompiledAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc/>
    public string ScriptId { get; }

    /// <inheritdoc/>
    public string SourceCode { get; }

    /// <inheritdoc/>
    public string? ScriptPath { get; }

    /// <inheritdoc/>
    public DateTimeOffset CompiledAt { get; }

    /// <inheritdoc/>
    public string SourceHash { get; }

    /// <inheritdoc/>
    public IReadOnlyList<ScriptDiagnostic> Diagnostics { get; }

    /// <inheritdoc/>
    public bool HasWarnings => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning);

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object> Metadata { get; }

    /// <inheritdoc/>
    public IReadOnlyList<string> ExposedFunctions { get; }

    /// <summary>
    /// Generates a unique identifier for the script based on source hash and path.
    /// </summary>
    private static string GenerateScriptId(string sourceCode, string? scriptPath)
    {
        var hash = ComputeSourceHash(sourceCode);
        var pathPart = scriptPath != null ? Path.GetFileName(scriptPath) : "inline";
        return $"{pathPart}_{hash}";
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
}
