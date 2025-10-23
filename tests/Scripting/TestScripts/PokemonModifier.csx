// @name: Pokemon Stat Modifier
// @version: 1.0.0
// @description: Modifies Pokemon stats for testing

// Expects: pokemon (Pokemon object)
// Returns: Modified Pokemon

if (pokemon == null)
{
    throw new ArgumentNullException("pokemon");
}

// Double all stats
pokemon.Stats.Attack *= 2;
pokemon.Stats.Defense *= 2;
pokemon.Stats.Speed *= 2;
pokemon.Stats.HP *= 2;

return pokemon;
