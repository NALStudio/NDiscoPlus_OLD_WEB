using HueApi.Models;
using MemoryPack;

namespace NDiscoPlus.Shared.MemoryPack.Formatters;

internal class HuePositionFormatterAttribute : MemoryPackCustomFormatterAttribute<HuePosition>
{
    public override IMemoryPackFormatter<HuePosition> GetFormatter()
        => HuePositionFormatter.Default;
}

internal class HuePositionFormatter : MemoryPackFormatter<HuePosition>
{
    internal static readonly HuePositionFormatter Default = new();

    public override void Deserialize(ref MemoryPackReader reader, scoped ref HuePosition? value)
    {
        if (!reader.TryReadObjectHeader(out byte memberCount))
        {
            value = null;
            return;
        }

        if (memberCount != 3)
            MemoryPackSerializationException.ThrowInvalidPropertyCount(3, memberCount);

        double x = reader.ReadValue<double>();
        double y = reader.ReadValue<double>();
        double z = reader.ReadValue<double>();

        value = new HuePosition(x, y, z);
    }

    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref HuePosition? value)
    {
        if (value is null)
        {
            writer.WriteNullObjectHeader();
            return;
        }

        writer.WriteObjectHeader(3);
        writer.WriteValue(value.X);
        writer.WriteValue(value.Y);
        writer.WriteValue(value.Z);
    }
}
