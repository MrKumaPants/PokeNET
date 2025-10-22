namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Represents the core statistics of an entity (typically a creature/Pokemon).
/// This component follows the Single Responsibility Principle by only handling stat data.
/// </summary>
public struct Stats
{
    /// <summary>
    /// Attack power - affects physical damage output.
    /// </summary>
    public int Attack { get; set; }

    /// <summary>
    /// Defense - reduces physical damage taken.
    /// </summary>
    public int Defense { get; set; }

    /// <summary>
    /// Special Attack - affects special/magical damage output.
    /// </summary>
    public int SpecialAttack { get; set; }

    /// <summary>
    /// Special Defense - reduces special/magical damage taken.
    /// </summary>
    public int SpecialDefense { get; set; }

    /// <summary>
    /// Speed - affects turn order and evasion.
    /// </summary>
    public int Speed { get; set; }

    /// <summary>
    /// Current level of the entity.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Initializes a new stats component with all values.
    /// </summary>
    public Stats(int attack, int defense, int specialAttack, int specialDefense, int speed, int level = 1)
    {
        Attack = attack;
        Defense = defense;
        SpecialAttack = specialAttack;
        SpecialDefense = specialDefense;
        Speed = speed;
        Level = level;
    }

    /// <summary>
    /// Calculates the total base stat value (sum of all stats).
    /// </summary>
    public readonly int TotalBaseStats => Attack + Defense + SpecialAttack + SpecialDefense + Speed;

    public override readonly string ToString() =>
        $"Lv.{Level} ATK:{Attack} DEF:{Defense} SPATK:{SpecialAttack} SPDEF:{SpecialDefense} SPD:{Speed}";
}
