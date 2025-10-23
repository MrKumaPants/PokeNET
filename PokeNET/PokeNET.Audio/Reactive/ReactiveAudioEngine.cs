using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;
using PokeNET.Domain.ECS.Events;
using PokeNET.Domain.Models;
using GameState = PokeNET.Domain.Models.GameState;

namespace PokeNET.Audio.Reactive
{
    /// <summary>
    /// Main reactive audio engine that subscribes to game events and dynamically
    /// adjusts music and sound based on game state changes.
    /// </summary>
    public class ReactiveAudioEngine : IDisposable
    {
        private readonly ILogger<ReactiveAudioEngine> _logger;
        private readonly IAudioManager _audioManager;
        private readonly IEventBus _eventBus;
        private readonly Dictionary<AudioReactionType, bool> _reactionStates;

        private bool _isInitialized;
        private bool _isDisposed;
        private GameState _currentGameState;
        private float _currentHealthPercentage = 1.0f;
        private bool _isLowHealthMusicPlaying;

        /// <summary>
        /// Gets whether the reactive audio engine is initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Initializes a new instance of the ReactiveAudioEngine class.
        /// </summary>
        /// <param name="logger">Logger for diagnostics.</param>
        /// <param name="audioManager">Audio manager for playback control.</param>
        /// <param name="eventBus">Event bus for game event subscriptions.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public ReactiveAudioEngine(
            ILogger<ReactiveAudioEngine> logger,
            IAudioManager audioManager,
            IEventBus eventBus)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _audioManager = audioManager ?? throw new ArgumentNullException(nameof(audioManager));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            _reactionStates = new Dictionary<AudioReactionType, bool>();
            InitializeReactionStates();
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

            _logger.LogInformation("Initializing ReactiveAudioEngine...");

            try
            {
                // Subscribe to game events
                _eventBus.Subscribe<GameStateChangedEvent>(OnGameStateChangedEvent);
                _eventBus.Subscribe<BattleEvent>(OnBattleEvent);
                _eventBus.Subscribe<HealthChangedEvent>(OnHealthChangedEvent);
                _eventBus.Subscribe<WeatherChangedEvent>(OnWeatherChangedEvent);
                _eventBus.Subscribe<ItemUseEvent>(OnItemUseEvent);
                _eventBus.Subscribe<PokemonCaughtEvent>(OnPokemonCaughtEvent);
                _eventBus.Subscribe<LevelUpEvent>(OnLevelUpEvent);

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
        /// Initializes all reaction states to enabled by default.
        /// </summary>
        private void InitializeReactionStates()
        {
            foreach (AudioReactionType reactionType in Enum.GetValues(typeof(AudioReactionType)))
            {
                _reactionStates[reactionType] = true;
            }
        }

        /// <summary>
        /// Sets whether a specific audio reaction type is enabled.
        /// </summary>
        /// <param name="type">The reaction type to configure.</param>
        /// <param name="enabled">True to enable, false to disable.</param>
        public void SetReactionEnabled(AudioReactionType type, bool enabled)
        {
            _reactionStates[type] = enabled;
            _logger.LogDebug("Audio reaction {ReactionType} set to {Enabled}", type, enabled);
        }

        /// <summary>
        /// Gets whether a specific audio reaction type is enabled.
        /// </summary>
        /// <param name="type">The reaction type to check.</param>
        /// <returns>True if enabled, false otherwise.</returns>
        public bool IsReactionEnabled(AudioReactionType type)
        {
            return _reactionStates.TryGetValue(type, out var enabled) && enabled;
        }

        #region Event Handlers

        /// <summary>
        /// Handles game state changed events.
        /// </summary>
        private void OnGameStateChangedEvent(GameStateChangedEvent evt)
        {
            OnGameStateChangedAsync(evt).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Handles game state changes asynchronously.
        /// </summary>
        /// <param name="evt">The game state changed event.</param>
        /// <returns>A task representing the operation.</returns>
        public async Task OnGameStateChangedAsync(GameStateChangedEvent evt)
        {
            await OnGameStateChangedAsync(evt.PreviousState, evt.NewState);
        }

        /// <summary>
        /// Handles game state changes asynchronously.
        /// </summary>
        /// <param name="previousState">The previous game state.</param>
        /// <param name="newState">The new game state.</param>
        /// <returns>A task representing the operation.</returns>
        public async Task OnGameStateChangedAsync(GameState previousState, GameState newState)
        {
            _logger.LogInformation("Game state changed from {PreviousState} to {NewState}", previousState, newState);
            _currentGameState = newState;

            // Handle battle state
            if (newState == GameState.Battle && IsReactionEnabled(AudioReactionType.BattleMusic))
            {
                await _audioManager.PlayMusicAsync("audio/music/battle_wild.ogg", 1.0f);
            }
            else if (previousState == GameState.Battle && newState == GameState.Overworld && IsReactionEnabled(AudioReactionType.LocationMusic))
            {
                await _audioManager.PlayMusicAsync("audio/music/route_01.ogg", 1.0f);
            }
            // Handle menu state
            else if (newState == GameState.Menu && IsReactionEnabled(AudioReactionType.MenuDucking))
            {
                await _audioManager.DuckMusicAsync(0.3f);
            }
            else if (previousState == GameState.Menu && IsReactionEnabled(AudioReactionType.MenuDucking))
            {
                await _audioManager.StopDuckingAsync();
            }
            // Handle overworld
            else if (newState == GameState.Overworld && IsReactionEnabled(AudioReactionType.LocationMusic))
            {
                await _audioManager.PlayMusicAsync("audio/music/route_01.ogg", 1.0f);
            }
            // Handle cutscene
            else if (newState == GameState.Cutscene)
            {
                _logger.LogInformation("Entered cutscene state");
            }
        }

        /// <summary>
        /// Handles battle events.
        /// </summary>
        private void OnBattleEvent(BattleEvent evt)
        {
            if (evt is BattleStartEvent startEvent)
            {
                OnBattleStartAsync(startEvent).GetAwaiter().GetResult();
            }
            else if (evt is BattleEndEvent endEvent)
            {
                OnBattleEndAsync(endEvent).GetAwaiter().GetResult();
            }
            else if (evt is PokemonFaintEvent faintEvent)
            {
                OnPokemonFaintAsync(faintEvent).GetAwaiter().GetResult();
            }
            else if (evt is AttackEvent attackEvent)
            {
                OnAttackUseAsync(attackEvent).GetAwaiter().GetResult();
            }
            else if (evt is CriticalHitEvent critEvent)
            {
                OnCriticalHitAsync(critEvent).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Handles battle start events.
        /// </summary>
        public async Task OnBattleStartAsync(BattleStartEvent evt)
        {
            if (!IsReactionEnabled(AudioReactionType.BattleSoundEffects))
                return;

            _logger.LogInformation("Battle started: Wild={IsWild}, Gym={IsGym}", evt.IsWildBattle, evt.IsGymLeader);

            // Play battle intro sound
            await _audioManager.PlaySoundEffectAsync("audio/sfx/battle_start.wav", 1.0f);

            if (!IsReactionEnabled(AudioReactionType.BattleMusic))
                return;

            // Play appropriate battle music
            if (evt.IsGymLeader)
            {
                await _audioManager.PlayMusicAsync("audio/music/battle_gym_leader.ogg", 1.0f);
            }
            else if (evt.IsWildBattle)
            {
                await _audioManager.PlayMusicAsync("audio/music/battle_wild.ogg", 1.0f);
            }
            else
            {
                await _audioManager.PlayMusicAsync("audio/music/battle_trainer.ogg", 1.0f);
            }
        }

        /// <summary>
        /// Handles battle end events.
        /// </summary>
        public async Task OnBattleEndAsync(BattleEndEvent evt)
        {
            if (!IsReactionEnabled(AudioReactionType.BattleMusic))
                return;

            _logger.LogInformation("Battle ended: Victory={PlayerWon}", evt.PlayerWon);

            if (evt.PlayerWon)
            {
                await _audioManager.PlayMusicAsync("audio/music/victory.ogg", 1.0f);
            }
        }

        /// <summary>
        /// Handles Pokemon faint events.
        /// </summary>
        public async Task OnPokemonFaintAsync(PokemonFaintEvent evt)
        {
            if (!IsReactionEnabled(AudioReactionType.BattleSoundEffects))
                return;

            _logger.LogInformation("Pokemon fainted: {PokemonName}", evt.PokemonName);
            await _audioManager.PlaySoundEffectAsync("audio/sfx/faint.wav", 1.0f);
        }

        /// <summary>
        /// Handles attack use events.
        /// </summary>
        public async Task OnAttackUseAsync(AttackEvent evt)
        {
            if (!IsReactionEnabled(AudioReactionType.BattleSoundEffects))
                return;

            _logger.LogDebug("Attack used: {AttackName} ({AttackType})", evt.AttackName, evt.AttackType);

            // Play type-specific attack sound
            var soundPath = $"audio/sfx/attack_{evt.AttackType?.ToLower() ?? "normal"}.wav";
            await _audioManager.PlaySoundEffectAsync(soundPath, 0.8f);
        }

        /// <summary>
        /// Handles critical hit events.
        /// </summary>
        public async Task OnCriticalHitAsync(CriticalHitEvent evt)
        {
            if (!IsReactionEnabled(AudioReactionType.BattleSoundEffects))
                return;

            _logger.LogInformation("Critical hit!");
            await _audioManager.PlaySoundEffectAsync("audio/sfx/critical.wav", 1.0f);
        }

        /// <summary>
        /// Handles health changed events.
        /// </summary>
        private void OnHealthChangedEvent(HealthChangedEvent evt)
        {
            OnHealthChangedAsync(evt).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Handles health changed events asynchronously.
        /// </summary>
        public async Task OnHealthChangedAsync(HealthChangedEvent evt)
        {
            if (!IsReactionEnabled(AudioReactionType.HealthMusic))
                return;

            _currentHealthPercentage = evt.HealthPercentage;
            const float lowHealthThreshold = 0.25f;

            if (evt.HealthPercentage <= lowHealthThreshold && !_isLowHealthMusicPlaying)
            {
                _logger.LogWarning("Health critical: {Percentage:P0}", evt.HealthPercentage);
                await _audioManager.PlayMusicAsync("audio/music/low_health.ogg", 1.0f);
                _isLowHealthMusicPlaying = true;
            }
            else if (evt.HealthPercentage > lowHealthThreshold && _isLowHealthMusicPlaying)
            {
                _logger.LogInformation("Health restored above critical threshold");
                await _audioManager.StopMusicAsync();
                _isLowHealthMusicPlaying = false;
            }
        }

        /// <summary>
        /// Handles weather changed events.
        /// </summary>
        private void OnWeatherChangedEvent(WeatherChangedEvent evt)
        {
            OnWeatherChangedAsync(evt).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Handles weather changed events asynchronously.
        /// </summary>
        public async Task OnWeatherChangedAsync(WeatherChangedEvent evt)
        {
            if (!IsReactionEnabled(AudioReactionType.WeatherAmbient))
                return;

            _logger.LogInformation("Weather changed to: {Weather}", evt.NewWeather);

            if (evt.NewWeather == Weather.Clear)
            {
                await _audioManager.StopAmbientAsync();
            }
            else
            {
                var ambientPath = evt.NewWeather switch
                {
                    Weather.Rain => "audio/ambient/rain.ogg",
                    Weather.Snow => "audio/ambient/snow.ogg",
                    Weather.Sandstorm => "audio/ambient/sandstorm.ogg",
                    Weather.Fog => "audio/ambient/fog.ogg",
                    _ => null
                };

                if (ambientPath != null)
                {
                    await _audioManager.PlayAmbientAsync(ambientPath, 0.6f);
                }
            }
        }

        /// <summary>
        /// Handles item use events.
        /// </summary>
        private void OnItemUseEvent(ItemUseEvent evt)
        {
            OnItemUseAsync(evt).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Handles item use events asynchronously.
        /// </summary>
        public async Task OnItemUseAsync(ItemUseEvent evt)
        {
            if (!IsReactionEnabled(AudioReactionType.ItemSoundEffects))
                return;

            _logger.LogDebug("Item used: {ItemName}", evt.ItemName);
            await _audioManager.PlaySoundEffectAsync("audio/sfx/item_use.wav", 0.7f);
        }

        /// <summary>
        /// Handles Pokemon caught events.
        /// </summary>
        private void OnPokemonCaughtEvent(PokemonCaughtEvent evt)
        {
            OnPokemonCaughtAsync(evt).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Handles Pokemon caught events asynchronously.
        /// </summary>
        public async Task OnPokemonCaughtAsync(PokemonCaughtEvent evt)
        {
            if (!IsReactionEnabled(AudioReactionType.PokemonSounds))
                return;

            _logger.LogInformation("Pokemon caught: {PokemonName}", evt.PokemonName);
            await _audioManager.PlaySoundEffectAsync("audio/sfx/pokemon_catch.wav", 1.0f);
        }

        /// <summary>
        /// Handles level up events.
        /// </summary>
        private void OnLevelUpEvent(LevelUpEvent evt)
        {
            OnLevelUpAsync(evt).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Handles level up events asynchronously.
        /// </summary>
        public async Task OnLevelUpAsync(LevelUpEvent evt)
        {
            if (!IsReactionEnabled(AudioReactionType.PokemonSounds))
                return;

            _logger.LogInformation("Pokemon leveled up to level {Level}", evt.NewLevel);
            await _audioManager.PlaySoundEffectAsync("audio/sfx/level_up.wav", 1.0f);
        }

        #endregion

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

        /// <summary>
        /// Gets the current music state.
        /// </summary>
        /// <returns>The current music state.</returns>
        public MusicState GetCurrentMusicState()
        {
            return new MusicState
            {
                IsPlaying = _audioManager.IsMusicPlaying,
                CurrentTrack = _audioManager.CurrentMusicTrack
            };
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
                _eventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChangedEvent);
                _eventBus.Unsubscribe<BattleEvent>(OnBattleEvent);
                _eventBus.Unsubscribe<HealthChangedEvent>(OnHealthChangedEvent);
                _eventBus.Unsubscribe<WeatherChangedEvent>(OnWeatherChangedEvent);
                _eventBus.Unsubscribe<ItemUseEvent>(OnItemUseEvent);
                _eventBus.Unsubscribe<PokemonCaughtEvent>(OnPokemonCaughtEvent);
                _eventBus.Unsubscribe<LevelUpEvent>(OnLevelUpEvent);

                _isDisposed = true;
                _logger.LogInformation("ReactiveAudioEngine disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ReactiveAudioEngine disposal");
            }
        }
    }

    #region Supporting Classes

    /// <summary>
    /// Represents the current music playback state.
    /// </summary>
    public class MusicState
    {
        /// <summary>
        /// Gets or sets whether music is currently playing.
        /// </summary>
        public bool IsPlaying { get; set; }

        /// <summary>
        /// Gets or sets the current music track name.
        /// </summary>
        public string CurrentTrack { get; set; } = string.Empty;
    }

    #endregion
}
