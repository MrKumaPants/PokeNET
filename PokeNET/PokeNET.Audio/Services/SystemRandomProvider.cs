using PokeNET.Audio.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PokeNET.Audio.Services;

/// <summary>
/// Thread-safe implementation of IRandomProvider using System.Random.
/// SOLID PRINCIPLE: Dependency Inversion - Concrete implementation of random abstraction.
/// SOLID PRINCIPLE: Single Responsibility - Focuses solely on random number generation.
/// </summary>
/// <remarks>
/// Uses ThreadLocal&lt;Random&gt; to ensure thread-safety without locking overhead.
/// Each thread gets its own Random instance with a unique seed derived from
/// the global Random instance.
/// </remarks>
public class SystemRandomProvider : IRandomProvider
{
    private static readonly Random _globalRandom = new Random();
    private static readonly object _globalLock = new object();

    private readonly ThreadLocal<Random> _threadLocalRandom = new ThreadLocal<Random>(() =>
    {
        lock (_globalLock)
        {
            return new Random(_globalRandom.Next());
        }
    });

    private Random Random => _threadLocalRandom.Value ?? throw new InvalidOperationException("ThreadLocal Random not initialized");

    /// <summary>
    /// Generates a random integer between 0 and maxValue (exclusive).
    /// </summary>
    /// <param name="maxValue">Upper bound (exclusive)</param>
    /// <returns>Random integer in range [0, maxValue)</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxValue is negative.</exception>
    public int Next(int maxValue)
    {
        if (maxValue < 0)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be non-negative");

        return Random.Next(maxValue);
    }

    /// <summary>
    /// Generates a random integer within a specified range.
    /// </summary>
    /// <param name="minValue">Lower bound (inclusive)</param>
    /// <param name="maxValue">Upper bound (exclusive)</param>
    /// <returns>Random integer in range [minValue, maxValue)</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when minValue > maxValue.</exception>
    public int Next(int minValue, int maxValue)
    {
        if (minValue > maxValue)
            throw new ArgumentOutOfRangeException(nameof(minValue), "minValue must be less than or equal to maxValue");

        return Random.Next(minValue, maxValue);
    }

    /// <summary>
    /// Generates a random double between 0.0 and 1.0.
    /// </summary>
    /// <returns>Random double in range [0.0, 1.0)</returns>
    public double NextDouble()
    {
        return Random.NextDouble();
    }

    /// <summary>
    /// Generates a random float between 0.0 and 1.0.
    /// </summary>
    /// <returns>Random float in range [0.0, 1.0)</returns>
    public float NextFloat()
    {
        return (float)Random.NextDouble();
    }

    /// <summary>
    /// Generates a random float within a specified range.
    /// </summary>
    /// <param name="minValue">Lower bound (inclusive)</param>
    /// <param name="maxValue">Upper bound (exclusive)</param>
    /// <returns>Random float in range [minValue, maxValue)</returns>
    public float NextFloat(float minValue, float maxValue)
    {
        if (minValue > maxValue)
            throw new ArgumentOutOfRangeException(nameof(minValue), "minValue must be less than or equal to maxValue");

        return minValue + (float)Random.NextDouble() * (maxValue - minValue);
    }

    /// <summary>
    /// Generates a random boolean value.
    /// </summary>
    /// <returns>True or false with equal probability.</returns>
    public bool NextBool()
    {
        return Random.Next(2) == 1;
    }

    /// <summary>
    /// Generates a random boolean with a specified probability.
    /// </summary>
    /// <param name="probability">Probability of returning true (0.0 to 1.0).</param>
    /// <returns>True with the specified probability, false otherwise.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when probability is not in range [0.0, 1.0].</exception>
    public bool NextBool(float probability)
    {
        if (probability < 0.0f || probability > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(probability), "Probability must be between 0.0 and 1.0");

        return Random.NextDouble() < probability;
    }

    /// <summary>
    /// Selects a random element from a collection.
    /// </summary>
    /// <typeparam name="T">Type of elements in the collection.</typeparam>
    /// <param name="items">Collection to select from.</param>
    /// <returns>A randomly selected element.</returns>
    /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
    /// <exception cref="ArgumentException">Thrown when items is empty.</exception>
    public T Choose<T>(IReadOnlyList<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        if (items.Count == 0)
            throw new ArgumentException("Cannot choose from empty collection", nameof(items));

        return items[Random.Next(items.Count)];
    }

    /// <summary>
    /// Selects multiple random elements from a collection without replacement.
    /// </summary>
    /// <typeparam name="T">Type of elements in the collection.</typeparam>
    /// <param name="items">Collection to select from.</param>
    /// <param name="count">Number of elements to select.</param>
    /// <returns>List of randomly selected elements.</returns>
    /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
    /// <exception cref="ArgumentException">Thrown when count > items.Count.</exception>
    public IReadOnlyList<T> Choose<T>(IReadOnlyList<T> items, int count)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");

        if (count > items.Count)
            throw new ArgumentException("Cannot select more items than available in collection", nameof(count));

        // Use Fisher-Yates shuffle on a copy and take first 'count' elements
        var itemsCopy = items.ToList();
        var result = new List<T>(count);

        for (int i = 0; i < count; i++)
        {
            var index = Random.Next(i, itemsCopy.Count);
            result.Add(itemsCopy[index]);

            // Swap selected item with position i to avoid re-selecting
            (itemsCopy[i], itemsCopy[index]) = (itemsCopy[index], itemsCopy[i]);
        }

        return result;
    }

    /// <summary>
    /// Shuffles a collection in place using Fisher-Yates algorithm.
    /// </summary>
    /// <typeparam name="T">Type of elements in the collection.</typeparam>
    /// <param name="items">Collection to shuffle.</param>
    /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
    public void Shuffle<T>(IList<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        // Fisher-Yates shuffle algorithm
        for (int i = items.Count - 1; i > 0; i--)
        {
            var j = Random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
