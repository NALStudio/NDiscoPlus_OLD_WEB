using MemoryPack;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Spotify.Serializable;

[MemoryPackable]
internal partial class MemoryPackableTrackAudioFeatures
{
    [MemoryPackIgnore]
    internal readonly TrackAudioFeatures BASE;

    [MemoryPackConstructor]
    private MemoryPackableTrackAudioFeatures()
    {
        BASE = new();
    }

    internal MemoryPackableTrackAudioFeatures(TrackAudioFeatures trackAudioFeatures)
    {
        BASE = trackAudioFeatures;
    }

    public float Acousticness { get => BASE.Acousticness; set => BASE.Acousticness = value; }
    public string AnalysisUrl { get => BASE.AnalysisUrl; set => BASE.AnalysisUrl = value; }
    public float Danceability { get => BASE.Danceability; set => BASE.Danceability = value; }
    public int DurationMs { get => BASE.DurationMs; set => BASE.DurationMs = value; }
    public float Energy { get => BASE.Energy; set => BASE.Energy = value; }
    public string Id { get => BASE.Id; set => BASE.Id = value; }
    public float Instrumentalness { get => BASE.Instrumentalness; set => BASE.Instrumentalness = value; }
    public int Key { get => BASE.Key; set => BASE.Key = value; }
    public float Liveness { get => BASE.Liveness; set => BASE.Liveness = value; }
    public float Loudness { get => BASE.Loudness; set => BASE.Loudness = value; }
    public int Mode { get => BASE.Mode; set => BASE.Mode = value; }
    public float Speechiness { get => BASE.Speechiness; set => BASE.Speechiness = value; }
    public float Tempo { get => BASE.Tempo; set => BASE.Tempo = value; }
    public int TimeSignature { get => BASE.TimeSignature; set => BASE.TimeSignature = value; }
    public string TrackHref { get => BASE.TrackHref; set => BASE.TrackHref = value; }
    public string Type { get => BASE.Type; set => BASE.Type = value; }
    public string Uri { get => BASE.Uri; set => BASE.Uri = value; }
    public float Valence { get => BASE.Valence; set => BASE.Valence = value; }
}

public class TrackAudioFeaturesFormatter : MemoryPackFormatter<TrackAudioFeatures>
{

    public static readonly TrackAudioFeaturesFormatter Instance = new();

    public override void Deserialize(ref MemoryPackReader reader, scoped ref TrackAudioFeatures? value)
    {
        if (reader.PeekIsNull())
        {
            reader.Advance(1);
            value = null;
            return;
        }

        value = reader.ReadPackable<MemoryPackableTrackAudioFeatures>()?.BASE;
    }

    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TrackAudioFeatures? value)
    {
        if (value is null)
        {
            writer.WriteNullObjectHeader();
            return;
        }

        writer.WritePackable(new MemoryPackableTrackAudioFeatures(value));
    }
}

public class TrackAudioFeaturesFormatterAttribute : MemoryPackCustomFormatterAttribute<TrackAudioFeatures>
{
    public override IMemoryPackFormatter<TrackAudioFeatures> GetFormatter()
        => TrackAudioFeaturesFormatter.Instance;
}