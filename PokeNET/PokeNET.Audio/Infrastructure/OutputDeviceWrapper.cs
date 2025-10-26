using Melanchall.DryWetMidi.Multimedia;
using PokeNET.Audio.Abstractions;

namespace PokeNET.Audio.Infrastructure;

/// <summary>
/// Wrapper implementation for Melanchall.DryWetMidi.Multimedia.OutputDevice.
/// Implements IMidiOutputDevice to enable dependency injection and testing.
/// Simply delegates all calls to the wrapped DryWetMidi OutputDevice.
/// </summary>
public class OutputDeviceWrapper : IMidiOutputDevice
{
    private readonly OutputDevice _device;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputDeviceWrapper"/> class.
    /// </summary>
    /// <param name="device">The underlying OutputDevice to wrap.</param>
    public OutputDeviceWrapper(OutputDevice device)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public event EventHandler<MidiEventSentEventArgs>? EventSent
    {
        add => _device.EventSent += value;
        remove => _device.EventSent -= value;
    }

    /// <summary>
    /// Creates a wrapper for an OutputDevice by device ID.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <returns>A new OutputDeviceWrapper instance.</returns>
    public static OutputDeviceWrapper GetByIndex(int deviceId)
    {
        var device = OutputDevice.GetByIndex(deviceId);
        return new OutputDeviceWrapper(device);
    }

    /// <inheritdoc />
    public void SendEvent(MidiEvent midiEvent)
    {
        ThrowIfDisposed();
        _device.SendEvent(midiEvent);
    }

    /// <inheritdoc />
    public void PrepareForEventsSending()
    {
        ThrowIfDisposed();
        _device.PrepareForEventsSending();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _device?.Dispose();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(OutputDeviceWrapper));
    }
}
