using System;
using Arch.Core;
using Microsoft.Extensions.Logging;
using Moq;
using PokeNET.Domain.ECS.Systems;
using Xunit;

namespace PokeNET.Tests.RegressionTests.CodeQualityFixes;

/// <summary>
/// Regression tests for Issue #2: Null reference risks in SystemBase.
/// </summary>
public class SystemBaseNullSafetyTests
{
    private class TestSystem : SystemBase
    {
        public TestSystem(ILogger logger) : base(logger) { }

        protected override void OnUpdate(float deltaTime)
        {
            // Access World property to test null safety
            var queryDesc = new QueryDescription();
            _ = World.CountEntities(in queryDesc);
        }

        public void AccessWorldBeforeInit()
        {
            // Try to access World before Initialize() is called
            _ = World;
        }
    }

    [Fact]
    public void World_AccessedBeforeInitialize_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TestSystem>>();
        var system = new TestSystem(logger);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => system.AccessWorldBeforeInit());

        Assert.Contains("not initialized", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Initialize()", exception.Message);
    }

    [Fact]
    public void World_AccessedAfterInitialize_ShouldWork()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TestSystem>>();
        var system = new TestSystem(logger);
        var world = World.Create();

        try
        {
            // Act
            system.Initialize(world);

            // Assert - Should not throw
            system.Update(0.016f);
        }
        finally
        {
            world.Dispose();
        }
    }

    [Fact]
    public void Initialize_WithNullWorld_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TestSystem>>();
        var system = new TestSystem(logger);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => system.Initialize(null!));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestSystem(null!));
    }
}
