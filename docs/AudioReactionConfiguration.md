# Audio Reaction Configuration System

## Overview

The Audio Reaction Configuration System allows you to define dynamic audio behaviors in response to game events using JSON configuration files. This enables non-programmers to create complex audio reactions without modifying code.

## Architecture

### Components

1. **AudioReactionConfig.cs** - Data classes for JSON deserialization
   - `AudioReactionConfig` - Root configuration container
   - `ReactionDefinition` - Individual reaction with conditions and actions
   - `ConditionDefinition` - Condition evaluation rules
   - `ActionDefinition` - Actions to execute

2. **AudioReactionLoader.cs** - Configuration loader with validation
   - Loads reactions from JSON files
   - Validates configurations
   - Evaluates conditions using reflection
   - Executes actions via IAudioManager
   - Supports hot-reload via FileSystemWatcher

3. **audio-reactions.json** - Configuration file (19 built-in reactions)

## Configuration File Structure

```json
{
  "reactions": [
    {
      "name": "ReactionName",
      "type": "MusicTransition|SoundEffect|Ambient|VolumeControl",
      "eventType": "BattleStartEvent|HealthChangedEvent|WeatherChangedEvent|...",
      "enabled": true,
      "conditions": [
        {
          "property": "PropertyName",
          "operator": "equals|notEquals|lessThan|greaterThan|contains",
          "value": "ComparisonValue"
        }
      ],
      "actions": [
        {
          "type": "PlayMusic|PlaySound|FadeIn|FadeOut|SetVolume|StopAll",
          "channel": "Music|SoundEffect|Ambient",
          "path": "audio/path/to/file.ogg",
          "volume": 1.0,
          "fadeIn": 0.5,
          "fadeOut": 0.5,
          "duration": 1.0,
          "loop": true
        }
      ]
    }
  ]
}
```

## Supported Event Types

- `BattleStartEvent` - When battles begin
- `BattleEndEvent` - When battles end
- `HealthChangedEvent` - When health changes
- `WeatherChangedEvent` - When weather changes
- `AttackEvent` - When attacks are used
- `CriticalHitEvent` - When critical hits occur
- `PokemonFaintEvent` - When Pokemon faint
- `ItemUseEvent` - When items are used
- `PokemonCaughtEvent` - When Pokemon are caught
- `LevelUpEvent` - When Pokemon level up
- `GameStateChangedEvent` - When game state changes

## Condition Operators

### equals
Checks if property value equals the specified value.
```json
{
  "property": "IsWildBattle",
  "operator": "equals",
  "value": true
}
```

### notEquals
Checks if property value does not equal the specified value.
```json
{
  "property": "IsGymLeader",
  "operator": "notEquals",
  "value": false
}
```

### lessThan
Checks if numeric property is less than the specified value.
```json
{
  "property": "HealthPercentage",
  "operator": "lessThan",
  "value": 0.25
}
```

### greaterThan
Checks if numeric property is greater than the specified value.
```json
{
  "property": "HealthPercentage",
  "operator": "greaterThan",
  "value": 0.75
}
```

### contains
Checks if string property contains the specified substring (case-insensitive).
```json
{
  "property": "PokemonName",
  "operator": "contains",
  "value": "Pika"
}
```

## Action Types

### PlayMusic
Plays background music with optional looping.
```json
{
  "type": "PlayMusic",
  "path": "audio/music/battle_wild.ogg",
  "volume": 1.0,
  "fadeIn": 0.5,
  "loop": true
}
```

### PlaySound
Plays a one-shot sound effect.
```json
{
  "type": "PlaySound",
  "path": "audio/sfx/critical.wav",
  "volume": 0.8
}
```

### FadeIn
Fades audio in over specified duration (not yet fully implemented).
```json
{
  "type": "FadeIn",
  "channel": "Music",
  "duration": 1.0
}
```

### FadeOut
Fades audio out over specified duration.
```json
{
  "type": "FadeOut",
  "channel": "Music",
  "duration": 0.5
}
```

### SetVolume
Sets channel volume (not yet fully implemented).
```json
{
  "type": "SetVolume",
  "channel": "Music",
  "volume": 0.3,
  "duration": 0.2
}
```

### StopAll
Stops all audio playback immediately.
```json
{
  "type": "StopAll"
}
```

## Built-in Reactions (19 Total)

### Battle Music
1. **WildBattleMusic** - Plays wild battle music
2. **GymBattleMusic** - Plays gym leader battle music
3. **TrainerBattleMusic** - Plays trainer battle music
4. **VictoryMusic** - Plays victory fanfare

### Health-Based
5. **LowHealthBeep** - Plays beeping sound at <25% health
6. **CriticalHealthMusic** - Changes music at <15% health

### Combat Events
7. **PokemonFaintSound** - Sound when Pokemon faints
8. **CriticalHitSound** - Sound for critical hits
9. **FireAttackSound** - Fire-type attack sound
10. **WaterAttackSound** - Water-type attack sound
11. **ElectricAttackSound** - Electric-type attack sound

### Weather Ambient
12. **RainAmbient** - Rain ambient sounds
13. **SnowAmbient** - Snow ambient sounds
14. **SandstormAmbient** - Sandstorm ambient sounds

### Item & Pokemon Events
15. **ItemUseSound** - Sound for item usage
16. **PokemonCatchSound** - Sound for catching Pokemon
17. **LevelUpSound** - Sound for leveling up

### Volume Control
18. **MenuDucking** - Lowers music volume in menus
19. **MenuUnduck** - Restores music volume when leaving menus

## Usage Examples

### Loading Configuration

```csharp
using PokeNET.Audio.Configuration;

var logger = loggerFactory.CreateLogger<AudioReactionLoader>();
var loader = new AudioReactionLoader(logger, audioManager, "config/audio-reactions.json");

// Load configuration
var config = await loader.LoadAsync();

// Enable hot-reload
loader.EnableHotReload();

// Subscribe to reload events
loader.ConfigurationReloaded += (sender, e) =>
{
    Console.WriteLine($"Configuration reloaded at {e.Timestamp}");
};
```

### Evaluating Conditions

```csharp
var battleEvent = new BattleStartEvent
{
    IsWildBattle = true,
    IsGymLeader = false
};

var reaction = config.Reactions.First(r => r.Name == "WildBattleMusic");

// Check if all conditions are met
var allConditionsMet = reaction.Conditions.All(c =>
    loader.EvaluateCondition(c, battleEvent));

if (allConditionsMet && reaction.Enabled)
{
    // Execute actions
    foreach (var action in reaction.Actions)
    {
        await loader.ExecuteActionAsync(action);
    }
}
```

### Creating Custom Reactions

```json
{
  "name": "BossBattleMusic",
  "type": "MusicTransition",
  "eventType": "BattleStartEvent",
  "enabled": true,
  "conditions": [
    {
      "property": "IsWildBattle",
      "operator": "equals",
      "value": false
    },
    {
      "property": "BattleType",
      "operator": "equals",
      "value": 3
    }
  ],
  "actions": [
    {
      "type": "FadeOut",
      "channel": "Music",
      "duration": 1.0
    },
    {
      "type": "PlayMusic",
      "path": "audio/music/battle_boss.ogg",
      "volume": 1.0,
      "fadeIn": 1.5,
      "loop": true
    }
  ]
}
```

### Disabling Specific Reactions

```json
{
  "name": "LowHealthBeep",
  "type": "SoundEffect",
  "eventType": "HealthChangedEvent",
  "enabled": false,  // Disable this reaction
  "conditions": [...],
  "actions": [...]
}
```

## Validation Rules

The loader validates all configurations on load:

1. **Reaction must have a name** - Cannot be empty or whitespace
2. **Reaction must have an event type** - Must specify which event to listen for
3. **Conditions must have properties** - Cannot be empty
4. **Operators must be valid** - Must be one of: equals, notEquals, lessThan, greaterThan, contains
5. **Conditions must have values** - Cannot be null
6. **Reactions must have at least one action** - Empty action lists are invalid
7. **Action types must be valid** - Must be one of: PlayMusic, PlaySound, FadeIn, FadeOut, SetVolume, StopAll
8. **Playback actions require paths** - PlayMusic and PlaySound must specify audio file paths
9. **Volume must be 0.0 to 1.0** - Values outside this range are invalid
10. **Fade actions require positive duration** - FadeIn and FadeOut must have duration > 0

## Hot-Reload Support

The system supports hot-reload via FileSystemWatcher:

```csharp
// Enable hot-reload
loader.EnableHotReload();

// Configuration automatically reloads when file changes
// ConfigurationReloaded event is raised
```

### Hot-Reload Features

- Monitors config file for changes
- Automatically reloads on file save
- Validates new configuration
- Raises event with new configuration
- Includes 100ms debounce to ensure file is fully written
- Graceful error handling (logs errors, keeps old config on failure)

## Integration with Reactive Audio Engine

```csharp
public class ReactiveAudioEngine
{
    private readonly AudioReactionLoader _loader;

    public async Task InitializeAsync()
    {
        // Load reactions
        var config = await _loader.LoadAsync();
        _loader.EnableHotReload();

        // Subscribe to events
        _eventBus.Subscribe<BattleStartEvent>(async evt =>
        {
            var matchingReactions = config.Reactions
                .Where(r => r.EventType == nameof(BattleStartEvent))
                .Where(r => r.Enabled)
                .Where(r => r.Conditions.All(c => _loader.EvaluateCondition(c, evt)));

            foreach (var reaction in matchingReactions)
            {
                foreach (var action in reaction.Actions)
                {
                    await _loader.ExecuteActionAsync(action);
                }
            }
        });
    }
}
```

## Performance Considerations

- **Reflection-based evaluation**: Conditions use reflection to access event properties
- **File watching overhead**: Hot-reload adds FileSystemWatcher overhead
- **JSON parsing**: Configuration is parsed once on load, then reused
- **Validation**: Comprehensive validation ensures runtime safety

## Future Enhancements

1. **Advanced fade support** - Implement FadeIn/FadeOut with actual audio fading
2. **SetVolume action** - Implement dynamic volume control
3. **Compiled expressions** - Cache reflection calls for better performance
4. **Complex conditions** - Support AND/OR logic, nested conditions
5. **Action delays** - Support delayed action execution
6. **Priority system** - Allow reactions to override each other
7. **Interpolation** - Support value interpolation in paths (e.g., `audio/music/route_{level}.ogg`)

## Files Created

1. `/mnt/c/Users/nate0/RiderProjects/PokeNET/PokeNET/PokeNET.Audio/Configuration/AudioReactionConfig.cs` (135 lines)
2. `/mnt/c/Users/nate0/RiderProjects/PokeNET/PokeNET/PokeNET.Audio/Configuration/AudioReactionLoader.cs` (448 lines)
3. `/mnt/c/Users/nate0/RiderProjects/PokeNET/config/audio-reactions.json` (268 lines)
4. `/mnt/c/Users/nate0/RiderProjects/PokeNET/tests/Audio/AudioReactionLoaderTests.cs` (532 lines)

## Total Lines of Code: ~1,383 lines
