using NDiscoPlus.Shared.Effects.API;
using NDiscoPlus.Shared.Effects.API.Channels.Background;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
using NDiscoPlus.Shared.Music;
using SkiaSharp;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Models;

public class NDPData
{
    [JsonConstructor]
    internal NDPData(
        SpotifyPlayerTrack track,
        NDPColorPalette referencePalette, NDPColorPalette effectPalette,
        EffectConfig effectConfig, ExportedEffectsCollection effects
    )
    {
        Track = track;
        ReferencePalette = referencePalette;
        EffectPalette = effectPalette;

        EffectConfig = effectConfig;
        Effects = effects;
    }

    public SpotifyPlayerTrack Track { get; }

    [JsonConverter(typeof(JsonNDPColorPaletteConverter))]
    public NDPColorPalette ReferencePalette { get; }
    [JsonConverter(typeof(JsonNDPColorPaletteConverter))]
    public NDPColorPalette EffectPalette { get; }

    public EffectConfig EffectConfig { get; }
    public ExportedEffectsCollection Effects { get; }

    public static string Serialize(NDPData data)
    {
        string output = JsonSerializer.Serialize(data);
        Debug.Assert(!string.IsNullOrEmpty(output));
        return output;
    }

    public static NDPData Deserialize(string data)
    {
        NDPData? d = JsonSerializer.Deserialize<NDPData>(data);
        return d ?? throw new InvalidOperationException("Cannot deserialize value.");
    }
}

/// <summary>
/// Effects that come after should be rendered on top of any previous effects.
/// Effects should be rendered as two nested for-loops.
/// </summary>
#pragma warning disable IDE0051, RCS1213 // Remove unused private members, Remove unused member declaration
public class ExportedEffectsCollection
{
    [JsonIgnore]
    public IList<IList<Effect>> Effects { get; }

    [JsonIgnore]
    public IDictionary<LightId, IList<BackgroundTransition>> BackgroundTransitions { get; }

    [JsonInclude]
    private IEnumerable<IEnumerable<Effect>> JsonEffects => Effects;

    [JsonInclude]
    private IEnumerable<BackgroundTransition> JsonBackgroundTransitions => BackgroundTransitions.Values.SelectMany(x => x);

    internal ExportedEffectsCollection(IEnumerable<IEnumerable<Effect>> effects, IEnumerable<KeyValuePair<LightId, IList<BackgroundTransition>>> backgroundTransitions)
    {
        Effects = effects.Select(e => (IList<Effect>)e.ToImmutableArray()).ToImmutableArray();
        BackgroundTransitions = backgroundTransitions.Select(x => new KeyValuePair<LightId, IList<BackgroundTransition>>(x.Key, x.Value.ToImmutableArray())).ToFrozenDictionary();
    }

    [JsonConstructor]
    private ExportedEffectsCollection(IEnumerable<IEnumerable<Effect>> jsonEffects, IEnumerable<BackgroundTransition> jsonBackgroundTransitions)
    {
        Effects = jsonEffects.Select(effects => (IList<Effect>)effects.ToImmutableArray()).ToImmutableArray();

        IGrouping<LightId, BackgroundTransition>[] groups = jsonBackgroundTransitions.GroupBy(x => x.LightId).ToArray();
        BackgroundTransitions = groups.ToFrozenDictionary(key => key.Key, value => (IList<BackgroundTransition>)value.ToImmutableArray());
    }
}
#pragma warning restore IDE0051, RCS1213