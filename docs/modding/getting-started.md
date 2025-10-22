# Getting Started with PokeNET Modding

## Introduction

Welcome to PokeNET modding! This guide will help you create your first mod for the PokeNET framework. Whether you want to add new creatures, create custom abilities, or completely transform gameplay, this guide will get you started.

## What is a Mod?

A mod (modification) is a package that extends or modifies PokeNET's functionality. Mods can:

- **Add new content**: Creatures, moves, items, maps
- **Modify existing content**: Change stats, behaviors, graphics
- **Transform gameplay**: New battle systems, mechanics, features
- **Fix bugs**: Community patches and improvements

## Types of Mods

PokeNET supports four types of mods, from simple to advanced:

### 1. Data Mods (Easiest)
**No coding required** - Use JSON or XML files to define content.

**What you can do**:
- Add new creatures with stats and abilities
- Create new moves and items
- Define new trainers and locations
- Modify existing data

**Skills needed**: Basic JSON/XML editing

### 2. Content Mods (Easy)
**Replace or add game assets** - Provide new graphics and sounds.

**What you can do**:
- Custom creature sprites
- New UI graphics
- Custom music and sound effects
- Modified textures

**Skills needed**: Image/audio editing software

### 3. Script Mods (Intermediate)
**C# scripting** - Write behavior in C# without compiling DLLs.

**What you can do**:
- Custom move effects
- Event scripting
- Procedural generation
- Dynamic behaviors

**Skills needed**: Basic C# knowledge

### 4. Code Mods (Advanced)
**Full code modifications** - Use Harmony to patch game code at runtime.

**What you can do**:
- Deep gameplay changes
- New systems and mechanics
- Performance improvements
- Complete overhauls

**Skills needed**: C# programming, Harmony knowledge

## Prerequisites

### Required Software

1. **Text Editor**
   - Visual Studio Code (recommended)
   - Any text editor for JSON/XML

2. **For Script/Code Mods**
   - .NET 9 SDK
   - Visual Studio 2022 or JetBrains Rider

3. **For Content Mods**
   - Image editor (GIMP, Photoshop, Aseprite)
   - Audio editor (Audacity) - optional

### Recommended Tools

- **JSON Schema Validator**: VS Code extension
- **Version Control**: Git
- **Compression**: 7-Zip or WinRAR

## Your First Mod: Hello PokeNET

Let's create a simple mod that adds a new creature!

### Step 1: Create Mod Directory

Navigate to your PokeNET installation and find the `Mods` folder:

```
PokeNET/
└── Mods/
    └── HelloPokeNET/    ← Create this folder
```

### Step 2: Create Mod Manifest

Create `modinfo.json` in your mod folder:

```json
{
  "id": "yourname.hellopokeNet",
  "name": "Hello PokeNET",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "My first PokeNET mod!",
  "targetGameVersion": "1.0.0",
  "dependencies": []
}
```

### Step 3: Add Data Definition

Create `Defs/Creatures/` folder structure:

```
HelloPokeNET/
├── modinfo.json
└── Defs/
    └── Creatures/
        └── HelloMon.json
```

Create `HelloMon.json`:

```json
{
  "id": "creature_hellomon",
  "name": "HelloMon",
  "type": "normal",
  "baseStats": {
    "hp": 50,
    "attack": 45,
    "defense": 40,
    "spAttack": 35,
    "spDefense": 40,
    "speed": 55
  },
  "abilities": ["ability_friendly"],
  "learnset": [
    {
      "move": "move_tackle",
      "level": 1
    },
    {
      "move": "move_growl",
      "level": 1
    }
  ],
  "evolutions": [],
  "catchRate": 255,
  "baseExperience": 60,
  "growthRate": "mediumFast",
  "eggGroups": ["field"],
  "genderRatio": 0.5
}
```

### Step 4: Test Your Mod

1. Launch PokeNET
2. Check the console/log for mod loading messages
3. Look for "HelloMon" in the creature list

Congratulations! You've created your first mod! 🎉

## Mod Folder Structure

A complete mod can have this structure:

```
MyMod/
├── modinfo.json              # Mod metadata (REQUIRED)
├── About/
│   ├── About.xml             # Additional metadata
│   └── Preview.png           # Mod thumbnail
├── Defs/                     # Data definitions
│   ├── Creatures/
│   │   └── *.json
│   ├── Moves/
│   │   └── *.json
│   ├── Items/
│   │   └── *.json
│   └── Abilities/
│       └── *.json
├── Textures/                 # Graphics assets
│   ├── Creatures/
│   ├── Items/
│   └── UI/
├── Sounds/                   # Audio assets
│   ├── Music/
│   └── SFX/
├── Scripts/                  # C# scripts
│   └── *.csx
├── Assemblies/               # Compiled DLLs
│   └── MyMod.dll
└── Patches/                  # Harmony patches
    └── *.xml
```

## Mod Manifest Reference

### Required Fields

```json
{
  "id": "author.modname",           // Unique identifier
  "name": "Mod Display Name",       // Human-readable name
  "version": "1.0.0",               // Semantic version
  "author": "Author Name",          // Your name
  "description": "What it does",    // Brief description
  "targetGameVersion": "1.0.0"      // Compatible game version
}
```

### Optional Fields

```json
{
  "dependencies": [
    {
      "id": "other.mod.id",
      "version": "1.0.0",           // Minimum version
      "optional": false
    }
  ],
  "incompatibilities": [
    "incompatible.mod.id"
  ],
  "loadAfter": [
    "should.load.before.this"
  ],
  "loadBefore": [
    "should.load.after.this"
  ],
  "homepage": "https://example.com",
  "supportUrl": "https://example.com/support",
  "license": "MIT",
  "tags": ["creatures", "gameplay"]
}
```

## Mod Loading Order

Mods are loaded in this order:

1. **Explicit dependencies** - Mods listed in `dependencies`
2. **Load order hints** - `loadBefore` and `loadAfter`
3. **Alphabetical** - By mod ID

### Example

```json
// Mod A
{
  "id": "author.modA",
  "loadBefore": ["author.modB"]
}

// Mod B
{
  "id": "author.modB",
  "dependencies": [
    {"id": "author.modC"}
  ]
}

// Mod C
{
  "id": "author.modC"
}
```

**Load order**: C → A → B

## Best Practices

### 1. Use Unique IDs
Always prefix your mod ID with your username:
- ✅ `yourname.coolmod`
- ❌ `coolmod`

### 2. Semantic Versioning
Use [SemVer](https://semver.org/) for versions:
- `1.0.0` - Major.Minor.Patch
- `1.0.1` - Bug fixes
- `1.1.0` - New features (compatible)
- `2.0.0` - Breaking changes

### 3. Document Your Mod
Include:
- README.md with description and usage
- CHANGELOG.md with version history
- LICENSE file

### 4. Test Thoroughly
- Test with and without other mods
- Check for errors in console
- Verify all features work
- Test edge cases

### 5. Handle Errors Gracefully
- Validate data before use
- Provide helpful error messages
- Don't crash the game

## Common Issues

### Mod Not Loading

**Symptoms**: Mod doesn't appear in mod list

**Solutions**:
1. Check `modinfo.json` syntax (use JSON validator)
2. Ensure `id` field is unique
3. Verify folder is in `Mods/` directory
4. Check console for error messages

### Missing Dependencies

**Symptoms**: "Dependency not found" error

**Solutions**:
1. Install required mods
2. Check dependency IDs are correct
3. Verify dependency versions match

### Data Not Applying

**Symptoms**: Changes don't appear in game

**Solutions**:
1. Check JSON syntax
2. Verify file paths match expected structure
3. Ensure IDs match existing definitions
4. Check load order relative to other mods

### Asset Not Found

**Symptoms**: Missing textures or sounds

**Solutions**:
1. Verify file paths in data definitions
2. Check file extensions are correct
3. Ensure assets are in correct folders
4. Use forward slashes in paths: `Textures/Items/potion.png`

## Next Steps

Now that you know the basics, explore these guides:

1. [Mod Structure and Manifest](mod-structure.md) - Detailed manifest options
2. [Data Mods](data-mods.md) - Learn JSON data definition
3. [Content Mods](content-mods.md) - Add custom graphics and audio
4. [Script Mods](script-mods.md) - Write C# scripts
5. [Code Mods](code-mods.md) - Advanced Harmony patching

## Resources

- [API Reference](../api/modapi-overview.md)
- [Example Mod](../examples/example-mod/README.md)
- [Community Discord](https://discord.gg/yourserver)
- [Mod Showcase](https://github.com/youruser/pokenet-mods)

## Getting Help

- **Documentation**: Check the guides in this folder
- **Discord**: Ask in #modding-help channel
- **GitHub Issues**: Report bugs or request features
- **Community Forum**: Share your creations!

---

*Last Updated: 2025-10-22*
