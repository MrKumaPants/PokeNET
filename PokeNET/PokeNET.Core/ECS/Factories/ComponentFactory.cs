using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using PokeNET.Core.ECS.Factories;

namespace PokeNET.Core.ECS.Factories;

/// <summary>
/// Default implementation of IComponentFactory using a registry-based approach
/// with reflection fallback.
/// </summary>
public class ComponentFactory : IComponentFactory
{
    private readonly ConcurrentDictionary<Type, Delegate> _builders = new();
    private readonly ILogger<ComponentFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the ComponentFactory.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    public ComponentFactory(ILogger<ComponentFactory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("ComponentFactory initialized");
    }

    /// <inheritdoc/>
    public T Create<T>(ComponentDefinition definition)
        where T : struct
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));

        var type = typeof(T);

        _logger.LogDebug("Creating component of type {Type} from definition", type.Name);

        // Check for registered builder
        if (_builders.TryGetValue(type, out var builder))
        {
            try
            {
                var typedBuilder = (Func<ComponentDefinition, T>)builder;
                var component = typedBuilder(definition);
                _logger.LogDebug("Component {Type} created using registered builder", type.Name);
                return component;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Builder for {Type} threw an exception", type.Name);
                throw new ComponentCreationException(
                    $"Registered builder for {type.Name} failed",
                    type,
                    definition,
                    ex
                );
            }
        }

        // Fallback to reflection
        return CreateViaReflection<T>(definition);
    }

    /// <inheritdoc/>
    public object CreateDynamic(Type componentType, ComponentDefinition definition)
    {
        if (componentType == null)
            throw new ArgumentNullException(nameof(componentType));
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));
        if (!componentType.IsValueType)
            throw new ArgumentException(
                $"Type {componentType.Name} must be a struct",
                nameof(componentType)
            );

        _logger.LogDebug("Creating component of type {Type} dynamically", componentType.Name);

        // Check for registered builder
        if (_builders.TryGetValue(componentType, out var builder))
        {
            try
            {
                var result = builder.DynamicInvoke(definition);
                _logger.LogDebug(
                    "Component {Type} created using registered builder (dynamic)",
                    componentType.Name
                );
                return result!;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Builder for {Type} threw an exception (dynamic)",
                    componentType.Name
                );
                throw new ComponentCreationException(
                    $"Registered builder for {componentType.Name} failed",
                    componentType,
                    definition,
                    ex
                );
            }
        }

        // Fallback to reflection
        try
        {
            var instance = Activator.CreateInstance(componentType)!;
            PopulateProperties(instance, componentType, definition);
            _logger.LogDebug(
                "Component {Type} created via reflection (dynamic)",
                componentType.Name
            );
            return instance;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create component {Type} via reflection",
                componentType.Name
            );
            throw new ComponentCreationException(
                $"Failed to create component {componentType.Name} via reflection",
                componentType,
                definition,
                ex
            );
        }
    }

    /// <inheritdoc/>
    public void RegisterBuilder<T>(Func<ComponentDefinition, T> builder)
        where T : struct
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        var type = typeof(T);
        _builders[type] = builder;
        _logger.LogInformation("Registered builder for component type {Type}", type.Name);
    }

    /// <inheritdoc/>
    public bool CanCreate(Type componentType)
    {
        if (componentType == null || !componentType.IsValueType)
            return false;

        // Can create if builder is registered
        if (_builders.ContainsKey(componentType))
            return true;

        // Can create if it's a struct with settable properties
        try
        {
            var properties = componentType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToArray();
            return properties.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public IEnumerable<Type> GetRegisteredTypes()
    {
        return _builders.Keys.ToArray();
    }

    /// <inheritdoc/>
    public bool UnregisterBuilder<T>()
        where T : struct
    {
        var type = typeof(T);
        var removed = _builders.TryRemove(type, out _);
        if (removed)
        {
            _logger.LogInformation("Unregistered builder for component type {Type}", type.Name);
        }
        return removed;
    }

    private T CreateViaReflection<T>(ComponentDefinition definition)
        where T : struct
    {
        var type = typeof(T);

        try
        {
            // Create default instance
            var instance = Activator.CreateInstance<T>();

            // Set properties
            var boxed = (object)instance;
            PopulateProperties(boxed, type, definition);

            _logger.LogDebug("Component {Type} created via reflection", type.Name);
            return (T)boxed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create component {Type} via reflection", type.Name);
            throw new ComponentCreationException(
                $"Failed to create component {type.Name} via reflection",
                type,
                definition,
                ex
            );
        }
    }

    private void PopulateProperties(object instance, Type type, ComponentDefinition definition)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToArray();

        foreach (var property in properties)
        {
            if (!definition.Properties.TryGetValue(property.Name, out var value))
                continue;

            try
            {
                var convertedValue = ConvertValue(value, property.PropertyType);
                property.SetValue(instance, convertedValue);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to set property {Property} on type {Type}",
                    property.Name,
                    type.Name
                );
            }
        }
    }

    private object? ConvertValue(object value, Type targetType)
    {
        if (value == null)
            return null;

        var valueType = value.GetType();

        // Direct assignment if types match
        if (targetType.IsAssignableFrom(valueType))
            return value;

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null)
            return ConvertValue(value, underlyingType);

        // Handle enums
        if (targetType.IsEnum)
        {
            if (value is string strValue)
                return Enum.Parse(targetType, strValue, ignoreCase: true);
            return Enum.ToObject(targetType, value);
        }

        // Handle special types that need custom conversion
        if (targetType == typeof(TimeSpan) && value is string timeSpanStr)
            return TimeSpan.Parse(timeSpanStr);

        if (targetType == typeof(Guid) && value is string guidStr)
            return Guid.Parse(guidStr);

        // Try Convert.ChangeType as fallback
        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            // If all else fails, return the value as-is and let the property setter handle it
            return value;
        }
    }
}
