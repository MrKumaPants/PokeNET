using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace PokeNET.Audio.Reactive
{
    /// <summary>
    /// Controls state-based music selection by mapping game states to appropriate music tracks.
    /// Implements a sophisticated state matching system with priority and fallback mechanisms.
    /// </summary>
    public class AudioStateController : IDisposable
    {
        private readonly ILogger<AudioStateController> _logger;
        private readonly Dictionary<string, MusicStateRule> _stateRules;
        private readonly Dictionary<string, List<string>> _locationMusicMap;
        private readonly Dictionary<BattleType, List<string>> _battleMusicMap;
        private readonly Dictionary<TimeOfDay, string> _ambientMusicMap;

        private bool _isInitialized;
        private bool _isDisposed;
        private string _defaultMusic;
        private string? _lastSelectedTrack;

        /// <summary>
        /// Initializes a new instance of the AudioStateController class.
        /// </summary>
        /// <param name="logger">Logger for diagnostics.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        public AudioStateController(ILogger<AudioStateController> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stateRules = new Dictionary<string, MusicStateRule>();
            _locationMusicMap = new Dictionary<string, List<string>>();
            _battleMusicMap = new Dictionary<BattleType, List<string>>();
            _ambientMusicMap = new Dictionary<TimeOfDay, string>();

            _defaultMusic = "default_ambient";
        }

        /// <summary>
        /// Initialize the audio state controller with music mappings.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                _logger.LogWarning("AudioStateController is already initialized");
                return;
            }

            _logger.LogInformation("Initializing AudioStateController...");

            SetupDefaultMusicMappings();
            SetupStateRules();

            _isInitialized = true;
            _logger.LogInformation("AudioStateController initialized successfully");
        }

        /// <summary>
        /// Setup default music mappings for various game states.
        /// </summary>
        private void SetupDefaultMusicMappings()
        {
            // Battle music mappings
            _battleMusicMap[BattleType.Wild] = new List<string>
            {
                "battle_wild_1",
                "battle_wild_2",
                "battle_wild_intense",
            };

            _battleMusicMap[BattleType.Trainer] = new List<string>
            {
                "battle_trainer_1",
                "battle_trainer_2",
                "battle_trainer_epic",
            };

            _battleMusicMap[BattleType.Gym] = new List<string>
            {
                "battle_gym_leader",
                "battle_gym_leader_final",
            };

            _battleMusicMap[BattleType.Boss] = new List<string>
            {
                "battle_boss",
                "battle_legendary",
            };

            _battleMusicMap[BattleType.EliteFour] = new List<string>
            {
                "battle_elite_four",
                "battle_champion",
            };

            // Location music mappings
            _locationMusicMap["route"] = new List<string>
            {
                "route_1",
                "route_2",
                "route_adventure",
            };

            _locationMusicMap["town"] = new List<string>
            {
                "town_peaceful",
                "town_day",
                "town_night",
            };

            _locationMusicMap["city"] = new List<string>
            {
                "city_bustling",
                "city_modern",
                "city_night",
            };

            _locationMusicMap["cave"] = new List<string>
            {
                "cave_dark",
                "cave_mystery",
                "cave_deep",
            };

            _locationMusicMap["forest"] = new List<string>
            {
                "forest_calm",
                "forest_enchanted",
                "forest_dense",
            };

            // Time of day ambient music
            _ambientMusicMap[TimeOfDay.Morning] = "ambient_morning";
            _ambientMusicMap[TimeOfDay.Day] = "ambient_day";
            _ambientMusicMap[TimeOfDay.Evening] = "ambient_evening";
            _ambientMusicMap[TimeOfDay.Night] = "ambient_night";
        }

        /// <summary>
        /// Setup priority-based state rules for music selection.
        /// </summary>
        private void SetupStateRules()
        {
            // Battle states have highest priority
            AddStateRule(
                new MusicStateRule
                {
                    Name = "BossBattle",
                    Priority = 100,
                    MatchCondition = state => state.InBattle && state.BattleType == BattleType.Boss,
                    MusicSelector = state =>
                        SelectBattleMusic(state.BattleType, state.BattleIntensity),
                }
            );

            AddStateRule(
                new MusicStateRule
                {
                    Name = "GymBattle",
                    Priority = 90,
                    MatchCondition = state => state.InBattle && state.BattleType == BattleType.Gym,
                    MusicSelector = state =>
                        SelectBattleMusic(state.BattleType, state.BattleIntensity),
                }
            );

            AddStateRule(
                new MusicStateRule
                {
                    Name = "TrainerBattle",
                    Priority = 80,
                    MatchCondition = state => state.InBattle && state.IsTrainerBattle,
                    MusicSelector = state =>
                        SelectBattleMusic(BattleType.Trainer, state.BattleIntensity),
                }
            );

            AddStateRule(
                new MusicStateRule
                {
                    Name = "WildBattle",
                    Priority = 70,
                    MatchCondition = state => state.InBattle && state.BattleType == BattleType.Wild,
                    MusicSelector = state =>
                        SelectBattleMusic(BattleType.Wild, state.BattleIntensity),
                }
            );

            // Location-based states
            AddStateRule(
                new MusicStateRule
                {
                    Name = "IndoorLocation",
                    Priority = 60,
                    MatchCondition = state => state.IsIndoors && !state.InBattle,
                    MusicSelector = state => SelectIndoorMusic(state.CurrentLocation),
                }
            );

            AddStateRule(
                new MusicStateRule
                {
                    Name = "CaveLocation",
                    Priority = 55,
                    MatchCondition = state =>
                        state.LocationType == LocationType.Cave && !state.InBattle,
                    MusicSelector = state => SelectLocationMusic("cave", state.TimeOfDay),
                }
            );

            AddStateRule(
                new MusicStateRule
                {
                    Name = "CityLocation",
                    Priority = 50,
                    MatchCondition = state =>
                        state.LocationType == LocationType.City && !state.InBattle,
                    MusicSelector = state => SelectLocationMusic("city", state.TimeOfDay),
                }
            );

            AddStateRule(
                new MusicStateRule
                {
                    Name = "TownLocation",
                    Priority = 45,
                    MatchCondition = state =>
                        state.LocationType == LocationType.Town && !state.InBattle,
                    MusicSelector = state => SelectLocationMusic("town", state.TimeOfDay),
                }
            );

            AddStateRule(
                new MusicStateRule
                {
                    Name = "RouteLocation",
                    Priority = 40,
                    MatchCondition = state =>
                        state.LocationType == LocationType.Route && !state.InBattle,
                    MusicSelector = state => SelectLocationMusic("route", state.TimeOfDay),
                }
            );

            // Time-based ambient music (lowest priority)
            AddStateRule(
                new MusicStateRule
                {
                    Name = "AmbientMusic",
                    Priority = 10,
                    MatchCondition = state => !state.InBattle,
                    MusicSelector = state => SelectAmbientMusic(state.TimeOfDay),
                }
            );
        }

        /// <summary>
        /// Add a custom state rule to the controller.
        /// </summary>
        public void AddStateRule(MusicStateRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            _stateRules[rule.Name] = rule;
        }

        /// <summary>
        /// Get the appropriate music track for the current game state.
        /// </summary>
        public string GetMusicForState(ReactiveAudioState state)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("AudioStateController is not initialized.");
            }

            if (state == null)
            {
                return _defaultMusic;
            }

            // Find the highest priority matching rule
            var matchingRule = _stateRules
                .Values.Where(rule => rule.MatchCondition(state))
                .OrderByDescending(rule => rule.Priority)
                .FirstOrDefault();

            if (matchingRule != null)
            {
                var selectedTrack = matchingRule.MusicSelector(state);
                if (!string.IsNullOrEmpty(selectedTrack))
                {
                    _lastSelectedTrack = selectedTrack;
                    return selectedTrack;
                }
            }

            // Fallback to default music
            return _defaultMusic;
        }

        /// <summary>
        /// Select battle music based on battle type and intensity.
        /// </summary>
        private string SelectBattleMusic(BattleType battleType, float intensity)
        {
            if (!_battleMusicMap.TryGetValue(battleType, out var musicList) || musicList.Count == 0)
            {
                return "battle_default";
            }

            // Select music based on intensity (0.0 to 1.0)
            int index = Math.Min((int)(intensity * musicList.Count), musicList.Count - 1);
            return musicList[index];
        }

        /// <summary>
        /// Select location-based music with time of day variation.
        /// </summary>
        private string SelectLocationMusic(string locationType, TimeOfDay timeOfDay)
        {
            var locationKey = locationType.ToLower();

            if (
                !_locationMusicMap.TryGetValue(locationKey, out var musicList)
                || musicList.Count == 0
            )
            {
                return SelectAmbientMusic(timeOfDay);
            }

            // Prefer time-specific tracks if available
            var timeSpecificTrack = musicList.FirstOrDefault(track =>
                track.Contains(timeOfDay.ToString().ToLower())
            );

            if (!string.IsNullOrEmpty(timeSpecificTrack))
            {
                return timeSpecificTrack;
            }

            // Otherwise return the first track
            return musicList[0];
        }

        /// <summary>
        /// Select indoor music based on building type.
        /// </summary>
        private string SelectIndoorMusic(string locationName)
        {
            if (string.IsNullOrEmpty(locationName))
            {
                return "indoor_default";
            }

            var lowerName = locationName.ToLower();

            if (lowerName.Contains("pokecenter") || lowerName.Contains("center"))
            {
                return "pokecenter_theme";
            }

            if (
                lowerName.Contains("pokemart")
                || lowerName.Contains("mart")
                || lowerName.Contains("shop")
            )
            {
                return "pokemart_theme";
            }

            if (lowerName.Contains("gym"))
            {
                return "gym_interior";
            }

            if (lowerName.Contains("lab") || lowerName.Contains("laboratory"))
            {
                return "lab_theme";
            }

            return "indoor_default";
        }

        /// <summary>
        /// Select ambient music based on time of day.
        /// </summary>
        private string SelectAmbientMusic(TimeOfDay timeOfDay)
        {
            if (_ambientMusicMap.TryGetValue(timeOfDay, out var ambientTrack))
            {
                return ambientTrack;
            }

            return _defaultMusic;
        }

        /// <summary>
        /// Register custom location music.
        /// </summary>
        public void RegisterLocationMusic(string location, List<string> tracks)
        {
            if (string.IsNullOrEmpty(location) || tracks == null || tracks.Count == 0)
            {
                return;
            }

            _locationMusicMap[location.ToLower()] = new List<string>(tracks);
        }

        /// <summary>
        /// Register custom battle music.
        /// </summary>
        public void RegisterBattleMusic(BattleType battleType, List<string> tracks)
        {
            if (tracks == null || tracks.Count == 0)
            {
                return;
            }

            _battleMusicMap[battleType] = new List<string>(tracks);
        }

        /// <summary>
        /// Set default music track.
        /// </summary>
        public void SetDefaultMusic(string trackName)
        {
            if (!string.IsNullOrEmpty(trackName))
            {
                _defaultMusic = trackName;
            }
        }

        /// <summary>
        /// Get the last selected track.
        /// </summary>
        public string GetLastSelectedTrack()
        {
            return _lastSelectedTrack ?? _defaultMusic;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _stateRules.Clear();
            _locationMusicMap.Clear();
            _battleMusicMap.Clear();
            _ambientMusicMap.Clear();

            _isDisposed = true;
        }
    }

    /// <summary>
    /// Represents a rule for selecting music based on game state.
    /// </summary>
    public class MusicStateRule
    {
        /// <summary>
        /// Gets or sets the name of this rule.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the priority of this rule (higher = more important).
        /// </summary>
        public required int Priority { get; set; }

        /// <summary>
        /// Gets or sets the condition that must be met for this rule to match.
        /// </summary>
        public required Func<ReactiveAudioState, bool> MatchCondition { get; set; }

        /// <summary>
        /// Gets or sets the function that selects music when this rule matches.
        /// </summary>
        public required Func<ReactiveAudioState, string> MusicSelector { get; set; }
    }
}
