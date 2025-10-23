namespace PokeNET.Scripting.Interfaces;

/// <summary>
/// Contains metadata information about a script.
/// </summary>
/// <remarks>
/// Script metadata includes identification, versioning, dependencies, and
/// security permissions required by the script.
/// </remarks>
public interface IScriptMetadata
{
    /// <summary>
    /// Gets the unique identifier for the script.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the human-readable name of the script.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the script version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the script author.
    /// </summary>
    string? Author { get; }

    /// <summary>
    /// Gets the script description.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the file path to the script source file.
    /// </summary>
    string FilePath { get; }

    /// <summary>
    /// Gets the directory containing the script.
    /// </summary>
    string Directory { get; }

    /// <summary>
    /// Gets the list of dependencies required by this script.
    /// </summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// Gets the required permissions for script execution.
    /// </summary>
    IReadOnlyList<string> RequiredPermissions { get; }

    /// <summary>
    /// Gets the timestamp when the script file was last modified.
    /// </summary>
    DateTime LastModified { get; }

    /// <summary>
    /// Gets whether the script is enabled for execution.
    /// </summary>
    bool IsEnabled { get; }
}
