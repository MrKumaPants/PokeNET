# PokeNET CLI Tool

A beautiful command-line interface for testing and exploring the PokeNET game engine, built with [Spectre.Console](https://spectreconsole.net/).

## Features

- 🎮 **Interactive Mode**: User-friendly menu system when no arguments are provided
- ⚡ **Command Mode**: Direct command execution with arguments
- 📊 **Data Inspection**: Browse species, moves, items, and types
- ⚔️ **Battle Calculator**: Calculate stats with IVs/EVs/Natures and type effectiveness
- 🧪 **System Tests**: Validate data loading and system integrity
- 🔧 **Mod Manager**: List, validate, and test mods

## Installation

Build the CLI project:

```bash
cd PokeNET/PokeNET.CLI
dotnet build
```

## Usage

### Interactive Mode

Run without arguments to enter interactive mode:

```bash
dotnet run
# or if built:
./pokenet-cli
```

This launches a beautiful menu-driven interface where you can:
- Browse game data
- Calculate battle stats
- Run system tests
- Manage mods

### Command Mode

Execute commands directly from the command line:

```bash
pokenet-cli <command> [options]
```

## Commands

### Data Commands

**List all species:**
```bash
pokenet-cli data list-species [--type Fire] [--limit 50]
```

**Show species details:**
```bash
pokenet-cli data show species bulbasaur
pokenet-cli data show species 1
```

**Show move details:**
```bash
pokenet-cli data show move "flamethrower"
```

**Show item details:**
```bash
pokenet-cli data show item "potion"
```

**Show type effectiveness:**
```bash
pokenet-cli data show type fire
```

### Battle Commands

**Calculate Pokemon stats:**
```bash
pokenet-cli battle stats charizard --level 50 --nature Adamant --ivs 31,31,31,31,31,31 --evs 252,0,0,0,0,252
```

Parameters:
- `--level`: Pokemon level (1-100), default: 50
- `--nature`: Pokemon nature (Hardy, Adamant, Timid, etc.)
- `--ivs`: IVs in format HP,Atk,Def,SpA,SpD,Spe (0-31 each), default: 31,31,31,31,31,31
- `--evs`: EVs in format HP,Atk,Def,SpA,SpD,Spe (0-252 each, max 510 total), default: 0,0,0,0,0,0

**Calculate type effectiveness:**
```bash
# Single type
pokenet-cli battle effectiveness fire grass

# Dual type
pokenet-cli battle effectiveness fire grass flying
```

### System Commands

**Test data loading:**
```bash
pokenet-cli system test data
```

This validates that all game data (species, moves, items, types, encounters) loads correctly.

### Mod Commands

**List loaded mods:**
```bash
pokenet-cli mod list
```

**Validate mods:**
```bash
pokenet-cli mod validate [--directory Mods]
```

## Examples

### Example 1: Browse Species
```bash
# List all Fire-type species
pokenet-cli data list-species --type Fire --limit 10

# Show detailed info about Charizard
pokenet-cli data show species charizard
```

Output:
```
╔═══════════════════════════════════════╗
║         #006 Charizard (Fire/Flying)  ║
╚═══════════════════════════════════════╝

┌───────────────────────────────────────┐
│ Base Stats                            │
├──────────┬────────────────────────────┤
│ HP       │ 78 ███████████████░░░░░    │
│ Attack   │ 84 ████████████████░░░░    │
│ Defense  │ 78 ███████████████░░░░░    │
│ Sp. Atk  │ 109 █████████████████████  │
│ Sp. Def  │ 85 ████████████████░░░░    │
│ Speed    │ 100 ████████████████████   │
│ Total    │ 534                        │
└──────────┴────────────────────────────┘
```

### Example 2: Calculate Competitive Stats
```bash
# Adamant Garchomp with max Attack and Speed
pokenet-cli battle stats garchomp --level 100 --nature Adamant --evs 0,252,0,0,4,252
```

### Example 3: Check Type Matchups
```bash
# Check if Electric is good against Water/Flying (Gyarados)
pokenet-cli battle effectiveness electric water flying

# Output: 4.0× Super Duper Effective!
```

### Example 4: Validate Game Data
```bash
pokenet-cli system test data
```

Output:
```
✓ Species: 151 loaded
✓ Moves: 165 loaded
✓ Items: 50 loaded
✓ Types: 18 loaded
✓ Encounters: 12 loaded

All tests passed! Total items loaded: 396
```

## Configuration

Edit `appsettings.json` to customize paths:

```json
{
  "Data": {
    "Path": "Data"
  },
  "Mods": {
    "Directory": "Mods"
  },
  "Scripts": {
    "MaxCacheSize": 100
  }
}
```

## Development

The CLI is built with a clean architecture:

```
PokeNET.CLI/
├── Commands/           # Command implementations
│   ├── Battle/        # Battle calculator commands
│   ├── Data/          # Data inspection commands
│   ├── Mod/           # Mod management commands
│   └── System/        # System test commands
├── Infrastructure/     # Core services and DI
├── Interactive/        # Interactive menu system
└── Program.cs         # Entry point and configuration
```

### Adding New Commands

1. Create a new command class inheriting from `CliCommand<TSettings>`
2. Define the `Settings` class with command arguments/options
3. Implement `ExecuteCommandAsync`
4. Register in `Program.ConfigureCommands()`

Example:
```csharp
public class MyCommand : CliCommand<MyCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<NAME>")]
        public string Name { get; set; } = string.Empty;
    }

    public MyCommand(CliContext context) : base(context) { }

    protected override async Task ExecuteCommandAsync(
        CommandContext context, 
        Settings settings)
    {
        // Your command logic here
        AnsiConsole.MarkupLine($"[green]Hello {settings.Name}![/]");
    }
}
```

## Spectre.Console Features Used

- **Tables**: Display structured data
- **Panels**: Box important information
- **Progress Bars**: Show long-running operations
- **Selection Prompts**: Interactive menus
- **Status Spinners**: Loading indicators
- **Markup**: Colored and styled text
- **Exception Formatting**: Beautiful error display

## Tips

- Use arrow keys to navigate in interactive mode
- Press Ctrl+C to exit at any time
- Run commands with `--help` to see all options
- In debug mode, full stack traces are shown for errors
- Interactive mode is perfect for exploration
- Command mode is great for automation and scripting

## Requirements

- .NET 9.0 or later
- PokeNET.Core and PokeNET.Scripting assemblies
- Game data files in the Data directory

## License

Part of the PokeNET project.

