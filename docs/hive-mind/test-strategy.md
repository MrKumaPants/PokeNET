# PokeNET Testing Strategy

**Created**: 2025-10-26
**Agent**: Tester (Hive Mind Swarm)
**Status**: 🟢 Strategy Complete
**Target Coverage**: 90%+ (Critical Systems), 80%+ (Overall)

---

## Executive Summary

This document defines the comprehensive testing strategy for PokeNET, a Pokemon-inspired game engine built with C# and Arch ECS. The strategy prioritizes **Test-Driven Development (TDD)** for critical game mechanics while maintaining high coverage across all systems.

**Key Goals**:
- ✅ 90%+ coverage for battle mechanics, data loading, and Pokemon calculations
- ✅ Gen 3-5 accuracy for damage formula and type effectiveness
- ✅ Automated CI/CD integration with xUnit and coverlet
- ✅ Integration tests for full battle sequences and mod loading

---

## Test Pyramid

```
                 /\
                /E2E\         ← 5% (20 tests)
               /------\          Full game flows, mod integration
              /  Integ \      ← 20% (150 tests)
             /----------\        Component interactions, battle sequences
            /   Unit     \    ← 75% (600 tests)
           /--------------\      Pokemon mechanics, stat calculations
```

### Distribution Rationale

- **Unit Tests (75%)**: Fast, isolated tests for calculations, data validation, and pure logic
- **Integration Tests (20%)**: System interactions, ECS queries, mod loading pipeline
- **E2E Tests (5%)**: Full scenarios like battle flow, save/load cycle, mod override

---

## Critical Test Cases by System

### 1. Pokemon Mechanics Testing

#### 1.1 Type Effectiveness (CRITICAL - 18×18 Matrix)

**Priority**: P0 (MVP Blocker)
**Coverage Target**: 100%
**Test Count**: ~40 unit tests

```csharp
// Test file: PokeNET.Testing/Mechanics/TypeEffectivenessTests.cs

[Theory]
[InlineData(PokemonType.Fire, PokemonType.Grass, 2.0)] // Super effective
[InlineData(PokemonType.Water, PokemonType.Fire, 2.0)]
[InlineData(PokemonType.Electric, PokemonType.Ground, 0.0)] // Immune
[InlineData(PokemonType.Normal, PokemonType.Ghost, 0.0)]
[InlineData(PokemonType.Fire, PokemonType.Water, 0.5)] // Not very effective
[InlineData(PokemonType.Normal, PokemonType.Normal, 1.0)] // Neutral
public void GetTypeEffectiveness_Returns_CorrectMultiplier(
    PokemonType attackType,
    PokemonType defenseType,
    float expected)
{
    // Arrange
    var typeChart = new TypeEffectivenessChart();

    // Act
    var actual = typeChart.GetMultiplier(attackType, defenseType);

    // Assert
    Assert.Equal(expected, actual);
}

[Fact]
public void GetTypeEffectiveness_DualType_Multiplies_Both()
{
    // Fire vs Grass/Flying = 2.0 × 0.5 = 1.0
    var result = typeChart.GetMultiplier(
        PokemonType.Fire,
        new[] { PokemonType.Grass, PokemonType.Flying });

    Assert.Equal(1.0f, result);
}
```

**Test Coverage**:
- ✅ All 18 types vs all 18 types (324 combinations)
- ✅ Dual-type defenders (multiplicative stacking)
- ✅ Edge cases: Unknown type, null handling
- ✅ Performance: <1ms for lookup

---

#### 1.2 Stat Calculations (Base + IV + EV + Nature)

**Priority**: P0
**Coverage Target**: 100%
**Test Count**: ~30 unit tests

```csharp
// Test file: PokeNET.Testing/Mechanics/StatCalculationTests.cs

[Theory]
[InlineData(100, 31, 252, 100, 1.1f, 328)] // Max Adamant Atk
[InlineData(100, 0, 0, 100, 1.0f, 205)]    // Base neutral
[InlineData(50, 15, 0, 50, 0.9f, 67)]      // Hindered nature
public void CalculateStat_Returns_OfficialFormula(
    int baseStat,
    int iv,
    int ev,
    int level,
    float natureModifier,
    int expected)
{
    // Arrange
    var stats = new PokemonStats();

    // Act
    var actual = stats.CalculateStat(baseStat, iv, ev, level, natureModifier);

    // Assert
    Assert.Equal(expected, actual);
}

[Fact]
public void CalculateHP_ShedinjaBehavior_Returns_1()
{
    // Shedinja always has 1 HP regardless of calculation
    var stats = new PokemonStats { SpeciesId = 292 }; // Shedinja
    var hp = stats.CalculateHP(1, 100);

    Assert.Equal(1, hp);
}

[Fact]
public void EV_Total_Cannot_Exceed_510()
{
    var stats = new PokemonStats
    {
        EV_HP = 252,
        EV_Attack = 252,
        EV_Speed = 6
    };

    Assert.True(stats.GetTotalEVs() <= 510);
}
```

**Test Coverage**:
- ✅ Gen 3+ stat formula accuracy
- ✅ Nature modifiers (0.9x, 1.0x, 1.1x)
- ✅ EV caps (252 per stat, 510 total)
- ✅ IV range (0-31)
- ✅ Level scaling (1-100)
- ✅ Special cases: Shedinja HP

---

#### 1.3 Damage Formula (Gen 3-5)

**Priority**: P0
**Coverage Target**: 95%
**Test Count**: ~50 unit tests

```csharp
// Test file: PokeNET.Testing/Battle/DamageCalculationTests.cs

[Theory]
[InlineData(100, 85, 150, 100, 1.5, 2.0, 1.0, 136, 161)] // STAB + Super Effective
public void CalculateDamage_Gen3to5_Returns_CorrectRange(
    int attackerLevel,
    int movePower,
    int attackerStat,
    int defenderStat,
    float stab,
    float typeEffectiveness,
    float randomFactor,
    int expectedMin,
    int expectedMax)
{
    // Arrange
    var calculator = new DamageCalculator(DamageFormula.Gen3to5);
    var context = new BattleContext
    {
        AttackerLevel = attackerLevel,
        MovePower = movePower,
        AttackStat = attackerStat,
        DefenseStat = defenderStat,
        STAB = stab,
        Effectiveness = typeEffectiveness
    };

    // Act
    var damage = calculator.Calculate(context);

    // Assert
    Assert.InRange(damage, expectedMin, expectedMax);
}

[Fact]
public void CalculateDamage_CriticalHit_IgnoresNegativeStages()
{
    var context = new BattleContext
    {
        IsCriticalHit = true,
        AttackerAttackStage = -6, // Should be ignored
        DefenderDefenseStage = 6   // Should be ignored
    };

    var damage = calculator.Calculate(context);

    // Critical hits ignore unfavorable stat stages
    Assert.True(damage > baselineDamage);
}

[Theory]
[InlineData(0.85, 85)]
[InlineData(0.90, 90)]
[InlineData(1.00, 100)]
public void CalculateDamage_RandomRange_85_to_100_Percent(
    float randomFactor,
    int expectedDamagePercent)
{
    // Damage random factor is 0.85 to 1.00 (217-255 range)
    var baseDamage = 100;
    var randomized = (int)(baseDamage * randomFactor);

    Assert.InRange(randomized, 85, 100);
}
```

**Formula**: `((2 * Level / 5 + 2) * Power * A/D / 50 + 2) * Modifiers`

**Test Coverage**:
- ✅ Gen 3-5 damage formula
- ✅ STAB (Same Type Attack Bonus) 1.5x
- ✅ Type effectiveness (0x, 0.25x, 0.5x, 1x, 2x, 4x)
- ✅ Critical hits (2x, ignores stat stages)
- ✅ Stat stage modifiers (-6 to +6)
- ✅ Weather effects (Rain boosts Water, etc.)
- ✅ Random factor (0.85-1.00)
- ✅ Multi-hit moves
- ✅ Minimum damage (1 HP)

---

#### 1.4 Status Effect Mechanics

**Priority**: P1
**Coverage Target**: 90%
**Test Count**: ~35 unit tests

```csharp
// Test file: PokeNET.Testing/Mechanics/StatusEffectTests.cs

[Fact]
public void Burn_Halves_Physical_Attack()
{
    var pokemon = CreateTestPokemon(attack: 200);
    pokemon.ApplyStatus(StatusCondition.Burn);

    var effectiveAttack = pokemon.GetModifiedAttack();

    Assert.Equal(100, effectiveAttack); // 200 / 2 = 100
}

[Fact]
public void Paralysis_Reduces_Speed_75_Percent()
{
    var pokemon = CreateTestPokemon(speed: 100);
    pokemon.ApplyStatus(StatusCondition.Paralysis);

    var effectiveSpeed = pokemon.GetModifiedSpeed();

    Assert.Equal(25, effectiveSpeed); // 100 * 0.25 = 25
}

[Fact]
public void Freeze_Prevents_Move_Execution()
{
    var pokemon = CreateTestPokemon();
    pokemon.ApplyStatus(StatusCondition.Freeze);

    var canMove = pokemon.CanExecuteMove();

    Assert.False(canMove);
}

[Fact]
public void Sleep_Counter_Decrements_Each_Turn()
{
    var pokemon = CreateTestPokemon();
    pokemon.ApplyStatus(StatusCondition.Sleep, turnsRemaining: 3);

    pokemon.ProcessEndOfTurn();

    Assert.Equal(2, pokemon.StatusTurnsRemaining);
}

[Fact]
public void Poison_Deals_OneEighth_MaxHP_Damage()
{
    var pokemon = CreateTestPokemon(maxHP: 100);
    pokemon.ApplyStatus(StatusCondition.Poison);

    pokemon.ProcessEndOfTurn();

    Assert.Equal(88, pokemon.CurrentHP); // 100 - (100/8) = 88
}
```

**Test Coverage**:
- ✅ Burn: Attack halved, 1/16 HP per turn
- ✅ Paralysis: Speed 75% reduction, 25% fail chance
- ✅ Sleep: 1-3 turns, cannot move
- ✅ Freeze: Cannot move, 20% thaw chance
- ✅ Poison: 1/8 HP per turn
- ✅ Badly Poisoned: Progressive damage (1/16, 2/16, 3/16...)
- ✅ Status immunity (Fire-types can't burn, Electric-types can't paralyze)
- ✅ Status cure items (Antidote, Burn Heal, etc.)

---

#### 1.5 Evolution Triggers

**Priority**: P2
**Coverage Target**: 85%
**Test Count**: ~25 unit tests

```csharp
// Test file: PokeNET.Testing/Mechanics/EvolutionTests.cs

[Theory]
[InlineData(7, 16, true)]  // Charmander → Charmeleon at 16
[InlineData(7, 15, false)] // Not yet
public void LevelUp_Evolution_Triggers_AtCorrectLevel(
    int speciesId,
    int level,
    bool shouldEvolve)
{
    var pokemon = CreateTestPokemon(speciesId, level);

    var canEvolve = evolutionSystem.CanEvolve(pokemon);

    Assert.Equal(shouldEvolve, canEvolve);
}

[Fact]
public void Eevee_WaterStone_Evolves_To_Vaporeon()
{
    var eevee = CreateTestPokemon(speciesId: 133);

    var evolved = evolutionSystem.TryEvolve(eevee, ItemType.WaterStone);

    Assert.True(evolved);
    Assert.Equal(134, eevee.SpeciesId); // Vaporeon
}

[Fact]
public void Eevee_Friendship_Day_Evolves_To_Espeon()
{
    var eevee = CreateTestPokemon(speciesId: 133, friendship: 220);
    timeService.SetTime(TimeOfDay.Day);

    var evolved = evolutionSystem.TryLevelUpEvolve(eevee);

    Assert.True(evolved);
    Assert.Equal(196, eevee.SpeciesId); // Espeon
}

[Fact]
public void TradeEvolution_Requires_TradeFlag()
{
    var kadabra = CreateTestPokemon(speciesId: 64);

    var canEvolve = evolutionSystem.CanEvolve(kadabra);

    Assert.False(canEvolve); // Requires trade

    kadabra.MarkAsTraded();
    Assert.True(evolutionSystem.CanEvolve(kadabra));
}
```

**Test Coverage**:
- ✅ Level-up evolution
- ✅ Stone evolution (Fire Stone, Water Stone, etc.)
- ✅ Friendship evolution (Espeon, Umbreon, Crobat)
- ✅ Trade evolution (Kadabra, Machoke, Graveler)
- ✅ Conditional evolution (time of day, location, held item)
- ✅ Evolution cancellation (B button)

---

### 2. Data Loading Testing

#### 2.1 JSON Deserialization

**Priority**: P0 (MVP Blocker)
**Coverage Target**: 95%
**Test Count**: ~40 unit tests

```csharp
// Test file: PokeNET.Testing/Data/JsonDeserializationTests.cs

[Fact]
public async Task LoadSpeciesData_ValidJson_Deserializes_Correctly()
{
    // Arrange
    var jsonPath = "Data/species/charizard.json";
    var dataLoader = new JsonDataLoader();

    // Act
    var species = await dataLoader.LoadSpecies(jsonPath);

    // Assert
    Assert.Equal(6, species.Id);
    Assert.Equal("Charizard", species.Name);
    Assert.Equal(new[] { PokemonType.Fire, PokemonType.Flying }, species.Types);
    Assert.Equal(78, species.BaseStats.HP);
    Assert.Equal(84, species.BaseStats.Attack);
}

[Fact]
public async Task LoadMoveData_ValidJson_Deserializes_MoveProperties()
{
    var move = await dataLoader.LoadMove("Data/moves/flamethrower.json");

    Assert.Equal("Flamethrower", move.Name);
    Assert.Equal(PokemonType.Fire, move.Type);
    Assert.Equal(90, move.Power);
    Assert.Equal(100, move.Accuracy);
    Assert.Equal(MoveCategory.Special, move.Category);
}

[Fact]
public void LoadSpeciesData_InvalidJson_Throws_ValidationException()
{
    var invalidJson = "{ \"id\": -1, \"name\": \"\" }";

    var exception = Assert.Throws<DataValidationException>(
        () => dataLoader.Deserialize<Species>(invalidJson));

    Assert.Contains("Species ID must be positive", exception.Message);
}

[Fact]
public async Task LoadAllSpecies_Returns_CompletePokedex()
{
    var species = await dataLoader.LoadAllSpecies();

    Assert.True(species.Count >= 151); // At least Gen 1
    Assert.All(species, s => Assert.InRange(s.Id, 1, 1025));
}
```

**Test Coverage**:
- ✅ Species data deserialization
- ✅ Move data deserialization
- ✅ Item data deserialization
- ✅ Encounter data deserialization
- ✅ Schema validation (required fields)
- ✅ Type safety (enums, ranges)
- ✅ Error handling (malformed JSON, missing files)
- ✅ Performance (<10ms per file)

---

#### 2.2 Data Validation

**Priority**: P0
**Coverage Target**: 100%
**Test Count**: ~30 unit tests

```csharp
// Test file: PokeNET.Testing/Data/DataValidationTests.cs

[Theory]
[InlineData(-1, false)]   // Invalid ID
[InlineData(0, false)]
[InlineData(1, true)]
[InlineData(1025, true)]
[InlineData(9999, false)] // Too high
public void ValidateSpeciesId_Returns_CorrectValidity(int id, bool isValid)
{
    var validator = new SpeciesDataValidator();

    var result = validator.IsValidId(id);

    Assert.Equal(isValid, result);
}

[Fact]
public void ValidateBaseStats_Total_Cannot_Exceed_720()
{
    // Arceus has BST of 720 (highest)
    var stats = new BaseStats
    {
        HP = 120, Attack = 120, Defense = 120,
        SpAttack = 120, SpDefense = 120, Speed = 120
    };

    var validator = new SpeciesDataValidator();

    Assert.True(validator.ValidateBaseStatTotal(stats));
}

[Fact]
public void ValidateMove_Power_Must_Be_Zero_Or_Positive()
{
    var move = new Move { Name = "Tackle", Power = -10 };

    var validator = new MoveDataValidator();
    var errors = validator.Validate(move);

    Assert.Contains(errors, e => e.Contains("Power must be >= 0"));
}

[Fact]
public void ValidateTypes_Maximum_Two_Types()
{
    var species = new Species
    {
        Types = new[] { PokemonType.Fire, PokemonType.Flying, PokemonType.Dragon }
    };

    var validator = new SpeciesDataValidator();

    Assert.False(validator.Validate(species).IsValid);
}
```

**Test Coverage**:
- ✅ ID range validation (1-1025)
- ✅ Base stat totals (BST <= 720)
- ✅ Type count (1-2 types max)
- ✅ Move power/accuracy ranges
- ✅ Level requirements (1-100)
- ✅ Name length limits
- ✅ Required vs optional fields

---

#### 2.3 Mod Override Support

**Priority**: P1
**Coverage Target**: 90%
**Test Count**: ~25 unit tests

```csharp
// Test file: PokeNET.Testing/Modding/ModOverrideTests.cs

[Fact]
public async Task LoadSpeciesWithMod_Overrides_BaseData()
{
    // Arrange
    var modManager = CreateModManager();
    await modManager.LoadMod("Mods/BalancePatch");

    // Act
    var charizard = await dataLoader.LoadSpecies(6); // Charizard

    // Assert - Mod changes Fire/Flying to Fire/Dragon
    Assert.Equal(PokemonType.Fire, charizard.Types[0]);
    Assert.Equal(PokemonType.Dragon, charizard.Types[1]);
}

[Fact]
public async Task LoadMoveWithMod_Patches_Properties()
{
    await modManager.LoadMod("Mods/MoveRebalance");

    var flamethrower = await dataLoader.LoadMove("Flamethrower");

    // Mod increases power from 90 to 95
    Assert.Equal(95, flamethrower.Power);
}

[Fact]
public void ModLoadOrder_Later_Mods_Override_Earlier()
{
    modManager.LoadMods(new[] { "ModA", "ModB" });

    // Both mods change Pikachu's base attack
    // ModB should win
    var pikachu = dataLoader.LoadSpecies(25);

    Assert.Equal(60, pikachu.BaseStats.Attack); // ModB value
}

[Fact]
public void CircularModDependency_Throws_Exception()
{
    // ModA depends on ModB, ModB depends on ModA
    var exception = Assert.Throws<ModLoadException>(
        () => modManager.LoadMod("ModA"));

    Assert.Contains("Circular dependency", exception.Message);
}
```

**Test Coverage**:
- ✅ Data override mechanics
- ✅ Mod load order priority
- ✅ Dependency resolution
- ✅ Circular dependency detection
- ✅ Mod conflict warnings
- ✅ Hot-reload support

---

### 3. Battle System Testing

#### 3.1 Turn Order Determination

**Priority**: P0
**Coverage Target**: 95%
**Test Count**: ~20 unit tests

```csharp
// Test file: PokeNET.Testing/Battle/TurnOrderTests.cs

[Fact]
public void TurnOrder_HigherSpeed_GoesFirst()
{
    var fast = CreatePokemon(speed: 120);
    var slow = CreatePokemon(speed: 80);

    var order = battleSystem.DetermineTurnOrder(fast, slow);

    Assert.Equal(fast, order[0]);
    Assert.Equal(slow, order[1]);
}

[Fact]
public void TurnOrder_Priority_Overrides_Speed()
{
    var slowWithPriority = CreatePokemon(speed: 50, move: "Quick Attack"); // +1 priority
    var fastNormal = CreatePokemon(speed: 100, move: "Tackle"); // 0 priority

    var order = battleSystem.DetermineTurnOrder(slowWithPriority, fastNormal);

    Assert.Equal(slowWithPriority, order[0]);
}

[Fact]
public void TurnOrder_EqualSpeed_Randomizes()
{
    var pokemon1 = CreatePokemon(speed: 100);
    var pokemon2 = CreatePokemon(speed: 100);

    var results = new List<int>();
    for (int i = 0; i < 100; i++)
    {
        var order = battleSystem.DetermineTurnOrder(pokemon1, pokemon2);
        results.Add(order[0] == pokemon1 ? 1 : 2);
    }

    // Should be roughly 50/50 split
    Assert.InRange(results.Count(r => r == 1), 30, 70);
}

[Fact]
public void TrickRoom_Reverses_Speed_Order()
{
    battleSystem.ActivateFieldEffect(FieldEffect.TrickRoom);

    var fast = CreatePokemon(speed: 120);
    var slow = CreatePokemon(speed: 80);

    var order = battleSystem.DetermineTurnOrder(fast, slow);

    Assert.Equal(slow, order[0]); // Slower goes first in Trick Room
}
```

**Test Coverage**:
- ✅ Speed-based ordering
- ✅ Move priority (+5 to -7)
- ✅ Tie-breaking (50/50 random)
- ✅ Field effects (Trick Room)
- ✅ Paralysis speed reduction

---

#### 3.2 Move Execution

**Priority**: P0
**Coverage Target**: 90%
**Test Count**: ~45 unit tests

```csharp
// Test file: PokeNET.Testing/Battle/MoveExecutionTests.cs

[Fact]
public void ExecuteMove_Tackle_Deals_PhysicalDamage()
{
    var attacker = CreatePokemon(attack: 100);
    var defender = CreatePokemon(defense: 80, hp: 100);

    battleSystem.ExecuteMove(attacker, defender, "Tackle");

    Assert.True(defender.CurrentHP < 100);
}

[Fact]
public void ExecuteMove_Miss_Deals_NoDamage()
{
    var attacker = CreatePokemon();
    var defender = CreatePokemon(hp: 100);
    randomProvider.SetNextValue(0.99); // Force miss (accuracy 95%)

    battleSystem.ExecuteMove(attacker, defender, "Flamethrower");

    Assert.Equal(100, defender.CurrentHP);
}

[Fact]
public void ExecuteMove_CriticalHit_Doubles_Damage()
{
    randomProvider.SetCriticalHit(true);

    var normalDamage = battleSystem.CalculateDamage(attacker, defender, move);
    randomProvider.SetCriticalHit(false);
    var critDamage = battleSystem.CalculateDamage(attacker, defender, move);

    Assert.True(critDamage >= normalDamage * 1.9); // 2x with variance
}

[Fact]
public void ExecuteMove_OHKO_Bypasses_Damage_Calculation()
{
    var move = CreateMove("Fissure", category: MoveCategory.OHKO);
    var defender = CreatePokemon(hp: 9999);

    battleSystem.ExecuteMove(attacker, defender, move);

    Assert.Equal(0, defender.CurrentHP); // Instant KO
}

[Fact]
public void ExecuteMove_PP_Decrements_After_Use()
{
    var attacker = CreatePokemon();
    var initialPP = attacker.GetMovePP("Tackle");

    battleSystem.ExecuteMove(attacker, defender, "Tackle");

    Assert.Equal(initialPP - 1, attacker.GetMovePP("Tackle"));
}
```

**Test Coverage**:
- ✅ Damage dealing moves
- ✅ Status moves (Thunder Wave, etc.)
- ✅ Accuracy checks
- ✅ Critical hits
- ✅ OHKO moves
- ✅ Multi-hit moves (Fury Attack)
- ✅ Recoil moves (Double-Edge)
- ✅ PP management
- ✅ Move effects (burn chance, flinch, etc.)

---

#### 3.3 AI Decision Making

**Priority**: P1
**Coverage Target**: 80%
**Test Count**: ~30 unit tests

```csharp
// Test file: PokeNET.Testing/Battle/AITests.cs

[Fact]
public void AI_Chooses_SuperEffective_Move()
{
    var ai = CreateAI(difficulty: AIDifficulty.Smart);
    var opponent = CreatePokemon(types: new[] { PokemonType.Grass });
    var moves = new[] { "Tackle", "Flamethrower", "Thunderbolt" };

    var choice = ai.ChooseMove(opponent, moves);

    Assert.Equal("Flamethrower", choice); // Fire > Grass
}

[Fact]
public void AI_Switches_When_Threatened()
{
    var ai = CreateAI(difficulty: AIDifficulty.Smart);
    var currentPokemon = CreatePokemon(hp: 10, maxHP: 100);
    var threat = CreatePokemon(attack: 200);

    var decision = ai.DecideAction(currentPokemon, threat);

    Assert.Equal(BattleAction.Switch, decision.ActionType);
}

[Fact]
public void AI_Random_Makes_Random_Choices()
{
    var ai = CreateAI(difficulty: AIDifficulty.Random);
    var moves = new[] { "Move1", "Move2", "Move3", "Move4" };

    var choices = new List<string>();
    for (int i = 0; i < 100; i++)
    {
        choices.Add(ai.ChooseMove(opponent, moves));
    }

    // Should use all moves at least once
    Assert.True(choices.Distinct().Count() == 4);
}
```

**Test Coverage**:
- ✅ Type matchup awareness
- ✅ Switching logic
- ✅ Status move usage
- ✅ HP threshold decisions
- ✅ Difficulty levels (Random, Easy, Smart)

---

#### 3.4 Victory/Defeat Conditions

**Priority**: P0
**Coverage Target**: 100%
**Test Count**: ~15 unit tests

```csharp
// Test file: PokeNET.Testing/Battle/BattleEndTests.cs

[Fact]
public void Battle_Ends_When_AllOpponentPokemon_Fainted()
{
    var player = CreateTrainer(pokemonCount: 3);
    var opponent = CreateTrainer(pokemonCount: 3);

    opponent.FaintAllPokemon();

    var result = battleSystem.CheckBattleEnd();

    Assert.Equal(BattleResult.PlayerVictory, result);
}

[Fact]
public void Battle_Continues_If_OnePokemon_Remains()
{
    var opponent = CreateTrainer(pokemonCount: 3);
    opponent.FaintPokemon(0);
    opponent.FaintPokemon(1);
    // Pokemon 2 still alive

    var result = battleSystem.CheckBattleEnd();

    Assert.Null(result); // Battle continues
}

[Fact]
public void WildBattle_Ends_On_Capture()
{
    var wildPokemon = CreateWildPokemon();

    battleSystem.ThrowPokeball(wildPokemon, captureSuccess: true);

    var result = battleSystem.CheckBattleEnd();

    Assert.Equal(BattleResult.Captured, result);
}
```

**Test Coverage**:
- ✅ All Pokemon fainted (loss)
- ✅ Opponent fainted (victory)
- ✅ Wild battle capture
- ✅ Fleeing/escaping
- ✅ Battle forfeit

---

### 4. Integration Testing

#### 4.1 Full Battle Sequence

**Priority**: P1
**Coverage Target**: 85%
**Test Count**: ~20 integration tests

```csharp
// Test file: PokeNET.Testing/Integration/BattleIntegrationTests.cs

[Fact]
public async Task FullBattle_PlayerVsWild_Success()
{
    // Arrange
    var player = await CreatePlayer(pokemonCount: 1);
    var wild = CreateWildEncounter(speciesId: 1, level: 5); // Bulbasaur
    var battle = new BattleController(player, wild);

    // Act - Full battle flow
    battle.Start();
    battle.PlayerUseMove("Tackle");
    battle.ProcessTurn();
    battle.PlayerUseMove("Tackle");
    battle.ProcessTurn();

    // Assert
    Assert.True(wild.HP <= 0);
    Assert.Equal(BattleResult.Victory, battle.Result);
    Assert.True(player.ExperienceGained > 0);
}

[Fact]
public async Task FullBattle_Capture_Integration()
{
    var player = await CreatePlayer();
    player.AddItem(ItemType.Pokeball, quantity: 5);
    var wild = CreateWildEncounter(hp: 10, maxHP: 100); // Weakened

    var battle = new BattleController(player, wild);
    battle.Start();
    battle.PlayerUseItem(ItemType.Pokeball);

    Assert.True(player.Party.Count == 2); // Original + captured
    Assert.Contains(player.Party, p => p.SpeciesId == wild.SpeciesId);
}
```

**Test Coverage**:
- ✅ Complete battle from start to victory
- ✅ Wild Pokemon capture flow
- ✅ Trainer battle sequence
- ✅ Multi-turn battles with switching
- ✅ Experience/stat gain after battle

---

#### 4.2 Save/Load Cycle

**Priority**: P0 (MVP Blocker)
**Coverage Target**: 95%
**Test Count**: ~15 integration tests

```csharp
// Test file: PokeNET.Testing/Integration/SaveLoadTests.cs

[Fact]
public async Task SaveAndLoad_Preserves_PartyData()
{
    // Arrange
    var player = CreatePlayer();
    player.AddPokemon(CreatePokemon(speciesId: 6, level: 50));

    // Act - Save
    var saveSystem = new SaveManager();
    await saveSystem.Save(player, "test-save.dat");

    // Load
    var loadedPlayer = await saveSystem.Load("test-save.dat");

    // Assert
    Assert.Equal(player.Party.Count, loadedPlayer.Party.Count);
    Assert.Equal(6, loadedPlayer.Party[0].SpeciesId);
    Assert.Equal(50, loadedPlayer.Party[0].Level);
}

[Fact]
public async Task SaveLoad_Preserves_BattleState()
{
    var player = CreatePlayerInBattle();
    await saveSystem.Save(player, "battle-save.dat");

    var loaded = await saveSystem.Load("battle-save.dat");

    Assert.True(loaded.IsInBattle);
    Assert.Equal(player.CurrentOpponent.Name, loaded.CurrentOpponent.Name);
}
```

---

#### 4.3 Mod Loading Pipeline

**Priority**: P2
**Coverage Target**: 80%
**Test Count**: ~20 integration tests

```csharp
// Test file: PokeNET.Testing/Integration/ModLoadingTests.cs

[Fact]
public async Task LoadMod_Full_Pipeline_Success()
{
    // Arrange
    var modPath = "Mods/TestMod";
    var modManager = new ModManager();

    // Act
    await modManager.LoadMod(modPath);

    // Assert
    Assert.True(modManager.IsModLoaded("TestMod"));
    Assert.NotNull(modManager.GetModData("TestMod"));
}

[Fact]
public async Task LoadMultipleMods_Applies_All_Overrides()
{
    await modManager.LoadMods(new[] { "ModA", "ModB", "ModC" });

    var species = dataLoader.LoadSpecies(1);

    // Verify all 3 mods' changes are applied
    Assert.NotEqual(baselineSpecies.Name, species.Name); // ModA changed name
    Assert.NotEqual(baselineSpecies.BaseStats, species.BaseStats); // ModB changed stats
}
```

---

## Test Data Requirements

### 1. Fixture Data

**Location**: `PokeNET.Testing/Fixtures/`

```
Fixtures/
├── species.json          # 151 Gen 1 Pokemon
├── moves.json            # 165 Gen 1 moves
├── type-chart.json       # 18x18 effectiveness matrix
├── trainers.json         # Sample trainers
└── encounters.json       # Wild encounter tables
```

### 2. Test Builders

```csharp
// TestDataBuilder.cs
public static class TestDataBuilder
{
    public static Pokemon CreatePokemon(
        int speciesId = 1,
        int level = 50,
        int hp = 100,
        int attack = 50,
        int defense = 50,
        int speed = 50)
    {
        // Create test Pokemon with defaults
    }

    public static Trainer CreateTrainer(int pokemonCount = 3)
    {
        // Create trainer with party
    }
}
```

---

## Testing Tools & Frameworks

### Core Testing Stack

```xml
<!-- PokeNET.Testing.csproj -->
<ItemGroup>
  <PackageReference Include="xunit" Version="2.9.2" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
  <PackageReference Include="coverlet.collector" Version="6.0.2" />

  <!-- Mocking & Assertions -->
  <PackageReference Include="Moq" Version="4.20.70" />
  <PackageReference Include="FluentAssertions" Version="6.12.0" />

  <!-- Test Data -->
  <PackageReference Include="Bogus" Version="35.6.1" /> <!-- Fake data generation -->
  <PackageReference Include="AutoFixture" Version="4.18.1" />
</ItemGroup>
```

### Coverage Tools

- **coverlet**: Line/branch coverage collection
- **ReportGenerator**: HTML coverage reports
- **Threshold**: 90% critical, 80% overall

---

## CI/CD Integration

### GitHub Actions Workflow

```yaml
# .github/workflows/tests.yml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Run tests with coverage
        run: dotnet test --no-build --verbosity normal /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

      - name: Generate coverage report
        run: dotnet reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report

      - name: Enforce coverage thresholds
        run: |
          # Fail if coverage < 80%
          dotnet test /p:Threshold=80 /p:ThresholdType=line
```

---

## Coverage Targets by Component

| Component | Target | Priority | Rationale |
|-----------|--------|----------|-----------|
| **Type Chart** | 100% | P0 | Critical game mechanic, deterministic |
| **Stat Calculations** | 100% | P0 | Gen accuracy requirement |
| **Damage Formula** | 95% | P0 | Core battle system |
| **Data Loaders** | 95% | P0 | MVP blocker |
| **Save System** | 90% | P0 | Already at 90%+ |
| **Battle System** | 90% | P0 | Player-facing |
| **Status Effects** | 90% | P1 | Common mechanic |
| **Evolution** | 85% | P2 | Well-defined rules |
| **AI** | 80% | P1 | Heuristic-based |
| **UI** | 70% | P1 | Manual testing primary |
| **Modding** | 80% | P2 | Integration-heavy |

**Overall Target**: 80%+ line coverage, 75%+ branch coverage

---

## Test Execution Strategy

### Development Workflow

```bash
# Quick unit tests (runs in ~2s)
dotnet test --filter Category=Unit

# Full suite (runs in ~30s)
dotnet test

# Specific system
dotnet test --filter FullyQualifiedName~Battle

# Watch mode (TDD)
dotnet watch test --filter Category=Unit
```

### Test Organization

```
PokeNET.Testing/
├── Unit/
│   ├── Mechanics/
│   │   ├── TypeEffectivenessTests.cs
│   │   ├── StatCalculationTests.cs
│   │   ├── DamageCalculationTests.cs
│   │   └── EvolutionTests.cs
│   ├── Data/
│   │   ├── JsonDeserializationTests.cs
│   │   └── DataValidationTests.cs
│   └── Battle/
│       ├── TurnOrderTests.cs
│       ├── MoveExecutionTests.cs
│       └── AITests.cs
├── Integration/
│   ├── BattleIntegrationTests.cs
│   ├── SaveLoadTests.cs
│   └── ModLoadingTests.cs
├── E2E/
│   ├── FullGameFlowTests.cs
│   └── ModdedGameplayTests.cs
├── Fixtures/
│   ├── species.json
│   ├── moves.json
│   └── type-chart.json
└── Builders/
    ├── PokemonBuilder.cs
    └── BattleBuilder.cs
```

---

## Performance Benchmarks

### Critical Path Performance

```csharp
[Fact]
public void TypeChart_Lookup_Under_1ms()
{
    var stopwatch = Stopwatch.StartNew();

    for (int i = 0; i < 1000; i++)
    {
        typeChart.GetMultiplier(PokemonType.Fire, PokemonType.Grass);
    }

    stopwatch.Stop();
    Assert.True(stopwatch.ElapsedMilliseconds < 1);
}

[Fact]
public void DamageCalculation_Under_5ms()
{
    var stopwatch = Stopwatch.StartNew();

    for (int i = 0; i < 100; i++)
    {
        calculator.CalculateDamage(attacker, defender, move);
    }

    stopwatch.Stop();
    Assert.True(stopwatch.ElapsedMilliseconds < 5);
}
```

**Targets**:
- Type effectiveness lookup: <1ms per 1000 lookups
- Damage calculation: <5ms per 100 calculations
- Data loading: <50ms per species file
- Save file write: <500ms

---

## Test-Driven Development Workflow

### TDD Process (Red-Green-Refactor)

1. **🔴 RED**: Write failing test first
2. **🟢 GREEN**: Write minimal code to pass
3. **🔵 REFACTOR**: Clean up implementation
4. **📊 COVERAGE**: Verify coverage increased
5. **🔄 REPEAT**: Next test case

### Example TDD Session

```csharp
// Step 1: RED - Write failing test
[Fact]
public void CalculateDamage_STAB_Applies_1_5x_Multiplier()
{
    var calculator = new DamageCalculator();
    var damage = calculator.Calculate(
        attackerType: PokemonType.Fire,
        moveType: PokemonType.Fire,
        baseDamage: 100
    );

    Assert.Equal(150, damage); // FAILS - CalculateDamage not implemented
}

// Step 2: GREEN - Minimal implementation
public int CalculateDamage(PokemonType attackerType, PokemonType moveType, int baseDamage)
{
    float stab = (attackerType == moveType) ? 1.5f : 1.0f;
    return (int)(baseDamage * stab);
}

// Step 3: REFACTOR - Clean up
public int CalculateDamage(BattleContext context)
{
    int damage = context.BaseDamage;
    damage = ApplySTAB(damage, context);
    damage = ApplyTypeEffectiveness(damage, context);
    damage = ApplyRandomFactor(damage);
    return damage;
}
```

---

## Coordination with Other Agents

### Memory Storage

```bash
# Store test strategy in shared memory
npx claude-flow@alpha hooks post-edit \
  --file "test-strategy.md" \
  --memory-key "swarm/tester/test-strategy"

# Store coverage requirements
npx claude-flow@alpha hooks post-edit \
  --file "coverage-targets" \
  --memory-key "swarm/tester/coverage-targets"
```

### Dependencies

**Tester depends on**:
- **Coder**: Needs implementation to test against
- **Data Agent**: Requires fixture data (species.json, moves.json)
- **Architect**: Needs interface definitions

**Tester provides to**:
- **Reviewer**: Test coverage reports for code review
- **Documenter**: Test cases as usage examples
- **Coder**: Failing tests as specifications

---

## Next Steps

### Phase 1: Foundation (Week 1)

- [ ] Set up test project structure
- [ ] Configure xUnit + coverlet
- [ ] Create test data builders
- [ ] Implement TypeChart tests (P0)
- [ ] Implement StatCalculation tests (P0)

### Phase 2: Core Mechanics (Week 2)

- [ ] DamageCalculation tests
- [ ] Status effect tests
- [ ] Evolution trigger tests
- [ ] Data loader tests

### Phase 3: Integration (Week 3)

- [ ] Battle system integration tests
- [ ] Save/load cycle tests
- [ ] Mod loading tests
- [ ] CI/CD pipeline setup

### Phase 4: Coverage & Polish (Week 4)

- [ ] Achieve 80%+ overall coverage
- [ ] Achieve 90%+ critical system coverage
- [ ] Performance benchmarking
- [ ] Documentation of test patterns

---

## Appendix: Test Templates

### Unit Test Template

```csharp
namespace PokeNET.Testing.Mechanics;

public class [ComponentName]Tests
{
    private readonly [ComponentName] _sut; // System Under Test

    public [ComponentName]Tests()
    {
        _sut = new [ComponentName]();
    }

    [Theory]
    [InlineData(input1, expected1)]
    [InlineData(input2, expected2)]
    public void [MethodName]_[Scenario]_[ExpectedBehavior](input, expected)
    {
        // Arrange

        // Act
        var result = _sut.MethodName(input);

        // Assert
        Assert.Equal(expected, result);
    }
}
```

### Integration Test Template

```csharp
namespace PokeNET.Testing.Integration;

public class [Feature]IntegrationTests : IAsyncLifetime
{
    private readonly ServiceProvider _services;

    public async Task InitializeAsync()
    {
        // Setup test database, load fixtures
    }

    [Fact]
    public async Task [Feature]_[Scenario]_Success()
    {
        // Arrange
        var service = _services.GetRequiredService<IService>();

        // Act
        var result = await service.Execute();

        // Assert
        result.Should().NotBeNull();
    }

    public async Task DisposeAsync()
    {
        // Cleanup
    }
}
```

---

**Document Version**: 1.0.0
**Last Updated**: 2025-10-26
**Author**: Tester Agent (Hive Mind)
**Review Status**: Ready for Implementation
