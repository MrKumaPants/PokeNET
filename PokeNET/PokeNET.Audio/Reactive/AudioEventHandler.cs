using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace PokeNET.Audio.Reactive
{
    /// <summary>
    /// Handles audio events with a priority-based system, managing sound effects
    /// that require ducking, interruption, or special handling.
    /// </summary>
    public class AudioEventHandler : IDisposable
    {
        private readonly ILogger<AudioEventHandler> _logger;
        private readonly PriorityQueue<AudioEvent> _eventQueue;
        private readonly List<ActiveAudioEvent> _activeEvents;
        private readonly Dictionary<AudioEventType, AudioEventConfig> _eventConfigs;

        private int _maxConcurrentEvents;
        private bool _isInitialized;
        private bool _isDisposed;

        /// <summary>
        /// Gets the count of currently active audio events.
        /// </summary>
        public int GetActiveEventCount() => _activeEvents.Count;

        /// <summary>
        /// Initializes a new instance of the AudioEventHandler class.
        /// </summary>
        /// <param name="logger">Logger for diagnostics.</param>
        /// <param name="maxConcurrentEvents">Maximum number of concurrent audio events.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        public AudioEventHandler(ILogger<AudioEventHandler> logger, int maxConcurrentEvents = 10)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventQueue = new PriorityQueue<AudioEvent>();
            _activeEvents = new List<ActiveAudioEvent>();
            _eventConfigs = new Dictionary<AudioEventType, AudioEventConfig>();

            _maxConcurrentEvents = maxConcurrentEvents;
        }

        /// <summary>
        /// Initialize the audio event handler with default configurations.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                _logger.LogWarning("AudioEventHandler is already initialized");
                return;
            }

            _logger.LogInformation("Initializing AudioEventHandler...");

            SetupDefaultEventConfigs();
            _isInitialized = true;

            _logger.LogInformation("AudioEventHandler initialized successfully");
        }

        /// <summary>
        /// Setup default configurations for various audio event types.
        /// </summary>
        private void SetupDefaultEventConfigs()
        {
            // Critical events
            _eventConfigs[AudioEventType.BossBattle] = new AudioEventConfig
            {
                EventType = AudioEventType.BossBattle,
                DefaultPriority = AudioPriority.Critical,
                CanInterrupt = true,
                RequiresDucking = false,
                DefaultDuckingAmount = 0.0f,
                MaxDuration = 0.0f, // No duration limit
                SoundEffect = "battle_start_epic"
            };

            _eventConfigs[AudioEventType.HealthCritical] = new AudioEventConfig
            {
                EventType = AudioEventType.HealthCritical,
                DefaultPriority = AudioPriority.Critical,
                CanInterrupt = true,
                RequiresDucking = true,
                DefaultDuckingAmount = 0.4f,
                MaxDuration = 2.0f,
                SoundEffect = "health_critical_warning"
            };

            _eventConfigs[AudioEventType.WildEncounter] = new AudioEventConfig
            {
                EventType = AudioEventType.WildEncounter,
                DefaultPriority = AudioPriority.Critical,
                CanInterrupt = true,
                RequiresDucking = true,
                DefaultDuckingAmount = 0.6f,
                MaxDuration = 1.5f,
                SoundEffect = "wild_encounter"
            };

            // High priority events
            _eventConfigs[AudioEventType.BattleStart] = new AudioEventConfig
            {
                EventType = AudioEventType.BattleStart,
                DefaultPriority = AudioPriority.High,
                CanInterrupt = true,
                RequiresDucking = false,
                DefaultDuckingAmount = 0.0f,
                MaxDuration = 2.0f,
                SoundEffect = "battle_start"
            };

            _eventConfigs[AudioEventType.LocationChange] = new AudioEventConfig
            {
                EventType = AudioEventType.LocationChange,
                DefaultPriority = AudioPriority.High,
                CanInterrupt = false,
                RequiresDucking = false,
                DefaultDuckingAmount = 0.0f,
                MaxDuration = 0.0f,
                SoundEffect = null // No sound effect, just music change
            };

            // Medium priority events
            _eventConfigs[AudioEventType.BattleEnd] = new AudioEventConfig
            {
                EventType = AudioEventType.BattleEnd,
                DefaultPriority = AudioPriority.Medium,
                CanInterrupt = false,
                RequiresDucking = false,
                DefaultDuckingAmount = 0.0f,
                MaxDuration = 3.0f,
                SoundEffect = "battle_end"
            };

            _eventConfigs[AudioEventType.ItemPickup] = new AudioEventConfig
            {
                EventType = AudioEventType.ItemPickup,
                DefaultPriority = AudioPriority.Medium,
                CanInterrupt = false,
                RequiresDucking = true,
                DefaultDuckingAmount = 0.7f,
                MaxDuration = 1.0f,
                SoundEffect = "item_get"
            };

            // Low priority events
            _eventConfigs[AudioEventType.MenuSound] = new AudioEventConfig
            {
                EventType = AudioEventType.MenuSound,
                DefaultPriority = AudioPriority.Low,
                CanInterrupt = false,
                RequiresDucking = false,
                DefaultDuckingAmount = 0.0f,
                MaxDuration = 0.3f,
                SoundEffect = "menu_select"
            };
        }

        /// <summary>
        /// Handle an audio event by adding it to the queue or playing it immediately.
        /// </summary>
        public void HandleEvent(AudioEvent audioEvent)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("AudioEventHandler is not initialized.");
            }

            if (audioEvent == null)
            {
                return;
            }

            // Apply configuration defaults if not specified
            ApplyEventConfig(audioEvent);

            // Check if we can play this event immediately
            if (CanPlayEventNow(audioEvent))
            {
                PlayEvent(audioEvent);
            }
            else
            {
                // Queue the event for later
                _eventQueue.Enqueue(audioEvent, (int)audioEvent.Priority);
            }
        }

        /// <summary>
        /// Apply configuration defaults to an audio event.
        /// </summary>
        private void ApplyEventConfig(AudioEvent audioEvent)
        {
            if (_eventConfigs.TryGetValue(audioEvent.EventType, out var config))
            {
                // Apply defaults if not already set
                if (audioEvent.Priority == AudioPriority.Medium) // Default priority
                {
                    audioEvent.Priority = config.DefaultPriority;
                }

                if (!audioEvent.RequiresDucking)
                {
                    audioEvent.RequiresDucking = config.RequiresDucking;
                }

                if (audioEvent.DuckingAmount == 0.0f)
                {
                    audioEvent.DuckingAmount = config.DefaultDuckingAmount;
                }

                if (string.IsNullOrEmpty(audioEvent.SoundEffect))
                {
                    audioEvent.SoundEffect = config.SoundEffect;
                }

                audioEvent.CanInterrupt = config.CanInterrupt;
                audioEvent.MaxDuration = config.MaxDuration;
            }
        }

        /// <summary>
        /// Check if an event can be played immediately.
        /// </summary>
        private bool CanPlayEventNow(AudioEvent audioEvent)
        {
            // If at max capacity, check if this event can interrupt
            if (_activeEvents.Count >= _maxConcurrentEvents)
            {
                if (!audioEvent.CanInterrupt)
                {
                    return false;
                }

                // Find lowest priority active event
                var lowestPriorityEvent = _activeEvents
                    .OrderBy(e => e.Event.Priority)
                    .FirstOrDefault();

                // Can only interrupt if this event has higher priority
                return lowestPriorityEvent != null && audioEvent.Priority > lowestPriorityEvent.Event.Priority;
            }

            return true;
        }

        /// <summary>
        /// Play an audio event.
        /// </summary>
        private void PlayEvent(AudioEvent audioEvent)
        {
            // If at max capacity and can interrupt, remove lowest priority event
            if (_activeEvents.Count >= _maxConcurrentEvents && audioEvent.CanInterrupt)
            {
                var lowestPriorityEvent = _activeEvents
                    .OrderBy(e => e.Event.Priority)
                    .FirstOrDefault();

                if (lowestPriorityEvent != null)
                {
                    StopEvent(lowestPriorityEvent);
                }
            }

            // Create active event
            var activeEvent = new ActiveAudioEvent
            {
                Event = audioEvent,
                StartTime = 0.0f,
                ElapsedTime = 0.0f,
                IsPlaying = true
            };

            _activeEvents.Add(activeEvent);

            // Trigger sound effect if specified
            if (!string.IsNullOrEmpty(audioEvent.SoundEffect))
            {
                PlaySoundEffect(audioEvent.SoundEffect);
            }

            // Trigger callback if specified
            audioEvent.OnEventStarted?.Invoke();
        }

        /// <summary>
        /// Stop an active audio event.
        /// </summary>
        private void StopEvent(ActiveAudioEvent activeEvent)
        {
            if (activeEvent == null || !activeEvent.IsPlaying)
            {
                return;
            }

            activeEvent.IsPlaying = false;
            activeEvent.Event.OnEventCompleted?.Invoke();

            _activeEvents.Remove(activeEvent);
        }

        /// <summary>
        /// Play a sound effect (placeholder - would integrate with audio system).
        /// </summary>
        private void PlaySoundEffect(string soundEffect)
        {
            // This would integrate with the actual audio playback system
            // For now, it's a placeholder
            _logger.LogDebug("Playing sound effect: {SoundEffect}", soundEffect);
        }

        /// <summary>
        /// Update the audio event handler (call per frame).
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_isInitialized)
            {
                return;
            }

            // Update active events
            UpdateActiveEvents(deltaTime);

            // Try to play queued events
            ProcessEventQueue();
        }

        /// <summary>
        /// Update all active events and remove completed ones.
        /// </summary>
        private void UpdateActiveEvents(float deltaTime)
        {
            var completedEvents = new List<ActiveAudioEvent>();

            foreach (var activeEvent in _activeEvents)
            {
                activeEvent.ElapsedTime += deltaTime;

                // Check if event has exceeded max duration
                if (activeEvent.Event.MaxDuration > 0 &&
                    activeEvent.ElapsedTime >= activeEvent.Event.MaxDuration)
                {
                    completedEvents.Add(activeEvent);
                }
            }

            // Remove completed events
            foreach (var completedEvent in completedEvents)
            {
                StopEvent(completedEvent);
            }
        }

        /// <summary>
        /// Process queued events and play them if possible.
        /// </summary>
        private void ProcessEventQueue()
        {
            while (_eventQueue.Count > 0 && _activeEvents.Count < _maxConcurrentEvents)
            {
                var nextEvent = _eventQueue.Dequeue();

                if (CanPlayEventNow(nextEvent))
                {
                    PlayEvent(nextEvent);
                }
            }
        }

        /// <summary>
        /// Register a custom event configuration.
        /// </summary>
        public void RegisterEventConfig(AudioEventConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _eventConfigs[config.EventType] = config;
        }

        /// <summary>
        /// Clear all active events and queued events.
        /// </summary>
        public void ClearAllEvents()
        {
            _activeEvents.Clear();
            _eventQueue.Clear();
        }

        /// <summary>
        /// Get all currently active events.
        /// </summary>
        public List<AudioEvent> GetActiveEvents()
        {
            return _activeEvents.Select(ae => ae.Event).ToList();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            ClearAllEvents();
            _eventConfigs.Clear();

            _isDisposed = true;
        }
    }

    #region Supporting Classes and Enums

    public enum AudioEventType
    {
        BattleStart,
        BattleEnd,
        BossBattle,
        LocationChange,
        WildEncounter,
        HealthCritical,
        ItemPickup,
        MenuSound,
        Dialogue,
        Achievement
    }

    public enum AudioPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Represents an audio event with priority and behavior.
    /// </summary>
    public class AudioEvent
    {
        public required AudioEventType EventType { get; set; }
        public AudioPriority Priority { get; set; }
        public object? Data { get; set; }
        public bool RequiresDucking { get; set; }
        public float DuckingAmount { get; set; }
        public string? SoundEffect { get; set; }
        public bool CanInterrupt { get; set; }
        public float MaxDuration { get; set; }

        public Action? OnEventStarted { get; set; }
        public Action? OnEventCompleted { get; set; }
    }

    /// <summary>
    /// Configuration for an audio event type.
    /// </summary>
    public class AudioEventConfig
    {
        public required AudioEventType EventType { get; set; }
        public required AudioPriority DefaultPriority { get; set; }
        public bool CanInterrupt { get; set; }
        public bool RequiresDucking { get; set; }
        public float DefaultDuckingAmount { get; set; }
        public float MaxDuration { get; set; }
        public string? SoundEffect { get; set; }
    }

    /// <summary>
    /// Represents an actively playing audio event.
    /// </summary>
    public class ActiveAudioEvent
    {
        public required AudioEvent Event { get; set; }
        public float StartTime { get; set; }
        public float ElapsedTime { get; set; }
        public bool IsPlaying { get; set; }
    }

    /// <summary>
    /// Simple priority queue implementation for audio events.
    /// </summary>
    public class PriorityQueue<T>
    {
        private readonly SortedDictionary<int, Queue<T>> _queues;

        public int Count { get; private set; }

        public PriorityQueue()
        {
            _queues = new SortedDictionary<int, Queue<T>>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
            Count = 0;
        }

        public void Enqueue(T item, int priority)
        {
            if (!_queues.ContainsKey(priority))
            {
                _queues[priority] = new Queue<T>();
            }

            _queues[priority].Enqueue(item);
            Count++;
        }

        public T Dequeue()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Queue is empty");
            }

            foreach (var kvp in _queues)
            {
                if (kvp.Value.Count > 0)
                {
                    Count--;
                    var item = kvp.Value.Dequeue();

                    if (kvp.Value.Count == 0)
                    {
                        _queues.Remove(kvp.Key);
                    }

                    return item;
                }
            }

            throw new InvalidOperationException("Queue is in invalid state");
        }

        public void Clear()
        {
            _queues.Clear();
            Count = 0;
        }
    }

    #endregion
}
