using System;
using System.Diagnostics;
using Arch.System;
using Microsoft.Extensions.Logging;

namespace PokeNET.Domain.ECS.Systems;

/// <summary>
/// Decorator that wraps any ISystem&lt;T&gt; to add performance metrics tracking.
/// Tracks update time, counts, and averages for any Arch.Extended system.
/// </summary>
/// <typeparam name="T">The state type passed to the system (typically float for delta time)</typeparam>
/// <example>
/// Usage example:
/// <code>
/// // Wrap an existing system with metrics tracking
/// var movementSystem = new MovementSystem(world, logger);
/// var metricsSystem = new SystemMetricsDecorator&lt;float&gt;(movementSystem, logger);
///
/// // Use it in your game loop
/// float deltaTime = 0.016f;
/// metricsSystem.Update(ref deltaTime);
///
/// // Access metrics
/// Console.WriteLine($"Last update: {metricsSystem.LastUpdateTime:F2}ms");
/// Console.WriteLine($"Average: {metricsSystem.AverageUpdateTime:F2}ms");
/// Console.WriteLine($"Updates: {metricsSystem.UpdateCount}");
///
/// // Get structured metrics
/// var metrics = metricsSystem.GetMetrics();
///
/// // Reset metrics for a new measurement period
/// metricsSystem.ResetMetrics();
/// </code>
/// </example>
public class SystemMetricsDecorator<T> : ISystem<T>
{
    private readonly ISystem<T> _innerSystem;
    private readonly ILogger? _logger;
    private readonly Stopwatch _stopwatch;
    private double _totalUpdateTime;
    private long _updateCount;

    /// <summary>
    /// Gets the duration of the last Update() call in milliseconds.
    /// </summary>
    public double LastUpdateTime { get; private set; }

    /// <summary>
    /// Gets the total number of Update() calls.
    /// </summary>
    public long UpdateCount => _updateCount;

    /// <summary>
    /// Gets the average update time across all Update() calls in milliseconds.
    /// </summary>
    public double AverageUpdateTime => _updateCount > 0 ? _totalUpdateTime / _updateCount : 0;

    /// <summary>
    /// Gets the total accumulated update time in milliseconds.
    /// </summary>
    public double TotalUpdateTime => _totalUpdateTime;

    /// <summary>
    /// Creates a new metrics decorator around an existing system.
    /// </summary>
    /// <param name="innerSystem">The system to wrap with metrics tracking</param>
    /// <param name="logger">Optional logger for performance warnings</param>
    public SystemMetricsDecorator(ISystem<T> innerSystem, ILogger? logger = null)
    {
        _innerSystem = innerSystem ?? throw new ArgumentNullException(nameof(innerSystem));
        _logger = logger;
        _stopwatch = new Stopwatch();
    }

    /// <inheritdoc />
    public void Initialize() => _innerSystem.Initialize();

    /// <inheritdoc />
    public void Dispose()
    {
        _innerSystem.Dispose();
        _logger?.LogInformation(
            "System {SystemName} disposed. Final metrics: {UpdateCount} updates, {AverageTime:F2}ms avg",
            _innerSystem.GetType().Name,
            _updateCount,
            AverageUpdateTime
        );
    }

    /// <inheritdoc />
    public void BeforeUpdate(in T t)
    {
        // Note: BeforeUpdate is called by the system framework, not by the decorator
        // The decorator only wraps Update() for metrics tracking
    }

    /// <inheritdoc />
    public void Update(in T t)
    {
        _stopwatch.Restart();

        try
        {
            _innerSystem.Update(in t);

            _stopwatch.Stop();
            LastUpdateTime = _stopwatch.Elapsed.TotalMilliseconds;
            _totalUpdateTime += LastUpdateTime;
            _updateCount++;

            // Warn on slow frames (>16.67ms = below 60 FPS)
            if (LastUpdateTime > 16.67)
            {
                _logger?.LogWarning(
                    "System {SystemName} took {UpdateTime:F2}ms (above 16.67ms threshold for 60 FPS)",
                    _innerSystem.GetType().Name,
                    LastUpdateTime
                );
            }
        }
        catch (Exception ex)
        {
            _stopwatch.Stop();
            _logger?.LogError(
                ex,
                "Error in system {SystemName} after {ElapsedTime:F2}ms",
                _innerSystem.GetType().Name,
                _stopwatch.Elapsed.TotalMilliseconds
            );
            throw;
        }
    }

    /// <inheritdoc />
    public void AfterUpdate(in T t)
    {
        // Note: AfterUpdate is called by the system framework, not by the decorator
        // The decorator only wraps Update() for metrics tracking
    }

    /// <summary>
    /// Resets all accumulated metrics to zero.
    /// Useful for starting a new measurement period.
    /// </summary>
    public void ResetMetrics()
    {
        _totalUpdateTime = 0;
        _updateCount = 0;
        LastUpdateTime = 0;
        _logger?.LogDebug("Metrics reset for system {SystemName}", _innerSystem.GetType().Name);
    }

    /// <summary>
    /// Gets a structured snapshot of current metrics.
    /// </summary>
    /// <returns>A SystemMetrics record containing all current metrics</returns>
    public SystemMetrics GetMetrics()
    {
        return new SystemMetrics
        {
            SystemName = _innerSystem.GetType().Name,
            UpdateCount = _updateCount,
            LastUpdateTime = LastUpdateTime,
            AverageUpdateTime = AverageUpdateTime,
            TotalUpdateTime = _totalUpdateTime,
        };
    }

    /// <summary>
    /// Gets the wrapped inner system.
    /// Useful for accessing system-specific functionality.
    /// </summary>
    /// <returns>The underlying system instance</returns>
    public ISystem<T> GetInnerSystem() => _innerSystem;
}
