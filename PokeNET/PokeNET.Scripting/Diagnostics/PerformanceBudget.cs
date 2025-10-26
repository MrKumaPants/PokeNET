using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace PokeNET.Scripting.Diagnostics
{
    /// <summary>
    /// Performance budget configuration and validation.
    /// </summary>
    public sealed class PerformanceBudget
    {
        public TimeSpan? MaxCompilationTime { get; set; }
        public TimeSpan? MaxExecutionTime { get; set; }
        public TimeSpan? MaxTotalTime { get; set; }
        public long? MaxMemoryUsage { get; set; }
        public long? MaxPeakMemory { get; set; }
        public int? MaxGCCollections { get; set; }
        public TimeSpan? MaxHotReloadTime { get; set; }

        /// <summary>
        /// Creates a strict performance budget for production scenarios.
        /// </summary>
        public static PerformanceBudget Strict()
        {
            return new PerformanceBudget
            {
                MaxCompilationTime = TimeSpan.FromMilliseconds(500),
                MaxExecutionTime = TimeSpan.FromMilliseconds(100),
                MaxTotalTime = TimeSpan.FromMilliseconds(600),
                MaxMemoryUsage = 10 * 1024 * 1024, // 10 MB
                MaxPeakMemory = 20 * 1024 * 1024, // 20 MB
                MaxGCCollections = 5,
                MaxHotReloadTime = TimeSpan.FromMilliseconds(200),
            };
        }

        /// <summary>
        /// Creates a moderate performance budget for development scenarios.
        /// </summary>
        public static PerformanceBudget Moderate()
        {
            return new PerformanceBudget
            {
                MaxCompilationTime = TimeSpan.FromSeconds(2),
                MaxExecutionTime = TimeSpan.FromMilliseconds(500),
                MaxTotalTime = TimeSpan.FromSeconds(3),
                MaxMemoryUsage = 50 * 1024 * 1024, // 50 MB
                MaxPeakMemory = 100 * 1024 * 1024, // 100 MB
                MaxGCCollections = 10,
                MaxHotReloadTime = TimeSpan.FromMilliseconds(500),
            };
        }

        /// <summary>
        /// Creates a relaxed performance budget for testing scenarios.
        /// </summary>
        public static PerformanceBudget Relaxed()
        {
            return new PerformanceBudget
            {
                MaxCompilationTime = TimeSpan.FromSeconds(10),
                MaxExecutionTime = TimeSpan.FromSeconds(5),
                MaxTotalTime = TimeSpan.FromSeconds(15),
                MaxMemoryUsage = 200 * 1024 * 1024, // 200 MB
                MaxPeakMemory = 500 * 1024 * 1024, // 500 MB
                MaxGCCollections = 50,
                MaxHotReloadTime = TimeSpan.FromSeconds(2),
            };
        }

        /// <summary>
        /// Validates metrics against the budget.
        /// </summary>
        public BudgetViolationReport Validate(PerformanceMetrics metrics)
        {
            var report = new BudgetViolationReport(metrics.ScriptName);

            ValidateTimeMetric(
                report,
                "Compilation Time",
                metrics.CompilationTime,
                MaxCompilationTime
            );

            ValidateTimeMetric(report, "Execution Time", metrics.ExecutionTime, MaxExecutionTime);

            ValidateTimeMetric(report, "Total Time", metrics.TotalTime, MaxTotalTime);

            ValidateMemoryMetric(
                report,
                "Total Memory Usage",
                metrics.TotalMemoryAllocated,
                MaxMemoryUsage
            );

            ValidateMemoryMetric(
                report,
                "Peak Memory Usage",
                metrics.PeakMemoryUsage,
                MaxPeakMemory
            );

            if (MaxGCCollections.HasValue && metrics.TotalGCCollections > MaxGCCollections.Value)
            {
                report.AddViolation(
                    "GC Collections",
                    metrics.TotalGCCollections.ToString(),
                    MaxGCCollections.Value.ToString(),
                    BudgetViolationSeverity.Warning
                );
            }

            if (MaxHotReloadTime.HasValue && metrics.HotReloadCount > 0)
            {
                var maxReloadTime = metrics.HotReloadTimes.Max();
                if (maxReloadTime > MaxHotReloadTime.Value)
                {
                    report.AddViolation(
                        "Hot Reload Time",
                        $"{maxReloadTime.TotalMilliseconds:F2} ms",
                        $"{MaxHotReloadTime.Value.TotalMilliseconds:F2} ms",
                        BudgetViolationSeverity.Warning
                    );
                }
            }

            return report;
        }

        private void ValidateTimeMetric(
            BudgetViolationReport report,
            string metricName,
            TimeSpan actual,
            TimeSpan? budget
        )
        {
            if (budget.HasValue && actual > budget.Value)
            {
                var severity =
                    actual > budget.Value * 2
                        ? BudgetViolationSeverity.Critical
                        : BudgetViolationSeverity.Warning;

                report.AddViolation(
                    metricName,
                    $"{actual.TotalMilliseconds:F2} ms",
                    $"{budget.Value.TotalMilliseconds:F2} ms",
                    severity
                );
            }
        }

        private void ValidateMemoryMetric(
            BudgetViolationReport report,
            string metricName,
            long actual,
            long? budget
        )
        {
            if (budget.HasValue && actual > budget.Value)
            {
                var severity =
                    actual > budget.Value * 2
                        ? BudgetViolationSeverity.Critical
                        : BudgetViolationSeverity.Warning;

                report.AddViolation(
                    metricName,
                    FormatBytes(actual),
                    FormatBytes(budget.Value),
                    severity
                );
            }
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
    /// Severity level for budget violations.
    /// </summary>
    public enum BudgetViolationSeverity
    {
        Info,
        Warning,
        Critical,
    }

    /// <summary>
    /// Report of performance budget violations.
    /// </summary>
    public sealed class BudgetViolationReport
    {
        private readonly List<BudgetViolation> _violations = new();

        public string ScriptName { get; }
        public IReadOnlyList<BudgetViolation> Violations => _violations;
        public bool HasViolations => _violations.Any();
        public bool HasCriticalViolations =>
            _violations.Any(v => v.Severity == BudgetViolationSeverity.Critical);
        public bool HasWarnings =>
            _violations.Any(v => v.Severity == BudgetViolationSeverity.Warning);

        public BudgetViolationReport(string scriptName)
        {
            ScriptName = scriptName;
        }

        internal void AddViolation(
            string metric,
            string actual,
            string budget,
            BudgetViolationSeverity severity
        )
        {
            _violations.Add(new BudgetViolation(metric, actual, budget, severity));
        }

        /// <summary>
        /// Logs violations using the provided logger.
        /// </summary>
        public void LogViolations(ILogger logger)
        {
            if (!HasViolations)
            {
                logger.LogInformation(
                    "Script '{ScriptName}' passed all performance budget checks",
                    ScriptName
                );
                return;
            }

            foreach (var violation in _violations)
            {
                var logLevel = violation.Severity switch
                {
                    BudgetViolationSeverity.Critical => LogLevel.Error,
                    BudgetViolationSeverity.Warning => LogLevel.Warning,
                    _ => LogLevel.Information,
                };

                logger.Log(
                    logLevel,
                    "Performance budget violation in '{ScriptName}': {Metric} = {Actual} (budget: {Budget})",
                    ScriptName,
                    violation.Metric,
                    violation.Actual,
                    violation.Budget
                );
            }
        }

        /// <summary>
        /// Gets a formatted report string.
        /// </summary>
        public string GetReport()
        {
            if (!HasViolations)
            {
                return $"✓ Script '{ScriptName}' passed all performance budget checks";
            }

            var report =
                $@"Performance Budget Violations for '{ScriptName}'
{'='.Repeat(50)}";

            var critical = _violations
                .Where(v => v.Severity == BudgetViolationSeverity.Critical)
                .ToList();
            var warnings = _violations
                .Where(v => v.Severity == BudgetViolationSeverity.Warning)
                .ToList();

            if (critical.Any())
            {
                report += "\n\nCRITICAL VIOLATIONS:";
                foreach (var v in critical)
                {
                    report += $"\n  ✗ {v.Metric}: {v.Actual} (budget: {v.Budget})";
                }
            }

            if (warnings.Any())
            {
                report += "\n\nWARNINGS:";
                foreach (var v in warnings)
                {
                    report += $"\n  ⚠ {v.Metric}: {v.Actual} (budget: {v.Budget})";
                }
            }

            return report;
        }
    }

    /// <summary>
    /// Represents a single budget violation.
    /// </summary>
    public sealed class BudgetViolation
    {
        public string Metric { get; }
        public string Actual { get; }
        public string Budget { get; }
        public BudgetViolationSeverity Severity { get; }

        public BudgetViolation(
            string metric,
            string actual,
            string budget,
            BudgetViolationSeverity severity
        )
        {
            Metric = metric;
            Actual = actual;
            Budget = budget;
            Severity = severity;
        }
    }

    internal static class StringExtensions
    {
        public static string Repeat(this char c, int count)
        {
            return new string(c, count);
        }
    }
}
