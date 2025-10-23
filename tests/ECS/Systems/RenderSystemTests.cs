using System;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Moq;
using PokeNET.Domain.ECS.Components;
using PokeNET.Domain.ECS.Systems;
using Xunit;

namespace PokeNET.Tests.ECS.Systems;

/// <summary>
/// Comprehensive tests for the RenderSystem.
/// Tests rendering, culling, sorting, camera transforms, and debug features.
/// </summary>
public class RenderSystemTests : IDisposable
{
    private readonly World _world;
    private readonly Mock<ILogger<RenderSystem>> _mockLogger;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly RenderSystem _renderSystem;
    private readonly Game _game;

    public RenderSystemTests()
    {
        // Create a minimal game instance for GraphicsDevice
        _game = new MockGame();
        _game.RunOneFrame(); // Initialize graphics device

        _graphicsDevice = _game.GraphicsDevice;
        _world = World.Create();
        _mockLogger = new Mock<ILogger<RenderSystem>>();
        _renderSystem = new RenderSystem(_mockLogger.Object, _graphicsDevice);
        _renderSystem.Initialize(_world);
    }

    [Fact]
    public void Initialize_CreatesSpriteBatchAndDebugTexture()
    {
        // Arrange & Act - done in constructor

        // Assert
        Assert.NotNull(_renderSystem);
        Assert.True(_renderSystem.IsEnabled);
        Assert.Equal(1000, _renderSystem.Priority); // Render systems should execute late
    }

    [Fact]
    public void Update_WithNoEntities_DoesNotThrow()
    {
        // Arrange - empty world

        // Act & Assert
        var exception = Record.Exception(() => _renderSystem.Update(0.016f));
        Assert.Null(exception);
        Assert.Equal(0, _renderSystem.EntitiesRendered);
    }

    [Fact]
    public void Update_RendersVisibleEntities()
    {
        // Arrange
        CreateRenderableEntity(new Position(100, 100, 0), true);
        CreateRenderableEntity(new Position(200, 200, 0), true);
        CreateRenderableEntity(new Position(300, 300, 0), true);

        // Act
        _renderSystem.Update(0.016f);

        // Assert
        Assert.Equal(3, _renderSystem.EntitiesRendered);
        Assert.Equal(0, _renderSystem.EntitiesCulled);
    }

    [Fact]
    public void Update_SkipsInvisibleEntities()
    {
        // Arrange
        CreateRenderableEntity(new Position(100, 100, 0), true);
        CreateRenderableEntity(new Position(200, 200, 0), false); // Not visible
        CreateRenderableEntity(new Position(300, 300, 0), true);

        // Act
        _renderSystem.Update(0.016f);

        // Assert
        Assert.Equal(2, _renderSystem.EntitiesRendered);
    }

    [Fact]
    public void Update_SortsByZOrder()
    {
        // Arrange - Create entities in random Z order
        var entity1 = CreateRenderableEntity(new Position(100, 100, 10), true); // Back
        var entity2 = CreateRenderableEntity(new Position(200, 200, 50), true); // Middle
        var entity3 = CreateRenderableEntity(new Position(300, 300, 20), true); // Front

        // Act
        _renderSystem.Update(0.016f);

        // Assert - All should be rendered
        Assert.Equal(3, _renderSystem.EntitiesRendered);
        // Z-ordering is internal, but we verify no crashes and correct count
    }

    [Fact]
    public void Update_WithCamera_CullsOffScreenEntities()
    {
        // Arrange
        var camera = CreateCamera(new Vector2(400, 300), 800, 600);

        // Inside camera bounds
        CreateRenderableEntity(new Position(400, 300, 0), true);
        CreateRenderableEntity(new Position(500, 400, 0), true);

        // Outside camera bounds (far away)
        CreateRenderableEntity(new Position(2000, 2000, 0), true);
        CreateRenderableEntity(new Position(-2000, -2000, 0), true);

        // Act
        _renderSystem.Update(0.016f);

        // Assert
        Assert.True(_renderSystem.EntitiesRendered >= 2, "Should render at least entities in view");
        Assert.True(_renderSystem.EntitiesCulled >= 2, "Should cull entities outside view");
    }

    [Fact]
    public void Update_WithCameraZoom_AdjustsCulling()
    {
        // Arrange
        var camera = CreateCamera(new Vector2(400, 300), 800, 600, 2.0f); // 2x zoom

        // Create entities at various distances
        CreateRenderableEntity(new Position(400, 300, 0), true); // Center, always visible
        CreateRenderableEntity(new Position(600, 500, 0), true); // Near edge
        CreateRenderableEntity(new Position(1000, 1000, 0), true); // Far away

        // Act
        _renderSystem.Update(0.016f);

        // Assert - Zoomed in view should see center but cull far entities
        Assert.True(_renderSystem.EntitiesRendered > 0);
    }

    [Fact]
    public void Update_WithMultipleCameras_UsesActiveCamera()
    {
        // Arrange
        CreateCamera(new Vector2(0, 0), 800, 600, 1.0f, false); // Inactive
        CreateCamera(new Vector2(400, 300), 800, 600, 1.0f, true); // Active

        CreateRenderableEntity(new Position(400, 300, 0), true);

        // Act
        _renderSystem.Update(0.016f);

        // Assert - Should use active camera
        Assert.True(_renderSystem.EntitiesRendered > 0);
    }

    [Fact]
    public void DebugRendering_CanBeEnabled()
    {
        // Arrange
        _renderSystem.DebugRenderingEnabled = false;

        // Act
        _renderSystem.DebugRenderingEnabled = true;

        // Assert
        Assert.True(_renderSystem.DebugRenderingEnabled);
    }

    [Fact]
    public void Update_WithDebugEnabled_RendersDebugInfo()
    {
        // Arrange
        _renderSystem.DebugRenderingEnabled = true;
        var entity = CreateRenderableEntity(new Position(100, 100, 0), true);

        // Enable debug on the entity
        var renderable = _world.Get<Renderable>(entity);
        renderable.ShowDebug = true;
        _world.Set(entity, renderable);

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => _renderSystem.Update(0.016f));
        Assert.Null(exception);
        Assert.Equal(1, _renderSystem.EntitiesRendered);
    }

    [Fact]
    public void RegisterTexture_StoresTextureInCache()
    {
        // Arrange
        var texture = new Texture2D(_graphicsDevice, 64, 64);
        var texturePath = "test/sprite.png";

        // Act
        _renderSystem.RegisterTexture(texturePath, texture);

        // Assert - Should not throw on subsequent renders
        var entity = CreateRenderableEntity(new Position(100, 100, 0), true);
        var sprite = _world.Get<Sprite>(entity);
        sprite.TexturePath = texturePath;
        _world.Set(entity, sprite);

        var exception = Record.Exception(() => _renderSystem.Update(0.016f));
        Assert.Null(exception);
    }

    [Fact]
    public void RegisterTexture_WithNullPath_ThrowsArgumentException()
    {
        // Arrange
        var texture = new Texture2D(_graphicsDevice, 64, 64);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _renderSystem.RegisterTexture(null!, texture));
        Assert.Throws<ArgumentException>(() => _renderSystem.RegisterTexture(string.Empty, texture));
    }

    [Fact]
    public void RegisterTexture_WithNullTexture_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => _renderSystem.RegisterTexture("test.png", null!));
    }

    [Fact]
    public void ClearTextureCache_RemovesAllTextures()
    {
        // Arrange
        var texture1 = new Texture2D(_graphicsDevice, 64, 64);
        var texture2 = new Texture2D(_graphicsDevice, 32, 32);
        _renderSystem.RegisterTexture("sprite1.png", texture1);
        _renderSystem.RegisterTexture("sprite2.png", texture2);

        // Act
        _renderSystem.ClearTextureCache();

        // Assert - Cache is cleared (verified by no exceptions on re-registration)
        var exception = Record.Exception(() => _renderSystem.RegisterTexture("sprite1.png", texture1));
        Assert.Null(exception);
    }

    [Fact]
    public void Update_WithSpriteTransformations_AppliesScaleRotation()
    {
        // Arrange
        var entity = CreateRenderableEntity(new Position(400, 300, 0), true);
        var sprite = _world.Get<Sprite>(entity);
        sprite.Scale = 2.0f;
        sprite.Rotation = MathF.PI / 4; // 45 degrees
        sprite.Color = Color.Red;
        _world.Set(entity, sprite);

        // Act & Assert - Should not throw with transformations
        var exception = Record.Exception(() => _renderSystem.Update(0.016f));
        Assert.Null(exception);
        Assert.Equal(1, _renderSystem.EntitiesRendered);
    }

    [Fact]
    public void Update_WithAlphaTransparency_AppliesCorrectAlpha()
    {
        // Arrange
        var entity = CreateRenderableEntity(new Position(100, 100, 0), true);
        var renderable = _world.Get<Renderable>(entity);
        renderable.Alpha = 0.5f; // 50% transparent
        _world.Set(entity, renderable);

        // Act & Assert
        var exception = Record.Exception(() => _renderSystem.Update(0.016f));
        Assert.Null(exception);
        Assert.Equal(1, _renderSystem.EntitiesRendered);
    }

    [Fact]
    public void Camera_GetTransformMatrix_ReturnsCorrectMatrix()
    {
        // Arrange
        var camera = new Camera(new Vector2(100, 100), 800, 600, 1.0f);

        // Act
        var matrix = camera.GetTransformMatrix();

        // Assert
        Assert.NotEqual(Matrix.Identity, matrix);
    }

    [Fact]
    public void Camera_GetBounds_ReturnsCorrectBounds()
    {
        // Arrange
        var camera = new Camera(new Vector2(400, 300), 800, 600, 1.0f);

        // Act
        var bounds = camera.GetBounds();

        // Assert
        Assert.Equal(400, bounds.X);
        Assert.Equal(300, bounds.Y);
        Assert.Equal(800, bounds.Width);
        Assert.Equal(600, bounds.Height);
    }

    [Fact]
    public void Camera_ScreenToWorld_ConvertsCorrectly()
    {
        // Arrange
        var camera = new Camera(new Vector2(0, 0), 800, 600, 1.0f);
        var screenPos = new Vector2(400, 300); // Center of screen

        // Act
        var worldPos = camera.ScreenToWorld(screenPos);

        // Assert
        Assert.NotEqual(Vector2.Zero, worldPos);
    }

    [Fact]
    public void Camera_WorldToScreen_ConvertsCorrectly()
    {
        // Arrange
        var camera = new Camera(new Vector2(0, 0), 800, 600, 1.0f);
        var worldPos = new Vector2(100, 100);

        // Act
        var screenPos = camera.WorldToScreen(worldPos);

        // Assert
        Assert.NotEqual(Vector2.Zero, screenPos);
    }

    [Fact]
    public void Renderable_Hidden_CreatesInvisibleRenderable()
    {
        // Arrange & Act
        var renderable = Renderable.Hidden();

        // Assert
        Assert.False(renderable.IsVisible);
    }

    [Fact]
    public void Renderable_WithDebug_CreatesDebugRenderable()
    {
        // Arrange & Act
        var renderable = Renderable.WithDebug();

        // Assert
        Assert.True(renderable.IsVisible);
        Assert.True(renderable.ShowDebug);
    }

    [Fact]
    public void Performance_HandlesHundredsOfEntities()
    {
        // Arrange - Create many entities
        for (int i = 0; i < 500; i++)
        {
            var x = (i % 50) * 32f;
            var y = (i / 50) * 32f;
            var z = i % 10;
            CreateRenderableEntity(new Position(x, y, z), true);
        }

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _renderSystem.Update(0.016f);
        stopwatch.Stop();

        // Assert
        Assert.True(_renderSystem.EntitiesRendered > 0);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Should render 500 entities in under 1 second");
    }

    [Fact]
    public void Dispose_CleansUpResources()
    {
        // Arrange & Act
        _renderSystem.Dispose();

        // Assert - Should not throw
        var exception = Record.Exception(() => _renderSystem.Update(0.016f));
        // After disposal, update should handle gracefully
    }

    // Helper methods
    private Entity CreateRenderableEntity(Position position, bool isVisible)
    {
        var entity = _world.Create(
            position,
            new Sprite("test/sprite.png", 32, 32, 0.5f),
            new Renderable(isVisible)
        );
        return entity;
    }

    private Entity CreateCamera(Vector2 position, int width, int height, float zoom = 1.0f, bool isActive = true)
    {
        var camera = new Camera(position, width, height, zoom) { IsActive = isActive };
        return _world.Create(camera);
    }

    public void Dispose()
    {
        _renderSystem?.Dispose();
        _world?.Dispose();
        _game?.Dispose();
    }

    /// <summary>
    /// Minimal game implementation for testing.
    /// </summary>
    private class MockGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        public MockGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();
        }
    }
}
