# SystemManager Metrics - Quick Reference Guide

## Quick Start

### Enable Metrics

```csharp
// Option 1: Enable at creation
var manager = new SystemManager(logger, enableMetrics: true);

// Option 2: Enable at runtime
var manager = new SystemManager(logger);
manager.EnableMetrics(true);
```

### Basic Usage

```csharp
// Update systems normally
manager.UpdateSystems(deltaTime);

// Get metrics for a specific system
var system = manager.GetSystem<MovementSystem>();
var metrics = manager.GetSystemMetrics(system);

if (metrics != null)
{
    Console.WriteLine($"{metrics.SystemName}:");
    Console.WriteLine($"  Last: {metrics.LastUpdateTime:F2}ms");
    Console.WriteLine($"  Avg:  {metrics.AverageUpdateTime:F2}ms");
    Console.WriteLine($"  Count: {metrics.UpdateCount}");
}
```

## Common Patterns

### 1. Performance Profiling

```csharp
// Enable metrics for profiling session
manager.EnableMetrics(true);

// Run game loop
for (int frame = 0; frame < 1000; frame++)
{
    manager.UpdateSystems(deltaTime);
}

// Analyze results
manager.LogMetrics(LogLevel.Information);

// Disable when done
manager.EnableMetrics(false);
```

### 2. Bottleneck Detection

```csharp
var allMetrics = manager.GetAllMetrics();

// Find systems taking > 5ms on average
var bottlenecks = allMetrics
    .Where(m => m.Value.AverageUpdateTime > 5.0)
    .OrderByDescending(m => m.Value.AverageUpdateTime);

foreach (var (name, metric) in bottlenecks)
{
    logger.LogWarning("Performance bottleneck: {Name} @ {Time:F2}ms",
        name, metric.AverageUpdateTime);
}
```

### 3. Frame Budget Monitoring

```csharp
const double TARGET_FRAME_TIME = 16.67; // 60 FPS

var frameMetrics = manager.GetTotalMetrics();
if (frameMetrics != null && frameMetrics.LastFrameTime > TARGET_FRAME_TIME)
{
    logger.LogWarning("Frame budget exceeded: {Time:F2}ms (target: {Target}ms)",
        frameMetrics.LastFrameTime, TARGET_FRAME_TIME);

    // Find which systems are slow
    var allMetrics = manager.GetAllMetrics();
    foreach (var (name, metric) in allMetrics.OrderByDescending(m => m.Value.LastUpdateTime))
    {
        var percent = (metric.LastUpdateTime / frameMetrics.LastFrameTime) * 100;
        logger.LogWarning("  {Name}: {Time:F2}ms ({Percent:F1}%)",
            name, metric.LastUpdateTime, percent);
    }
}
```

### 4. Performance Comparison

```csharp
// Before optimization
manager.ResetMetrics();
manager.EnableMetrics(true);

for (int i = 0; i < 100; i++)
    manager.UpdateSystems(deltaTime);

var beforeMetrics = manager.GetSystem<TargetSystem>();
var beforeAvg = manager.GetSystemMetrics(beforeMetrics)?.AverageUpdateTime ?? 0;

// Apply optimization...

// After optimization
manager.ResetMetrics();

for (int i = 0; i < 100; i++)
    manager.UpdateSystems(deltaTime);

var afterAvg = manager.GetSystemMetrics(beforeMetrics)?.AverageUpdateTime ?? 0;

var improvement = ((beforeAvg - afterAvg) / beforeAvg) * 100;
logger.LogInformation("Optimization improved performance by {Percent:F1}%", improvement);
```

### 5. Real-Time Dashboard

```csharp
// Update metrics display every second
if (timeSinceLastDisplay >= 1.0f)
{
    var frameMetrics = manager.GetTotalMetrics();
    var allMetrics = manager.GetAllMetrics();

    // Display in ImGui or custom UI
    ImGui.Text($"Frame Time: {frameMetrics?.LastFrameTime:F2}ms");
    ImGui.Text($"System Time: {frameMetrics?.TotalSystemTime:F2}ms");
    ImGui.Text($"Overhead: {frameMetrics?.OverheadTime:F2}ms");
    ImGui.Separator();

    foreach (var (name, metric) in allMetrics)
    {
        var percent = frameMetrics != null
            ? (metric.LastUpdateTime / frameMetrics.LastFrameTime) * 100
            : 0;

        ImGui.Text($"{name}: {metric.LastUpdateTime:F2}ms ({percent:F1}%)");
        ImGui.ProgressBar((float)(metric.LastUpdateTime / 16.67));
    }

    timeSinceLastDisplay = 0;
}
```

### 6. Automated Performance Testing

```csharp
[Fact]
public void PerformanceTest_AllSystemsMeetBudget()
{
    var manager = new SystemManager(logger, enableMetrics: true);

    // Register systems
    manager.RegisterSystem(new MovementSystem(logger));
    manager.RegisterSystem(new BattleSystem(logger));
    manager.InitializeSystems(world);

    // Warm up
    for (int i = 0; i < 100; i++)
        manager.UpdateSystems(0.016f);

    manager.ResetMetrics();

    // Measure
    for (int i = 0; i < 1000; i++)
        manager.UpdateSystems(0.016f);

    // Verify
    var allMetrics = manager.GetAllMetrics();
    foreach (var (name, metric) in allMetrics)
    {
        Assert.True(metric.AverageUpdateTime < 5.0,
            $"{name} exceeded budget: {metric.AverageUpdateTime:F2}ms");
    }
}
```

### 7. Conditional Metrics (Development Only)

```csharp
public class SystemManager : ISystemManager
{
    public SystemManager(ILogger<SystemManager> logger)
        : this(logger, enableMetrics: IsDevelopmentMode())
    {
    }

    private static bool IsDevelopmentMode()
    {
#if DEBUG
        return true;
#else
        return Environment.GetEnvironmentVariable("ENABLE_METRICS") == "1";
#endif
    }
}
```

### 8. Periodic Performance Reports

```csharp
private float _reportInterval = 60.0f; // Every 60 seconds
private float _timeSinceReport = 0;

void Update(float deltaTime)
{
    manager.UpdateSystems(deltaTime);

    _timeSinceReport += deltaTime;
    if (_timeSinceReport >= _reportInterval)
    {
        GeneratePerformanceReport();
        manager.ResetMetrics();
        _timeSinceReport = 0;
    }
}

void GeneratePerformanceReport()
{
    var report = new StringBuilder();
    report.AppendLine("=== Performance Report ===");

    var frameMetrics = manager.GetTotalMetrics();
    if (frameMetrics != null)
    {
        report.AppendLine($"Average Frame Time: {frameMetrics.LastFrameTime:F2}ms");
        report.AppendLine($"Total Updates: {frameMetrics.TotalUpdateCount}");
    }

    var allMetrics = manager.GetAllMetrics()
        .OrderByDescending(m => m.Value.AverageUpdateTime);

    report.AppendLine("\nSystem Performance:");
    foreach (var (name, metric) in allMetrics)
    {
        report.AppendLine($"  {name,-30} {metric.AverageUpdateTime,6:F2}ms");
    }

    logger.LogInformation(report.ToString());

    // Optionally save to file
    File.WriteAllText($"perf-report-{DateTime.Now:yyyyMMdd-HHmmss}.txt",
        report.ToString());
}
```

## API Reference

### Properties & Methods

```csharp
// Enable/disable metrics
void EnableMetrics(bool enabled)

// Get specific system metrics
SystemMetrics? GetSystemMetrics(ISystem system)

// Get all system metrics
Dictionary<string, SystemMetrics> GetAllMetrics()

// Get frame-level metrics
FrameMetrics? GetTotalMetrics()

// Reset all metrics
void ResetMetrics()

// Log formatted metrics
void LogMetrics(LogLevel logLevel = LogLevel.Information)
```

### SystemMetrics Properties

```csharp
string SystemName              // Name of the system
Type SystemType                // Type of the system
double LastUpdateTime          // Last frame time (ms)
double TotalUpdateTime         // Total accumulated time (ms)
long UpdateCount               // Number of updates
double AverageUpdateTime       // Calculated: Total / Count
double PercentOfFrame          // % of frame time (must be set externally)
```

### FrameMetrics Properties

```csharp
double LastFrameTime           // Total frame time (ms)
double TotalSystemTime         // Sum of system times (ms)
int ActiveSystemCount          // Number of active systems
long TotalUpdateCount          // Sum of all update counts
double OverheadTime            // Calculated: Frame - System
double SystemTimePercentage    // Calculated: (System / Frame) * 100
```

## Best Practices

### DO ✅
- Enable metrics only when needed (profiling, debugging)
- Reset metrics after optimization changes
- Use meaningful logging levels
- Monitor frame budget regularly
- Profile in Release builds for accurate results

### DON'T ❌
- Leave metrics enabled in production (adds ~0.5% overhead)
- Compare metrics from different runs without resetting
- Profile in Debug builds (too slow)
- Ignore frame overhead time
- Optimize systems without measuring first

## Performance Tips

1. **Profile in Release Mode**: Debug builds have 10-100x overhead
2. **Warm Up First**: First few frames are always slower
3. **Measure Multiple Times**: Average over 100+ frames
4. **Reset Between Tests**: Ensure clean baselines
5. **Consider GC**: Large allocations can skew results

## Troubleshooting

### Metrics returning null?
```csharp
// Check if metrics are enabled
if (manager.GetSystemMetrics(system) == null)
{
    manager.EnableMetrics(true);
}
```

### High overhead time?
```csharp
var frameMetrics = manager.GetTotalMetrics();
if (frameMetrics != null && frameMetrics.OverheadTime > 5.0)
{
    // Check for:
    // - Entity creation/deletion overhead
    // - World.Query() overhead
    // - Component archetype changes
    logger.LogWarning("High overhead: {Time:F2}ms", frameMetrics.OverheadTime);
}
```

### Inconsistent results?
```csharp
// Reset and measure again with more samples
manager.ResetMetrics();

for (int i = 0; i < 1000; i++) // More samples = more stable
    manager.UpdateSystems(deltaTime);

var metrics = manager.GetAllMetrics();
```

## Migration from Legacy Code

### Before (No Metrics)
```csharp
var manager = new SystemManager(logger);
manager.UpdateSystems(deltaTime);
```

### After (With Metrics)
```csharp
var manager = new SystemManager(logger, enableMetrics: true);
manager.UpdateSystems(deltaTime);

// Now you can inspect performance!
var metrics = manager.GetAllMetrics();
```

**No code changes required** - perfectly backward compatible!

---

**Quick Reference Version**: 1.0
**Last Updated**: 2025-10-24
**Status**: Production Ready
