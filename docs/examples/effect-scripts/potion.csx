// Example Item Effect Script: Potion
// Restores HP to the selected Pokemon (amount specified in parameters)
// This same script is reused for Potion (20 HP), Super Potion (50 HP), etc.

#r "PokeNET.Domain.dll"
using PokeNET.Domain.Scripting;

public class PotionEffect : IItemEffect
{
    public string Name => "Restore HP";
    public string Description => "Restores HP to a Pokemon.";

    public bool CanUse(IScriptContext context)
    {
        // Can only use if Pokemon is damaged
        var pokemon = context.SelectedPokemon;
        return pokemon.CurrentHP < pokemon.MaxHP;
    }

    public async Task<bool> UseAsync(IScriptContext context)
    {
        var pokemon = context.SelectedPokemon;

        // Get heal amount from parameters (from JSON)
        int healAmount = context.GetParameter("healAmount", 20);

        // Calculate actual healing (don't overheal)
        int actualHeal = Math.Min(healAmount, pokemon.MaxHP - pokemon.CurrentHP);

        // Apply healing
        pokemon.CurrentHP += actualHeal;

        // Show message to player
        await context.ShowMessageAsync($"{pokemon.Name} restored {actualHeal} HP!");

        // Return true to consume the item
        return true;
    }
}
