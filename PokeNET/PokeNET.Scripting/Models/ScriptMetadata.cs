using PokeNET.Scripting.Interfaces;

namespace PokeNET.Scripting.Models;

/// <summary>
/// Implementation of script metadata containing script identification and configuration.
/// </summary>
public sealed class ScriptMetadata : IScriptMetadata
{
    /// <inheritdoc/>
    public string Id { get; init; }

    /// <inheritdoc/>
    public string Name { get; init; }

    /// <inheritdoc/>
    public string Version { get; init; }

    /// <inheritdoc/>
    public string? Author { get; init; }

    /// <inheritdoc/>
    public string? Description { get; init; }

    /// <inheritdoc/>
    public string FilePath { get; init; }

    /// <inheritdoc/>
    public string Directory { get; init; }

    /// <inheritdoc/>
    public IReadOnlyList<string> Dependencies { get; init; }

    /// <inheritdoc/>
    public IReadOnlyList<string> RequiredPermissions { get; init; }

    /// <inheritdoc/>
    public DateTime LastModified { get; init; }

    /// <inheritdoc/>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Initializes a new instance of script metadata.
    /// </summary>
    /// <param name="id">Unique script identifier.</param>
    /// <param name="name">Human-readable script name.</param>
    /// <param name="filePath">Path to the script file.</param>
    /// <exception cref="ArgumentNullException">Required parameters are null or empty.</exception>
    public ScriptMetadata(string id, string name, string filePath)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        Id = id;
        Name = name;
        FilePath = filePath;
        Directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        Version = "1.0.0";
        Dependencies = Array.Empty<string>();
        RequiredPermissions = Array.Empty<string>();
        LastModified = File.GetLastWriteTimeUtc(filePath);
        IsEnabled = true;
    }

    /// <summary>
    /// Creates script metadata from a file path.
    /// </summary>
    /// <param name="filePath">Path to the script file.</param>
    /// <returns>Script metadata with default values.</returns>
    public static ScriptMetadata FromFile(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var id = GenerateId(filePath);

        return new ScriptMetadata(id, fileName, filePath);
    }

    /// <summary>
    /// Creates a builder for constructing script metadata.
    /// </summary>
    /// <param name="id">Unique script identifier.</param>
    /// <param name="name">Human-readable script name.</param>
    /// <param name="filePath">Path to the script file.</param>
    /// <returns>A new metadata builder instance.</returns>
    public static ScriptMetadataBuilder Builder(string id, string name, string filePath)
    {
        return new ScriptMetadataBuilder(id, name, filePath);
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

    /// <summary>
    /// Builder class for constructing ScriptMetadata instances.
    /// </summary>
    public sealed class ScriptMetadataBuilder
    {
        private readonly string _id;
        private readonly string _name;
        private readonly string _filePath;
        private string _version = "1.0.0";
        private string? _author;
        private string? _description;
        private List<string> _dependencies = new();
        private List<string> _permissions = new();
        private bool _isEnabled = true;

        internal ScriptMetadataBuilder(string id, string name, string filePath)
        {
            _id = id;
            _name = name;
            _filePath = filePath;
        }

        /// <summary>
        /// Sets the script version.
        /// </summary>
        public ScriptMetadataBuilder WithVersion(string version)
        {
            _version = version;
            return this;
        }

        /// <summary>
        /// Sets the script author.
        /// </summary>
        public ScriptMetadataBuilder WithAuthor(string author)
        {
            _author = author;
            return this;
        }

        /// <summary>
        /// Sets the script description.
        /// </summary>
        public ScriptMetadataBuilder WithDescription(string description)
        {
            _description = description;
            return this;
        }

        /// <summary>
        /// Adds a dependency to the script.
        /// </summary>
        public ScriptMetadataBuilder WithDependency(string dependency)
        {
            _dependencies.Add(dependency);
            return this;
        }

        /// <summary>
        /// Adds multiple dependencies to the script.
        /// </summary>
        public ScriptMetadataBuilder WithDependencies(params string[] dependencies)
        {
            _dependencies.AddRange(dependencies);
            return this;
        }

        /// <summary>
        /// Adds a required permission to the script.
        /// </summary>
        public ScriptMetadataBuilder WithPermission(string permission)
        {
            _permissions.Add(permission);
            return this;
        }

        /// <summary>
        /// Adds multiple required permissions to the script.
        /// </summary>
        public ScriptMetadataBuilder WithPermissions(params string[] permissions)
        {
            _permissions.AddRange(permissions);
            return this;
        }

        /// <summary>
        /// Sets whether the script is enabled.
        /// </summary>
        public ScriptMetadataBuilder WithEnabled(bool isEnabled)
        {
            _isEnabled = isEnabled;
            return this;
        }

        /// <summary>
        /// Builds the script metadata instance.
        /// </summary>
        public ScriptMetadata Build()
        {
            return new ScriptMetadata(_id, _name, _filePath)
            {
                Version = _version,
                Author = _author,
                Description = _description,
                Dependencies = _dependencies.AsReadOnly(),
                RequiredPermissions = _permissions.AsReadOnly(),
                IsEnabled = _isEnabled,
            };
        }
    }
}
