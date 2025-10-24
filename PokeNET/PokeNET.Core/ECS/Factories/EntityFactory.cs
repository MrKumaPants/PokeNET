using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.ECS.Events;
using PokeNET.Domain.ECS.Factories;

namespace PokeNET.Core.ECS.Factories;

/// <summary>
/// Base implementation of entity factory with template management and validation.
/// Follows Open/Closed Principle - extend for specialized entity creation logic.
/// Thread-safe for template registration and retrieval.
/// </summary>
public class EntityFactory : IEntityFactory
{
    private readonly ILogger<EntityFactory> _logger;
    private readonly IEventBus? _eventBus;
    private readonly Dictionary<string, EntityDefinition> _templates;
    private readonly object _lock = new();

    /// <summary>
    /// Gets the factory name for logging and events.
    /// </summary>
    protected virtual string FactoryName => GetType().Name;

    public EntityFactory(ILogger<EntityFactory> logger, IEventBus? eventBus = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventBus = eventBus;
        _templates = new Dictionary<string, EntityDefinition>(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("{FactoryName} initialized", FactoryName);
    }

    /// <inheritdoc/>
    public virtual Entity Create(World world, EntityDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(definition);

        if (!definition.IsValid())
        {
            throw new InvalidOperationException(
                $"Invalid entity definition: {definition}. Name and components required.");
        }

        _logger.LogDebug("Creating entity '{Name}' with {Count} components",
            definition.Name, definition.Components.Count);

        // Validate components before creation
        ValidateComponents(definition.Components);

        // Create entity with all components at once (efficient for Arch ECS)
        var entity = world.Create(definition.Components.ToArray());

        // Publish creation event if event bus is available
        _eventBus?.Publish(new EntityCreatedEvent(
            entity,
            definition.Name,
            FactoryName,
            definition.Components.Count
        ));

        _logger.LogDebug("Entity '{Name}' created successfully: {Entity}",
            definition.Name, entity);

        return entity;
    }

    /// <inheritdoc/>
    public Entity CreateFromTemplate(World world, string templateName)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (string.IsNullOrWhiteSpace(templateName))
        {
            throw new ArgumentException("Template name cannot be empty", nameof(templateName));
        }

        EntityDefinition definition;
        lock (_lock)
        {
            if (!_templates.TryGetValue(templateName, out definition!))
            {
                throw new ArgumentException(
                    $"Template '{templateName}' not found. Available templates: " +
                    $"{string.Join(", ", _templates.Keys)}",
                    nameof(templateName));
            }
        }

        _logger.LogDebug("Creating entity from template '{Template}'", templateName);
        return Create(world, definition);
    }

    /// <inheritdoc/>
    public void RegisterTemplate(string name, EntityDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Template name cannot be empty", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(definition);

        if (!definition.IsValid())
        {
            throw new ArgumentException(
                $"Cannot register invalid definition: {definition}", nameof(definition));
        }

        lock (_lock)
        {
            if (_templates.ContainsKey(name))
            {
                throw new ArgumentException(
                    $"Template '{name}' is already registered", nameof(name));
            }

            _templates[name] = definition;
        }

        _logger.LogInformation("Registered template '{Name}': {Definition}", name, definition);
    }

    /// <inheritdoc/>
    public bool HasTemplate(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        lock (_lock)
        {
            return _templates.ContainsKey(name);
        }
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetTemplateNames()
    {
        lock (_lock)
        {
            return _templates.Keys.ToList();
        }
    }

    /// <inheritdoc/>
    public bool UnregisterTemplate(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        lock (_lock)
        {
            var removed = _templates.Remove(name);
            if (removed)
            {
                _logger.LogInformation("Unregistered template '{Name}'", name);
            }
            return removed;
        }
    }

    /// <summary>
    /// Validates components before entity creation.
    /// Override to add custom validation logic.
    /// </summary>
    /// <param name="components">Components to validate.</param>
    /// <exception cref="ArgumentException">If components are invalid.</exception>
    protected virtual void ValidateComponents(IReadOnlyList<object> components)
    {
        if (components.Count == 0)
        {
            throw new ArgumentException("Entity must have at least one component");
        }

        // Check for duplicate component types
        var types = components.Select(c => c.GetType()).ToList();
        var duplicates = types.GroupBy(t => t).Where(g => g.Count() > 1).Select(g => g.Key);

        if (duplicates.Any())
        {
            throw new ArgumentException(
                $"Duplicate component types found: {string.Join(", ", duplicates.Select(t => t.Name))}");
        }
    }
}
