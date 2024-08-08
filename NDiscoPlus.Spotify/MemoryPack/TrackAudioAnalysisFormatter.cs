using MemoryPack;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Spotify.Serializable;

[MemoryPackable]
internal partial class MemoryPackableTimeInterval
{
    [MemoryPackIgnore]
    internal readonly TimeInterval BASE;

    [MemoryPackConstructor]
    private MemoryPackableTimeInterval()
    {
        BASE = new();
    }

    internal MemoryPackableTimeInterval(TimeInterval @base)
    {
        BASE = @base;
    }

    public float Start { get => BASE.Start; set => BASE.Start = value; }
    public float Duration { get => BASE.Duration; set => BASE.Duration = value; }
    public float Confidence { get => BASE.Confidence; set => BASE.Confidence = value; }
}

[MemoryPackable]
internal partial class MemoryPackableSection
{
    [MemoryPackIgnore]
    internal readonly Section BASE;

    [MemoryPackConstructor]
    private MemoryPackableSection()
    {
        BASE = new();
    }

    internal MemoryPackableSection(Section section)
    {
        BASE = section;
    }

    public float Start { get => BASE.Start; set => BASE.Start = value; }
    public float Duration { get => BASE.Duration; set => BASE.Duration = value; }
    public float Confidence { get => BASE.Confidence; set => BASE.Confidence = value; }
    public float Loudness { get => BASE.Loudness; set => BASE.Loudness = value; }
    public float Tempo { get => BASE.Tempo; set => BASE.Tempo = value; }
    public float TempoConfidence { get => BASE.TempoConfidence; set => BASE.TempoConfidence = value; }
    public int Key { get => BASE.Key; set => BASE.Key = value; }
    public float KeyConfidence { get => BASE.KeyConfidence; set => BASE.KeyConfidence = value; }
    public int Mode { get => BASE.Mode; set => BASE.Mode = value; }
    public float ModeConfidence { get => BASE.ModeConfidence; set => BASE.ModeConfidence = value; }
    public int TimeSignature { get => BASE.TimeSignature; set => BASE.TimeSignature = value; }
    public float TimeSignatureConfidence { get => BASE.TimeSignatureConfidence; set => BASE.TimeSignatureConfidence = value; }
}

[MemoryPackable]
internal partial class MemoryPackableSegment
{
    [MemoryPackIgnore]
    internal readonly Segment BASE;

    [MemoryPackConstructor]
    private MemoryPackableSegment()
    {
        BASE = new();
    }

    internal MemoryPackableSegment(Segment segment)
    {
        BASE = segment;
    }

    public float Start { get => BASE.Start; set => BASE.Start = value; }
    public float Duration { get => BASE.Duration; set => BASE.Duration = value; }
    public float Confidence { get => BASE.Confidence; set => BASE.Confidence = value; }
    public float LoudnessStart { get => BASE.LoudnessStart; set => BASE.LoudnessStart = value; }
    public float LoudnessMax { get => BASE.LoudnessMax; set => BASE.LoudnessMax = value; }
    public float LoudnessMaxTime { get => BASE.LoudnessMaxTime; set => BASE.LoudnessMaxTime = value; }
    public float LoudnessEnd { get => BASE.LoudnessEnd; set => BASE.LoudnessEnd = value; }
    public List<float> Pitches { get => BASE.Pitches; set => BASE.Pitches = value; }
    public List<float> Timbre { get => BASE.Timbre; set => BASE.Timbre = value; }
}

[MemoryPackable]
internal partial class MemoryPackableTrackAudio
{
    [MemoryPackIgnore]
    internal readonly TrackAudio BASE;

    [MemoryPackConstructor]
    private MemoryPackableTrackAudio()
    {
        BASE = new();
    }

    internal MemoryPackableTrackAudio(TrackAudio trackAudio)
    {
        BASE = trackAudio;
    }

    public float Duration { get => BASE.Duration; set => BASE.Duration = value; }

    public string SampleMd5 { get => BASE.SampleMd5; set => BASE.SampleMd5 = value; }

    public int OffsetSeconds { get => BASE.OffsetSeconds; set => BASE.OffsetSeconds = value; }

    public int WindowSeconds { get => BASE.WindowSeconds; set => BASE.WindowSeconds = value; }

    public int AnalysisSampleRate { get => BASE.AnalysisSampleRate; set => BASE.AnalysisSampleRate = value; }

    public int AnalysisChannels { get => BASE.AnalysisChannels; set => BASE.AnalysisChannels = value; }

    public float EndOfFadeIn { get => BASE.EndOfFadeIn; set => BASE.EndOfFadeIn = value; }

    public float StartOfFadeOut { get => BASE.StartOfFadeOut; set => BASE.StartOfFadeOut = value; }

    public float Loudness { get => BASE.Loudness; set => BASE.Loudness = value; }

    public float Tempo { get => BASE.Tempo; set => BASE.Tempo = value; }

    public float TempConfidence { get => BASE.TempConfidence; set => BASE.TempConfidence = value; }

    public int TimeSignature { get => BASE.TimeSignature; set => BASE.TimeSignature = value; }

    public float TimeSignatureConfidence { get => BASE.TimeSignatureConfidence; set => BASE.TimeSignatureConfidence = value; }

    public int Key { get => BASE.Key; set => BASE.Key = value; }

    public float KeyConfidence { get => BASE.KeyConfidence; set => BASE.KeyConfidence = value; }

    public int Mode { get => BASE.Mode; set => BASE.Mode = value; }

    public float ModeConfidence { get => BASE.ModeConfidence; set => BASE.ModeConfidence = value; }

    public string Codestring { get => BASE.Codestring; set => BASE.Codestring = value; }

    public float CodeVersion { get => BASE.CodeVersion; set => BASE.CodeVersion = value; }

    public string Echoprintstring { get => BASE.Echoprintstring; set => BASE.Echoprintstring = value; }

    public float EchoprintVersion { get => BASE.EchoprintVersion; set => BASE.EchoprintVersion = value; }

    public string Synchstring { get => BASE.Synchstring; set => BASE.Synchstring = value; }

    public float SynchVersion { get => BASE.SynchVersion; set => BASE.SynchVersion = value; }

    public string Rhythmstring { get => BASE.Rhythmstring; set => BASE.Rhythmstring = value; }

    public float RhythmVersion { get => BASE.RhythmVersion; set => BASE.RhythmVersion = value; }

}

[MemoryPackable]
internal partial class MemoryPackableTrackMeta
{
    [MemoryPackIgnore]
    internal readonly TrackMeta BASE;

    [MemoryPackConstructor]
    private MemoryPackableTrackMeta()
    {
        BASE = new();
    }

    internal MemoryPackableTrackMeta(TrackMeta trackMeta)
    {
        BASE = trackMeta;
    }

    public float AnalysisTime { get => BASE.AnalysisTime; set => BASE.AnalysisTime = value; }

    public string AnalyzerVersion { get => BASE.AnalyzerVersion; set => BASE.AnalyzerVersion = value; }

    public string DetailedStatus { get => BASE.DetailedStatus; set => BASE.DetailedStatus = value; }

    public string InputProcess { get => BASE.InputProcess; set => BASE.InputProcess = value; }

    public string Platform { get => BASE.Platform; set => BASE.Platform = value; }

    public int StatusCode { get => BASE.StatusCode; set => BASE.StatusCode = value; }

    public long Timestamp { get => BASE.Timestamp; set => BASE.Timestamp = value; }
}

[MemoryPackable]
internal partial class MemoryPackableTrackAudioAnalysis
{
    [MemoryPackIgnore]
    internal readonly TrackAudioAnalysis BASE;

    [MemoryPackConstructor]
    private MemoryPackableTrackAudioAnalysis()
    {
        BASE = new();
    }

    internal MemoryPackableTrackAudioAnalysis(TrackAudioAnalysis trackAudioAnalysis)
    {
        BASE = trackAudioAnalysis;
    }

    public IEnumerable<MemoryPackableTimeInterval> Bars { get => BASE.Bars.Select(b => new MemoryPackableTimeInterval(b)); set => BASE.Bars = value.Select(b => b.BASE).ToList(); }
    public IEnumerable<MemoryPackableTimeInterval> Beats { get => BASE.Beats.Select(b => new MemoryPackableTimeInterval(b)); set => BASE.Beats = value.Select(b => b.BASE).ToList(); }
    public IEnumerable<MemoryPackableSection> Sections { get => BASE.Sections.Select(s => new MemoryPackableSection(s)); set => BASE.Sections = value.Select(s => s.BASE).ToList(); }
    public IEnumerable<MemoryPackableSegment> Segments { get => BASE.Segments.Select(s => new MemoryPackableSegment(s)); set => BASE.Segments = value.Select(s => s.BASE).ToList(); }
    public IEnumerable<MemoryPackableTimeInterval> Tatums { get => BASE.Tatums.Select(t => new MemoryPackableTimeInterval(t)); set => BASE.Tatums = value.Select(t => t.BASE).ToList(); }
    public MemoryPackableTrackAudio Track { get => new(BASE.Track); set => BASE.Track = value.BASE; }
    public MemoryPackableTrackMeta Meta { get => new(BASE.Meta); set => BASE.Meta = value.BASE; }
}

public class TrackAudioAnalysisFormatter : MemoryPackFormatter<TrackAudioAnalysis>
{

    public static readonly TrackAudioAnalysisFormatter Instance = new();

    public override void Deserialize(ref MemoryPackReader reader, scoped ref TrackAudioAnalysis? value)
    {
        if (reader.PeekIsNull())
        {
            reader.Advance(1);
            value = null;
            return;
        }

        value = reader.ReadPackable<MemoryPackableTrackAudioAnalysis>()?.BASE;
    }

    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TrackAudioAnalysis? value)
    {
        if (value is null)
        {
            writer.WriteNullObjectHeader();
            return;
        }

        writer.WritePackable(new MemoryPackableTrackAudioAnalysis(value));
    }
}

public class TrackAudioAnalysisFormatterAttribute : MemoryPackCustomFormatterAttribute<TrackAudioAnalysis>
{
    public override IMemoryPackFormatter<TrackAudioAnalysis> GetFormatter()
        => TrackAudioAnalysisFormatter.Instance;
}