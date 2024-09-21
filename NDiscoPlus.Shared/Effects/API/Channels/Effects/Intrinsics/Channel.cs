namespace NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;

[Flags]
public enum Channel
{
    None = 0,

    // Channel that come after override previous channels 
    Background = 1 << 0,
    Default = 1 << 1,
    Flash = 1 << 2,
    Strobe = 1 << 3,

    // int with all bits set
    All = int.MaxValue
}