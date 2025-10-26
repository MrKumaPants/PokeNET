# InputSystem Architecture - Command Pattern Implementation

## Overview

The InputSystem is a production-ready ECS system that implements the Command pattern for flexible, maintainable, and testable input handling. It provides keyboard, mouse, and gamepad support with rebindable controls, input buffering, and undo/redo functionality.

## Architecture Components

### 1. Command Pattern Infrastructure

#### ICommand Interface (`/PokeNET.Domain/Input/ICommand.cs`)
- **Purpose**: Base contract for all commands
- **Features**:
  - Priority-based execution
  - Timestamp tracking
  - CanExecute guard clause
  - Execute/Undo operations
  - Undo support flag

#### CommandBase (`/PokeNET.Domain/Input/CommandBase.cs`)
- **Purpose**: Abstract base class with common functionality
- **Features**:
  - Default implementations
  - Template Method pattern
  - Automatic timestamp generation

### 2. Command Management

#### CommandQueue (`/PokeNET.Domain/Input/CommandQueue.cs`)
- **Purpose**: Thread-safe command queuing and execution
- **Features**:
  - Priority-based sorting
  - Configurable max queue size (prevents buffer overflow)
  - Batch processing
  - Thread-safe operations using ConcurrentQueue
  - Logging support

#### CommandHistory (`/PokeNET.Domain/Input/CommandHistory.cs`)
- **Purpose**: Undo/redo support via Memento pattern
- **Features**:
  - Undo/redo stacks
  - Configurable history size
  - Automatic redo stack clearing on new commands
  - Oldest command removal when at capacity

### 3. Configuration

#### InputConfig (`/PokeNET.Domain/Input/InputConfig.cs`)
- **Purpose**: Configurable input bindings
- **Features**:
  - Keyboard and gamepad bindings
  - Sensitivity and dead zone settings
  - JSON serialization/deserialization
  - Runtime remapping
  - Default configuration factory
  - Persistent storage

### 4. Concrete Commands

#### MoveCommand (`/PokeNET.Domain/Input/Commands/MoveCommand.cs`)
- **Priority**: 10 (high)
- **Supports Undo**: Yes
- **Features**:
  - Normalized direction vectors
  - Configurable speed
  - Position storage for undo
  - Component validation (Position, Velocity required)

#### InteractCommand (`/PokeNET.Domain/Input/Commands/InteractCommand.cs`)
- **Priority**: 20 (medium-high)
- **Supports Undo**: No
- **Features**:
  - Event bus integration
  - Publishes InteractionEvent
  - Extensible for dialogue, item pickup, etc.

#### MenuCommand (`/PokeNET.Domain/Input/Commands/MenuCommand.cs`)
- **Priority**: 30 (medium)
- **Supports Undo**: No
- **Features**:
  - MenuAction enum (Inventory, Map, Settings, etc.)
  - Event bus integration
  - Publishes MenuActionEvent

#### PauseCommand (`/PokeNET.Domain/Input/Commands/PauseCommand.cs`)
- **Priority**: 0 (highest - executes first)
- **Supports Undo**: No
- **Features**:
  - Pause/unpause toggle
  - Event bus integration
  - Publishes PauseEvent

### 5. InputSystem

#### InputSystem (`/PokeNET.Domain/ECS/Systems/InputSystem.cs`)
- **Priority**: -100 (executes before all other systems)
- **Features**:
  - Keyboard, gamepad, and mouse support
  - Input state tracking (current/previous frames)
  - Command generation from input
  - Player entity tracking
  - Input enable/disable toggle
  - Configuration loading/saving
  - Event bus integration
  - Dead zone support for analog input
  - Key press vs hold detection

### 6. Events

#### InputEvents (`/PokeNET.Domain/ECS/Events/InputEvents.cs`)
- **InputCommandEvent**: Published when a command is created
- **InputStateChangedEvent**: Published when input is enabled/disabled
- **KeyBindingChangedEvent**: Published when key bindings change
- **InteractionEvent**: Published when interaction occurs
- **MenuActionEvent**: Published when menu actions occur
- **PauseEvent**: Published when game is paused/unpaused

## Usage Examples

### Basic Setup

```csharp
// Create dependencies
var logger = loggerFactory.CreateLogger<InputSystem>();
var queueLogger = loggerFactory.CreateLogger<CommandQueue>();
var historyLogger = loggerFactory.CreateLogger<CommandHistory>();

var commandQueue = new CommandQueue(queueLogger, maxQueueSize: 100);
var commandHistory = new CommandHistory(historyLogger, maxHistorySize: 50);
var eventBus = new EventBus(eventBusLogger);

// Load or create configuration
var config = InputConfig.LoadFromFile("config/InputConfig.json");

// Create system
var inputSystem = new InputSystem(
    logger,
    commandQueue,
    commandHistory,
    eventBus,
    config
);

// Register with system manager
systemManager.RegisterSystem(inputSystem);

// Set player entity
var player = world.Create(new Position(), new Velocity());
inputSystem.SetPlayerEntity(player);
```

### Custom Command

```csharp
public class AttackCommand : CommandBase
{
    private readonly Entity _attacker;
    private readonly Vector2 _direction;

    public override int Priority => 15;

    public AttackCommand(Entity attacker, Vector2 direction)
    {
        _attacker = attacker;
        _direction = direction;
    }

    public override bool CanExecute(World world)
    {
        return base.CanExecute(world) &&
               world.IsAlive(_attacker) &&
               world.Has<Stats>(_attacker);
    }

    public override void Execute(World world)
    {
        // Implement attack logic
    }
}
```

### Runtime Key Remapping

```csharp
// Get current config
var config = inputSystem.GetConfig();

// Remap key
config.RemapKey("MoveUp", Keys.Up);

// Save changes
inputSystem.SaveConfig("config/InputConfig.json");
```

### Undo/Redo

```csharp
// Undo last action
if (inputSystem.CommandHistory.CanUndo)
{
    inputSystem.CommandHistory.Undo(world);
}

// Redo last undone action
if (inputSystem.CommandHistory.CanRedo)
{
    inputSystem.CommandHistory.Redo(world);
}
```

### Event Handling

```csharp
// Subscribe to input events
eventBus.Subscribe<InteractionEvent>(e =>
{
    Console.WriteLine($"Entity {e.Entity} interacted at {e.Timestamp}");
});

eventBus.Subscribe<PauseEvent>(e =>
{
    gameState.IsPaused = e.IsPaused;
});
```

## Design Patterns Used

1. **Command Pattern**: Encapsulates requests as objects
2. **Template Method**: CommandBase provides common structure
3. **Observer Pattern**: Event bus for loose coupling
4. **Memento Pattern**: Command history for undo/redo
5. **Strategy Pattern**: Different input sources (keyboard, gamepad, mouse)
6. **Factory Pattern**: InputConfig.CreateDefault()

## Benefits

### 1. Maintainability
- Clear separation of concerns
- Single Responsibility Principle
- Easy to add new commands

### 2. Testability
- Commands are independently testable
- Mock-friendly dependencies
- Comprehensive test coverage

### 3. Flexibility
- Runtime key remapping
- Priority-based execution
- Input buffering
- Undo/redo support

### 4. Network-Ready
- Commands can be serialized
- Replay functionality built-in
- Deterministic execution

### 5. Performance
- Thread-safe command queue
- Efficient priority sorting
- Configurable buffer sizes
- No GC pressure from pooling potential

## Integration Points

### With ECS
- Extends SystemBase
- Uses World for entity queries
- Operates on Position and Velocity components
- Priority: -100 (executes first)

### With EventBus
- Publishes input events
- Subscribes to game state changes
- Loose coupling via events

### With Localization
- Key binding names can be localized
- Button prompts for UI

### With Save System
- Configuration persistence
- Key binding profiles
- Input replay data

## Testing

Comprehensive test suite in `/tests/InputSystemTests.cs`:

- ✅ Command creation and execution
- ✅ Command queue operations
- ✅ Priority-based execution
- ✅ Undo/redo functionality
- ✅ Input configuration
- ✅ Key remapping
- ✅ Event publishing
- ✅ System initialization
- ✅ Player entity tracking
- ✅ Config persistence

## Performance Characteristics

- **Command Queue**: O(n log n) for sorting, O(1) enqueue/dequeue
- **Command History**: O(1) undo/redo, O(n) for size management
- **Input Polling**: O(1) per input type per frame
- **Memory**: Configurable with max queue/history sizes

## Future Enhancements

1. **Input Profiles**: Multiple configurations (e.g., accessibility)
2. **Combo System**: Sequential command detection
3. **Macro Recording**: Command sequence playback
4. **Touch Input**: Mobile platform support
5. **Command Pooling**: Reduce GC allocations
6. **Network Synchronization**: Multiplayer command transmission
7. **Input Prediction**: Latency compensation
8. **Gesture Recognition**: Complex input patterns

## File Structure

```
PokeNET.Domain/
├── Input/
│   ├── ICommand.cs
│   ├── CommandBase.cs
│   ├── CommandQueue.cs
│   ├── CommandHistory.cs
│   ├── InputConfig.cs
│   └── Commands/
│       ├── MoveCommand.cs
│       ├── InteractCommand.cs
│       ├── MenuCommand.cs
│       └── PauseCommand.cs
└── ECS/
    ├── Systems/
    │   └── InputSystem.cs
    └── Events/
        └── InputEvents.cs

config/
└── InputConfig.json

tests/
└── InputSystemTests.cs

docs/
└── InputSystem-Architecture.md
```

## Conclusion

The InputSystem provides a robust, flexible, and maintainable foundation for game input handling. The Command pattern enables powerful features like undo/redo, input replay, and network synchronization, while maintaining clean architecture principles and comprehensive test coverage.
