using Microsoft.Extensions.Logging;

namespace PokeNET.Scripting.Interfaces;

/// <summary>
/// Provides scripts with access to game services through dependency injection.
/// </summary>
/// <remarks>
/// <para>
/// The script context is a secure wrapper around the service provider that scripts use
/// to interact with the game engine. It provides controlled access to the ECS world,
/// event bus, and other game systems while maintaining security boundaries.
/// </para>
/// <para>
/// This interface follows the Facade pattern to simplify script interactions with
/// the complex underlying game systems.
/// </para>
/// </remarks>
public interface IScriptContext
{
    /// <summary>
    /// Gets the logger for script output and debugging.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Gets the script API for safe ECS world interaction.
    /// </summary>
    IScriptApi Api { get; }

    /// <summary>
    /// Gets metadata about the currently executing script.
    /// </summary>
    IScriptMetadata Metadata { get; }

    /// <summary>
    /// Retrieves a service from the dependency injection container.
    /// </summary>
    /// <typeparam name="T">The service type to retrieve.</typeparam>
    /// <returns>The requested service instance.</returns>
    /// <exception cref="InvalidOperationException">Service not found.</exception>
    T GetService<T>() where T : notnull;

    /// <summary>
    /// Attempts to retrieve a service from the dependency injection container.
    /// </summary>
    /// <typeparam name="T">The service type to retrieve.</typeparam>
    /// <param name="service">The service instance if found; otherwise null.</param>
    /// <returns>True if the service was found; otherwise false.</returns>
    bool TryGetService<T>(out T? service) where T : class;
}
