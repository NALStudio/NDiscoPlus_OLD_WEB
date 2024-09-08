using SpotifyAPI.Web;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Models;

public readonly record struct NDPInterval(TimeSpan Start, TimeSpan Duration)
{
    public static NDPInterval FromSeconds(double Start, double Duration)
        => new(TimeSpan.FromSeconds(Start), TimeSpan.FromSeconds(Duration));
    public static NDPInterval FromStartAndEnd(TimeSpan Start, TimeSpan End)
        => new(Start, End - Start);

    public TimeSpan End => Start + Duration;

    public bool Contains(TimeSpan t)
    {
        return t >= Start && t <= End;
    }

    public bool ContainsInterval(NDPInterval interval)
    {
        return interval.Start >= Start && interval.End <= End;
    }

    public static bool Overlap(NDPInterval a, NDPInterval b)
    {
        return a.Start < b.End && b.Start < a.End;
    }

    public static explicit operator NDPInterval(TimeInterval interval)
    {
        TimeSpan start = TimeSpan.FromSeconds(interval.Start);
        TimeSpan duration = TimeSpan.FromSeconds(interval.Duration);
        return new NDPInterval(Start: start, Duration: duration);
    }
}