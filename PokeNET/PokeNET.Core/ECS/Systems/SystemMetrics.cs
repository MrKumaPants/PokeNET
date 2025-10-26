namespace PokeNET.Core.ECS.Systems;

/// <summary>
/// Immutable snapshot of system performance metrics.
/// </summary>
/// <remarks>
/// This record is used to capture and report performance statistics
/// from systems wrapped with SystemMetricsDecorator.
/// </remarks>
public record SystemMetrics
{
    /// <summary>
    /// Gets the name of the system being measured.
    /// </summary>
    public required string SystemName { get; init; }

    /// <summary>
    /// Gets the total number of Update() calls since metrics were last reset.
    /// </summary>
    public required long UpdateCount { get; init; }

    /// <summary>
    /// Gets the duration of the most recent Update() call in milliseconds.
    /// </summary>
    public required double LastUpdateTime { get; init; }

    /// <summary>
    /// Gets the average update time across all Update() calls in milliseconds.
    /// </summary>
    public required double AverageUpdateTime { get; init; }

    /// <summary>
    /// Gets the total accumulated time spent in Update() calls in milliseconds.
    /// </summary>
    public required double TotalUpdateTime { get; init; }

    /// <summary>
    /// Gets the estimated frames per second based on average update time.
    /// Assumes the system is the only bottleneck.
    /// </summary>
    public double EstimatedFps => AverageUpdateTime > 0 ? 1000.0 / AverageUpdateTime : 0;

    /// <summary>
    /// Gets whether the average update time meets 60 FPS target (16.67ms or less).
    /// </summary>
    public bool Meets60FpsTarget => AverageUpdateTime <= 16.67;

    /// <summary>
    /// Gets whether the average update time meets 30 FPS target (33.33ms or less).
    /// </summary>
    public bool Meets30FpsTarget => AverageUpdateTime <= 33.33;

    /// <summary>
    /// Returns a formatted string representation of the metrics.
    /// </summary>
    public override string ToString()
    {
        return $"{SystemName}: {UpdateCount} updates, "
            + $"Last: {LastUpdateTime:F2}ms, "
            + $"Avg: {AverageUpdateTime:F2}ms, "
            + $"Total: {TotalUpdateTime:F2}ms, "
            + $"Est FPS: {EstimatedFps:F1}";
    }
}
