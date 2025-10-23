using System;

namespace PokeNET.Audio.Mixing
{
    /// <summary>
    /// Represents the type of audio channel
    /// </summary>
    public enum ChannelType
    {
        Master,
        Music,
        SoundEffects,
        Voice,
        Ambient
    }

    /// <summary>
    /// Represents an individual audio channel with volume control and muting capabilities
    /// </summary>
    public class AudioChannel
    {
        private float _volume;
        private float _targetVolume;
        private bool _isMuted;
        private float _fadeSpeed;

        /// <summary>
        /// Gets the channel type
        /// </summary>
        public ChannelType Type { get; }

        /// <summary>
        /// Gets or sets the channel name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the channel volume (0.0 to 1.0)
        /// </summary>
        public float Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Clamp(value, 0.0f, 1.0f);
                if (!_isMuted)
                {
                    _targetVolume = _volume;
                }
                OnVolumeChanged?.Invoke(this, _volume);
            }
        }

        /// <summary>
        /// Gets the effective volume after applying mute and ducking
        /// </summary>
        public float EffectiveVolume => _isMuted ? 0.0f : CurrentVolume;

        /// <summary>
        /// Gets the current volume (used for smooth transitions)
        /// </summary>
        public float CurrentVolume { get; private set; }

        /// <summary>
        /// Gets or sets whether the channel is muted
        /// </summary>
        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                if (_isMuted != value)
                {
                    _isMuted = value;
                    _targetVolume = _isMuted ? 0.0f : _volume;
                    OnMuteChanged?.Invoke(this, _isMuted);
                }
            }
        }

        /// <summary>
        /// Gets or sets the ducking level (0.0 to 1.0, where 0.0 is fully ducked)
        /// </summary>
        public float DuckingLevel { get; set; }

        /// <summary>
        /// Gets whether ducking is active on this channel
        /// </summary>
        public bool IsDucked { get; private set; }

        /// <summary>
        /// Event fired when volume changes
        /// </summary>
        public event EventHandler<float>? OnVolumeChanged;

        /// <summary>
        /// Event fired when mute state changes
        /// </summary>
        public event EventHandler<bool>? OnMuteChanged;

        /// <summary>
        /// Initializes a new instance of the AudioChannel class
        /// </summary>
        /// <param name="type">The channel type</param>
        /// <param name="name">The channel name</param>
        /// <param name="initialVolume">Initial volume (0.0 to 1.0)</param>
        public AudioChannel(ChannelType type, string name, float initialVolume = 1.0f)
        {
            Type = type;
            Name = name;
            _volume = Math.Clamp(initialVolume, 0.0f, 1.0f);
            _targetVolume = _volume;
            CurrentVolume = _volume;
            _isMuted = false;
            DuckingLevel = 1.0f;
            IsDucked = false;
            _fadeSpeed = 5.0f; // Default fade speed
        }

        /// <summary>
        /// Updates the channel volume with smooth transitions
        /// </summary>
        /// <param name="deltaTime">Time since last update in seconds</param>
        public void Update(float deltaTime)
        {
            // Smooth volume transitions
            if (Math.Abs(CurrentVolume - _targetVolume) > 0.001f)
            {
                CurrentVolume = Lerp(CurrentVolume, _targetVolume, _fadeSpeed * deltaTime);

                // Snap to target if very close
                if (Math.Abs(CurrentVolume - _targetVolume) < 0.001f)
                {
                    CurrentVolume = _targetVolume;
                }
            }

            // Apply ducking
            if (IsDucked && DuckingLevel < 1.0f)
            {
                CurrentVolume *= DuckingLevel;
            }
        }

        /// <summary>
        /// Sets the ducking state for this channel
        /// </summary>
        /// <param name="isDucked">Whether ducking should be active</param>
        /// <param name="duckLevel">The ducking level (0.0 to 1.0)</param>
        public void SetDucking(bool isDucked, float duckLevel = 0.3f)
        {
            IsDucked = isDucked;
            DuckingLevel = Math.Clamp(duckLevel, 0.0f, 1.0f);
        }

        /// <summary>
        /// Fades the channel to a target volume over time
        /// </summary>
        /// <param name="targetVolume">Target volume (0.0 to 1.0)</param>
        /// <param name="fadeSpeed">Fade speed multiplier</param>
        public void FadeTo(float targetVolume, float fadeSpeed = 5.0f)
        {
            _targetVolume = Math.Clamp(targetVolume, 0.0f, 1.0f);
            _fadeSpeed = fadeSpeed;
            _volume = _targetVolume; // Update the base volume
        }

        /// <summary>
        /// Instantly sets the volume without fading
        /// </summary>
        /// <param name="volume">Target volume (0.0 to 1.0)</param>
        public void SetVolumeInstant(float volume)
        {
            _volume = Math.Clamp(volume, 0.0f, 1.0f);
            _targetVolume = _volume;
            CurrentVolume = _volume;
            OnVolumeChanged?.Invoke(this, _volume);
        }

        /// <summary>
        /// Resets the channel to default state
        /// </summary>
        public void Reset()
        {
            _volume = 1.0f;
            _targetVolume = 1.0f;
            CurrentVolume = 1.0f;
            _isMuted = false;
            DuckingLevel = 1.0f;
            IsDucked = false;
            _fadeSpeed = 5.0f;
        }

        /// <summary>
        /// Linear interpolation helper
        /// </summary>
        private float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Math.Clamp(t, 0.0f, 1.0f);
        }

        /// <summary>
        /// Gets the channel configuration as a serializable object
        /// </summary>
        public ChannelConfig GetConfig()
        {
            return new ChannelConfig
            {
                Type = Type,
                Name = Name,
                Volume = _volume,
                IsMuted = _isMuted
            };
        }

        /// <summary>
        /// Loads channel configuration
        /// </summary>
        public void LoadConfig(ChannelConfig config)
        {
            if (config.Type != Type)
            {
                throw new ArgumentException($"Config type {config.Type} does not match channel type {Type}");
            }

            Name = config.Name;
            Volume = config.Volume;
            IsMuted = config.IsMuted;
        }
    }

    /// <summary>
    /// Serializable channel configuration
    /// </summary>
    public class ChannelConfig
    {
        public ChannelType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public float Volume { get; set; }
        public bool IsMuted { get; set; }
    }
}
