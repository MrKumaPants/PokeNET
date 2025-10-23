using PokeNET.Scripting.Models;

namespace PokeNET.Scripting.Abstractions;

/// <summary>
/// Represents a compiled script ready for execution.
/// Immutable and thread-safe for concurrent execution.
/// </summary>
public interface ICompiledScript
{
    /// <summary>
    /// Gets the unique identifier for this compiled script.
    /// </summary>
    string ScriptId { get; }

    /// <summary>
    /// Gets the original source code of the script.
    /// </summary>
    string SourceCode { get; }

    /// <summary>
    /// Gets the path to the script file, if available.
    /// </summary>
    string? ScriptPath { get; }

    /// <summary>
    /// Gets the timestamp when the script was compiled.
    /// Used for cache invalidation and hot-reloading.
    /// </summary>
    DateTimeOffset CompiledAt { get; }

    /// <summary>
    /// Gets the hash of the source code for cache validation.
    /// Used to detect changes and trigger recompilation.
    /// </summary>
    string SourceHash { get; }

    /// <summary>
    /// Gets compilation diagnostics (warnings, information messages).
    /// </summary>
    IReadOnlyList<ScriptDiagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets whether the script has any warnings.
    /// </summary>
    bool HasWarnings { get; }

    /// <summary>
    /// Gets metadata about the script (author, version, dependencies, etc.).
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Gets a list of function names exposed by this script.
    /// Useful for introspection and dynamic invocation.
    /// </summary>
    IReadOnlyList<string> ExposedFunctions { get; }
}
