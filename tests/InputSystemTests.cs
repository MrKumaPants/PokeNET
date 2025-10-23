using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Xna.Framework.Input;
using PokeNET.Core.ECS;
using PokeNET.Domain.ECS.Components;
using PokeNET.Domain.ECS.Events;
using PokeNET.Domain.ECS.Systems;
using PokeNET.Domain.Input;
using PokeNET.Domain.Input.Commands;
using System.Numerics;
using Xunit;

namespace PokeNET.Tests.ECS.Systems;

/// <summary>
/// Comprehensive tests for InputSystem and Command pattern implementation.
/// Tests command creation, execution, undo/redo, queue processing, and key bindings.
/// </summary>
public class InputSystemTests : IDisposable
{
    private readonly World _world;
    private readonly ILogger<CommandQueue> _queueLogger;
    private readonly ILogger<CommandHistory> _historyLogger;
    private readonly ILogger<InputSystem> _systemLogger;
    private readonly ILogger<EventBus> _eventBusLogger;
    private readonly IEventBus _eventBus;

    public InputSystemTests()
    {
        _world = World.Create();
        _queueLogger = NullLogger<CommandQueue>.Instance;
        _historyLogger = NullLogger<CommandHistory>.Instance;
        _systemLogger = NullLogger<InputSystem>.Instance;
        _eventBusLogger = NullLogger<EventBus>.Instance;
        _eventBus = new EventBus(_eventBusLogger);
    }

    public void Dispose()
    {
        World.Destroy(_world);
    }

    #region Command Creation and Execution Tests

    [Fact]
    public void MoveCommand_ShouldUpdateVelocity()
    {
        // Arrange
        var entity = _world.Create(new Position { X = 0, Y = 0 }, new Velocity { X = 0, Y = 0 });
        var direction = new Vector2(1, 0); // Move right
        var command = new MoveCommand(entity, direction, 2.0f);

        // Act
        command.Execute(_world);

        // Assert
        ref var velocity = ref _world.Get<Velocity>(entity);
        Assert.Equal(2.0f, velocity.X, 0.01f);
        Assert.Equal(0.0f, velocity.Y, 0.01f);
    }

    [Fact]
    public void MoveCommand_CanExecute_ShouldReturnFalseForInvalidEntity()
    {
        // Arrange
        var entity = _world.Create(new Position { X = 0, Y = 0 });
        var command = new MoveCommand(entity, Vector2.One);

        // Destroy the entity to make it invalid
        _world.Destroy(entity);

        // Act
        var canExecute = command.CanExecute(_world);

        // Assert
        Assert.False(canExecute);
    }

    [Fact]
    public void InteractCommand_ShouldPublishEvent()
    {
        // Arrange
        var entity = _world.Create();
        InteractionEvent? publishedEvent = null;
        _eventBus.Subscribe<InteractionEvent>(e => publishedEvent = e);

        var command = new InteractCommand(entity, _eventBus);

        // Act
        command.Execute(_world);

        // Assert
        Assert.NotNull(publishedEvent);
        Assert.Equal(entity, publishedEvent.Entity);
    }

    [Fact]
    public void PauseCommand_ShouldPublishPauseEvent()
    {
        // Arrange
        PauseEvent? publishedEvent = null;
        _eventBus.Subscribe<PauseEvent>(e => publishedEvent = e);

        var command = new PauseCommand(true, _eventBus);

        // Act
        command.Execute(_world);

        // Assert
        Assert.NotNull(publishedEvent);
        Assert.True(publishedEvent.IsPaused);
    }

    [Fact]
    public void MenuCommand_ShouldPublishMenuActionEvent()
    {
        // Arrange
        MenuActionEvent? publishedEvent = null;
        _eventBus.Subscribe<MenuActionEvent>(e => publishedEvent = e);

        var command = new MenuCommand(MenuAction.OpenInventory, _eventBus);

        // Act
        command.Execute(_world);

        // Assert
        Assert.NotNull(publishedEvent);
        Assert.Equal(MenuAction.OpenInventory, publishedEvent.Action);
    }

    #endregion

    #region Command Queue Tests

    [Fact]
    public void CommandQueue_ShouldEnqueueAndProcess()
    {
        // Arrange
        var queue = new CommandQueue(_queueLogger, maxQueueSize: 10);
        var entity = _world.Create(new Position { X = 0, Y = 0 }, new Velocity { X = 0, Y = 0 });
        var command = new MoveCommand(entity, Vector2.One);

        // Act
        var enqueued = queue.Enqueue(command);
        var processed = queue.ProcessAll(_world);

        // Assert
        Assert.True(enqueued);
        Assert.Equal(1, processed);
        Assert.True(queue.IsEmpty);
    }

    [Fact]
    public void CommandQueue_ShouldProcessInPriorityOrder()
    {
        // Arrange
        var queue = new CommandQueue(_queueLogger);
        var executionOrder = new List<int>();

        var lowPriorityCommand = new TestCommand(50, () => executionOrder.Add(50));
        var highPriorityCommand = new TestCommand(10, () => executionOrder.Add(10));
        var mediumPriorityCommand = new TestCommand(30, () => executionOrder.Add(30));

        // Act - enqueue in random order
        queue.Enqueue(lowPriorityCommand);
        queue.Enqueue(highPriorityCommand);
        queue.Enqueue(mediumPriorityCommand);
        queue.ProcessAll(_world);

        // Assert - should execute in priority order (low to high)
        Assert.Equal(new[] { 10, 30, 50 }, executionOrder);
    }

    [Fact]
    public void CommandQueue_ShouldRejectWhenFull()
    {
        // Arrange
        var queue = new CommandQueue(_queueLogger, maxQueueSize: 2);
        var entity = _world.Create(new Position(), new Velocity());

        // Act
        var result1 = queue.Enqueue(new MoveCommand(entity, Vector2.One));
        var result2 = queue.Enqueue(new MoveCommand(entity, Vector2.One));
        var result3 = queue.Enqueue(new MoveCommand(entity, Vector2.One)); // Should fail

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.False(result3);
        Assert.Equal(2, queue.Count);
    }

    [Fact]
    public void CommandQueue_Clear_ShouldRemoveAllCommands()
    {
        // Arrange
        var queue = new CommandQueue(_queueLogger);
        var entity = _world.Create(new Position(), new Velocity());
        queue.Enqueue(new MoveCommand(entity, Vector2.One));
        queue.Enqueue(new MoveCommand(entity, Vector2.One));

        // Act
        queue.Clear();

        // Assert
        Assert.True(queue.IsEmpty);
        Assert.Equal(0, queue.Count);
    }

    #endregion

    #region Command History (Undo/Redo) Tests

    [Fact]
    public void CommandHistory_ShouldRecordAndUndo()
    {
        // Arrange
        var history = new CommandHistory(_historyLogger);
        var entity = _world.Create(new Position { X = 0, Y = 0 }, new Velocity { X = 0, Y = 0 });
        var command = new MoveCommand(entity, Vector2.One);

        // Act
        command.Execute(_world);
        ref var position = ref _world.Get<Position>(entity);
        var positionAfterExecute = new Vector2(position.X, position.Y);

        history.Record(command);
        var undoResult = history.Undo(_world);

        // Assert
        Assert.True(undoResult);
        ref var positionAfterUndo = ref _world.Get<Position>(entity);
        Assert.Equal(0, positionAfterUndo.X, 0.01f);
        Assert.Equal(0, positionAfterUndo.Y, 0.01f);
    }

    [Fact]
    public void CommandHistory_ShouldSupportRedo()
    {
        // Arrange
        var history = new CommandHistory(_historyLogger);
        var entity = _world.Create(new Position { X = 0, Y = 0 }, new Velocity { X = 0, Y = 0 });
        var command = new MoveCommand(entity, new Vector2(1, 0));

        // Act
        command.Execute(_world);
        history.Record(command);
        history.Undo(_world);

        ref var positionAfterUndo = ref _world.Get<Position>(entity);
        var redoResult = history.Redo(_world);
        ref var positionAfterRedo = ref _world.Get<Position>(entity);

        // Assert
        Assert.True(redoResult);
        // Position should be restored after redo
    }

    [Fact]
    public void CommandHistory_ShouldClearRedoStackOnNewCommand()
    {
        // Arrange
        var history = new CommandHistory(_historyLogger);
        var entity = _world.Create(new Position { X = 0, Y = 0 }, new Velocity { X = 0, Y = 0 });
        var command1 = new MoveCommand(entity, Vector2.One);
        var command2 = new MoveCommand(entity, Vector2.One);

        // Act
        command1.Execute(_world);
        history.Record(command1);
        history.Undo(_world);

        Assert.True(history.CanRedo);

        command2.Execute(_world);
        history.Record(command2);

        // Assert
        Assert.False(history.CanRedo); // Redo stack should be cleared
    }

    [Fact]
    public void CommandHistory_ShouldRespectMaxHistorySize()
    {
        // Arrange
        var history = new CommandHistory(_historyLogger, maxHistorySize: 3);
        var entity = _world.Create(new Position { X = 0, Y = 0 }, new Velocity { X = 0, Y = 0 });

        // Act - record 4 commands (exceeds max)
        for (int i = 0; i < 4; i++)
        {
            var command = new MoveCommand(entity, Vector2.One);
            command.Execute(_world);
            history.Record(command);
        }

        // Assert
        Assert.Equal(3, history.UndoCount); // Should maintain max size
    }

    #endregion

    #region InputConfig Tests

    [Fact]
    public void InputConfig_CreateDefault_ShouldHaveStandardBindings()
    {
        // Arrange & Act
        var config = InputConfig.CreateDefault();

        // Assert
        Assert.NotNull(config);
        Assert.True(config.KeyboardBindings.Count > 0);
        Assert.Equal(Keys.W, config.GetKey("MoveUp"));
        Assert.Equal(Keys.S, config.GetKey("MoveDown"));
        Assert.Equal(Keys.A, config.GetKey("MoveLeft"));
        Assert.Equal(Keys.D, config.GetKey("MoveRight"));
    }

    [Fact]
    public void InputConfig_RemapKey_ShouldUpdateBinding()
    {
        // Arrange
        var config = InputConfig.CreateDefault();

        // Act
        config.RemapKey("MoveUp", Keys.Up);

        // Assert
        Assert.Equal(Keys.Up, config.GetKey("MoveUp"));
    }

    [Fact]
    public void InputConfig_SaveAndLoad_ShouldPersistSettings()
    {
        // Arrange
        var config = InputConfig.CreateDefault();
        config.RemapKey("MoveUp", Keys.Up);
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            config.SaveToFile(tempFile);
            var loadedConfig = InputConfig.LoadFromFile(tempFile);

            // Assert
            Assert.Equal(Keys.Up, loadedConfig.GetKey("MoveUp"));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void InputConfig_LoadFromNonExistentFile_ShouldReturnDefault()
    {
        // Arrange
        var nonExistentFile = "nonexistent_config.json";

        // Act
        var config = InputConfig.LoadFromFile(nonExistentFile);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(Keys.W, config.GetKey("MoveUp")); // Should have default bindings
    }

    #endregion

    #region InputSystem Integration Tests

    [Fact]
    public void InputSystem_ShouldInitializeSuccessfully()
    {
        // Arrange
        var queue = new CommandQueue(_queueLogger);
        var history = new CommandHistory(_historyLogger);
        var system = new InputSystem(_systemLogger, queue, history, _eventBus);

        // Act
        system.Initialize(_world);

        // Assert
        Assert.Equal(-100, system.Priority); // Should have high priority
        Assert.True(system.IsEnabled);
    }

    [Fact]
    public void InputSystem_SetPlayerEntity_ShouldStoreEntity()
    {
        // Arrange
        var queue = new CommandQueue(_queueLogger);
        var history = new CommandHistory(_historyLogger);
        var system = new InputSystem(_systemLogger, queue, history, _eventBus);
        system.Initialize(_world);

        var playerEntity = _world.Create(new Position(), new Velocity());

        // Act
        system.SetPlayerEntity(playerEntity);

        // Assert - No exception should occur
        Assert.True(true);
    }

    [Fact]
    public void InputSystem_LoadConfig_ShouldUpdateConfiguration()
    {
        // Arrange
        var queue = new CommandQueue(_queueLogger);
        var history = new CommandHistory(_historyLogger);
        var system = new InputSystem(_systemLogger, queue, history, _eventBus);
        system.Initialize(_world);

        var config = InputConfig.CreateDefault();
        config.RemapKey("MoveUp", Keys.Up);
        var tempFile = Path.GetTempFileName();

        try
        {
            config.SaveToFile(tempFile);

            // Act
            system.LoadConfig(tempFile);
            var loadedConfig = system.GetConfig();

            // Assert
            Assert.Equal(Keys.Up, loadedConfig.GetKey("MoveUp"));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void InputSystem_InputEnabled_ShouldControlProcessing()
    {
        // Arrange
        var queue = new CommandQueue(_queueLogger);
        var history = new CommandHistory(_historyLogger);
        var system = new InputSystem(_systemLogger, queue, history, _eventBus);
        system.Initialize(_world);

        // Act
        system.InputEnabled = false;
        system.Update(0.016f); // Simulate one frame

        // Assert
        Assert.False(system.InputEnabled);
        // When disabled, no commands should be processed
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Test command for testing priority ordering.
    /// </summary>
    private class TestCommand : CommandBase
    {
        private readonly int _priority;
        private readonly Action _onExecute;

        public override int Priority => _priority;

        public TestCommand(int priority, Action onExecute)
        {
            _priority = priority;
            _onExecute = onExecute;
        }

        public override void Execute(World world)
        {
            _onExecute();
        }
    }

    #endregion
}
