# Complete Example Mod: "Stellar Creatures"

This example mod demonstrates all four mod types working together to add a complete new creature type to PokeNET.

## What This Mod Does

- Adds "Stellar" type creatures (new type)
- Includes a sample creature: "Starlight"
- Custom ability: "Cosmic Shield"
- New move: "Star Burst"
- Procedurally generated battle music
- Custom sprites and sound effects
- Harmony patch for type effectiveness

## Mod Structure

```
StellarCreatures/
├── modinfo.json                    # Mod manifest
├── README.md                       # This file
├── CHANGELOG.md                    # Version history
├── LICENSE                         # MIT License
│
├── About/
│   ├── About.xml                   # Extended metadata
│   └── Preview.png                 # Mod thumbnail
│
├── Defs/                          # Data definitions (Data Mod)
│   ├── Types/
│   │   └── StellarType.json       # New "Stellar" type
│   ├── Creatures/
│   │   └── Starlight.json         # New creature
│   ├── Moves/
│   │   └── StarBurst.json         # New move
│   └── Abilities/
│       └── CosmicShield.json      # New ability
│
├── Textures/                       # Graphics (Content Mod)
│   ├── Creatures/
│   │   ├── Starlight_Front.png
│   │   ├── Starlight_Back.png
│   │   └── Starlight_Icon.png
│   └── UI/
│       └── StellarTypeIcon.png
│
├── Sounds/                         # Audio (Content Mod)
│   ├── Music/
│   │   └── StellarBattle.ogg
│   └── SFX/
│       ├── StarBurst.wav
│       └── CosmicShield.wav
│
├── Scripts/                        # C# Scripts (Script Mod)
│   ├── CosmicShieldEffect.csx     # Ability behavior
│   └── ProceduralStellarMusic.csx # Dynamic music
│
└── Assemblies/                     # Compiled DLLs (Code Mod)
    ├── StellarCreatures.dll        # Main mod assembly
    └── StellarCreatures.pdb        # Debug symbols
```

## Installation

1. Download the latest release from GitHub
2. Extract to `PokeNET/Mods/StellarCreatures/`
3. Launch PokeNET
4. Enable the mod in the mod manager
5. Restart the game

## File-by-File Breakdown

### 1. Mod Manifest (modinfo.json)

```json
{
  "id": "example.stellarcreatures",
  "name": "Stellar Creatures",
  "version": "1.0.0",
  "author": "PokeNET Team",
  "description": "Adds mysterious Stellar-type creatures from the cosmos.",
  "targetGameVersion": "1.0.0",
  "dependencies": [],
  "tags": ["creatures", "types", "content"],
  "homepage": "https://github.com/pokenet/stellar-creatures",
  "license": "MIT"
}
```

### 2. Data Definitions

#### StellarType.json (New Type)

```json
{
  "id": "type_stellar",
  "name": "Stellar",
  "color": "#9D4EDD",
  "effectiveness": {
    "normal": 1.0,
    "fire": 0.5,
    "water": 1.0,
    "electric": 2.0,
    "grass": 1.0,
    "ice": 0.5,
    "fighting": 1.0,
    "poison": 1.0,
    "ground": 0.5,
    "flying": 2.0,
    "psychic": 1.5,
    "bug": 1.0,
    "rock": 0.5,
    "ghost": 2.0,
    "dragon": 1.5,
    "dark": 2.0,
    "steel": 0.5,
    "fairy": 1.0,
    "stellar": 1.0
  }
}
```

#### Starlight.json (New Creature)

```json
{
  "id": "creature_starlight",
  "name": "Starlight",
  "types": ["stellar"],
  "description": "A mysterious creature said to have fallen from the stars. Its body glows with cosmic energy.",
  "baseStats": {
    "hp": 65,
    "attack": 45,
    "defense": 55,
    "spAttack": 85,
    "spDefense": 75,
    "speed": 70
  },
  "abilities": ["ability_cosmic_shield"],
  "hiddenAbility": "ability_levitate",
  "learnset": [
    { "move": "move_tackle", "level": 1 },
    { "move": "move_cosmic_power", "level": 1 },
    { "move": "move_star_burst", "level": 10 },
    { "move": "move_light_screen", "level": 15 },
    { "move": "move_psychic", "level": 25 },
    { "move": "move_cosmic_beam", "level": 35 }
  ],
  "evolutions": [
    {
      "target": "creature_nebula",
      "method": "level",
      "parameter": 30
    }
  ],
  "catchRate": 45,
  "baseExperience": 120,
  "baseFriendship": 70,
  "growthRate": "mediumSlow",
  "eggGroups": ["mineral", "amorphous"],
  "genderRatio": -1,
  "hatchTime": 5120,
  "height": 0.8,
  "weight": 15.5,
  "sprites": {
    "front": "Textures/Creatures/Starlight_Front.png",
    "back": "Textures/Creatures/Starlight_Back.png",
    "icon": "Textures/Creatures/Starlight_Icon.png"
  },
  "cries": {
    "normal": "Sounds/SFX/Starlight_Cry.wav"
  }
}
```

#### StarBurst.json (New Move)

```json
{
  "id": "move_star_burst",
  "name": "Star Burst",
  "type": "stellar",
  "category": "special",
  "power": 80,
  "accuracy": 100,
  "pp": 15,
  "priority": 0,
  "target": "single",
  "description": "The user releases a burst of stellar energy. May lower the target's Sp. Def.",
  "effects": [
    {
      "type": "damage",
      "calculation": "standard"
    },
    {
      "type": "statChange",
      "stat": "spDefense",
      "stages": -1,
      "chance": 30,
      "target": "opponent"
    }
  ],
  "flags": ["protect", "mirror"],
  "animation": "stellar_burst",
  "sound": "Sounds/SFX/StarBurst.wav"
}
```

#### CosmicShield.json (New Ability)

```json
{
  "id": "ability_cosmic_shield",
  "name": "Cosmic Shield",
  "description": "Reduces damage from super-effective moves by 25%.",
  "script": "Scripts/CosmicShieldEffect.csx"
}
```

### 3. Scripts (Script Mods)

#### CosmicShieldEffect.csx

```csharp
using PokeNET.ModApi;
using PokeNET.ModApi.Events;

/// <summary>
/// Cosmic Shield ability effect
/// Reduces super-effective damage by 25%
/// </summary>
public class CosmicShieldEffect
{
    private readonly IScriptApi _api;

    public CosmicShieldEffect(IScriptApi api)
    {
        _api = api;

        // Subscribe to damage calculation event
        _api.Events.Subscribe<DamageCalculationEvent>(OnDamageCalculation);
    }

    private void OnDamageCalculation(DamageCalculationEvent evt)
    {
        // Check if defender has Cosmic Shield ability
        if (!evt.Defender.HasAbility("ability_cosmic_shield"))
            return;

        // Check if move is super-effective
        if (evt.Effectiveness <= 1.0f)
            return;

        // Log the damage reduction
        _api.Logger.LogDebug($"Cosmic Shield activated! " +
            $"Reducing damage from {evt.Damage} to {evt.Damage * 0.75f}");

        // Reduce damage by 25%
        evt.Damage *= 0.75f;

        // Show battle message
        _api.Events.Publish(new BattleMessageEvent
        {
            Message = $"{evt.Defender.Name}'s Cosmic Shield reduced the damage!",
            Priority = MessagePriority.AbilityTrigger
        });
    }
}

// Script entry point
var effect = new CosmicShieldEffect(Api);
return effect;
```

#### ProceduralStellarMusic.csx

```csharp
using PokeNET.ModApi;
using PokeNET.ModApi.Audio;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;

/// <summary>
/// Generates procedural stellar battle music
/// </summary>
public class ProceduralStellarMusic
{
    private readonly IAudioApi _audio;
    private readonly ILogger _logger;

    public ProceduralStellarMusic(IScriptApi api)
    {
        _audio = api.Audio;
        _logger = api.Logger;

        // Subscribe to battle start
        api.Events.Subscribe<BattleStartEvent>(OnBattleStart);
    }

    private void OnBattleStart(BattleStartEvent evt)
    {
        // Check if battle involves stellar-type creature
        if (!IsStellarBattle(evt))
            return;

        _logger.LogInformation("Generating stellar battle music...");

        // Create procedural music settings
        var settings = new MusicSettings
        {
            Tempo = 140,
            TimeSignature = new TimeSignature(4, 4),
            Key = NoteName.C,
            Scale = ScaleType.Minor,
            Mood = MusicMood.Mysterious
        };

        // Generate the track
        var music = _audio.CreateProceduralMusic(settings);

        // Add cosmic atmosphere
        AddCosmicMelody(music);
        AddPulsatingBass(music);
        AddShimmeringArpeggio(music);

        // Play the generated music
        music.Play(loop: true);
    }

    private bool IsStellarBattle(BattleStartEvent evt)
    {
        return evt.PlayerTeam.Any(c => c.HasType("stellar")) ||
               evt.OpponentTeam.Any(c => c.HasType("stellar"));
    }

    private void AddCosmicMelody(IProceduralMusic music)
    {
        // Ethereal lead melody using stellar scale
        music.AddTrack("melody", new TrackSettings
        {
            Instrument = MidiProgram.Pad2Warm,
            Volume = 0.7f,
            Pattern = PatternType.Melodic,
            Notes = new[] {
                NoteName.C, NoteName.D, NoteName.E,
                NoteName.G, NoteName.A, NoteName.C
            }
        });
    }

    private void AddPulsatingBass(IProceduralMusic music)
    {
        // Deep, pulsing bass
        music.AddTrack("bass", new TrackSettings
        {
            Instrument = MidiProgram.SynthBass1,
            Volume = 0.8f,
            Pattern = PatternType.Rhythmic,
            Notes = new[] { NoteName.C, NoteName.G }
        });
    }

    private void AddShimmeringArpeggio(IProceduralMusic music)
    {
        // Shimmering arpeggiated texture
        music.AddTrack("arpeggio", new TrackSettings
        {
            Instrument = MidiProgram.Celesta,
            Volume = 0.5f,
            Pattern = PatternType.Arpeggio,
            Notes = new[] {
                NoteName.C, NoteName.E, NoteName.G,
                NoteName.B, NoteName.D
            }
        });
    }
}

// Script entry point
var music = new ProceduralStellarMusic(Api);
return music;
```

### 4. Code Mod (Harmony Patches)

#### StellarCreatures.dll Source

```csharp
using HarmonyLib;
using PokeNET.ModApi;
using PokeNET.Domain.Battle;

namespace StellarCreatures
{
    /// <summary>
    /// Main mod entry point
    /// </summary>
    public class StellarCreaturesMod : IMod
    {
        private Harmony _harmony;
        private IModContext _context;

        public ModManifest Manifest => new()
        {
            Id = "example.stellarcreatures",
            Name = "Stellar Creatures",
            Version = new Version(1, 0, 0),
            Author = "PokeNET Team"
        };

        public void Initialize(IModContext context)
        {
            _context = context;
            _harmony = new Harmony("example.stellarcreatures");

            // Apply Harmony patches
            _harmony.PatchAll();

            context.Logger.LogInformation("Stellar Creatures mod initialized!");
        }

        public void OnGameStart(IGameContext context)
        {
            _context.Logger.LogInformation("Stellar Creatures ready!");
        }

        public void Shutdown()
        {
            // Unpatch all Harmony patches
            _harmony?.UnpatchAll("example.stellarcreatures");
        }
    }

    /// <summary>
    /// Harmony patch: Add visual effects for Stellar-type moves
    /// </summary>
    [HarmonyPatch(typeof(BattleAnimationSystem), nameof(BattleAnimationSystem.PlayMoveAnimation))]
    public static class StellarMoveAnimationPatch
    {
        static void Postfix(MoveDefinition move, ref AnimationSettings __result)
        {
            // Add extra sparkle effects for Stellar-type moves
            if (move.Type == "stellar")
            {
                __result.ParticleEffect = "stellar_sparkles";
                __result.ScreenShake = new ShakeSettings
                {
                    Intensity = 0.1f,
                    Duration = 0.3f
                };

                Logger.LogDebug($"Added stellar effects to move: {move.Name}");
            }
        }
    }

    /// <summary>
    /// Harmony patch: Custom type effectiveness display
    /// </summary>
    [HarmonyPatch(typeof(BattleUI), "ShowTypeEffectiveness")]
    public static class StellarTypeEffectivenessPatch
    {
        static void Postfix(float effectiveness, string attackType)
        {
            if (attackType == "stellar" && effectiveness > 1.0f)
            {
                // Show custom "Super Stellar!" message
                BattleUI.ShowCustomMessage(
                    "Super Stellar!",
                    color: Color.Purple,
                    duration: 1.5f
                );
            }
        }
    }
}
```

## Building the Code Mod

### Prerequisites
- .NET 9 SDK
- Visual Studio 2022 or JetBrains Rider

### Build Steps

1. Create new Class Library project:
```bash
dotnet new classlib -n StellarCreatures -f net9.0
```

2. Add NuGet references:
```xml
<ItemGroup>
  <PackageReference Include="PokeNET.ModApi" Version="1.0.0" />
  <PackageReference Include="Lib.Harmony" Version="2.3.0" />
</ItemGroup>
```

3. Build the project:
```bash
dotnet build -c Release
```

4. Copy DLL to mod folder:
```bash
cp bin/Release/net9.0/StellarCreatures.dll ../Assemblies/
```

## Testing the Mod

### 1. Verify Mod Loads
Check game console for:
```
[INFO] Loading mod: Stellar Creatures (example.stellarcreatures)
[INFO] Stellar Creatures mod initialized!
[INFO] Stellar Creatures ready!
```

### 2. Test New Content
- Open creature list → Find "Starlight"
- Start battle with Starlight → Check for procedural music
- Use Star Burst move → Verify damage and effects
- Check Cosmic Shield → Confirm damage reduction

### 3. Check Harmony Patches
- Use Stellar-type move → Look for particle effects
- Super-effective Stellar move → Verify custom message

## Compatibility

- **Game Version**: 1.0.0+
- **Conflicts**: None known
- **Load Order**: Can load anywhere

## Troubleshooting

### Mod Doesn't Load
- Check `modinfo.json` syntax
- Verify folder structure matches documentation
- Check console for errors

### Starlight Not Appearing
- Ensure JSON files are valid
- Check data definition IDs match references
- Verify sprites are in correct locations

### Scripts Not Running
- Check for C# syntax errors in `.csx` files
- Verify script paths in JSON match actual files
- Enable script debugging in settings

### Harmony Patches Failing
- Ensure DLL is compiled for .NET 9
- Check Harmony patch target methods exist
- Review patch logs for errors

## Credits

- **Created by**: PokeNET Team
- **Sprites by**: Example Artist
- **Music by**: Procedural Generation
- **Code by**: Example Developer

## License

MIT License - See LICENSE file for details

## Changelog

### Version 1.0.0 (2025-10-22)
- Initial release
- Added Stellar type
- Added Starlight creature
- Added Star Burst move
- Added Cosmic Shield ability
- Procedural battle music
- Harmony patches for effects

---

*This is an example mod for educational purposes. Feel free to use it as a template for your own creations!*
