namespace PokeNET.ModAPI.Interfaces;

/// <summary>
/// Main entry point for the PokeNET modding API.
/// Provides access to all mod subsystems and services.
/// </summary>
/// <remarks>
/// This interface is injected into mod initialization and provides stable access
/// to game systems. Use this to interact with entities, assets, events, and world data.
/// </remarks>
/// <example>
/// <code>
/// public class MyMod : IMod
/// {
///     public void Initialize(IModApi api)
///     {
///         // Spawn a custom entity
///         var entity = api.EntityApi.SpawnEntity(new EntityDefinition
///         {
///             Name = "CustomPokemon",
///             Tag = "pokemon"
///         });
///
///         // Subscribe to events
///         api.EventApi.Subscribe&lt;EntitySpawnedEvent&gt;(OnEntitySpawned);
///
///         // Log initialization
///         api.Logger.Info("MyMod initialized successfully!");
///     }
/// }
/// </code>
/// </example>
public interface IModApi
{
    /// <summary>
    /// Access to entity creation, destruction, and component management.
    /// </summary>
    IEntityApi EntityApi { get; }

    /// <summary>
    /// Access to asset loading, registration, and retrieval.
    /// </summary>
    IAssetApi AssetApi { get; }

    /// <summary>
    /// Access to the event subscription and publishing system.
    /// </summary>
    IEventApi EventApi { get; }

    /// <summary>
    /// Access to world queries and ECS operations.
    /// </summary>
    IWorldApi WorldApi { get; }

    /// <summary>
    /// Logger instance scoped to the current mod.
    /// </summary>
    ILogger Logger { get; }
}
