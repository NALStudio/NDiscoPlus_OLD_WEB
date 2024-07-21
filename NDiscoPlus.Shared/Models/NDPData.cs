using NDiscoPlus.Shared.Effects.API;
using NDiscoPlus.Shared.Effects.API.Channels.Background;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
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
    internal NDPData(SpotifyPlayerTrack track, NDPColorPalette referencePalette, NDPColorPalette effectPalette, ExportedEffectsCollection effects)
    {
        Track = track;
        ReferencePalette = referencePalette;
        EffectPalette = effectPalette;
        Effects = effects;
    }

    public SpotifyPlayerTrack Track { get; }

    [JsonConverter(typeof(JsonNDPColorPaletteConverter))]
    public NDPColorPalette ReferencePalette { get; }
    [JsonConverter(typeof(JsonNDPColorPaletteConverter))]
    public NDPColorPalette EffectPalette { get; }

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
/// </summary>
#pragma warning disable IDE0051, RCS1213 // Remove unused private members, Remove unused member declaration
public class ExportedEffectsCollection
{
    [JsonIgnore]
    public IList<Effect> Effects { get; }

    [JsonIgnore]
    public IDictionary<LightId, IList<BackgroundTransition>> BackgroundTransitions { get; }

    [JsonInclude]
    private IEnumerable<Effect> JsonEffects => Effects;

    [JsonInclude]
    private IEnumerable<BackgroundTransition> JsonBackgroundTransitions => BackgroundTransitions.Values.SelectMany(x => x);

    internal ExportedEffectsCollection(EffectAPI api)
    {
        BackgroundTransitions = api.Background.ToFrozenDictionary(key => key.Key, value => (IList<BackgroundTransition>)value.Value.ToImmutableArray());
        // channels are reversed so that channels that are on top (i.e. strobes on top of flashes) are at the end of the list (thus overwriting the previous effects)
        Effects = api.Channels.Reverse().SelectMany(chnl => chnl.Effects).ToImmutableArray();
    }

    [JsonConstructor]
    private ExportedEffectsCollection(IEnumerable<Effect> jsonEffects, IEnumerable<BackgroundTransition> jsonBackgroundTransitions)
    {
        Effects = jsonEffects.ToImmutableArray();

        IGrouping<LightId, BackgroundTransition>[] groups = jsonBackgroundTransitions.GroupBy(x => x.LightId).ToArray();
        BackgroundTransitions = groups.ToFrozenDictionary(key => key.Key, value => (IList<BackgroundTransition>)value.ToImmutableArray());
    }
}
#pragma warning restore IDE0051, RCS1213