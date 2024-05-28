using NDiscoPlus.Shared.Music;

namespace NDiscoPlus.Shared.Interpreters;
public abstract class LightInterpreter
{
    protected readonly MusicIL IL;

    public LightInterpreter(MusicIL IL)
    {
        this.IL = IL;
    }
}
