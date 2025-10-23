# Script Performance Diagnostics

Comprehensive performance monitoring and diagnostics system for script execution with real-time tracking, profiling, and budget enforcement.

## Features

### 1. **PerformanceMetrics**
- Real-time tracking of compilation, execution, memory, and GC metrics
- Timeline event recording for detailed analysis
- Hot-reload performance tracking
- Custom phase measurement with automatic scoping
- Detailed summary reports

### 2. **PerformanceBudget**
- Configurable performance budgets (Strict, Moderate, Relaxed)
- Automatic violation detection with severity levels
- Budget validation with detailed reports
- Warning and critical threshold enforcement

### 3. **ScriptPerformanceMonitor**
- Comprehensive monitoring orchestration
- Historical performance tracking (last 100 executions)
- Execution and memory profiling
- Statistical analysis across multiple runs
- Full report generation with trends

## Quick Start

### Basic Monitoring

```csharp
using PokeNET.Scripting.Diagnostics;
using Microsoft.Extensions.Logging;

// Create monitor with moderate budget
var monitor = new ScriptPerformanceMonitor(
    logger,
    PerformanceBudget.Moderate(),
    enableProfiling: true);

// Start monitoring
var metrics = monitor.StartMonitoring("MyScript.psl");

// Compilation phase
metrics.StartCompilation();
// ... compile script ...
metrics.EndCompilation();

// Execution phase
metrics.StartExecution();
// ... execute script ...
metrics.EndExecution();

// Stop monitoring and get report
var report = monitor.StopMonitoring("MyScript.psl");
Console.WriteLine(report.GenerateReport());
```

### Custom Phase Tracking

```csharp
var metrics = monitor.StartMonitoring("ComplexScript.psl");

// Automatic phase tracking with using statement
using (metrics.MeasurePhase("Initialization"))
{
    // Initialization code
}

using (metrics.MeasurePhase("Data Processing"))
{
    // Processing code
}

using (metrics.MeasurePhase("Cleanup"))
{
    // Cleanup code
}
```

### Operation Profiling

```csharp
// Profile specific operations
using (monitor.ProfileOperation("MyScript.psl", "DatabaseQuery"))
{
    // Database operation
}

using (monitor.ProfileOperation("MyScript.psl", "FileIO"))
{
    // File I/O operation
}

// Get profiling statistics
var stats = monitor.GetStatistics("MyScript.psl");
Console.WriteLine(stats?.GetSummary());
```

### Hot-Reload Tracking

```csharp
var metrics = monitor.GetActiveMetrics("MyScript.psl");

// Record hot reload
var reloadStopwatch = Stopwatch.StartNew();
// ... perform hot reload ...
reloadStopwatch.Stop();
metrics.RecordHotReload(reloadStopwatch.Elapsed);
```

## Performance Budgets

### Strict (Production)
```csharp
var budget = PerformanceBudget.Strict();
// Max Compilation: 500ms
// Max Execution: 100ms
// Max Memory: 10 MB
// Max Peak Memory: 20 MB
// Max GC Collections: 5
// Max Hot Reload: 200ms
```

### Moderate (Development)
```csharp
var budget = PerformanceBudget.Moderate();
// Max Compilation: 2s
// Max Execution: 500ms
// Max Memory: 50 MB
// Max Peak Memory: 100 MB
// Max GC Collections: 10
// Max Hot Reload: 500ms
```

### Relaxed (Testing)
```csharp
var budget = PerformanceBudget.Relaxed();
// Max Compilation: 10s
// Max Execution: 5s
// Max Memory: 200 MB
// Max Peak Memory: 500 MB
// Max GC Collections: 50
// Max Hot Reload: 2s
```

### Custom Budget
```csharp
var customBudget = new PerformanceBudget
{
    MaxCompilationTime = TimeSpan.FromMilliseconds(750),
    MaxExecutionTime = TimeSpan.FromMilliseconds(250),
    MaxMemoryUsage = 25 * 1024 * 1024, // 25 MB
    MaxGCCollections = 8
};
```

## Budget Validation

```csharp
var report = monitor.StopMonitoring("MyScript.psl");

if (report.BudgetReport?.HasCriticalViolations == true)
{
    logger.LogError("Critical performance violations detected!");
    Console.WriteLine(report.BudgetReport.GetReport());

    // Take action (e.g., disable script, alert ops team)
}
else if (report.BudgetReport?.HasWarnings == true)
{
    logger.LogWarning("Performance warnings detected");
    // Monitor but allow execution
}
```

## Historical Analysis

```csharp
// Get statistics for a script across all executions
var stats = monitor.GetStatistics("MyScript.psl");
if (stats != null)
{
    Console.WriteLine($"Average Compilation: {stats.AverageCompilationTime}");
    Console.WriteLine($"Average Execution: {stats.AverageExecutionTime}");
    Console.WriteLine($"Average Memory: {stats.AverageMemoryUsage}");
    Console.WriteLine($"Execution Count: {stats.ExecutionCount}");
}

// Generate full report for all scripts
string fullReport = monitor.GenerateFullReport();
File.WriteAllText("performance-report.txt", fullReport);
```

## Integration with ScriptEngine

```csharp
public class MonitoredScriptEngine
{
    private readonly ScriptPerformanceMonitor _perfMonitor;

    public async Task<object?> ExecuteScriptAsync(string scriptPath)
    {
        var scriptName = Path.GetFileName(scriptPath);
        var metrics = _perfMonitor.StartMonitoring(scriptName);

        try
        {
            // Compilation
            metrics.StartCompilation();
            var compilation = await CompileScriptAsync(scriptPath);
            metrics.EndCompilation();

            // Execution
            metrics.StartExecution();
            using (_perfMonitor.ProfileOperation(scriptName, "ScriptExecution"))
            {
                var result = await ExecuteCompiledScriptAsync(compilation);
                metrics.EndExecution();
                return result;
            }
        }
        finally
        {
            var report = _perfMonitor.StopMonitoring(scriptName);

            // Log if budget violations occurred
            if (report.BudgetReport?.HasViolations == true)
            {
                _logger.LogWarning("Performance budget violations in {Script}", scriptName);
                report.BudgetReport.LogViolations(_logger);
            }
        }
    }
}
```

## Profiling Examples

### Execution Profile
```csharp
var report = monitor.StopMonitoring("MyScript.psl");
var execProfile = report.ExecutionProfile?.GetStatistics();

if (execProfile != null)
{
    foreach (var (operation, stats) in execProfile.OrderByDescending(s => s.Value.Average))
    {
        Console.WriteLine($"{operation}:");
        Console.WriteLine($"  Average: {stats.Average.TotalMilliseconds:F2} ms");
        Console.WriteLine($"  P95: {stats.P95.TotalMilliseconds:F2} ms");
        Console.WriteLine($"  P99: {stats.P99.TotalMilliseconds:F2} ms");
        Console.WriteLine($"  Samples: {stats.SampleCount}");
    }
}
```

### Memory Profile
```csharp
var memProfile = report.MemoryProfile?.GetStatistics();

if (memProfile != null)
{
    foreach (var (operation, stats) in memProfile.OrderByDescending(s => s.Value.Total))
    {
        Console.WriteLine($"{operation}:");
        Console.WriteLine($"  Total: {FormatBytes(stats.Total)}");
        Console.WriteLine($"  Average: {FormatBytes(stats.Average)}");
        Console.WriteLine($"  Peak: {FormatBytes(stats.Max)}");
    }
}
```

## Sample Output

### Performance Metrics Summary
```
Performance Metrics for 'GameLogic.psl'
========================================
Duration: 2025-10-22 14:32:15.123 -> 2025-10-22 14:32:15.845

Timing:
  Total Time:       722.45 ms
  Compilation:      456.23 ms (63.2%)
  Execution:        245.67 ms (34.0%)

Memory:
  Total Allocated:  12.45 MB
  Peak Usage:       18.23 MB
  Compilation:      8.12 MB
  Execution:        4.33 MB

Garbage Collection:
  Total Collections: 3
  During Compilation: 2
  During Execution:   1
  Gen 0: 2
  Gen 1: 1
  Gen 2: 0

Custom Phases:
  Initialization: 23.45 ms (512 KB)
  Data Loading: 145.67 ms (3.2 MB)
  Processing: 234.12 ms (1.8 MB)
```

### Budget Violation Report
```
Performance Budget Violations for 'SlowScript.psl'
==================================================

WARNINGS:
  ⚠ Compilation Time: 2345.67 ms (budget: 2000.00 ms)
  ⚠ Peak Memory Usage: 125.45 MB (budget: 100.00 MB)
```

## Best Practices

1. **Always monitor in production** with Strict or Moderate budgets
2. **Enable profiling during development** to identify bottlenecks
3. **Review historical statistics** to detect performance regressions
4. **Set appropriate budgets** based on your use case
5. **Use custom phases** to track specific operations
6. **Monitor hot-reload performance** in development workflows
7. **Generate regular reports** for performance trending

## Performance Tips

- Monitor compilation and execution separately to identify bottlenecks
- Use profiling to identify expensive operations
- Track GC pressure to optimize memory allocations
- Set realistic budgets based on historical data
- Review timeline events for detailed analysis
- Compare performance across script versions
- Use hot-reload metrics to optimize development workflow
