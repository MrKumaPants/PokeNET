using System;

namespace PokeNET.Core.ECS.Components;

/// <summary>
/// Component representing a Pokémon Trainer (both player and NPCs).
/// Contains core trainer information including identity, money, and badges.
/// </summary>
public sealed class Trainer
{
    /// <summary>
    /// Unique identifier for this trainer.
    /// </summary>
    public Guid TrainerId { get; init; }

    /// <summary>
    /// Display name of the trainer.
    /// </summary>
    public string TrainerName { get; private set; }

    /// <summary>
    /// Trainer class/type (e.g., "Pokemon Trainer", "Gym Leader", "Elite Four", etc.).
    /// </summary>
    public string TrainerClass { get; init; }

    /// <summary>
    /// Current amount of money the trainer possesses.
    /// </summary>
    public int Money { get; private set; }

    /// <summary>
    /// 8-bit flag representing owned badges (1 bit per badge, 8 badges total).
    /// Each bit corresponds to a specific gym badge.
    /// </summary>
    public byte BadgesOwned { get; private set; }

    /// <summary>
    /// Indicates whether this trainer is the player character.
    /// </summary>
    public bool IsPlayer { get; init; }

    /// <summary>
    /// Gender of the trainer.
    /// </summary>
    public TrainerGender Gender { get; init; }

    /// <summary>
    /// Initializes a new trainer component.
    /// </summary>
    /// <param name="trainerId">Unique trainer identifier.</param>
    /// <param name="trainerName">Display name of the trainer.</param>
    /// <param name="trainerClass">Trainer class/type.</param>
    /// <param name="isPlayer">Whether this is the player character.</param>
    /// <param name="gender">Trainer's gender.</param>
    public Trainer(
        Guid trainerId,
        string trainerName,
        string trainerClass,
        bool isPlayer,
        TrainerGender gender
    )
    {
        TrainerId = trainerId;
        TrainerName = trainerName ?? throw new ArgumentNullException(nameof(trainerName));
        TrainerClass = trainerClass ?? throw new ArgumentNullException(nameof(trainerClass));
        IsPlayer = isPlayer;
        Gender = gender;
        Money = isPlayer ? 3000 : 0; // Starting money for player
        BadgesOwned = 0;
    }

    /// <summary>
    /// Adds money to the trainer's current balance.
    /// </summary>
    public void AddMoney(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));
        Money = Math.Min(Money + amount, 999999); // Cap at 999,999
    }

    /// <summary>
    /// Removes money from the trainer's balance.
    /// </summary>
    /// <returns>True if successful, false if insufficient funds.</returns>
    public bool RemoveMoney(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));
        if (Money < amount)
            return false;
        Money -= amount;
        return true;
    }

    /// <summary>
    /// Awards a specific gym badge to the trainer.
    /// </summary>
    /// <param name="badgeNumber">Badge number (0-7).</param>
    public void AwardBadge(int badgeNumber)
    {
        if (badgeNumber < 0 || badgeNumber > 7)
            throw new ArgumentOutOfRangeException(nameof(badgeNumber), "Badge number must be 0-7");
        BadgesOwned |= (byte)(1 << badgeNumber);
    }

    /// <summary>
    /// Checks if the trainer owns a specific badge.
    /// </summary>
    public bool HasBadge(int badgeNumber)
    {
        if (badgeNumber < 0 || badgeNumber > 7)
            return false;
        return (BadgesOwned & (1 << badgeNumber)) != 0;
    }

    /// <summary>
    /// Gets the total number of badges owned.
    /// </summary>
    public int BadgeCount => System.Numerics.BitOperations.PopCount(BadgesOwned);
}

/// <summary>
/// Represents trainer gender.
/// </summary>
public enum TrainerGender
{
    Male,
    Female,
    NonBinary,
}
