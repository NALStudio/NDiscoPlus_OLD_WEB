using NDiscoPlus.Shared.Music;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Models;

internal readonly struct SectionData
{
    private readonly Section section;

    public SectionData(Section section)
    {
        this.section = section;
    }

    public double MinutesPerBeat => 1d / section.Tempo;
    public double SecondsPerBeat => MinutesPerBeat * 60d;
    public double SecondsPerBar => SecondsPerBeat * section.TimeSignature;
}

internal readonly struct EffectContext
{
    public EffectContext(LightInterpreterConfig config, NDPLightCollection lights, NDPColorPalette palette, Random random, TimeSpan progress, double deltaTime, bool newTrack, int barIndex, bool newBar, int beatIndex, bool newBeat, int tatumIndex, bool newTatum)
    {
        Config = config;

        Lights = lights;
        Palette = palette;

        Random = random;

        Progress = progress;
        DeltaTime = deltaTime;

        NewTrack = newTrack;

        BarIndex = barIndex;
        NewBar = newBar;
        BeatIndex = beatIndex;
        NewBeat = newBeat;
        TatumIndex = tatumIndex;
        NewTatum = newTatum;
    }

    public LightInterpreterConfig Config { get; }

    public NDPLightCollection Lights { get; }
    public NDPColorPalette Palette { get; }

    public Random Random { get; }

    public TimeSpan Progress { get; }
    /// <summary>
    /// A more accurate way to measure time between frames than <see cref="Progress"/>
    /// </summary>
    public double DeltaTime { get; }

    /// <summary>
    /// Whether the track has changed.
    /// </summary>
    public bool NewTrack { get; }

    public int BarIndex { get; }
    public bool NewBar { get; }
    public int BeatIndex { get; }
    public bool NewBeat { get; }
    public int TatumIndex { get; }
    public bool NewTatum { get; }

    public bool GetSync(EffectSync sync)
    {
        return sync switch
        {
            EffectSync.Bar => NewBar,
            EffectSync.Beat => NewBeat,
            EffectSync.Tatum => NewTatum,
            _ => throw new ArgumentException("Invalid sync", nameof(sync))
        };
    }

    public int GetSyncIndex(EffectSync sync)
    {
        return sync switch
        {
            EffectSync.Bar => BarIndex,
            EffectSync.Beat => BeatIndex,
            EffectSync.Tatum => TatumIndex,
            _ => throw new ArgumentException("Invalid sync", nameof(sync))
        };
    }
}