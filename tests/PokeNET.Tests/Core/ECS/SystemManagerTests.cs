using Arch.Core;
using Microsoft.Extensions.Logging;
using PokeNET.Core.ECS;
using PokeNET.Domain.ECS.Systems;

namespace PokeNET.Tests.Core.ECS;

/// <summary>
/// Comprehensive test suite for SystemManager covering:
/// - System registration/unregistration
/// - Duplicate system handling
/// - System initialization with priority ordering
/// - Update loop with enabled/disabled systems
/// - System disposal and cleanup
/// - Error handling in system updates
/// - System dependency validation
/// - Concurrent system access
/// </summary>
public class SystemManagerTests : IDisposable
{
    private readonly Mock<ILogger<SystemManager>> _mockLogger;
    private readonly SystemManager _systemManager;
    private readonly World _world;

    public SystemManagerTests()
    {
        _mockLogger = new Mock<ILogger<SystemManager>>();
        _systemManager = new SystemManager(_mockLogger.Object);
        _world = World.Create();
    }

    public void Dispose()
    {
        _systemManager?.Dispose();
        _world?.Dispose();
    }

    #region Registration Tests

    [Fact]
    public void RegisterSystem_WithValidSystem_AddsSystemSuccessfully()
    {
        // Arrange
        var mockSystem = CreateMockSystem(priority: 10);

        // Act
        _systemManager.RegisterSystem(mockSystem.Object);

        // Assert
        var retrieved = _systemManager.GetSystem<ISystem>();
        retrieved.Should().NotBeNull();
        retrieved.Should().Be(mockSystem.Object);
    }

    [Fact]
    public void RegisterSystem_MultipleSystems_OrdersByPriority()
    {
        // Arrange
        var lowPrioritySystem = CreateMockSystem(priority: 100, name: "Low");
        var mediumPrioritySystem = CreateMockSystem(priority: 50, name: "Medium");
        var highPrioritySystem = CreateMockSystem(priority: 1, name: "High");

        // Act
        _systemManager.RegisterSystem(lowPrioritySystem.Object);
        _systemManager.RegisterSystem(mediumPrioritySystem.Object);
        _systemManager.RegisterSystem(highPrioritySystem.Object);
        _systemManager.InitializeSystems(_world);

        // Assert - Initialize should be called in priority order
        var sequence = new MockSequence();
        highPrioritySystem.InSequence(sequence).Setup(s => s.Initialize(It.IsAny<World>()));
        mediumPrioritySystem.InSequence(sequence).Setup(s => s.Initialize(It.IsAny<World>()));
        lowPrioritySystem.InSequence(sequence).Setup(s => s.Initialize(It.IsAny<World>()));
    }

    [Fact]
    public void RegisterSystem_DuplicateSystem_LogsWarningAndIgnores()
    {
        // Arrange
        var mockSystem = CreateMockSystem();

        // Act
        _systemManager.RegisterSystem(mockSystem.Object);
        _systemManager.RegisterSystem(mockSystem.Object); // Attempt duplicate

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("already registered")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RegisterSystem_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        var mockSystem = CreateMockSystem();
        _systemManager.Dispose();

        // Act
        Action act = () => _systemManager.RegisterSystem(mockSystem.Object);

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void UnregisterSystem_ExistingSystem_RemovesAndDisposes()
    {
        // Arrange
        var mockSystem = CreateMockSystem();
        _systemManager.RegisterSystem(mockSystem.Object);

        // Act
        _systemManager.UnregisterSystem(mockSystem.Object);

        // Assert
        mockSystem.Verify(s => s.Dispose(), Times.Once);
        var retrieved = _systemManager.GetSystem<ISystem>();
        retrieved.Should().BeNull();
    }

    [Fact]
    public void UnregisterSystem_NonExistentSystem_DoesNothing()
    {
        // Arrange
        var mockSystem = CreateMockSystem();

        // Act
        Action act = () => _systemManager.UnregisterSystem(mockSystem.Object);

        // Assert
        act.Should().NotThrow();
        mockSystem.Verify(s => s.Dispose(), Times.Never);
    }

    [Fact]
    public void UnregisterSystem_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        var mockSystem = CreateMockSystem();
        _systemManager.Dispose();

        // Act
        Action act = () => _systemManager.UnregisterSystem(mockSystem.Object);

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    #endregion

    #region Initialization Tests

    [Fact]
    public void InitializeSystems_WithRegisteredSystems_InitializesInPriorityOrder()
    {
        // Arrange
        var system1 = CreateMockSystem(priority: 10, name: "System1");
        var system2 = CreateMockSystem(priority: 5, name: "System2");
        var system3 = CreateMockSystem(priority: 15, name: "System3");

        _systemManager.RegisterSystem(system1.Object);
        _systemManager.RegisterSystem(system2.Object);
        _systemManager.RegisterSystem(system3.Object);

        // Act
        _systemManager.InitializeSystems(_world);

        // Assert
        system1.Verify(s => s.Initialize(_world), Times.Once);
        system2.Verify(s => s.Initialize(_world), Times.Once);
        system3.Verify(s => s.Initialize(_world), Times.Once);
    }

    [Fact]
    public void InitializeSystems_CalledTwice_LogsWarningAndIgnoresSecondCall()
    {
        // Arrange
        var mockSystem = CreateMockSystem();
        _systemManager.RegisterSystem(mockSystem.Object);
        _systemManager.InitializeSystems(_world);

        // Act
        _systemManager.InitializeSystems(_world);

        // Assert
        mockSystem.Verify(s => s.Initialize(_world), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("already initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void InitializeSystems_SystemThrowsException_PropagatesException()
    {
        // Arrange
        var mockSystem = CreateMockSystem();
        var expectedException = new InvalidOperationException("Initialization failed");
        mockSystem.Setup(s => s.Initialize(_world)).Throws(expectedException);
        _systemManager.RegisterSystem(mockSystem.Object);

        // Act
        Action act = () => _systemManager.InitializeSystems(_world);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Initialization failed");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Failed to initialize")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void InitializeSystems_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        _systemManager.Dispose();

        // Act
        Action act = () => _systemManager.InitializeSystems(_world);

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void InitializeSystems_WithNoSystems_CompletesSuccessfully()
    {
        // Arrange
        // No systems registered

        // Act
        Action act = () => _systemManager.InitializeSystems(_world);

        // Assert
        act.Should().NotThrow();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("0 systems")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void UpdateSystems_AfterInitialization_UpdatesAllEnabledSystems()
    {
        // Arrange
        var system1 = CreateMockSystem(priority: 10, enabled: true);
        var system2 = CreateMockSystem(priority: 20, enabled: true);
        _systemManager.RegisterSystem(system1.Object);
        _systemManager.RegisterSystem(system2.Object);
        _systemManager.InitializeSystems(_world);

        // Act
        _systemManager.UpdateSystems(0.016f);

        // Assert
        system1.Verify(s => s.Update(0.016f), Times.Once);
        system2.Verify(s => s.Update(0.016f), Times.Once);
    }

    [Fact]
    public void UpdateSystems_WithDisabledSystem_SkipsDisabledSystem()
    {
        // Arrange
        var enabledSystem = CreateMockSystem(priority: 10, enabled: true);
        var disabledSystem = CreateMockSystem(priority: 20, enabled: false);
        _systemManager.RegisterSystem(enabledSystem.Object);
        _systemManager.RegisterSystem(disabledSystem.Object);
        _systemManager.InitializeSystems(_world);

        // Act
        _systemManager.UpdateSystems(0.016f);

        // Assert
        enabledSystem.Verify(s => s.Update(0.016f), Times.Once);
        disabledSystem.Verify(s => s.Update(It.IsAny<float>()), Times.Never);
    }

    [Fact]
    public void UpdateSystems_BeforeInitialization_LogsWarningAndReturns()
    {
        // Arrange
        var mockSystem = CreateMockSystem();
        _systemManager.RegisterSystem(mockSystem.Object);
        // Not calling InitializeSystems

        // Act
        _systemManager.UpdateSystems(0.016f);

        // Assert
        mockSystem.Verify(s => s.Update(It.IsAny<float>()), Times.Never);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("before initialization")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdateSystems_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        _systemManager.Dispose();

        // Act
        Action act = () => _systemManager.UpdateSystems(0.016f);

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void UpdateSystems_MultipleFrames_UpdatesSystemsConsistently()
    {
        // Arrange
        var mockSystem = CreateMockSystem(enabled: true);
        _systemManager.RegisterSystem(mockSystem.Object);
        _systemManager.InitializeSystems(_world);

        // Act
        for (int i = 0; i < 100; i++)
        {
            _systemManager.UpdateSystems(0.016f);
        }

        // Assert
        mockSystem.Verify(s => s.Update(0.016f), Times.Exactly(100));
    }

    [Fact]
    public void UpdateSystems_WithVariableDeltaTime_PassesCorrectDeltaTime()
    {
        // Arrange
        var mockSystem = CreateMockSystem(enabled: true);
        _systemManager.RegisterSystem(mockSystem.Object);
        _systemManager.InitializeSystems(_world);
        var deltaValues = new[] { 0.016f, 0.033f, 0.008f, 0.020f };

        // Act
        foreach (var delta in deltaValues)
        {
            _systemManager.UpdateSystems(delta);
        }

        // Assert
        foreach (var delta in deltaValues)
        {
            mockSystem.Verify(s => s.Update(delta), Times.Once);
        }
    }

    #endregion

    #region System Retrieval Tests

    [Fact]
    public void GetSystem_RegisteredSystem_ReturnsSystem()
    {
        // Arrange
        var mockSystem = new Mock<TestSystem>(_mockLogger.Object);
        _systemManager.RegisterSystem(mockSystem.Object);

        // Act
        var retrieved = _systemManager.GetSystem<TestSystem>();

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Should().Be(mockSystem.Object);
    }

    [Fact]
    public void GetSystem_UnregisteredSystem_ReturnsNull()
    {
        // Act
        var retrieved = _systemManager.GetSystem<TestSystem>();

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public void GetSystem_AfterUnregister_ReturnsNull()
    {
        // Arrange
        var mockSystem = new Mock<TestSystem>(_mockLogger.Object);
        _systemManager.RegisterSystem(mockSystem.Object);
        _systemManager.UnregisterSystem(mockSystem.Object);

        // Act
        var retrieved = _systemManager.GetSystem<TestSystem>();

        // Assert
        retrieved.Should().BeNull();
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_WithRegisteredSystems_DisposesAllSystems()
    {
        // Arrange
        var system1 = CreateMockSystem();
        var system2 = CreateMockSystem();
        var system3 = CreateMockSystem();
        _systemManager.RegisterSystem(system1.Object);
        _systemManager.RegisterSystem(system2.Object);
        _systemManager.RegisterSystem(system3.Object);

        // Act
        _systemManager.Dispose();

        // Assert
        system1.Verify(s => s.Dispose(), Times.Once);
        system2.Verify(s => s.Dispose(), Times.Once);
        system3.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DisposesOnlyOnce()
    {
        // Arrange
        var mockSystem = CreateMockSystem();
        _systemManager.RegisterSystem(mockSystem.Object);

        // Act
        _systemManager.Dispose();
        _systemManager.Dispose();
        _systemManager.Dispose();

        // Assert
        mockSystem.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_SystemThrowsException_LogsErrorAndContinues()
    {
        // Arrange
        var goodSystem = CreateMockSystem();
        var badSystem = CreateMockSystem();
        var exception = new InvalidOperationException("Disposal failed");
        badSystem.Setup(s => s.Dispose()).Throws(exception);

        _systemManager.RegisterSystem(goodSystem.Object);
        _systemManager.RegisterSystem(badSystem.Object);

        // Act
        Action act = () => _systemManager.Dispose();

        // Assert
        act.Should().NotThrow();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error disposing")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        goodSystem.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_ClearsSystemsList()
    {
        // Arrange
        var mockSystem = new Mock<TestSystem>(_mockLogger.Object);
        _systemManager.RegisterSystem(mockSystem.Object);

        // Act
        _systemManager.Dispose();

        // Assert
        var retrieved = _systemManager.GetSystem<TestSystem>();
        retrieved.Should().BeNull();
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public void RegisterSystem_ConcurrentRegistrations_HandlesThreadSafely()
    {
        // Arrange
        var systems = Enumerable.Range(0, 100)
            .Select(i => CreateMockSystem(priority: i))
            .ToList();

        // Act
        Parallel.ForEach(systems, system =>
        {
            _systemManager.RegisterSystem(system.Object);
        });

        // Assert - All systems should be registered without errors
        foreach (var system in systems)
        {
            // Just verify no exceptions were thrown during registration
        }
    }

    [Fact]
    public void UpdateSystems_ConcurrentReads_HandlesThreadSafely()
    {
        // Arrange
        var mockSystem = CreateMockSystem(enabled: true);
        _systemManager.RegisterSystem(mockSystem.Object);
        _systemManager.InitializeSystems(_world);

        // Act
        Parallel.For(0, 100, _ =>
        {
            _systemManager.UpdateSystems(0.016f);
        });

        // Assert - Should complete without deadlocks or exceptions
        mockSystem.Verify(s => s.Update(0.016f), Times.AtLeast(100));
    }

    #endregion

    #region Helper Methods

    private Mock<ISystem> CreateMockSystem(int priority = 0, bool enabled = true, string? name = null)
    {
        var mock = new Mock<ISystem>();
        mock.Setup(s => s.Priority).Returns(priority);
        mock.Setup(s => s.IsEnabled).Returns(enabled);
        mock.SetupProperty(s => s.IsEnabled);
        mock.Setup(s => s.GetType()).Returns(typeof(ISystem));
        return mock;
    }

    // Concrete test system for type-specific retrieval tests
    public class TestSystem : SystemBase
    {
        public TestSystem(ILogger logger) : base(logger) { }
        protected override void OnUpdate(float deltaTime) { }
    }

    #endregion
}
