using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Audio.Configuration
{
    /// <summary>
    /// Loads and manages audio reaction configurations from JSON files with hot-reload support.
    /// </summary>
    public class AudioReactionLoader : IDisposable
    {
        private readonly ILogger<AudioReactionLoader> _logger;
        private readonly IAudioManager _audioManager;
        private readonly string _configFilePath;
        private FileSystemWatcher? _fileWatcher;
        private AudioReactionConfig? _config;
        private bool _isDisposed;

        /// <summary>
        /// Gets the currently loaded configuration.
        /// </summary>
        public AudioReactionConfig? Configuration => _config;

        /// <summary>
        /// Event raised when the configuration is reloaded.
        /// </summary>
        public event EventHandler<ConfigurationReloadedEventArgs>? ConfigurationReloaded;

        /// <summary>
        /// Initializes a new instance of the AudioReactionLoader class.
        /// </summary>
        /// <param name="logger">Logger for diagnostics.</param>
        /// <param name="audioManager">Audio manager for playback control.</param>
        /// <param name="configFilePath">Path to the JSON configuration file.</param>
        public AudioReactionLoader(
            ILogger<AudioReactionLoader> logger,
            IAudioManager audioManager,
            string configFilePath)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _audioManager = audioManager ?? throw new ArgumentNullException(nameof(audioManager));
            _configFilePath = configFilePath ?? throw new ArgumentNullException(nameof(configFilePath));
        }

        /// <summary>
        /// Loads the audio reaction configuration from the JSON file.
        /// </summary>
        /// <returns>The loaded configuration.</returns>
        /// <exception cref="FileNotFoundException">Thrown when config file is not found.</exception>
        /// <exception cref="JsonException">Thrown when JSON is invalid.</exception>
        public async Task<AudioReactionConfig> LoadAsync()
        {
            if (!File.Exists(_configFilePath))
            {
                _logger.LogError("Configuration file not found: {FilePath}", _configFilePath);
                throw new FileNotFoundException($"Audio reaction config file not found: {_configFilePath}");
            }

            _logger.LogInformation("Loading audio reaction configuration from: {FilePath}", _configFilePath);

            try
            {
                var json = await File.ReadAllTextAsync(_configFilePath);
                var config = JsonSerializer.Deserialize<AudioReactionConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });

                if (config == null)
                {
                    throw new JsonException("Failed to deserialize audio reaction configuration");
                }

                ValidateConfiguration(config);
                _config = config;

                _logger.LogInformation("Loaded {Count} audio reactions", config.Reactions.Count);
                return config;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON in audio reaction configuration");
                throw;
            }
        }

        /// <summary>
        /// Validates the loaded configuration for correctness.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
        private void ValidateConfiguration(AudioReactionConfig config)
        {
            var errors = new List<string>();

            foreach (var reaction in config.Reactions)
            {
                // Validate reaction name
                if (string.IsNullOrWhiteSpace(reaction.Name))
                {
                    errors.Add("Reaction must have a name");
                }

                // Validate event type
                if (string.IsNullOrWhiteSpace(reaction.EventType))
                {
                    errors.Add($"Reaction '{reaction.Name}' must have an event type");
                }

                // Validate conditions
                foreach (var condition in reaction.Conditions)
                {
                    if (string.IsNullOrWhiteSpace(condition.Property))
                    {
                        errors.Add($"Reaction '{reaction.Name}' has a condition without a property");
                    }

                    var validOperators = new[] { "equals", "notEquals", "lessThan", "greaterThan", "contains" };
                    if (!validOperators.Contains(condition.Operator, StringComparer.OrdinalIgnoreCase))
                    {
                        errors.Add($"Reaction '{reaction.Name}' has invalid operator '{condition.Operator}'. Valid operators: {string.Join(", ", validOperators)}");
                    }

                    if (condition.Value == null)
                    {
                        errors.Add($"Reaction '{reaction.Name}' has a condition without a value");
                    }
                }

                // Validate actions
                if (reaction.Actions.Count == 0)
                {
                    errors.Add($"Reaction '{reaction.Name}' must have at least one action");
                }

                foreach (var action in reaction.Actions)
                {
                    var validActionTypes = new[] { "PlayMusic", "PlaySound", "FadeIn", "FadeOut", "SetVolume", "StopAll" };
                    if (!validActionTypes.Contains(action.Type, StringComparer.OrdinalIgnoreCase))
                    {
                        errors.Add($"Reaction '{reaction.Name}' has invalid action type '{action.Type}'. Valid types: {string.Join(", ", validActionTypes)}");
                    }

                    // Validate path for playback actions
                    if ((action.Type.Equals("PlayMusic", StringComparison.OrdinalIgnoreCase) ||
                         action.Type.Equals("PlaySound", StringComparison.OrdinalIgnoreCase)) &&
                        string.IsNullOrWhiteSpace(action.Path))
                    {
                        errors.Add($"Reaction '{reaction.Name}' action '{action.Type}' requires a path");
                    }

                    // Validate volume range
                    if (action.Volume < 0.0f || action.Volume > 1.0f)
                    {
                        errors.Add($"Reaction '{reaction.Name}' has invalid volume {action.Volume}. Must be between 0.0 and 1.0");
                    }

                    // Validate duration for fade actions
                    if ((action.Type.Equals("FadeIn", StringComparison.OrdinalIgnoreCase) ||
                         action.Type.Equals("FadeOut", StringComparison.OrdinalIgnoreCase)) &&
                        action.Duration <= 0)
                    {
                        errors.Add($"Reaction '{reaction.Name}' fade action requires a positive duration");
                    }
                }
            }

            if (errors.Any())
            {
                var errorMessage = $"Configuration validation failed:\n{string.Join("\n", errors)}";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            _logger.LogInformation("Configuration validation passed");
        }

        /// <summary>
        /// Evaluates a condition against an event object using reflection.
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="eventObj">The event object to evaluate against.</param>
        /// <returns>True if the condition is met, false otherwise.</returns>
        public bool EvaluateCondition(ConditionDefinition condition, object eventObj)
        {
            if (eventObj == null)
                return false;

            var property = eventObj.GetType().GetProperty(condition.Property, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
            {
                _logger.LogWarning("Property '{Property}' not found on event type {EventType}", condition.Property, eventObj.GetType().Name);
                return false;
            }

            var actualValue = property.GetValue(eventObj);
            var expectedValue = condition.Value;

            return condition.Operator.ToLowerInvariant() switch
            {
                "equals" => CompareEquals(actualValue, expectedValue),
                "notequals" => !CompareEquals(actualValue, expectedValue),
                "lessthan" => CompareLessThan(actualValue, expectedValue),
                "greaterthan" => CompareGreaterThan(actualValue, expectedValue),
                "contains" => CompareContains(actualValue, expectedValue),
                _ => false
            };
        }

        private bool CompareEquals(object? actual, object? expected)
        {
            if (actual == null && expected == null)
                return true;
            if (actual == null || expected == null)
                return false;

            // Handle JsonElement from deserialization
            if (expected is JsonElement jsonElement)
            {
                return CompareWithJsonElement(actual, jsonElement);
            }

            return actual.Equals(Convert.ChangeType(expected, actual.GetType()));
        }

        private bool CompareLessThan(object? actual, object? expected)
        {
            if (actual == null || expected == null)
                return false;

            try
            {
                var actualDouble = Convert.ToDouble(actual);
                var expectedDouble = expected is JsonElement jsonElement
                    ? jsonElement.GetDouble()
                    : Convert.ToDouble(expected);
                return actualDouble < expectedDouble;
            }
            catch
            {
                return false;
            }
        }

        private bool CompareGreaterThan(object? actual, object? expected)
        {
            if (actual == null || expected == null)
                return false;

            try
            {
                var actualDouble = Convert.ToDouble(actual);
                var expectedDouble = expected is JsonElement jsonElement
                    ? jsonElement.GetDouble()
                    : Convert.ToDouble(expected);
                return actualDouble > expectedDouble;
            }
            catch
            {
                return false;
            }
        }

        private bool CompareContains(object? actual, object? expected)
        {
            if (actual == null || expected == null)
                return false;

            var actualString = actual.ToString() ?? string.Empty;
            var expectedString = expected is JsonElement jsonElement
                ? jsonElement.GetString() ?? string.Empty
                : expected.ToString() ?? string.Empty;

            return actualString.Contains(expectedString, StringComparison.OrdinalIgnoreCase);
        }

        private bool CompareWithJsonElement(object actual, JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.True or JsonValueKind.False => actual is bool b && b == jsonElement.GetBoolean(),
                JsonValueKind.Number => actual is IConvertible && Math.Abs(Convert.ToDouble(actual) - jsonElement.GetDouble()) < 0.0001,
                JsonValueKind.String => actual.ToString() == jsonElement.GetString(),
                _ => false
            };
        }

        /// <summary>
        /// Executes an action using the audio manager.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>A task representing the operation.</returns>
        public async Task ExecuteActionAsync(ActionDefinition action)
        {
            try
            {
                switch (action.Type.ToLowerInvariant())
                {
                    case "playmusic":
                        if (!string.IsNullOrEmpty(action.Path))
                        {
                            await _audioManager.PlayMusicAsync(action.Path, action.Volume);
                        }
                        break;

                    case "playsound":
                        if (!string.IsNullOrEmpty(action.Path))
                        {
                            await _audioManager.PlaySoundEffectAsync(action.Path, action.Volume);
                        }
                        break;

                    case "fadein":
                        // Note: Current IAudioManager doesn't have explicit fade methods
                        // This would require extending the interface
                        _logger.LogWarning("FadeIn action not yet implemented");
                        break;

                    case "fadeout":
                        if (action.Channel?.Equals("Music", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            await _audioManager.StopMusicAsync();
                        }
                        break;

                    case "setvolume":
                        _logger.LogWarning("SetVolume action not yet implemented");
                        break;

                    case "stopall":
                        _audioManager.StopAll();
                        break;

                    default:
                        _logger.LogWarning("Unknown action type: {ActionType}", action.Type);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action {ActionType}", action.Type);
            }
        }

        /// <summary>
        /// Enables hot-reload by watching the configuration file for changes.
        /// </summary>
        public void EnableHotReload()
        {
            if (_fileWatcher != null)
            {
                _logger.LogWarning("Hot-reload is already enabled");
                return;
            }

            var directory = Path.GetDirectoryName(_configFilePath);
            var fileName = Path.GetFileName(_configFilePath);

            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
            {
                _logger.LogError("Invalid config file path for hot-reload: {FilePath}", _configFilePath);
                return;
            }

            _fileWatcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += async (sender, e) => await OnConfigFileChangedAsync(e);

            _logger.LogInformation("Hot-reload enabled for: {FilePath}", _configFilePath);
        }

        /// <summary>
        /// Handles configuration file changes for hot-reload.
        /// </summary>
        private async Task OnConfigFileChangedAsync(FileSystemEventArgs e)
        {
            _logger.LogInformation("Configuration file changed, reloading...");

            try
            {
                // Add a small delay to ensure file is fully written
                await Task.Delay(100);

                var newConfig = await LoadAsync();
                ConfigurationReloaded?.Invoke(this, new ConfigurationReloadedEventArgs
                {
                    Configuration = newConfig,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Configuration reloaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading configuration");
            }
        }

        /// <summary>
        /// Disposes the audio reaction loader and stops file watching.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _fileWatcher?.Dispose();
            _isDisposed = true;
        }
    }

    /// <summary>
    /// Event arguments for configuration reload events.
    /// </summary>
    public class ConfigurationReloadedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the reloaded configuration.
        /// </summary>
        public AudioReactionConfig Configuration { get; set; } = new();

        /// <summary>
        /// Gets or sets the timestamp when the reload occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
