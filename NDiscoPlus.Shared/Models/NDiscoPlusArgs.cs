using MemoryPack;
using NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
using NDiscoPlus.Spotify.Models;
using NDiscoPlus.Spotify.Serializable;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Models;

[MemoryPackable]
public partial class NDiscoPlusArgs
{
    public NDiscoPlusArgs(SpotifyPlayerTrack track, TrackAudioFeatures features, TrackAudioAnalysis analysis, EffectConfig effects, ImmutableArray<LightRecord> lights)
    {
        Track = track;

        Features = features;
        Analysis = analysis;

        Effects = effects;
        Lights = lights;
    }

    public SpotifyPlayerTrack Track { get; }

    [TrackAudioFeaturesFormatter]
    public TrackAudioFeatures Features { get; }
    [TrackAudioAnalysisFormatter]
    public TrackAudioAnalysis Analysis { get; }

    public EffectConfig Effects { get; }
    public ImmutableArray<LightRecord> Lights { get; }

    public bool AllowHDR { get; init; } = false;
    /// <summary>
    /// <para>If <see langword="null"/>, use a random default color palette.</para>
    /// </summary>
    public NDPColorPalette? ReferencePalette { get; init; } = null;
}