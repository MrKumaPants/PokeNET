// SOLID Principles Applied:
// - Single Responsibility: Loader only handles script discovery and loading
// - Open/Closed: New loading strategies can be added via implementations
// - Liskov Substitution: All loaders can substitute for each other
// - Interface Segregation: Focused on file/mod operations only
// - Dependency Inversion: Scripting engine depends on this abstraction

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PokeNET.Scripting.Abstractions;

/// <summary>
/// Interface for discovering, loading, and managing script files from various sources.
/// Supports file system, embedded resources, mods, and networked script repositories.
/// </summary>
/// <remarks>
/// <para><b>Architectural Purpose:</b></para>
/// <list type="bullet">
///   <item>Abstracts script file location and retrieval</item>
///   <item>Enables multiple script sources (file system, mods, network)</item>
///   <item>Supports hot-reloading and watch-based updates</item>
///   <item>Provides mod integration and priority ordering</item>
/// </list>
/// <para><b>Loading Strategies:</b></para>
/// <list type="bullet">
///   <item><b>FileSystem:</b> Loads from local directories</item>
///   <item><b>Embedded:</b> Loads from assembly resources</item>
///   <item><b>Mod:</b> Loads from mod packages with override support</item>
///   <item><b>Network:</b> Loads from remote repositories (future)</item>
/// </list>
/// <para><b>SOLID Alignment:</b></para>
/// <list type="bullet">
///   <item><b>SRP:</b> Only responsible for script discovery and retrieval</item>
///   <item><b>OCP:</b> New sources can be added without modifying interface</item>
///   <item><b>DIP:</b> Engine depends on abstraction, not concrete file system</item>
/// </list>
/// </remarks>
public interface IScriptLoader
{
    /// <summary>
    /// Gets the name of this script loader (e.g., "FileSystem", "ModLoader", "Network").
    /// </summary>
    string LoaderName { get; }

    /// <summary>
    /// Gets the priority of this loader (higher values are checked first).
    /// Useful for mod overrides - mod loaders have higher priority than base game scripts.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Loads a script from the specified path.
    /// </summary>
    /// <param name="scriptPath">
    /// Relative or absolute path to the script.
    /// Format depends on loader (e.g., "scripts/player.cs" or "mod:mymod/scripts/custom.cs").
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded script information including source code and metadata.</returns>
    /// <exception cref="ScriptLoadException">Thrown when the script cannot be found or loaded.</exception>
    /// <exception cref="ArgumentNullException">Thrown when scriptPath is null.</exception>
    Task<ScriptLoadResult> LoadScriptAsync(
        string scriptPath,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Checks if a script exists at the specified path.
    /// </summary>
    /// <param name="scriptPath">The path to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the script exists and is accessible; otherwise false.</returns>
    Task<bool> ScriptExistsAsync(string scriptPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers all scripts in a directory (or mod package).
    /// </summary>
    /// <param name="searchPath">
    /// The path to search (e.g., "scripts/", "mod:mymod/scripts/").
    /// </param>
    /// <param name="searchPattern">
    /// File pattern to match (e.g., "*.cs", "*.csx"). Null means all files.
    /// </param>
    /// <param name="recursive">Whether to search subdirectories.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of discovered script paths.</returns>
    Task<IReadOnlyList<string>> DiscoverScriptsAsync(
        string searchPath,
        string? searchPattern = null,
        bool recursive = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Watches a directory for script changes and invokes a callback when changes occur.
    /// Enables hot-reloading during development.
    /// </summary>
    /// <param name="watchPath">The path to watch.</param>
    /// <param name="onChange">Callback invoked when scripts change.</param>
    /// <param name="cancellationToken">Token to stop watching.</param>
    /// <returns>A watcher that can be disposed to stop monitoring.</returns>
    /// <remarks>
    /// Not all loaders support watching (e.g., embedded resources cannot change).
    /// Check <see cref="SupportsWatching"/> before calling.
    /// </remarks>
    Task<IScriptWatcher> WatchScriptsAsync(
        string watchPath,
        Action<ScriptChangeEvent> onChange,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets a value indicating whether this loader supports file watching.
    /// </summary>
    bool SupportsWatching { get; }

    /// <summary>
    /// Gets metadata about the loader's configuration (base paths, mod info, etc.).
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }
}

/// <summary>
/// Result of loading a script, including source code and metadata.
/// </summary>
public sealed class ScriptLoadResult
{
    /// <summary>
    /// Gets or sets the script identifier (derived from path).
    /// </summary>
    public string ScriptId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full source code of the script.
    /// </summary>
    public string SourceCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path where the script was loaded from.
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the loader that loaded this script.
    /// </summary>
    public string LoaderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the script was last modified.
    /// Used for cache invalidation and hot-reloading.
    /// </summary>
    public DateTimeOffset LastModified { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the script.
    /// May include mod information, author, version, dependencies, etc.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the encoding used for the script file.
    /// </summary>
    public string Encoding { get; set; } = "UTF-8";

    /// <summary>
    /// Gets or sets the size of the script in bytes.
    /// </summary>
    public long SizeBytes { get; set; }
}

/// <summary>
/// Monitors script files for changes and provides notifications.
/// </summary>
public interface IScriptWatcher : IDisposable
{
    /// <summary>
    /// Gets the path being watched.
    /// </summary>
    string WatchPath { get; }

    /// <summary>
    /// Gets a value indicating whether the watcher is currently active.
    /// </summary>
    bool IsWatching { get; }

    /// <summary>
    /// Stops watching for changes.
    /// </summary>
    void Stop();

    /// <summary>
    /// Resumes watching for changes after being stopped.
    /// </summary>
    void Resume();
}

/// <summary>
/// Event data for script file changes.
/// </summary>
public sealed class ScriptChangeEvent
{
    /// <summary>
    /// Gets or sets the type of change that occurred.
    /// </summary>
    public ScriptChangeType ChangeType { get; set; }

    /// <summary>
    /// Gets or sets the path of the script that changed.
    /// </summary>
    public string ScriptPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the change occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the old path (for rename operations).
    /// </summary>
    public string? OldPath { get; set; }
}

/// <summary>
/// Types of changes that can occur to script files.
/// </summary>
public enum ScriptChangeType
{
    /// <summary>
    /// A new script was created.
    /// </summary>
    Created,

    /// <summary>
    /// An existing script was modified.
    /// </summary>
    Modified,

    /// <summary>
    /// A script was deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// A script was renamed or moved.
    /// </summary>
    Renamed,
}

/// <summary>
/// Exception thrown when a script cannot be loaded.
/// </summary>
public class ScriptLoadException : Exception
{
    /// <summary>
    /// Gets the script path that failed to load.
    /// </summary>
    public string? ScriptPath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptLoadException"/> class.
    /// </summary>
    public ScriptLoadException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptLoadException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ScriptLoadException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptLoadException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ScriptLoadException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptLoadException"/> class with a script path.
    /// </summary>
    /// <param name="scriptPath">The script path that failed.</param>
    /// <param name="message">The error message.</param>
    public ScriptLoadException(string scriptPath, string message)
        : base(message)
    {
        ScriptPath = scriptPath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptLoadException"/> class with a script path and inner exception.
    /// </summary>
    /// <param name="scriptPath">The script path that failed.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ScriptLoadException(string scriptPath, string message, Exception innerException)
        : base(message, innerException)
    {
        ScriptPath = scriptPath;
    }
}

/// <summary>
/// Exception thrown when script compilation fails.
/// </summary>
public class ScriptCompilationException : Exception
{
    /// <summary>
    /// Gets the script ID that failed to compile.
    /// </summary>
    public string? ScriptId { get; }

    /// <summary>
    /// Gets the line number where the error occurred (if available).
    /// </summary>
    public int? LineNumber { get; }

    /// <summary>
    /// Gets the column number where the error occurred (if available).
    /// </summary>
    public int? ColumnNumber { get; }

    /// <summary>
    /// Gets the source code excerpt around the error (if available).
    /// </summary>
    public string? SourceExcerpt { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptCompilationException"/> class.
    /// </summary>
    public ScriptCompilationException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptCompilationException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ScriptCompilationException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptCompilationException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ScriptCompilationException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptCompilationException"/> class with detailed error information.
    /// </summary>
    /// <param name="scriptId">The script ID that failed.</param>
    /// <param name="message">The error message.</param>
    /// <param name="lineNumber">Line number of the error.</param>
    /// <param name="columnNumber">Column number of the error.</param>
    public ScriptCompilationException(
        string scriptId,
        string message,
        int? lineNumber = null,
        int? columnNumber = null
    )
        : base(message)
    {
        ScriptId = scriptId;
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
    }
}
