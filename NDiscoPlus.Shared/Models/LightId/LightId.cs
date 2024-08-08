using MemoryPack;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Models;

[MemoryPackable]
[MemoryPackUnion(0, typeof(HueLightId))]
[MemoryPackUnion(1, typeof(ScreenLightId))]
public abstract partial class LightId
{
    [MemoryPackIgnore]
    public abstract string HumanReadableString { get; }

    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();

    public static bool operator ==(LightId? a, object? b)
        => a is not null ? a.Equals(b) : b == null;
    public static bool operator !=(LightId? a, object? b)
        => !(a == b);
}