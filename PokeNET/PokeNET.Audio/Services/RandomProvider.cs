using PokeNET.Audio.Abstractions;

namespace PokeNET.Audio.Services;

/// <summary>
/// Thread-safe wrapper around System.Random for audio effects.
/// Uses ThreadLocal to ensure thread safety without locking.
/// </summary>
public sealed class RandomProvider : IRandomProvider
{
    private readonly ThreadLocal<Random> _random = new(() => new Random());

    /// <inheritdoc />
    public int Next(int maxValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxValue);
        return _random.Value!.Next(maxValue);
    }

    /// <inheritdoc />
    public int Next(int minValue, int maxValue)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minValue, maxValue);
        return _random.Value!.Next(minValue, maxValue);
    }

    /// <inheritdoc />
    public double NextDouble()
    {
        return _random.Value!.NextDouble();
    }

    /// <inheritdoc />
    public float NextFloat()
    {
        return (float)_random.Value!.NextDouble();
    }

    /// <inheritdoc />
    public float NextFloat(float minValue, float maxValue)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minValue, maxValue);
        return minValue + (float)_random.Value!.NextDouble() * (maxValue - minValue);
    }

    /// <inheritdoc />
    public bool NextBool()
    {
        return _random.Value!.Next(2) == 1;
    }

    /// <inheritdoc />
    public bool NextBool(float probability)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(probability);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(probability, 1.0f);
        return _random.Value!.NextDouble() < probability;
    }

    /// <inheritdoc />
    public T Choose<T>(IReadOnlyList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
            throw new ArgumentException("Cannot choose from an empty collection.", nameof(items));

        return items[_random.Value!.Next(items.Count)];
    }

    /// <inheritdoc />
    public IReadOnlyList<T> Choose<T>(IReadOnlyList<T> items, int count)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, items.Count);

        var indices = new HashSet<int>();
        var result = new List<T>(count);

        while (result.Count < count)
        {
            var index = _random.Value!.Next(items.Count);
            if (indices.Add(index))
            {
                result.Add(items[index]);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public void Shuffle<T>(IList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        // Fisher-Yates shuffle
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = _random.Value!.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
