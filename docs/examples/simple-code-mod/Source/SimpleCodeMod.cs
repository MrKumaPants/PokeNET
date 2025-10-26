using System;
using HarmonyLib;
using PokeNET.ModApi;

namespace SimpleCodeMod
{
    /// <summary>
    /// Main mod class implementing IMod interface.
    /// </summary>
    public class SimpleCodeMod : IMod
    {
        // Mod identification
        public string Id => "com.example.simplecodemod";
        public string Name => "Simple Code Mod";
        public Version Version => new Version(1, 0, 0);

        // Harmony instance for patching
        private Harmony _harmony;

        // Store context for later use
        private IModContext _context;

        /// <summary>
        /// Called when mod is loaded by PokeNET.
        /// </summary>
        public void OnLoad(IModContext context)
        {
            _context = context;

            try
            {
                // Create Harmony instance with unique ID
                _harmony = new Harmony(Id);

                // Apply all patches in this assembly
                _harmony.PatchAll();

                // Subscribe to game events (ISP-compliant: only depend on battle events)
                // NEW: Use focused event APIs instead of context.Events
                context.BattleEvents.OnBattleStart += OnBattleStart;
                context.BattleEvents.OnDamageCalculated += OnDamageDealt;

                context.Logger.Info($"{Name} v{Version} loaded successfully!");
                context.Logger.Info("All Harmony patches applied.");
            }
            catch (Exception ex)
            {
                context.Logger.Error($"Failed to load {Name}", ex);
            }
        }

        /// <summary>
        /// Called when mod is unloaded.
        /// </summary>
        public void OnUnload()
        {
            try
            {
                // Remove all Harmony patches
                _harmony?.UnpatchAll(Id);

                // Unsubscribe from events
                if (_context != null)
                {
                    _context.BattleEvents.OnBattleStart -= OnBattleStart;
                    _context.BattleEvents.OnDamageCalculated -= OnDamageDealt;
                }

                _context?.Logger.Info($"{Name} unloaded successfully.");
            }
            catch (Exception ex)
            {
                _context?.Logger.Error($"Error unloading {Name}", ex);
            }
        }

        /// <summary>
        /// Event handler for battle start.
        /// </summary>
        private void OnBattleStart(object sender, BattleEventArgs e)
        {
            _context.Logger.Info(
                $"Battle started: {e.PlayerCreature.Name} vs {e.OpponentCreature.Name}"
            );
        }

        /// <summary>
        /// Event handler for damage dealt.
        /// </summary>
        private void OnDamageDealt(object sender, DamageEventArgs e)
        {
            _context.Logger.Info($"{e.Attacker.Name} dealt {e.Damage} damage to {e.Defender.Name}");

            if (e.IsCritical)
            {
                _context.Logger.Info("Critical hit!");
            }
        }
    }
}
