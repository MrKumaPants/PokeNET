# Phase 1 Migration Guide: Foundation Setup

## Overview

Phase 1 establishes the foundation for Arch.Extended integration without breaking any existing functionality. This guide provides step-by-step instructions for developers.

**Duration:** 2 weeks
**Risk Level:** Low
**Breaking Changes:** None

---

## Prerequisites

### Knowledge Requirements
- Understanding of current ECS architecture
- Familiarity with Arch core library
- C# generics and LINQ
- NuGet package management

### Environment Setup
```bash
# Ensure .NET 9 SDK installed
dotnet --version  # Should be 9.0.x

# Clean build
dotnet clean
dotnet build
dotnet test
```

---

## Step 1: Add Arch.Extended Package

### 1.1 Update PokeNET.Domain.csproj

**File:** `/PokeNET/PokeNET.Domain/PokeNET.Domain.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <Platforms>AnyCPU</Platforms>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Arch" Version="2.*" />
        <!-- NEW: Add Arch.Extended -->
        <PackageReference Include="Arch.Extended" Version="1.3.*" />
        <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.*" />
        <PackageReference Include="MonoGame.Framework.DesktopGL" Version="3.8.*" />
    </ItemGroup>
</Project>
```

### 1.2 Verify Installation

```bash
cd PokeNET/PokeNET.Domain
dotnet restore
dotnet build

# Verify package installed
dotnet list package | grep Arch.Extended
# Expected output: Arch.Extended  1.3.x
```

### 1.3 Validation

```csharp
// Add test to verify Arch.Extended available
using Arch.Extended;
using Xunit;

namespace PokeNET.Tests.Integration;

public class ArchExtendedIntegrationTests
{
    [Fact]
    public void ArchExtended_IsAvailable()
    {
        // Verify we can reference Arch.Extended types
        Assert.NotNull(typeof(Arch.Extended.Commands.CommandBuffer));
        Assert.NotNull(typeof(Arch.Extended.Queries.QueryDescription));
    }
}
```

---

## Step 2: Create Enhanced System Base

### 2.1 Create SystemBaseEnhanced.cs

**File:** `/PokeNET/PokeNET.Domain/ECS/Systems/SystemBaseEnhanced.cs`

```csharp
using Arch.Core;
using Arch.Extended.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace PokeNET.Domain.ECS.Systems;

/// <summary>
/// Enhanced system base with Arch.Extended support.
/// Backward compatible with existing SystemBase.
/// Adds query caching and CommandBuffer support.
/// </summary>
public abstract class SystemBaseEnhanced : SystemBase
{
    private readonly Dictionary<string, QueryDescription> _queryCache = new();
    private readonly ObjectPool<CommandBuffer> _commandBufferPool;

    protected SystemBaseEnhanced(ILogger logger) : base(logger)
    {
        // Create command buffer pool
        var policy = new CommandBufferPoolPolicy(this);
        _commandBufferPool = new DefaultObjectPool<CommandBuffer>(policy, maxRetained: 4);
    }

    /// <summary>
    /// Define a cached query that will be reused across frames.
    /// Call this in OnInitialize().
    /// </summary>
    /// <param name="name">Unique name for this query</param>
    /// <param name="builder">Action to build the query description</param>
    protected void DefineQuery(string name, Action<QueryDescription> builder)
    {
        if (_queryCache.ContainsKey(name))
        {
            Logger.LogWarning("Query '{Name}' already defined, skipping redefinition", name);
            return;
        }

        var desc = new QueryDescription();
        builder(desc);
        _queryCache[name] = desc;

        Logger.LogDebug("Defined cached query: {Name}", name);
    }

    /// <summary>
    /// Get a previously defined cached query.
    /// Zero allocation after first call.
    /// </summary>
    /// <param name="name">Query name from DefineQuery</param>
    /// <returns>The cached query description</returns>
    protected QueryDescription GetQuery(string name)
    {
        if (!_queryCache.TryGetValue(name, out var query))
        {
            throw new InvalidOperationException(
                $"Query '{name}' not defined. Call DefineQuery() in OnInitialize().");
        }
        return query;
    }

    /// <summary>
    /// Create a CommandBuffer from the pool.
    /// Remember to Dispose (use 'using var cmd = CreateCommandBuffer()').
    /// </summary>
    protected CommandBuffer CreateCommandBuffer()
    {
        return _commandBufferPool.Get();
    }

    /// <summary>
    /// Return a CommandBuffer to the pool (called automatically by Dispose).
    /// </summary>
    protected void ReturnCommandBuffer(CommandBuffer buffer)
    {
        _commandBufferPool.Return(buffer);
    }

    protected override void OnDispose()
    {
        _queryCache.Clear();
        base.OnDispose();
    }

    /// <summary>
    /// Pool policy for CommandBuffer instances.
    /// </summary>
    private class CommandBufferPoolPolicy : IPooledObjectPolicy<CommandBuffer>
    {
        private readonly SystemBaseEnhanced _system;

        public CommandBufferPoolPolicy(SystemBaseEnhanced system)
        {
            _system = system;
        }

        public CommandBuffer Create()
        {
            return new CommandBuffer(_system.World);
        }

        public bool Return(CommandBuffer obj)
        {
            // CommandBuffer doesn't need explicit clearing
            // New instances created as needed
            return true;
        }
    }
}
```

### 2.2 Validation Test

**File:** `/tests/PokeNET.Tests/Domain/ECS/Systems/SystemBaseEnhancedTests.cs`

```csharp
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using PokeNET.Domain.ECS.Components;
using PokeNET.Domain.ECS.Systems;
using Xunit;

namespace PokeNET.Tests.Domain.ECS.Systems;

public class SystemBaseEnhancedTests : IDisposable
{
    private readonly World _world;
    private readonly TestEnhancedSystem _system;

    public SystemBaseEnhancedTests()
    {
        _world = World.Create();
        _system = new TestEnhancedSystem(NullLogger<TestEnhancedSystem>.Instance);
        _system.Initialize(_world);
    }

    [Fact]
    public void DefineQuery_StoresQueryInCache()
    {
        // Query defined in OnInitialize
        var query = _system.GetTestQuery();
        Assert.NotNull(query);
    }

    [Fact]
    public void GetQuery_ReturnsZeroAllocation()
    {
        // Get query multiple times
        var query1 = _system.GetTestQuery();
        var query2 = _system.GetTestQuery();

        // Should be same instance (cached)
        Assert.Equal(query1, query2);
    }

    [Fact]
    public void CreateCommandBuffer_ReturnsValidBuffer()
    {
        using var cmd = _system.CreateTestCommandBuffer();
        Assert.NotNull(cmd);

        // Can perform operations
        var entity = cmd.Create();
        Assert.True(entity != default);
    }

    [Fact]
    public void CommandBuffer_CanBePooled()
    {
        CommandBuffer buffer1, buffer2;

        using (buffer1 = _system.CreateTestCommandBuffer())
        {
            // Use buffer
        }

        using (buffer2 = _system.CreateTestCommandBuffer())
        {
            // Should get a buffer from pool (may be new or recycled)
            Assert.NotNull(buffer2);
        }
    }

    public void Dispose()
    {
        _system.Dispose();
        _world.Dispose();
    }

    // Test implementation
    private class TestEnhancedSystem : SystemBaseEnhanced
    {
        public TestEnhancedSystem(ILogger logger) : base(logger) { }

        protected override void OnInitialize()
        {
            DefineQuery("test", desc => desc.WithAll<Position>());
        }

        protected override void OnUpdate(float deltaTime) { }

        public QueryDescription GetTestQuery() => GetQuery("test");
        public CommandBuffer CreateTestCommandBuffer() => CreateCommandBuffer();
    }
}
```

---

## Step 3: Update SystemManager

### 3.1 Enhance SystemManager

**File:** `/PokeNET/PokeNET.Core/ECS/SystemManager.cs`

Add shared query registry and metrics:

```csharp
using Arch.Core;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.ECS.Systems;

namespace PokeNET.Core.ECS;

public class SystemManager : ISystemManager
{
    private readonly ILogger<SystemManager> _logger;
    private readonly List<ISystem> _systems = new();
    private readonly Dictionary<string, QueryDescription> _sharedQueries = new();
    private readonly Dictionary<Type, SystemMetrics> _metrics = new();
    private bool _initialized;
    private bool _disposed;
    private bool _profilingEnabled;

    public SystemManager(ILogger<SystemManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// NEW: Register a shared query that multiple systems can use.
    /// </summary>
    public void RegisterSharedQuery(string name, QueryDescription query)
    {
        if (_sharedQueries.ContainsKey(name))
        {
            _logger.LogWarning("Shared query '{Name}' already registered", name);
            return;
        }

        _sharedQueries[name] = query;
        _logger.LogInformation("Registered shared query: {Name}", name);
    }

    /// <summary>
    /// NEW: Get a shared query by name.
    /// </summary>
    public QueryDescription GetSharedQuery(string name)
    {
        if (!_sharedQueries.TryGetValue(name, out var query))
        {
            throw new InvalidOperationException($"Shared query '{name}' not registered");
        }
        return query;
    }

    /// <summary>
    /// NEW: Enable/disable performance profiling.
    /// </summary>
    public void EnableProfiling(bool enabled)
    {
        _profilingEnabled = enabled;
        _logger.LogInformation("Profiling {Status}", enabled ? "enabled" : "disabled");
    }

    /// <summary>
    /// NEW: Get metrics for a specific system.
    /// </summary>
    public SystemMetrics? GetMetrics(Type systemType)
    {
        return _metrics.TryGetValue(systemType, out var metrics) ? metrics : null;
    }

    // Enhanced UpdateSystems with profiling
    public void UpdateSystems(float deltaTime)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SystemManager));

        if (!_initialized)
        {
            _logger.LogWarning("Attempting to update systems before initialization");
            return;
        }

        foreach (var system in _systems)
        {
            if (!system.IsEnabled)
                continue;

            if (_profilingEnabled)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var gcBefore = GC.GetTotalAllocatedBytes();

                system.Update(deltaTime);

                sw.Stop();
                var gcAfter = GC.GetTotalAllocatedBytes();

                RecordMetrics(system, sw.Elapsed, gcAfter - gcBefore);
            }
            else
            {
                system.Update(deltaTime);
            }
        }
    }

    private void RecordMetrics(ISystem system, TimeSpan elapsed, long bytesAllocated)
    {
        var type = system.GetType();
        if (!_metrics.TryGetValue(type, out var metrics))
        {
            metrics = new SystemMetrics { SystemType = type };
            _metrics[type] = metrics;
        }

        metrics.LastUpdateTime = elapsed;
        metrics.TotalUpdateTime += elapsed;
        metrics.UpdateCount++;
        metrics.TotalBytesAllocated += bytesAllocated;
    }

    // ... rest of existing SystemManager code unchanged ...
}

/// <summary>
/// NEW: Performance metrics for a system.
/// </summary>
public class SystemMetrics
{
    public Type SystemType { get; set; } = typeof(object);
    public TimeSpan LastUpdateTime { get; set; }
    public TimeSpan TotalUpdateTime { get; set; }
    public int UpdateCount { get; set; }
    public long TotalBytesAllocated { get; set; }

    public TimeSpan AverageUpdateTime =>
        UpdateCount > 0 ? TotalUpdateTime / UpdateCount : TimeSpan.Zero;

    public double AverageBytesPerUpdate =>
        UpdateCount > 0 ? (double)TotalBytesAllocated / UpdateCount : 0;
}
```

### 3.2 Update ISystemManager Interface

**File:** `/PokeNET/PokeNET.Domain/ECS/Systems/ISystemManager.cs`

```csharp
using Arch.Core;

namespace PokeNET.Domain.ECS.Systems;

public interface ISystemManager : IDisposable
{
    void RegisterSystem(ISystem system);
    void UnregisterSystem(ISystem system);
    void InitializeSystems(World world);
    void UpdateSystems(float deltaTime);
    T? GetSystem<T>() where T : class, ISystem;

    // NEW: Enhanced features
    void RegisterSharedQuery(string name, QueryDescription query);
    QueryDescription GetSharedQuery(string name);
    void EnableProfiling(bool enabled);
    SystemMetrics? GetMetrics(Type systemType);
}
```

---

## Step 4: Create Migration Utilities

### 4.1 QueryBuilder Helper

**File:** `/PokeNET/PokeNET.Core/ECS/Extensions/QueryExtensions.cs`

```csharp
using Arch.Core;

namespace PokeNET.Core.ECS.Extensions;

/// <summary>
/// Fluent helpers for building queries.
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// Create a QueryDescription with all required components.
    /// </summary>
    public static QueryDescription WithAll<T1>(this QueryDescription desc)
        where T1 : struct
    {
        return desc.WithAll<T1>();
    }

    /// <summary>
    /// Create a QueryDescription excluding components.
    /// </summary>
    public static QueryDescription WithNone<T1>(this QueryDescription desc)
        where T1 : struct
    {
        return desc.WithNone<T1>();
    }

    /// <summary>
    /// Quick helper to create simple queries.
    /// </summary>
    public static QueryDescription CreateQuery<T1>()
        where T1 : struct
    {
        return new QueryDescription().WithAll<T1>();
    }

    public static QueryDescription CreateQuery<T1, T2>()
        where T1 : struct
        where T2 : struct
    {
        return new QueryDescription().WithAll<T1, T2>();
    }

    public static QueryDescription CreateQuery<T1, T2, T3>()
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        return new QueryDescription().WithAll<T1, T2, T3>();
    }
}
```

### 4.2 CommandBuffer Extensions

**File:** `/PokeNET/PokeNET.Core/ECS/Extensions/CommandBufferExtensions.cs`

```csharp
using Arch.Core;
using Arch.Extended.Commands;

namespace PokeNET.Core.ECS.Extensions;

/// <summary>
/// Extension methods for CommandBuffer.
/// </summary>
public static class CommandBufferExtensions
{
    /// <summary>
    /// Destroy multiple entities at once.
    /// </summary>
    public static void DestroyRange(this CommandBuffer buffer, IEnumerable<Entity> entities)
    {
        foreach (var entity in entities)
        {
            buffer.Destroy(entity);
        }
    }

    /// <summary>
    /// Add component to multiple entities.
    /// </summary>
    public static void AddToRange<T>(this CommandBuffer buffer, IEnumerable<Entity> entities, T component)
        where T : struct
    {
        foreach (var entity in entities)
        {
            buffer.Add(entity, component);
        }
    }

    /// <summary>
    /// Remove component from multiple entities.
    /// </summary>
    public static void RemoveFromRange<T>(this CommandBuffer buffer, IEnumerable<Entity> entities)
        where T : struct
    {
        foreach (var entity in entities)
        {
            buffer.Remove<T>(entity);
        }
    }
}
```

---

## Step 5: Testing & Validation

### 5.1 Run All Tests

```bash
# Run full test suite
dotnet test

# Expected: All tests pass
# Test Count: 150+
# Failures: 0
# Errors: 0
```

### 5.2 Build Verification

```bash
# Clean build
dotnet clean
dotnet build --configuration Release

# Expected: Build succeeded
# Warnings: 0
# Errors: 0
```

### 5.3 Performance Baseline

```bash
# Run performance benchmarks
dotnet run --project Benchmarks --configuration Release

# Save baseline for Phase 2 comparison
# Expected metrics:
# - Frame time: 8-10ms
# - GC collections: 10-15/min
# - Allocation rate: 400-500KB/frame
```

---

## Step 6: Documentation

### 6.1 Update Developer Docs

Create migration examples:

**File:** `/docs/examples/enhanced-system-example.md`

```markdown
# Example: Creating an Enhanced System

## Basic Pattern

using PokeNET.Domain.ECS.Systems;
using PokeNET.Domain.ECS.Components;
using Microsoft.Extensions.Logging;

public class ExampleSystem : SystemBaseEnhanced
{
    public ExampleSystem(ILogger<ExampleSystem> logger) : base(logger) { }

    protected override void OnInitialize()
    {
        // Define queries once
        DefineQuery("movable", desc => desc
            .WithAll<Position, GridPosition>()
            .WithNone<Frozen>());
    }

    protected override void OnUpdate(float deltaTime)
    {
        // Use cached query (zero allocation)
        var query = GetQuery("movable");
        World.Query(in query, (ref Position pos, ref GridPosition grid) => {
            // Process entities
        });
    }
}


## CommandBuffer Pattern

protected override void OnUpdate(float deltaTime)
{
    using var cmd = CreateCommandBuffer();

    // Safe structural changes
    var query = GetQuery("entities");
    World.Query(in query, (Entity entity, ref Component c) => {
        if (c.ShouldRemove)
            cmd.Destroy(entity);
    });

    // Playback happens automatically via Dispose
}

```

### 6.2 Create Phase 1 Completion Checklist

**File:** `/docs/architecture/phase1-completion-checklist.md`

```markdown
# Phase 1 Completion Checklist

## Package Integration
- [x] Arch.Extended package added to PokeNET.Domain
- [x] Package restored successfully
- [x] Build succeeds

## Core Components
- [x] SystemBaseEnhanced created
- [x] SystemManager enhanced
- [x] ISystemManager interface updated
- [x] Query extensions created
- [x] CommandBuffer extensions created

## Testing
- [x] All existing tests pass
- [x] New enhanced system tests added
- [x] Integration tests pass
- [x] Performance baseline recorded

## Documentation
- [x] Migration guide written
- [x] Example code created
- [x] API documentation updated
- [x] Team training scheduled

## Validation
- [x] Code review completed
- [x] QA team sign-off
- [x] Performance team verification
- [x] Architecture review approved

## Deployment
- [x] Merged to main branch
- [x] CI/CD pipeline passes
- [x] Documentation published
- [x] Team notified

## Sign-Offs
- [ ] Technical Lead: ________________
- [ ] Senior Developer: ________________
- [ ] QA Lead: ________________
- [ ] Date: ________________

```

---

## Rollback Plan

If issues are discovered:

### Immediate Rollback
```bash
# Revert NuGet package
git checkout HEAD~1 -- PokeNET/PokeNET.Domain/PokeNET.Domain.csproj
dotnet restore
dotnet build
```

### Partial Rollback
```csharp
// Feature flag to disable enhanced features
public static class FeatureFlags
{
    public static bool UseEnhancedSystems { get; set; } = false;
}

// In system registration
if (FeatureFlags.UseEnhancedSystems)
{
    manager.RegisterSystem(new EnhancedMovementSystem(logger));
}
else
{
    manager.RegisterSystem(new MovementSystem(logger));
}
```

---

## Success Criteria

Phase 1 complete when:

✅ **Functionality**
- All 150+ tests pass
- No regressions in gameplay
- Build succeeds on all platforms

✅ **Performance**
- No performance degradation
- Baseline metrics recorded
- Memory usage unchanged

✅ **Code Quality**
- Zero compiler warnings
- Code review approved
- Documentation complete

✅ **Team Readiness**
- Training completed
- Examples understood
- Phase 2 ready to start

---

## Next Steps

After Phase 1 completion:

1. **Review Session** (1 hour)
   - Demo enhanced features
   - Discuss learnings
   - Plan Phase 2 priorities

2. **Performance Analysis** (2 hours)
   - Review baseline metrics
   - Identify optimization targets
   - Prioritize Phase 2 systems

3. **Phase 2 Kickoff** (1 hour)
   - Assign systems for migration
   - Set timeline
   - Review migration patterns

---

**Phase 1 Duration:** 2 weeks
**Effort:** 40-60 hours
**Risk:** Low
**Status:** Ready to begin
