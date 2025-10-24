using Arch.Core;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.ECS.Systems;

namespace PokeNET.Tests.Domain.ECS.Systems;

/// <summary>
/// Comprehensive test suite for SystemBase covering:
/// - System initialization lifecycle
/// - Update method execution
/// - System enabled/disabled state
/// - World access and queries
/// - System dependencies
/// - Error handling and recovery
/// - Disposal patterns
/// - Performance metrics collection
/// </summary>
public class SystemBaseTests : IDisposable
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly World _world;

    public SystemBaseTests()
    {
        _mockLogger = new Mock<ILogger>();
        _world = World.Create();
    }

    public void Dispose()
    {
        _world?.Dispose();
    }

    #region Initialization Tests

    [Fact]
    public void Initialize_ValidWorld_SetsWorldAndCallsOnInitialize()
    {
        // Arrange
        var system = new TestSystem(_mockLogger.Object);

        // Act
        system.Initialize(_world);

        // Assert
        system.World.Should().Be(_world);
        system.OnInitializeCalled.Should().BeTrue();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Initialize_CalledMultipleTimes_UpdatesWorldEachTime()
    {
        // Arrange
        var system = new TestSystem(_mockLogger.Object);
        var world1 = World.Create();
        var world2 = World.Create();

        try
        {
            // Act
            system.Initialize(world1);
            system.Initialize(world2);

            // Assert
            system.World.Should().Be(world2);
            system.OnInitializeCalled.Should().BeTrue();
        }
        finally
        {
            world1.Dispose();
            world2.Dispose();
        }
    }

    [Fact]
    public void OnInitialize_CanBeOverridden_ExecutesCustomLogic()
    {
        // Arrange
        var customSystem = new CustomInitSystem(_mockLogger.Object);

        // Act
        customSystem.Initialize(_world);

        // Assert
        customSystem.CustomInitCalled.Should().BeTrue();
    }

    [Fact]
    public void Priority_DefaultValue_IsZero()
    {
        // Arrange
        var system = new TestSystem(_mockLogger.Object);

        // Act
        var priority = system.Priority;

        // Assert
        priority.Should().Be(0);
    }

    [Fact]
    public void Priority_CanBeOverridden_ReturnsCustomValue()
    {
        // Arrange
        var highPrioritySystem = new HighPrioritySystem(_mockLogger.Object);

        // Act
        var priority = highPrioritySystem.Priority;

        // Assert
        priority.Should().Be(-100);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_WhenEnabled_CallsOnUpdate()
    {
        // Arrange
        var system = new TestSystem(_mockLogger.Object);
        system.Initialize(_world);

        // Act
        system.Update(0.016f);

        // Assert
        system.OnUpdateCalled.Should().BeTrue();
        system.LastDeltaTime.Should().Be(0.016f);
    }

    [Fact]
    public void Update_WhenDisabled_DoesNotCallOnUpdate()
    {
        // Arrange
        var system = new TestSystem(_mockLogger.Object);
        system.Initialize(_world);
        system.IsEnabled = false;

        // Act
        system.Update(0.016f);

        // Assert
        system.OnUpdateCalled.Should().BeFalse();
    }

    [Fact]
    public void Update_MultipleFrames_CallsOnUpdateEachTime()
    {
        // Arrange
        var system = new TestSystem(_mockLogger.Object);
        system.Initialize(_world);

        // Act
        system.Update(0.016f);
        system.Update(0.033f);
        system.Update(0.008f);

        // Assert
        system.UpdateCallCount.Should().Be(3);
        system.LastDeltaTime.Should().Be(0.008f);
    }

    [Fact]
    public void Update_OnUpdateThrowsException_PropagatesException()
    {
        // Arrange
        var system = new ThrowingSystem(_mockLogger.Object);
        system.Initialize(_world);

        // Act
        Action act = () => system.Update(0.016f);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("OnUpdate error");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error in system")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Update_VariableDeltaTime_PassesCorrectValues()
    {
        // Arrange
        var system = new TestSystem(_mockLogger.Object);
        system.Initialize(_world);
        var deltaValues = new[] { 0.016f, 0.033f, 0.008f, 0.020f, 0.100f };

        // Act & Assert
        foreach (var delta in deltaValues)
        {
            system.Update(delta);
            system.LastDeltaTime.Should().Be(delta);
        }
    }

    [Fact]
    public void IsEnabled_DefaultValue_IsTrue()
    {
        // Arrange
        var system = new TestSystem(_mockLogger.Object);

        // Act
        var isEnabled = system.IsEnabled;

        // Assert
        isEnabled.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_CanBeToggled_AffectsUpdateBehavior()
    {
        // Arrange
        var system = new TestSystem(_mockLogger.Object);
        system.Initialize(_world);

        // Act & Assert - Enabled
        system.IsEnabled = true;
        system.Update(0.016f);
        system.UpdateCallCount.Should().Be(1);

        // Disable
        system.IsEnabled = false;
        system.Update(0.016f);
        system.UpdateCallCount.Should().Be(1); // Still 1, no increment

        // Re-enable
        system.IsEnabled = true;
        system.Update(0.016f);
        system.UpdateCallCount.Should().Be(2);
    }

    #endregion

    #region World Access Tests

    [Fact]
    public void World_AfterInitialization_IsAccessible()
    {
        // Arrange
        var system = new TestSystem(_mockLogger.Object);

        // Act
        system.Initialize(_world);

        // Assert
        system.World.Should().Be(_world);
        system.World.Should().NotBeNull();
    }

    [Fact]
    public void World_BeforeInitialization_ThrowsInvalidOperationException()
    {
        // Arrange
        var system = new TestSystem(_mockLogger.Object);

        // Act & Assert
        Action act = () => { _ = system.World; };
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Fact]
    public void World_CanBeUsedInOnUpdate_AccessesEntities()
    {
        // Arrange
        var system = new WorldAccessSystem(_mockLogger.Object);
        var entity = _world.Create(new TestComponent { Value = 42 });
        system.Initialize(_world);

        // Act
        system.Update(0.016f);

        // Assert
        system.EntityCount.Should().Be(1);
        system.ComponentValue.Should().Be(42);
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_CallsOnDispose_LogsDisposal()
    {
        // Arrange
        var system = new TestSystem(_mockLogger.Object);
        system.Initialize(_world);

        // Act
        system.Dispose();

        // Assert
        system.OnDisposeCalled.Should().BeTrue();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("disposed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_NoError()
    {
        // Arrange
        var system = new TestSystem(_mockLogger.Object);

        // Act
        system.Dispose();
        system.Dispose();
        system.Dispose();

        // Assert - Should complete without errors
        system.OnDisposeCalled.Should().BeTrue();
    }

    [Fact]
    public void OnDispose_CanBeOverridden_ExecutesCustomCleanup()
    {
        // Arrange
        var customSystem = new CustomDisposeSystem(_mockLogger.Object);

        // Act
        customSystem.Dispose();

        // Assert
        customSystem.CustomDisposeCalled.Should().BeTrue();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void Update_OnInitializeThrowsException_PropagatesException()
    {
        // Arrange
        var system = new InitThrowingSystem(_mockLogger.Object);

        // Act
        Action act = () => system.Initialize(_world);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Init error");
    }

    [Fact]
    public void Update_AfterException_CanRecoverInNextUpdate()
    {
        // Arrange
        var system = new RecoverableSystem(_mockLogger.Object);
        system.Initialize(_world);

        // Act & Assert
        system.ShouldThrow = true;
        Action act1 = () => system.Update(0.016f);
        act1.Should().Throw<InvalidOperationException>();

        system.ShouldThrow = false;
        Action act2 = () => system.Update(0.016f);
        act2.Should().NotThrow();
        system.UpdateCallCount.Should().Be(1); // Only the successful update counts
    }

    #endregion

    #region Logger Access Tests

    [Fact]
    public void Logger_IsAccessibleToSubclasses()
    {
        // Arrange & Act
        var system = new LoggingSystem(_mockLogger.Object);
        system.Initialize(_world);
        system.Update(0.016f);

        // Assert
        system.LoggerUsed.Should().BeTrue();
    }

    [Fact]
    public void Logger_LogsCustomMessages()
    {
        // Arrange
        var system = new LoggingSystem(_mockLogger.Object);

        // Act
        system.LogCustomMessage();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Custom message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void Update_ThousandsOfUpdates_MaintainsPerformance()
    {
        // Arrange
        var system = new TestSystem(_mockLogger.Object);
        system.Initialize(_world);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < 10000; i++)
        {
            system.Update(0.016f);
        }

        stopwatch.Stop();

        // Assert
        system.UpdateCallCount.Should().Be(10000);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100); // Should be very fast
    }

    [Fact]
    public void Update_WithWorldQueries_PerformsEfficiently()
    {
        // Arrange
        var system = new WorldAccessSystem(_mockLogger.Object);

        // Create 1000 entities
        for (int i = 0; i < 1000; i++)
        {
            _world.Create(new TestComponent { Value = i });
        }

        system.Initialize(_world);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < 100; i++)
        {
            system.Update(0.016f);
        }

        stopwatch.Stop();

        // Assert
        system.EntityCount.Should().Be(1000);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    #endregion

    #region Lifecycle Integration Tests

    [Fact]
    public void SystemLifecycle_CompleteFlow_ExecutesCorrectly()
    {
        // Arrange
        var system = new TestSystem(_mockLogger.Object);

        // Act & Assert - Initialize
        system.OnInitializeCalled.Should().BeFalse();
        system.Initialize(_world);
        system.OnInitializeCalled.Should().BeTrue();
        system.World.Should().Be(_world);

        // Update
        system.OnUpdateCalled.Should().BeFalse();
        system.Update(0.016f);
        system.OnUpdateCalled.Should().BeTrue();

        // Disable
        system.IsEnabled = false;
        system.Update(0.016f);
        system.UpdateCallCount.Should().Be(1); // No additional update

        // Re-enable
        system.IsEnabled = true;
        system.Update(0.016f);
        system.UpdateCallCount.Should().Be(2);

        // Dispose
        system.OnDisposeCalled.Should().BeFalse();
        system.Dispose();
        system.OnDisposeCalled.Should().BeTrue();
    }

    [Fact]
    public void SystemLifecycle_UpdateBeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var system = new SafeSystem(_mockLogger.Object);

        // Act
        Action act = () => system.Update(0.016f);

        // Assert - Should throw because World is not initialized
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    #endregion

    #region Helper Classes

    private class TestSystem : SystemBase
    {
        public bool OnInitializeCalled { get; private set; }
        public bool OnUpdateCalled { get; private set; }
        public bool OnDisposeCalled { get; private set; }
        public float LastDeltaTime { get; private set; }
        public int UpdateCallCount { get; private set; }

        // Expose World for test assertions
        public new World? World => base.World;

        public TestSystem(ILogger logger) : base(logger) { }

        protected override void OnInitialize()
        {
            OnInitializeCalled = true;
        }

        protected override void OnUpdate(float deltaTime)
        {
            OnUpdateCalled = true;
            LastDeltaTime = deltaTime;
            UpdateCallCount++;
        }

        protected override void OnDispose()
        {
            OnDisposeCalled = true;
        }
    }

    private class CustomInitSystem : SystemBase
    {
        public bool CustomInitCalled { get; private set; }

        public CustomInitSystem(ILogger logger) : base(logger) { }

        protected override void OnInitialize()
        {
            CustomInitCalled = true;
        }

        protected override void OnUpdate(float deltaTime) { }
    }

    private class CustomDisposeSystem : SystemBase
    {
        public bool CustomDisposeCalled { get; private set; }

        public CustomDisposeSystem(ILogger logger) : base(logger) { }

        protected override void OnUpdate(float deltaTime) { }

        protected override void OnDispose()
        {
            CustomDisposeCalled = true;
        }
    }

    private class HighPrioritySystem : SystemBase
    {
        public override int Priority => -100;

        public HighPrioritySystem(ILogger logger) : base(logger) { }

        protected override void OnUpdate(float deltaTime) { }
    }

    private class ThrowingSystem : SystemBase
    {
        public ThrowingSystem(ILogger logger) : base(logger) { }

        protected override void OnUpdate(float deltaTime)
        {
            throw new InvalidOperationException("OnUpdate error");
        }
    }

    private class InitThrowingSystem : SystemBase
    {
        public InitThrowingSystem(ILogger logger) : base(logger) { }

        protected override void OnInitialize()
        {
            throw new InvalidOperationException("Init error");
        }

        protected override void OnUpdate(float deltaTime) { }
    }

    private class RecoverableSystem : SystemBase
    {
        public bool ShouldThrow { get; set; }
        public int UpdateCallCount { get; private set; }

        public RecoverableSystem(ILogger logger) : base(logger) { }

        protected override void OnUpdate(float deltaTime)
        {
            if (ShouldThrow)
                throw new InvalidOperationException("Recoverable error");

            UpdateCallCount++;
        }
    }

    private class WorldAccessSystem : SystemBase
    {
        public int EntityCount { get; private set; }
        public int ComponentValue { get; private set; }

        public WorldAccessSystem(ILogger logger) : base(logger) { }

        protected override void OnUpdate(float deltaTime)
        {
            var queryDesc = new QueryDescription().WithAll<TestComponent>();
            EntityCount = World.CountEntities(in queryDesc);

            World.Query(in queryDesc, (ref TestComponent comp) =>
            {
                ComponentValue = comp.Value;
            });
        }
    }

    private class LoggingSystem : SystemBase
    {
        public bool LoggerUsed { get; private set; }

        public LoggingSystem(ILogger logger) : base(logger) { }

        protected override void OnUpdate(float deltaTime)
        {
            LoggerUsed = Logger != null;
        }

        public void LogCustomMessage()
        {
            Logger.LogDebug("Custom message from system");
        }
    }

    private class SafeSystem : SystemBase
    {
        public SafeSystem(ILogger logger) : base(logger) { }

        protected override void OnUpdate(float deltaTime)
        {
            // Access World to test uninitialized state
            // This will throw if not initialized
            var queryDesc = new QueryDescription();
            _ = World.CountEntities(in queryDesc);
        }
    }

    private struct TestComponent
    {
        public int Value;
    }

    #endregion
}
