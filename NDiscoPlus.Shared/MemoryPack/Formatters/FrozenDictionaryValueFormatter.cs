using MemoryPack;
using MemoryPack.Formatters;
using NDiscoPlus.Shared.Models;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.MemoryPack.Formatters;

// C# doesn't allow you to pass Func through an attribute so we make custom attributes for each type
internal class NDPLightFrozenDictionaryValueFormatterAttribute : MemoryPackCustomFormatterAttribute<FrozenDictionary<LightId, NDPLight>>
{
    private static readonly FrozenDictionaryValueFormatter<LightId, NDPLight> instance = new(key => key.Id);

    public override IMemoryPackFormatter<FrozenDictionary<LightId, NDPLight>> GetFormatter() => instance;
}


internal class FrozenDictionaryValueFormatter<TKey, TValue> : MemoryPackFormatter<FrozenDictionary<TKey, TValue?>> where TKey : notnull
{
    private readonly Func<TValue?, TKey> keySelector;

    public FrozenDictionaryValueFormatter(Func<TValue?, TKey> keySelector)
    {
        this.keySelector = keySelector;
    }

    public override void Deserialize(ref MemoryPackReader reader, scoped ref FrozenDictionary<TKey, TValue?>? value)
    {
        TValue?[]? array = reader.ReadArray<TValue?>();
        if (array is null)
            value = default;
        else if (array.Length == 0)
            value = FrozenDictionary<TKey, TValue?>.Empty;
        else
            value = array.ToFrozenDictionary(value => keySelector(value));
    }

    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref FrozenDictionary<TKey, TValue?>? value)
    {
        if (value is null)
        {
            writer.WriteNullCollectionHeader();
            return;
        }

        ImmutableArray<TValue?> values = value.Values;
        if (values.IsDefault)
        {
            writer.WriteNullCollectionHeader();
        }
        else
        {
            writer.WriteSpan(values.AsSpan());
        }
    }
}
