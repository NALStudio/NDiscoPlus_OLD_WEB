using NDiscoPlus.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Effects.BaseEffects;

internal abstract class EffectState;

internal readonly struct EffectContext
{
    public EffectContext(NDPLightCollection lights, NDPColorPalette palette, Random random, TimeSpan progress, double deltaTime, bool newTrack, int barIndex, bool newBar, int beatIndex, bool newBeat, int tatumIndex, bool newTatum)
    {
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
}

internal abstract class NDPBaseEffect
{
    /// <summary>A common base effect architecture</summary>
    protected NDPBaseEffect()
    {
    }

    public abstract EffectState CreateState();

    public abstract void Update(EffectContext ctx, EffectState effectState);
}