namespace PokeNET.Audio.Exceptions;

/// <summary>
/// Exception thrown when audio playback fails
/// </summary>
public class AudioPlaybackException : AudioException
{
    public AudioPlaybackException(string message) : base(message)
    {
    }

    public AudioPlaybackException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
