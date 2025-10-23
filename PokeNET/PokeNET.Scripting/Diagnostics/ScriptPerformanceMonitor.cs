using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PokeNET.Scripting.Diagnostics
{
    /// <summary>
    /// Comprehensive performance monitoring and diagnostics for script execution.
    /// Tracks compilation time, execution time, memory usage, GC pressure, and provides
    /// profiling tools with performance budget enforcement.
    /// </summary>
    public sealed class ScriptPerformanceMonitor
    {
        private readonly ILogger<ScriptPerformanceMonitor> _logger;
        private readonly ConcurrentDictionary<string, PerformanceMetrics> _activeMetrics = new();
        private readonly ConcurrentDictionary<string, List<PerformanceMetrics>> _historicalMetrics = new();
        private readonly PerformanceBudget _budget;
        private readonly bool _enableProfiling;

        // Profiling data
        private readonly ConcurrentDictionary<string, ExecutionProfile> _executionProfiles = new();
        private readonly ConcurrentDictionary<string, MemoryProfile> _memoryProfiles = new();

        public ScriptPerformanceMonitor(
            ILogger<ScriptPerformanceMonitor> logger,
            PerformanceBudget? budget = null,
            bool enableProfiling = false)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _budget = budget ?? PerformanceBudget.Moderate();
            _enableProfiling = enableProfiling;
        }

        /// <summary>
        /// Starts monitoring a script operation.
        /// </summary>
        public PerformanceMetrics StartMonitoring(string scriptName)
        {
            var metrics = new PerformanceMetrics(scriptName);
            metrics.Start();

            if (!_activeMetrics.TryAdd(scriptName, metrics))
            {
                _logger.LogWarning("Script '{ScriptName}' is already being monitored. Returning existing metrics.", scriptName);
                return _activeMetrics[scriptName];
            }

            if (_enableProfiling)
            {
                _executionProfiles[scriptName] = new ExecutionProfile(scriptName);
                _memoryProfiles[scriptName] = new MemoryProfile(scriptName);
            }

            _logger.LogDebug("Started performance monitoring for script '{ScriptName}'", scriptName);
            return metrics;
        }

        /// <summary>
        /// Stops monitoring and validates against budget.
        /// </summary>
        public PerformanceReport StopMonitoring(string scriptName)
        {
            if (!_activeMetrics.TryRemove(scriptName, out var metrics))
            {
                _logger.LogWarning("No active monitoring found for script '{ScriptName}'", scriptName);
                return new PerformanceReport(scriptName, null, null, null, null);
            }

            metrics.End();

            // Store in historical data
            _historicalMetrics.AddOrUpdate(scriptName,
                _ => new List<PerformanceMetrics> { metrics },
                (_, list) =>
                {
                    list.Add(metrics);
                    // Keep last 100 executions
                    if (list.Count > 100)
                    {
                        list.RemoveAt(0);
                    }
                    return list;
                });

            // Validate against budget
            var budgetReport = _budget.Validate(metrics);
            if (budgetReport.HasViolations)
            {
                budgetReport.LogViolations(_logger);
            }

            // Get profiling data if enabled
            ExecutionProfile? execProfile = null;
            MemoryProfile? memProfile = null;

            if (_enableProfiling)
            {
                _executionProfiles.TryRemove(scriptName, out execProfile);
                _memoryProfiles.TryRemove(scriptName, out memProfile);
            }

            var report = new PerformanceReport(scriptName, metrics, budgetReport, execProfile, memProfile);

            _logger.LogInformation("Performance monitoring completed for '{ScriptName}': {TotalTime}ms, {Memory}",
                scriptName, metrics.TotalTime.TotalMilliseconds, FormatBytes(metrics.TotalMemoryAllocated));

            return report;
        }

        /// <summary>
        /// Gets current metrics for an active script.
        /// </summary>
        public PerformanceMetrics? GetActiveMetrics(string scriptName)
        {
            return _activeMetrics.TryGetValue(scriptName, out var metrics) ? metrics : null;
        }

        /// <summary>
        /// Records a custom profiling sample.
        /// </summary>
        public void RecordProfileSample(string scriptName, string operation, TimeSpan duration, long memoryDelta)
        {
            if (!_enableProfiling) return;

            if (_executionProfiles.TryGetValue(scriptName, out var execProfile))
            {
                execProfile.RecordSample(operation, duration);
            }

            if (_memoryProfiles.TryGetValue(scriptName, out var memProfile))
            {
                memProfile.RecordAllocation(operation, memoryDelta);
            }
        }

        /// <summary>
        /// Creates a profiling scope for automatic measurement.
        /// </summary>
        public IDisposable ProfileOperation(string scriptName, string operation)
        {
            if (!_enableProfiling)
            {
                return new DisposableScope(() => { });
            }

            return new ProfilingScope(this, scriptName, operation);
        }

        /// <summary>
        /// Gets historical statistics for a script.
        /// </summary>
        public ScriptStatistics? GetStatistics(string scriptName)
        {
            if (!_historicalMetrics.TryGetValue(scriptName, out var history) || !history.Any())
            {
                return null;
            }

            return new ScriptStatistics(scriptName, history);
        }

        /// <summary>
        /// Generates a comprehensive performance report for all monitored scripts.
        /// </summary>
        public string GenerateFullReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Script Performance Monitoring Report");
            sb.AppendLine("=====================================");
            sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();

            if (_activeMetrics.Any())
            {
                sb.AppendLine($"Active Monitoring ({_activeMetrics.Count}):");
                foreach (var kvp in _activeMetrics.OrderBy(x => x.Key))
                {
                    sb.AppendLine($"  • {kvp.Key}: Running for {kvp.Value.TotalTime.TotalSeconds:F2}s");
                }
                sb.AppendLine();
            }

            if (_historicalMetrics.Any())
            {
                sb.AppendLine($"Historical Data ({_historicalMetrics.Count} scripts):");
                foreach (var kvp in _historicalMetrics.OrderBy(x => x.Key))
                {
                    var stats = new ScriptStatistics(kvp.Key, kvp.Value);
                    sb.AppendLine($"\n{stats.GetSummary()}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Clears historical data.
        /// </summary>
        public void ClearHistory()
        {
            _historicalMetrics.Clear();
            _logger.LogInformation("Cleared performance monitoring history");
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private sealed class ProfilingScope : IDisposable
        {
            private readonly ScriptPerformanceMonitor _monitor;
            private readonly string _scriptName;
            private readonly string _operation;
            private readonly Stopwatch _stopwatch;
            private readonly long _startMemory;

            public ProfilingScope(ScriptPerformanceMonitor monitor, string scriptName, string operation)
            {
                _monitor = monitor;
                _scriptName = scriptName;
                _operation = operation;
                _startMemory = GC.GetTotalMemory(false);
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                var endMemory = GC.GetTotalMemory(false);
                var memoryDelta = endMemory - _startMemory;
                _monitor.RecordProfileSample(_scriptName, _operation, _stopwatch.Elapsed, memoryDelta);
            }
        }

        private sealed class DisposableScope : IDisposable
        {
            private readonly Action _onDispose;

            public DisposableScope(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose() => _onDispose();
        }
    }

    /// <summary>
    /// Execution profiling data for a script.
    /// </summary>
    public sealed class ExecutionProfile
    {
        private readonly ConcurrentDictionary<string, List<TimeSpan>> _samples = new();

        public string ScriptName { get; }

        public ExecutionProfile(string scriptName)
        {
            ScriptName = scriptName;
        }

        public void RecordSample(string operation, TimeSpan duration)
        {
            _samples.AddOrUpdate(operation,
                _ => new List<TimeSpan> { duration },
                (_, list) =>
                {
                    list.Add(duration);
                    return list;
                });
        }

        public Dictionary<string, OperationStats> GetStatistics()
        {
            return _samples.ToDictionary(
                kvp => kvp.Key,
                kvp => new OperationStats(kvp.Value));
        }
    }

    /// <summary>
    /// Memory profiling data for a script.
    /// </summary>
    public sealed class MemoryProfile
    {
        private readonly ConcurrentDictionary<string, List<long>> _allocations = new();

        public string ScriptName { get; }

        public MemoryProfile(string scriptName)
        {
            ScriptName = scriptName;
        }

        public void RecordAllocation(string operation, long bytes)
        {
            _allocations.AddOrUpdate(operation,
                _ => new List<long> { bytes },
                (_, list) =>
                {
                    list.Add(bytes);
                    return list;
                });
        }

        public Dictionary<string, MemoryStats> GetStatistics()
        {
            return _allocations.ToDictionary(
                kvp => kvp.Key,
                kvp => new MemoryStats(kvp.Value));
        }
    }

    /// <summary>
    /// Statistics for operation timing.
    /// </summary>
    public sealed class OperationStats
    {
        public int SampleCount { get; }
        public TimeSpan Average { get; }
        public TimeSpan Min { get; }
        public TimeSpan Max { get; }
        public TimeSpan Median { get; }
        public TimeSpan P95 { get; }
        public TimeSpan P99 { get; }

        public OperationStats(List<TimeSpan> samples)
        {
            SampleCount = samples.Count;
            if (SampleCount == 0) return;

            var sorted = samples.OrderBy(s => s.TotalMilliseconds).ToList();
            Average = TimeSpan.FromMilliseconds(samples.Average(s => s.TotalMilliseconds));
            Min = sorted.First();
            Max = sorted.Last();
            Median = sorted[SampleCount / 2];
            P95 = sorted[(int)(SampleCount * 0.95)];
            P99 = sorted[(int)(SampleCount * 0.99)];
        }
    }

    /// <summary>
    /// Statistics for memory allocations.
    /// </summary>
    public sealed class MemoryStats
    {
        public int SampleCount { get; }
        public long Average { get; }
        public long Min { get; }
        public long Max { get; }
        public long Total { get; }

        public MemoryStats(List<long> samples)
        {
            SampleCount = samples.Count;
            if (SampleCount == 0) return;

            Average = (long)samples.Average();
            Min = samples.Min();
            Max = samples.Max();
            Total = samples.Sum();
        }
    }

    /// <summary>
    /// Statistical analysis of script performance over time.
    /// </summary>
    public sealed class ScriptStatistics
    {
        public string ScriptName { get; }
        public int ExecutionCount { get; }
        public TimeSpan AverageCompilationTime { get; }
        public TimeSpan AverageExecutionTime { get; }
        public TimeSpan AverageTotalTime { get; }
        public long AverageMemoryUsage { get; }
        public double AverageGCCollections { get; }
        public DateTime FirstExecution { get; }
        public DateTime LastExecution { get; }

        public ScriptStatistics(string scriptName, List<PerformanceMetrics> history)
        {
            ScriptName = scriptName;
            ExecutionCount = history.Count;

            if (ExecutionCount > 0)
            {
                AverageCompilationTime = TimeSpan.FromMilliseconds(
                    history.Average(m => m.CompilationTime.TotalMilliseconds));
                AverageExecutionTime = TimeSpan.FromMilliseconds(
                    history.Average(m => m.ExecutionTime.TotalMilliseconds));
                AverageTotalTime = TimeSpan.FromMilliseconds(
                    history.Average(m => m.TotalTime.TotalMilliseconds));
                AverageMemoryUsage = (long)history.Average(m => m.TotalMemoryAllocated);
                AverageGCCollections = history.Average(m => m.TotalGCCollections);
                FirstExecution = history.First().StartTime;
                LastExecution = history.Last().StartTime;
            }
        }

        public string GetSummary()
        {
            return $@"{ScriptName}:
  Executions: {ExecutionCount}
  Avg Compilation: {AverageCompilationTime.TotalMilliseconds:F2} ms
  Avg Execution: {AverageExecutionTime.TotalMilliseconds:F2} ms
  Avg Total: {AverageTotalTime.TotalMilliseconds:F2} ms
  Avg Memory: {FormatBytes(AverageMemoryUsage)}
  Avg GC: {AverageGCCollections:F1} collections
  Period: {FirstExecution:yyyy-MM-dd} to {LastExecution:yyyy-MM-dd}";
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// Comprehensive performance report.
    /// </summary>
    public sealed class PerformanceReport
    {
        public string ScriptName { get; }
        public PerformanceMetrics? Metrics { get; }
        public BudgetViolationReport? BudgetReport { get; }
        public ExecutionProfile? ExecutionProfile { get; }
        public MemoryProfile? MemoryProfile { get; }

        public PerformanceReport(
            string scriptName,
            PerformanceMetrics? metrics,
            BudgetViolationReport? budgetReport,
            ExecutionProfile? executionProfile,
            MemoryProfile? memoryProfile)
        {
            ScriptName = scriptName;
            Metrics = metrics;
            BudgetReport = budgetReport;
            ExecutionProfile = executionProfile;
            MemoryProfile = memoryProfile;
        }

        public string GenerateReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Performance Report for '{ScriptName}'");
            sb.AppendLine(new string('=', 60));

            if (Metrics != null)
            {
                sb.AppendLine(Metrics.GetSummary());
            }

            if (BudgetReport != null && BudgetReport.HasViolations)
            {
                sb.AppendLine();
                sb.AppendLine(BudgetReport.GetReport());
            }

            if (ExecutionProfile != null)
            {
                var stats = ExecutionProfile.GetStatistics();
                if (stats.Any())
                {
                    sb.AppendLine("\nExecution Profile:");
                    foreach (var kvp in stats.OrderByDescending(s => s.Value.Average))
                    {
                        sb.AppendLine($"  {kvp.Key}:");
                        sb.AppendLine($"    Avg: {kvp.Value.Average.TotalMilliseconds:F2} ms");
                        sb.AppendLine($"    P95: {kvp.Value.P95.TotalMilliseconds:F2} ms");
                        sb.AppendLine($"    Samples: {kvp.Value.SampleCount}");
                    }
                }
            }

            if (MemoryProfile != null)
            {
                var stats = MemoryProfile.GetStatistics();
                if (stats.Any())
                {
                    sb.AppendLine("\nMemory Profile:");
                    foreach (var kvp in stats.OrderByDescending(s => s.Value.Total))
                    {
                        sb.AppendLine($"  {kvp.Key}:");
                        sb.AppendLine($"    Total: {FormatBytes(kvp.Value.Total)}");
                        sb.AppendLine($"    Avg: {FormatBytes(kvp.Value.Average)}");
                        sb.AppendLine($"    Samples: {kvp.Value.SampleCount}");
                    }
                }
            }

            return sb.ToString();
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
