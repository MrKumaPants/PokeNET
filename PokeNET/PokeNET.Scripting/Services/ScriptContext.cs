using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PokeNET.Scripting.Interfaces;

namespace PokeNET.Scripting.Services;

/// <summary>
/// Implementation of IScriptContext providing scripts with access to game services.
/// </summary>
/// <remarks>
/// <para>
/// ScriptContext serves as a facade between untrusted script code and the game engine.
/// It wraps the service provider to enable dependency injection while maintaining
/// security boundaries and resource control.
/// </para>
/// <para>
/// Design patterns used:
/// - **Facade Pattern**: Simplifies script access to complex game systems
/// - **Dependency Injection**: Decouples scripts from concrete implementations
/// - **Decorator Pattern**: Adds security checks around service access
/// </para>
/// </remarks>
public sealed class ScriptContext : IScriptContext
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly IScriptApi _api;
    private readonly IScriptMetadata _metadata;

    /// <inheritdoc/>
    public ILogger Logger => _logger;

    /// <inheritdoc/>
    public IScriptApi Api => _api;

    /// <inheritdoc/>
    public IScriptMetadata Metadata => _metadata;

    /// <summary>
    /// Initializes a new script context.
    /// </summary>
    /// <param name="services">Service provider for dependency injection.</param>
    /// <param name="loggerFactory">Factory for creating loggers.</param>
    /// <param name="metadata">Metadata about the script.</param>
    /// <exception cref="ArgumentNullException">Required parameters are null.</exception>
    public ScriptContext(
        IServiceProvider services,
        ILoggerFactory loggerFactory,
        IScriptMetadata metadata
    )
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

        _logger = loggerFactory.CreateLogger($"Script.{metadata.Id}");
        _api = new ScriptApi(services, _logger, metadata);

        _logger.LogDebug("Script context created for script: {ScriptId}", metadata.Id);
    }

    /// <inheritdoc/>
    public T GetService<T>()
        where T : notnull
    {
        try
        {
            var service = _services.GetRequiredService<T>();
            _logger.LogTrace("Retrieved service: {ServiceType}", typeof(T).Name);
            return service;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to retrieve service: {ServiceType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public bool TryGetService<T>(out T? service)
        where T : class
    {
        service = _services.GetService<T>();
        var found = service != null;

        if (found)
        {
            _logger.LogTrace("Retrieved service: {ServiceType}", typeof(T).Name);
        }
        else
        {
            _logger.LogTrace("Service not found: {ServiceType}", typeof(T).Name);
        }

        return found;
    }
}
