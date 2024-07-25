using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
public readonly struct Effect
{
    [JsonConverter(typeof(JsonLightIdConverter))]
    public LightId LightId { get; }
    public TimeSpan Position { get; }
    public TimeSpan Duration { get; }

    public double? X { get; init; } = null;
    public double? Y { get; init; } = null;
    public double? Brightness { get; init; } = null;

    public TimeSpan FadeIn { get; init; } = TimeSpan.Zero;
    public TimeSpan FadeOut { get; init; } = TimeSpan.Zero;

    [JsonIgnore]
    public TimeSpan Start => Position - FadeIn;
    [JsonIgnore]
    public TimeSpan End => Position + Duration + FadeOut;

    public Effect(LightId light, TimeSpan position, TimeSpan duration)
    {
        LightId = light;
        Position = position;
        Duration = duration;
    }

    public Effect(LightId light, TimeSpan position, TimeSpan duration, NDPColor color) : this(light: light, position: position, duration: duration)
    {
        X = color.X;
        Y = color.Y;
        Brightness = color.Brightness;
    }

    public Effect(LightId light, TimeSpan position, TimeSpan duration, double brightness) : this(light: light, position: position, duration: duration)
    {
        Brightness = brightness;
    }

    public static Effect CreateStrobe(EffectConfig config, LightId light, TimeSpan position, TimeSpan duration)
    {
        NDPColor color = config.StrobeColor;

        return config.StrobeStyle switch
        {
            EffectConfig.StrobeStyles.Instant => new(light, position, duration, color),
            _ => throw new NotImplementedException()
        };
    }

#pragma warning disable IDE0051 // Remove unused private members
    [JsonConstructor]
    private Effect(LightId lightId, TimeSpan position, TimeSpan duration, double? x, double? y, double? brightness, TimeSpan fadeIn, TimeSpan fadeOut)
    {
        LightId = lightId;
        Position = position;
        Duration = duration;
        X = x;
        Y = y;
        Brightness = brightness;
        FadeIn = fadeIn;
        FadeOut = fadeOut;
    }
#pragma warning restore IDE0051 // Remove unused private members

    public NDPColor GetColor(NDPColor baseColor)
        => new(X ?? baseColor.X, Y ?? baseColor.Y, Brightness ?? baseColor.Brightness);

    public NDPColor Interpolate(TimeSpan progress, NDPColor from)
    {
        NDPColor to = GetColor(from);

        if (progress >= Position && progress < (Position + Duration))
            return to;

        double t;
        if (progress < Position)
            t = (progress - Start) / FadeIn;
        else
            t = 1d - ((progress - (Position + Duration)) / FadeOut);

        return NDPColor.Lerp(from, to, t);
    }
}