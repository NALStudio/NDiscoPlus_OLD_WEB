using NDiscoPlus.Shared.Models;

namespace NDiscoPlus.Shared.Effects.Fade;

internal abstract class NDPFade
{
    public abstract void FadeOut(NDPLight[] lights, double t);

    public void FadeIn(NDPLight[] lights, double t) => FadeOut(lights, 1d - t);
}