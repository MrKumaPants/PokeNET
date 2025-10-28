# PokeNET CLI Quick Start

Get up and running with the PokeNET CLI in minutes!

## Prerequisites

- .NET 9.0 SDK
- PokeNET game data files

## Building

```bash
cd PokeNET/PokeNET.CLI
dotnet build
```

## Running

### Interactive Mode (Recommended for First-Time Users)

Simply run without arguments:

```bash
dotnet run
```

You'll see a beautiful menu:

```
╔══════════════════════════════════════╗
║     PokeNET CLI Tool v1.0           ║
║   Interactive Testing Environment    ║
╚══════════════════════════════════════╝

What would you like to do?
❯ Browse Data (Species, Moves, Items, Types)
  Battle Calculator
  System Tests
  Mod Manager
  Exit
```

Use arrow keys to navigate and Enter to select!

### Command Mode (For Power Users)

Execute commands directly:

```bash
# View Charizard's stats
dotnet run -- data show species charizard

# List all Fire-type Pokemon
dotnet run -- data list-species --type Fire

# Calculate competitive stats
dotnet run -- battle stats garchomp --level 100 --nature Adamant --evs 0,252,0,0,4,252

# Check type matchups
dotnet run -- battle effectiveness electric water flying

# Test all systems
dotnet run -- system test data
```

## Quick Examples

### 1. Exploring Pokemon

```bash
# Interactive: Just run and select "Browse Data" → "Species"
dotnet run

# Command: Show Bulbasaur
dotnet run -- data show species bulbasaur
```

### 2. Building a Competitive Team

```bash
# Calculate perfect IV Adamant Garchomp
dotnet run -- battle stats garchomp --level 100 --nature Adamant --ivs 31,31,31,31,31,31 --evs 0,252,0,0,4,252

# Calculate Timid Gengar
dotnet run -- battle stats gengar --level 100 --nature Timid --ivs 31,0,31,31,31,31 --evs 0,0,0,252,4,252
```

### 3. Learning Type Matchups

```bash
# What's Fire effective against?
dotnet run -- data show type fire

# Is Electric good against Gyarados (Water/Flying)?
dotnet run -- battle effectiveness electric water flying
# Output: 4.0x Super Duper Effective!
```

### 4. Testing Your Mod

```bash
# Validate your mod before loading
dotnet run -- mod validate --directory Mods

# List loaded mods
dotnet run -- mod list
```

## Tips

- **Start Interactive**: New users should start with interactive mode (`dotnet run`)
- **Use Tab Completion**: In interactive mode, navigate with arrow keys
- **Get Help**: Add `--help` to any command: `dotnet run -- data --help`
- **Debug Mode**: Set `DOTNET_ENVIRONMENT=Development` to see full stack traces
- **Automation**: Use command mode in scripts and CI/CD pipelines

## Troubleshooting

### "Data path does not exist"

Make sure the `Data` directory exists with JSON files:
```
PokeNET.CLI/
  Data/
    species.json
    moves.json
    items.json
    types.json
    encounters.json
```

### "Species not found"

- Check spelling (case-insensitive)
- Try the Pokedex number: `dotnet run -- data show species 1`
- List all species: `dotnet run -- data list-species`

### Build Errors

Restore packages:
```bash
dotnet restore
dotnet build
```

## Next Steps

- Read the full [README.md](README.md) for all commands
- Check out the examples in the README
- Explore the Interactive menu to discover features
- Build your competitive team with the battle calculator
- Test your mods with the mod manager

## Publishing (Optional)

Create a self-contained executable:

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained

# macOS
dotnet publish -c Release -r osx-x64 --self-contained
```

The executable will be in `bin/Release/net9.0/<runtime>/publish/`

Happy testing! 🎮

