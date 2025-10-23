// @name: Custom Item Effect
// @version: 1.0.0
// @description: Custom healing item for testing

// Expects: pokemon, item, api
// Returns: ItemApplicationResult

using System;

if (pokemon == null || item == null)
{
    return new ItemApplicationResult
    {
        Success = false,
        Message = "Invalid pokemon or item"
    };
}

// Custom super potion that heals 100 HP
var currentHP = pokemon.Stats.HP;
var maxHP = pokemon.Stats.MaxHP;

if (currentHP >= maxHP)
{
    return new ItemApplicationResult
    {
        Success = false,
        Message = "HP is already full",
        NewHP = currentHP
    };
}

var healAmount = Math.Min(100, maxHP - currentHP);
var newHP = currentHP + healAmount;

pokemon.Stats.HP = newHP;

return new ItemApplicationResult
{
    Success = true,
    Message = $"Restored {healAmount} HP",
    NewHP = newHP,
    HealedAmount = healAmount
};
