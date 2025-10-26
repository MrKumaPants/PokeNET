// Example Move Effect Script: Burn
// Chance to inflict Burn status on the target (chance specified in parameters)
// This same script is reused for different moves with different burn chances

#r "PokeNET.Domain.dll"
using PokeNET.Domain.Scripting;

public class BurnEffect : IMoveEffect
{
    public string Name => "Burn";
    public string Description => "May burn the target.";
    public int Priority => 0;

    public bool CanTrigger(IBattleContext context)
    {
        // Can only burn if target doesn't have a status
        return !context.Defender.HasStatus;
    }

    public async Task ApplyEffectAsync(IBattleContext context)
    {
        // Get burn chance from parameters (from JSON)
        int burnChance = context.GetParameter("burnChance", 10);

        // Check if burn triggers
        if (context.RandomChance(burnChance))
        {
            context.Defender.ApplyStatus("Burn");
            await context.ShowMessageAsync($"{context.Defender.Name} was burned!");
        }
    }
}
