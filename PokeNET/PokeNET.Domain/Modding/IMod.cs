using System.Threading;
using System.Threading.Tasks;

namespace PokeNET.Domain.Modding;

/// <summary>
/// Core interface that all PokeNET mods must implement.
/// This is the entry point for mod functionality.
/// </summary>
/// <remarks>
/// <para>
/// Mods should implement this interface in their entry class, which must be
/// specified in the mod manifest (modinfo.json) via the "entryPoint" field.
/// </para>
/// <para>
/// The mod lifecycle is:
/// 1. Constructor called (dependency injection available)
/// 2. <see cref="InitializeAsync"/> called with context
/// 3. Mod is active during gameplay
/// 4. <see cref="ShutdownAsync"/> called on game exit or mod unload
/// </para>
/// <para>
/// Harmony patches (if any) are applied automatically after <see cref="InitializeAsync"/>
/// returns successfully.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyModEntry : IMod
/// {
///     private readonly ILogger&lt;MyModEntry&gt; _logger;
///
///     public MyModEntry(ILogger&lt;MyModEntry&gt; logger)
///     {
///         _logger = logger;
///     }
///
///     public async Task InitializeAsync(IModContext context, CancellationToken cancellationToken)
///     {
///         _logger.LogInformation("MyMod initializing...");
///
///         // Load custom data
///         var creatures = await context.Assets.LoadDataAsync&lt;CreatureData&gt;("creatures.json", cancellationToken);
///
///         // Register event handlers
///         context.Events.OnBattleStart += OnBattleStart;
///
///         _logger.LogInformation("MyMod initialized successfully");
///     }
///
///     public Task ShutdownAsync(CancellationToken cancellationToken)
///     {
///         _logger.LogInformation("MyMod shutting down...");
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface IMod
{
    /// <summary>
    /// Called when the mod is being initialized during game startup.
    /// </summary>
    /// <param name="context">
    /// The mod context providing access to game systems, assets, logging, and other services.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token to abort initialization if requested (e.g., user cancels mod loading).
    /// </param>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    /// <exception cref="ModInitializationException">
    /// Thrown if initialization fails. The mod will not be loaded, and Harmony patches will not be applied.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is called after all mod dependencies have been loaded and initialized.
    /// </para>
    /// <para>
    /// Initialization should be fast (&lt; 5 seconds). Heavy operations should be deferred
    /// to background tasks or loaded on-demand.
    /// </para>
    /// <para>
    /// Any unhandled exceptions will cause the mod to fail loading. Use try-catch and
    /// log errors appropriately.
    /// </para>
    /// </remarks>
    Task InitializeAsync(IModContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the mod is being shut down (game exit or mod unload).
    /// </summary>
    /// <param name="cancellationToken">
    /// Cancellation token to abort shutdown if requested.
    /// </param>
    /// <returns>A task representing the asynchronous shutdown operation.</returns>
    /// <remarks>
    /// <para>
    /// This is the place to clean up resources, unsubscribe from events, save state, etc.
    /// </para>
    /// <para>
    /// Harmony patches are automatically removed after this method completes.
    /// </para>
    /// <para>
    /// Shutdown should complete quickly (&lt; 2 seconds). Long-running cleanup may be forcibly terminated.
    /// </para>
    /// </remarks>
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Optional interface for mods that need to perform configuration validation or setup
/// before full initialization.
/// </summary>
/// <remarks>
/// Implement this interface if your mod needs to:
/// - Validate configuration files before loading
/// - Perform compatibility checks with other mods
/// - Display warnings or errors before initialization
/// </remarks>
public interface IModConfigurable
{
    /// <summary>
    /// Called before <see cref="IMod.InitializeAsync"/> to allow configuration validation.
    /// </summary>
    /// <param name="context">Mod context with limited access (no game systems available yet).</param>
    /// <returns>True if configuration is valid and mod should initialize; false to skip mod loading.</returns>
    /// <remarks>
    /// If this method returns false, the mod will not be loaded and no error will be shown.
    /// Use the context logger to explain why the mod was skipped.
    /// </remarks>
    Task<bool> ValidateConfigurationAsync(IModContext context);
}

/// <summary>
/// Optional interface for mods that need to perform post-initialization setup after
/// all mods have been loaded.
/// </summary>
/// <remarks>
/// Implement this interface if your mod needs to:
/// - Interact with systems from other mods
/// - Perform cross-mod compatibility patches
/// - Register callbacks that depend on other mods being loaded
/// </remarks>
public interface IModPostInitialize
{
    /// <summary>
    /// Called after all mods have been initialized successfully.
    /// </summary>
    /// <param name="context">Full mod context with access to all loaded mods.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the post-initialization operation.</returns>
    /// <remarks>
    /// This is guaranteed to be called after all mods' <see cref="IMod.InitializeAsync"/> methods
    /// have completed successfully.
    /// </remarks>
    Task PostInitializeAsync(IModContext context, CancellationToken cancellationToken = default);
}
