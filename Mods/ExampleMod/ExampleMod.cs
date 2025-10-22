using HarmonyLib;
using Microsoft.Extensions.Logging;
using PokeNET.ModApi;

namespace ExampleMod;

/// <summary>
/// Example mod demonstrating the PokeNET modding API.
/// Shows how to:
/// - Implement IMod interface
/// - Access game services via IModContext
/// - Apply Harmony patches
/// - Use structured logging
/// </summary>
public class ExampleMod : IMod
{
    public string Id => "com.pokenet.examplemod";
    public string Name => "Example Mod";
    public Version Version => new Version(1, 0, 0);

    private IModContext? _context;
    private Harmony? _harmony;

    /// <summary>
    /// Called when the mod is loaded. Initialize resources and register patches here.
    /// </summary>
    public void OnLoad(IModContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        _context.Logger.LogInformation("Loading {ModName} v{Version}", Name, Version);
        _context.Logger.LogInformation("Mod directory: {ModDirectory}", context.ModDirectory);

        try
        {
            // Example: Apply Harmony patches
            _harmony = new Harmony(Id);
            _harmony.PatchAll(typeof(ExampleMod).Assembly);

            _context.Logger.LogInformation("Applied Harmony patches");

            // Example: Access game services
            // var eventBus = context.GetService<IEventBus>();
            // if (eventBus != null)
            // {
            //     _context.Logger.LogInformation("Event bus is available");
            // }

            _context.Logger.LogInformation("{ModName} loaded successfully!", Name);
        }
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Failed to load {ModName}", Name);
            throw;
        }
    }

    /// <summary>
    /// Called when the mod is being unloaded. Clean up resources here.
    /// </summary>
    public void OnUnload()
    {
        _context?.Logger.LogInformation("Unloading {ModName}", Name);

        // Remove Harmony patches
        _harmony?.UnpatchAll(Id);

        _context?.Logger.LogInformation("{ModName} unloaded", Name);
    }
}

/// <summary>
/// Example Harmony patch demonstrating how to modify game behavior.
/// This is a simple example that logs when the PokeNETGame is initialized.
/// </summary>
[HarmonyPatch]
public static class GameInitializationPatch
{
    /// <summary>
    /// Specifies which method to patch. This targets the PokeNETGame.Initialize method.
    /// </summary>
    [HarmonyTargetMethod]
    public static System.Reflection.MethodBase TargetMethod()
    {
        // Find the Initialize method on PokeNETGame
        var gameType = AccessTools.TypeByName("PokeNET.Core.PokeNETGame");
        return AccessTools.Method(gameType, "Initialize");
    }

    /// <summary>
    /// Prefix patch - runs BEFORE the original method.
    /// Return false to skip the original method, true to run it.
    /// </summary>
    [HarmonyPrefix]
    public static void Prefix()
    {
        Console.WriteLine("[ExampleMod] Game is initializing (Prefix)");
    }

    /// <summary>
    /// Postfix patch - runs AFTER the original method.
    /// Can access and modify the return value.
    /// </summary>
    [HarmonyPostfix]
    public static void Postfix()
    {
        Console.WriteLine("[ExampleMod] Game initialized successfully (Postfix)");
    }
}
