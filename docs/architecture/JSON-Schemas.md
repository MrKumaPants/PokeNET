# JSON Data Schema Examples

**Version:** 1.0
**Date:** 2025-10-26

This document provides complete JSON schema examples for all PokeNET data files.

---

## species.json

**File location:** `/Data/species.json`
**Array of:** SpeciesData objects

```json
[
  {
    "id": 1,
    "name": "Bulbasaur",
    "types": ["Grass", "Poison"],
    "baseStats": {
      "hp": 45,
      "attack": 49,
      "defense": 49,
      "specialAttack": 65,
      "specialDefense": 65,
      "speed": 45
    },
    "abilities": ["Overgrow"],
    "hiddenAbility": "Chlorophyll",
    "growthRate": "Medium Slow",
    "baseExperience": 64,
    "genderRatio": 31,
    "catchRate": 45,
    "baseFriendship": 70,
    "eggGroups": ["Monster", "Grass"],
    "hatchSteps": 5120,
    "height": 7,
    "weight": 69,
    "description": "A strange seed was planted on its back at birth. The plant sprouts and grows with this Pokémon.",
    "levelMoves": [
      { "level": 1, "moveName": "Tackle" },
      { "level": 3, "moveName": "Growl" },
      { "level": 7, "moveName": "Vine Whip" },
      { "level": 9, "moveName": "Poison Powder" },
      { "level": 13, "moveName": "Razor Leaf" },
      { "level": 15, "moveName": "Sleep Powder" },
      { "level": 19, "moveName": "Take Down" },
      { "level": 21, "moveName": "Sweet Scent" },
      { "level": 25, "moveName": "Growth" },
      { "level": 27, "moveName": "Double-Edge" },
      { "level": 31, "moveName": "Worry Seed" },
      { "level": 33, "moveName": "Synthesis" },
      { "level": 37, "moveName": "Seed Bomb" }
    ],
    "tmMoves": [
      "Toxic", "Venoshock", "Hidden Power", "Sunny Day", "Light Screen",
      "Protect", "Frustration", "Solar Beam", "Return", "Double Team",
      "Sludge Bomb", "Facade", "Rest", "Attract", "Round", "Energy Ball",
      "Grass Knot", "Swagger", "Sleep Talk", "Substitute", "Nature Power",
      "Confide"
    ],
    "eggMoves": [
      "Skull Bash", "Amnesia", "Curse", "Ingrain", "Nature Power",
      "Power Whip", "Leaf Storm", "Grassy Terrain", "Sludge"
    ],
    "evolutions": [
      {
        "targetSpeciesId": 2,
        "method": "Level",
        "requiredLevel": 16
      }
    ]
  },
  {
    "id": 25,
    "name": "Pikachu",
    "types": ["Electric"],
    "baseStats": {
      "hp": 35,
      "attack": 55,
      "defense": 40,
      "specialAttack": 50,
      "specialDefense": 50,
      "speed": 90
    },
    "abilities": ["Static"],
    "hiddenAbility": "Lightning Rod",
    "growthRate": "Medium Fast",
    "baseExperience": 112,
    "genderRatio": 127,
    "catchRate": 190,
    "baseFriendship": 70,
    "eggGroups": ["Field", "Fairy"],
    "hatchSteps": 2560,
    "height": 4,
    "weight": 60,
    "description": "When several of these Pokémon gather, their electricity can build and cause lightning storms.",
    "levelMoves": [
      { "level": 1, "moveName": "Thunder Shock" },
      { "level": 5, "moveName": "Tail Whip" },
      { "level": 10, "moveName": "Quick Attack" },
      { "level": 13, "moveName": "Thunder Wave" },
      { "level": 18, "moveName": "Electro Ball" },
      { "level": 21, "moveName": "Feint" },
      { "level": 26, "moveName": "Spark" },
      { "level": 29, "moveName": "Slam" },
      { "level": 34, "moveName": "Thunderbolt" },
      { "level": 37, "moveName": "Agility" },
      { "level": 42, "moveName": "Wild Charge" },
      { "level": 45, "moveName": "Light Screen" },
      { "level": 50, "moveName": "Thunder" }
    ],
    "tmMoves": [
      "Toxic", "Hidden Power", "Light Screen", "Protect", "Rain Dance",
      "Frustration", "Thunderbolt", "Return", "Brick Break", "Double Team",
      "Facade", "Rest", "Attract", "Round", "Charge Beam", "Thunder Wave",
      "Wild Charge", "Swagger", "Sleep Talk", "Substitute", "Confide"
    ],
    "eggMoves": [
      "Volt Tackle", "Reversal", "Wish", "Encore", "Present", "Fake Out",
      "Tickle", "Charge", "Lucky Chant", "Thunder Punch", "Disarming Voice"
    ],
    "evolutions": [
      {
        "targetSpeciesId": 26,
        "method": "Stone",
        "requiredItem": "Thunder Stone"
      }
    ]
  },
  {
    "id": 130,
    "name": "Gyarados",
    "types": ["Water", "Flying"],
    "baseStats": {
      "hp": 95,
      "attack": 125,
      "defense": 79,
      "specialAttack": 60,
      "specialDefense": 100,
      "speed": 81
    },
    "abilities": ["Intimidate"],
    "hiddenAbility": "Moxie",
    "growthRate": "Slow",
    "baseExperience": 189,
    "genderRatio": 127,
    "catchRate": 45,
    "baseFriendship": 70,
    "eggGroups": ["Water 2", "Dragon"],
    "hatchSteps": 5120,
    "height": 65,
    "weight": 2350,
    "description": "Once it begins to rampage, a Gyarados will burn everything down, even in a harsh storm.",
    "levelMoves": [
      { "level": 1, "moveName": "Thrash" },
      { "level": 1, "moveName": "Bite" },
      { "level": 4, "moveName": "Dragon Rage" },
      { "level": 8, "moveName": "Leer" },
      { "level": 11, "moveName": "Twister" },
      { "level": 15, "moveName": "Ice Fang" },
      { "level": 18, "moveName": "Aqua Tail" },
      { "level": 21, "moveName": "Rain Dance" },
      { "level": 24, "moveName": "Crunch" },
      { "level": 27, "moveName": "Waterfall" },
      { "level": 30, "moveName": "Dragon Dance" },
      { "level": 33, "moveName": "Hydro Pump" },
      { "level": 36, "moveName": "Hurricane" },
      { "level": 39, "moveName": "Hyper Beam" }
    ],
    "tmMoves": [
      "Toxic", "Hail", "Hidden Power", "Ice Beam", "Blizzard", "Hyper Beam",
      "Protect", "Rain Dance", "Frustration", "Earthquake", "Return",
      "Double Team", "Flamethrower", "Sandstorm", "Fire Blast", "Facade",
      "Rest", "Attract", "Round", "Scald", "Bulldoze", "Dragon Tail",
      "Swagger", "Sleep Talk", "Substitute", "Surf", "Waterfall", "Confide"
    ],
    "eggMoves": [],
    "evolutions": []
  }
]
```

---

## moves.json

**File location:** `/Data/moves.json`
**Array of:** MoveData objects

```json
[
  {
    "name": "Tackle",
    "type": "Normal",
    "category": "Physical",
    "power": 40,
    "accuracy": 100,
    "pp": 35,
    "priority": 0,
    "target": "SingleTarget",
    "effectChance": 0,
    "description": "A physical attack in which the user charges and slams into the target with its whole body.",
    "flags": ["Contact", "Protect", "Mirror"],
    "effectScript": null,
    "effectParameters": null
  },
  {
    "name": "Thunderbolt",
    "type": "Electric",
    "category": "Special",
    "power": 90,
    "accuracy": 100,
    "pp": 15,
    "priority": 0,
    "target": "SingleTarget",
    "effectChance": 10,
    "description": "A strong electric blast that may paralyze the target.",
    "flags": ["Protect", "Mirror"],
    "effectScript": "scripts/moves/paralysis.csx",
    "effectParameters": {
      "statusCondition": "Paralysis",
      "chance": 10
    }
  },
  {
    "name": "Flamethrower",
    "type": "Fire",
    "category": "Special",
    "power": 90,
    "accuracy": 100,
    "pp": 15,
    "priority": 0,
    "target": "SingleTarget",
    "effectChance": 10,
    "description": "The target is scorched with an intense blast of fire. This may also leave the target with a burn.",
    "flags": ["Protect", "Mirror"],
    "effectScript": "scripts/moves/burn.csx",
    "effectParameters": {
      "statusCondition": "Burn",
      "chance": 10
    }
  },
  {
    "name": "Hydro Pump",
    "type": "Water",
    "category": "Special",
    "power": 110,
    "accuracy": 80,
    "pp": 5,
    "priority": 0,
    "target": "SingleTarget",
    "effectChance": 0,
    "description": "The target is blasted by a huge volume of water launched under great pressure.",
    "flags": ["Protect", "Mirror"],
    "effectScript": null,
    "effectParameters": null
  },
  {
    "name": "Quick Attack",
    "type": "Normal",
    "category": "Physical",
    "power": 40,
    "accuracy": 100,
    "pp": 30,
    "priority": 1,
    "target": "SingleTarget",
    "effectChance": 0,
    "description": "The user lunges at the target at a speed that makes it almost invisible. This move always goes first.",
    "flags": ["Contact", "Protect", "Mirror"],
    "effectScript": null,
    "effectParameters": null
  },
  {
    "name": "Protect",
    "type": "Normal",
    "category": "Status",
    "power": 0,
    "accuracy": 0,
    "pp": 10,
    "priority": 4,
    "target": "User",
    "effectChance": 0,
    "description": "This move enables the user to protect itself from all attacks. Its chance of failing rises if it is used in succession.",
    "flags": [],
    "effectScript": "scripts/moves/protect.csx",
    "effectParameters": {
      "protectTurns": 1
    }
  },
  {
    "name": "Dragon Dance",
    "type": "Dragon",
    "category": "Status",
    "power": 0,
    "accuracy": 0,
    "pp": 20,
    "priority": 0,
    "target": "User",
    "effectChance": 0,
    "description": "The user vigorously performs a mystic, powerful dance that raises its Attack and Speed stats.",
    "flags": ["Dance"],
    "effectScript": "scripts/moves/stat_boost.csx",
    "effectParameters": {
      "attack": 1,
      "speed": 1
    }
  },
  {
    "name": "Earthquake",
    "type": "Ground",
    "category": "Physical",
    "power": 100,
    "accuracy": 100,
    "pp": 10,
    "priority": 0,
    "target": "AllAdjacentFoes",
    "effectChance": 0,
    "description": "The user sets off an earthquake that strikes every Pokémon around it.",
    "flags": ["Protect", "Mirror"],
    "effectScript": null,
    "effectParameters": null
  }
]
```

---

## items.json

**File location:** `/Data/items.json`
**Array of:** ItemData objects

```json
[
  {
    "id": 17,
    "name": "Potion",
    "category": "Medicine",
    "buyPrice": 200,
    "sellPrice": 100,
    "description": "A spray-type medicine for treating wounds. It restores 20 HP to an injured Pokémon.",
    "consumable": true,
    "usableInBattle": true,
    "usableOutsideBattle": true,
    "holdable": false,
    "effectScript": "scripts/items/heal_hp.csx",
    "effectParameters": {
      "healAmount": 20
    },
    "spritePath": "sprites/items/potion.png"
  },
  {
    "id": 18,
    "name": "Super Potion",
    "category": "Medicine",
    "buyPrice": 700,
    "sellPrice": 350,
    "description": "A spray-type medicine for treating wounds. It restores 60 HP to an injured Pokémon.",
    "consumable": true,
    "usableInBattle": true,
    "usableOutsideBattle": true,
    "holdable": false,
    "effectScript": "scripts/items/heal_hp.csx",
    "effectParameters": {
      "healAmount": 60
    },
    "spritePath": "sprites/items/super_potion.png"
  },
  {
    "id": 4,
    "name": "Poke Ball",
    "category": "Pokeball",
    "buyPrice": 200,
    "sellPrice": 100,
    "description": "A device for catching wild Pokémon. It's thrown like a ball at a Pokémon, comfortably encapsulating its target.",
    "consumable": true,
    "usableInBattle": true,
    "usableOutsideBattle": false,
    "holdable": false,
    "effectScript": "scripts/items/pokeball.csx",
    "effectParameters": {
      "catchRateMultiplier": 1.0
    },
    "spritePath": "sprites/items/pokeball.png"
  },
  {
    "id": 3,
    "name": "Ultra Ball",
    "category": "Pokeball",
    "buyPrice": 1200,
    "sellPrice": 600,
    "description": "An ultra-high-performance Poké Ball that provides a higher success rate for catching Pokémon than a Great Ball.",
    "consumable": true,
    "usableInBattle": true,
    "usableOutsideBattle": false,
    "holdable": false,
    "effectScript": "scripts/items/pokeball.csx",
    "effectParameters": {
      "catchRateMultiplier": 2.0
    },
    "spritePath": "sprites/items/ultraball.png"
  },
  {
    "id": 84,
    "name": "Thunder Stone",
    "category": "EvolutionItem",
    "buyPrice": 2100,
    "sellPrice": 1050,
    "description": "A peculiar stone that can make certain species of Pokémon evolve. It has a distinct thunderbolt pattern.",
    "consumable": true,
    "usableInBattle": false,
    "usableOutsideBattle": true,
    "holdable": false,
    "effectScript": "scripts/items/evolution_stone.csx",
    "effectParameters": {
      "evolutionType": "ThunderStone"
    },
    "spritePath": "sprites/items/thunderstone.png"
  },
  {
    "id": 220,
    "name": "Leftovers",
    "category": "HeldItem",
    "buyPrice": 0,
    "sellPrice": 100,
    "description": "An item to be held by a Pokémon. The holder's HP is slowly but steadily restored throughout every battle.",
    "consumable": false,
    "usableInBattle": false,
    "usableOutsideBattle": false,
    "holdable": true,
    "effectScript": "scripts/items/leftovers.csx",
    "effectParameters": {
      "healPercentPerTurn": 6.25
    },
    "spritePath": "sprites/items/leftovers.png"
  },
  {
    "id": 328,
    "name": "TM24",
    "category": "TM",
    "buyPrice": 0,
    "sellPrice": 500,
    "description": "The user strikes everything around it by swamping the area with a giant sludge wave. This may also poison those hit. (Thunderbolt)",
    "consumable": false,
    "usableInBattle": false,
    "usableOutsideBattle": true,
    "holdable": false,
    "effectScript": "scripts/items/teach_move.csx",
    "effectParameters": {
      "moveName": "Thunderbolt"
    },
    "spritePath": "sprites/items/tm_electric.png"
  },
  {
    "id": 465,
    "name": "Bike Voucher",
    "category": "KeyItem",
    "buyPrice": 0,
    "sellPrice": 0,
    "description": "A voucher for obtaining a bicycle from the Bike Shop in Cerulean City.",
    "consumable": false,
    "usableInBattle": false,
    "usableOutsideBattle": false,
    "holdable": false,
    "effectScript": null,
    "effectParameters": null,
    "spritePath": "sprites/items/key_item.png"
  }
]
```

---

## encounters.json

**File location:** `/Data/encounters.json`
**Array of:** EncounterTable objects

```json
[
  {
    "locationId": "route_1",
    "locationName": "Route 1",
    "grassEncounters": [
      {
        "speciesId": 16,
        "minLevel": 2,
        "maxLevel": 4,
        "rate": 40,
        "timeOfDay": "Any",
        "weather": "Any"
      },
      {
        "speciesId": 19,
        "minLevel": 2,
        "maxLevel": 3,
        "rate": 35,
        "timeOfDay": "Any",
        "weather": "Any"
      },
      {
        "speciesId": 16,
        "minLevel": 3,
        "maxLevel": 5,
        "rate": 25,
        "timeOfDay": "Any",
        "weather": "Any"
      }
    ],
    "waterEncounters": [],
    "oldRodEncounters": [],
    "goodRodEncounters": [],
    "superRodEncounters": [],
    "caveEncounters": [],
    "specialEncounters": []
  },
  {
    "locationId": "viridian_forest",
    "locationName": "Viridian Forest",
    "grassEncounters": [
      {
        "speciesId": 10,
        "minLevel": 3,
        "maxLevel": 5,
        "rate": 40,
        "timeOfDay": "Any",
        "weather": "Any"
      },
      {
        "speciesId": 13,
        "minLevel": 3,
        "maxLevel": 5,
        "rate": 35,
        "timeOfDay": "Any",
        "weather": "Any"
      },
      {
        "speciesId": 25,
        "minLevel": 3,
        "maxLevel": 5,
        "rate": 5,
        "timeOfDay": "Any",
        "weather": "Any"
      }
    ],
    "waterEncounters": [],
    "oldRodEncounters": [],
    "goodRodEncounters": [],
    "superRodEncounters": [],
    "caveEncounters": [],
    "specialEncounters": [
      {
        "encounterId": "rare_pikachu",
        "speciesId": 25,
        "level": 5,
        "oneTime": false,
        "conditions": {
          "weather": "Thunderstorm"
        },
        "script": null
      }
    ]
  },
  {
    "locationId": "route_22",
    "locationName": "Route 22",
    "grassEncounters": [
      {
        "speciesId": 19,
        "minLevel": 2,
        "maxLevel": 4,
        "rate": 40,
        "timeOfDay": "Any",
        "weather": "Any"
      },
      {
        "speciesId": 21,
        "minLevel": 2,
        "maxLevel": 5,
        "rate": 35,
        "timeOfDay": "Any",
        "weather": "Any"
      },
      {
        "speciesId": 56,
        "minLevel": 5,
        "maxLevel": 7,
        "rate": 25,
        "timeOfDay": "Any",
        "weather": "Any"
      }
    ],
    "waterEncounters": [
      {
        "speciesId": 60,
        "minLevel": 15,
        "maxLevel": 25,
        "rate": 70,
        "timeOfDay": "Any",
        "weather": "Any"
      },
      {
        "speciesId": 118,
        "minLevel": 10,
        "maxLevel": 20,
        "rate": 30,
        "timeOfDay": "Any",
        "weather": "Any"
      }
    ],
    "oldRodEncounters": [
      {
        "speciesId": 129,
        "minLevel": 5,
        "maxLevel": 10,
        "rate": 100,
        "timeOfDay": "Any",
        "weather": "Any"
      }
    ],
    "goodRodEncounters": [
      {
        "speciesId": 60,
        "minLevel": 10,
        "maxLevel": 20,
        "rate": 60,
        "timeOfDay": "Any",
        "weather": "Any"
      },
      {
        "speciesId": 129,
        "minLevel": 10,
        "maxLevel": 15,
        "rate": 40,
        "timeOfDay": "Any",
        "weather": "Any"
      }
    ],
    "superRodEncounters": [
      {
        "speciesId": 60,
        "minLevel": 20,
        "maxLevel": 30,
        "rate": 60,
        "timeOfDay": "Any",
        "weather": "Any"
      },
      {
        "speciesId": 129,
        "minLevel": 15,
        "maxLevel": 25,
        "rate": 40,
        "timeOfDay": "Any",
        "weather": "Any"
      }
    ],
    "caveEncounters": [],
    "specialEncounters": []
  },
  {
    "locationId": "cerulean_cave",
    "locationName": "Cerulean Cave",
    "grassEncounters": [],
    "waterEncounters": [],
    "oldRodEncounters": [],
    "goodRodEncounters": [],
    "superRodEncounters": [],
    "caveEncounters": [
      {
        "speciesId": 42,
        "minLevel": 50,
        "maxLevel": 55,
        "rate": 30,
        "timeOfDay": "Any",
        "weather": "Any"
      },
      {
        "speciesId": 49,
        "minLevel": 50,
        "maxLevel": 55,
        "rate": 25,
        "timeOfDay": "Any",
        "weather": "Any"
      },
      {
        "speciesId": 82,
        "minLevel": 50,
        "maxLevel": 55,
        "rate": 25,
        "timeOfDay": "Any",
        "weather": "Any"
      },
      {
        "speciesId": 101,
        "minLevel": 52,
        "maxLevel": 56,
        "rate": 20,
        "timeOfDay": "Any",
        "weather": "Any"
      }
    ],
    "specialEncounters": [
      {
        "encounterId": "mewtwo_encounter",
        "speciesId": 150,
        "level": 70,
        "oneTime": true,
        "conditions": {
          "badge_count": 8,
          "champion_defeated": true
        },
        "script": "scripts/encounters/mewtwo.csx"
      }
    ]
  }
]
```

---

## typechart.json (Optional - Loaded into TypeChart class)

**File location:** `/Data/typechart.json`
**Structure:** Type effectiveness mappings

**Note:** TypeChart can be initialized programmatically (as in current implementation) or loaded from JSON for mod support.

```json
{
  "version": "gen6+",
  "effectiveness": {
    "Normal": {
      "Rock": 0.5,
      "Steel": 0.5,
      "Ghost": 0.0
    },
    "Fire": {
      "Fire": 0.5,
      "Water": 0.5,
      "Grass": 2.0,
      "Ice": 2.0,
      "Bug": 2.0,
      "Rock": 0.5,
      "Dragon": 0.5,
      "Steel": 2.0
    },
    "Water": {
      "Fire": 2.0,
      "Water": 0.5,
      "Grass": 0.5,
      "Ground": 2.0,
      "Rock": 2.0,
      "Dragon": 0.5
    },
    "Electric": {
      "Water": 2.0,
      "Electric": 0.5,
      "Grass": 0.5,
      "Ground": 0.0,
      "Flying": 2.0,
      "Dragon": 0.5
    },
    "Grass": {
      "Fire": 0.5,
      "Water": 2.0,
      "Grass": 0.5,
      "Poison": 0.5,
      "Ground": 2.0,
      "Flying": 0.5,
      "Bug": 0.5,
      "Rock": 2.0,
      "Dragon": 0.5,
      "Steel": 0.5
    },
    "Ice": {
      "Fire": 0.5,
      "Water": 0.5,
      "Grass": 2.0,
      "Ice": 0.5,
      "Ground": 2.0,
      "Flying": 2.0,
      "Dragon": 2.0,
      "Steel": 0.5
    },
    "Fighting": {
      "Normal": 2.0,
      "Ice": 2.0,
      "Poison": 0.5,
      "Flying": 0.5,
      "Psychic": 0.5,
      "Bug": 0.5,
      "Rock": 2.0,
      "Ghost": 0.0,
      "Dark": 2.0,
      "Steel": 2.0,
      "Fairy": 0.5
    },
    "Poison": {
      "Grass": 2.0,
      "Poison": 0.5,
      "Ground": 0.5,
      "Rock": 0.5,
      "Ghost": 0.5,
      "Steel": 0.0,
      "Fairy": 2.0
    },
    "Ground": {
      "Fire": 2.0,
      "Electric": 2.0,
      "Grass": 0.5,
      "Poison": 2.0,
      "Flying": 0.0,
      "Bug": 0.5,
      "Rock": 2.0,
      "Steel": 2.0
    },
    "Flying": {
      "Electric": 0.5,
      "Grass": 2.0,
      "Fighting": 2.0,
      "Bug": 2.0,
      "Rock": 0.5,
      "Steel": 0.5
    },
    "Psychic": {
      "Fighting": 2.0,
      "Poison": 2.0,
      "Psychic": 0.5,
      "Dark": 0.0,
      "Steel": 0.5
    },
    "Bug": {
      "Fire": 0.5,
      "Grass": 2.0,
      "Fighting": 0.5,
      "Poison": 0.5,
      "Flying": 0.5,
      "Psychic": 2.0,
      "Ghost": 0.5,
      "Dark": 2.0,
      "Steel": 0.5,
      "Fairy": 0.5
    },
    "Rock": {
      "Fire": 2.0,
      "Ice": 2.0,
      "Fighting": 0.5,
      "Ground": 0.5,
      "Flying": 2.0,
      "Bug": 2.0,
      "Steel": 0.5
    },
    "Ghost": {
      "Normal": 0.0,
      "Psychic": 2.0,
      "Ghost": 2.0,
      "Dark": 0.5
    },
    "Dragon": {
      "Dragon": 2.0,
      "Steel": 0.5,
      "Fairy": 0.0
    },
    "Dark": {
      "Fighting": 0.5,
      "Psychic": 2.0,
      "Ghost": 2.0,
      "Dark": 0.5,
      "Fairy": 0.5
    },
    "Steel": {
      "Fire": 0.5,
      "Water": 0.5,
      "Electric": 0.5,
      "Ice": 2.0,
      "Rock": 2.0,
      "Steel": 0.5,
      "Fairy": 2.0
    },
    "Fairy": {
      "Fire": 0.5,
      "Fighting": 2.0,
      "Poison": 0.5,
      "Dragon": 2.0,
      "Dark": 2.0,
      "Steel": 0.5
    }
  }
}
```

---

## Validation Rules

### species.json
- `id` must be unique positive integer
- `name` must be non-empty string
- `types` array must have 1-2 elements, each valid PokemonType
- `baseStats` values must be 1-255
- `genderRatio` must be -1 or 0-254
- `catchRate` must be 0-255
- `levelMoves` must be sorted by level ascending
- `evolutions[].targetSpeciesId` must reference valid species

### moves.json
- `name` must be unique non-empty string
- `type` must be valid PokemonType
- `power` must be 0-250 (0 for status moves)
- `accuracy` must be 0-100 (0 for "never miss")
- `pp` must be 1-40
- `priority` must be -7 to +5
- `category` must be Physical, Special, or Status

### items.json
- `id` must be unique positive integer
- `name` must be non-empty string
- `buyPrice` and `sellPrice` must be non-negative
- `category` must be valid ItemCategory enum value
- If `consumable` is false, `usableInBattle` and `usableOutsideBattle` must both be false

### encounters.json
- `locationId` must be unique non-empty string
- `speciesId` must reference valid species in species.json
- `minLevel` ≤ `maxLevel`, both 1-100
- `rate` must be 0-100
- Sum of rates in each encounter list should equal 100 (encounter chance)
- `specialEncounters[].encounterId` must be globally unique

---

**Next Steps:**
1. Create sample data files with 10 species, 20 moves, 10 items, 5 encounter tables
2. Implement JSON validation tool
3. Write unit tests for data deserialization
4. Document mod override examples
