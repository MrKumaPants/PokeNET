// SOLID Principles Applied:
// - Single Responsibility: Context only provides access to services and APIs
// - Open/Closed: New services can be registered without modifying interface
// - Liskov Substitution: All implementations provide consistent service access
// - Interface Segregation: Separated from execution concerns (IScriptingEngine)
// - Dependency Inversion: Scripts depend on abstractions, not concrete services

using System;
using System.Collections.Generic;

namespace PokeNET.Scripting.Abstractions;

/// <summary>
/// Provides the execution context for scripts, including dependency injection
/// access and controlled API surface for ECS world interaction.
/// </summary>
/// <remarks>
/// <para><b>Architectural Purpose:</b></para>
/// <list type="bullet">
///   <item>Acts as a service locator for dependency injection within scripts</item>
///   <item>Enforces security boundaries - scripts can only access registered APIs</item>
///   <item>Provides script-scoped data storage (per-execution state)</item>
///   <item>Enables communication between scripts through shared context</item>
/// </list>
/// <para><b>Security Model:</b></para>
/// <list type="bullet">
///   <item>Scripts cannot access arbitrary .NET APIs</item>
///   <item>Only explicitly registered services are available</item>
///   <item>API implementations control what operations are permitted</item>
///   <item>Sandboxing prevents scripts from interfering with each other</item>
/// </list>
/// <para><b>SOLID Alignment:</b></para>
/// <list type="bullet">
///   <item><b>SRP:</b> Only manages service access and context state</item>
///   <item><b>DIP:</b> Scripts depend on this abstraction, not concrete implementations</item>
///   <item><b>OCP:</b> New APIs can be added without changing the interface</item>
/// </list>
/// </remarks>
public interface IScriptContext
{
    /// <summary>
    /// Gets the unique identifier for this execution context.
    /// Each script execution gets a unique context instance.
    /// </summary>
    Guid ContextId { get; }

    /// <summary>
    /// Gets the identifier of the script being executed in this context.
    /// </summary>
    string ScriptId { get; }

    /// <summary>
    /// Gets the timestamp when this context was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the primary API interface for interacting with the ECS world.
    /// This is the main entry point for game state manipulation.
    /// </summary>
    /// <remarks>
    /// The API surface is intentionally limited to prevent scripts from
    /// performing dangerous operations or accessing internal engine state.
    /// </remarks>
    IScriptApi Api { get; }

    /// <summary>
    /// Gets a service from the dependency injection container.
    /// </summary>
    /// <typeparam name="T">Type of the service to retrieve.</typeparam>
    /// <returns>The requested service instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the service is not registered or not accessible to scripts.
    /// </exception>
    /// <remarks>
    /// <para>Only services explicitly registered as script-accessible are available.</para>
    /// <para>This prevents scripts from accessing internal engine services.</para>
    /// <para>Thread-safe for concurrent script execution.</para>
    /// </remarks>
    T GetService<T>()
        where T : notnull;

    /// <summary>
    /// Attempts to get a service from the dependency injection container.
    /// </summary>
    /// <typeparam name="T">Type of the service to retrieve.</typeparam>
    /// <param name="service">The service instance if found; otherwise default value.</param>
    /// <returns>True if the service was found; otherwise false.</returns>
    /// <remarks>
    /// Use this method when the service is optional and you want to avoid exceptions.
    /// </remarks>
    bool TryGetService<T>(out T? service)
        where T : notnull;

    /// <summary>
    /// Gets a service by type name (for dynamic scripting languages).
    /// </summary>
    /// <param name="serviceType">Full type name of the service.</param>
    /// <returns>The service instance as an object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when service is not found.</exception>
    /// <exception cref="ArgumentNullException">Thrown when serviceType is null.</exception>
    object GetService(Type serviceType);

    /// <summary>
    /// Gets script-scoped data storage for maintaining state during execution.
    /// </summary>
    /// <remarks>
    /// <para>Data stored here is isolated to this script execution.</para>
    /// <para>Useful for maintaining state across function calls within the same execution.</para>
    /// <para>Cleared automatically when execution completes.</para>
    /// </remarks>
    IDictionary<string, object?> Data { get; }

    /// <summary>
    /// Gets shared data storage accessible to all scripts in the same session.
    /// </summary>
    /// <remarks>
    /// <para>Enables inter-script communication and data sharing.</para>
    /// <para>Thread-safe for concurrent access.</para>
    /// <para>Persists across individual script executions within a session.</para>
    /// <para><b>Warning:</b> Use sparingly to avoid tight coupling between scripts.</para>
    /// </remarks>
    ISharedScriptData SharedData { get; }

    /// <summary>
    /// Gets the logger for this script context.
    /// Scripts should use this for diagnostic output instead of Console.WriteLine.
    /// </summary>
    IScriptLogger Logger { get; }

    /// <summary>
    /// Gets metadata about the current execution environment.
    /// Includes information like game version, mod context, player data, etc.
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Registers a callback to be invoked when the context is disposed.
    /// Useful for cleanup operations (releasing resources, unregistering listeners, etc.).
    /// </summary>
    /// <param name="callback">The cleanup callback.</param>
    void RegisterCleanupCallback(Action callback);
}

/// <summary>
/// Thread-safe shared data storage for inter-script communication.
/// </summary>
/// <remarks>
/// Implements a simple key-value store with atomic operations.
/// Use this for coordination between scripts, not for high-volume data transfer.
/// </remarks>
public interface ISharedScriptData
{
    /// <summary>
    /// Gets or sets a value in the shared data store.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <returns>The value associated with the key.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when getting a non-existent key.</exception>
    object? this[string key] { get; set; }

    /// <summary>
    /// Attempts to get a value from the shared data store.
    /// </summary>
    /// <typeparam name="T">Expected type of the value.</typeparam>
    /// <param name="key">The data key.</param>
    /// <param name="value">The value if found and of correct type.</param>
    /// <returns>True if found and type matches; otherwise false.</returns>
    bool TryGetValue<T>(string key, out T? value);

    /// <summary>
    /// Sets a value in the shared data store.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <param name="value">The value to store.</param>
    void Set(string key, object? value);

    /// <summary>
    /// Removes a value from the shared data store.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <returns>True if the key existed and was removed; otherwise false.</returns>
    bool Remove(string key);

    /// <summary>
    /// Checks if a key exists in the shared data store.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <returns>True if the key exists; otherwise false.</returns>
    bool ContainsKey(string key);

    /// <summary>
    /// Clears all data from the shared store.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets all keys currently in the shared data store.
    /// </summary>
    IReadOnlyCollection<string> Keys { get; }
}

/// <summary>
/// Logger interface for scripts to produce diagnostic output.
/// Integrates with the game's logging infrastructure.
/// </summary>
public interface IScriptLogger
{
    /// <summary>
    /// Logs a debug message (lowest priority, for development only).
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Debug(string message);

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
    /// Logs an error message with an associated exception.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">The exception that occurred.</param>
    void Error(string message, Exception exception);

    /// <summary>
    /// Gets a value indicating whether debug logging is enabled.
    /// Scripts can check this to avoid expensive string formatting for debug messages.
    /// </summary>
    bool IsDebugEnabled { get; }
}
