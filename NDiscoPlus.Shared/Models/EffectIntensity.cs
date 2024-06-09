using NDiscoPlus.Shared.Helpers;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Models;
internal readonly struct EffectIntensity : IEquatable<EffectIntensity>, IComparable<EffectIntensity>
{
    private readonly byte intensity;

    private EffectIntensity(byte intensity)
    {
        this.intensity = intensity;
    }

    public static readonly EffectIntensity VeryLow = new(1);
    public static readonly EffectIntensity Low = new(2);
    public static readonly EffectIntensity Medium = new(3);
    public static readonly EffectIntensity High = new(4);
    public static readonly EffectIntensity VeryHigh = new(5);

    public EffectIntensity FromSection(Section section)
    {
        double loudnessFactor = ((double)section.Loudness).Remap01(-60, 0);

        // range: 0 - 1
        double totalFactor = loudnessFactor;

        // range: 0 - 5 where 5 is very very rare
        int intensityRef = (int)(totalFactor * 5);

        // range: 1 - 5
        byte intensity = (byte)Math.Clamp(intensityRef + 1, 1, 5);

        return new(intensity);
    }

    public override bool Equals(object? obj)
        => obj is EffectIntensity intensity && Equals(intensity);

    public bool Equals(EffectIntensity other)
        => intensity == other.intensity;

    public override int GetHashCode()
        => HashCode.Combine(intensity);

    public int CompareTo(EffectIntensity other)
        => intensity.CompareTo(other.intensity);

    public static bool operator ==(EffectIntensity left, EffectIntensity right)
        => left.Equals(right);

    public static bool operator !=(EffectIntensity left, EffectIntensity right)
        => !(left == right);
}