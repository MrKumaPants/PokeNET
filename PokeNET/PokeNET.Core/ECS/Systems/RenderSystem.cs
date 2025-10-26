using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokeNET.Core.ECS.Components;
using Entity = Arch.Core.Entity;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace PokeNET.Core.ECS.Systems;

/// <summary>
/// System responsible for rendering sprites with support for layering, batching, culling, and camera transformations.
/// Follows Single Responsibility Principle - only handles rendering logic.
/// </summary>
public partial class RenderSystem : BaseSystem<World, float>
{
    private readonly ILogger _logger;
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

    // Rendering state for source-generated queries
    private float _deltaTime;
    private List<RenderableEntity> _renderables;
    private Rectangle? _cameraBounds;

    /// <summary>
    /// Initializes a new render system.
    /// </summary>
    /// <param name="world">ECS world instance.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="graphicsDevice">MonoGame graphics device for rendering.</param>
    public RenderSystem(World world, ILogger<RenderSystem> logger, GraphicsDevice graphicsDevice)
        : base(world)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _textureCache = new Dictionary<string, Texture2D>();
        _debugRenderingEnabled = false;
        _renderables = new List<RenderableEntity>();

        // Initialize immediately
        _spriteBatch = new SpriteBatch(_graphicsDevice);
        _debugTexture = new Texture2D(_graphicsDevice, 1, 1);
        _debugTexture.SetData(new[] { Color.White });

        _logger.LogInformation("RenderSystem initialized with graphics device");
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

    public override void Update(in float deltaTime)
    {
        if (_spriteBatch == null)
        {
            _logger.LogWarning("SpriteBatch not initialized, skipping render");
            return;
        }

        // Store state for source-generated queries
        _deltaTime = deltaTime;
        _entitiesRendered = 0;
        _entitiesCulled = 0;
        _drawCalls = 0;
        _renderables.Clear();

        // Find active camera
        _activeCamera = FindActiveCamera();
        _cameraBounds = _activeCamera?.GetBounds();

        // Collect renderable entities using source-generated query
        CollectRenderableQuery(World);

        if (_renderables.Count == 0)
        {
            return;
        }

        // Sort by Z-order (Position.Z) for proper layering
        _renderables.Sort((a, b) => a.Position.Z.CompareTo(b.Position.Z));

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
        foreach (var renderable in _renderables)
        {
            RenderEntity(renderable);
        }

        _spriteBatch.End();

        // Log performance metrics periodically
        if (_debugRenderingEnabled)
        {
            _logger.LogDebug(
                "Rendered {Rendered} entities, culled {Culled}, draw calls: {DrawCalls}",
                _entitiesRendered,
                _entitiesCulled,
                _drawCalls
            );
        }
    }

    // Store active camera for query
    private Camera? _foundCamera;

    /// <summary>
    /// Source-generated query for finding active camera.
    /// Generated method: FindActiveCameraQuery(World world)
    /// </summary>
    [Query]
    [All<Camera>]
    private void CheckActiveCamera(ref Camera camera)
    {
        if (camera.IsActive && _foundCamera == null)
        {
            _foundCamera = camera;
        }
    }

    /// <summary>
    /// Finds the active camera in the world.
    /// </summary>
    private Camera? FindActiveCamera()
    {
        _foundCamera = null;
        CheckActiveCameraQuery(World);
        return _foundCamera;
    }

    /// <summary>
    /// Source-generated query for collecting renderable entities.
    /// Generated method: CollectRenderableQuery(World world)
    /// </summary>
    [Query]
    [All<Position, Sprite, Renderable>]
    private void CollectRenderable(
        in Entity entity,
        ref Position position,
        ref Sprite sprite,
        ref Renderable renderable
    )
    {
        // Skip if not visible
        if (!renderable.IsVisible || !sprite.IsVisible)
        {
            return;
        }

        // Frustum culling - skip if outside camera view
        if (_cameraBounds.HasValue && !IsInCameraBounds(position, sprite, _cameraBounds.Value))
        {
            _entitiesCulled++;
            return;
        }

        _renderables.Add(
            new RenderableEntity
            {
                Entity = entity,
                Position = position,
                Sprite = sprite,
                Renderable = renderable,
            }
        );
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

        // Use GridPosition for world position if available, otherwise fall back to Position
        Vector2 worldPos;
        if (World.Has<GridPosition>(renderable.Entity))
        {
            var gridPos = World.Get<GridPosition>(renderable.Entity);
            worldPos = MovementSystem.GetInterpolatedPosition(gridPos);
        }
        else
        {
            worldPos = new Vector2(position.X, position.Y);
        }

        // Apply sprite facing based on Direction component if available
        SpriteEffects effects = SpriteEffects.None;
        if (World.Has<Direction>(renderable.Entity))
        {
            var direction = World.Get<Direction>(renderable.Entity);
            // Flip sprite horizontally when facing west/southwest/northwest
            if (
                direction == Direction.West
                || direction == Direction.SouthWest
                || direction == Direction.NorthWest
            )
            {
                effects = SpriteEffects.FlipHorizontally;
            }
        }

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
            worldPos,
            sprite.SourceRectangle,
            color,
            sprite.Rotation,
            sprite.Origin,
            sprite.Scale,
            effects,
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
        _spriteBatch.Draw(
            _debugTexture,
            new Rectangle(bounds.Left, bounds.Top, bounds.Width, thickness),
            debugColor
        );
        // Bottom
        _spriteBatch.Draw(
            _debugTexture,
            new Rectangle(bounds.Left, bounds.Bottom - thickness, bounds.Width, thickness),
            debugColor
        );
        // Left
        _spriteBatch.Draw(
            _debugTexture,
            new Rectangle(bounds.Left, bounds.Top, thickness, bounds.Height),
            debugColor
        );
        // Right
        _spriteBatch.Draw(
            _debugTexture,
            new Rectangle(bounds.Right - thickness, bounds.Top, thickness, bounds.Height),
            debugColor
        );
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
        _logger.LogWarning("Texture not found in cache: {TexturePath}", texturePath);
        return null;
    }

    /// <summary>
    /// Registers a texture in the cache for use by entities.
    /// </summary>
    public void RegisterTexture(string texturePath, Texture2D texture)
    {
        if (string.IsNullOrEmpty(texturePath))
        {
            throw new ArgumentException(
                "Texture path cannot be null or empty",
                nameof(texturePath)
            );
        }

        if (texture == null)
        {
            throw new ArgumentNullException(nameof(texture));
        }

        _textureCache[texturePath] = texture;
        _logger.LogInformation("Registered texture: {TexturePath}", texturePath);
    }

    /// <summary>
    /// Clears the texture cache.
    /// </summary>
    public void ClearTextureCache()
    {
        _textureCache.Clear();
        _logger.LogInformation("Texture cache cleared");
    }

    public override void Dispose()
    {
        _spriteBatch?.Dispose();
        _debugTexture?.Dispose();
        _textureCache.Clear();

        _logger.LogInformation("RenderSystem disposed");
        base.Dispose();
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
