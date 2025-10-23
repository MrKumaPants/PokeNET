using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokeNET.Domain.ECS.Components;

namespace PokeNET.Domain.ECS.Systems;

/// <summary>
/// System responsible for rendering sprites with support for layering, batching, culling, and camera transformations.
/// Follows Single Responsibility Principle - only handles rendering logic.
/// </summary>
public class RenderSystem : SystemBase
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Dictionary<string, Texture2D> _textureCache;
    private SpriteBatch? _spriteBatch;
    private Camera? _activeCamera;
    private bool _debugRenderingEnabled;
    private Texture2D? _debugTexture;

    // Performance metrics
    private int _entitiesRendered;
    private int _entitiesCulled;
    private int _drawCalls;

    /// <summary>
    /// Render system executes late in the update cycle.
    /// </summary>
    public override int Priority => 1000;

    /// <summary>
    /// Initializes a new render system.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="graphicsDevice">MonoGame graphics device for rendering.</param>
    public RenderSystem(ILogger<RenderSystem> logger, GraphicsDevice graphicsDevice)
        : base(logger)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _textureCache = new Dictionary<string, Texture2D>();
        _debugRenderingEnabled = false;
    }

    /// <summary>
    /// Enables or disables debug rendering (bounding boxes, entity counts).
    /// </summary>
    public bool DebugRenderingEnabled
    {
        get => _debugRenderingEnabled;
        set => _debugRenderingEnabled = value;
    }

    /// <summary>
    /// Gets the number of entities rendered in the last frame.
    /// </summary>
    public int EntitiesRendered => _entitiesRendered;

    /// <summary>
    /// Gets the number of entities culled in the last frame.
    /// </summary>
    public int EntitiesCulled => _entitiesCulled;

    /// <summary>
    /// Gets the number of draw calls in the last frame.
    /// </summary>
    public int DrawCalls => _drawCalls;

    protected override void OnInitialize()
    {
        _spriteBatch = new SpriteBatch(_graphicsDevice);

        // Create 1x1 white texture for debug rendering
        _debugTexture = new Texture2D(_graphicsDevice, 1, 1);
        _debugTexture.SetData(new[] { Color.White });

        Logger.LogInformation("RenderSystem initialized with graphics device");
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (_spriteBatch == null)
        {
            Logger.LogWarning("SpriteBatch not initialized, skipping render");
            return;
        }

        // Reset performance metrics
        _entitiesRendered = 0;
        _entitiesCulled = 0;
        _drawCalls = 0;

        // Find active camera
        _activeCamera = FindActiveCamera();

        // Collect and sort renderable entities
        var renderables = CollectRenderables();

        if (renderables.Count == 0)
        {
            return;
        }

        // Sort by Z-order (Position.Z) for proper layering
        renderables.Sort((a, b) => a.Position.Z.CompareTo(b.Position.Z));

        // Begin sprite batch with camera transform
        var transformMatrix = _activeCamera?.GetTransformMatrix() ?? Matrix.Identity;

        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            null,
            transformMatrix
        );

        _drawCalls++;

        // Render each entity
        foreach (var renderable in renderables)
        {
            RenderEntity(renderable);
        }

        _spriteBatch.End();

        // Log performance metrics periodically
        if (_debugRenderingEnabled)
        {
            Logger.LogDebug(
                "Rendered {Rendered} entities, culled {Culled}, draw calls: {DrawCalls}",
                _entitiesRendered,
                _entitiesCulled,
                _drawCalls
            );
        }
    }

    /// <summary>
    /// Finds the active camera in the world.
    /// </summary>
    private Camera? FindActiveCamera()
    {
        var query = new QueryDescription().WithAll<Camera>();

        Camera? activeCamera = null;
        World.Query(in query, (ref Camera camera) =>
        {
            if (camera.IsActive)
            {
                activeCamera = camera;
            }
        });

        return activeCamera;
    }

    /// <summary>
    /// Collects all renderable entities with required components.
    /// </summary>
    private List<RenderableEntity> CollectRenderables()
    {
        var renderables = new List<RenderableEntity>();
        var cameraBounds = _activeCamera?.GetBounds();

        // Query entities with Position, Sprite, and Renderable components
        var query = new QueryDescription()
            .WithAll<Position, Sprite, Renderable>();

        World.Query(in query, (Entity entity, ref Position position, ref Sprite sprite, ref Renderable renderable) =>
        {
            // Skip if not visible
            if (!renderable.IsVisible || !sprite.IsVisible)
            {
                return;
            }

            // Frustum culling - skip if outside camera view
            if (cameraBounds.HasValue && !IsInCameraBounds(position, sprite, cameraBounds.Value))
            {
                _entitiesCulled++;
                return;
            }

            renderables.Add(new RenderableEntity
            {
                Entity = entity,
                Position = position,
                Sprite = sprite,
                Renderable = renderable
            });
        });

        return renderables;
    }

    /// <summary>
    /// Checks if an entity is within the camera's view bounds.
    /// </summary>
    private bool IsInCameraBounds(Position position, Sprite sprite, Rectangle cameraBounds)
    {
        var halfWidth = sprite.Width * sprite.Scale * 0.5f;
        var halfHeight = sprite.Height * sprite.Scale * 0.5f;

        var entityBounds = new Rectangle(
            (int)(position.X - halfWidth),
            (int)(position.Y - halfHeight),
            (int)(sprite.Width * sprite.Scale),
            (int)(sprite.Height * sprite.Scale)
        );

        return cameraBounds.Intersects(entityBounds);
    }

    /// <summary>
    /// Renders a single entity.
    /// </summary>
    private void RenderEntity(RenderableEntity renderable)
    {
        if (_spriteBatch == null || _debugTexture == null)
        {
            return;
        }

        var sprite = renderable.Sprite;
        var position = renderable.Position;
        var renderableComp = renderable.Renderable;

        // Load texture (using placeholder for now, as we don't have ContentManager)
        var texture = GetOrLoadTexture(sprite.TexturePath);
        if (texture == null)
        {
            // Use debug texture as fallback
            texture = _debugTexture;
        }

        // Apply alpha from Renderable component
        var color = sprite.Color * renderableComp.Alpha;

        // Render sprite
        _spriteBatch.Draw(
            texture,
            new Vector2(position.X, position.Y),
            sprite.SourceRectangle,
            color,
            sprite.Rotation,
            sprite.Origin,
            sprite.Scale,
            SpriteEffects.None,
            sprite.LayerDepth
        );

        _entitiesRendered++;

        // Render debug bounding box if enabled
        if (_debugRenderingEnabled && renderableComp.ShowDebug)
        {
            RenderDebugBounds(position, sprite);
        }
    }

    /// <summary>
    /// Renders debug bounding box for an entity.
    /// </summary>
    private void RenderDebugBounds(Position position, Sprite sprite)
    {
        if (_spriteBatch == null || _debugTexture == null)
        {
            return;
        }

        var halfWidth = sprite.Width * sprite.Scale * 0.5f;
        var halfHeight = sprite.Height * sprite.Scale * 0.5f;

        var bounds = new Rectangle(
            (int)(position.X - halfWidth),
            (int)(position.Y - halfHeight),
            (int)(sprite.Width * sprite.Scale),
            (int)(sprite.Height * sprite.Scale)
        );

        // Draw bounding box outline
        var thickness = 2;
        var debugColor = Color.Red * 0.5f;

        // Top
        _spriteBatch.Draw(_debugTexture, new Rectangle(bounds.Left, bounds.Top, bounds.Width, thickness), debugColor);
        // Bottom
        _spriteBatch.Draw(_debugTexture, new Rectangle(bounds.Left, bounds.Bottom - thickness, bounds.Width, thickness), debugColor);
        // Left
        _spriteBatch.Draw(_debugTexture, new Rectangle(bounds.Left, bounds.Top, thickness, bounds.Height), debugColor);
        // Right
        _spriteBatch.Draw(_debugTexture, new Rectangle(bounds.Right - thickness, bounds.Top, thickness, bounds.Height), debugColor);
    }

    /// <summary>
    /// Gets or loads a texture from the cache.
    /// </summary>
    private Texture2D? GetOrLoadTexture(string texturePath)
    {
        if (string.IsNullOrEmpty(texturePath))
        {
            return null;
        }

        if (_textureCache.TryGetValue(texturePath, out var texture))
        {
            return texture;
        }

        // In a real implementation, this would use ContentManager to load textures
        // For now, we return null and let the system use debug texture as fallback
        Logger.LogWarning("Texture not found in cache: {TexturePath}", texturePath);
        return null;
    }

    /// <summary>
    /// Registers a texture in the cache for use by entities.
    /// </summary>
    public void RegisterTexture(string texturePath, Texture2D texture)
    {
        if (string.IsNullOrEmpty(texturePath))
        {
            throw new ArgumentException("Texture path cannot be null or empty", nameof(texturePath));
        }

        if (texture == null)
        {
            throw new ArgumentNullException(nameof(texture));
        }

        _textureCache[texturePath] = texture;
        Logger.LogInformation("Registered texture: {TexturePath}", texturePath);
    }

    /// <summary>
    /// Clears the texture cache.
    /// </summary>
    public void ClearTextureCache()
    {
        _textureCache.Clear();
        Logger.LogInformation("Texture cache cleared");
    }

    protected override void OnDispose()
    {
        _spriteBatch?.Dispose();
        _debugTexture?.Dispose();
        _textureCache.Clear();

        Logger.LogInformation("RenderSystem disposed");
    }

    /// <summary>
    /// Helper struct for collecting renderable entities.
    /// </summary>
    private struct RenderableEntity
    {
        public Entity Entity;
        public Position Position;
        public Sprite Sprite;
        public Renderable Renderable;
    }
}
