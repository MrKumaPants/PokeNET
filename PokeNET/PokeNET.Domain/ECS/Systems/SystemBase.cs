using Arch.Core;
using Microsoft.Extensions.Logging;

namespace PokeNET.Domain.ECS.Systems;

/// <summary>
/// Abstract base class for ECS systems providing common functionality.
/// Follows the Open/Closed Principle - open for extension, closed for modification.
/// Follows the Dependency Inversion Principle - depends on abstractions (ILogger).
/// </summary>
public abstract class SystemBase : ISystem
{
    protected readonly ILogger Logger;
    private World? _world;

    /// <summary>
    /// Gets the world associated with this system.
    /// Throws InvalidOperationException if accessed before Initialize() is called.
    /// </summary>
    protected World World
    {
        get => _world ?? throw new InvalidOperationException(
            $"System '{GetType().Name}' not initialized. Call Initialize() before accessing World.");
        private set => _world = value;
    }

    /// <inheritdoc/>
    public virtual int Priority => 0;

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Initializes the system base with logging support.
    /// </summary>
    /// <param name="logger">Logger instance for this system.</param>
    protected SystemBase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public virtual void Initialize(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        World = world;
        Logger.LogInformation("System {SystemName} initialized", GetType().Name);
        OnInitialize();
    }

    /// <summary>
    /// Override this method to provide custom initialization logic.
    /// </summary>
    protected virtual void OnInitialize()
    {
    }

    /// <inheritdoc/>
    public void Update(float deltaTime)
    {
        if (!IsEnabled)
            return;

        try
        {
            OnUpdate(deltaTime);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in system {SystemName} update", GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// Override this method to provide custom update logic.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds.</param>
    protected abstract void OnUpdate(float deltaTime);

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        Logger.LogInformation("System {SystemName} disposed", GetType().Name);
        OnDispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Override this method to provide custom disposal logic.
    /// </summary>
    protected virtual void OnDispose()
    {
    }
}
