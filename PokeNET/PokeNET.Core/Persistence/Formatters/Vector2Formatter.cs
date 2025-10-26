using MessagePack;
using MessagePack.Formatters;
using Microsoft.Xna.Framework;

namespace PokeNET.Core.Persistence.Formatters;

/// <summary>
/// Custom MessagePack formatter for MonoGame Vector2.
/// Serializes Vector2 as a 2-element array [X, Y].
/// </summary>
public sealed class Vector2Formatter : IMessagePackFormatter<Vector2>
{
    public void Serialize(
        ref MessagePackWriter writer,
        Vector2 value,
        MessagePackSerializerOptions options
    )
    {
        writer.WriteArrayHeader(2);
        writer.Write(value.X);
        writer.Write(value.Y);
    }

    public Vector2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var count = reader.ReadArrayHeader();
        if (count != 2)
            throw new MessagePackSerializationException(
                $"Invalid Vector2 array length: {count}, expected 2"
            );

        var x = reader.ReadSingle();
        var y = reader.ReadSingle();

        return new Vector2(x, y);
    }
}
