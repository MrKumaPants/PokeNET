using MessagePack;
using MessagePack.Formatters;
using Microsoft.Xna.Framework;

namespace PokeNET.Core.Persistence.Formatters;

/// <summary>
/// Custom MessagePack formatter for MonoGame Color.
/// Serializes Color as a 4-element array [R, G, B, A] (bytes).
/// </summary>
public sealed class ColorFormatter : IMessagePackFormatter<Color>
{
    public void Serialize(
        ref MessagePackWriter writer,
        Color value,
        MessagePackSerializerOptions options
    )
    {
        writer.WriteArrayHeader(4);
        writer.Write(value.R);
        writer.Write(value.G);
        writer.Write(value.B);
        writer.Write(value.A);
    }

    public Color Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var count = reader.ReadArrayHeader();
        if (count != 4)
            throw new MessagePackSerializationException(
                $"Invalid Color array length: {count}, expected 4"
            );

        var r = reader.ReadByte();
        var g = reader.ReadByte();
        var b = reader.ReadByte();
        var a = reader.ReadByte();

        return new Color(r, g, b, a);
    }
}
