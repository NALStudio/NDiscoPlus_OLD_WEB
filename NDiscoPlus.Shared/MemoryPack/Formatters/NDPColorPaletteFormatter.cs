using MemoryPack;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace NDiscoPlus.Shared.MemoryPack.Formatters;

internal class NDPColorPaletteFormatterAttribute : MemoryPackCustomFormatterAttribute<NDPColorPalette>
{
    public override IMemoryPackFormatter<NDPColorPalette> GetFormatter()
        => NDPColorPaletteFormatter.Instance;
}

internal class NDPColorPaletteFormatter : MemoryPackFormatter<NDPColorPalette>
{
    public static readonly NDPColorPaletteFormatter Instance = new();

    public override void Deserialize(ref MemoryPackReader reader, scoped ref NDPColorPalette value)
    {
        NDPColor[]? colors = reader.ReadArray<NDPColor>();
        if (colors is null)
            return;

        value = new NDPColorPalette(ImmutableCollectionsMarshal.AsImmutableArray(colors));
    }

    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref NDPColorPalette value)
    {
        writer.WriteArray(ImmutableCollectionsMarshal.AsArray((ImmutableArray<NDPColor>)value.Colors));
    }
}
