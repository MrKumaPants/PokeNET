using Microsoft.Extensions.Logging;

namespace PokeNET.Domain.Modding;

/// <summary>
/// Provides runtime context and services to a mod during its lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// The mod context is the primary way for mods to interact with the game.
/// It provides access to:
/// - Asset loading and management
/// - Entity creation and manipulation (ECS)
/// - Event subscription
/// - Logging
/// - Configuration
/// - Other loaded mods
/// </para>
/// <para>
/// The context is created when the mod is loaded and remains valid until shutdown.
/// </para>
/// </remarks>
public interface IModContext
{
    /// <summary>
    /// The mod's manifest metadata.
    /// </summary>
    IModManifest Manifest { get; }

    /// <summary>
    /// Logger for this mod (automatically prefixed with mod name).
    /// </summary>
    /// <remarks>
    /// All log messages will be tagged with the mod ID for easy filtering and debugging.
    /// </remarks>
    ILogger Logger { get; }

    /// <summary>
    /// Asset API for loading and accessing game assets.
    /// </summary>
    IAssetApi Assets { get; }

    /// <summary>
    /// Entity API for interacting with the ECS (Entity Component System) world.
    /// </summary>
    IEntityApi Entities { get; }

    /// <summary>
    /// Event API for subscribing to game events.
    /// </summary>
    IEventApi Events { get; }

    /// <summary>
    /// Configuration API for reading mod configuration.
    /// </summary>
    IConfigurationApi Configuration { get; }

    /// <summary>
    /// Provides access to other loaded mods for cross-mod integration.
    /// </summary>
    IModRegistry ModRegistry { get; }

    /// <summary>
    /// Directory path where the mod is located.
    /// </summary>
    /// <remarks>
    /// Use this to construct paths to mod-specific files:
    /// <code>
    /// var dataPath = Path.Combine(context.ModDirectory, "Data", "creatures.json");
    /// </code>
    /// </remarks>
    string ModDirectory { get; }

    /// <summary>
    /// Gets a service from the game's dependency injection container.
    /// </summary>
    /// <typeparam name="T">Type of service to retrieve.</typeparam>
    /// <returns>The requested service instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the service is not registered or not accessible to mods.
    /// </exception>
    /// <remarks>
    /// Only a subset of game services are exposed to mods for security and stability.
    /// Use the specific APIs (<see cref="Assets"/>, <see cref="Entities"/>, etc.) when possible.
    /// </remarks>
    T GetService<T>() where T : notnull;

    /// <summary>
    /// Attempts to get a service from the dependency injection container.
    /// </summary>
    /// <typeparam name="T">Type of service to retrieve.</typeparam>
    /// <param name="service">The service instance if found; otherwise null.</param>
    /// <returns>True if the service was found; otherwise false.</returns>
    bool TryGetService<T>(out T? service) where T : class;
}

/// <summary>
/// Registry for accessing information about loaded mods.
/// </summary>
public interface IModRegistry
{
    /// <summary>
    /// Gets all loaded mods.
    /// </summary>
    IReadOnlyList<IModManifest> GetAllMods();

    /// <summary>
    /// Gets a mod by its ID.
    /// </summary>
    /// <param name="modId">The unique mod identifier.</param>
    /// <returns>The mod manifest if found; otherwise null.</returns>
    IModManifest? GetMod(string modId);

    /// <summary>
    /// Checks if a mod with the specified ID is loaded.
    /// </summary>
    /// <param name="modId">The mod ID to check.</param>
    /// <returns>True if the mod is loaded; otherwise false.</returns>
    bool IsModLoaded(string modId);

    /// <summary>
    /// Gets the API instance exposed by another mod (if any).
    /// </summary>
    /// <typeparam name="TApi">The API interface type.</typeparam>
    /// <param name="modId">The ID of the mod providing the API.</param>
    /// <returns>The API instance if the mod provides it; otherwise null.</returns>
    /// <remarks>
    /// <para>
    /// Mods can expose public APIs for other mods to integrate with.
    /// To provide an API, implement <see cref="IModApiProvider"/>.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// var economyApi = context.ModRegistry.GetApi&lt;IEconomyApi&gt;("com.example.economy");
    /// if (economyApi != null)
    /// {
    ///     economyApi.AddCurrency(player, 100);
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    TApi? GetApi<TApi>(string modId) where TApi : class;

    /// <summary>
    /// Gets all mods that depend on the specified mod.
    /// </summary>
    /// <param name="modId">The mod ID to check dependents for.</param>
    /// <returns>List of mods that declare this mod as a dependency.</returns>
    IReadOnlyList<IModManifest> GetDependentMods(string modId);

    /// <summary>
    /// Gets all mods that the specified mod depends on.
    /// </summary>
    /// <param name="modId">The mod ID to check dependencies for.</param>
    /// <returns>List of mods that this mod depends on.</returns>
    IReadOnlyList<IModManifest> GetDependencies(string modId);
}

/// <summary>
/// Optional interface for mods that want to expose an API to other mods.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface in your mod entry class to provide a public API.
/// Other mods can then call <see cref="IModRegistry.GetApi{TApi}"/> to access it.
/// </para>
/// <para>
/// Example:
/// <code>
/// public class EconomyMod : IMod, IModApiProvider
/// {
///     public object GetApi() => new EconomyApi(this);
/// }
///
/// public interface IEconomyApi
/// {
///     void AddCurrency(Entity player, int amount);
///     int GetBalance(Entity player);
/// }
///
/// public class EconomyApi : IEconomyApi
/// {
///     private readonly EconomyMod _mod;
///     public EconomyApi(EconomyMod mod) => _mod = mod;
///
///     public void AddCurrency(Entity player, int amount) { ... }
///     public int GetBalance(Entity player) { ... }
/// }
/// </code>
/// </para>
/// </remarks>
public interface IModApiProvider
{
    /// <summary>
    /// Gets the API instance to expose to other mods.
    /// </summary>
    /// <returns>An object implementing one or more public API interfaces.</returns>
    /// <remarks>
    /// The returned object should implement well-defined interfaces that other mods can reference.
    /// Document your API interfaces thoroughly and maintain backward compatibility.
    /// </remarks>
    object GetApi();
}
