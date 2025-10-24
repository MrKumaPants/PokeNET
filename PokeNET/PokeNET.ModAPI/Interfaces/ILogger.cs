namespace PokeNET.ModAPI.Interfaces;

/// <summary>
/// Provides logging capabilities scoped to individual mods.
/// </summary>
/// <remarks>
/// All log messages are automatically prefixed with the mod identifier
/// and routed through the game's logging system.
/// </remarks>
public interface ILogger
{
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Info(string message);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Warning(string message);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Error(string message);

    /// <summary>
    /// Logs an error message with an exception.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">The exception to log.</param>
    void Error(string message, Exception exception);

    /// <summary>
    /// Logs a debug message (only visible in debug builds).
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Debug(string message);
}
