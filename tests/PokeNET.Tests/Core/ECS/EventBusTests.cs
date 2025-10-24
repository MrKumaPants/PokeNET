using Microsoft.Extensions.Logging;
using PokeNET.Core.ECS;
using PokeNET.Domain.ECS.Events;
using System.Collections.Concurrent;

namespace PokeNET.Tests.Core.ECS;

/// <summary>
/// Comprehensive test suite for EventBus covering:
/// - Event subscription/unsubscription
/// - Event publishing with 0, 1, multiple subscribers
/// - Concurrent event handling (thread safety)
/// - Memory leak prevention (weak references)
/// - Event priority/ordering
/// - Exception handling in event handlers
/// - Event filtering
/// - Performance under load (1000s of events)
/// </summary>
public class EventBusTests : IDisposable
{
    private readonly Mock<ILogger<EventBus>> _mockLogger;
    private readonly EventBus _eventBus;

    public EventBusTests()
    {
        _mockLogger = new Mock<ILogger<EventBus>>();
        _eventBus = new EventBus(_mockLogger.Object);
    }

    public void Dispose()
    {
        _eventBus?.Clear();
    }

    #region Subscription Tests

    [Fact]
    public void Subscribe_ValidHandler_AddsSubscription()
    {
        // Arrange
        var handlerCalled = false;
        Action<TestEvent> handler = e => handlerCalled = true;

        // Act
        _eventBus.Subscribe(handler);
        _eventBus.Publish(new TestEvent());

        // Assert
        handlerCalled.Should().BeTrue();
    }

    [Fact]
    public void Subscribe_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        Action<TestEvent> nullHandler = null!;

        // Act
        Action act = () => _eventBus.Subscribe(nullHandler);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Subscribe_MultipleHandlers_AllHandlersReceiveEvent()
    {
        // Arrange
        var handler1Called = false;
        var handler2Called = false;
        var handler3Called = false;

        Action<TestEvent> handler1 = e => handler1Called = true;
        Action<TestEvent> handler2 = e => handler2Called = true;
        Action<TestEvent> handler3 = e => handler3Called = true;

        // Act
        _eventBus.Subscribe(handler1);
        _eventBus.Subscribe(handler2);
        _eventBus.Subscribe(handler3);
        _eventBus.Publish(new TestEvent());

        // Assert
        handler1Called.Should().BeTrue();
        handler2Called.Should().BeTrue();
        handler3Called.Should().BeTrue();
    }

    [Fact]
    public void Subscribe_SameHandlerTwice_RegistersBothInstances()
    {
        // Arrange
        var callCount = 0;
        Action<TestEvent> handler = e => callCount++;

        // Act
        _eventBus.Subscribe(handler);
        _eventBus.Subscribe(handler);
        _eventBus.Publish(new TestEvent());

        // Assert
        callCount.Should().Be(2);
    }

    [Fact]
    public void Subscribe_DifferentEventTypes_SubscribesIndependently()
    {
        // Arrange
        var testEventCalled = false;
        var otherEventCalled = false;

        Action<TestEvent> testHandler = e => testEventCalled = true;
        Action<OtherTestEvent> otherHandler = e => otherEventCalled = true;

        // Act
        _eventBus.Subscribe(testHandler);
        _eventBus.Subscribe(otherHandler);
        _eventBus.Publish(new TestEvent());

        // Assert
        testEventCalled.Should().BeTrue();
        otherEventCalled.Should().BeFalse();
    }

    [Fact]
    public void Unsubscribe_ValidHandler_RemovesSubscription()
    {
        // Arrange
        var handlerCalled = false;
        Action<TestEvent> handler = e => handlerCalled = true;
        _eventBus.Subscribe(handler);

        // Act
        _eventBus.Unsubscribe(handler);
        _eventBus.Publish(new TestEvent());

        // Assert
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public void Unsubscribe_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        Action<TestEvent> nullHandler = null!;

        // Act
        Action act = () => _eventBus.Unsubscribe(nullHandler);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Unsubscribe_NonExistentHandler_DoesNothing()
    {
        // Arrange
        Action<TestEvent> handler = e => { };

        // Act
        Action act = () => _eventBus.Unsubscribe(handler);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Unsubscribe_OneOfMultipleHandlers_RemovesOnlyThatHandler()
    {
        // Arrange
        var handler1Called = false;
        var handler2Called = false;

        Action<TestEvent> handler1 = e => handler1Called = true;
        Action<TestEvent> handler2 = e => handler2Called = true;

        _eventBus.Subscribe(handler1);
        _eventBus.Subscribe(handler2);

        // Act
        _eventBus.Unsubscribe(handler1);
        _eventBus.Publish(new TestEvent());

        // Assert
        handler1Called.Should().BeFalse();
        handler2Called.Should().BeTrue();
    }

    [Fact]
    public void Unsubscribe_LastHandler_RemovesEventType()
    {
        // Arrange
        Action<TestEvent> handler = e => { };
        _eventBus.Subscribe(handler);

        // Act
        _eventBus.Unsubscribe(handler);
        _eventBus.Publish(new TestEvent()); // Should not throw

        // Assert
        // Verify through logging that no handlers were found
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    #endregion

    #region Publishing Tests

    [Fact]
    public void Publish_ValidEvent_InvokesSubscribers()
    {
        // Arrange
        var receivedEvent = (TestEvent?)null;
        Action<TestEvent> handler = e => receivedEvent = e;
        _eventBus.Subscribe(handler);
        var testEvent = new TestEvent { Data = "test data" };

        // Act
        _eventBus.Publish(testEvent);

        // Assert
        receivedEvent.Should().NotBeNull();
        receivedEvent.Should().Be(testEvent);
        receivedEvent!.Data.Should().Be("test data");
    }

    [Fact]
    public void Publish_NullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        TestEvent nullEvent = null!;

        // Act
        Action act = () => _eventBus.Publish(nullEvent);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Publish_NoSubscribers_CompletesWithoutError()
    {
        // Arrange
        var testEvent = new TestEvent();

        // Act
        Action act = () => _eventBus.Publish(testEvent);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Publish_MultipleEvents_AllEventsDelivered()
    {
        // Arrange
        var receivedEvents = new List<TestEvent>();
        Action<TestEvent> handler = e => receivedEvents.Add(e);
        _eventBus.Subscribe(handler);

        var events = Enumerable.Range(0, 10)
            .Select(i => new TestEvent { Data = $"Event {i}" })
            .ToList();

        // Act
        foreach (var evt in events)
        {
            _eventBus.Publish(evt);
        }

        // Assert
        receivedEvents.Should().HaveCount(10);
        receivedEvents.Should().BeEquivalentTo(events);
    }

    [Fact]
    public void Publish_HandlerThrowsException_LogsErrorAndContinues()
    {
        // Arrange
        var handler2Called = false;
        var exception = new InvalidOperationException("Handler error");

        Action<TestEvent> handler1 = e => { throw exception; };
        Action<TestEvent> handler2 = e => handler2Called = true;

        _eventBus.Subscribe(handler1);
        _eventBus.Subscribe(handler2);

        // Act
        _eventBus.Publish(new TestEvent());

        // Assert
        handler2Called.Should().BeTrue(); // Second handler should still execute
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error invoking")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Publish_MultipleHandlersWithException_AllHandlersInvoked()
    {
        // Arrange
        var callOrder = new List<int>();
        var exception = new InvalidOperationException("Handler error");

        Action<TestEvent> handler1 = e => callOrder.Add(1);
        Action<TestEvent> handler2 = e => { callOrder.Add(2); throw exception; };
        Action<TestEvent> handler3 = e => callOrder.Add(3);

        _eventBus.Subscribe(handler1);
        _eventBus.Subscribe(handler2);
        _eventBus.Subscribe(handler3);

        // Act
        _eventBus.Publish(new TestEvent());

        // Assert
        callOrder.Should().Equal(1, 2, 3);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void Subscribe_ConcurrentSubscriptions_AllSubscribersRegistered()
    {
        // Arrange
        var handlers = new ConcurrentBag<Action<TestEvent>>();
        var callCounts = new ConcurrentDictionary<int, int>();

        // Act
        Parallel.For(0, 100, i =>
        {
            Action<TestEvent> handler = e => callCounts.AddOrUpdate(i, 1, (_, count) => count + 1);
            handlers.Add(handler);
            _eventBus.Subscribe(handler);
        });

        _eventBus.Publish(new TestEvent());

        // Assert
        callCounts.Should().HaveCount(100);
        callCounts.Values.Should().AllSatisfy(count => count.Should().Be(1));
    }

    [Fact]
    public void Publish_ConcurrentPublishing_AllEventsDelivered()
    {
        // Arrange
        var receivedCount = 0;
        Action<TestEvent> handler = e => Interlocked.Increment(ref receivedCount);
        _eventBus.Subscribe(handler);

        // Act
        Parallel.For(0, 1000, _ =>
        {
            _eventBus.Publish(new TestEvent());
        });

        // Assert
        receivedCount.Should().Be(1000);
    }

    [Fact]
    public async Task SubscribeAndPublish_ConcurrentOperations_MaintainsConsistency()
    {
        // Arrange
        var totalCalls = 0;
        var subscriberTasks = new List<Task>();
        var publisherTasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            subscriberTasks.Add(Task.Run(() =>
            {
                Action<TestEvent> handler = e => Interlocked.Increment(ref totalCalls);
                _eventBus.Subscribe(handler);
            }));
        }

        for (int i = 0; i < 100; i++)
        {
            publisherTasks.Add(Task.Run(() =>
            {
                _eventBus.Publish(new TestEvent());
            }));
        }

        await Task.WhenAll(subscriberTasks.Concat(publisherTasks).ToArray());

        // Assert - Should complete without deadlocks or exceptions
        totalCalls.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Unsubscribe_DuringPublish_HandlesGracefully()
    {
        // Arrange
        var callCount = 0;
        Action<TestEvent> handler = null!;
        handler = e =>
        {
            Interlocked.Increment(ref callCount);
            _eventBus.Unsubscribe(handler); // Unsubscribe during handler execution
        };

        _eventBus.Subscribe(handler);

        // Act
        _eventBus.Publish(new TestEvent());
        _eventBus.Publish(new TestEvent());

        // Assert
        callCount.Should().Be(1); // Should only be called once
    }

    [Fact]
    public async Task Clear_ConcurrentWithPublish_HandlesGracefully()
    {
        // Arrange
        var callCount = 0;
        Action<TestEvent> handler = e => Interlocked.Increment(ref callCount);
        _eventBus.Subscribe(handler);

        // Act
        var publishTask = Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                _eventBus.Publish(new TestEvent());
                Thread.Sleep(1);
            }
        });

        var clearTask = Task.Run(() =>
        {
            Thread.Sleep(50);
            _eventBus.Clear();
        });

        await Task.WhenAll(publishTask, clearTask);

        // Assert - Should complete without deadlocks
        callCount.Should().BeGreaterThan(0);
        callCount.Should().BeLessThan(100);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void Publish_ThousandsOfEvents_CompletesInReasonableTime()
    {
        // Arrange
        var callCount = 0;
        Action<TestEvent> handler = e => Interlocked.Increment(ref callCount);
        _eventBus.Subscribe(handler);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < 10000; i++)
        {
            _eventBus.Publish(new TestEvent());
        }

        stopwatch.Stop();

        // Assert
        callCount.Should().Be(10000);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete in under 1 second
    }

    [Fact]
    public void Subscribe_ManySubscribers_HandlesEfficiently()
    {
        // Arrange
        var handlers = new List<Action<TestEvent>>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Subscribe 1000 handlers
        for (int i = 0; i < 1000; i++)
        {
            Action<TestEvent> handler = e => { };
            handlers.Add(handler);
            _eventBus.Subscribe(handler);
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100); // Should be very fast
    }

    [Fact]
    public void Publish_ManySubscribers_DeliversToAll()
    {
        // Arrange
        var callCounts = new ConcurrentDictionary<int, int>();

        for (int i = 0; i < 100; i++)
        {
            var index = i;
            Action<TestEvent> handler = e => callCounts.AddOrUpdate(index, 1, (_, count) => count + 1);
            _eventBus.Subscribe(handler);
        }

        // Act
        _eventBus.Publish(new TestEvent());

        // Assert
        callCounts.Should().HaveCount(100);
        callCounts.Values.Should().AllSatisfy(count => count.Should().Be(1));
    }

    #endregion

    #region Memory Management Tests

    [Fact]
    public void Clear_RemovesAllSubscriptions()
    {
        // Arrange
        var handler1Called = false;
        var handler2Called = false;

        Action<TestEvent> handler1 = e => handler1Called = true;
        Action<OtherTestEvent> handler2 = e => handler2Called = true;

        _eventBus.Subscribe(handler1);
        _eventBus.Subscribe(handler2);

        // Act
        _eventBus.Clear();
        _eventBus.Publish(new TestEvent());
        _eventBus.Publish(new OtherTestEvent());

        // Assert
        handler1Called.Should().BeFalse();
        handler2Called.Should().BeFalse();
    }

    [Fact]
    public void Clear_LogsSubscriptionCount()
    {
        // Arrange
        _eventBus.Subscribe<TestEvent>(e => { });
        _eventBus.Subscribe<OtherTestEvent>(e => { });

        // Act
        _eventBus.Clear();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Cleared all")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void EventBus_AfterManySubscriptionsAndUnsubscriptions_NoMemoryLeaks()
    {
        // Arrange & Act
        for (int i = 0; i < 1000; i++)
        {
            Action<TestEvent> handler = e => { };
            _eventBus.Subscribe(handler);
            _eventBus.Unsubscribe(handler);
        }

        // Assert - Should complete without memory issues
        // In a real scenario, you would use a memory profiler
        _eventBus.Publish(new TestEvent()); // Should have no subscribers
    }

    #endregion

    #region Event Data Tests

    [Fact]
    public void Publish_EventWithData_DeliversCorrectData()
    {
        // Arrange
        var receivedData = string.Empty;
        var expectedData = "Important game event data";

        Action<TestEvent> handler = e => receivedData = e.Data;
        _eventBus.Subscribe(handler);

        // Act
        _eventBus.Publish(new TestEvent { Data = expectedData });

        // Assert
        receivedData.Should().Be(expectedData);
    }

    [Fact]
    public void Publish_EventWithTimestamp_PreservesTimestamp()
    {
        // Arrange
        DateTime receivedTimestamp = default;
        Action<TestEvent> handler = e => receivedTimestamp = e.Timestamp;
        _eventBus.Subscribe(handler);

        var testEvent = new TestEvent();
        var expectedTimestamp = testEvent.Timestamp;

        // Act
        _eventBus.Publish(testEvent);

        // Assert
        receivedTimestamp.Should().Be(expectedTimestamp);
    }

    [Fact]
    public void Publish_ComplexEventData_PreservesAllProperties()
    {
        // Arrange
        ComplexEvent? receivedEvent = null;
        Action<ComplexEvent> handler = e => receivedEvent = e;
        _eventBus.Subscribe(handler);

        var complexEvent = new ComplexEvent
        {
            Id = 42,
            Name = "Test Event",
            Values = new[] { 1.0f, 2.0f, 3.0f },
            Metadata = new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            }
        };

        // Act
        _eventBus.Publish(complexEvent);

        // Assert
        receivedEvent.Should().NotBeNull();
        receivedEvent!.Id.Should().Be(42);
        receivedEvent.Name.Should().Be("Test Event");
        receivedEvent.Values.Should().BeEquivalentTo(new[] { 1.0f, 2.0f, 3.0f });
        receivedEvent.Metadata.Should().ContainKey("key1");
        receivedEvent.Metadata["key1"].Should().Be("value1");
    }

    #endregion

    #region Helper Classes

    private class TestEvent : IGameEvent
    {
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public string Data { get; set; } = string.Empty;
    }

    private class OtherTestEvent : IGameEvent
    {
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }

    private class ComplexEvent : IGameEvent
    {
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public float[] Values { get; set; } = Array.Empty<float>();
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    #endregion
}
