using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;

namespace PokeNET.Audio.Services.Managers;

/// <summary>
/// Manages volume control across all audio channels.
/// Implements single responsibility for volume management and ducking.
/// </summary>
public sealed class AudioVolumeManager : IAudioVolumeManager
{
    private readonly ILogger<AudioVolumeManager> _logger;
    private readonly IMusicPlayer _musicPlayer;
    private readonly ISoundEffectPlayer _sfxPlayer;

    private float _masterVolume = 1.0f;
    private float _musicVolume = 1.0f;
    private float _sfxVolume = 1.0f;
    private float _ambientVolume = 1.0f;
    private float _originalMusicVolume = 1.0f;

    /// <summary>
    /// Gets the current master volume (0.0 to 1.0).
    /// </summary>
    public float MasterVolume => _masterVolume;

    /// <summary>
    /// Gets the current music volume (0.0 to 1.0).
    /// </summary>
    public float MusicVolume => _musicVolume;

    /// <summary>
    /// Gets the current sound effects volume (0.0 to 1.0).
    /// </summary>
    public float SfxVolume => _sfxVolume;

    /// <summary>
    /// Gets the current ambient volume (0.0 to 1.0).
    /// </summary>
    public float AmbientVolume => _ambientVolume;

    /// <summary>
    /// Gets the original music volume before ducking.
    /// </summary>
    public float OriginalMusicVolume => _originalMusicVolume;

    /// <summary>
    /// Initializes a new instance of the AudioVolumeManager class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="musicPlayer">Music player for volume control.</param>
    /// <param name="sfxPlayer">Sound effect player for volume control.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public AudioVolumeManager(
        ILogger<AudioVolumeManager> logger,
        IMusicPlayer musicPlayer,
        ISoundEffectPlayer sfxPlayer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _musicPlayer = musicPlayer ?? throw new ArgumentNullException(nameof(musicPlayer));
        _sfxPlayer = sfxPlayer ?? throw new ArgumentNullException(nameof(sfxPlayer));
    }

    /// <summary>
    /// Sets the master volume for all audio channels.
    /// Automatically updates both music and SFX player volumes.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    public void SetMasterVolume(float volume)
    {
        _masterVolume = Math.Clamp(volume, 0.0f, 1.0f);
        ApplyVolumesToPlayers();
        _logger.LogInformation("Set master volume to {Volume}", _masterVolume);
    }

    /// <summary>
    /// Sets the music volume.
    /// Automatically applies master volume multiplication.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    public void SetMusicVolume(float volume)
    {
        _musicVolume = Math.Clamp(volume, 0.0f, 1.0f);
        _musicPlayer.SetVolume(_musicVolume * _masterVolume);
        _logger.LogDebug("Set music volume to {Volume}", _musicVolume);
    }

    /// <summary>
    /// Sets the sound effects volume.
    /// Automatically applies master volume multiplication.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    public void SetSfxVolume(float volume)
    {
        _sfxVolume = Math.Clamp(volume, 0.0f, 1.0f);
        _sfxPlayer.SetMasterVolume(_sfxVolume * _masterVolume);
        _logger.LogDebug("Set SFX volume to {Volume}", _sfxVolume);
    }

    /// <summary>
    /// Sets the ambient audio volume.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    public void SetAmbientVolume(float volume)
    {
        _ambientVolume = Math.Clamp(volume, 0.0f, 1.0f);
        _logger.LogDebug("Set ambient volume to {Volume}", _ambientVolume);
    }

    /// <summary>
    /// Applies volume settings to music and SFX players.
    /// </summary>
    public void ApplyVolumesToPlayers()
    {
        _musicPlayer.SetVolume(_musicVolume * _masterVolume);
        _sfxPlayer.SetMasterVolume(_sfxVolume * _masterVolume);
    }

    /// <summary>
    /// Ducks (lowers) music volume temporarily.
    /// </summary>
    /// <param name="duckAmount">Amount to duck (0.0 to 1.0, where 1.0 is full duck).</param>
    public void DuckMusic(float duckAmount)
    {
        _originalMusicVolume = _musicVolume;
        var duckedVolume = _musicVolume * (1.0f - Math.Clamp(duckAmount, 0.0f, 1.0f));
        _musicPlayer.SetVolume(duckedVolume * _masterVolume);
        _logger.LogDebug("Ducked music volume by {DuckAmount}", duckAmount);
    }

    /// <summary>
    /// Restores music volume after ducking.
    /// </summary>
    public void StopDucking()
    {
        _musicPlayer.SetVolume(_originalMusicVolume * _masterVolume);
        _logger.LogDebug("Restored music volume after ducking");
    }
}
