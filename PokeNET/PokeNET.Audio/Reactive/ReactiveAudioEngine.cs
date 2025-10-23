using System;
using System.Collections.Generic;
using System.Linq;

namespace PokeNET.Audio.Reactive
{
    /// <summary>
    /// Main reactive audio engine that subscribes to game events and dynamically
    /// adjusts music and sound based on game state changes.
    /// </summary>
    public class ReactiveAudioEngine : IDisposable
    {
        private readonly AudioStateController _stateController;
        private readonly MusicTransitionManager _transitionManager;
        private readonly AudioEventHandler _eventHandler;
        private readonly Dictionary<string, Action<object>> _eventSubscriptions;

        private bool _isInitialized;
        private bool _isDisposed;
        private GameState _currentGameState;

        public bool IsEnabled { get; set; }
        public float MasterVolume { get; set; }
        public GameState CurrentState => _currentGameState;

        public ReactiveAudioEngine(
            AudioStateController stateController,
            MusicTransitionManager transitionManager,
            AudioEventHandler eventHandler)
        {
            _stateController = stateController ?? throw new ArgumentNullException(nameof(stateController));
            _transitionManager = transitionManager ?? throw new ArgumentNullException(nameof(transitionManager));
            _eventHandler = eventHandler ?? throw new ArgumentNullException(nameof(eventHandler));

            _eventSubscriptions = new Dictionary<string, Action<object>>();
            IsEnabled = true;
            MasterVolume = 1.0f;
            _currentGameState = new GameState();
        }

        /// <summary>
        /// Initialize the reactive audio engine and subscribe to all game events.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("ReactiveAudioEngine is already initialized.");
            }

            SubscribeToGameEvents();
            _stateController.Initialize();
            _transitionManager.Initialize();
            _eventHandler.Initialize();

            _isInitialized = true;
        }

        /// <summary>
        /// Subscribe to all relevant game events.
        /// </summary>
        private void SubscribeToGameEvents()
        {
            // Battle events
            SubscribeToEvent("BattleStarted", OnBattleStarted);
            SubscribeToEvent("BattleEnded", OnBattleEnded);
            SubscribeToEvent("BattleIntensityChanged", OnBattleIntensityChanged);
            SubscribeToEvent("PokemonHealthCritical", OnPokemonHealthCritical);

            // Location events
            SubscribeToEvent("LocationChanged", OnLocationChanged);
            SubscribeToEvent("EnteredBuilding", OnEnteredBuilding);
            SubscribeToEvent("ExitedBuilding", OnExitedBuilding);
            SubscribeToEvent("EncounteredWildPokemon", OnEncounteredWildPokemon);

            // Time events
            SubscribeToEvent("TimeOfDayChanged", OnTimeOfDayChanged);
            SubscribeToEvent("WeatherChanged", OnWeatherChanged);

            // UI events
            SubscribeToEvent("MenuOpened", OnMenuOpened);
            SubscribeToEvent("MenuClosed", OnMenuClosed);
            SubscribeToEvent("DialogueStarted", OnDialogueStarted);
            SubscribeToEvent("DialogueEnded", OnDialogueEnded);

            // Story events
            SubscribeToEvent("StoryEventTriggered", OnStoryEventTriggered);
            SubscribeToEvent("BossBattleStarted", OnBossBattleStarted);
        }

        /// <summary>
        /// Subscribe to a specific game event.
        /// </summary>
        private void SubscribeToEvent(string eventName, Action<object> handler)
        {
            if (!_eventSubscriptions.ContainsKey(eventName))
            {
                _eventSubscriptions[eventName] = handler;
            }
        }

        /// <summary>
        /// Trigger a game event and process it through the reactive system.
        /// </summary>
        public void TriggerEvent(string eventName, object eventData = null)
        {
            if (!IsEnabled || !_isInitialized)
            {
                return;
            }

            if (_eventSubscriptions.TryGetValue(eventName, out var handler))
            {
                handler?.Invoke(eventData);
            }
        }

        #region Event Handlers

        private void OnBattleStarted(object data)
        {
            var battleData = data as BattleEventData;
            _currentGameState.InBattle = true;
            _currentGameState.BattleType = battleData?.BattleType ?? BattleType.Wild;
            _currentGameState.IsTrainerBattle = battleData?.IsTrainerBattle ?? false;

            var musicTrack = _stateController.GetMusicForState(_currentGameState);
            _transitionManager.TransitionToTrack(musicTrack, TransitionType.Crossfade, 2.0f);

            _eventHandler.HandleEvent(new AudioEvent
            {
                EventType = AudioEventType.BattleStart,
                Priority = AudioPriority.High,
                Data = battleData
            });
        }

        private void OnBattleEnded(object data)
        {
            _currentGameState.InBattle = false;
            _currentGameState.BattleType = BattleType.None;

            var musicTrack = _stateController.GetMusicForState(_currentGameState);
            _transitionManager.TransitionToTrack(musicTrack, TransitionType.Crossfade, 3.0f);

            _eventHandler.HandleEvent(new AudioEvent
            {
                EventType = AudioEventType.BattleEnd,
                Priority = AudioPriority.Medium,
                Data = data
            });
        }

        private void OnBattleIntensityChanged(object data)
        {
            var intensity = data is float f ? f : 0.5f;
            _currentGameState.BattleIntensity = intensity;

            // Adjust music intensity dynamically
            _transitionManager.AdjustIntensity(intensity);
        }

        private void OnPokemonHealthCritical(object data)
        {
            _eventHandler.HandleEvent(new AudioEvent
            {
                EventType = AudioEventType.HealthCritical,
                Priority = AudioPriority.Critical,
                Data = data,
                RequiresDucking = true,
                DuckingAmount = 0.4f
            });
        }

        private void OnLocationChanged(object data)
        {
            var locationData = data as LocationEventData;
            _currentGameState.CurrentLocation = locationData?.LocationName ?? "Unknown";
            _currentGameState.LocationType = locationData?.LocationType ?? LocationType.Route;

            var musicTrack = _stateController.GetMusicForState(_currentGameState);
            _transitionManager.TransitionToTrack(musicTrack, TransitionType.Fade, 2.5f);

            _eventHandler.HandleEvent(new AudioEvent
            {
                EventType = AudioEventType.LocationChange,
                Priority = AudioPriority.High,
                Data = locationData
            });
        }

        private void OnEnteredBuilding(object data)
        {
            _currentGameState.IsIndoors = true;

            // Apply indoor audio effects
            _transitionManager.ApplyEnvironmentFilter(EnvironmentFilter.Indoor);

            var musicTrack = _stateController.GetMusicForState(_currentGameState);
            _transitionManager.TransitionToTrack(musicTrack, TransitionType.Crossfade, 1.5f);
        }

        private void OnExitedBuilding(object data)
        {
            _currentGameState.IsIndoors = false;

            // Remove indoor audio effects
            _transitionManager.ApplyEnvironmentFilter(EnvironmentFilter.Outdoor);

            var musicTrack = _stateController.GetMusicForState(_currentGameState);
            _transitionManager.TransitionToTrack(musicTrack, TransitionType.Crossfade, 1.5f);
        }

        private void OnEncounteredWildPokemon(object data)
        {
            _eventHandler.HandleEvent(new AudioEvent
            {
                EventType = AudioEventType.WildEncounter,
                Priority = AudioPriority.Critical,
                Data = data,
                RequiresDucking = true,
                DuckingAmount = 0.6f
            });
        }

        private void OnTimeOfDayChanged(object data)
        {
            var timeData = data as TimeEventData;
            _currentGameState.TimeOfDay = timeData?.TimeOfDay ?? TimeOfDay.Day;

            var musicTrack = _stateController.GetMusicForState(_currentGameState);
            _transitionManager.TransitionToTrack(musicTrack, TransitionType.Fade, 4.0f);
        }

        private void OnWeatherChanged(object data)
        {
            var weatherData = data as WeatherEventData;
            _currentGameState.Weather = weatherData?.Weather ?? Weather.Clear;

            // Apply weather-based audio effects
            _transitionManager.ApplyWeatherEffects(_currentGameState.Weather);
        }

        private void OnMenuOpened(object data)
        {
            _currentGameState.MenuOpen = true;
            _transitionManager.DuckMusic(0.3f, 0.5f);
        }

        private void OnMenuClosed(object data)
        {
            _currentGameState.MenuOpen = false;
            _transitionManager.RestoreMusicVolume(0.5f);
        }

        private void OnDialogueStarted(object data)
        {
            _currentGameState.InDialogue = true;
            _transitionManager.DuckMusic(0.5f, 0.3f);
        }

        private void OnDialogueEnded(object data)
        {
            _currentGameState.InDialogue = false;
            _transitionManager.RestoreMusicVolume(0.5f);
        }

        private void OnStoryEventTriggered(object data)
        {
            var storyData = data as StoryEventData;
            _currentGameState.CurrentStoryEvent = storyData?.EventId ?? "";

            if (storyData?.HasCustomMusic ?? false)
            {
                _transitionManager.TransitionToTrack(storyData.MusicTrack, TransitionType.Crossfade, 2.0f);
            }
        }

        private void OnBossBattleStarted(object data)
        {
            _currentGameState.InBattle = true;
            _currentGameState.BattleType = BattleType.Boss;

            var musicTrack = _stateController.GetMusicForState(_currentGameState);
            _transitionManager.TransitionToTrack(musicTrack, TransitionType.Immediate, 0.0f);

            _eventHandler.HandleEvent(new AudioEvent
            {
                EventType = AudioEventType.BossBattle,
                Priority = AudioPriority.Critical,
                Data = data
            });
        }

        #endregion

        /// <summary>
        /// Update the reactive audio engine (call per frame).
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!IsEnabled || !_isInitialized)
            {
                return;
            }

            _transitionManager.Update(deltaTime);
            _eventHandler.Update(deltaTime);
        }

        /// <summary>
        /// Force an immediate state update based on current game state.
        /// </summary>
        public void ForceStateUpdate()
        {
            if (!_isInitialized)
            {
                return;
            }

            var musicTrack = _stateController.GetMusicForState(_currentGameState);
            _transitionManager.TransitionToTrack(musicTrack, TransitionType.Crossfade, 1.0f);
        }

        /// <summary>
        /// Get current audio system status for debugging.
        /// </summary>
        public AudioSystemStatus GetStatus()
        {
            return new AudioSystemStatus
            {
                IsEnabled = IsEnabled,
                CurrentState = _currentGameState,
                CurrentTrack = _transitionManager.CurrentTrack,
                IsTransitioning = _transitionManager.IsTransitioning,
                ActiveEvents = _eventHandler.GetActiveEventCount()
            };
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _eventSubscriptions.Clear();
            _stateController?.Dispose();
            _transitionManager?.Dispose();
            _eventHandler?.Dispose();

            _isDisposed = true;
        }
    }

    #region Supporting Classes and Enums

    public class GameState
    {
        public bool InBattle { get; set; }
        public BattleType BattleType { get; set; }
        public bool IsTrainerBattle { get; set; }
        public float BattleIntensity { get; set; }
        public string CurrentLocation { get; set; }
        public LocationType LocationType { get; set; }
        public bool IsIndoors { get; set; }
        public TimeOfDay TimeOfDay { get; set; }
        public Weather Weather { get; set; }
        public bool MenuOpen { get; set; }
        public bool InDialogue { get; set; }
        public string CurrentStoryEvent { get; set; }
    }

    public enum BattleType
    {
        None,
        Wild,
        Trainer,
        Gym,
        Boss,
        EliteFour
    }

    public enum LocationType
    {
        Route,
        Town,
        City,
        Cave,
        Water,
        Building,
        Forest,
        Mountain
    }

    public enum TimeOfDay
    {
        Morning,
        Day,
        Evening,
        Night
    }

    public enum Weather
    {
        Clear,
        Rain,
        Storm,
        Snow,
        Fog,
        Sandstorm
    }

    public class BattleEventData
    {
        public BattleType BattleType { get; set; }
        public bool IsTrainerBattle { get; set; }
        public string OpponentName { get; set; }
        public int OpponentLevel { get; set; }
    }

    public class LocationEventData
    {
        public string LocationName { get; set; }
        public LocationType LocationType { get; set; }
        public string Region { get; set; }
    }

    public class TimeEventData
    {
        public TimeOfDay TimeOfDay { get; set; }
        public int Hour { get; set; }
    }

    public class WeatherEventData
    {
        public Weather Weather { get; set; }
        public float Intensity { get; set; }
    }

    public class StoryEventData
    {
        public string EventId { get; set; }
        public bool HasCustomMusic { get; set; }
        public string MusicTrack { get; set; }
    }

    public class AudioSystemStatus
    {
        public bool IsEnabled { get; set; }
        public GameState CurrentState { get; set; }
        public string CurrentTrack { get; set; }
        public bool IsTransitioning { get; set; }
        public int ActiveEvents { get; set; }
    }

    #endregion
}
