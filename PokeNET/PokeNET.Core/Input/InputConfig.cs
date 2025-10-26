using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework.Input;

namespace PokeNET.Core.Input;

/// <summary>
/// Configuration for input bindings and settings.
/// Supports serialization to/from JSON for persistent configuration.
/// </summary>
public class InputConfig
{
    /// <summary>
    /// Keyboard bindings for game actions.
    /// </summary>
    [JsonPropertyName("keyboardBindings")]
    public Dictionary<string, Keys> KeyboardBindings { get; set; } = new();

    /// <summary>
    /// Gamepad bindings for game actions.
    /// </summary>
    [JsonPropertyName("gamepadBindings")]
    public Dictionary<string, Buttons> GamepadBindings { get; set; } = new();

    /// <summary>
    /// Input sensitivity settings.
    /// </summary>
    [JsonPropertyName("sensitivity")]
    public float Sensitivity { get; set; } = 1.0f;

    /// <summary>
    /// Dead zone for analog stick input (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("deadZone")]
    public float DeadZone { get; set; } = 0.2f;

    /// <summary>
    /// Whether to enable input buffering.
    /// </summary>
    [JsonPropertyName("enableBuffering")]
    public bool EnableBuffering { get; set; } = true;

    /// <summary>
    /// Maximum number of inputs to buffer.
    /// </summary>
    [JsonPropertyName("maxBufferSize")]
    public int MaxBufferSize { get; set; } = 10;

    /// <summary>
    /// Creates a default input configuration.
    /// </summary>
    public static InputConfig CreateDefault()
    {
        return new InputConfig
        {
            KeyboardBindings = new Dictionary<string, Keys>
            {
                // Movement
                ["MoveUp"] = Keys.W,
                ["MoveDown"] = Keys.S,
                ["MoveLeft"] = Keys.A,
                ["MoveRight"] = Keys.D,

                // Actions
                ["Interact"] = Keys.E,
                ["Cancel"] = Keys.Q,
                ["Action1"] = Keys.Space,
                ["Action2"] = Keys.LeftShift,

                // Menu
                ["Pause"] = Keys.Escape,
                ["Menu"] = Keys.Tab,
                ["Inventory"] = Keys.I,
                ["Map"] = Keys.M,

                // Debug
                ["DebugToggle"] = Keys.F3,
                ["DebugSpeed"] = Keys.F4,

                // Undo/Redo (for debugging/replays)
                ["Undo"] = Keys.Z,
                ["Redo"] = Keys.Y,
            },
            GamepadBindings = new Dictionary<string, Buttons>
            {
                // Movement handled by thumbstick
                ["Interact"] = Buttons.A,
                ["Cancel"] = Buttons.B,
                ["Action1"] = Buttons.X,
                ["Action2"] = Buttons.Y,

                ["Pause"] = Buttons.Start,
                ["Menu"] = Buttons.Back,

                ["ShoulderLeft"] = Buttons.LeftShoulder,
                ["ShoulderRight"] = Buttons.RightShoulder,
            },
            Sensitivity = 1.0f,
            DeadZone = 0.2f,
            EnableBuffering = true,
            MaxBufferSize = 10,
        };
    }

    /// <summary>
    /// Loads configuration from a JSON file.
    /// </summary>
    public static InputConfig LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return CreateDefault();

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<InputConfig>(json) ?? CreateDefault();
        }
        catch
        {
            return CreateDefault();
        }
    }

    /// <summary>
    /// Saves configuration to a JSON file.
    /// </summary>
    public void SaveToFile(string filePath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };

        var json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Remaps a keyboard binding.
    /// </summary>
    public void RemapKey(string action, Keys newKey)
    {
        KeyboardBindings[action] = newKey;
    }

    /// <summary>
    /// Remaps a gamepad binding.
    /// </summary>
    public void RemapButton(string action, Buttons newButton)
    {
        GamepadBindings[action] = newButton;
    }

    /// <summary>
    /// Gets the keyboard key for an action.
    /// </summary>
    public Keys? GetKey(string action)
    {
        return KeyboardBindings.TryGetValue(action, out var key) ? key : null;
    }

    /// <summary>
    /// Gets the gamepad button for an action.
    /// </summary>
    public Buttons? GetButton(string action)
    {
        return GamepadBindings.TryGetValue(action, out var button) ? button : null;
    }
}
