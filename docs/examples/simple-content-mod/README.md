# Simple Content Mod Example

This example demonstrates how to create a content mod that adds custom sprites to PokeNET.

## What This Mod Does

Replaces the sprite for a creature with a custom image. This mod shows you how to:
- Add custom sprite assets
- Register sprites with the game
- Override existing sprites

## File Structure

```
simple-content-mod/
├── README.md                    # This file
├── modinfo.json                 # Mod manifest
├── Data/
│   └── assets.json              # Asset registration
└── Content/
    └── Sprites/
        └── custom_creature.png  # Your custom sprite
```

## Step-by-Step Guide

### Step 1: Create Mod Directory

Create a new folder in your `Mods/` directory:

```
Mods/
└── SimpleContentMod/
```

### Step 2: Create modinfo.json

Create `modinfo.json`:

```json
{
  "id": "com.example.simplecontentmod",
  "name": "Simple Content Mod",
  "version": "1.0.0",
  "author": "YourName",
  "description": "Adds custom sprite for Flame Dragon",
  "dependencies": [
    {
      "id": "com.pokenet.core",
      "version": ">=1.0.0"
    }
  ]
}
```

### Step 3: Create Directory Structure

Create the content directories:

```
Mods/SimpleContentMod/
├── Content/
│   └── Sprites/
└── Data/
```

### Step 4: Add Your Sprite

Create or find a sprite image with these specifications:

**Creature Sprites:**
- **Format**: PNG with transparency
- **Size**: 96x96 pixels (recommended)
- **Color depth**: 32-bit RGBA
- **File naming**: `custom_creature.png`

Place your sprite in `Content/Sprites/custom_creature.png`

**Tips for creating sprites:**
- Use pixel art style to match game aesthetic
- Keep important details in center 64x64 area
- Use transparency for non-creature parts
- Consider different poses (front, back, shiny)

### Step 5: Register Your Asset

Create `Data/assets.json`:

```json
{
  "sprites": [
    {
      "id": "flame_dragon_sprite",
      "path": "Content/Sprites/custom_creature.png",
      "type": "creature",
      "metadata": {
        "width": 96,
        "height": 96,
        "artist": "YourName"
      }
    }
  ]
}
```

**Explanation:**
- **id**: Sprite identifier (matches creature's `spriteId`)
- **path**: Relative path from mod root
- **type**: Asset type (creature, item, ui, etc.)
- **metadata**: Optional additional information

### Step 6: Test Your Mod

1. Copy `SimpleContentMod/` to `Mods/`
2. Launch PokeNET
3. Check mod manager shows "Simple Content Mod"
4. Find a Flame Dragon in-game
5. Verify your custom sprite appears

## Adding More Assets

### Item Sprites

Add item sprites with these specifications:

**Item Sprites:**
- Format: PNG with transparency
- Size: 32x32 pixels
- Location: `Content/Sprites/Items/`

```json
{
  "sprites": [
    {
      "id": "dragon_stone_sprite",
      "path": "Content/Sprites/Items/dragon_stone.png",
      "type": "item"
    }
  ]
}
```

### UI Elements

Add UI sprites:

**UI Sprites:**
- Format: PNG with transparency
- Size: Varies by element
- Location: `Content/UI/`

```json
{
  "sprites": [
    {
      "id": "custom_button",
      "path": "Content/UI/button.png",
      "type": "ui"
    }
  ]
}
```

### Audio Files

Add music and sound effects:

**Music:**
- Format: OGG Vorbis (recommended) or MP3
- Location: `Content/Audio/Music/`

**Sound Effects:**
- Format: WAV or OGG
- Location: `Content/Audio/SFX/`

```json
{
  "audio": [
    {
      "id": "dragon_roar",
      "path": "Content/Audio/SFX/dragon_roar.wav",
      "type": "sfx",
      "metadata": {
        "volume": 0.8,
        "loop": false
      }
    },
    {
      "id": "dragon_battle_theme",
      "path": "Content/Audio/Music/dragon_battle.ogg",
      "type": "music",
      "metadata": {
        "volume": 0.6,
        "loop": true
      }
    }
  ]
}
```

## Advanced: Multiple Sprite Variants

Add shiny variants and different forms:

```json
{
  "sprites": [
    {
      "id": "flame_dragon_sprite",
      "path": "Content/Sprites/flame_dragon_normal.png",
      "type": "creature"
    },
    {
      "id": "flame_dragon_sprite_shiny",
      "path": "Content/Sprites/flame_dragon_shiny.png",
      "type": "creature",
      "metadata": {
        "variant": "shiny"
      }
    },
    {
      "id": "flame_dragon_sprite_back",
      "path": "Content/Sprites/flame_dragon_back.png",
      "type": "creature",
      "metadata": {
        "view": "back"
      }
    }
  ]
}
```

## Sprite Creation Tools

### Recommended Software

**Free:**
- **Aseprite** (open source builds)
- **Piskel** (online pixel editor)
- **GIMP** (general image editor)
- **Krita** (digital painting)

**Paid:**
- **Aseprite** (official version)
- **Photoshop**
- **GraphicsGale**

### Pixel Art Tips

1. **Start with silhouette** - Get the shape right first
2. **Use limited palette** - 4-8 colors per sprite
3. **Add contrast** - Light and dark areas
4. **Details last** - Big shapes first, then details
5. **Test in-game** - How it looks at actual size

### Animation (Advanced)

Create sprite sheets for animated sprites:

```
Frame 1  Frame 2  Frame 3  Frame 4
[    ]   [    ]   [    ]   [    ]
```

Register animated sprite:

```json
{
  "sprites": [
    {
      "id": "flame_dragon_animated",
      "path": "Content/Sprites/flame_dragon_sheet.png",
      "type": "creature",
      "metadata": {
        "frames": 4,
        "frameWidth": 96,
        "frameHeight": 96,
        "frameRate": 10
      }
    }
  ]
}
```

## Asset Conflict Resolution

If multiple mods provide sprites with the same ID:

### Priority System

1. **Load order** - Last loaded mod wins
2. **Explicit priority** - Set in manifest

```json
{
  "sprites": [
    {
      "id": "flame_dragon_sprite",
      "path": "Content/Sprites/my_sprite.png",
      "type": "creature",
      "priority": "high"
    }
  ]
}
```

### Compatibility Patch

Create a compatibility mod that handles conflicts:

```json
{
  "id": "com.example.sprite_compatibility",
  "name": "Sprite Pack Compatibility",
  "dependencies": [
    {"id": "com.example.pack1", "version": ">=1.0.0"},
    {"id": "com.example.pack2", "version": ">=1.0.0"}
  ],
  "loadAfter": [
    "com.example.pack1",
    "com.example.pack2"
  ]
}
```

## Common Issues

### Sprite Not Appearing

**Problem**: Custom sprite doesn't show in-game

**Solution**:
1. Check file path in `assets.json`
2. Verify PNG is valid and not corrupted
3. Check sprite ID matches creature's `spriteId`
4. Look for errors in log files

### Sprite Appears Stretched/Distorted

**Problem**: Sprite looks wrong in-game

**Solution**:
1. Verify image is exactly 96x96 pixels
2. Check aspect ratio is 1:1
3. Ensure no odd dimensions
4. Verify DPI is 72 or 96

### Transparency Issues

**Problem**: Sprite has white/black background

**Solution**:
1. Save as PNG (not JPG)
2. Enable alpha channel in editor
3. Delete background layer
4. Verify transparency in image viewer

### Performance Issues

**Problem**: Game lags with many custom sprites

**Solution**:
1. Optimize PNG files (use pngcrush or optipng)
2. Reduce sprite sheet sizes
3. Use lower-resolution variants
4. Enable sprite caching

## Next Steps

Once you've mastered content mods, try:

1. **Animated sprites** - Add sprite sheet animations
2. **Audio mods** - Add custom music and sounds
3. **UI themes** - Create complete UI overhauls
4. **Code mods** - Use Harmony to add new features

## Resources

- **Modding Guide**: `/docs/modding/phase4-modding-guide.md`
- **API Reference**: `/docs/api/modapi-phase4.md`
- **More Examples**: `/docs/examples/`
- **Pixel Art Tutorial**: https://lospec.com/pixel-art-tutorials

## Example Sprite

Since we can't include an actual image file, here's a placeholder text file:

**Content/Sprites/PLACEHOLDER.txt**:
```
This is where your 96x96 PNG sprite would go.

For testing, you can use any Pokemon sprite from:
- https://veekun.com/dex/downloads
- https://pokemondb.net/sprites

Rename it to match your asset ID and place it here.
```
