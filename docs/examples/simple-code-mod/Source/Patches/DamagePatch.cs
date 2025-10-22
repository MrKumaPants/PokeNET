using HarmonyLib;
using PokeNET.Core.Battle;

namespace SimpleCodeMod.Patches
{
    /// <summary>
    /// Patch to double all damage dealt in battles.
    /// </summary>
    [HarmonyPatch(typeof(BattleSystem), "CalculateDamage")]
    public class DamagePatch
    {
        /// <summary>
        /// Postfix runs AFTER the original method.
        /// We use __result to modify the return value.
        /// </summary>
        /// <param name="__result">The damage value calculated by original method</param>
        static void Postfix(ref int __result)
        {
            // Double the damage
            __result *= 2;
        }
    }
}
