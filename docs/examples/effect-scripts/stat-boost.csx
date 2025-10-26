// Example Move Effect Script: Stat Change
// Changes a stat by specified stages (stat and amount from parameters)
// This same script is reused for all stat-changing moves

#r "PokeNET.Domain.dll"
using PokeNET.Domain.Scripting;

public class StatChangeEffect : IMoveEffect
{
    public string Name => "Stat Change";
    public string Description => "Changes a stat stage.";
    public int Priority => 1; // Higher priority than status effects

    public bool CanTrigger(IBattleContext context)
    {
        // Get parameters
        string targetStat = context.GetParameter("stat", "Attack");
        int statChange = context.GetParameter("statChange", 1);
        bool targetSelf = context.GetParameter("targetSelf", true);

        var target = targetSelf ? context.Attacker : context.Defender;

        // Check if we can still boost/lower the stat
        int currentStage = target.GetStatStage(targetStat);

        if (statChange > 0)
        {
            return currentStage < 6; // Can boost up to +6
        }
        else
        {
            return currentStage > -6; // Can lower down to -6
        }
    }

    public async Task ApplyEffectAsync(IBattleContext context)
    {
        // Get parameters from JSON
        string targetStat = context.GetParameter("stat", "Attack");
        int statChange = context.GetParameter("statChange", 1);
        bool targetSelf = context.GetParameter("targetSelf", true);

        var target = targetSelf ? context.Attacker : context.Defender;

        // Apply stat change
        target.BoostStat(targetStat, statChange);

        // Build message
        string statName = targetStat;
        string direction = statChange > 0 ? "rose" : "fell";
        string amount = Math.Abs(statChange) > 1 ? " sharply" : "";

        await context.ShowMessageAsync($"{target.Name}'s {statName}{amount} {direction}!");
    }
}
