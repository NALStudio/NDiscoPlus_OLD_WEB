using MemoryPack;

namespace NDiscoPlus.Shared.Models;

[MemoryPackable]
public partial class ScreenLightId : LightId
{
    public int Index { get; }

    public override string HumanReadableString => $"Screen Light (index: {Index})";

    public ScreenLightId(int index)
    {
        Index = index;
    }

    public override int GetHashCode()
        => HashCode.Combine(GetType(), Index);

    public override bool Equals(object? obj)
        => obj is ScreenLightId sli && Index == sli.Index;
}
