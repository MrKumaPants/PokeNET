using System;
using HarmonyLib;
using PokeNET.Core.Battle;

namespace SimpleCodeMod.Patches
{
    /// <summary>
    /// Patch to display custom message on critical hits.
    /// </summary>
    [HarmonyPatch(typeof(BattleSystem), "CheckCriticalHit")]
    public class CriticalPatch
    {
        /// <summary>
        /// Prefix runs BEFORE the original method.
        /// We can access method parameters and skip the original method if needed.
        /// </summary>
        static void Prefix(Creature attacker, Move move)
        {
            Console.WriteLine($"{attacker.Name} is attacking with {move.Name}!");
        }

        /// <summary>
        /// Postfix runs AFTER the original method.
        /// We use __result to check if it was a critical hit.
        /// </summary>
        static void Postfix(bool __result, Creature attacker)
        {
            if (__result)
            {
                Console.WriteLine($"A critical hit from {attacker.Name}!");
            }
        }
    }
}
