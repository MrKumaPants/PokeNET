using Arch.Core;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.ECS.Systems;

namespace PokeNET.Core.ECS;

/// <summary>
/// Concrete implementation of system lifecycle management.
/// Follows the Single Responsibility Principle - only manages system lifecycle.
/// Follows the Dependency Inversion Principle - depends on ISystem abstraction.
/// </summary>
public class SystemManager : ISystemManager
{
    private readonly ILogger<SystemManager> _logger;
    private readonly List<ISystem> _systems = new();
    private bool _initialized;
    private bool _disposed;

    /// <summary>
    /// Initializes a new system manager with logging support.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public SystemManager(ILogger<SystemManager> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void RegisterSystem(ISystem system)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SystemManager));

        if (_systems.Contains(system))
        {
            _logger.LogWarning("System {SystemType} already registered", system.GetType().Name);
            return;
        }

        _systems.Add(system);
        _systems.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        _logger.LogInformation("Registered system {SystemType} with priority {Priority}",
            system.GetType().Name, system.Priority);
    }

    /// <inheritdoc/>
    public void UnregisterSystem(ISystem system)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SystemManager));

        if (_systems.Remove(system))
        {
            _logger.LogInformation("Unregistered system {SystemType}", system.GetType().Name);
            system.Dispose();
        }
    }

    /// <inheritdoc/>
    public void InitializeSystems(World world)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SystemManager));

        if (_initialized)
        {
            _logger.LogWarning("Systems already initialized");
            return;
        }

        _logger.LogInformation("Initializing {Count} systems", _systems.Count);

        foreach (var system in _systems)
        {
            try
            {
                system.Initialize(world);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize system {SystemType}", system.GetType().Name);
                throw;
            }
        }

        _initialized = true;
        _logger.LogInformation("All systems initialized successfully");
    }

    /// <inheritdoc/>
    public void UpdateSystems(float deltaTime)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SystemManager));

        if (!_initialized)
        {
            _logger.LogWarning("Attempting to update systems before initialization");
            return;
        }

        foreach (var system in _systems)
        {
            if (system.IsEnabled)
            {
                system.Update(deltaTime);
            }
        }
    }

    /// <inheritdoc/>
    public T? GetSystem<T>() where T : class, ISystem
    {
        return _systems.OfType<T>().FirstOrDefault();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogInformation("Disposing system manager and {Count} systems", _systems.Count);

        foreach (var system in _systems)
        {
            try
            {
                system.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing system {SystemType}", system.GetType().Name);
            }
        }

        _systems.Clear();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
