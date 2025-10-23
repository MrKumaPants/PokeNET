namespace PokeNET.Scripting.Interfaces;

/// <summary>
/// Responsible for discovering and loading scripts from the file system.
/// </summary>
/// <remarks>
/// <para>
/// The script loader scans mod directories for script files, validates them,
/// and prepares them for compilation and execution. It follows the Single
/// Responsibility Principle by focusing solely on script discovery and loading.
/// </para>
/// <para>
/// Supported script file extensions:
/// - .csx (C# Script files)
/// - .cs (C# source files with script annotations)
/// </para>
/// </remarks>
public interface IScriptLoader
{
    /// <summary>
    /// Discovers all scripts in the specified directory.
    /// </summary>
    /// <param name="directory">The directory to scan for scripts.</param>
    /// <param name="recursive">Whether to scan subdirectories.</param>
    /// <returns>Collection of discovered script metadata.</returns>
    /// <exception cref="ArgumentNullException">Directory is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Directory does not exist.</exception>
    IReadOnlyList<IScriptMetadata> DiscoverScripts(string directory, bool recursive = true);

    /// <summary>
    /// Loads a script file and returns its metadata and source code.
    /// </summary>
    /// <param name="filePath">The path to the script file.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Script metadata and source code.</returns>
    /// <exception cref="ArgumentNullException">File path is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Script file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Script file is invalid or corrupted.</exception>
    Task<(IScriptMetadata Metadata, string SourceCode)> LoadScriptAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a file is a valid script file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>True if the file is a valid script; otherwise false.</returns>
    bool IsValidScriptFile(string filePath);

    /// <summary>
    /// Gets all supported script file extensions.
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }
}
