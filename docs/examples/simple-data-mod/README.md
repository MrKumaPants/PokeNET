# Simple Data Mod Example

This example demonstrates how to create a basic data mod that adds new creatures to PokeNET.

## What This Mod Does

Adds three new fire-type creatures to the game:
- **Flame Lizard** (starter form)
- **Flame Dragon** (evolution)
- **Mega Flame Dragon** (final evolution with special item)

## File Structure

```
simple-data-mod/
├── README.md              # This file
├── modinfo.json           # Mod manifest
└── Data/
    └── creatures.json     # Creature definitions
```

## Step-by-Step Guide

### Step 1: Create Mod Directory

Create a new folder in your `Mods/` directory:

```
Mods/
└── SimpleDataMod/
```

### Step 2: Create modinfo.json

This file tells PokeNET about your mod. Create `modinfo.json` with the following content:

```json
{
  "id": "com.example.simpledatamod",
  "name": "Simple Data Mod",
  "version": "1.0.0",
  "author": "YourName",
  "description": "Adds three new fire-type creatures to the game",
  "dependencies": [
    {
      "id": "com.pokenet.core",
      "version": ">=1.0.0"
    }
  ]
}
```

**Explanation:**
- **id**: Unique identifier (use reverse domain notation)
- **name**: Display name in mod manager
- **version**: Mod version (MAJOR.MINOR.PATCH)
- **author**: Your name
- **description**: What your mod does
- **dependencies**: Required mods and their versions

### Step 3: Create Data Directory

Create a `Data/` folder inside your mod:

```
Mods/SimpleDataMod/Data/
```

### Step 4: Create creatures.json

Create `Data/creatures.json` with your creature definitions:

```json
{
  "creatures": [
    {
      "id": "flame_lizard",
      "name": "Flame Lizard",
      "type": ["fire"],
      "baseStats": {
        "hp": 39,
        "attack": 52,
        "defense": 43,
        "specialAttack": 60,
        "specialDefense": 50,
        "speed": 65
      },
      "abilities": ["blaze"],
      "learnset": [
        {"level": 1, "move": "scratch"},
        {"level": 1, "move": "ember"},
        {"level": 7, "move": "smokescreen"},
        {"level": 10, "move": "dragon_rage"},
        {"level": 17, "move": "scary_face"},
        {"level": 22, "move": "fire_fang"},
        {"level": 28, "move": "slash"},
        {"level": 34, "move": "flamethrower"}
      ],
      "evolutionChain": [
        {"to": "flame_dragon", "level": 16}
      ],
      "spriteId": "flame_lizard_sprite",
      "description": "A small lizard with a flame on its tail. The flame indicates its mood and health.",
      "catchRate": 0.45,
      "baseExperience": 62,
      "growthRate": "medium_slow"
    },
    {
      "id": "flame_dragon",
      "name": "Flame Dragon",
      "type": ["fire", "dragon"],
      "baseStats": {
        "hp": 58,
        "attack": 64,
        "defense": 58,
        "specialAttack": 80,
        "specialDefense": 65,
        "speed": 80
      },
      "abilities": ["blaze"],
      "learnset": [
        {"level": 1, "move": "scratch"},
        {"level": 1, "move": "ember"},
        {"level": 1, "move": "smokescreen"},
        {"level": 1, "move": "dragon_rage"},
        {"level": 17, "move": "scary_face"},
        {"level": 22, "move": "fire_fang"},
        {"level": 28, "move": "slash"},
        {"level": 34, "move": "flamethrower"},
        {"level": 36, "move": "dragon_claw"},
        {"level": 42, "move": "fire_spin"},
        {"level": 50, "move": "dragon_pulse"}
      ],
      "evolutionChain": [
        {"from": "flame_lizard", "level": 16},
        {"to": "mega_flame_dragon", "item": "dragon_stone"}
      ],
      "spriteId": "flame_dragon_sprite",
      "description": "Wings have grown on its back, and its flames burn even hotter. It can breathe powerful fire.",
      "catchRate": 0.25,
      "baseExperience": 142,
      "growthRate": "medium_slow"
    },
    {
      "id": "mega_flame_dragon",
      "name": "Mega Flame Dragon",
      "type": ["fire", "dragon"],
      "baseStats": {
        "hp": 78,
        "attack": 84,
        "defense": 78,
        "specialAttack": 109,
        "specialDefense": 85,
        "speed": 100
      },
      "abilities": ["blaze", "solar_power"],
      "learnset": [
        {"level": 1, "move": "dragon_dance"},
        {"level": 1, "move": "flare_blitz"},
        {"level": 1, "move": "dragon_pulse"},
        {"level": 1, "move": "air_slash"}
      ],
      "evolutionChain": [
        {"from": "flame_dragon", "item": "dragon_stone"}
      ],
      "spriteId": "mega_flame_dragon_sprite",
      "description": "The ultimate form of the flame dragon. Its power is legendary, and its flames can melt steel.",
      "catchRate": 0.05,
      "baseExperience": 240,
      "growthRate": "medium_slow"
    }
  ]
}
```

**Explanation:**

Each creature has these properties:
- **id**: Unique identifier
- **name**: Display name
- **type**: Array of types (fire, water, grass, etc.)
- **baseStats**: Six core stats
- **abilities**: Array of ability IDs
- **learnset**: Moves learned at specific levels
- **evolutionChain**: How this creature evolves
- **spriteId**: Reference to sprite asset
- **description**: Pokedex entry
- **catchRate**: Probability of capture (0-1)
- **baseExperience**: EXP gained when defeated
- **growthRate**: How fast it levels up

### Step 5: Test Your Mod

1. Copy your `SimpleDataMod/` folder to `Mods/`
2. Launch PokeNET
3. Open the Mod Manager (should show "Simple Data Mod")
4. Check the log for any errors
5. Start a new game and verify your creatures appear

## Customization Ideas

### Add More Creatures

Add more creature objects to the `creatures` array:

```json
{
  "creatures": [
    {
      "id": "flame_lizard",
      ...
    },
    {
      "id": "your_new_creature",
      "name": "Your New Creature",
      ...
    }
  ]
}
```

### Change Stats

Modify the `baseStats` section to make creatures stronger/weaker:

```json
"baseStats": {
  "hp": 100,        // Increase HP
  "attack": 150,    // Increase attack
  "defense": 80,
  "specialAttack": 120,
  "specialDefense": 90,
  "speed": 110
}
```

### Add More Moves

Extend the `learnset` array:

```json
"learnset": [
  {"level": 1, "move": "scratch"},
  {"level": 45, "move": "your_custom_move"}
]
```

### Different Evolution Methods

```json
"evolutionChain": [
  {"to": "next_form", "level": 30},           // Level-based
  {"to": "item_form", "item": "fire_stone"},  // Item-based
  {"to": "trade_form", "condition": "trade"}  // Trade-based
]
```

## Common Issues

### Mod Not Loading

**Problem**: Mod doesn't appear in mod manager

**Solution**:
1. Check `modinfo.json` is in the correct location
2. Validate JSON syntax (use jsonlint.com)
3. Ensure all required fields are present

### Creatures Not Appearing

**Problem**: Mod loads but creatures don't appear in-game

**Solution**:
1. Check `Data/creatures.json` syntax
2. Verify file is in `Data/` folder
3. Check logs for parsing errors
4. Ensure creature IDs are unique

### Evolution Not Working

**Problem**: Creature doesn't evolve at specified level

**Solution**:
1. Check `evolutionChain` has both `from` and `to`
2. Verify target creature exists
3. Check level is correct
4. For item evolution, ensure item exists in game

## Next Steps

Once you've mastered data mods, try:

1. **Simple Content Mod** - Add custom sprites for your creatures
2. **Simple Code Mod** - Use Harmony to modify game behavior
3. **Complex Data Mod** - Add items, abilities, and moves

## Resources

- **Modding Guide**: `/docs/modding/phase4-modding-guide.md`
- **API Reference**: `/docs/api/modapi-phase4.md`
- **More Examples**: `/docs/examples/`
