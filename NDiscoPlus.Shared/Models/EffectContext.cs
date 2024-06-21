using NDiscoPlus.Shared.Music;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

internal readonly struct TimingContext
{
    public TimingContext(int currentIndex, NDPInterval? current, bool isNew, TimeSpan? progress, int nextIndex, NDPInterval? next, TimeSpan? untilNext)
    {
        CurrentIndex = currentIndex;
        Current = current;
        IsNew = isNew;
        Progress = progress;
        NextIndex = nextIndex;
        Next = next;
        UntilNext = untilNext;
    }

    public int CurrentIndex { get; }
    public NDPInterval? Current { get; }

    /// <summary>
    /// Whether the bar/beat/tatum is new (index has changed)
    /// </summary>
    public bool IsNew { get; }

    /// <summary>
    /// How long have we progressed inside this bar/beat/tatum.
    /// </summary>
    public TimeSpan? Progress { get; }

    public int NextIndex { get; }
    public NDPInterval? Next { get; }
    /// <summary>
    /// How long until the next bar/beat/tatum
    /// </summary>
    public TimeSpan? UntilNext { get; }


    public static TimingContext Construct(TimeSpan progress, int currentIndex, bool isNew, IList<NDPInterval> intervals)
    {
        NDPInterval? current = currentIndex > -1 ? intervals[currentIndex] : null;

        int nextIndex = currentIndex + 1;
        if (nextIndex >= intervals.Count)
            nextIndex = -1;

        NDPInterval? next = nextIndex > -1 ? intervals[nextIndex] : null;

        return new TimingContext(
            currentIndex: currentIndex,
            current: current,
            isNew: isNew,
            progress: current.HasValue ? progress - current.Value.Start : null,

            nextIndex: nextIndex,
            next: next,
            untilNext: next.HasValue ? next.Value.Start - progress : null
        );
    }
}

internal readonly struct EffectContext
{
    public EffectContext(
        LightInterpreterConfig config,
        NDPLightCollection lights,
        NDPColorPalette palette,
        Random random,
        TimeSpan progress,
        double deltaTime,
        bool newTrack,
        TimingContext barTiming,
        TimingContext beatTiming,
        TimingContext tatumTiming
    )
    {
        Config = config;

        Lights = lights;
        Palette = palette;

        Random = random;

        Progress = progress;
        DeltaTime = deltaTime;

        NewTrack = newTrack;

        BarTiming = barTiming;
        BeatTiming = beatTiming;
        TatumTiming = tatumTiming;
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

    public TimingContext BarTiming { get; }
    public TimingContext BeatTiming { get; }
    public TimingContext TatumTiming { get; }
}