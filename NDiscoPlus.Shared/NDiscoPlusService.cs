using NDiscoPlus.Shared.Effects.API;
using NDiscoPlus.Shared.Effects.API.Channels.Background;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
using NDiscoPlus.Shared.Effects.BaseEffects;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;
using NDiscoPlus.Shared.Music;
using SkiaSharp;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared;

public class NDiscoPlusArgs
{
    public NDiscoPlusArgs(SpotifyPlayerTrack track, TrackAudioFeatures features, TrackAudioAnalysis analysis, EffectConfig effects, NDiscoPlusArgsLights lights)
    {
        Track = track;
        Features = features;
        Analysis = analysis;
        Effects = effects;
        Lights = lights;
    }

    public SpotifyPlayerTrack Track { get; }
    public TrackAudioFeatures Features { get; }
    public TrackAudioAnalysis Analysis { get; }

    public EffectConfig Effects { get; }
    public NDiscoPlusArgsLights Lights { get; }

    /// <summary>
    /// Serialize to pass object to workers.
    /// Serialized representation is currently JSON, but might change in the future.
    /// </summary>
    public static string Serialize(NDiscoPlusArgs args)
    {
        string output = JsonSerializer.Serialize(args);
        Debug.Assert(!string.IsNullOrEmpty(output));
        return output;
    }

    /// <summary>
    /// Deserialize to receive object from workers.
    /// Serialized representation is currently JSON, but might change in the future. Use the one provided by <see cref="Serialize(SpotifyPlayerTrack)"/>
    /// </summary>
    public static NDiscoPlusArgs Deserialize(string args)
    {
        NDiscoPlusArgs? t = JsonSerializer.Deserialize<NDiscoPlusArgs>(args);
        return t ?? throw new InvalidOperationException("Cannot deserialize value.");
    }
}

public class NDiscoPlusArgsLights
{
    [JsonInclude]
    private ImmutableArray<NDPLight> strobe;
    [JsonInclude]
    private ImmutableArray<NDPLight> flash;
    [JsonInclude]
    private ImmutableArray<NDPLight> effect;
    [JsonInclude]
    private ImmutableArray<NDPLight> background;

    public NDiscoPlusArgsLights()
    {
        strobe = ImmutableArray<NDPLight>.Empty;
        flash = ImmutableArray<NDPLight>.Empty;
        effect = ImmutableArray<NDPLight>.Empty;
        background = ImmutableArray<NDPLight>.Empty;
    }

    [JsonConstructor]
    private NDiscoPlusArgsLights(ImmutableArray<NDPLight> strobe, ImmutableArray<NDPLight> flash, ImmutableArray<NDPLight> effect, ImmutableArray<NDPLight> background)
    {
        this.strobe = strobe;
        this.flash = flash;
        this.effect = effect;
        this.background = background;
    }

    [JsonIgnore]
    public IList<NDPLight> Strobe
    {
        get { return strobe; }
        init { strobe = value.ToImmutableArray(); }
    }

    [JsonIgnore]
    public IList<NDPLight> Flash
    {
        get { return flash; }
        init { flash = value.ToImmutableArray(); }
    }

    [JsonIgnore]
    public IList<NDPLight> Effect
    {
        get { return effect; }
        init { effect = value.ToImmutableArray(); }
    }

    [JsonIgnore]
    public IList<NDPLight> Background
    {
        get { return background; }
        init { background = value.ToImmutableArray(); }
    }

    public static NDiscoPlusArgsLights CreateSingleChannel(IEnumerable<NDPLight> lights)
        => CreateSingleChannel(lights.ToImmutableArray());

    public static NDiscoPlusArgsLights CreateSingleChannel(params NDPLight[] lights)
        => CreateSingleChannel(lights.ToImmutableArray());

    public static NDiscoPlusArgsLights CreateSingleChannel(ImmutableArray<NDPLight> lights)
    {
        return new(lights, lights, lights, lights);
    }
}

public class NDiscoPlusService
{
    HttpClient? http;
    readonly Random random = new();

    public static readonly ImmutableList<NDPColorPalette> DefaultPalettes =
    [
        // sRGB
        new NDPColorPalette(new SKColor(255, 0, 0), new SKColor(0, 255, 255), new SKColor(255, 105, 180), new SKColor(102, 51, 153)),
        new NDPColorPalette(new SKColor(15, 192, 252), new SKColor(123, 29, 175), new SKColor(255, 47, 185), new SKColor(212, 255, 71)),
        new NDPColorPalette(new SKColor(255, 0, 0), new SKColor(0, 255, 0), new SKColor(0, 0, 255), new SKColor(255, 255, 0)),
        new NDPColorPalette(new SKColor(164, 20, 217), new SKColor(255, 128, 43), new SKColor(249, 225, 5), new SKColor(52, 199, 165), new SKColor(93, 80, 206)),

        // Hue color space
        // six colors might be a bit excessive, but I was annoyed that the final lerp was missing.
        new NDPColorPalette(
            ColorGamut.hueGamutC.Red.ToColor(),
            NDPColor.Lerp(ColorGamut.hueGamutC.Red.ToColor(), ColorGamut.hueGamutC.Green.ToColor(), 0.5),
            ColorGamut.hueGamutC.Green.ToColor(),
            NDPColor.Lerp(ColorGamut.hueGamutC.Green.ToColor(), ColorGamut.hueGamutC.Blue.ToColor(), 0.5),
            ColorGamut.hueGamutC.Blue.ToColor(),
            NDPColor.Lerp(ColorGamut.hueGamutC.Blue.ToColor(), ColorGamut.hueGamutC.Red.ToColor(), 0.5)
        )
    ];

    private NDPColorPalette GetRandomDefaultPalette()
    {
        return DefaultPalettes[random.Next(DefaultPalettes.Count)];
    }

    private NDPColorPalette ModifyPaletteForEffects(NDPColorPalette palette)
    {
        const int MinPaletteCount = 4;
        const int MaxPaletteCount = 5;

        // cull out similar colors
        List<NDPColor> newPalette = new(capacity: palette.Count);
        for (int i = 0; i < palette.Count; i++)
        {
            NDPColor col = palette[i];

            bool append = true;
            for (int j = 0; j < i; j++)
            {
                NDPColor c = palette[j];

                double xDist = col.X - c.X;
                double yDist = col.Y - c.Y;
                double distance = Math.Sqrt((xDist * xDist) + (yDist * yDist));
                if (distance < 0.1)
                    append = false;
            }

            if (append)
                newPalette.Add(col);
        }

        if (newPalette.Count < MinPaletteCount)
            return GetRandomDefaultPalette();

        if (newPalette.Count > MaxPaletteCount)
            return new NDPColorPalette(palette.Take(MaxPaletteCount));

        return palette;
    }

    public NDPData ComputeData(NDiscoPlusArgs args, NDPColorPalette palette)
    {
        MusicEffectGenerator effectGen = MusicEffectGenerator.CreateRandom(random);

        NDPColorPalette effectPalette = ModifyPaletteForEffects(palette);

        EffectAPI api = new(
            args.Effects,
            [
                new StrobeEffectChannel(args.Lights.Strobe),
                new FlashEffectChannel(args.Lights.Flash),
                new DefaultEffectChannel(args.Lights.Effect)
            ],
            new BackgroundChannel(args.Lights.Background)
        );

        // TODO: Strobes

        // TODO: Flahes

        // Effects
        foreach (EffectRecord eff in effectGen.Generate(args))
        {
            EffectContext ctx = EffectContext.Create(
                random: random,
                palette: effectPalette,
                analysis: args.Analysis,
                section: eff.Section
            );

            eff.Effect?.Generate(ctx, api);
        }

        // Background
        BackgroundContext bCtx = new(random, effectPalette, args.Analysis);
        new ColorCycleBackgroundEffect().Generate(bCtx, api);

        float endOfFadeIn = args.Analysis.Track.EndOfFadeIn;
        float startOfFadeOut = args.Analysis.Track.StartOfFadeOut;

        return new NDPData(
            track: args.Track,
            referencePalette: palette,
            effectPalette: effectPalette,

            effectConfig: api.Config,
            effects: api.Export()
        );
    }

    public async Task<NDPData> ComputeDataWithImageColors(NDiscoPlusArgs args)
    {
        var palette = await FetchImagePalette(args.Track);
        return ComputeData(args, palette);
    }

    /// <summary>
    /// Blazor workers have a hard time serializing objects.
    /// </summary>
    /// <param name="argsSerialized">A serialized <see cref="NDiscoPlusArgs"/> instance.</param>
    public async Task<string> ComputeDataWithImageColorsFromSerialized(string argsSerialized)
    {
        NDiscoPlusArgs args = NDiscoPlusArgs.Deserialize(argsSerialized);
        NDPData data = await ComputeDataWithImageColors(args);
        return NDPData.Serialize(data);
    }

    public async Task<NDPColorPalette> FetchImagePalette(SpotifyPlayerTrack track)
    {
        http ??= new HttpClient();
        var result = await http.GetAsync(track.ImageUrl);
        if (!result.IsSuccessStatusCode)
            return GetRandomDefaultPalette();

        SKBitmap bitmap = SKBitmap.Decode(await result.Content.ReadAsStreamAsync());
        uint[] pixels = Array.ConvertAll(bitmap.Pixels, p => (uint)p);
        List<uint> rawColors = MaterialColorUtilities.Utils.ImageUtils.ColorsFromImage(pixels);

        return new NDPColorPalette(rawColors.Select(c => new SKColor(c)));
    }
}