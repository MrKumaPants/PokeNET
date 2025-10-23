using System;
using System.Collections.Generic;
using System.Linq;
using PokeNET.Audio.Models;

namespace PokeNET.Audio.Mixing
{
    /// <summary>
    /// Ducking priority levels
    /// </summary>
    public enum DuckingPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Ducking rule configuration
    /// </summary>
    public class DuckingRule
    {
        /// <summary>
        /// Gets or sets the trigger channel type (channel that triggers ducking)
        /// </summary>
        public ChannelType TriggerChannel { get; set; }

        /// <summary>
        /// Gets or sets the affected channel types (channels that get ducked)
        /// </summary>
        public List<ChannelType> AffectedChannels { get; set; } = new();

        /// <summary>
        /// Gets or sets the ducking level (0.0 = full duck, 1.0 = no duck)
        /// </summary>
        public float DuckLevel { get; set; }

        /// <summary>
        /// Gets or sets the fade-in time in seconds
        /// </summary>
        public float FadeInTime { get; set; }

        /// <summary>
        /// Gets or sets the fade-out time in seconds
        /// </summary>
        public float FadeOutTime { get; set; }

        /// <summary>
        /// Gets or sets the priority of this ducking rule
        /// </summary>
        public DuckingPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets whether this rule is enabled
        /// </summary>
        public bool Enabled { get; set; }

        public DuckingRule()
        {
            DuckLevel = 0.3f;
            FadeInTime = 0.5f;
            FadeOutTime = 0.3f;
            Priority = DuckingPriority.Normal;
            Enabled = true;
        }
    }

    /// <summary>
    /// Active ducking state for a channel
    /// </summary>
    internal class DuckingState
    {
        public ChannelType ChannelType { get; set; }
        public float TargetDuckLevel { get; set; }
        public float CurrentDuckLevel { get; set; }
        public float FadeSpeed { get; set; }
        public bool IsActive { get; set; }
        public DuckingPriority Priority { get; set; }
        public DateTime ActivationTime { get; set; }

        public DuckingState()
        {
            TargetDuckLevel = 1.0f;
            CurrentDuckLevel = 1.0f;
            FadeSpeed = 1.0f;
            IsActive = false;
            Priority = DuckingPriority.Normal;
            ActivationTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Controls audio ducking behavior (lowering volume of certain channels when others play)
    /// </summary>
    public class DuckingController
    {
        private readonly Dictionary<ChannelType, DuckingState> _duckingStates;
        private readonly List<DuckingRule> _duckingRules;
        private readonly Dictionary<ChannelType, bool> _channelActivity;

        /// <summary>
        /// Gets or sets whether ducking is globally enabled
        /// </summary>
        public bool DuckingEnabled { get; set; }

        /// <summary>
        /// Gets or sets the minimum volume threshold to trigger ducking
        /// </summary>
        public float ActivityThreshold { get; set; }

        /// <summary>
        /// Gets the collection of ducking rules
        /// </summary>
        public IReadOnlyList<DuckingRule> DuckingRules => _duckingRules.AsReadOnly();

        /// <summary>
        /// Event fired when ducking state changes
        /// </summary>
        public event EventHandler<DuckingStateChangedEventArgs>? OnDuckingStateChanged;

        /// <summary>
        /// Initializes a new instance of the DuckingController class
        /// </summary>
        public DuckingController()
        {
            _duckingStates = new Dictionary<ChannelType, DuckingState>();
            _duckingRules = new List<DuckingRule>();
            _channelActivity = new Dictionary<ChannelType, bool>();

            DuckingEnabled = true;
            ActivityThreshold = 0.1f;

            InitializeDefaultRules();
            InitializeDuckingStates();
        }

        /// <summary>
        /// Initializes default ducking rules
        /// </summary>
        private void InitializeDefaultRules()
        {
            // Sound effects duck music
            AddDuckingRule(new DuckingRule
            {
                TriggerChannel = ChannelType.SoundEffects,
                AffectedChannels = new List<ChannelType> { ChannelType.Music, ChannelType.Ambient },
                DuckLevel = 0.4f,
                FadeInTime = 0.2f,
                FadeOutTime = 0.5f,
                Priority = DuckingPriority.Normal,
                Enabled = true
            });

            // Voice/dialogue ducks everything else
            AddDuckingRule(new DuckingRule
            {
                TriggerChannel = ChannelType.Voice,
                AffectedChannels = new List<ChannelType> { ChannelType.Music, ChannelType.SoundEffects, ChannelType.Ambient },
                DuckLevel = 0.25f,
                FadeInTime = 0.3f,
                FadeOutTime = 0.5f,
                Priority = DuckingPriority.High,
                Enabled = true
            });
        }

        /// <summary>
        /// Initializes ducking states for all channel types
        /// </summary>
        private void InitializeDuckingStates()
        {
            foreach (ChannelType channelType in Enum.GetValues(typeof(ChannelType)))
            {
                _duckingStates[channelType] = new DuckingState
                {
                    ChannelType = channelType
                };
                _channelActivity[channelType] = false;
            }
        }

        /// <summary>
        /// Updates ducking states based on channel activity
        /// </summary>
        /// <param name="channels">Collection of audio channels to process</param>
        /// <param name="deltaTime">Time since last update in seconds</param>
        public void Update(IEnumerable<AudioChannel> channels, float deltaTime)
        {
            if (!DuckingEnabled)
            {
                // Reset all ducking when disabled
                foreach (var state in _duckingStates.Values)
                {
                    state.TargetDuckLevel = 1.0f;
                    state.IsActive = false;
                }
                return;
            }

            var channelList = channels.ToList();

            // Update channel activity
            UpdateChannelActivity(channelList);

            // Process ducking rules
            ProcessDuckingRules();

            // Update ducking states with smooth transitions
            UpdateDuckingStates(deltaTime);

            // Apply ducking to channels
            ApplyDuckingToChannels(channelList);
        }

        /// <summary>
        /// Updates which channels are currently active
        /// </summary>
        private void UpdateChannelActivity(List<AudioChannel> channels)
        {
            foreach (var channel in channels)
            {
                bool wasActive = _channelActivity[channel.Type];
                bool isActive = channel.EffectiveVolume > ActivityThreshold;

                _channelActivity[channel.Type] = isActive;

                // Fire event on activity state change
                if (wasActive != isActive)
                {
                    OnDuckingStateChanged?.Invoke(this, new DuckingStateChangedEventArgs
                    {
                        ChannelType = channel.Type,
                        IsActive = isActive,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
        }

        /// <summary>
        /// Processes all ducking rules based on channel activity
        /// </summary>
        private void ProcessDuckingRules()
        {
            // Reset target duck levels
            foreach (var state in _duckingStates.Values)
            {
                state.TargetDuckLevel = 1.0f;
            }

            // Apply ducking rules (sorted by priority)
            var activeRules = _duckingRules
                .Where(r => r.Enabled && _channelActivity.GetValueOrDefault(r.TriggerChannel, false))
                .OrderByDescending(r => r.Priority);

            foreach (var rule in activeRules)
            {
                foreach (var affectedChannel in rule.AffectedChannels)
                {
                    var state = _duckingStates[affectedChannel];

                    // Apply the strongest ducking (lowest level) with highest priority
                    if (rule.DuckLevel < state.TargetDuckLevel || rule.Priority > state.Priority)
                    {
                        state.TargetDuckLevel = rule.DuckLevel;
                        state.FadeSpeed = 1.0f / (rule.DuckLevel < state.CurrentDuckLevel ? rule.FadeInTime : rule.FadeOutTime);
                        state.IsActive = true;
                        state.Priority = rule.Priority;
                    }
                }
            }
        }

        /// <summary>
        /// Updates ducking states with smooth transitions
        /// </summary>
        private void UpdateDuckingStates(float deltaTime)
        {
            foreach (var state in _duckingStates.Values)
            {
                // Smooth transition to target duck level
                if (Math.Abs(state.CurrentDuckLevel - state.TargetDuckLevel) > 0.001f)
                {
                    float change = state.FadeSpeed * deltaTime;

                    if (state.CurrentDuckLevel < state.TargetDuckLevel)
                    {
                        state.CurrentDuckLevel = Math.Min(state.CurrentDuckLevel + change, state.TargetDuckLevel);
                    }
                    else
                    {
                        state.CurrentDuckLevel = Math.Max(state.CurrentDuckLevel - change, state.TargetDuckLevel);
                    }
                }
                else
                {
                    state.CurrentDuckLevel = state.TargetDuckLevel;
                }

                // Update active state
                state.IsActive = state.CurrentDuckLevel < 0.99f;
            }
        }

        /// <summary>
        /// Applies ducking to audio channels
        /// </summary>
        private void ApplyDuckingToChannels(List<AudioChannel> channels)
        {
            foreach (var channel in channels)
            {
                var state = _duckingStates[channel.Type];
                channel.SetDucking(state.IsActive, state.CurrentDuckLevel);
            }
        }

        /// <summary>
        /// Adds a ducking rule
        /// </summary>
        public void AddDuckingRule(DuckingRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            _duckingRules.Add(rule);
        }

        /// <summary>
        /// Removes a ducking rule
        /// </summary>
        public bool RemoveDuckingRule(DuckingRule rule)
        {
            return _duckingRules.Remove(rule);
        }

        /// <summary>
        /// Clears all ducking rules
        /// </summary>
        public void ClearDuckingRules()
        {
            _duckingRules.Clear();
        }

        /// <summary>
        /// Gets the current ducking level for a channel
        /// </summary>
        public float GetDuckingLevel(ChannelType channelType)
        {
            return _duckingStates.TryGetValue(channelType, out var state) ? state.CurrentDuckLevel : 1.0f;
        }

        /// <summary>
        /// Checks if a channel is currently being ducked
        /// </summary>
        public bool IsChannelDucked(ChannelType channelType)
        {
            return _duckingStates.TryGetValue(channelType, out var state) && state.IsActive;
        }

        /// <summary>
        /// Resets all ducking states
        /// </summary>
        public void Reset()
        {
            foreach (var state in _duckingStates.Values)
            {
                state.TargetDuckLevel = 1.0f;
                state.CurrentDuckLevel = 1.0f;
                state.IsActive = false;
            }

            foreach (var key in _channelActivity.Keys.ToList())
            {
                _channelActivity[key] = false;
            }
        }

        /// <summary>
        /// Gets the controller configuration
        /// </summary>
        public DuckingControllerConfig GetConfig()
        {
            return new DuckingControllerConfig
            {
                DuckingEnabled = DuckingEnabled,
                ActivityThreshold = ActivityThreshold,
                DuckingRules = _duckingRules.Select(r => new DuckingRuleConfig
                {
                    TriggerChannel = r.TriggerChannel,
                    AffectedChannels = r.AffectedChannels.ToList(),
                    DuckLevel = r.DuckLevel,
                    FadeInTime = r.FadeInTime,
                    FadeOutTime = r.FadeOutTime,
                    Priority = r.Priority,
                    Enabled = r.Enabled
                }).ToList()
            };
        }

        /// <summary>
        /// Loads controller configuration
        /// </summary>
        public void LoadConfig(DuckingControllerConfig config)
        {
            DuckingEnabled = config.DuckingEnabled;
            ActivityThreshold = config.ActivityThreshold;

            _duckingRules.Clear();
            foreach (var ruleConfig in config.DuckingRules)
            {
                AddDuckingRule(new DuckingRule
                {
                    TriggerChannel = ruleConfig.TriggerChannel,
                    AffectedChannels = ruleConfig.AffectedChannels.ToList(),
                    DuckLevel = ruleConfig.DuckLevel,
                    FadeInTime = ruleConfig.FadeInTime,
                    FadeOutTime = ruleConfig.FadeOutTime,
                    Priority = ruleConfig.Priority,
                    Enabled = ruleConfig.Enabled
                });
            }
        }
    }

    /// <summary>
    /// Event arguments for ducking state changes
    /// </summary>
    public class DuckingStateChangedEventArgs : EventArgs
    {
        public ChannelType ChannelType { get; set; }
        public bool IsActive { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Serializable ducking controller configuration
    /// </summary>
    public class DuckingControllerConfig
    {
        public bool DuckingEnabled { get; set; }
        public float ActivityThreshold { get; set; }
        public List<DuckingRuleConfig> DuckingRules { get; set; } = new();
    }

    /// <summary>
    /// Serializable ducking rule configuration
    /// </summary>
    public class DuckingRuleConfig
    {
        public ChannelType TriggerChannel { get; set; }
        public List<ChannelType> AffectedChannels { get; set; } = new();
        public float DuckLevel { get; set; }
        public float FadeInTime { get; set; }
        public float FadeOutTime { get; set; }
        public DuckingPriority Priority { get; set; }
        public bool Enabled { get; set; }
    }
}
