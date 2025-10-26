namespace PokeNET.Audio.Exceptions;

/// <summary>
/// Base exception class for all audio system errors.
/// SOLID PRINCIPLE: Single Responsibility - Represents audio-specific errors.
/// </summary>
public class AudioException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AudioException"/> class.
    /// </summary>
    public AudioException()
        : base("An audio system error occurred.") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public AudioException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AudioException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when audio playback fails.
/// </summary>
public class PlaybackException : AudioException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackException"/> class.
    /// </summary>
    public PlaybackException()
        : base("Audio playback failed.") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public PlaybackException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public PlaybackException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when audio file loading fails.
/// </summary>
public class AudioLoadException : AudioException
{
    /// <summary>
    /// Gets the file path that failed to load.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioLoadException"/> class.
    /// </summary>
    public AudioLoadException()
        : base("Failed to load audio file.") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioLoadException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public AudioLoadException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioLoadException"/> class with a message and file path.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="filePath">The file path that failed to load.</param>
    public AudioLoadException(string message, string filePath)
        : base(message)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioLoadException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AudioLoadException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when audio initialization fails.
/// </summary>
public class AudioInitializationException : AudioException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AudioInitializationException"/> class.
    /// </summary>
    public AudioInitializationException()
        : base("Audio system initialization failed.") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioInitializationException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public AudioInitializationException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioInitializationException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AudioInitializationException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when audio configuration is invalid.
/// </summary>
public class AudioConfigurationException : AudioException
{
    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IReadOnlyCollection<string> ValidationErrors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioConfigurationException"/> class.
    /// </summary>
    public AudioConfigurationException()
        : base("Audio configuration is invalid.") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioConfigurationException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public AudioConfigurationException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioConfigurationException"/> class with validation errors.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="validationErrors">Collection of validation errors.</param>
    public AudioConfigurationException(string message, IReadOnlyCollection<string> validationErrors)
        : base(message)
    {
        ValidationErrors = validationErrors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioConfigurationException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AudioConfigurationException(string message, Exception innerException)
        : base(message, innerException) { }
}
