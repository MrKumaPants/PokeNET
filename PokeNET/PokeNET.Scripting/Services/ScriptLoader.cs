using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PokeNET.Scripting.Interfaces;
using PokeNET.Scripting.Models;

namespace PokeNET.Scripting.Services;

/// <summary>
/// Implementation of IScriptLoader for discovering and loading scripts from the file system.
/// </summary>
/// <remarks>
/// <para>
/// The script loader scans directories for script files, validates their format,
/// and extracts metadata from script headers. It supports both .csx (C# Script)
/// and .cs (C# source with script annotations) files.
/// </para>
/// <para>
/// Script files can include metadata in comments at the top of the file:
/// <code>
/// // @script-id: my-awesome-script
/// // @name: My Awesome Script
/// // @version: 1.0.0
/// // @author: Your Name
/// // @description: Does awesome things
/// // @dependencies: other-script-id
/// // @permissions: entities.create, events.publish
/// </code>
/// </para>
/// </remarks>
public sealed class ScriptLoader : IScriptLoader
{
    private readonly ILogger<ScriptLoader> _logger;
    private static readonly string[] DefaultExtensions = { ".csx", ".cs" };

    // Cache compiled regex patterns for better performance
    private static readonly ConcurrentDictionary<string, Regex> _regexCache = new();

    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedExtensions => DefaultExtensions;

    /// <summary>
    /// Initializes a new script loader.
    /// </summary>
    /// <param name="logger">Logger for script loading operations.</param>
    public ScriptLoader(ILogger<ScriptLoader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public IReadOnlyList<IScriptMetadata> DiscoverScripts(string directory, bool recursive = true)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentNullException(nameof(directory));

        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Directory not found: {directory}");

        _logger.LogInformation("Discovering scripts in directory: {Directory} (recursive: {Recursive})",
            directory, recursive);

        var scripts = new List<IScriptMetadata>();
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (var extension in SupportedExtensions)
        {
            var pattern = $"*{extension}";
            var files = Directory.GetFiles(directory, pattern, searchOption);

            foreach (var file in files)
            {
                if (IsValidScriptFile(file))
                {
                    try
                    {
                        var metadata = ExtractMetadata(file);
                        scripts.Add(metadata);
                        _logger.LogDebug("Discovered script: {ScriptId} at {FilePath}",
                            metadata.Id, file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract metadata from script file: {FilePath}", file);
                    }
                }
            }
        }

        _logger.LogInformation("Discovered {Count} scripts in {Directory}", scripts.Count, directory);
        return scripts.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<(IScriptMetadata Metadata, string SourceCode)> LoadScriptAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Script file not found: {filePath}");

        if (!IsValidScriptFile(filePath))
            throw new InvalidOperationException($"Invalid script file: {filePath}");

        try
        {
            _logger.LogDebug("Loading script from file: {FilePath}", filePath);

            var sourceCode = await File.ReadAllTextAsync(filePath, cancellationToken);
            var metadata = ExtractMetadata(filePath, sourceCode);

            _logger.LogInformation("Loaded script: {ScriptId} ({Size} bytes)",
                metadata.Id, sourceCode.Length);

            return (metadata, sourceCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load script from file: {FilePath}", filePath);
            throw new InvalidOperationException($"Failed to load script: {filePath}", ex);
        }
    }

    /// <inheritdoc/>
    public bool IsValidScriptFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        if (!File.Exists(filePath))
            return false;

        var extension = Path.GetExtension(filePath);
        return SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts metadata from a script file.
    /// </summary>
    /// <param name="filePath">Path to the script file.</param>
    /// <param name="sourceCode">Optional source code (will be read from file if null).</param>
    /// <returns>Extracted script metadata.</returns>
    private IScriptMetadata ExtractMetadata(string filePath, string? sourceCode = null)
    {
        sourceCode ??= File.ReadAllText(filePath);

        var builder = ScriptMetadata.Builder(
            ExtractTag(sourceCode, "script-id") ?? GenerateId(filePath),
            ExtractTag(sourceCode, "name") ?? Path.GetFileNameWithoutExtension(filePath),
            filePath
        );

        var version = ExtractTag(sourceCode, "version");
        if (!string.IsNullOrWhiteSpace(version))
            builder.WithVersion(version);

        var author = ExtractTag(sourceCode, "author");
        if (!string.IsNullOrWhiteSpace(author))
            builder.WithAuthor(author);

        var description = ExtractTag(sourceCode, "description");
        if (!string.IsNullOrWhiteSpace(description))
            builder.WithDescription(description);

        var dependencies = ExtractTags(sourceCode, "dependencies");
        if (dependencies.Count > 0)
            builder.WithDependencies(dependencies.ToArray());

        var permissions = ExtractTags(sourceCode, "permissions");
        if (permissions.Count > 0)
            builder.WithPermissions(permissions.ToArray());

        var enabled = ExtractTag(sourceCode, "enabled");
        if (!string.IsNullOrWhiteSpace(enabled))
            builder.WithEnabled(bool.Parse(enabled));

        return builder.Build();
    }

    /// <summary>
    /// Extracts a single metadata tag from the source code.
    /// Uses compiled regex cache for improved performance.
    /// </summary>
    /// <param name="sourceCode">The script source code.</param>
    /// <param name="tagName">The tag name to extract.</param>
    /// <returns>The tag value if found; otherwise null.</returns>
    private static string? ExtractTag(string sourceCode, string tagName)
    {
        var pattern = $@"//\s*@{tagName}:\s*(.+)";

        // Get or create cached compiled regex
        var regex = _regexCache.GetOrAdd(pattern,
            p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled));

        var match = regex.Match(sourceCode);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    /// <summary>
    /// Extracts multiple values from a comma-separated metadata tag.
    /// </summary>
    /// <param name="sourceCode">The script source code.</param>
    /// <param name="tagName">The tag name to extract.</param>
    /// <returns>List of tag values.</returns>
    private static List<string> ExtractTags(string sourceCode, string tagName)
    {
        var value = ExtractTag(sourceCode, tagName);
        if (string.IsNullOrWhiteSpace(value))
            return new List<string>();

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    /// <summary>
    /// Generates a unique ID for a script based on its file path.
    /// </summary>
    private static string GenerateId(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var directory = Path.GetFileName(Path.GetDirectoryName(filePath)) ?? "default";
        return $"{directory}.{fileName}";
    }
}
