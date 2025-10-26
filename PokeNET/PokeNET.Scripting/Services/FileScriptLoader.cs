using Microsoft.Extensions.Logging;
using PokeNET.Scripting.Abstractions;

namespace PokeNET.Scripting.Services;

/// <summary>
/// Loads C# scripts from the file system with support for .cs and .csx files.
/// </summary>
public sealed class FileScriptLoader : IScriptLoader
{
    private readonly ILogger<FileScriptLoader> _logger;
    private static readonly string[] _supportedExtensions = { ".cs", ".csx" };

    /// <summary>
    /// Initializes a new instance of the <see cref="FileScriptLoader"/> class.
    /// </summary>
    /// <param name="logger">Logger for loader operations.</param>
    public FileScriptLoader(ILogger<FileScriptLoader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public string LoaderName => "FileSystem";

    /// <inheritdoc/>
    public int Priority => 0;

    /// <inheritdoc/>
    public bool SupportsWatching => true;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object> Metadata =>
        new Dictionary<string, object>
        {
            ["SupportedExtensions"] = _supportedExtensions,
            ["LoaderType"] = "FileSystem",
        };

    /// <summary>
    /// Gets the supported file extensions.
    /// </summary>
    public IReadOnlyList<string> SupportedExtensions => _supportedExtensions;

    /// <inheritdoc/>
    public async Task<ScriptLoadResult> LoadScriptAsync(
        string scriptPath,
        CancellationToken cancellationToken = default
    )
    {
        var sourceCode = await LoadFromFileAsync(scriptPath, cancellationToken);
        var fileInfo = new FileInfo(scriptPath);

        return new ScriptLoadResult
        {
            ScriptId = Path.GetFileNameWithoutExtension(scriptPath),
            SourceCode = sourceCode,
            SourcePath = scriptPath,
            LoaderName = LoaderName,
            LastModified = fileInfo.Exists
                ? new DateTimeOffset(fileInfo.LastWriteTimeUtc)
                : DateTimeOffset.UtcNow,
            SizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
            Encoding = "UTF-8",
        };
    }

    /// <inheritdoc/>
    public Task<bool> ScriptExistsAsync(
        string scriptPath,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(IsValidScriptFile(scriptPath));
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> DiscoverScriptsAsync(
        string searchPath,
        string? searchPattern = null,
        bool recursive = true,
        CancellationToken cancellationToken = default
    )
    {
        if (!Directory.Exists(searchPath))
        {
            _logger.LogWarning("Search path does not exist: {SearchPath}", searchPath);
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        var pattern = searchPattern ?? "*.cs";
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        try
        {
            var files = Directory
                .GetFiles(searchPath, pattern, searchOption)
                .Where(IsValidScriptFile)
                .ToList();

            _logger.LogInformation(
                "Discovered {Count} scripts in {SearchPath}",
                files.Count,
                searchPath
            );
            return Task.FromResult<IReadOnlyList<string>>(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover scripts in {SearchPath}", searchPath);
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }
    }

    /// <inheritdoc/>
    public Task<IScriptWatcher> WatchScriptsAsync(
        string watchPath,
        Action<ScriptChangeEvent> onChange,
        CancellationToken cancellationToken = default
    )
    {
        if (!Directory.Exists(watchPath))
        {
            throw new DirectoryNotFoundException($"Watch path does not exist: {watchPath}");
        }

        var watcher = new FileScriptWatcher(watchPath, _supportedExtensions, onChange, _logger);
        return Task.FromResult<IScriptWatcher>(watcher);
    }

    /// <summary>
    /// Loads script content from a file.
    /// </summary>
    private async Task<string> LoadFromFileAsync(
        string scriptPath,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(scriptPath))
            throw new ArgumentException("Script path cannot be null or empty.", nameof(scriptPath));

        _logger.LogDebug("Loading script from file: {ScriptPath}", scriptPath);

        // Validate file exists
        if (!File.Exists(scriptPath))
        {
            _logger.LogError("Script file not found: {ScriptPath}", scriptPath);
            throw new FileNotFoundException($"Script file not found: {scriptPath}", scriptPath);
        }

        // Validate file extension
        var extension = Path.GetExtension(scriptPath);
        if (!_supportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "Unsupported script file extension: {Extension}. Supported: {SupportedExtensions}",
                extension,
                string.Join(", ", _supportedExtensions)
            );

            throw new InvalidOperationException(
                $"Unsupported script file extension: {extension}. "
                    + $"Supported extensions: {string.Join(", ", _supportedExtensions)}"
            );
        }

        try
        {
            // Read file content asynchronously
            var content = await File.ReadAllTextAsync(scriptPath, cancellationToken);

            _logger.LogInformation(
                "Successfully loaded script from file: {ScriptPath} ({Length} characters)",
                scriptPath,
                content.Length
            );

            return content;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Script loading cancelled: {ScriptPath}", scriptPath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load script from file: {ScriptPath}", scriptPath);
            throw new InvalidOperationException(
                $"Failed to load script from file: {scriptPath}",
                ex
            );
        }
    }

    /// <inheritdoc/>
    public bool IsValidScriptFile(string scriptPath)
    {
        if (string.IsNullOrWhiteSpace(scriptPath))
            return false;

        if (!File.Exists(scriptPath))
            return false;

        var extension = Path.GetExtension(scriptPath);
        return _supportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// File system watcher for script changes.
    /// </summary>
    private sealed class FileScriptWatcher : IScriptWatcher
    {
        private readonly FileSystemWatcher _watcher;
        private readonly Action<ScriptChangeEvent> _onChange;
        private readonly ILogger? _logger;

        public FileScriptWatcher(
            string watchPath,
            string[] supportedExtensions,
            Action<ScriptChangeEvent> onChange,
            ILogger? logger
        )
        {
            WatchPath = watchPath;
            _onChange = onChange;
            _logger = logger;

            _watcher = new FileSystemWatcher(watchPath)
            {
                NotifyFilter =
                    NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                Filter = "*.cs*",
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
            };

            _watcher.Created += OnFileChanged;
            _watcher.Changed += OnFileChanged;
            _watcher.Deleted += OnFileChanged;
            _watcher.Renamed += OnFileRenamed;

            _logger?.LogInformation("Started watching scripts in {WatchPath}", watchPath);
        }

        public string WatchPath { get; }
        public bool IsWatching => _watcher.EnableRaisingEvents;

        public void Stop()
        {
            _watcher.EnableRaisingEvents = false;
            _logger?.LogInformation("Stopped watching scripts in {WatchPath}", WatchPath);
        }

        public void Resume()
        {
            _watcher.EnableRaisingEvents = true;
            _logger?.LogInformation("Resumed watching scripts in {WatchPath}", WatchPath);
        }

        public void Dispose()
        {
            _watcher.Created -= OnFileChanged;
            _watcher.Changed -= OnFileChanged;
            _watcher.Deleted -= OnFileChanged;
            _watcher.Renamed -= OnFileRenamed;
            _watcher.Dispose();
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            var changeType = e.ChangeType switch
            {
                WatcherChangeTypes.Created => ScriptChangeType.Created,
                WatcherChangeTypes.Changed => ScriptChangeType.Modified,
                WatcherChangeTypes.Deleted => ScriptChangeType.Deleted,
                _ => ScriptChangeType.Modified,
            };

            _onChange(
                new ScriptChangeEvent
                {
                    ChangeType = changeType,
                    ScriptPath = e.FullPath,
                    Timestamp = DateTimeOffset.UtcNow,
                }
            );
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            _onChange(
                new ScriptChangeEvent
                {
                    ChangeType = ScriptChangeType.Renamed,
                    ScriptPath = e.FullPath,
                    OldPath = e.OldFullPath,
                    Timestamp = DateTimeOffset.UtcNow,
                }
            );
        }
    }
}
