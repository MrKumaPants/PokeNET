using FluentAssertions;
using FluentAssertions.Execution;

namespace PokeNET.Tests.Utilities;

/// <summary>
/// Custom assertion extensions for game-specific testing scenarios.
/// </summary>
public static class AssertionExtensions
{
    /// <summary>
    /// Asserts that a value is within a specified percentage tolerance.
    /// Useful for floating-point comparisons and performance metrics.
    /// </summary>
    public static void ShouldBeApproximately(
        this double actual,
        double expected,
        double percentTolerance = 0.01)
    {
        var tolerance = Math.Abs(expected * percentTolerance);
        actual.Should().BeInRange(expected - tolerance, expected + tolerance,
            $"expected {actual} to be within {percentTolerance * 100}% of {expected}");
    }

    /// <summary>
    /// Asserts that a value is within a specified percentage tolerance.
    /// </summary>
    public static void ShouldBeApproximately(
        this float actual,
        float expected,
        float percentTolerance = 0.01f)
    {
        var tolerance = Math.Abs(expected * percentTolerance);
        actual.Should().BeInRange(expected - tolerance, expected + tolerance,
            $"expected {actual} to be within {percentTolerance * 100}% of {expected}");
    }

    /// <summary>
    /// Asserts that an action completes within a specified time frame.
    /// Useful for performance testing.
    /// </summary>
    public static void ShouldCompleteWithin(
        this Action action,
        TimeSpan maximumDuration,
        string because = "")
    {
        var startTime = DateTime.UtcNow;
        action();
        var duration = DateTime.UtcNow - startTime;

        duration.Should().BeLessThanOrEqualTo(maximumDuration, because);
    }

    /// <summary>
    /// Asserts that an async action completes within a specified time frame.
    /// </summary>
    public static async Task ShouldCompleteWithinAsync(
        this Func<Task> action,
        TimeSpan maximumDuration,
        string because = "")
    {
        var startTime = DateTime.UtcNow;
        await action();
        var duration = DateTime.UtcNow - startTime;

        duration.Should().BeLessThanOrEqualTo(maximumDuration, because);
    }

    /// <summary>
    /// Asserts that a collection has exactly the expected count of items matching a predicate.
    /// </summary>
    public static void ShouldContainExactly<T>(
        this IEnumerable<T> collection,
        int expectedCount,
        Func<T, bool> predicate,
        string because = "")
    {
        var matchingItems = collection.Where(predicate).ToList();
        matchingItems.Should().HaveCount(expectedCount, because);
    }
}
