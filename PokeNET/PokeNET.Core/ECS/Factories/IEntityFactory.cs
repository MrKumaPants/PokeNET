using System;
using System.Collections.Generic;
using Arch.Core;
using PokeNET.Core.ECS.Commands;

namespace PokeNET.Core.ECS.Factories;

/// <summary>
/// Factory interface for creating entities with predefined component configurations.
/// Follows the Open/Closed Principle - extend by creating new factory implementations.
/// Enables separation of entity creation logic from business logic.
///
/// Phase 6 Migration: Now uses CommandBuffer for safe deferred entity creation.
/// This prevents iterator invalidation crashes when factories are called during queries.
/// </summary>
public interface IEntityFactory
{
    /// <summary>
    /// Creates an entity from an entity definition with validation using CommandBuffer.
    /// Safe to call during query execution - structural changes are deferred.
    /// </summary>
    /// <param name="cmd">CommandBuffer for deferred entity creation.</param>
    /// <param name="definition">The entity definition containing components and metadata.</param>
    /// <returns>CreateCommand that can be used to get the entity after Playback via GetEntity().</returns>
    /// <exception cref="ArgumentNullException">If cmd or definition is null.</exception>
    /// <exception cref="InvalidOperationException">If entity creation fails validation.</exception>
    CommandBuffer.CreateCommand Create(CommandBuffer cmd, EntityDefinition definition);

    /// <summary>
    /// Creates an entity from a registered template by name using CommandBuffer.
    /// Safe to call during query execution - structural changes are deferred.
    /// </summary>
    /// <param name="cmd">CommandBuffer for deferred entity creation.</param>
    /// <param name="templateName">The name of the registered template.</param>
    /// <returns>CreateCommand that can be used to get the entity after Playback via GetEntity().</returns>
    /// <exception cref="ArgumentException">If template name is not found.</exception>
    CommandBuffer.CreateCommand CreateFromTemplate(CommandBuffer cmd, string templateName);

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
