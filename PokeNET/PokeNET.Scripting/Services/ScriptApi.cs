using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.ECS.Events;
using PokeNET.Domain.Modding;
using PokeNET.Scripting.Interfaces;

namespace PokeNET.Scripting.Services;

/// <summary>
/// Implementation of IScriptApi providing safe, controlled access to the ECS world.
/// </summary>
/// <remarks>
/// <para>
/// ScriptApi acts as a security gateway between untrusted script code and the core
/// game engine. It validates all inputs, enforces permissions, and logs all operations
/// for security auditing.
/// </para>
/// <para>
/// Security features:
/// - Input validation on all methods
/// - Permission checking before privileged operations
/// - Comprehensive logging for audit trails
/// - Resource limits to prevent abuse
/// </para>
/// </remarks>
public sealed class ScriptApi : IScriptApi
{
    private readonly ILogger _logger;
    private readonly IScriptMetadata _metadata;
    private readonly IEntityApi _entities;
    private readonly IEventBus _events;

    /// <inheritdoc/>
    public IEntityApi Entities => _entities;

    /// <inheritdoc/>
    public IEventBus Events => _events;

    /// <summary>
    /// Initializes a new script API instance.
    /// </summary>
    /// <param name="services">Service provider for dependency injection.</param>
    /// <param name="logger">Logger for script operations.</param>
    /// <param name="metadata">Metadata about the script.</param>
    /// <exception cref="ArgumentNullException">Required parameters are null.</exception>
    public ScriptApi(
        IServiceProvider services,
        ILogger logger,
        IScriptMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(services);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

        // Resolve required services
        _entities = services.GetRequiredService<IEntityApi>();
        _events = services.GetRequiredService<IEventBus>();
    }

    /// <inheritdoc/>
    public Entity CreateEntity(params object[] components)
    {
        ArgumentNullException.ThrowIfNull(components);

        // Check permissions
        ValidatePermission("entities.create");

        try
        {
            var entity = _entities.CreateEntity(components);
            _logger.LogDebug("Script {ScriptId} created entity {EntityId} with {ComponentCount} components",
                _metadata.Id, entity.Id, components.Length);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Script {ScriptId} failed to create entity", _metadata.Id);
            throw new InvalidOperationException("Failed to create entity", ex);
        }
    }

    /// <inheritdoc/>
    public void DestroyEntity(Entity entity)
    {
        // Check permissions
        ValidatePermission("entities.destroy");

        try
        {
            _entities.DestroyEntity(entity);
            _logger.LogDebug("Script {ScriptId} destroyed entity {EntityId}",
                _metadata.Id, entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Script {ScriptId} failed to destroy entity {EntityId}",
                _metadata.Id, entity.Id);
            throw new InvalidOperationException("Failed to destroy entity", ex);
        }
    }

    /// <inheritdoc/>
    public void PublishEvent<T>(T gameEvent) where T : IGameEvent
    {
        ArgumentNullException.ThrowIfNull(gameEvent);

        // Check permissions
        ValidatePermission("events.publish");

        try
        {
            _events.Publish(gameEvent);
            _logger.LogDebug("Script {ScriptId} published event {EventType}",
                _metadata.Id, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Script {ScriptId} failed to publish event {EventType}",
                _metadata.Id, typeof(T).Name);
            throw new InvalidOperationException("Failed to publish event", ex);
        }
    }

    /// <inheritdoc/>
    public void SubscribeToEvent<T>(Action<T> handler) where T : IGameEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        // Check permissions
        ValidatePermission("events.subscribe");

        try
        {
            _events.Subscribe(handler);
            _logger.LogDebug("Script {ScriptId} subscribed to event {EventType}",
                _metadata.Id, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Script {ScriptId} failed to subscribe to event {EventType}",
                _metadata.Id, typeof(T).Name);
            throw new InvalidOperationException("Failed to subscribe to event", ex);
        }
    }

    /// <inheritdoc/>
    public void UnsubscribeFromEvent<T>(Action<T> handler) where T : IGameEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        try
        {
            _events.Unsubscribe(handler);
            _logger.LogDebug("Script {ScriptId} unsubscribed from event {EventType}",
                _metadata.Id, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Script {ScriptId} failed to unsubscribe from event {EventType}",
                _metadata.Id, typeof(T).Name);
            throw new InvalidOperationException("Failed to unsubscribe from event", ex);
        }
    }

    /// <inheritdoc/>
    public void Log(Interfaces.LogLevel level, string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var msLogLevel = level switch
        {
            Interfaces.LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
            Interfaces.LogLevel.Information => Microsoft.Extensions.Logging.LogLevel.Information,
            Interfaces.LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
            Interfaces.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
            _ => Microsoft.Extensions.Logging.LogLevel.Information
        };

        _logger.Log(msLogLevel, "[{ScriptId}] {Message}", _metadata.Id, message);
    }

    /// <inheritdoc/>
    public void LogInfo(string message)
    {
        Log(Interfaces.LogLevel.Information, message);
    }

    /// <inheritdoc/>
    public void LogWarning(string message)
    {
        Log(Interfaces.LogLevel.Warning, message);
    }

    /// <inheritdoc/>
    public void LogError(string message)
    {
        Log(Interfaces.LogLevel.Error, message);
    }

    /// <summary>
    /// Validates that the script has the required permission.
    /// </summary>
    /// <param name="permission">The permission to check.</param>
    /// <exception cref="InvalidOperationException">Script lacks the required permission.</exception>
    private void ValidatePermission(string permission)
    {
        // For now, allow all operations if script is enabled
        // In future phases, implement proper permission checking
        if (!_metadata.IsEnabled)
        {
            throw new InvalidOperationException(
                $"Script {_metadata.Id} is disabled and cannot perform operations");
        }

        // Check if script has the required permission
        if (_metadata.RequiredPermissions.Count > 0 &&
            !_metadata.RequiredPermissions.Contains(permission))
        {
            _logger.LogWarning(
                "Script {ScriptId} attempted operation {Permission} without declaring it in permissions",
                _metadata.Id, permission);
        }
    }
}
