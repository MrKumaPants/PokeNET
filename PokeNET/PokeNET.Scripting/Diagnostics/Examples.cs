using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PokeNET.Scripting.Diagnostics
{
    /// <summary>
    /// Example usage patterns for the performance monitoring system.
    /// </summary>
    public static class PerformanceMonitoringExamples
    {
        /// <summary>
        /// Example 1: Basic script monitoring with budget validation.
        /// </summary>
        public static async Task BasicMonitoringExample(ILogger logger)
        {
            // Create monitor with moderate budget
            var monitor = new ScriptPerformanceMonitor(
                logger as ILogger<ScriptPerformanceMonitor>,
                PerformanceBudget.Moderate(),
                enableProfiling: true);

            // Start monitoring
            var metrics = monitor.StartMonitoring("PlayerController.psl");

            try
            {
                // Compilation phase
                metrics.StartCompilation();
                await SimulateCompilation();
                metrics.EndCompilation();

                // Execution phase
                metrics.StartExecution();
                await SimulateExecution();
                metrics.EndExecution();
            }
            finally
            {
                // Get report and validate budget
                var report = monitor.StopMonitoring("PlayerController.psl");

                Console.WriteLine(report.GenerateReport());

                if (report.BudgetReport?.HasCriticalViolations == true)
                {
                    logger.LogError("Critical performance violations in PlayerController.psl");
                }
            }
        }

        /// <summary>
        /// Example 2: Advanced profiling with custom phases.
        /// </summary>
        public static async Task AdvancedProfilingExample(ILogger logger)
        {
            var monitor = new ScriptPerformanceMonitor(
                logger as ILogger<ScriptPerformanceMonitor>,
                PerformanceBudget.Strict(),
                enableProfiling: true);

            var metrics = monitor.StartMonitoring("ComplexGameLogic.psl");

            try
            {
                // Compilation
                metrics.StartCompilation();
                using (metrics.MeasurePhase("Syntax Analysis"))
                {
                    await Task.Delay(50);
                }
                using (metrics.MeasurePhase("Type Checking"))
                {
                    await Task.Delay(30);
                }
                using (metrics.MeasurePhase("Code Generation"))
                {
                    await Task.Delay(70);
                }
                metrics.EndCompilation();

                // Execution with profiled operations
                metrics.StartExecution();

                using (monitor.ProfileOperation("ComplexGameLogic.psl", "Initialize"))
                {
                    await Task.Delay(20);
                }

                using (monitor.ProfileOperation("ComplexGameLogic.psl", "LoadData"))
                {
                    await Task.Delay(100);
                }

                using (monitor.ProfileOperation("ComplexGameLogic.psl", "ProcessLogic"))
                {
                    await Task.Delay(80);
                }

                metrics.EndExecution();
            }
            finally
            {
                var report = monitor.StopMonitoring("ComplexGameLogic.psl");
                Console.WriteLine(report.GenerateReport());
            }
        }

        /// <summary>
        /// Example 3: Hot-reload performance tracking.
        /// </summary>
        public static async Task HotReloadMonitoringExample(ILogger logger)
        {
            var monitor = new ScriptPerformanceMonitor(
                logger as ILogger<ScriptPerformanceMonitor>,
                PerformanceBudget.Moderate());

            var metrics = monitor.StartMonitoring("DevelopmentScript.psl");

            // Initial load
            metrics.StartCompilation();
            await Task.Delay(200);
            metrics.EndCompilation();
            metrics.StartExecution();
            await Task.Delay(100);
            metrics.EndExecution();

            // Simulate multiple hot reloads
            for (int i = 0; i < 5; i++)
            {
                var reloadTimer = Stopwatch.StartNew();

                // Simulate hot reload
                await Task.Delay(50 + i * 10);

                reloadTimer.Stop();
                metrics.RecordHotReload(reloadTimer.Elapsed);

                logger.LogInformation("Hot reload #{Count} completed in {Time}ms",
                    i + 1, reloadTimer.ElapsedMilliseconds);
            }

            var report = monitor.StopMonitoring("DevelopmentScript.psl");
            Console.WriteLine($"Average hot reload time: {metrics.AverageHotReloadTime.TotalMilliseconds:F2}ms");
        }

        /// <summary>
        /// Example 4: Historical analysis and trending.
        /// </summary>
        public static async Task HistoricalAnalysisExample(ILogger logger)
        {
            var monitor = new ScriptPerformanceMonitor(
                logger as ILogger<ScriptPerformanceMonitor>,
                PerformanceBudget.Moderate());

            // Simulate multiple executions
            for (int i = 0; i < 10; i++)
            {
                var metrics = monitor.StartMonitoring("RepeatingScript.psl");

                metrics.StartCompilation();
                await Task.Delay(100 + i * 5); // Gradually slower compilation
                metrics.EndCompilation();

                metrics.StartExecution();
                await Task.Delay(50 + i * 2); // Gradually slower execution
                metrics.EndExecution();

                monitor.StopMonitoring("RepeatingScript.psl");
            }

            // Analyze historical data
            var stats = monitor.GetStatistics("RepeatingScript.psl");
            if (stats != null)
            {
                Console.WriteLine(stats.GetSummary());

                if (stats.ExecutionCount >= 10)
                {
                    logger.LogInformation("Detected performance regression trend");
                }
            }

            // Generate full report
            string fullReport = monitor.GenerateFullReport();
            Console.WriteLine(fullReport);
        }

        /// <summary>
        /// Example 5: Custom budget with adaptive thresholds.
        /// </summary>
        public static async Task CustomBudgetExample(ILogger logger)
        {
            // Create custom budget based on script complexity
            var customBudget = new PerformanceBudget
            {
                MaxCompilationTime = TimeSpan.FromMilliseconds(750),
                MaxExecutionTime = TimeSpan.FromMilliseconds(250),
                MaxTotalTime = TimeSpan.FromSeconds(1),
                MaxMemoryUsage = 25 * 1024 * 1024, // 25 MB
                MaxPeakMemory = 50 * 1024 * 1024,  // 50 MB
                MaxGCCollections = 8,
                MaxHotReloadTime = TimeSpan.FromMilliseconds(300)
            };

            var monitor = new ScriptPerformanceMonitor(
                logger as ILogger<ScriptPerformanceMonitor>,
                customBudget,
                enableProfiling: true);

            var metrics = monitor.StartMonitoring("CustomBudgetScript.psl");

            try
            {
                metrics.StartCompilation();
                await Task.Delay(800); // Exceeds budget
                metrics.EndCompilation();

                metrics.StartExecution();
                await Task.Delay(150); // Within budget
                metrics.EndExecution();
            }
            finally
            {
                var report = monitor.StopMonitoring("CustomBudgetScript.psl");

                if (report.BudgetReport?.HasViolations == true)
                {
                    Console.WriteLine("Budget Violations:");
                    foreach (var violation in report.BudgetReport.Violations)
                    {
                        Console.WriteLine($"  [{violation.Severity}] {violation.Metric}: " +
                                        $"{violation.Actual} (budget: {violation.Budget})");
                    }
                }
            }
        }

        /// <summary>
        /// Example 6: Memory pressure monitoring.
        /// </summary>
        public static async Task MemoryPressureExample(ILogger logger)
        {
            var monitor = new ScriptPerformanceMonitor(
                logger as ILogger<ScriptPerformanceMonitor>,
                PerformanceBudget.Strict(),
                enableProfiling: true);

            var metrics = monitor.StartMonitoring("MemoryIntensiveScript.psl");

            try
            {
                metrics.StartCompilation();

                // Simulate memory-intensive compilation
                using (metrics.MeasurePhase("AST Construction"))
                {
                    var largeData = new byte[5 * 1024 * 1024]; // 5 MB
                    await Task.Delay(50);
                }

                metrics.EndCompilation();

                metrics.StartExecution();

                // Simulate memory-intensive execution
                using (monitor.ProfileOperation("MemoryIntensiveScript.psl", "DataProcessing"))
                {
                    var tempData = new byte[10 * 1024 * 1024]; // 10 MB
                    await Task.Delay(100);
                }

                metrics.EndExecution();
            }
            finally
            {
                var report = monitor.StopMonitoring("MemoryIntensiveScript.psl");

                Console.WriteLine($"Peak Memory: {FormatBytes(metrics.PeakMemoryUsage)}");
                Console.WriteLine($"Total Allocated: {FormatBytes(metrics.TotalMemoryAllocated)}");
                Console.WriteLine($"GC Collections: {metrics.TotalGCCollections}");

                if (metrics.TotalGCCollections > 5)
                {
                    logger.LogWarning("High GC pressure detected: {Count} collections",
                        metrics.TotalGCCollections);
                }
            }
        }

        /// <summary>
        /// Example 7: Comparative performance analysis.
        /// </summary>
        public static async Task ComparativeAnalysisExample(ILogger logger)
        {
            var monitor = new ScriptPerformanceMonitor(
                logger as ILogger<ScriptPerformanceMonitor>,
                PerformanceBudget.Moderate(),
                enableProfiling: true);

            // Run two versions of a script
            var results = new System.Collections.Generic.Dictionary<string, PerformanceReport>();

            foreach (var version in new[] { "v1", "v2" })
            {
                var scriptName = $"OptimizationTest_{version}.psl";
                var metrics = monitor.StartMonitoring(scriptName);

                metrics.StartCompilation();
                await Task.Delay(version == "v1" ? 200 : 150); // v2 is faster
                metrics.EndCompilation();

                metrics.StartExecution();
                await Task.Delay(version == "v1" ? 300 : 180); // v2 is faster
                metrics.EndExecution();

                results[version] = monitor.StopMonitoring(scriptName);
            }

            // Compare results
            Console.WriteLine("Performance Comparison:");
            Console.WriteLine($"v1 Total: {results["v1"].Metrics?.TotalTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"v2 Total: {results["v2"].Metrics?.TotalTime.TotalMilliseconds:F2}ms");

            var improvement = ((results["v1"].Metrics!.TotalTime - results["v2"].Metrics!.TotalTime)
                             / results["v1"].Metrics.TotalTime) * 100;
            Console.WriteLine($"Improvement: {improvement:F1}%");
        }

        private static async Task SimulateCompilation()
        {
            await Task.Delay(150);
        }

        private static async Task SimulateExecution()
        {
            await Task.Delay(100);
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
    /// Integration example with a complete script execution pipeline.
    /// </summary>
    public class PerformanceMonitoredScriptEngine
    {
        private readonly ScriptPerformanceMonitor _perfMonitor;
        private readonly ILogger _logger;

        public PerformanceMonitoredScriptEngine(
            ILogger<PerformanceMonitoredScriptEngine> logger,
            PerformanceBudget? budget = null)
        {
            _logger = logger;
            _perfMonitor = new ScriptPerformanceMonitor(
                logger as ILogger<ScriptPerformanceMonitor>,
                budget ?? PerformanceBudget.Moderate(),
                enableProfiling: true);
        }

        public async Task<ExecutionResult> ExecuteScriptAsync(string scriptPath, object? context = null)
        {
            var scriptName = System.IO.Path.GetFileName(scriptPath);
            var metrics = _perfMonitor.StartMonitoring(scriptName);

            try
            {
                // Compilation phase with detailed tracking
                metrics.StartCompilation();

                object? compiledScript;
                using (metrics.MeasurePhase("Load Source"))
                {
                    // Load script source
                    await Task.Delay(20);
                }

                using (metrics.MeasurePhase("Parse"))
                {
                    // Parse script
                    await Task.Delay(50);
                }

                using (metrics.MeasurePhase("Compile"))
                {
                    // Compile to IL
                    compiledScript = new object();
                    await Task.Delay(80);
                }

                metrics.EndCompilation();

                // Execution phase with profiling
                metrics.StartExecution();

                object? result;
                using (_perfMonitor.ProfileOperation(scriptName, "Initialize Context"))
                {
                    // Setup execution context
                    await Task.Delay(10);
                }

                using (_perfMonitor.ProfileOperation(scriptName, "Execute"))
                {
                    // Execute script
                    result = "Execution result";
                    await Task.Delay(100);
                }

                using (_perfMonitor.ProfileOperation(scriptName, "Cleanup"))
                {
                    // Cleanup resources
                    await Task.Delay(5);
                }

                metrics.EndExecution();

                return new ExecutionResult(true, result, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Script execution failed: {Script}", scriptName);
                return new ExecutionResult(false, null, ex.Message);
            }
            finally
            {
                // Generate performance report
                var report = _perfMonitor.StopMonitoring(scriptName);

                // Log performance summary
                _logger.LogInformation(
                    "Script '{Script}' completed - Compilation: {CompileTime}ms, Execution: {ExecTime}ms, Memory: {Memory}",
                    scriptName,
                    metrics.CompilationTime.TotalMilliseconds,
                    metrics.ExecutionTime.TotalMilliseconds,
                    FormatBytes(metrics.TotalMemoryAllocated));

                // Check for budget violations
                if (report.BudgetReport?.HasCriticalViolations == true)
                {
                    _logger.LogError("Critical performance budget violations in {Script}", scriptName);
                    report.BudgetReport.LogViolations(_logger);
                }
                else if (report.BudgetReport?.HasWarnings == true)
                {
                    _logger.LogWarning("Performance budget warnings in {Script}", scriptName);
                }
            }
        }

        public ScriptStatistics? GetScriptStatistics(string scriptName)
        {
            return _perfMonitor.GetStatistics(scriptName);
        }

        public string GeneratePerformanceReport()
        {
            return _perfMonitor.GenerateFullReport();
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

    public record ExecutionResult(bool Success, object? Result, string? Error);
}
