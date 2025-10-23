// @name: Custom Battle Damage Calculation
// @version: 1.0.0
// @description: Custom damage formula for testing

// Expects: attacker, defender, move, api
// Returns: int damage

using System;

if (attacker == null || defender == null || move == null)
{
    return 0;
}

// Custom damage formula for testing
var level = attacker.Level;
var power = move.Power;
var attack = move.Category == "Physical" ? attacker.Stats.Attack : attacker.Stats.SpecialAttack;
var defense = move.Category == "Physical" ? defender.Stats.Defense : defender.Stats.SpecialDefense;

// Basic damage calculation
var baseDamage = ((2 * level / 5 + 2) * power * attack / defense) / 50 + 2;

// Apply type effectiveness using API
var effectiveness = await api.GetTypeEffectivenessAsync(move.Type, defender.Types);
var finalDamage = (int)(baseDamage * effectiveness);

return finalDamage;
