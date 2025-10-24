using Arch.Core;

namespace PokeNET.Domain.ECS.Factories;

/// <summary>
/// Factory interface for creating entities with predefined component configurations.
/// Follows the Open/Closed Principle - extend by creating new factory implementations.
/// Enables separation of entity creation logic from business logic.
/// </summary>
public interface IEntityFactory
{
    /// <summary>
    /// Creates an entity from an entity definition with validation.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="definition">The entity definition containing components and metadata.</param>
    /// <returns>The created entity.</returns>
    /// <exception cref="ArgumentNullException">If world or definition is null.</exception>
    /// <exception cref="InvalidOperationException">If entity creation fails validation.</exception>
    Entity Create(World world, EntityDefinition definition);

    /// <summary>
    /// Creates an entity from a registered template by name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="templateName">The name of the registered template.</param>
    /// <returns>The created entity.</returns>
    /// <exception cref="ArgumentException">If template name is not found.</exception>
    Entity CreateFromTemplate(World world, string templateName);

    /// <summary>
    /// Registers a named template for reuse.
    /// </summary>
    /// <param name="name">The unique name for the template.</param>
    /// <param name="definition">The entity definition template.</param>
    /// <exception cref="ArgumentException">If name is empty or already registered.</exception>
    void RegisterTemplate(string name, EntityDefinition definition);

    /// <summary>
    /// Checks if a template is registered.
    /// </summary>
    /// <param name="name">The template name to check.</param>
    /// <returns>True if the template exists.</returns>
    bool HasTemplate(string name);

    /// <summary>
    /// Gets all registered template names.
    /// </summary>
    /// <returns>Collection of template names.</returns>
    IEnumerable<string> GetTemplateNames();

    /// <summary>
    /// Unregisters a template by name.
    /// </summary>
    /// <param name="name">The template name to remove.</param>
    /// <returns>True if the template was removed.</returns>
    bool UnregisterTemplate(string name);
}
