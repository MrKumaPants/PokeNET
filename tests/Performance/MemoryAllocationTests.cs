using PokeNET.Core;
using PokeNET.Tests.Utilities;

namespace PokeNET.Tests.Performance;

/// <summary>
/// Tests for monitoring memory allocation patterns and GC pressure.
/// These tests help identify excessive allocations and memory leaks.
/// </summary>
public class MemoryAllocationTests
{
    [Fact]
    public void GameCreation_ShouldNotExcessivelyAllocate()
    {
        // Arrange
        const long maxAllowedBytes = 10 * 1024 * 1024; // 10 MB

        // Act
        var allocations = MemoryTestHelpers.MeasureAllocations(() =>
        {
            using var game = new PokeNETGame();
        });

        // Assert
        allocations.Should().BeLessThan(maxAllowedBytes,
            $"Game creation allocated {allocations:N0} bytes, expected less than {maxAllowedBytes:N0}");
    }

    [Fact]
    public void GameTime_Creation_ShouldBeAllocEfficient()
    {
        // Arrange & Act
        var (avgAllocations, avgDuration) = MemoryTestHelpers.BenchmarkAction(
            () => TestGameFactory.CreateGameTime(),
            iterations: 1000
        );

        // Assert
        avgAllocations.Should().BeLessThan(200,
            $"Creating GameTime allocated {avgAllocations:N0} bytes on average");
        avgDuration.Should().BeLessThan(TimeSpan.FromMicroseconds(10),
            $"Creating GameTime took {avgDuration.TotalMicroseconds:F2}µs on average");
    }

    [Fact]
    public void FrameSequence_ShouldNotAccumulateMemory()
    {
        // Arrange
        const int frameCount = 1000;

        // Act - Force GC before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();

        var startMemory = GC.GetTotalMemory(true);

        foreach (var gameTime in TestGameFactory.CreateFrameSequence(frameCount))
        {
            // Simulate frame processing
            _ = gameTime.TotalGameTime;
            _ = gameTime.ElapsedGameTime;
        }

        GC.Collect();
        var endMemory = GC.GetTotalMemory(true);

        var memoryGrowth = endMemory - startMemory;

        // Assert - Should not accumulate significant memory
        memoryGrowth.Should().BeLessThan(1024 * 1024, // 1 MB
            $"Processing {frameCount} frames accumulated {memoryGrowth:N0} bytes");
    }
}
