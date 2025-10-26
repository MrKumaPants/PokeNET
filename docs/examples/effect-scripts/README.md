# Effect Scripts Examples

This directory contains example Roslyn scripts (`.csx`) that implement item and move effects for PokeNET.

## Key Concept: Script Reusability

**Scripts are parameterized** - the same script can be reused with different parameters from JSON data files.

### Example: Potion Family

**One script** (`potion.csx`) is reused for multiple items:

```json
{
  "name": "Potion",
  "effectScript": "scripts/items/potion.csx",
  "effectParameters": { "healAmount": 20 }
},
{
  "name": "Super Potion",
  "effectScript": "scripts/items/potion.csx",
  "effectParameters": { "healAmount": 50 }
},
{
  "name": "Hyper Potion",
  "effectScript": "scripts/items/potion.csx",
  "effectParameters": { "healAmount": 200 }
},
{
  "name": "Max Potion",
  "effectScript": "scripts/items/potion.csx",
  "effectParameters": { "healAmount": 9999 }
}
```

**The script reads the parameter:**
```csharp
public async Task<bool> UseAsync(IScriptContext context)
{
    // Get heal amount from JSON parameters
    int healAmount = context.GetParameter("healAmount", 20);

    // Use the parameter
    int actualHeal = Math.Min(healAmount, pokemon.MaxHP - pokemon.CurrentHP);
    pokemon.CurrentHP += actualHeal;

    return true;
}
```

## Item Effect Scripts

### potion.csx
- **Purpose**: Restore HP to a Pokemon
- **Parameters**:
  - `healAmount` (int) - Amount of HP to restore
- **Reused by**: Potion, Super Potion, Hyper Potion, Max Potion

### full-heal.csx
- **Purpose**: Cure all status conditions
- **Parameters**: None
- **Reused by**: Full Heal, Lava Cookie, Heal Powder

## Move Effect Scripts

### burn.csx
- **Purpose**: Inflict Burn status
- **Parameters**:
  - `burnChance` (int) - Probability 0-100
- **Reused by**: Ember (10%), Flamethrower (10%), Fire Blast (30%)

### stat-change.csx
- **Purpose**: Change any stat by any amount
- **Parameters**:
  - `stat` (string) - "Attack", "Defense", "Speed", etc.
  - `statChange` (int) - Stages to change (-6 to +6)
  - `targetSelf` (bool) - true for self, false for opponent
- **Reused by**: Growl, Sword Dance, Tail Whip, Dragon Dance, etc.

## How Parameters Work

### 1. Define parameters in JSON

```json
{
  "name": "Ember",
  "effectScript": "scripts/moves/burn.csx",
  "effectParameters": {
    "burnChance": 10
  }
}
```

### 2. Access parameters in script

```csharp
public async Task ApplyEffectAsync(IBattleContext context)
{
    // Read parameter with default fallback
    int burnChance = context.GetParameter("burnChance", 10);

    // Use parameter
    if (context.RandomChance(burnChance))
    {
        context.Defender.ApplyStatus("Burn");
    }
}
```

### 3. GetParameter<T> Method

```csharp
// Get with default value
int amount = context.GetParameter("healAmount", 20);

// Get different types
string stat = context.GetParameter("stat", "Attack");
bool targetSelf = context.GetParameter("targetSelf", true);
float multiplier = context.GetParameter("multiplier", 1.5f);

// Get from Parameters dictionary directly
if (context.Parameters.TryGetValue("specialFlag", out var flag))
{
    // Do something with flag
}
```

## Benefits of Parameterization

✅ **Less Code** - One script instead of dozens
✅ **Consistency** - Same logic for similar effects
✅ **Moddability** - Easy to create variants
✅ **Maintainability** - Fix once, fixes all uses
✅ **Flexibility** - JSON controls behavior

## Creating New Effects

### Template: Item Effect

```csharp
#r "PokeNET.Domain.dll"
using PokeNET.Domain.Scripting;

public class MyItemEffect : IItemEffect
{
    public string Name => "My Effect";
    public string Description => "Does something cool!";

    public bool CanUse(IScriptContext context)
    {
        // Read parameters
        var myParam = context.GetParameter("myParam", defaultValue);

        // Check if item can be used
        return someCondition;
    }

    public async Task<bool> UseAsync(IScriptContext context)
    {
        // Read parameters
        var myParam = context.GetParameter("myParam", defaultValue);

        // Apply effect
        // ...

        await context.ShowMessageAsync("Effect applied!");

        return true; // Consume item
    }
}
```

### Template: Move Effect

```csharp
#r "PokeNET.Domain.dll"
using PokeNET.Domain.Scripting;

public class MyMoveEffect : IMoveEffect
{
    public string Name => "My Effect";
    public string Description => "Does something cool!";
    public int Priority => 0;

    public bool CanTrigger(IBattleContext context)
    {
        // Read parameters
        var myParam = context.GetParameter("myParam", defaultValue);

        // Check if effect can trigger
        return someCondition;
    }

    public async Task ApplyEffectAsync(IBattleContext context)
    {
        // Read parameters
        var myParam = context.GetParameter("myParam", defaultValue);

        // Apply effect
        // ...

        await context.ShowMessageAsync("Effect triggered!");
    }
}
```

## Testing Scripts

```csharp
[Fact]
public async Task Script_UsesParameters_Correctly()
{
    // Load script
    var effect = await scriptEngine.CompileAsync<IItemEffect>("potion.csx");

    // Create context with parameters
    var context = new MockScriptContext
    {
        Parameters = new Dictionary<string, object>
        {
            { "healAmount", 50 }  // Super Potion
        },
        SelectedPokemon = new MockPokemon { CurrentHP = 50, MaxHP = 100 }
    };

    // Execute
    await effect.UseAsync(context);

    // Assert uses the parameter (50, not hardcoded 20)
    Assert.Equal(100, context.SelectedPokemon.CurrentHP);
}
```

## Common Parameters

### Item Effects
- `healAmount` (int) - HP to restore
- `healPercent` (int) - HP percentage to restore (0-100)
- `ppRestore` (int) - PP to restore to a move
- `revivePercent` (int) - HP percentage when reviving (0-100)
- `cureConditions` (string[]) - Status conditions to cure

### Move Effects
- `statusChance` (int) - Probability to inflict status (0-100)
- `stat` (string) - Stat to modify
- `statChange` (int) - Stages to change (-6 to +6)
- `targetSelf` (bool) - Apply to user (true) or opponent (false)
- `healPercent` (int) - HP percentage to restore (0-100)
- `recoilPercent` (int) - Recoil damage percentage (0-100)

## Script Debugging Tips

1. **Use descriptive messages**: `await context.ShowMessageAsync($"Debug: healAmount = {healAmount}")`
2. **Validate parameters**: Check ranges and types
3. **Provide defaults**: Always use `GetParameter` with default values
4. **Test edge cases**: Max HP, min HP, stat stages at limits
5. **Check context state**: Verify Pokemon state before modifying

## Performance Considerations

- Scripts are **compiled once and cached** - no performance penalty
- Parameter access is **dictionary lookup** - very fast (O(1))
- Prefer `GetParameter<T>` over manual dictionary access
- Scripts run in **sandboxed environment** - safe for mods

---

**Next Steps**: See `/docs/DataApiUsage.md` for how DataManager loads and compiles these scripts.
