namespace NDiscoPlus.Shared.Models;
public class ScreenLightId : LightId
{
    public int Index { get; }

    public ScreenLightId(int index)
    {
        Index = index;
    }

}
