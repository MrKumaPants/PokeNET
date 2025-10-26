using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Arch.Core;
using Microsoft.Extensions.Logging;
using PokeNET.Core.ECS.Systems;

namespace PokeNET.Core.ECS;

/// <summary>
/// Concrete implementation of system lifecycle management.
/// Follows the Single Responsibility Principle - only manages system lifecycle.
/// Follows the Dependency Inversion Principle - depends on ISystem abstraction.
/// Enhanced with performance metrics tracking for both SystemBase and SystemBaseEnhanced.
/// </summary>
public class SystemManager : ISystemManager
{
    private readonly ILogger<SystemManager> _logger;
    private readonly List<ISystem> _systems = new();
    private readonly Dictionary<ISystem, SystemMetrics> _systemMetrics = new();
    private readonly Stopwatch _frameStopwatch = new();
    private bool _initialized;
    private bool _disposed;
    private bool _metricsEnabled;

    /// <summary>
    /// Initializes a new system manager with logging support.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="enableMetrics">Whether to enable performance metrics tracking (default: false).</param>
    public SystemManager(ILogger<SystemManager> logger, bool enableMetrics = false)
    {
        _logger = logger;
        _metricsEnabled = enableMetrics;

        if (_metricsEnabled)
        {
            _logger.LogInformation("SystemManager metrics tracking enabled");
        }
    }

    /// <summary>
    /// Enable or disable performance metrics tracking at runtime.
    /// </summary>
    /// <param name="enabled">True to enable metrics, false to disable.</param>
    public void EnableMetrics(bool enabled)
    {
        if (_metricsEnabled == enabled)
            return;

        _metricsEnabled = enabled;

        if (!enabled)
        {
            _systemMetrics.Clear();
        }

        _logger.LogInformation(
            "SystemManager metrics tracking {Status}",
            enabled ? "enabled" : "disabled"
        );
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

        // Initialize metrics tracking for this system
        if (_metricsEnabled)
        {
            _systemMetrics[system] = new SystemMetrics
            {
                SystemName = system.GetType().Name,
                SystemType = system.GetType(),
            };
        }

        _logger.LogInformation(
            "Registered system {SystemType} with priority {Priority}",
            system.GetType().Name,
            system.Priority
        );
    }

    /// <inheritdoc/>
    public void UnregisterSystem(ISystem system)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SystemManager));

        if (_systems.Remove(system))
        {
            // Remove metrics tracking
            _systemMetrics.Remove(system);

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
                _logger.LogError(
                    ex,
                    "Failed to initialize system {SystemType}",
                    system.GetType().Name
                );
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

        // Track total frame time if metrics enabled
        if (_metricsEnabled)
        {
            _frameStopwatch.Restart();
        }

        foreach (var system in _systems)
        {
            if (!system.IsEnabled)
                continue;

            if (_metricsEnabled)
            {
                // Track per-system execution time
                var systemStopwatch = Stopwatch.StartNew();

                system.Update(deltaTime);

                systemStopwatch.Stop();

                // Update metrics
                if (_systemMetrics.TryGetValue(system, out var metrics))
                {
                    metrics.UpdateCount++;
                    metrics.LastUpdateTime = systemStopwatch.Elapsed.TotalMilliseconds;
                    metrics.TotalUpdateTime += systemStopwatch.Elapsed.TotalMilliseconds;
                }
            }
            else
            {
                // Fast path when metrics disabled
                system.Update(deltaTime);
            }
        }

        if (_metricsEnabled)
        {
            _frameStopwatch.Stop();
        }
    }

    /// <inheritdoc/>
    public T? GetSystem<T>()
        where T : class, ISystem
    {
        return _systems.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Get performance metrics for a specific system.
    /// Returns null if metrics are disabled or system not found.
    /// </summary>
    /// <param name="system">The system to get metrics for.</param>
    /// <returns>System metrics or null if not available.</returns>
    public SystemMetrics? GetSystemMetrics(ISystem system)
    {
        if (!_metricsEnabled)
        {
            _logger.LogWarning("Metrics tracking is disabled. Enable with EnableMetrics(true).");
            return null;
        }

        return _systemMetrics.TryGetValue(system, out var metrics) ? metrics : null;
    }

    /// <summary>
    /// Get performance metrics for all registered systems.
    /// Returns empty dictionary if metrics are disabled.
    /// </summary>
    /// <returns>Dictionary of system names to their metrics.</returns>
    public Dictionary<string, SystemMetrics> GetAllMetrics()
    {
        if (!_metricsEnabled)
        {
            _logger.LogWarning("Metrics tracking is disabled. Enable with EnableMetrics(true).");
            return new Dictionary<string, SystemMetrics>();
        }

        return _systemMetrics.Values.ToDictionary(m => m.SystemName, m => m);
    }

    /// <summary>
    /// Get total frame metrics across all systems.
    /// </summary>
    /// <returns>Frame metrics or null if metrics disabled.</returns>
    public FrameMetrics? GetTotalMetrics()
    {
        if (!_metricsEnabled)
        {
            _logger.LogWarning("Metrics tracking is disabled. Enable with EnableMetrics(true).");
            return null;
        }

        var totalUpdateTime = _systemMetrics.Values.Sum(m => m.LastUpdateTime);
        var systemCount = _systemMetrics.Count;
        var totalUpdates = _systemMetrics.Values.Sum(m => m.UpdateCount);

        return new FrameMetrics
        {
            LastFrameTime = _frameStopwatch.Elapsed.TotalMilliseconds,
            TotalSystemTime = totalUpdateTime,
            ActiveSystemCount = systemCount,
            TotalUpdateCount = totalUpdates,
        };
    }

    /// <summary>
    /// Reset all collected metrics.
    /// </summary>
    public void ResetMetrics()
    {
        if (!_metricsEnabled)
        {
            _logger.LogWarning("Metrics tracking is disabled.");
            return;
        }

        foreach (var metrics in _systemMetrics.Values)
        {
            metrics.UpdateCount = 0;
            metrics.TotalUpdateTime = 0;
            metrics.LastUpdateTime = 0;
        }

        _logger.LogInformation("All metrics reset");
    }

    /// <summary>
    /// Log current metrics to the logger.
    /// </summary>
    /// <param name="logLevel">The log level to use (default: Information).</param>
    public void LogMetrics(LogLevel logLevel = LogLevel.Information)
    {
        if (!_metricsEnabled)
        {
            _logger.LogWarning("Metrics tracking is disabled. Enable with EnableMetrics(true).");
            return;
        }

        var frameMetrics = GetTotalMetrics();
        if (frameMetrics == null)
            return;

        _logger.Log(logLevel, "=== System Performance Metrics ===");
        _logger.Log(
            logLevel,
            "Last Frame: {FrameTime:F2}ms | Total System Time: {TotalTime:F2}ms | Systems: {Count}",
            frameMetrics.LastFrameTime,
            frameMetrics.TotalSystemTime,
            frameMetrics.ActiveSystemCount
        );

        foreach (
            var (systemName, metrics) in GetAllMetrics()
                .OrderByDescending(m => m.Value.AverageUpdateTime)
        )
        {
            _logger.Log(
                logLevel,
                "  {SystemName}: Last={LastTime:F2}ms | Avg={AvgTime:F2}ms | Count={Count}",
                systemName,
                metrics.LastUpdateTime,
                metrics.AverageUpdateTime,
                metrics.UpdateCount
            );
        }
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
        _systemMetrics.Clear();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Performance metrics for an individual system.
/// Tracks execution time and update frequency.
/// </summary>
public class SystemMetrics
{
    /// <summary>
    /// Human-readable name of the system.
    /// </summary>
    public string SystemName { get; set; } = string.Empty;

    /// <summary>
    /// Type of the system.
    /// </summary>
    public Type SystemType { get; set; } = typeof(object);

    /// <summary>
    /// Time taken by the most recent update in milliseconds.
    /// </summary>
    public double LastUpdateTime { get; set; }

    /// <summary>
    /// Total accumulated update time in milliseconds.
    /// </summary>
    public double TotalUpdateTime { get; set; }

    /// <summary>
    /// Number of times the system has been updated.
    /// </summary>
    public long UpdateCount { get; set; }

    /// <summary>
    /// Average update time per frame in milliseconds.
    /// </summary>
    public double AverageUpdateTime => UpdateCount > 0 ? TotalUpdateTime / UpdateCount : 0;

    /// <summary>
    /// Percentage of total frame time consumed by this system.
    /// Must be calculated externally based on total frame time.
    /// </summary>
    public double PercentOfFrame { get; set; }
}

/// <summary>
/// Overall frame performance metrics.
/// Tracks total frame time and aggregate system statistics.
/// </summary>
public class FrameMetrics
{
    /// <summary>
    /// Total frame time in milliseconds (including overhead).
    /// </summary>
    public double LastFrameTime { get; set; }

    /// <summary>
    /// Sum of all system update times in milliseconds.
    /// </summary>
    public double TotalSystemTime { get; set; }

    /// <summary>
    /// Number of systems currently registered and tracked.
    /// </summary>
    public int ActiveSystemCount { get; set; }

    /// <summary>
    /// Total number of system updates across all systems.
    /// </summary>
    public long TotalUpdateCount { get; set; }

    /// <summary>
    /// Overhead time (frame time - system time) in milliseconds.
    /// </summary>
    public double OverheadTime => LastFrameTime - TotalSystemTime;

    /// <summary>
    /// Percentage of frame time spent in system updates.
    /// </summary>
    public double SystemTimePercentage =>
        LastFrameTime > 0 ? (TotalSystemTime / LastFrameTime) * 100 : 0;
}
