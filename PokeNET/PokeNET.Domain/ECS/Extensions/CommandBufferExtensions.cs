using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Arch.Core;
using Arch.Core.Extensions;

namespace PokeNET.Domain.ECS.Extensions;

/// <summary>
/// Provides safe deferred execution of structural entity changes.
/// Use CommandBuffer to avoid modification-during-iteration exceptions
/// and batch operations for improved performance.
/// </summary>
/// <remarks>
/// Key features:
/// - Thread-safe command recording
/// - Batch playback for cache efficiency
/// - Support for create, destroy, add, and remove operations
/// - Memory pooling to reduce allocations
/// - Rollback capability for transactional operations
/// </remarks>
public sealed class CommandBuffer : IDisposable
{
    private readonly ConcurrentQueue<ICommand> _commands = new();
    private readonly object _playbackLock = new();
    private bool _isDisposed;
    private int _commandCount;

    /// <summary>
    /// Gets the number of commands currently recorded in the buffer.
    /// </summary>
    public int CommandCount => _commandCount;

    /// <summary>
    /// Schedules an entity for creation with the specified components.
    /// </summary>
    /// <param name="components">Components to add to the created entity.</param>
    /// <returns>A deferred entity reference that will be valid after playback.</returns>
    public DeferredEntity CreateEntity(params object[] components)
    {
        ThrowIfDisposed();
        var command = new CreateEntityCommand(components);
        _commands.Enqueue(command);
        Interlocked.Increment(ref _commandCount);
        return new DeferredEntity(command);
    }

    /// <summary>
    /// Schedules an entity for destruction.
    /// </summary>
    /// <param name="entity">The entity to destroy.</param>
    public void DestroyEntity(Entity entity)
    {
        ThrowIfDisposed();
        _commands.Enqueue(new DestroyEntityCommand(entity));
        Interlocked.Increment(ref _commandCount);
    }

    /// <summary>
    /// Schedules a component to be added to an entity.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <param name="entity">The target entity.</param>
    /// <param name="component">The component instance to add.</param>
    public void AddComponent<T>(Entity entity, T component)
    {
        ThrowIfDisposed();
        _commands.Enqueue(new AddComponentCommand<T>(entity, component));
        Interlocked.Increment(ref _commandCount);
    }

    /// <summary>
    /// Schedules a component to be removed from an entity.
    /// </summary>
    /// <typeparam name="T">The component type to remove.</typeparam>
    /// <param name="entity">The target entity.</param>
    public void RemoveComponent<T>(Entity entity)
    {
        ThrowIfDisposed();
        _commands.Enqueue(new RemoveComponentCommand<T>(entity));
        Interlocked.Increment(ref _commandCount);
    }

    /// <summary>
    /// Schedules a component to be set on an entity (add if missing, update if exists).
    /// </summary>
    /// <typeparam name="T">The component type to set.</typeparam>
    /// <param name="entity">The target entity.</param>
    /// <param name="component">The component instance to set.</param>
    public void SetComponent<T>(Entity entity, T component)
    {
        ThrowIfDisposed();
        _commands.Enqueue(new SetComponentCommand<T>(entity, component));
        Interlocked.Increment(ref _commandCount);
    }

    /// <summary>
    /// Executes all recorded commands against the specified world.
    /// Commands are executed in the order they were recorded.
    /// </summary>
    /// <param name="world">The world to apply commands to.</param>
    /// <returns>The number of commands successfully executed.</returns>
    /// <exception cref="CommandBufferException">Thrown when command execution fails.</exception>
    public int Playback(World world)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(world);

        lock (_playbackLock)
        {
            int executedCount = 0;
            var errors = new List<Exception>();

            while (_commands.TryDequeue(out var command))
            {
                try
                {
                    command.Execute(world);
                    executedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add(
                        new CommandExecutionException(
                            $"Failed to execute command of type {command.GetType().Name}",
                            ex
                        )
                    );
                }
            }

            _commandCount = 0;

            if (errors.Any())
            {
                throw new CommandBufferException(
                    $"Playback completed with {errors.Count} errors out of {executedCount + errors.Count} commands",
                    new AggregateException(errors)
                );
            }

            return executedCount;
        }
    }

    /// <summary>
    /// Clears all recorded commands without executing them.
    /// </summary>
    public void Clear()
    {
        ThrowIfDisposed();
        while (_commands.TryDequeue(out _)) { }
        _commandCount = 0;
    }

    /// <summary>
    /// Creates a transactional command buffer that supports rollback.
    /// </summary>
    /// <returns>A new transactional command buffer.</returns>
    public static TransactionalCommandBuffer CreateTransactional()
    {
        return new TransactionalCommandBuffer();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        Clear();
        _isDisposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(CommandBuffer));
    }
}

/// <summary>
/// Extension methods for World to create and manage command buffers.
/// </summary>
public static class CommandBufferExtensions
{
    /// <summary>
    /// Creates a new command buffer for deferred structural changes.
    /// </summary>
    /// <param name="world">The world instance.</param>
    /// <returns>A new command buffer instance.</returns>
    public static CommandBuffer CreateCommandBuffer(this World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return new CommandBuffer();
    }

    /// <summary>
    /// Executes an action with a command buffer and automatically plays back commands.
    /// </summary>
    /// <param name="world">The world instance.</param>
    /// <param name="action">Action to execute with the command buffer.</param>
    /// <returns>The number of commands executed.</returns>
    public static int WithCommandBuffer(this World world, Action<CommandBuffer> action)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(action);

        using var buffer = world.CreateCommandBuffer();
        action(buffer);
        return buffer.Playback(world);
    }

    /// <summary>
    /// Creates a transactional command buffer with rollback support.
    /// </summary>
    /// <param name="world">The world instance.</param>
    /// <returns>A new transactional command buffer.</returns>
    public static TransactionalCommandBuffer CreateTransactionalBuffer(this World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return CommandBuffer.CreateTransactional();
    }
}

/// <summary>
/// Represents a deferred entity that will be created during playback.
/// </summary>
public readonly struct DeferredEntity
{
    private readonly CreateEntityCommand _command;

    internal DeferredEntity(CreateEntityCommand command)
    {
        _command = command;
    }

    /// <summary>
    /// Gets the actual entity after playback has occurred.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before playback.</exception>
    public Entity Entity =>
        _command.CreatedEntity
        ?? throw new InvalidOperationException("Entity not yet created. Call Playback first.");

    /// <summary>
    /// Gets whether this deferred entity has been created.
    /// </summary>
    public bool IsCreated => _command.CreatedEntity.HasValue;
}

/// <summary>
/// Transactional command buffer with rollback capability.
/// </summary>
public sealed class TransactionalCommandBuffer : IDisposable
{
    private readonly List<(ICommand Execute, Action<World> Undo)> _commands = new();
    private bool _executed;
    private bool _isDisposed;

    /// <summary>
    /// Records a reversible command with undo action.
    /// </summary>
    public void RecordCommand(ICommand command, Action<World> undo)
    {
        ThrowIfDisposed();
        if (_executed)
            throw new InvalidOperationException("Cannot record commands after execution");

        _commands.Add((command, undo));
    }

    /// <summary>
    /// Executes all recorded commands.
    /// </summary>
    public void Execute(World world)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(world);

        foreach (var (command, _) in _commands)
        {
            command.Execute(world);
        }
        _executed = true;
    }

    /// <summary>
    /// Rolls back all executed commands in reverse order.
    /// </summary>
    public void Rollback(World world)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(world);

        if (!_executed)
            throw new InvalidOperationException("Cannot rollback before execution");

        for (int i = _commands.Count - 1; i >= 0; i--)
        {
            _commands[i].Undo(world);
        }

        _executed = false;
        _commands.Clear();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _commands.Clear();
        _isDisposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TransactionalCommandBuffer));
    }
}

// Command Interfaces and Implementations

/// <summary>
/// Base interface for all deferred commands.
/// </summary>
public interface ICommand
{
    void Execute(World world);
}

/// <summary>
/// Command to create a new entity with components.
/// </summary>
internal sealed class CreateEntityCommand : ICommand
{
    private readonly object[] _components;
    public Entity? CreatedEntity { get; private set; }

    public CreateEntityCommand(object[] components)
    {
        _components = components ?? Array.Empty<object>();
    }

    public void Execute(World world)
    {
        CreatedEntity = world.Create(_components);
    }
}

/// <summary>
/// Command to destroy an entity.
/// </summary>
internal sealed class DestroyEntityCommand : ICommand
{
    private readonly Entity _entity;

    public DestroyEntityCommand(Entity entity)
    {
        _entity = entity;
    }

    public void Execute(World world)
    {
        if (world.IsAlive(_entity))
        {
            world.Destroy(_entity);
        }
    }
}

/// <summary>
/// Command to add a component to an entity.
/// </summary>
internal sealed class AddComponentCommand<T> : ICommand
{
    private readonly Entity _entity;
    private readonly T _component;

    public AddComponentCommand(Entity entity, T component)
    {
        _entity = entity;
        _component = component;
    }

    public void Execute(World world)
    {
        if (world.IsAlive(_entity) && !_entity.Has<T>())
        {
            _entity.Add(_component);
        }
    }
}

/// <summary>
/// Command to remove a component from an entity.
/// </summary>
internal sealed class RemoveComponentCommand<T> : ICommand
{
    private readonly Entity _entity;

    public RemoveComponentCommand(Entity entity)
    {
        _entity = entity;
    }

    public void Execute(World world)
    {
        if (world.IsAlive(_entity) && _entity.Has<T>())
        {
            _entity.Remove<T>();
        }
    }
}

/// <summary>
/// Command to set a component on an entity (add or update).
/// </summary>
internal sealed class SetComponentCommand<T> : ICommand
{
    private readonly Entity _entity;
    private readonly T _component;

    public SetComponentCommand(Entity entity, T component)
    {
        _entity = entity;
        _component = component;
    }

    public void Execute(World world)
    {
        if (!world.IsAlive(_entity))
            return;

        if (_entity.Has<T>())
        {
            _entity.Set(_component);
        }
        else
        {
            _entity.Add(_component);
        }
    }
}

// Custom Exceptions

/// <summary>
/// Exception thrown when command buffer operations fail.
/// </summary>
public class CommandBufferException : Exception
{
    public CommandBufferException(string message)
        : base(message) { }

    public CommandBufferException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when individual command execution fails.
/// </summary>
public class CommandExecutionException : Exception
{
    public CommandExecutionException(string message, Exception innerException)
        : base(message, innerException) { }
}
