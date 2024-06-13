using NDiscoPlus.Shared.Effects.BaseEffects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Models;

internal readonly struct BackgroundStateContext
{
    public BackgroundStateContext(int lightCount)
    {
        LightCount = lightCount;
    }

    public int LightCount { get; }
}

internal readonly struct StateContext
{
    public StateContext(int lightCount, SectionData sectionData, EffectState? previousState)
    {
        LightCount = lightCount;
        SectionData = sectionData;
        PreviousState = previousState;
    }

    public int LightCount { get; }

    public SectionData SectionData { get; }

    public EffectState? PreviousState { get; }
}
