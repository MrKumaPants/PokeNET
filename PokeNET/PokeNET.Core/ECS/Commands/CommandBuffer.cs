using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;

namespace PokeNET.Core.ECS.Commands;

/// <summary>
/// CommandBuffer for safe deferred structural changes to the ECS World.
/// Prevents collection modification exceptions during query iteration.
///
/// Usage:
/// <code>
/// using var cmd = new CommandBuffer(World);
/// World.Query(in query, (Entity e) => {
///     if (condition) cmd.Destroy(e);
///     if (needComponent) cmd.Add&lt;Component&gt;(e);
/// });
/// cmd.Playback(); // Execute all deferred changes
/// </code>
///
/// Migration from unsafe patterns:
/// - World.Destroy(entity) during iteration → cmd.Destroy(entity)
/// - World.Create() during iteration → cmd.Create()
/// - World.Add&lt;T&gt;(entity) during iteration → cmd.Add&lt;T&gt;(entity)
/// - World.Remove&lt;T&gt;(entity) during iteration → cmd.Remove&lt;T&gt;(entity)
/// </summary>
public sealed class CommandBuffer : IDisposable
{
    private readonly World _world;
    private readonly List<ICommand> _commands;
    private bool _isPlayedBack;

    public CommandBuffer(World world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _commands = new List<ICommand>();
        _isPlayedBack = false;
    }

    /// <summary>
    /// Defers entity destruction until Playback.
    /// Safe to call during query iteration.
    /// </summary>
    public void Destroy(in Entity entity)
    {
        ThrowIfPlayedBack();
        _commands.Add(new DestroyCommand(entity));
    }

    /// <summary>
    /// Defers entity creation until Playback.
    /// Safe to call during query iteration.
    /// </summary>
    /// <returns>A reference to the command (entity will be created during Playback)</returns>
    public CreateCommand Create()
    {
        ThrowIfPlayedBack();
        var cmd = new CreateCommand();
        _commands.Add(cmd);
        return cmd;
    }

    /// <summary>
    /// Defers component addition until Playback.
    /// Safe to call during query iteration.
    /// </summary>
    public void Add<T>(in Entity entity)
        where T : struct
    {
        ThrowIfPlayedBack();
        _commands.Add(new AddComponentCommand<T>(entity));
    }

    /// <summary>
    /// Defers component addition with value until Playback.
    /// Safe to call during query iteration.
    /// </summary>
    public void Add<T>(in Entity entity, T component)
        where T : struct
    {
        ThrowIfPlayedBack();
        _commands.Add(new AddComponentWithValueCommand<T>(entity, component));
    }

    /// <summary>
    /// Defers component removal until Playback.
    /// Safe to call during query iteration.
    /// </summary>
    public void Remove<T>(in Entity entity)
        where T : struct
    {
        ThrowIfPlayedBack();
        _commands.Add(new RemoveComponentCommand<T>(entity));
    }

    /// <summary>
    /// Executes all deferred commands in order.
    /// Must be called AFTER query iteration completes.
    /// </summary>
    public void Playback()
    {
        if (_isPlayedBack)
            return;

        foreach (var command in _commands)
        {
            command.Execute(_world);
        }

        _isPlayedBack = true;
        _commands.Clear();
    }

    private void ThrowIfPlayedBack()
    {
        if (_isPlayedBack)
            throw new InvalidOperationException(
                "CommandBuffer has already been played back. Create a new CommandBuffer for additional commands."
            );
    }

    public void Dispose()
    {
        if (!_isPlayedBack)
        {
            Playback();
        }
    }

    // Internal command interface
    private interface ICommand
    {
        void Execute(World world);
    }

    private readonly struct DestroyCommand : ICommand
    {
        private readonly Entity _entity;

        public DestroyCommand(Entity entity)
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

    public sealed class CreateCommand : ICommand
    {
        private Entity _createdEntity;
        private readonly List<Action<World, Entity>> _componentAdders;

        public CreateCommand()
        {
            _componentAdders = new List<Action<World, Entity>>();
        }

        /// <summary>
        /// Adds a component to the entity being created.
        /// </summary>
        public CreateCommand With<T>()
            where T : struct
        {
            _componentAdders.Add((world, entity) => world.Add<T>(entity));
            return this;
        }

        /// <summary>
        /// Adds a component with value to the entity being created.
        /// </summary>
        public CreateCommand With<T>(T component)
            where T : struct
        {
            // Capture component value in closure
            var componentCopy = component;
            _componentAdders.Add((world, entity) => world.Add(entity, componentCopy));
            return this;
        }

        /// <summary>
        /// Gets the created entity (only valid after Playback).
        /// </summary>
        public Entity GetEntity()
        {
            if (_createdEntity == default)
                throw new InvalidOperationException(
                    "Entity has not been created yet. Call Playback first."
                );
            return _createdEntity;
        }

        public void Execute(World world)
        {
            _createdEntity = world.Create();
            foreach (var adder in _componentAdders)
            {
                adder(world, _createdEntity);
            }
        }
    }

    private readonly struct AddComponentCommand<T> : ICommand
        where T : struct
    {
        private readonly Entity _entity;

        public AddComponentCommand(Entity entity)
        {
            _entity = entity;
        }

        public void Execute(World world)
        {
            if (world.IsAlive(_entity) && !world.Has<T>(_entity))
            {
                world.Add<T>(_entity);
            }
        }
    }

    private readonly struct AddComponentWithValueCommand<T> : ICommand
        where T : struct
    {
        private readonly Entity _entity;
        private readonly T _component;

        public AddComponentWithValueCommand(Entity entity, T component)
        {
            _entity = entity;
            _component = component;
        }

        public void Execute(World world)
        {
            if (world.IsAlive(_entity))
            {
                if (world.Has<T>(_entity))
                {
                    world.Set(_entity, _component);
                }
                else
                {
                    world.Add(_entity, _component);
                }
            }
        }
    }

    private readonly struct RemoveComponentCommand<T> : ICommand
        where T : struct
    {
        private readonly Entity _entity;

        public RemoveComponentCommand(Entity entity)
        {
            _entity = entity;
        }

        public void Execute(World world)
        {
            if (world.IsAlive(_entity) && world.Has<T>(_entity))
            {
                world.Remove<T>(_entity);
            }
        }
    }
}
