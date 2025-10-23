namespace PokeNET.Audio.Abstractions;

/// <summary>
/// Provides random number generation for procedural audio generation.
/// SOLID PRINCIPLE: Dependency Inversion - Abstracts random number generation.
/// SOLID PRINCIPLE: Interface Segregation - Focused interface for randomness.
/// </summary>
/// <remarks>
/// This interface enables testable, deterministic procedural music generation
/// by allowing injection of custom random providers (e.g., seeded for replay,
/// or cryptographic for unpredictable sequences).
/// </remarks>
public interface IRandomProvider
{
    /// <summary>
    /// Generates a random integer between 0 and maxValue (exclusive).
    /// </summary>
    /// <param name="maxValue">Upper bound (exclusive)</param>
    /// <returns>Random integer in range [0, maxValue)</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxValue is negative.</exception>
    int Next(int maxValue);

    /// <summary>
    /// Generates a random integer within a specified range.
    /// </summary>
    /// <param name="minValue">Lower bound (inclusive)</param>
    /// <param name="maxValue">Upper bound (exclusive)</param>
    /// <returns>Random integer in range [minValue, maxValue)</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when minValue > maxValue.</exception>
    int Next(int minValue, int maxValue);

    /// <summary>
    /// Generates a random double between 0.0 and 1.0.
    /// </summary>
    /// <returns>Random double in range [0.0, 1.0)</returns>
    double NextDouble();

    /// <summary>
    /// Generates a random float between 0.0 and 1.0.
    /// </summary>
    /// <returns>Random float in range [0.0, 1.0)</returns>
    float NextFloat();

    /// <summary>
    /// Generates a random float within a specified range.
    /// </summary>
    /// <param name="minValue">Lower bound (inclusive)</param>
    /// <param name="maxValue">Upper bound (exclusive)</param>
    /// <returns>Random float in range [minValue, maxValue)</returns>
    float NextFloat(float minValue, float maxValue);

    /// <summary>
    /// Generates a random boolean value.
    /// </summary>
    /// <returns>True or false with equal probability.</returns>
    bool NextBool();

    /// <summary>
    /// Generates a random boolean with a specified probability.
    /// </summary>
    /// <param name="probability">Probability of returning true (0.0 to 1.0).</param>
    /// <returns>True with the specified probability, false otherwise.</returns>
    bool NextBool(float probability);

    /// <summary>
    /// Selects a random element from a collection.
    /// </summary>
    /// <typeparam name="T">Type of elements in the collection.</typeparam>
    /// <param name="items">Collection to select from.</param>
    /// <returns>A randomly selected element.</returns>
    /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
    /// <exception cref="ArgumentException">Thrown when items is empty.</exception>
    T Choose<T>(IReadOnlyList<T> items);

    /// <summary>
    /// Selects multiple random elements from a collection without replacement.
    /// </summary>
    /// <typeparam name="T">Type of elements in the collection.</typeparam>
    /// <param name="items">Collection to select from.</param>
    /// <param name="count">Number of elements to select.</param>
    /// <returns>List of randomly selected elements.</returns>
    /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
    /// <exception cref="ArgumentException">Thrown when count > items.Count.</exception>
    IReadOnlyList<T> Choose<T>(IReadOnlyList<T> items, int count);

    /// <summary>
    /// Shuffles a collection in place using Fisher-Yates algorithm.
    /// </summary>
    /// <typeparam name="T">Type of elements in the collection.</typeparam>
    /// <param name="items">Collection to shuffle.</param>
    /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
    void Shuffle<T>(IList<T> items);
}
