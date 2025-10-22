using System.Diagnostics;

namespace PokeNET.Tests.Utilities;

/// <summary>
/// Helper utilities for memory-related testing and profiling.
/// </summary>
public static class MemoryTestHelpers
{
    /// <summary>
    /// Measures the memory allocated by an action.
    /// </summary>
    public static long MeasureAllocations(Action action)
    {
        // Force GC to get accurate baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var startMemory = GC.GetTotalMemory(false);

        action();

        var endMemory = GC.GetTotalMemory(false);

        return endMemory - startMemory;
    }

    /// <summary>
    /// Measures the execution time of an action.
    /// </summary>
    public static TimeSpan MeasureExecutionTime(Action action)
    {
        var stopwatch = Stopwatch.StartNew();
        action();
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    /// <summary>
    /// Measures both memory allocations and execution time.
    /// </summary>
    public static (long allocations, TimeSpan duration) MeasurePerformance(Action action)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var startMemory = GC.GetTotalMemory(false);
        var stopwatch = Stopwatch.StartNew();

        action();

        stopwatch.Stop();
        var endMemory = GC.GetTotalMemory(false);

        return (endMemory - startMemory, stopwatch.Elapsed);
    }

    /// <summary>
    /// Runs an action multiple times and returns average metrics.
    /// </summary>
    public static (long avgAllocations, TimeSpan avgDuration) BenchmarkAction(
        Action action,
        int iterations = 100)
    {
        var allocations = new List<long>();
        var durations = new List<TimeSpan>();

        // Warm-up
        for (int i = 0; i < 10; i++)
            action();

        // Actual measurements
        for (int i = 0; i < iterations; i++)
        {
            var (alloc, duration) = MeasurePerformance(action);
            allocations.Add(alloc);
            durations.Add(duration);
        }

        return (
            (long)allocations.Average(),
            TimeSpan.FromTicks((long)durations.Average(d => d.Ticks))
        );
    }
}
