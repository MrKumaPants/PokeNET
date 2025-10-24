using Arch.Core;

namespace PokeNET.Domain.ECS.Systems;

/// <summary>
/// Base interface for all ECS systems.
/// Follows the Interface Segregation Principle by defining minimal contract.
/// Systems process entities and their components each frame.
/// </summary>
public interface ISystem : IDisposable
{
    /// <summary>
    /// Gets the execution priority of this system.
    /// Lower values execute first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets whether this system is currently enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Initializes the system with the ECS world.
    /// Called once before the first Update.
    /// </summary>
    /// <param name="world">The ECS world instance.</param>
    void Initialize(World world);

    /// <summary>
    /// Updates the system logic for the current frame.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds.</param>
    void Update(float deltaTime);
}
