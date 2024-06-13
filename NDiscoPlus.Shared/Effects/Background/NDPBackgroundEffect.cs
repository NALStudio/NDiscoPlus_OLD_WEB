using NDiscoPlus.Shared.Models;

namespace NDiscoPlus.Shared.Effects.BaseEffects;

internal abstract class EffectState;

internal abstract class NDPBackgroundEffect
{
    protected NDPBackgroundEffect()
    {
    }

    public abstract EffectState CreateState(BackgroundStateContext ctx);

    public abstract void Update(EffectContext ctx, EffectState effectState);
}