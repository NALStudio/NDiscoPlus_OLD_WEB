using MemoryPack;
using Microsoft.AspNetCore.WebUtilities;
using NDiscoPlus.Shared.Effects.API.Channels.Background;
using NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.MemoryPack.Formatters;
using NDiscoPlus.Spotify.Models;
using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace NDiscoPlus.Shared.Models;

[MemoryPackable]
public partial class NDPData
{
    [MemoryPackConstructor]
    internal NDPData(
        SpotifyPlayerTrack track,
        NDPColorPalette referencePalette, NDPColorPalette effectPalette,
        EffectConfig effectConfig, ExportedEffectsCollection effects,
        FrozenDictionary<LightId, NDPLight> lights
    )
    {
        Track = track;
        ReferencePalette = referencePalette;
        EffectPalette = effectPalette;

        EffectConfig = effectConfig;
        Effects = effects;

        Lights = lights;
    }

    public SpotifyPlayerTrack Track { get; }

    [NDPColorPaletteFormatter]
    public NDPColorPalette ReferencePalette { get; }
    [NDPColorPaletteFormatter]
    public NDPColorPalette EffectPalette { get; }

    public EffectConfig EffectConfig { get; }
    public ExportedEffectsCollection Effects { get; }

    [NDPLightFrozenDictionaryValueFormatter]
    public FrozenDictionary<LightId, NDPLight> Lights { get; }

    public static string Serialize(NDPData data)
    {
        byte[] bytes = MemoryPackSerializer.Serialize(data);
        return ByteHelper.UnsafeCastToString(bytes);
    }

    public static NDPData Deserialize(string data)
    {
        byte[] bytes = ByteHelper.UnsafeCastFromString(data);
        NDPData? d = MemoryPackSerializer.Deserialize<NDPData>(bytes);
        return d ?? throw new InvalidOperationException("Cannot deserialize value.");
    }
}

/// <summary>
/// Effects that come after should be rendered on top of any previous effects.
/// Effects should be rendered as two nested for-loops.
/// </summary>
#pragma warning disable IDE0051, RCS1213 // Remove unused private members, Remove unused member declaration
[MemoryPackable]
public partial class ExportedEffectsCollection
{
    public ImmutableList<ImmutableList<Effect>> Effects { get; }
    public FrozenDictionary<LightId, IList<BackgroundTransition>> BackgroundTransitions { get; }

    internal ExportedEffectsCollection(ImmutableList<ImmutableList<Effect>> effects, FrozenDictionary<LightId, IList<BackgroundTransition>> backgroundTransitions)
    {
        Effects = effects;
        BackgroundTransitions = backgroundTransitions;
    }
}
#pragma warning restore IDE0051, RCS1213