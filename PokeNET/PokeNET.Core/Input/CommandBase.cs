using System;
using Arch.Core;

namespace PokeNET.Core.Input;

/// <summary>
/// Abstract base class providing common functionality for commands.
/// Follows the Template Method pattern.
/// </summary>
public abstract class CommandBase : ICommand
{
    /// <inheritdoc/>
    public virtual int Priority => 0;

    /// <inheritdoc/>
    public DateTime Timestamp { get; }

    /// <inheritdoc/>
    public virtual bool SupportsUndo => false;

    /// <summary>
    /// Initializes a new command with the current timestamp.
    /// </summary>
    protected CommandBase()
    {
        Timestamp = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public virtual bool CanExecute(World world)
    {
        return world != null;
    }

    /// <inheritdoc/>
    public abstract void Execute(World world);

    /// <inheritdoc/>
    public virtual bool Undo(World world)
    {
        return false; // Default: no undo support
    }
}
