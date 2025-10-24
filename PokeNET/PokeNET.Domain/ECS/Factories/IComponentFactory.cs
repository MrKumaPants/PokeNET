namespace PokeNET.Domain.ECS.Factories;

/// <summary>
/// Factory for creating components dynamically from configuration data.
/// This enables the Open/Closed Principle by allowing new component types
/// to be created without modifying existing code.
/// </summary>
/// <remarks>
/// <para>
/// The component factory supports two creation modes:
/// 1. **Registered Builders**: Fast, type-safe creation via pre-registered builder functions
/// 2. **Reflection Fallback**: Dynamic creation for unregistered types using reflection
/// </para>
/// <para>
/// This pattern is essential for:
/// - Loading entities from JSON/XML configuration files
/// - Creating components from mod data
/// - Dynamic entity spawning from scripts
/// - Hot-reloading component definitions
/// </para>
/// </remarks>
public interface IComponentFactory
{
    /// <summary>
    /// Creates a component of the specified type from a definition.
    /// </summary>
    /// <typeparam name="T">The component type to create (must be a struct).</typeparam>
    /// <param name="definition">The component definition containing property values.</param>
    /// <returns>The created component instance.</returns>
    /// <exception cref="ArgumentNullException">Definition is null.</exception>
    /// <exception cref="ComponentCreationException">Failed to create the component.</exception>
    /// <example>
    /// <code>
    /// var definition = new ComponentDefinition
    /// {
    ///     TypeName = "Position",
    ///     Properties = new Dictionary&lt;string, object&gt;
    ///     {
    ///         ["X"] = 100f,
    ///         ["Y"] = 200f,
    ///         ["Z"] = 0.5f
    ///     }
    /// };
    /// var position = factory.Create&lt;Position&gt;(definition);
    /// </code>
    /// </example>
    T Create<T>(ComponentDefinition definition) where T : struct;

    /// <summary>
    /// Creates a component dynamically when the type is only known at runtime.
    /// </summary>
    /// <param name="componentType">The component type to create.</param>
    /// <param name="definition">The component definition containing property values.</param>
    /// <returns>The created component instance as an object (requires boxing).</returns>
    /// <exception cref="ArgumentNullException">ComponentType or definition is null.</exception>
    /// <exception cref="ArgumentException">ComponentType is not a struct.</exception>
    /// <exception cref="ComponentCreationException">Failed to create the component.</exception>
    /// <example>
    /// <code>
    /// var type = Type.GetType("PokeNET.Domain.ECS.Components.Position");
    /// var component = factory.CreateDynamic(type, definition);
    /// </code>
    /// </example>
    object CreateDynamic(Type componentType, ComponentDefinition definition);

    /// <summary>
    /// Registers a custom builder function for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type (must be a struct).</typeparam>
    /// <param name="builder">The builder function that creates the component from a definition.</param>
    /// <exception cref="ArgumentNullException">Builder is null.</exception>
    /// <remarks>
    /// Registered builders are always used instead of reflection, providing better performance
    /// and allowing custom initialization logic.
    /// </remarks>
    /// <example>
    /// <code>
    /// factory.RegisterBuilder&lt;Position&gt;(def => new Position(
    ///     def.GetFloat("X", 0f),
    ///     def.GetFloat("Y", 0f),
    ///     def.GetFloat("Z", 0f)
    /// ));
    /// </code>
    /// </example>
    void RegisterBuilder<T>(Func<ComponentDefinition, T> builder) where T : struct;

    /// <summary>
    /// Checks if the factory can create a component of the specified type.
    /// </summary>
    /// <param name="componentType">The component type to check.</param>
    /// <returns>True if the component can be created; otherwise false.</returns>
    /// <remarks>
    /// Returns true if:
    /// - A builder is registered for the type, OR
    /// - The type is a struct with a parameterless constructor and settable properties
    /// </remarks>
    bool CanCreate(Type componentType);

    /// <summary>
    /// Gets all component types that have registered builders.
    /// </summary>
    /// <returns>An enumerable of registered component types.</returns>
    IEnumerable<Type> GetRegisteredTypes();

    /// <summary>
    /// Unregisters a builder for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to unregister.</typeparam>
    /// <returns>True if the builder was removed; false if no builder was registered.</returns>
    bool UnregisterBuilder<T>() where T : struct;
}

/// <summary>
/// Represents a component definition loaded from configuration data.
/// </summary>
/// <remarks>
/// This is typically deserialized from JSON/XML or created programmatically
/// by mods or scripting systems.
/// </remarks>
public record ComponentDefinition
{
    /// <summary>
    /// The component type name (e.g., "Position", "PokeNET.Domain.ECS.Components.Position").
    /// </summary>
    public string TypeName { get; init; } = string.Empty;

    /// <summary>
    /// Dictionary of property names to their values.
    /// </summary>
    /// <remarks>
    /// Values can be primitive types, strings, or nested dictionaries/arrays.
    /// The factory will attempt to convert values to the correct property types.
    /// </remarks>
    public IDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets a property value as the specified type with a default fallback.
    /// </summary>
    /// <typeparam name="T">The expected property type.</typeparam>
    /// <param name="propertyName">The property name.</param>
    /// <param name="defaultValue">The default value if the property doesn't exist.</param>
    /// <returns>The property value or default.</returns>
    public T GetProperty<T>(string propertyName, T defaultValue = default!)
    {
        if (!Properties.TryGetValue(propertyName, out var value))
            return defaultValue;

        try
        {
            return value is T typedValue ? typedValue : (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Gets a float property value.
    /// </summary>
    public float GetFloat(string propertyName, float defaultValue = 0f) =>
        GetProperty(propertyName, defaultValue);

    /// <summary>
    /// Gets an integer property value.
    /// </summary>
    public int GetInt(string propertyName, int defaultValue = 0) =>
        GetProperty(propertyName, defaultValue);

    /// <summary>
    /// Gets a string property value.
    /// </summary>
    public string GetString(string propertyName, string defaultValue = "") =>
        GetProperty(propertyName, defaultValue);

    /// <summary>
    /// Gets a boolean property value.
    /// </summary>
    public bool GetBool(string propertyName, bool defaultValue = false) =>
        GetProperty(propertyName, defaultValue);

    /// <summary>
    /// Checks if a property exists.
    /// </summary>
    public bool HasProperty(string propertyName) => Properties.ContainsKey(propertyName);
}

/// <summary>
/// Exception thrown when component creation fails.
/// </summary>
public class ComponentCreationException : Exception
{
    /// <summary>
    /// The component type that failed to create.
    /// </summary>
    public Type? ComponentType { get; }

    /// <summary>
    /// The component definition that was used.
    /// </summary>
    public ComponentDefinition? Definition { get; }

    public ComponentCreationException(string message) : base(message) { }

    public ComponentCreationException(string message, Exception innerException)
        : base(message, innerException) { }

    public ComponentCreationException(string message, Type componentType, ComponentDefinition definition)
        : base(message)
    {
        ComponentType = componentType;
        Definition = definition;
    }

    public ComponentCreationException(string message, Type componentType, ComponentDefinition definition, Exception innerException)
        : base(message, innerException)
    {
        ComponentType = componentType;
        Definition = definition;
    }
}
