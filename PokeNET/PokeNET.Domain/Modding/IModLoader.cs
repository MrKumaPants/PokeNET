namespace PokeNET.Domain.Modding;

/// <summary>
/// Interface for the mod loading system.
/// </summary>
/// <remarks>
/// This interface is primarily for internal use by the game engine.
/// Mods typically do not need to interact with the loader directly.
/// </remarks>
public interface IModLoader
{
    /// <summary>
    /// Gets all currently loaded mods.
    /// </summary>
    IReadOnlyList<IModManifest> LoadedMods { get; }

    /// <summary>
    /// Loads mods from the specified directory.
    /// </summary>
    /// <param name="modsDirectory">Directory containing mod subdirectories.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task that completes when all mods are loaded.</returns>
    /// <exception cref="ModLoadException">Thrown if mod loading fails.</exception>
    Task LoadModsAsync(string modsDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reloads a specific mod (hot reload for development).
    /// </summary>
    /// <param name="modId">ID of the mod to reload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task that completes when the mod is reloaded.</returns>
    /// <remarks>
    /// This is primarily for development. In production, mods should be reloaded by restarting the game.
    /// </remarks>
    Task ReloadModAsync(string modId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloads a specific mod.
    /// </summary>
    /// <param name="modId">ID of the mod to unload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task that completes when the mod is unloaded.</returns>
    Task UnloadModAsync(string modId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the load order for the currently loaded mods.
    /// </summary>
    /// <returns>List of mod IDs in the order they were loaded.</returns>
    IReadOnlyList<string> GetLoadOrder();

    /// <summary>
    /// Validates mod manifests and dependencies without loading.
    /// </summary>
    /// <param name="modsDirectory">Directory to validate.</param>
    /// <returns>Validation report with errors and warnings.</returns>
    Task<ModValidationReport> ValidateModsAsync(string modsDirectory);
}

/// <summary>
/// Report containing validation results for mods.
/// </summary>
public class ModValidationReport
{
    /// <summary>
    /// List of validation errors (prevent mod loading).
    /// </summary>
    public List<ModValidationError> Errors { get; init; } = new();

    /// <summary>
    /// List of validation warnings (mod can still load).
    /// </summary>
    public List<ModValidationWarning> Warnings { get; init; } = new();

    /// <summary>
    /// Whether validation passed (no errors).
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets a summary of validation results.
    /// </summary>
    public string GetSummary()
    {
        return $"Validation: {(IsValid ? "PASSED" : "FAILED")} - " +
               $"{Errors.Count} error(s), {Warnings.Count} warning(s)";
    }
}

/// <summary>
/// Represents a validation error that prevents mod loading.
/// </summary>
public record ModValidationError
{
    /// <summary>
    /// ID of the mod with the error (if known).
    /// </summary>
    public string? ModId { get; init; }

    /// <summary>
    /// Error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Error category.
    /// </summary>
    public ModValidationErrorType ErrorType { get; init; }

    /// <summary>
    /// Additional context or details.
    /// </summary>
    public string? Details { get; init; }
}

/// <summary>
/// Represents a validation warning (mod can still load).
/// </summary>
public record ModValidationWarning
{
    /// <summary>
    /// ID of the mod with the warning.
    /// </summary>
    public required string ModId { get; init; }

    /// <summary>
    /// Warning message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Warning category.
    /// </summary>
    public ModValidationWarningType WarningType { get; init; }
}

/// <summary>
/// Categories of validation errors.
/// </summary>
public enum ModValidationErrorType
{
    /// <summary>
    /// Manifest file is missing or invalid.
    /// </summary>
    InvalidManifest,

    /// <summary>
    /// Required dependency is missing.
    /// </summary>
    MissingDependency,

    /// <summary>
    /// Dependency version is incompatible.
    /// </summary>
    IncompatibleDependency,

    /// <summary>
    /// Circular dependency detected.
    /// </summary>
    CircularDependency,

    /// <summary>
    /// Required API version is not supported.
    /// </summary>
    IncompatibleApiVersion,

    /// <summary>
    /// Mod entry point class not found or invalid.
    /// </summary>
    InvalidEntryPoint,

    /// <summary>
    /// Required assembly file is missing.
    /// </summary>
    MissingAssembly,

    /// <summary>
    /// Incompatible mod is also loaded.
    /// </summary>
    IncompatibleModLoaded,

    /// <summary>
    /// Mod ID is duplicated.
    /// </summary>
    DuplicateModId
}

/// <summary>
/// Categories of validation warnings.
/// </summary>
public enum ModValidationWarningType
{
    /// <summary>
    /// Optional dependency is missing.
    /// </summary>
    MissingOptionalDependency,

    /// <summary>
    /// Mod patches a sensitive method.
    /// </summary>
    SensitivePatch,

    /// <summary>
    /// Potential conflict with another mod.
    /// </summary>
    PotentialConflict,

    /// <summary>
    /// Mod manifest uses deprecated fields.
    /// </summary>
    DeprecatedManifestField,

    /// <summary>
    /// Mod uses deprecated API.
    /// </summary>
    DeprecatedApiUsage,

    /// <summary>
    /// Mod checksum verification failed.
    /// </summary>
    ChecksumMismatch
}

/// <summary>
/// Exception thrown when mod loading fails.
/// </summary>
public class ModLoadException : Exception
{
    /// <summary>
    /// ID of the mod that failed to load.
    /// </summary>
    public string? ModId { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ModLoadException"/>.
    /// </summary>
    public ModLoadException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of <see cref="ModLoadException"/> with a mod ID.
    /// </summary>
    public ModLoadException(string message, string modId) : base(message)
    {
        ModId = modId;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ModLoadException"/> with an inner exception.
    /// </summary>
    public ModLoadException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of <see cref="ModLoadException"/> with a mod ID and inner exception.
    /// </summary>
    public ModLoadException(string message, string modId, Exception innerException)
        : base(message, innerException)
    {
        ModId = modId;
    }
}

/// <summary>
/// Exception thrown during mod initialization.
/// </summary>
public class ModInitializationException : ModLoadException
{
    /// <summary>
    /// Initializes a new instance of <see cref="ModInitializationException"/>.
    /// </summary>
    public ModInitializationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of <see cref="ModInitializationException"/> with a mod ID.
    /// </summary>
    public ModInitializationException(string message, string modId) : base(message, modId) { }

    /// <summary>
    /// Initializes a new instance of <see cref="ModInitializationException"/> with an inner exception.
    /// </summary>
    public ModInitializationException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of <see cref="ModInitializationException"/> with a mod ID and inner exception.
    /// </summary>
    public ModInitializationException(string message, string modId, Exception innerException)
        : base(message, modId, innerException) { }
}
