# PokeNET Modding Guide - Phase 4

## Table of Contents
- [Introduction](#introduction)
- [Mod Types](#mod-types)
- [Getting Started](#getting-started)
- [Mod Manifest](#mod-manifest-modinfoJson)
- [Creating Data Mods](#creating-data-mods)
- [Creating Content Mods](#creating-content-mods)
- [Creating Code Mods (Harmony)](#creating-code-mods-harmony)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)
- [Advanced Topics](#advanced-topics)

## Introduction

Welcome to PokeNET modding! This guide will help you create custom modifications for PokeNET using our RimWorld-style modding framework. Whether you want to add new creatures, change game mechanics, or create entirely new content, this guide has you covered.

PokeNET's modding system is built on three pillars:
1. **Data-driven design** - Modify game content through JSON files
2. **Asset replacement** - Add custom sprites, sounds, and assets
3. **Runtime patching** - Use Harmony to modify game code without recompiling

## Mod Types

### 1. Data Mods

Data mods override or add game data using JSON files. These are the easiest mods to create and require no programming knowledge.

**Examples:**
- New creature definitions
- Custom item properties
- Modified battle formulas
- New dialogue trees

**Pros:**
- No coding required
- Easy to create and share
- Safe and compatible

**Cons:**
- Limited to predefined data structures
- Cannot add new game mechanics

### 2. Content Mods

Content mods replace or add art and audio assets. These mods enhance the visual and audio experience.

**Examples:**
- Custom creature sprites
- New UI themes
- Music and sound effects
- Custom fonts and animations

**Pros:**
- Pure creative expression
- Highly visible changes
- No coding required

**Cons:**
- Requires artistic skills
- File size can be large
- Asset format requirements

### 3. Code Mods

Code mods use Harmony to patch game code at runtime. These are the most powerful mods that can fundamentally change gameplay.

**Examples:**
- New battle mechanics
- Custom AI behaviors
- Game mode additions
- Integration with external systems

**Pros:**
- Unlimited possibilities
- Can modify any game system
- No source code access needed

**Cons:**
- Requires C# knowledge
- Higher compatibility risk
- More complex debugging

## Getting Started

### Prerequisites

- PokeNET installed and running
- Text editor (VS Code, Notepad++, etc.)
- For code mods: Visual Studio or Rider IDE
- Basic understanding of JSON format

### Creating Your First Mod

Follow these steps to create a simple data mod:

1. **Create mod directory structure:**
   ```
   Mods/
   └── MyFirstMod/
       ├── modinfo.json
       ├── Data/
       │   └── creatures.json
       └── Content/
           └── Sprites/
   ```

2. **Create the manifest file** (`modinfo.json` - see below)

3. **Add your content** (data files, assets, or code)

4. **Load the game** - Your mod will be automatically detected

5. **Test thoroughly** - Verify your changes work as expected

### Directory Structure

```
Mods/YourModName/
├── modinfo.json           # Required: Mod manifest
├── Data/                  # Optional: JSON data files
│   ├── creatures.json
│   ├── items.json
│   └── abilities.json
├── Content/               # Optional: Asset files
│   ├── Sprites/
│   │   └── creature_001.png
│   ├── Audio/
│   │   └── battle_music.ogg
│   └── UI/
│       └── custom_theme.png
└── Assemblies/            # Optional: Code mods (DLLs)
    └── YourMod.dll
```

## Mod Manifest (modinfo.json)

Every mod requires a `modinfo.json` file at its root directory. This file tells PokeNET how to load and manage your mod.

### Required Fields

```json
{
  "id": "com.example.mymod",
  "name": "My First Mod",
  "version": "1.0.0"
}
```

- **id**: Unique identifier (reverse domain notation recommended)
- **name**: Display name shown in the mod manager
- **version**: Semantic version (MAJOR.MINOR.PATCH)

### Optional Fields

```json
{
  "id": "com.example.mymod",
  "name": "My First Mod",
  "version": "1.0.0",
  "author": "YourName",
  "description": "Adds 10 new fire-type creatures with unique abilities",
  "url": "https://github.com/yourname/mymod",
  "dependencies": [
    {
      "id": "com.pokenet.core",
      "version": ">=1.0.0"
    }
  ],
  "loadAfter": ["com.other.mod"],
  "loadBefore": ["com.another.mod"],
  "incompatibleWith": ["com.conflicting.mod"]
}
```

- **author**: Your name or username
- **description**: What your mod does (shown in mod manager)
- **url**: Homepage or repository URL
- **dependencies**: Array of required mods with version constraints
- **loadAfter**: Load after these mods (soft dependency)
- **loadBefore**: Load before these mods
- **incompatibleWith**: Mods that conflict with yours

### Complete Example

```json
{
  "id": "com.example.dragontypes",
  "name": "Dragon Type Expansion",
  "version": "1.2.0",
  "author": "DragonMaster99",
  "description": "Adds 15 new dragon-type creatures and 5 dragon-specific abilities. Includes custom sprites and battle animations.",
  "url": "https://github.com/dragonmaster99/pokenet-dragons",
  "dependencies": [
    {
      "id": "com.pokenet.core",
      "version": ">=1.0.0"
    },
    {
      "id": "com.pokenet.extendedtypes",
      "version": "^2.0.0"
    }
  ],
  "loadAfter": ["com.pokenet.baseexpansion"],
  "incompatibleWith": ["com.other.dragonmod"]
}
```

## Creating Data Mods

Data mods use JSON files in the `Data/` directory to define game content. PokeNET loads these files at startup and merges them with the base game data.

### Creature Definition

Create `Data/creatures.json`:

```json
{
  "creatures": [
    {
      "id": "flame_dragon",
      "name": "Flame Dragon",
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
        {"level": 1, "move": "ember"},
        {"level": 7, "move": "dragon_breath"},
        {"level": 15, "move": "flamethrower"},
        {"level": 30, "move": "dragon_claw"}
      ],
      "evolutionChain": [
        {"from": "fire_lizard", "level": 16},
        {"to": "mega_flame_dragon", "item": "dragon_stone"}
      ],
      "spriteId": "flame_dragon_sprite",
      "description": "A majestic dragon wreathed in eternal flames."
    }
  ]
}
```

### Item Definition

Create `Data/items.json`:

```json
{
  "items": [
    {
      "id": "dragon_stone",
      "name": "Dragon Stone",
      "type": "evolution",
      "description": "A mysterious stone that radiates draconic energy.",
      "cost": 2100,
      "spriteId": "dragon_stone_sprite",
      "effect": {
        "type": "evolution",
        "target": "flame_dragon",
        "evolution": "mega_flame_dragon"
      }
    },
    {
      "id": "super_potion",
      "name": "Super Potion",
      "type": "consumable",
      "description": "Restores 50 HP to a creature.",
      "cost": 700,
      "spriteId": "super_potion_sprite",
      "effect": {
        "type": "heal",
        "amount": 50
      }
    }
  ]
}
```

### Ability Definition

Create `Data/abilities.json`:

```json
{
  "abilities": [
    {
      "id": "dragon_force",
      "name": "Dragon Force",
      "description": "Powers up dragon-type moves by 50% in sunny weather.",
      "effect": {
        "type": "damage_modifier",
        "condition": {
          "weather": "sunny",
          "moveType": "dragon"
        },
        "multiplier": 1.5
      }
    }
  ]
}
```

### Data Override Rules

1. **Merging**: If your mod defines a creature with the same ID as base game, your version takes precedence
2. **Addition**: New IDs are added to the game
3. **Array concatenation**: Arrays like `learnset` are merged, not replaced
4. **Property override**: Individual properties can be overridden without replacing entire objects

## Creating Content Mods

Content mods add or replace visual and audio assets.

### Sprite Requirements

**Creature Sprites:**
- Format: PNG with transparency
- Size: 96x96 pixels (recommended)
- Naming: `{creature_id}_sprite.png`
- Location: `Content/Sprites/`

**Item Sprites:**
- Format: PNG with transparency
- Size: 32x32 pixels
- Naming: `{item_id}_sprite.png`
- Location: `Content/Sprites/Items/`

### Audio Requirements

**Music:**
- Format: OGG Vorbis (recommended) or MP3
- Naming: `{music_id}.ogg`
- Location: `Content/Audio/Music/`

**Sound Effects:**
- Format: WAV or OGG
- Naming: `{sound_id}.wav`
- Location: `Content/Audio/SFX/`

### Asset Manifest

Create `Data/assets.json` to register your assets:

```json
{
  "sprites": [
    {
      "id": "flame_dragon_sprite",
      "path": "Content/Sprites/flame_dragon.png",
      "type": "creature"
    }
  ],
  "audio": [
    {
      "id": "dragon_roar",
      "path": "Content/Audio/SFX/dragon_roar.wav",
      "type": "sfx"
    },
    {
      "id": "dragon_battle_theme",
      "path": "Content/Audio/Music/dragon_battle.ogg",
      "type": "music"
    }
  ]
}
```

## Creating Code Mods (Harmony)

Code mods use [Harmony](https://harmony.pardeike.net/) to patch game code at runtime without modifying the original assemblies.

### Setting Up Development Environment

1. **Install Visual Studio 2022** or **JetBrains Rider**

2. **Create Class Library project** (.NET 6.0 or later)

3. **Add NuGet references:**
   ```xml
   <ItemGroup>
     <PackageReference Include="Lib.Harmony" Version="2.2.2" />
     <Reference Include="PokeNET.Core">
       <HintPath>path/to/PokeNET/PokeNET.Core.dll</HintPath>
     </Reference>
     <Reference Include="PokeNET.ModApi">
       <HintPath>path/to/PokeNET/PokeNET.ModApi.dll</HintPath>
     </Reference>
   </ItemGroup>
   ```

### Basic Harmony Patch

**Example: Double Damage Modifier**

```csharp
using HarmonyLib;
using PokeNET.Core.Battle;

namespace MyMod.Patches
{
    [HarmonyPatch(typeof(BattleSystem), "CalculateDamage")]
    public class DoubleDamagePatch
    {
        // Postfix runs AFTER the original method
        static void Postfix(ref int __result)
        {
            __result *= 2; // Double all damage!
        }
    }
}
```

**Example: Add Critical Hit Message**

```csharp
using HarmonyLib;
using PokeNET.Core.Battle;

namespace MyMod.Patches
{
    [HarmonyPatch(typeof(BattleSystem), "CalculateDamage")]
    public class CriticalHitMessagePatch
    {
        // Prefix runs BEFORE the original method
        static void Prefix(BattleContext context, out bool __state)
        {
            __state = context.IsCriticalHit;
        }

        static void Postfix(bool __state, BattleContext context)
        {
            if (__state)
            {
                context.AddMessage("Critical hit!");
            }
        }
    }
}
```

### Mod Entry Point

Every code mod must implement `IMod` interface:

```csharp
using HarmonyLib;
using PokeNET.ModApi;
using System;

namespace MyMod
{
    public class MyMod : IMod
    {
        public string Id => "com.example.mymod";
        public string Name => "My Awesome Mod";
        public Version Version => new Version(1, 0, 0);

        private Harmony _harmony;

        public void OnLoad(IModContext context)
        {
            // Apply all Harmony patches in this assembly
            _harmony = new Harmony(Id);
            _harmony.PatchAll();

            // Register event handlers
            context.Events.OnBattleStart += OnBattleStart;
            context.Events.OnCreatureCaptured += OnCreatureCaptured;

            // Access game data
            var creatures = context.GameData.Creatures;
            context.Logger.Info($"Loaded with {creatures.Count} creatures");
        }

        public void OnUnload()
        {
            // Clean up Harmony patches
            _harmony?.UnpatchAll(Id);
        }

        private void OnBattleStart(object sender, BattleEventArgs e)
        {
            // Custom battle start logic
        }

        private void OnCreatureCaptured(object sender, CaptureEventArgs e)
        {
            // Custom capture logic
        }
    }
}
```

### Advanced Harmony Patterns

**Transpiler (IL manipulation):**

```csharp
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

[HarmonyPatch(typeof(Experience), "GainExp")]
public class ExpMultiplierTranspiler
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            // Find where exp amount is loaded
            if (instruction.opcode == OpCodes.Ldarg_1)
            {
                yield return instruction;
                yield return new CodeInstruction(OpCodes.Ldc_I4_2); // Load 2
                yield return new CodeInstruction(OpCodes.Mul); // Multiply
            }
            else
            {
                yield return instruction;
            }
        }
    }
}
```

**Reverse Patch (expose private methods):**

```csharp
[HarmonyPatch]
public class ExposedMethods
{
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(BattleSystem), "GetHiddenPower")]
    public static int GetHiddenPower(object instance, Creature creature)
    {
        // This will be replaced with the actual method body
        throw new NotImplementedException("Stub");
    }
}
```

### Building and Testing

1. **Build your project** - Output DLL to `YourMod/Assemblies/`

2. **Copy to Mods folder:**
   ```
   Mods/
   └── YourMod/
       ├── modinfo.json
       └── Assemblies/
           └── YourMod.dll
   ```

3. **Launch PokeNET** - Check logs for mod loading

4. **Test in-game** - Verify your patches work

## Best Practices

### 1. Use Semantic Versioning

Follow [SemVer](https://semver.org/):
- **MAJOR** version for incompatible changes
- **MINOR** version for backwards-compatible features
- **PATCH** version for backwards-compatible bug fixes

Examples:
- `1.0.0` → `1.0.1` (bug fix)
- `1.0.1` → `1.1.0` (new feature)
- `1.1.0` → `2.0.0` (breaking change)

### 2. Test With Other Mods

- Test your mod with popular mods installed
- Check for conflicts in load order
- Verify compatibility with different game versions
- Test with different mod configurations

### 3. Handle Errors Gracefully

```csharp
public void OnLoad(IModContext context)
{
    try
    {
        _harmony = new Harmony(Id);
        _harmony.PatchAll();
        context.Logger.Info("Patches applied successfully");
    }
    catch (Exception ex)
    {
        context.Logger.Error($"Failed to apply patches: {ex.Message}");
        // Fallback or disable mod functionality
    }
}
```

### 4. Document Your API

If other mods can interact with yours, document it:

```csharp
/// <summary>
/// Gets the dragon power multiplier for a creature.
/// </summary>
/// <param name="creature">The creature to check</param>
/// <returns>Multiplier value (1.0 = normal, 2.0 = double)</returns>
public static float GetDragonPower(Creature creature)
{
    // Implementation
}
```

### 5. Provide Examples

Include example code or sample mods showing how to use your systems.

### 6. Performance Considerations

- Avoid expensive operations in frequently-called patches
- Cache computed values when possible
- Use Harmony Prefix to skip original methods when needed
- Profile your mod to identify bottlenecks

### 7. Localization Support

Support multiple languages:

```json
{
  "strings": {
    "en": {
      "dragon_stone_name": "Dragon Stone",
      "dragon_stone_desc": "A mysterious stone..."
    },
    "es": {
      "dragon_stone_name": "Piedra de Dragón",
      "dragon_stone_desc": "Una piedra misteriosa..."
    }
  }
}
```

## Troubleshooting

### Mod Not Loading

**Symptoms:**
- Mod doesn't appear in mod manager
- No errors in log

**Solutions:**

1. **Check manifest JSON syntax:**
   ```bash
   # Use online JSON validator
   # Verify all required fields present
   ```

2. **Verify file location:**
   ```
   Mods/YourMod/modinfo.json  ✓ Correct
   Mods/modinfo.json          ✗ Wrong
   ```

3. **Check dependencies:**
   - Ensure all required mods are installed
   - Verify version constraints are satisfied
   - Check for circular dependencies

4. **Review load order:**
   - Check if `loadAfter` mods are present
   - Verify no incompatible mods loaded

### Harmony Patches Not Working

**Symptoms:**
- Mod loads but behavior doesn't change
- No errors or warnings

**Solutions:**

1. **Verify target method signature:**
   ```csharp
   // Wrong: Method name typo
   [HarmonyPatch(typeof(BattleSystem), "CalculateDammage")]

   // Correct:
   [HarmonyPatch(typeof(BattleSystem), "CalculateDamage")]
   ```

2. **Check parameter types:**
   ```csharp
   // Wrong: Parameter types don't match
   [HarmonyPatch(typeof(BattleSystem), "Attack",
       new Type[] { typeof(string) })]

   // Correct:
   [HarmonyPatch(typeof(BattleSystem), "Attack",
       new Type[] { typeof(Creature), typeof(Move) })]
   ```

3. **Enable Harmony debugging:**
   ```csharp
   Harmony.DEBUG = true;
   ```

4. **Check for patch conflicts:**
   - Multiple mods patching same method
   - Use Harmony priority to resolve conflicts

### Game Crashes After Installing Mod

**Symptoms:**
- Game crashes on startup or during specific actions

**Solutions:**

1. **Check error logs:**
   - Location: `Logs/error.log`
   - Look for stack traces mentioning your mod

2. **Disable mod and test:**
   - Remove mod from Mods folder
   - If crash stops, issue is in your mod

3. **Binary search for problematic code:**
   - Comment out half of your patches
   - Determine which half causes crash
   - Repeat until you find the specific issue

4. **Common crash causes:**
   - Null reference exceptions
   - Invalid IL in transpilers
   - Missing type references
   - Infinite loops in patches

### Data Mod Not Applying Changes

**Symptoms:**
- JSON file present but data unchanged in-game

**Solutions:**

1. **Validate JSON syntax:**
   - Use JSON validator
   - Check for trailing commas
   - Verify proper escaping

2. **Check file location:**
   ```
   YourMod/Data/creatures.json  ✓ Correct
   YourMod/creatures.json       ✗ Wrong
   ```

3. **Verify data format:**
   - Ensure JSON structure matches expected schema
   - Check field names for typos
   - Verify data types (numbers vs strings)

4. **Check load order:**
   - If another mod overrides your data
   - Use `loadAfter` to control order

### Performance Issues

**Symptoms:**
- Game runs slowly with mod installed
- Frame drops during specific actions

**Solutions:**

1. **Profile your patches:**
   ```csharp
   static void Prefix()
   {
       var sw = Stopwatch.StartNew();
       // Your code
       sw.Stop();
       if (sw.ElapsedMilliseconds > 1)
           Debug.Log($"Slow patch: {sw.ElapsedMilliseconds}ms");
   }
   ```

2. **Optimize hot paths:**
   - Cache frequently-accessed data
   - Avoid LINQ in performance-critical code
   - Use object pooling for frequent allocations

3. **Use Prefix to skip methods:**
   ```csharp
   static bool Prefix()
   {
       // Return false to skip original method
       return false;
   }
   ```

## Advanced Topics

### Dependency Version Ranges

Use semantic version constraints:

```json
{
  "dependencies": [
    {
      "id": "com.pokenet.core",
      "version": ">=1.0.0"      // At least 1.0.0
    },
    {
      "id": "com.other.mod",
      "version": "^2.0.0"        // 2.x.x (compatible)
    },
    {
      "id": "com.another.mod",
      "version": "~1.2.0"        // 1.2.x (patch updates)
    },
    {
      "id": "com.exact.mod",
      "version": "1.5.3"         // Exactly 1.5.3
    }
  ]
}
```

**Version constraint syntax:**
- `>=1.0.0` - Greater than or equal to
- `^2.0.0` - Compatible with 2.0.0 (2.x.x)
- `~1.2.0` - Approximately 1.2.0 (1.2.x)
- `1.5.3` - Exact version

### Patch Priority

Control patch execution order:

```csharp
[HarmonyPatch(typeof(BattleSystem), "CalculateDamage")]
[HarmonyPriority(Priority.High)]
public class HighPriorityPatch
{
    // Runs before normal priority patches
}

[HarmonyPatch(typeof(BattleSystem), "CalculateDamage")]
[HarmonyPriority(Priority.Low)]
public class LowPriorityPatch
{
    // Runs after normal priority patches
}
```

**Priority levels:**
- `Priority.First` (800)
- `Priority.VeryHigh` (600)
- `Priority.High` (400)
- `Priority.Normal` (0) - default
- `Priority.Low` (-400)
- `Priority.VeryLow` (-600)
- `Priority.Last` (-800)

### Asset Conflict Resolution

When multiple mods provide the same asset:

1. **Load order determines priority**
   - Mods loaded later override earlier ones
   - Use `loadAfter` to control this

2. **Explicit override in manifest:**
   ```json
   {
     "assetOverrides": [
       {
         "id": "some_sprite",
         "priority": "high"
       }
     ]
   }
   ```

3. **Compatibility patches:**
   - Create compatibility mods that mediate conflicts
   - Merge assets programmatically

### Dynamic Content Loading

Load additional content at runtime:

```csharp
public void OnLoad(IModContext context)
{
    // Load content from external files
    var customData = context.ContentLoader.LoadJson<CustomData>(
        "Data/custom_data.json");

    // Register content dynamically
    foreach (var creature in customData.Creatures)
    {
        context.GameData.RegisterCreature(creature);
    }

    // Load assets asynchronously
    context.ContentLoader.LoadSpriteAsync("custom_sprite", sprite =>
    {
        context.GameData.RegisterSprite("custom_sprite", sprite);
    });
}
```

### Mod Settings and Configuration

Create user-configurable settings:

```csharp
public class ModSettings
{
    public float DamageMultiplier { get; set; } = 2.0f;
    public bool EnableDebugMode { get; set; } = false;
}

public class MyMod : IMod
{
    private ModSettings _settings;

    public void OnLoad(IModContext context)
    {
        // Load settings from file
        _settings = context.Settings.Load<ModSettings>("settings.json");

        // Use settings
        DamageMultiplier = _settings.DamageMultiplier;
    }
}
```

### Inter-Mod Communication

Allow mods to communicate:

```csharp
// Mod A: Provide API
public interface IDragonAPI
{
    float GetDragonPower(Creature creature);
}

public class MyMod : IMod, IDragonAPI
{
    public void OnLoad(IModContext context)
    {
        // Register API
        context.ModRegistry.RegisterAPI<IDragonAPI>(this);
    }

    public float GetDragonPower(Creature creature)
    {
        return creature.HasType("dragon") ? 2.0f : 1.0f;
    }
}

// Mod B: Use API
public void OnLoad(IModContext context)
{
    var dragonAPI = context.ModRegistry.GetAPI<IDragonAPI>();
    if (dragonAPI != null)
    {
        var power = dragonAPI.GetDragonPower(myCreature);
    }
}
```

### Event System

Subscribe to game events:

```csharp
public void OnLoad(IModContext context)
{
    context.Events.OnBattleStart += (sender, e) =>
    {
        var battle = e.Battle;
        context.Logger.Info($"Battle started: {battle.Id}");
    };

    context.Events.OnCreatureCaptured += (sender, e) =>
    {
        var creature = e.Creature;
        context.Logger.Info($"Captured: {creature.Name}");
    };

    context.Events.OnItemUsed += (sender, e) =>
    {
        var item = e.Item;
        context.Logger.Info($"Used item: {item.Name}");
    };
}
```

### Custom Content Pipeline

Create custom asset processors:

```csharp
public class CustomSpriteProcessor : IAssetProcessor
{
    public bool CanProcess(string extension)
    {
        return extension == ".custom";
    }

    public object Process(byte[] data)
    {
        // Custom processing logic
        return ProcessCustomFormat(data);
    }
}

public void OnLoad(IModContext context)
{
    context.ContentPipeline.RegisterProcessor(new CustomSpriteProcessor());
}
```

---

## Additional Resources

- **PokeNET Modding API Reference**: `/docs/api/modapi-phase4.md`
- **Example Mods**: `/docs/examples/`
- **Tutorials**: `/docs/tutorials/mod-development.md`
- **Harmony Documentation**: https://harmony.pardeike.net/
- **Community Discord**: [Link to be added]
- **Mod Repository**: [Link to be added]

## Getting Help

If you encounter issues:

1. Check this guide and troubleshooting section
2. Review example mods
3. Check error logs in `Logs/` folder
4. Ask in community Discord
5. Submit bug reports on GitHub

Happy modding!
