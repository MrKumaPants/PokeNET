# Performance Benchmark Guide

## Running Benchmarks

### Quick Start

```bash
# Run all Phase 1 benchmarks
dotnet run --project tests/PokeNET.Tests.csproj --configuration Release

# Run specific benchmark category
dotnet run --project tests/PokeNET.Tests.csproj --configuration Release --filter "*Query*"
```

### Benchmark Categories

1. **System Lifecycle Benchmarks**
   - `SystemBase_Update`: Baseline system overhead
   - `SystemBaseEnhanced_Update`: Enhanced system overhead
   - Target: <5% overhead increase

2. **Query Performance Benchmarks**
   - `ManualQuery_Create`: Manual query creation (allocates)
   - `CachedQuery_Use`: Cached query reuse (zero allocation)
   - Target: 0 bytes allocated

3. **CommandBuffer Benchmarks**
   - `DirectOperations_ScatteredWrites`: Direct entity modifications
   - `CommandBuffer_BatchedWrites`: Batched operations
   - Target: >10% performance improvement

4. **System-Specific Benchmarks**
   - `BattleSystem_Update`: Full battle system update
   - `MovementSystem_Update`: Full movement system update
   - `BattleSystem_ExecuteMove`: Damage calculation hot path
   - Target: Combined <16.67ms (60 FPS)

5. **Memory Benchmarks**
   - `Query_AllocationTest`: Query allocation tracking
   - `SystemUpdate_AllocationTest`: System update allocations
   - Target: Minimal allocations per frame

## Performance Targets

### Phase 1 Goals

| Target | Metric | Goal | Critical |
|--------|--------|------|----------|
| Lifecycle Overhead | % increase | <5% | ✅ Yes |
| Query Allocations | bytes | 0 | ✅ Yes |
| Batch Efficiency | % improvement | >10% | ⚠️ Medium |
| Frame Budget | milliseconds | <16.67 | ✅ Yes |

### Interpreting Results

**BenchmarkDotNet Output:**
- `Mean`: Average execution time
- `Error`: Standard error of the mean
- `StdDev`: Standard deviation
- `Gen0/Gen1/Gen2`: GC collections per 1000 operations
- `Allocated`: Memory allocated per operation

**Performance Report Sections:**
1. **Executive Summary**: Overall benchmark statistics
2. **Target Validation**: Pass/fail status for each target
3. **Detailed Results**: Full benchmark data table
4. **Memory Analysis**: Allocation hotspots
5. **Bottleneck Identification**: Slowest operations
6. **Recommendations**: Prioritized optimization suggestions

## Best Practices

### Writing Benchmarks

```csharp
[MemoryDiagnoser]
public class MyBenchmark
{
    private World _world;

    [GlobalSetup]
    public void Setup()
    {
        // Initialize test data once
        _world = World.Create();
    }

    [Benchmark]
    public void MyOperation()
    {
        // Benchmark code here
        // Should be representative of real-world usage
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // Clean up resources
        World.Destroy(_world);
    }
}
```

### Benchmark Guidelines

1. **Use [MemoryDiagnoser]**: Always track allocations
2. **Setup Once**: Use [GlobalSetup] for expensive initialization
3. **Representative Data**: Use realistic entity counts and patterns
4. **Measure Hot Paths**: Focus on frequently executed code
5. **Avoid External Factors**: Disable antivirus, close other apps
6. **Run in Release**: Always benchmark optimized code
7. **Multiple Iterations**: Let BenchmarkDotNet handle statistics

### Common Pitfalls

❌ **Don't:**
- Benchmark Debug builds
- Include I/O operations in benchmarks
- Test with trivial data sizes
- Run while system is under load
- Compare across different machines

✅ **Do:**
- Use Release configuration
- Test with representative data
- Run multiple iterations
- Control environment variables
- Use consistent hardware

## Continuous Integration

### GitHub Actions Integration

```yaml
name: Performance Benchmarks

on:
  pull_request:
    branches: [ main ]
  schedule:
    - cron: '0 0 * * 0'  # Weekly on Sunday

jobs:
  benchmark:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 9.0.x
      - name: Run Benchmarks
        run: |
          dotnet run --project tests/PokeNET.Tests.csproj \
            --configuration Release
      - name: Upload Report
        uses: actions/upload-artifact@v3
        with:
          name: performance-report
          path: docs/performance/Phase1_Report_*.md
```

## Performance Monitoring

### Baseline Tracking

Track performance over time:

```bash
# Run and save baseline
dotnet run --project tests --configuration Release > baseline.txt

# Compare against baseline later
dotnet run --project tests --configuration Release > current.txt
diff baseline.txt current.txt
```

### Regression Detection

Watch for:
- Execution time increases >5%
- New allocations in hot paths
- GC pressure increases
- Frame budget violations

## Troubleshooting

### Benchmark Failures

**"System not initialized" errors:**
- Ensure [GlobalSetup] creates required resources
- Check that Initialize() is called on systems

**High variance in results:**
- Close background applications
- Disable CPU power management
- Increase warmup iterations

**Memory diagnostic failures:**
- Verify BenchmarkDotNet is latest version
- Check that [MemoryDiagnoser] attribute is present
- Run as administrator on Windows

### Getting Help

- Check BenchmarkDotNet docs: https://benchmarkdotnet.org/
- Review sample benchmarks in tests/Performance/
- Open an issue with benchmark results attached

## Future Enhancements

### Phase 2 Benchmarks (Planned)

- Entity pooling performance
- Multi-threaded system execution
- Job system parallelization
- Network synchronization overhead
- Serialization performance

### Custom Metrics (Planned)

- Entity count vs. performance curves
- Memory fragmentation analysis
- Cache miss rates
- Branch prediction analysis

---

*Last updated: 2025-10-24*
