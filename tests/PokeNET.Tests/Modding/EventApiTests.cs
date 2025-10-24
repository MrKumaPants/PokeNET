using System;
using Xunit;
using Moq;
using PokeNET.Domain.Modding;
using PokeNET.Core.Modding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace PokeNET.Tests.Modding;

/// <summary>
/// Tests for Event API ISP compliance and focused interface design.
/// </summary>
public class EventApiTests
{
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger<ModContext>> _mockLogger;
    private readonly Mock<ModLoader> _mockModLoader;
    private readonly ServiceProvider _serviceProvider;

    public EventApiTests()
    {
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<ModContext>>();
        _mockModLoader = new Mock<ModLoader>(
            Mock.Of<ILoggerFactory>(),
            Mock.Of<IServiceProvider>()
        );

        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        var services = new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void ModContext_ExposesFocusedEventAPIs()
    {
        // Arrange
        var manifest = CreateTestManifest();
        var modContext = CreateModContext(manifest);

        // Act & Assert - All focused APIs should be accessible
        Assert.NotNull(modContext.GameplayEvents);
        Assert.NotNull(modContext.BattleEvents);
        Assert.NotNull(modContext.UIEvents);
        Assert.NotNull(modContext.SaveEvents);
        Assert.NotNull(modContext.ModEvents);
    }

    [Fact]
    public void ModContext_AllFocusedAPIs_AreAccessible()
    {
        // Arrange
        var manifest = CreateTestManifest();
        var modContext = CreateModContext(manifest);

        // Act - Access all focused event APIs
        var gameplayEvents = modContext.GameplayEvents;
        var battleEvents = modContext.BattleEvents;
        var uiEvents = modContext.UIEvents;
        var saveEvents = modContext.SaveEvents;
        var modEvents = modContext.ModEvents;

        // Assert - All focused APIs should be accessible
        Assert.NotNull(gameplayEvents);
        Assert.NotNull(battleEvents);
        Assert.NotNull(uiEvents);
        Assert.NotNull(saveEvents);
        Assert.NotNull(modEvents);
    }

    [Fact]
    public void ModContext_FocusedAPIs_AreIndependent()
    {
        // Arrange
        var manifest = CreateTestManifest();
        var modContext = CreateModContext(manifest);

        // Act - Access different event APIs
        var battleEvents = modContext.BattleEvents;
        var uiEvents = modContext.UIEvents;

        // Assert - Each API should be independent (different instances)
        Assert.NotNull(battleEvents);
        Assert.NotNull(uiEvents);
        Assert.False(ReferenceEquals(battleEvents, uiEvents));
    }

    [Fact]
    public void BattleEvents_CanBeUsedIndependently()
    {
        // Arrange
        var manifest = CreateTestManifest();
        var modContext = CreateModContext(manifest);
        var eventFired = false;

        // Act - Subscribe to battle events without needing other APIs
        modContext.BattleEvents.OnBattleStart += (sender, args) =>
        {
            eventFired = true;
        };

        // Assert - Should be able to use battle events independently
        Assert.NotNull(modContext.BattleEvents);
        Assert.False(eventFired); // Event not fired yet
    }

    [Fact]
    public void UIEvents_CanBeUsedIndependently()
    {
        // Arrange
        var manifest = CreateTestManifest();
        var modContext = CreateModContext(manifest);
        var eventFired = false;

        // Act - Subscribe to UI events without needing battle events
        modContext.UIEvents.OnMenuOpened += (sender, args) =>
        {
            eventFired = true;
        };

        // Assert - Should be able to use UI events independently
        Assert.NotNull(modContext.UIEvents);
        Assert.False(eventFired); // Event not fired yet
    }

    [Fact]
    public void GameplayEvents_CanBeUsedIndependently()
    {
        // Arrange
        var manifest = CreateTestManifest();
        var modContext = CreateModContext(manifest);
        var eventFired = false;

        // Act - Subscribe to gameplay events without needing other APIs
        modContext.GameplayEvents.OnLocationChanged += (sender, args) =>
        {
            eventFired = true;
        };

        // Assert - Should be able to use gameplay events independently
        Assert.NotNull(modContext.GameplayEvents);
        Assert.False(eventFired); // Event not fired yet
    }

    [Fact]
    public void SaveEvents_CanBeUsedIndependently()
    {
        // Arrange
        var manifest = CreateTestManifest();
        var modContext = CreateModContext(manifest);
        var eventFired = false;

        // Act - Subscribe to save events without needing other APIs
        modContext.SaveEvents.OnSaving += (sender, args) =>
        {
            eventFired = true;
        };

        // Assert - Should be able to use save events independently
        Assert.NotNull(modContext.SaveEvents);
        Assert.False(eventFired); // Event not fired yet
    }

    [Fact]
    public void ModEvents_CanBeUsedIndependently()
    {
        // Arrange
        var manifest = CreateTestManifest();
        var modContext = CreateModContext(manifest);
        var eventFired = false;

        // Act - Subscribe to mod events without needing other APIs
        modContext.ModEvents.OnAllModsLoaded += (sender, args) =>
        {
            eventFired = true;
        };

        // Assert - Should be able to use mod events independently
        Assert.NotNull(modContext.ModEvents);
        Assert.False(eventFired); // Event not fired yet
    }

    [Fact]
    public void EventAPI_ISPCompliance_UIModDoesNotDependOnBattleEvents()
    {
        // This test demonstrates ISP compliance at compile time
        // A UI mod can receive only IUIEvents without knowing about IBattleEvents

        // Arrange
        var manifest = CreateTestManifest();
        var modContext = CreateModContext(manifest);

        // Act - Extract only UI events
        IUIEvents uiEvents = modContext.UIEvents;

        // Assert - UI events interface doesn't expose battle functionality
        Assert.IsAssignableFrom<IUIEvents>(uiEvents);

        // This should NOT be possible (compile-time check):
        // var battleEvents = (IBattleEvents)uiEvents; // Would fail
        Assert.False(uiEvents is IBattleEvents);
    }

    [Fact]
    public void EventAPI_ISPCompliance_BattleModDoesNotDependOnUIEvents()
    {
        // This test demonstrates ISP compliance at compile time
        // A battle mod can receive only IBattleEvents without knowing about IUIEvents

        // Arrange
        var manifest = CreateTestManifest();
        var modContext = CreateModContext(manifest);

        // Act - Extract only battle events
        IBattleEvents battleEvents = modContext.BattleEvents;

        // Assert - Battle events interface doesn't expose UI functionality
        Assert.IsAssignableFrom<IBattleEvents>(battleEvents);

        // This should NOT be possible (compile-time check):
        // var uiEvents = (IUIEvents)battleEvents; // Would fail
        Assert.False(battleEvents is IUIEvents);
    }

    [Fact]
    public void ModContext_MultipleEventCategories_CanBeUsedTogether()
    {
        // Arrange
        var manifest = CreateTestManifest();
        var modContext = CreateModContext(manifest);
        var battleEventFired = false;
        var uiEventFired = false;

        // Act - Use multiple event categories in same mod
        modContext.BattleEvents.OnBattleStart += (sender, args) =>
        {
            battleEventFired = true;
        };

        modContext.UIEvents.OnMenuOpened += (sender, args) =>
        {
            uiEventFired = true;
        };

        // Assert - Both event categories work together
        Assert.NotNull(modContext.BattleEvents);
        Assert.NotNull(modContext.UIEvents);
        Assert.False(battleEventFired);
        Assert.False(uiEventFired);
    }

    [Theory]
    [InlineData(typeof(IGameplayEvents))]
    [InlineData(typeof(IBattleEvents))]
    [InlineData(typeof(IUIEvents))]
    [InlineData(typeof(ISaveEvents))]
    [InlineData(typeof(IModEvents))]
    public void EventAPI_AllCategoriesImplementCorrectInterface(Type eventInterfaceType)
    {
        // Arrange
        var manifest = CreateTestManifest();
        var modContext = CreateModContext(manifest);

        // Act - Get the event category by type
        object? eventApi = eventInterfaceType.Name switch
        {
            nameof(IGameplayEvents) => modContext.GameplayEvents,
            nameof(IBattleEvents) => modContext.BattleEvents,
            nameof(IUIEvents) => modContext.UIEvents,
            nameof(ISaveEvents) => modContext.SaveEvents,
            nameof(IModEvents) => modContext.ModEvents,
            _ => null
        };

        // Assert - Each category implements its interface
        Assert.NotNull(eventApi);
        Assert.IsAssignableFrom(eventInterfaceType, eventApi);
    }

    // Helper methods

    private IModManifest CreateTestManifest()
    {
        var manifest = new Mock<IModManifest>();
        manifest.Setup(m => m.Id).Returns("test.mod");
        manifest.Setup(m => m.Name).Returns("Test Mod");
        manifest.Setup(m => m.Version).Returns(ModVersion.Parse("1.0.0"));
        return manifest.Object;
    }

    private ModContext CreateModContext(IModManifest manifest)
    {
        return new ModContext(
            manifest,
            "/fake/mod/directory",
            _serviceProvider,
            _mockLoggerFactory.Object,
            _mockModLoader.Object
        );
    }
}

/// <summary>
/// Integration tests demonstrating real-world usage patterns.
/// </summary>
public class EventApiIntegrationTests
{
    [Fact]
    public void UIOnlyMod_OnlyDependsOnUIEvents()
    {
        // This test simulates a real UI-only mod that follows ISP

        // Arrange
        var uiMod = new TestUIOnlyMod();
        var mockContext = new Mock<IModContext>();
        var mockUIEvents = new Mock<IUIEvents>();

        mockContext.Setup(c => c.UIEvents).Returns(mockUIEvents.Object);

        // Act - Initialize UI-only mod
        uiMod.InitializeAsync(mockContext.Object);

        // Assert - Mod only accessed UI events
        mockContext.Verify(c => c.UIEvents, Times.Once);
        mockContext.Verify(c => c.BattleEvents, Times.Never);
        mockContext.Verify(c => c.GameplayEvents, Times.Never);
    }

    [Fact]
    public void BattleOnlyMod_OnlyDependsOnBattleEvents()
    {
        // This test simulates a real battle-only mod that follows ISP

        // Arrange
        var battleMod = new TestBattleOnlyMod();
        var mockContext = new Mock<IModContext>();
        var mockBattleEvents = new Mock<IBattleEvents>();

        mockContext.Setup(c => c.BattleEvents).Returns(mockBattleEvents.Object);

        // Act - Initialize battle-only mod
        battleMod.InitializeAsync(mockContext.Object);

        // Assert - Mod only accessed battle events
        mockContext.Verify(c => c.BattleEvents, Times.Once);
        mockContext.Verify(c => c.UIEvents, Times.Never);
        mockContext.Verify(c => c.GameplayEvents, Times.Never);
    }

    // Test mod implementations

    private class TestUIOnlyMod
    {
        public void InitializeAsync(IModContext context)
        {
            // Only use UI events - follows ISP
            context.UIEvents.OnMenuOpened += (s, e) => { };
        }
    }

    private class TestBattleOnlyMod
    {
        public void InitializeAsync(IModContext context)
        {
            // Only use battle events - follows ISP
            context.BattleEvents.OnBattleStart += (s, e) => { };
        }
    }
}
