using Arch.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace PokeNET.Domain.Input;

/// <summary>
/// Thread-safe queue for managing and executing commands.
/// Supports priority-based execution and command buffering.
/// Follows the Queue data structure pattern with priority sorting.
/// </summary>
public class CommandQueue
{
    private readonly ILogger<CommandQueue> _logger;
    private readonly ConcurrentQueue<ICommand> _queue = new();
    private readonly int _maxQueueSize;
    private int _queuedCount;

    /// <summary>
    /// Gets the current number of queued commands.
    /// </summary>
    public int Count => _queuedCount;

    /// <summary>
    /// Gets whether the queue is empty.
    /// </summary>
    public bool IsEmpty => _queuedCount == 0;

    /// <summary>
    /// Initializes a new command queue.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="maxQueueSize">Maximum number of commands to queue (prevents input buffer overflow).</param>
    public CommandQueue(ILogger<CommandQueue> logger, int maxQueueSize = 100)
    {
        _logger = logger;
        _maxQueueSize = maxQueueSize;
    }

    /// <summary>
    /// Enqueues a command for execution.
    /// </summary>
    /// <param name="command">The command to enqueue.</param>
    /// <returns>True if the command was enqueued successfully.</returns>
    public bool Enqueue(ICommand command)
    {
        if (_queuedCount >= _maxQueueSize)
        {
            _logger.LogWarning("Command queue is full ({MaxSize}). Dropping command: {CommandType}",
                _maxQueueSize, command.GetType().Name);
            return false;
        }

        _queue.Enqueue(command);
        Interlocked.Increment(ref _queuedCount);

        _logger.LogTrace("Enqueued command: {CommandType} (Queue size: {QueueSize})",
            command.GetType().Name, _queuedCount);

        return true;
    }

    /// <summary>
    /// Processes all queued commands in priority order.
    /// </summary>
    /// <param name="world">The ECS world instance.</param>
    /// <returns>Number of commands executed.</returns>
    public int ProcessAll(World world)
    {
        if (IsEmpty)
            return 0;

        // Dequeue all commands and sort by priority
        var commands = new List<ICommand>();
        while (_queue.TryDequeue(out var command))
        {
            commands.Add(command);
            Interlocked.Decrement(ref _queuedCount);
        }

        commands.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        int executedCount = 0;
        foreach (var command in commands)
        {
            try
            {
                if (command.CanExecute(world))
                {
                    command.Execute(world);
                    executedCount++;

                    _logger.LogDebug("Executed command: {CommandType}", command.GetType().Name);
                }
                else
                {
                    _logger.LogTrace("Skipped command (CanExecute = false): {CommandType}",
                        command.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing command: {CommandType}", command.GetType().Name);
            }
        }

        if (executedCount > 0)
        {
            _logger.LogDebug("Processed {ExecutedCount} commands from queue", executedCount);
        }

        return executedCount;
    }

    /// <summary>
    /// Clears all queued commands.
    /// </summary>
    public void Clear()
    {
        while (_queue.TryDequeue(out _))
        {
            Interlocked.Decrement(ref _queuedCount);
        }

        _logger.LogInformation("Cleared command queue");
    }
}
