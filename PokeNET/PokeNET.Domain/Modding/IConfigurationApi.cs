namespace PokeNET.Domain.Modding;

/// <summary>
/// API for reading mod configuration.
/// </summary>
/// <remarks>
/// <para>
/// Configuration files should be placed in the mod's directory with the name "config.json".
/// </para>
/// <para>
/// Example directory structure:
/// <code>
/// Mods/MyMod/
///   ├── modinfo.json
///   ├── config.json       ← Mod configuration
///   ├── MyMod.dll
///   └── Assets/
/// </code>
/// </para>
/// </remarks>
public interface IConfigurationApi
{
    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    /// <typeparam name="T">Type of the configuration value.</typeparam>
    /// <param name="key">Configuration key (supports nested keys with ':' separator).</param>
    /// <param name="defaultValue">Default value if key not found.</param>
    /// <returns>The configuration value or default.</returns>
    /// <example>
    /// <code>
    /// // config.json:
    /// // {
    /// //   "difficulty": "hard",
    /// //   "features": {
    /// //     "enableCustomMoves": true
    /// //   }
    /// // }
    ///
    /// var difficulty = context.Configuration.Get("difficulty", "normal");
    /// var customMoves = context.Configuration.Get("features:enableCustomMoves", false);
    /// </code>
    /// </example>
    T Get<T>(string key, T defaultValue);

    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    /// <typeparam name="T">Type of the configuration value.</typeparam>
    /// <param name="key">Configuration key.</param>
    /// <returns>The configuration value.</returns>
    /// <exception cref="KeyNotFoundException">Key not found in configuration.</exception>
    T Get<T>(string key);

    /// <summary>
    /// Tries to get a configuration value by key.
    /// </summary>
    /// <typeparam name="T">Type of the configuration value.</typeparam>
    /// <param name="key">Configuration key.</param>
    /// <param name="value">The configuration value if found.</param>
    /// <returns>True if the key exists; otherwise false.</returns>
    bool TryGet<T>(string key, out T? value);

    /// <summary>
    /// Checks if a configuration key exists.
    /// </summary>
    /// <param name="key">Configuration key.</param>
    /// <returns>True if the key exists; otherwise false.</returns>
    bool HasKey(string key);

    /// <summary>
    /// Gets all configuration keys.
    /// </summary>
    /// <returns>List of all configuration keys.</returns>
    IReadOnlyList<string> GetAllKeys();

    /// <summary>
    /// Binds a configuration section to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">Type to bind to.</typeparam>
    /// <param name="section">Configuration section name (empty for root).</param>
    /// <returns>The bound configuration object.</returns>
    /// <example>
    /// <code>
    /// // config.json:
    /// // {
    /// //   "gameSettings": {
    /// //     "difficulty": "hard",
    /// //     "multiplier": 1.5
    /// //   }
    /// // }
    ///
    /// public class GameSettings
    /// {
    ///     public string Difficulty { get; set; } = "normal";
    ///     public double Multiplier { get; set; } = 1.0;
    /// }
    ///
    /// var settings = context.Configuration.Bind&lt;GameSettings&gt;("gameSettings");
    /// logger.LogInfo($"Difficulty: {settings.Difficulty}");
    /// </code>
    /// </example>
    T Bind<T>(string section = "") where T : new();

    /// <summary>
    /// Reloads configuration from disk (for hot reload during development).
    /// </summary>
    void Reload();
}
