namespace NDiscoPlus.Shared.Models;
public class ScreenLightId : LightId
{
    public int Index { get; }

    public ScreenLightId(int index)
    {
        Index = index;
    }

    public override int GetHashCode()
        => HashCode.Combine(GetType(), Index);

    public override bool Equals(object? obj)
        => obj is ScreenLightId sli && Index == sli.Index;
}
