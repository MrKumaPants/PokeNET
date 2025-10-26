// Example Item Effect Script: Full Heal
// Cures all status conditions from a Pokemon

#r "PokeNET.Domain.dll"
using PokeNET.Domain.Scripting;

public class FullHealEffect : IItemEffect
{
    public string Name => "Cure Status";
    public string Description => "Cures all status conditions.";

    public bool CanUse(IScriptContext context)
    {
        // Can only use if Pokemon has a status condition
        return context.SelectedPokemon.HasStatus;
    }

    public async Task<bool> UseAsync(IScriptContext context)
    {
        var pokemon = context.SelectedPokemon;
        string? previousStatus = pokemon.StatusCondition;

        // Remove status condition
        pokemon.RemoveStatus();

        // Show message
        await context.ShowMessageAsync($"{pokemon.Name} was cured of {previousStatus}!");

        // Return true to consume the item
        return true;
    }
}
