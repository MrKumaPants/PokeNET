using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Services;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Audio.Reactive;

/// <summary>
/// Main reactive audio engine that subscribes to game events and dynamically
/// adjusts music and sound based on game state changes.
/// REFACTORED: Now uses Strategy Pattern with IAudioReaction for extensibility.
/// SOLID PRINCIPLE: Open/Closed - New reactions can be added without modifying this class.
/// SOLID PRINCIPLE: Single Responsibility - Only manages event subscription and delegation.
/// SOLID PRINCIPLE: Dependency Inversion - Depends on abstractions (IAudioReaction, IEventBus).
/// </summary>
public class ReactiveAudioEngine : IDisposable
{
    private readonly ILogger<ReactiveAudioEngine> _logger;
    private readonly IAudioManager _audioManager;
    private readonly IEventBus _eventBus;
    private readonly AudioReactionRegistry _reactionRegistry;

    private bool _isInitialized;
    private bool _isDisposed;

    /// <summary>
    /// Gets whether the reactive audio engine is initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Gets the reaction registry for managing audio reactions.
    /// </summary>
    public AudioReactionRegistry ReactionRegistry => _reactionRegistry;

    /// <summary>
    /// Initializes a new instance of the ReactiveAudioEngine class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="audioManager">Audio manager for playback control.</param>
    /// <param name="eventBus">Event bus for game event subscriptions.</param>
    /// <param name="reactionRegistry">Registry containing all audio reactions.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public ReactiveAudioEngine(
        ILogger<ReactiveAudioEngine> logger,
        IAudioManager audioManager,
        IEventBus eventBus,
        AudioReactionRegistry reactionRegistry
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _audioManager = audioManager ?? throw new ArgumentNullException(nameof(audioManager));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _reactionRegistry =
            reactionRegistry ?? throw new ArgumentNullException(nameof(reactionRegistry));
    }

    /// <summary>
    /// Initializes the reactive audio engine and subscribes to all game events.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the initialization operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            _logger.LogWarning("ReactiveAudioEngine is already initialized");
            return;
        }

        _logger.LogInformation(
            "Initializing ReactiveAudioEngine with {ReactionCount} reactions...",
            _reactionRegistry.Reactions.Count
        );

        try
        {
            // Subscribe to all game events with a single generic handler
            _eventBus.Subscribe<IGameEvent>(OnGameEventAsync);

            _isInitialized = true;
            _logger.LogInformation("ReactiveAudioEngine initialized successfully");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ReactiveAudioEngine");
            throw;
        }
    }

    /// <summary>
    /// Generic event handler that delegates to appropriate reactions.
    /// This is the single entry point for all game events.
    /// </summary>
    /// <param name="gameEvent">The game event to process.</param>
    private async void OnGameEventAsync(IGameEvent gameEvent)
    {
        if (!_isInitialized || _isDisposed)
            return;

        try
        {
            // Get all reactions that can handle this event
            var reactions = _reactionRegistry.GetReactionsForEvent(gameEvent);

            // Execute each reaction asynchronously
            foreach (var reaction in reactions)
            {
                try
                {
                    await reaction.ReactAsync(gameEvent, _audioManager);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error executing reaction {ReactionType} for event {EventType}",
                        reaction.GetType().Name,
                        gameEvent.GetType().Name
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing game event {EventType}",
                gameEvent.GetType().Name
            );
        }
    }

    #region Audio Control

    /// <summary>
    /// Pauses all audio (music and ambient).
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    public async Task PauseAllAsync()
    {
        _logger.LogInformation("Pausing all audio");
        await _audioManager.PauseMusicAsync();
        await _audioManager.PauseAmbientAsync();
    }

    /// <summary>
    /// Resumes all audio (music and ambient).
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    public async Task ResumeAllAsync()
    {
        _logger.LogInformation("Resuming all audio");
        await _audioManager.ResumeMusicAsync();
        await _audioManager.ResumeAmbientAsync();
    }

    #endregion

    /// <summary>
    /// Disposes the reactive audio engine and unsubscribes from events.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _logger.LogInformation("Disposing ReactiveAudioEngine");

        try
        {
            // Unsubscribe from all events
            _eventBus.Unsubscribe<IGameEvent>(OnGameEventAsync);

            _isDisposed = true;
            _logger.LogInformation("ReactiveAudioEngine disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ReactiveAudioEngine disposal");
        }
    }
}
