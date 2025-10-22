using Arch.Core;

namespace PokeNET.Domain.ECS.Systems;

/// <summary>
/// Manages the lifecycle and execution of ECS systems.
/// Follows the Single Responsibility Principle and Interface Segregation Principle.
/// </summary>
public interface ISystemManager : IDisposable
{
    /// <summary>
    /// Registers a system for execution.
    /// Systems are executed in order of their Priority property.
    /// </summary>
    /// <param name="system">The system to register.</param>
    void RegisterSystem(ISystem system);

    /// <summary>
    /// Removes a system from execution.
    /// </summary>
    /// <param name="system">The system to remove.</param>
    void UnregisterSystem(ISystem system);

    /// <summary>
    /// Initializes all registered systems with the world.
    /// </summary>
    /// <param name="world">The ECS world instance.</param>
    void InitializeSystems(World world);

    /// <summary>
    /// Updates all enabled systems for the current frame.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds.</param>
    void UpdateSystems(float deltaTime);

    /// <summary>
    /// Gets a system of the specified type.
    /// </summary>
    /// <typeparam name="T">The system type.</typeparam>
    /// <returns>The system instance if found, null otherwise.</returns>
    T? GetSystem<T>() where T : class, ISystem;
}
