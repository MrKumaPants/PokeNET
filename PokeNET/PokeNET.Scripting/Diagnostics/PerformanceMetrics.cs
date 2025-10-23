using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PokeNET.Scripting.Diagnostics
{
    /// <summary>
    /// Comprehensive performance metrics for script operations.
    /// </summary>
    public sealed class PerformanceMetrics
    {
        private readonly Stopwatch _totalStopwatch = new();
        private readonly Stopwatch _phaseStopwatch = new();
        private long _initialMemory;
        private long _peakMemory;
        private int _gcCollections0;
        private int _gcCollections1;
        private int _gcCollections2;

        public string ScriptName { get; }
        public DateTime StartTime { get; private set; }
        public DateTime? EndTime { get; private set; }

        // Compilation Metrics
        public TimeSpan CompilationTime { get; private set; }
        public long CompilationMemoryUsed { get; private set; }
        public int CompilationGCCollections { get; private set; }

        // Execution Metrics
        public TimeSpan ExecutionTime { get; private set; }
        public long ExecutionMemoryUsed { get; private set; }
        public int ExecutionGCCollections { get; private set; }

        // Overall Metrics
        public TimeSpan TotalTime => _totalStopwatch.Elapsed;
        public long PeakMemoryUsage { get; private set; }
        public long TotalMemoryAllocated { get; private set; }
        public int TotalGCCollections { get; private set; }

        // Profiling Data
        public Dictionary<string, TimeSpan> PhaseTimes { get; } = new();
        public Dictionary<string, long> PhaseMemory { get; } = new();
        public List<(DateTime Time, string Event, long Memory)> Timeline { get; } = new();

        // Hot Reload Metrics
        public int HotReloadCount { get; private set; }
        public TimeSpan AverageHotReloadTime { get; private set; }
        public List<TimeSpan> HotReloadTimes { get; } = new();

        public PerformanceMetrics(string scriptName)
        {
            ScriptName = scriptName ?? throw new ArgumentNullException(nameof(scriptName));
        }

        /// <summary>
        /// Starts performance tracking.
        /// </summary>
        public void Start()
        {
            StartTime = DateTime.UtcNow;
            _initialMemory = GC.GetTotalMemory(false);
            _peakMemory = _initialMemory;

            _gcCollections0 = GC.CollectionCount(0);
            _gcCollections1 = GC.CollectionCount(1);
            _gcCollections2 = GC.CollectionCount(2);

            _totalStopwatch.Start();
            RecordEvent("Start", _initialMemory);
        }

        /// <summary>
        /// Starts a compilation phase.
        /// </summary>
        public void StartCompilation()
        {
            _phaseStopwatch.Restart();
            RecordEvent("Compilation Start", GC.GetTotalMemory(false));
        }

        /// <summary>
        /// Ends the compilation phase.
        /// </summary>
        public void EndCompilation()
        {
            CompilationTime = _phaseStopwatch.Elapsed;
            var currentMemory = GC.GetTotalMemory(false);
            CompilationMemoryUsed = Math.Max(0, currentMemory - _initialMemory);
            CompilationGCCollections = (GC.CollectionCount(0) - _gcCollections0) +
                                      (GC.CollectionCount(1) - _gcCollections1) +
                                      (GC.CollectionCount(2) - _gcCollections2);

            UpdatePeakMemory(currentMemory);
            PhaseTimes["Compilation"] = CompilationTime;
            PhaseMemory["Compilation"] = CompilationMemoryUsed;
            RecordEvent("Compilation End", currentMemory);
        }

        /// <summary>
        /// Starts an execution phase.
        /// </summary>
        public void StartExecution()
        {
            _phaseStopwatch.Restart();
            var executionStartGC0 = GC.CollectionCount(0);
            var executionStartGC1 = GC.CollectionCount(1);
            var executionStartGC2 = GC.CollectionCount(2);
            RecordEvent("Execution Start", GC.GetTotalMemory(false));
        }

        /// <summary>
        /// Ends the execution phase.
        /// </summary>
        public void EndExecution()
        {
            ExecutionTime = _phaseStopwatch.Elapsed;
            var currentMemory = GC.GetTotalMemory(false);
            var memoryBeforeExecution = _initialMemory + CompilationMemoryUsed;
            ExecutionMemoryUsed = Math.Max(0, currentMemory - memoryBeforeExecution);

            var currentGC0 = GC.CollectionCount(0);
            var currentGC1 = GC.CollectionCount(1);
            var currentGC2 = GC.CollectionCount(2);
            ExecutionGCCollections = (currentGC0 - _gcCollections0 - CompilationGCCollections);

            UpdatePeakMemory(currentMemory);
            PhaseTimes["Execution"] = ExecutionTime;
            PhaseMemory["Execution"] = ExecutionMemoryUsed;
            RecordEvent("Execution End", currentMemory);
        }

        /// <summary>
        /// Records a custom phase.
        /// </summary>
        public IDisposable MeasurePhase(string phaseName)
        {
            return new PhaseScope(this, phaseName);
        }

        /// <summary>
        /// Records a hot reload operation.
        /// </summary>
        public void RecordHotReload(TimeSpan reloadTime)
        {
            HotReloadCount++;
            HotReloadTimes.Add(reloadTime);
            AverageHotReloadTime = TimeSpan.FromMilliseconds(
                HotReloadTimes.Average(t => t.TotalMilliseconds));
            PhaseTimes[$"HotReload_{HotReloadCount}"] = reloadTime;
            RecordEvent($"Hot Reload #{HotReloadCount}", GC.GetTotalMemory(false));
        }

        /// <summary>
        /// Ends performance tracking.
        /// </summary>
        public void End()
        {
            _totalStopwatch.Stop();
            EndTime = DateTime.UtcNow;

            var finalMemory = GC.GetTotalMemory(false);
            TotalMemoryAllocated = Math.Max(0, finalMemory - _initialMemory);
            PeakMemoryUsage = _peakMemory - _initialMemory;

            TotalGCCollections = (GC.CollectionCount(0) - _gcCollections0) +
                                (GC.CollectionCount(1) - _gcCollections1) +
                                (GC.CollectionCount(2) - _gcCollections2);

            RecordEvent("End", finalMemory);
        }

        /// <summary>
        /// Gets a summary of the performance metrics.
        /// </summary>
        public string GetSummary()
        {
            var summary = $@"Performance Metrics for '{ScriptName}'
========================================
Duration: {StartTime:yyyy-MM-dd HH:mm:ss.fff} -> {EndTime:yyyy-MM-dd HH:mm:ss.fff}

Timing:
  Total Time:       {TotalTime.TotalMilliseconds:F2} ms
  Compilation:      {CompilationTime.TotalMilliseconds:F2} ms ({GetPercentage(CompilationTime, TotalTime):F1}%)
  Execution:        {ExecutionTime.TotalMilliseconds:F2} ms ({GetPercentage(ExecutionTime, TotalTime):F1}%)

Memory:
  Total Allocated:  {FormatBytes(TotalMemoryAllocated)}
  Peak Usage:       {FormatBytes(PeakMemoryUsage)}
  Compilation:      {FormatBytes(CompilationMemoryUsed)}
  Execution:        {FormatBytes(ExecutionMemoryUsed)}

Garbage Collection:
  Total Collections: {TotalGCCollections}
  During Compilation: {CompilationGCCollections}
  During Execution:   {ExecutionGCCollections}
  Gen 0: {GC.CollectionCount(0) - _gcCollections0}
  Gen 1: {GC.CollectionCount(1) - _gcCollections1}
  Gen 2: {GC.CollectionCount(2) - _gcCollections2}";

            if (HotReloadCount > 0)
            {
                summary += $@"

Hot Reload:
  Count:            {HotReloadCount}
  Average Time:     {AverageHotReloadTime.TotalMilliseconds:F2} ms
  Min Time:         {HotReloadTimes.Min().TotalMilliseconds:F2} ms
  Max Time:         {HotReloadTimes.Max().TotalMilliseconds:F2} ms";
            }

            if (PhaseTimes.Any())
            {
                summary += "\n\nCustom Phases:";
                foreach (var phase in PhaseTimes.OrderByDescending(p => p.Value))
                {
                    var memory = PhaseMemory.TryGetValue(phase.Key, out var mem) ? $" ({FormatBytes(mem)})" : "";
                    summary += $"\n  {phase.Key}: {phase.Value.TotalMilliseconds:F2} ms{memory}";
                }
            }

            return summary;
        }

        private void UpdatePeakMemory(long currentMemory)
        {
            if (currentMemory > _peakMemory)
            {
                _peakMemory = currentMemory;
            }
        }

        private void RecordEvent(string eventName, long memory)
        {
            Timeline.Add((DateTime.UtcNow, eventName, memory));
        }

        private static double GetPercentage(TimeSpan part, TimeSpan total)
        {
            return total.TotalMilliseconds > 0
                ? (part.TotalMilliseconds / total.TotalMilliseconds) * 100
                : 0;
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

        private sealed class PhaseScope : IDisposable
        {
            private readonly PerformanceMetrics _metrics;
            private readonly string _phaseName;
            private readonly Stopwatch _stopwatch;
            private readonly long _startMemory;

            public PhaseScope(PerformanceMetrics metrics, string phaseName)
            {
                _metrics = metrics;
                _phaseName = phaseName;
                _startMemory = GC.GetTotalMemory(false);
                _stopwatch = Stopwatch.StartNew();
                _metrics.RecordEvent($"{phaseName} Start", _startMemory);
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                var endMemory = GC.GetTotalMemory(false);
                _metrics.PhaseTimes[_phaseName] = _stopwatch.Elapsed;
                _metrics.PhaseMemory[_phaseName] = Math.Max(0, endMemory - _startMemory);
                _metrics.UpdatePeakMemory(endMemory);
                _metrics.RecordEvent($"{_phaseName} End", endMemory);
            }
        }
    }
}
