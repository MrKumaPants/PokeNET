using MessagePack;
using MessagePack.Formatters;
using Microsoft.Xna.Framework;

namespace PokeNET.Domain.ECS.Persistence.Formatters;

/// <summary>
/// Custom MessagePack formatter for MonoGame Rectangle.
/// Serializes Rectangle as a 4-element array [X, Y, Width, Height].
/// </summary>
public sealed class RectangleFormatter : IMessagePackFormatter<Rectangle>
{
    public void Serialize(
        ref MessagePackWriter writer,
        Rectangle value,
        MessagePackSerializerOptions options
    )
    {
        writer.WriteArrayHeader(4);
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Width);
        writer.Write(value.Height);
    }

    public Rectangle Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var count = reader.ReadArrayHeader();
        if (count != 4)
            throw new MessagePackSerializationException(
                $"Invalid Rectangle array length: {count}, expected 4"
            );

        var x = reader.ReadInt32();
        var y = reader.ReadInt32();
        var width = reader.ReadInt32();
        var height = reader.ReadInt32();

        return new Rectangle(x, y, width, height);
    }
}
