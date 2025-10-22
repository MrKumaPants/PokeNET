# Complete Mod Development Tutorial

Welcome to the complete PokeNET modding tutorial! This guide will walk you through creating your first mod from scratch.

## Table of Contents

1. [Introduction](#introduction)
2. [Setting Up Your Environment](#setting-up-your-environment)
3. [Tutorial 1: Your First Data Mod](#tutorial-1-your-first-data-mod)
4. [Tutorial 2: Adding Custom Sprites](#tutorial-2-adding-custom-sprites)
5. [Tutorial 3: Creating a Code Mod](#tutorial-3-creating-a-code-mod)
6. [Tutorial 4: Combining Everything](#tutorial-4-combining-everything)
7. [Publishing Your Mod](#publishing-your-mod)
8. [Common Pitfalls](#common-pitfalls)

---

## Introduction

### What We'll Build

By the end of this tutorial, you'll have created a complete mod that:
- Adds new creatures to the game
- Includes custom sprites
- Modifies game mechanics with code
- Is ready to share with others

### Prerequisites

- PokeNET installed and working
- Text editor (VS Code recommended)
- For code mods: Visual Studio or Rider
- 1-2 hours of time
- Basic understanding of JSON

### Learning Path

```
Data Mod (Easy)
    ↓
Content Mod (Easy)
    ↓
Code Mod (Medium)
    ↓
Complete Mod (Advanced)
```

---

## Setting Up Your Environment

### 1. Install Required Tools

**Text Editor (Choose one):**
- [VS Code](https://code.visualstudio.com/) - Free, lightweight
- [Notepad++](https://notepad-plus-plus.org/) - Simple and fast
- [Sublime Text](https://www.sublimetext.com/) - Fast and powerful

**For Code Mods (Optional):**
- [Visual Studio 2022 Community](https://visualstudio.microsoft.com/) - Free
- [JetBrains Rider](https://www.jetbrains.com/rider/) - Paid, excellent

**Helpful Extensions (VS Code):**
- JSON Tools - JSON validation and formatting
- C# - C# syntax highlighting and IntelliSense
- Prettier - Code formatting

### 2. Create Your Workspace

Create a folder for mod development:

```
Documents/
└── PokeNET Mods/
    └── MyFirstMod/        # Your mod folder
```

### 3. Understand PokeNET Structure

```
PokeNET/
├── PokeNET.exe            # Game executable
├── Mods/                  # Your mods go here
│   └── YourMod/
├── Data/                  # Base game data
├── Content/               # Base game assets
└── Logs/                  # Error logs
```

---

## Tutorial 1: Your First Data Mod

Let's create a simple mod that adds a new fire-type creature.

### Step 1: Plan Your Creature

Before coding, design your creature on paper:

```
Name: Spark Mouse
Type: Electric
Stats: Fast but fragile
Evolution: Level 18 → Thunder Mouse
Theme: Small electric rodent
```

### Step 2: Create Folder Structure

Create these folders:

```
MyFirstMod/
├── modinfo.json
└── Data/
    └── creatures.json
```

### Step 3: Write modinfo.json

This tells PokeNET about your mod:

```json
{
  "id": "com.yourname.myfirstmod",
  "name": "My First Mod",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "Adds Spark Mouse and Thunder Mouse",
  "dependencies": [
    {
      "id": "com.pokenet.core",
      "version": ">=1.0.0"
    }
  ]
}
```

**Explanation:**
- `id` - Unique identifier (use your name/username)
- `name` - What players see
- `version` - Start at 1.0.0
- `author` - Your name
- `dependencies` - Requires base game v1.0.0+

### Step 4: Create creatures.json

Define your creatures:

```json
{
  "creatures": [
    {
      "id": "spark_mouse",
      "name": "Spark Mouse",
      "type": ["electric"],
      "baseStats": {
        "hp": 35,
        "attack": 55,
        "defense": 30,
        "specialAttack": 50,
        "specialDefense": 40,
        "speed": 90
      },
      "abilities": ["static"],
      "learnset": [
        {"level": 1, "move": "tackle"},
        {"level": 1, "move": "thunder_shock"},
        {"level": 5, "move": "tail_whip"},
        {"level": 10, "move": "quick_attack"},
        {"level": 15, "move": "thunder_wave"},
        {"level": 20, "move": "spark"}
      ],
      "evolutionChain": [
        {"to": "thunder_mouse", "level": 18}
      ],
      "spriteId": "spark_mouse_sprite",
      "description": "A small mouse creature that stores electricity in its cheeks.",
      "catchRate": 0.50,
      "baseExperience": 55,
      "growthRate": "medium_fast"
    },
    {
      "id": "thunder_mouse",
      "name": "Thunder Mouse",
      "type": ["electric"],
      "baseStats": {
        "hp": 60,
        "attack": 90,
        "defense": 55,
        "specialAttack": 90,
        "specialDefense": 80,
        "speed": 110
      },
      "abilities": ["static"],
      "learnset": [
        {"level": 1, "move": "tackle"},
        {"level": 1, "move": "thunder_shock"},
        {"level": 1, "move": "tail_whip"},
        {"level": 1, "move": "quick_attack"},
        {"level": 18, "move": "agility"},
        {"level": 25, "move": "thunderbolt"},
        {"level": 35, "move": "thunder"}
      ],
      "evolutionChain": [
        {"from": "spark_mouse", "level": 18}
      ],
      "spriteId": "thunder_mouse_sprite",
      "description": "Its evolved form crackles with powerful electricity. Can unleash devastating thunderbolts.",
      "catchRate": 0.25,
      "baseExperience": 142,
      "growthRate": "medium_fast"
    }
  ]
}
```

### Step 5: Test Your Mod

1. **Copy folder to Mods:**
   ```
   PokeNET/Mods/MyFirstMod/
   ```

2. **Launch PokeNET**

3. **Check Mod Manager:**
   - Should see "My First Mod"
   - Status: Active

4. **Check Logs:**
   ```
   PokeNET/Logs/latest.log
   ```
   Look for: "Loaded mod: My First Mod"

5. **Test In-Game:**
   - Start new game
   - Search for Spark Mouse in wild encounters
   - Catch and level to 18 to test evolution

### Step 6: Debug Common Issues

**Mod Not Appearing:**
```bash
# Check JSON syntax
# Use https://jsonlint.com
# Common issues:
# - Missing commas
# - Trailing commas (last item)
# - Unclosed brackets
```

**Creatures Not Spawning:**
- Check creature IDs are unique
- Verify `creatures.json` is in `Data/` folder
- Check logs for parsing errors

---

## Tutorial 2: Adding Custom Sprites

Now let's add custom sprites for our creatures!

### Step 1: Find or Create Sprites

**Option A: Use Existing Sprites**
- Download from [Veekun](https://veekun.com/dex/downloads)
- Use Pokemon sprite generators online

**Option B: Create Your Own**
1. Open [Piskel](https://www.piskelapp.com/) (free online editor)
2. Create new sprite: 96x96 pixels
3. Draw your creature
4. Export as PNG

### Step 2: Prepare Sprites

Ensure your sprites meet requirements:
- Format: PNG with transparency
- Size: 96x96 pixels
- Color: 32-bit RGBA
- Names: `spark_mouse.png`, `thunder_mouse.png`

### Step 3: Add Sprites to Mod

Create folder structure:

```
MyFirstMod/
├── modinfo.json
├── Data/
│   ├── creatures.json
│   └── assets.json        # New!
└── Content/               # New!
    └── Sprites/
        ├── spark_mouse.png
        └── thunder_mouse.png
```

### Step 4: Create assets.json

Register your sprites:

```json
{
  "sprites": [
    {
      "id": "spark_mouse_sprite",
      "path": "Content/Sprites/spark_mouse.png",
      "type": "creature",
      "metadata": {
        "width": 96,
        "height": 96
      }
    },
    {
      "id": "thunder_mouse_sprite",
      "path": "Content/Sprites/thunder_mouse.png",
      "type": "creature",
      "metadata": {
        "width": 96,
        "height": 96
      }
    }
  ]
}
```

### Step 5: Test Sprites

1. Copy updated mod to `Mods/`
2. Launch game
3. Encounter Spark Mouse
4. Verify sprite appears correctly

**Common Issues:**

**White/Black Background:**
- PNG must have transparency
- Re-save with alpha channel

**Sprite Stretched:**
- Verify exact 96x96 pixel size
- Check aspect ratio is 1:1

**Sprite Not Loading:**
- Check file path in `assets.json`
- Verify PNG is valid (open in image viewer)
- Check logs for errors

---

## Tutorial 3: Creating a Code Mod

Let's add custom behavior using Harmony!

### Step 1: Set Up Visual Studio Project

1. **Open Visual Studio**
2. **Create New Project:**
   - File → New → Project
   - "Class Library (.NET 6.0)"
   - Name: "MyFirstCodeMod"
   - Location: `MyFirstMod/Source/`

### Step 2: Install Harmony

Right-click project → Manage NuGet Packages:
- Search: "Lib.Harmony"
- Install version 2.2.2

### Step 3: Add PokeNET References

Edit `MyFirstCodeMod.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Lib.Harmony" Version="2.2.2" />
  </ItemGroup>

  <ItemGroup>
    <Reference Include="PokeNET.Core">
      <HintPath>../../../PokeNET.Core.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="PokeNET.ModApi">
      <HintPath>../../../PokeNET.ModApi.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

### Step 4: Create Main Mod Class

Create `MyFirstCodeMod.cs`:

```csharp
using System;
using HarmonyLib;
using PokeNET.ModApi;

public class MyFirstCodeMod : IMod
{
    public string Id => "com.yourname.myfirstcodemod";
    public string Name => "My First Code Mod";
    public Version Version => new Version(1, 0, 0);

    private Harmony _harmony;
    private IModContext _context;

    public void OnLoad(IModContext context)
    {
        _context = context;

        // Apply Harmony patches
        _harmony = new Harmony(Id);
        _harmony.PatchAll();

        // Subscribe to events
        context.Events.OnCreatureCaptured += OnCreatureCaptured;

        context.Logger.Info($"{Name} loaded!");
    }

    public void OnUnload()
    {
        _harmony?.UnpatchAll(Id);

        if (_context != null)
        {
            _context.Events.OnCreatureCaptured -= OnCreatureCaptured;
        }
    }

    private void OnCreatureCaptured(object sender, CaptureEventArgs e)
    {
        if (e.Creature.Id == "spark_mouse" || e.Creature.Id == "thunder_mouse")
        {
            _context.Logger.Info($"Congrats on catching {e.Creature.Name}!");
        }
    }
}
```

### Step 5: Create a Simple Patch

Create `Patches/ElectricBoostPatch.cs`:

```csharp
using HarmonyLib;
using PokeNET.Core.Battle;

namespace MyFirstCodeMod.Patches
{
    // Boost electric moves by 50%
    [HarmonyPatch(typeof(BattleSystem), "CalculateDamage")]
    public class ElectricBoostPatch
    {
        static void Postfix(ref int __result, Move move)
        {
            if (move.Type == "electric")
            {
                __result = (int)(__result * 1.5f);
            }
        }
    }
}
```

### Step 6: Build and Deploy

1. **Build project** (Ctrl+Shift+B)
2. **Copy DLL:**
   ```
   Source/bin/Debug/net6.0/MyFirstCodeMod.dll
   →
   MyFirstMod/Assemblies/MyFirstCodeMod.dll
   ```

3. **Update modinfo.json:**
   ```json
   {
     "id": "com.yourname.myfirstcodemod",
     "version": "1.0.0"
   }
   ```

### Step 7: Test Code Mod

1. Copy mod to `Mods/`
2. Launch game
3. Test electric moves deal 50% more damage
4. Check console for capture messages

---

## Tutorial 4: Combining Everything

Let's create a complete, polished mod!

### Final Mod Structure

```
MyAwesomeMod/
├── modinfo.json
├── README.md                  # Documentation
├── CHANGELOG.md               # Version history
├── Data/
│   ├── creatures.json
│   ├── items.json
│   ├── abilities.json
│   └── assets.json
├── Content/
│   ├── Sprites/
│   │   ├── spark_mouse.png
│   │   └── thunder_mouse.png
│   └── Audio/
│       └── SFX/
│           └── electric_charge.wav
├── Assemblies/
│   └── MyAwesomeMod.dll
└── Source/                    # Source code (optional)
```

### Create README.md

```markdown
# My Awesome Mod

Adds electric mouse creatures with enhanced abilities!

## Features

- **New Creatures**: Spark Mouse and Thunder Mouse
- **Custom Sprites**: Hand-crafted pixel art
- **Electric Boost**: Electric moves deal 50% more damage
- **Sound Effects**: Custom electric sound effects

## Installation

1. Download latest release
2. Extract to `PokeNET/Mods/`
3. Launch game

## Requirements

- PokeNET v1.0.0 or later

## Changelog

### v1.0.0 (2024-01-15)
- Initial release
- Added Spark Mouse and Thunder Mouse
- Custom sprites and sounds
- Electric damage boost

## Credits

- Created by [Your Name]
- Sprites by [Artist Name]
- Inspired by Pokemon

## License

MIT License
```

### Create CHANGELOG.md

```markdown
# Changelog

## [1.0.0] - 2024-01-15

### Added
- Spark Mouse creature
- Thunder Mouse evolution
- Custom sprite assets
- Electric move damage boost
- Capture notification system

### Changed
- N/A (initial release)

### Fixed
- N/A (initial release)
```

---

## Publishing Your Mod

### Step 1: Prepare Release

1. **Test thoroughly:**
   - Test with other mods
   - Check all features work
   - Verify no errors in logs

2. **Create documentation:**
   - README with features
   - Installation instructions
   - Requirements and compatibility

3. **Version correctly:**
   - Use semantic versioning
   - Document changes in CHANGELOG

### Step 2: Package Mod

Create release folder:

```
MyAwesomeMod-v1.0.0/
├── MyAwesomeMod/          # The actual mod
│   ├── modinfo.json
│   ├── Data/
│   ├── Content/
│   └── Assemblies/
├── README.md
└── LICENSE.txt
```

### Step 3: Create Archive

Zip the release folder:
```
MyAwesomeMod-v1.0.0.zip
```

### Step 4: Upload

**Options:**
- GitHub Releases
- ModDB
- Nexus Mods
- PokeNET Mod Repository

**Include:**
- Description
- Screenshots
- Requirements
- Installation guide
- Known issues

### Step 5: Maintain Mod

**When updating:**
1. Increment version number
2. Update CHANGELOG.md
3. Test with latest game version
4. Notify users of breaking changes

---

## Common Pitfalls

### 1. JSON Syntax Errors

**Problem:** Trailing commas, missing brackets

**Solution:**
```json
// BAD
{
  "id": "test",
  "name": "Test",  // ← Trailing comma!
}

// GOOD
{
  "id": "test",
  "name": "Test"
}
```

### 2. Wrong File Paths

**Problem:** Can't find assets

**Solution:**
- Always use forward slashes: `Content/Sprites/test.png`
- Paths relative to mod root
- Check capitalization matches exactly

### 3. Mod Load Order

**Problem:** Mod features not working

**Solution:**
```json
{
  "loadAfter": ["com.required.mod"],
  "loadBefore": ["com.dependent.mod"]
}
```

### 4. Harmony Patch Conflicts

**Problem:** Multiple mods patching same method

**Solution:**
```csharp
[HarmonyPatch(typeof(BattleSystem), "CalculateDamage")]
[HarmonyPriority(Priority.High)]  // ← Set priority
public class MyPatch { }
```

### 5. Memory Leaks

**Problem:** Game slows down over time

**Solution:**
```csharp
public void OnUnload()
{
    // Always clean up!
    _harmony?.UnpatchAll(Id);
    context.Events.OnBattleStart -= OnBattleStart;
}
```

### 6. Breaking Changes

**Problem:** Update breaks existing saves

**Solution:**
- Use migration system
- Document breaking changes
- Provide upgrade path

### 7. Not Testing With Other Mods

**Problem:** Works alone, breaks with others

**Solution:**
- Test with popular mods
- Check compatibility
- Use soft dependencies

---

## Next Steps

### Advanced Topics

1. **Complex Harmony Patterns**
   - Transpilers
   - Reverse patches
   - Manual patching

2. **Inter-Mod APIs**
   - Create public APIs
   - Version APIs properly
   - Document for other modders

3. **Performance Optimization**
   - Profile your code
   - Cache computed values
   - Optimize hot paths

4. **Localization**
   - Multiple language support
   - Dynamic text loading
   - Community translations

### Learning Resources

- **PokeNET Modding Guide**: Complete reference
- **Harmony Docs**: https://harmony.pardeike.net/
- **C# Documentation**: https://docs.microsoft.com/dotnet/csharp/
- **Community Discord**: Get help from other modders

### Example Mods to Study

1. **Simple Data Mod** - Creature additions
2. **Simple Content Mod** - Sprite replacement
3. **Simple Code Mod** - Harmony patches
4. **Advanced Mods** - Community showcase

---

## Conclusion

Congratulations! You've learned:
- How to create data mods
- How to add custom assets
- How to write code mods with Harmony
- How to publish and maintain mods

**Remember:**
- Start small and iterate
- Test thoroughly
- Document everything
- Share with the community
- Have fun!

Happy modding!

---

## Appendix: Quick Reference

### File Formats

- **Creatures**: `Data/creatures.json`
- **Items**: `Data/items.json`
- **Abilities**: `Data/abilities.json`
- **Assets**: `Data/assets.json`
- **Sprites**: `Content/Sprites/*.png` (96x96)
- **Audio**: `Content/Audio/**/*.wav|ogg`

### Common Properties

```json
{
  "id": "unique_id",
  "name": "Display Name",
  "type": ["type1", "type2"],
  "baseStats": {
    "hp": 0-255,
    "attack": 0-255,
    "defense": 0-255,
    "specialAttack": 0-255,
    "specialDefense": 0-255,
    "speed": 0-255
  }
}
```

### Harmony Basics

```csharp
// Run after method
[HarmonyPatch(typeof(Class), "Method")]
static void Postfix(ref ReturnType __result) { }

// Run before method
[HarmonyPatch(typeof(Class), "Method")]
static bool Prefix() { return true; }

// Access parameters
static void Postfix(ParamType param) { }
```

### Event Subscriptions

```csharp
context.Events.OnBattleStart += Handler;
context.Events.OnDamageDealt += Handler;
context.Events.OnCreatureCaptured += Handler;
```
