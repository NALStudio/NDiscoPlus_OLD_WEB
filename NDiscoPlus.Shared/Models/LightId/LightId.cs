namespace NDiscoPlus.Shared.Models;
public abstract class LightId
{
    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();

    public static bool operator ==(LightId a, object? b)
        => a.Equals(b);
    public static bool operator !=(LightId a, object? b)
        => !a.Equals(b);
}
