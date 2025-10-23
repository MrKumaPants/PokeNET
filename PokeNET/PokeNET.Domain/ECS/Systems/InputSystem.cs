using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Input;
using PokeNET.Domain.ECS.Events;
using PokeNET.Domain.Input;
using PokeNET.Domain.Input.Commands;
using System.Numerics;

namespace PokeNET.Domain.ECS.Systems;

/// <summary>
/// System for handling player input and converting it to commands.
/// Implements the Command pattern for flexible input handling.
/// Supports keyboard, mouse, and gamepad input with rebindable controls.
/// Priority: -100 (executes before other systems).
/// </summary>
public class InputSystem : SystemBase
{
    private readonly CommandQueue _commandQueue;
    private readonly CommandHistory _commandHistory;
    private readonly IEventBus _eventBus;
    private InputConfig _config;

    // Input state tracking
    private KeyboardState _previousKeyboardState;
    private KeyboardState _currentKeyboardState;
    private GamePadState _previousGamePadState;
    private GamePadState _currentGamePadState;
    private MouseState _previousMouseState;
    private MouseState _currentMouseState;

    // Player entity tracking
    private Entity? _playerEntity;

    /// <inheritdoc/>
    public override int Priority => -100; // Execute first

    /// <summary>
    /// Gets whether input is currently enabled.
    /// </summary>
    public bool InputEnabled { get; set; } = true;

    /// <summary>
    /// Gets the command queue.
    /// </summary>
    public CommandQueue CommandQueue => _commandQueue;

    /// <summary>
    /// Gets the command history.
    /// </summary>
    public CommandHistory CommandHistory => _commandHistory;

    /// <summary>
    /// Initializes a new input system.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="commandQueue">Command queue for processing inputs.</param>
    /// <param name="commandHistory">Command history for undo/redo.</param>
    /// <param name="eventBus">Event bus for publishing input events.</param>
    /// <param name="config">Input configuration.</param>
    public InputSystem(
        ILogger<InputSystem> logger,
        CommandQueue commandQueue,
        CommandHistory commandHistory,
        IEventBus eventBus,
        InputConfig? config = null)
        : base(logger)
    {
        _commandQueue = commandQueue;
        _commandHistory = commandHistory;
        _eventBus = eventBus;
        _config = config ?? InputConfig.CreateDefault();
    }

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        Logger.LogInformation("InputSystem initialized with {KeyBindings} key bindings",
            _config.KeyboardBindings.Count);

        // Initialize input states
        _currentKeyboardState = Keyboard.GetState();
        _currentGamePadState = GamePad.GetState(0);
        _currentMouseState = Mouse.GetState();
    }

    /// <inheritdoc/>
    protected override void OnUpdate(float deltaTime)
    {
        if (!InputEnabled)
            return;

        // Update input states
        _previousKeyboardState = _currentKeyboardState;
        _previousGamePadState = _currentGamePadState;
        _previousMouseState = _currentMouseState;

        _currentKeyboardState = Keyboard.GetState();
        _currentGamePadState = GamePad.GetState(0);
        _currentMouseState = Mouse.GetState();

        // Process inputs and generate commands
        ProcessKeyboardInput();
        ProcessGamePadInput();
        ProcessMouseInput();

        // Execute all queued commands
        int executedCount = _commandQueue.ProcessAll(World);

        if (executedCount > 0)
        {
            Logger.LogTrace("Executed {Count} commands this frame", executedCount);
        }
    }

    /// <summary>
    /// Processes keyboard input and generates commands.
    /// </summary>
    private void ProcessKeyboardInput()
    {
        // Movement input (WASD or configured keys)
        var moveDirection = Vector2.Zero;

        if (IsKeyPressed(_config.GetKey("MoveUp")))
            moveDirection.Y -= 1;
        if (IsKeyPressed(_config.GetKey("MoveDown")))
            moveDirection.Y += 1;
        if (IsKeyPressed(_config.GetKey("MoveLeft")))
            moveDirection.X -= 1;
        if (IsKeyPressed(_config.GetKey("MoveRight")))
            moveDirection.X += 1;

        if (moveDirection != Vector2.Zero && _playerEntity.HasValue)
        {
            var command = new MoveCommand(_playerEntity.Value, moveDirection);
            EnqueueCommand(command);
        }

        // Action inputs (check for key press, not hold)
        if (WasKeyJustPressed(_config.GetKey("Interact")) && _playerEntity.HasValue)
        {
            var command = new InteractCommand(_playerEntity.Value, _eventBus);
            EnqueueCommand(command);
        }

        if (WasKeyJustPressed(_config.GetKey("Pause")))
        {
            var command = new PauseCommand(true, _eventBus);
            EnqueueCommand(command);
        }

        if (WasKeyJustPressed(_config.GetKey("Inventory")))
        {
            var command = new MenuCommand(MenuAction.OpenInventory, _eventBus);
            EnqueueCommand(command);
        }

        if (WasKeyJustPressed(_config.GetKey("Map")))
        {
            var command = new MenuCommand(MenuAction.OpenMap, _eventBus);
            EnqueueCommand(command);
        }

        // Undo/Redo support (for debugging/replays)
        if (WasKeyJustPressed(_config.GetKey("Undo")))
        {
            _commandHistory.Undo(World);
        }

        if (WasKeyJustPressed(_config.GetKey("Redo")))
        {
            _commandHistory.Redo(World);
        }
    }

    /// <summary>
    /// Processes gamepad input and generates commands.
    /// </summary>
    private void ProcessGamePadInput()
    {
        if (!_currentGamePadState.IsConnected)
            return;

        // Thumbstick movement
        var thumbstick = _currentGamePadState.ThumbSticks.Left;
        if (Math.Abs(thumbstick.X) > _config.DeadZone || Math.Abs(thumbstick.Y) > _config.DeadZone)
        {
            var moveDirection = new Vector2(thumbstick.X, -thumbstick.Y); // Invert Y for screen coordinates
            if (_playerEntity.HasValue)
            {
                var command = new MoveCommand(_playerEntity.Value, moveDirection);
                EnqueueCommand(command);
            }
        }

        // Button inputs
        if (WasButtonJustPressed(_config.GetButton("Interact")) && _playerEntity.HasValue)
        {
            var command = new InteractCommand(_playerEntity.Value, _eventBus);
            EnqueueCommand(command);
        }

        if (WasButtonJustPressed(_config.GetButton("Pause")))
        {
            var command = new PauseCommand(true, _eventBus);
            EnqueueCommand(command);
        }
    }

    /// <summary>
    /// Processes mouse input (for point-and-click interfaces, etc.).
    /// </summary>
    private void ProcessMouseInput()
    {
        // TODO: Implement mouse-specific input handling
        // - Click to move
        // - Context menus
        // - Drag and drop
    }

    /// <summary>
    /// Enqueues a command and records it in history if it supports undo.
    /// </summary>
    private void EnqueueCommand(ICommand command)
    {
        if (_commandQueue.Enqueue(command))
        {
            PublishInputEvent(command);
        }
    }

    /// <summary>
    /// Publishes an input event for the given command.
    /// </summary>
    private void PublishInputEvent(ICommand command)
    {
        _eventBus.Publish(new InputCommandEvent
        {
            Command = command,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Checks if a key is currently pressed.
    /// </summary>
    private bool IsKeyPressed(Keys? key)
    {
        return key.HasValue && _currentKeyboardState.IsKeyDown(key.Value);
    }

    /// <summary>
    /// Checks if a key was just pressed this frame.
    /// </summary>
    private bool WasKeyJustPressed(Keys? key)
    {
        return key.HasValue &&
               _currentKeyboardState.IsKeyDown(key.Value) &&
               _previousKeyboardState.IsKeyUp(key.Value);
    }

    /// <summary>
    /// Checks if a button was just pressed this frame.
    /// </summary>
    private bool WasButtonJustPressed(Buttons? button)
    {
        return button.HasValue &&
               _currentGamePadState.IsButtonDown(button.Value) &&
               _previousGamePadState.IsButtonUp(button.Value);
    }

    /// <summary>
    /// Sets the player entity for input processing.
    /// </summary>
    public void SetPlayerEntity(Entity entity)
    {
        _playerEntity = entity;
        Logger.LogInformation("Player entity set for InputSystem");
    }

    /// <summary>
    /// Loads input configuration from a file.
    /// </summary>
    public void LoadConfig(string filePath)
    {
        _config = InputConfig.LoadFromFile(filePath);
        Logger.LogInformation("Loaded input configuration from {FilePath}", filePath);
    }

    /// <summary>
    /// Saves input configuration to a file.
    /// </summary>
    public void SaveConfig(string filePath)
    {
        _config.SaveToFile(filePath);
        Logger.LogInformation("Saved input configuration to {FilePath}", filePath);
    }

    /// <summary>
    /// Gets the current input configuration.
    /// </summary>
    public InputConfig GetConfig() => _config;

    /// <summary>
    /// Updates the input configuration.
    /// </summary>
    public void SetConfig(InputConfig config)
    {
        _config = config;
        Logger.LogInformation("Input configuration updated");
    }

    /// <inheritdoc/>
    protected override void OnDispose()
    {
        _commandQueue.Clear();
        _commandHistory.Clear();
        Logger.LogInformation("InputSystem disposed");
    }
}
