using NDiscoPlus.Shared.Models;

namespace NDiscoPlus.Shared.Effects.BaseEffects;

internal abstract class EffectState;

internal abstract class NDPBaseEffect
{
    /// <summary>A common base effect architecture</summary>
    protected NDPBaseEffect()
    {
    }

    public abstract EffectState CreateState(StateContext ctx);

    public abstract void Update(EffectContext ctx, EffectState effectState);
}