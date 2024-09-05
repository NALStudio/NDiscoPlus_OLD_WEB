using MemoryPack;
using NDiscoPlus.Shared.Analyzer;
using NDiscoPlus.Shared.Analyzer.Analysis;
using NDiscoPlus.Shared.Effects.API;
using NDiscoPlus.Shared.Effects.API.Channels.Background;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
using NDiscoPlus.Shared.Effects.BaseEffects;
using NDiscoPlus.Shared.Effects.StrobeAnalyzers;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.MemoryPack.Formatters;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;
using NDiscoPlus.Shared.Music;
using NDiscoPlus.Spotify.Models;
using NDiscoPlus.Spotify.Serializable;
using SkiaSharp;
using SpotifyAPI.Web;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared;

[MemoryPackable]
public partial class NDiscoPlusArgs
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

    [TrackAudioFeaturesFormatter]
    public TrackAudioFeatures Features { get; }
    [TrackAudioAnalysisFormatter]
    public TrackAudioAnalysis Analysis { get; }

    public EffectConfig Effects { get; }
    public NDiscoPlusArgsLights Lights { get; }

    /// <summary>
    /// Serialize to pass object to workers.
    /// Serialized representation is currently JSON, but might change in the future.
    /// </summary>
    public static string Serialize(NDiscoPlusArgs args)
    {
        byte[] bytes = MemoryPackSerializer.Serialize(args);
        return ByteHelper.UnsafeCastToStringUtf8(bytes);
    }

    /// <summary>
    /// Deserialize to receive object from workers.
    /// Serialized representation is currently JSON, but might change in the future. Use the one provided by <see cref="Serialize(SpotifyPlayerTrack)"/>
    /// </summary>
    public static NDiscoPlusArgs Deserialize(string args)
    {
        ReadOnlySpan<byte> bytes = ByteHelper.UnsafeCastFromStringUtf8(args);
        NDiscoPlusArgs? d = MemoryPackSerializer.Deserialize<NDiscoPlusArgs>(bytes);
        return d ?? throw new InvalidOperationException("Cannot deserialize value.");
    }
}

[MemoryPackable]
public partial class NDiscoPlusArgsLights
{
    [NDPLightFrozenDictionaryValueFormatter]
    public FrozenDictionary<LightId, NDPLight> Lights { get; }

    public ImmutableArray<LightId> Strobe { get; }
    public ImmutableArray<LightId> Flash { get; }
    public ImmutableArray<LightId> Effect { get; }
    public ImmutableArray<LightId> Background { get; }

    public NDiscoPlusArgsLights(IEnumerable<NDPLight> lights)
    {
        Lights = lights.ToFrozenDictionary(key => key.Id);

        Strobe = ImmutableArray<LightId>.Empty;
        Flash = ImmutableArray<LightId>.Empty;
        Effect = ImmutableArray<LightId>.Empty;
        Background = ImmutableArray<LightId>.Empty;
    }

    [MemoryPackConstructor]
    private NDiscoPlusArgsLights(FrozenDictionary<LightId, NDPLight> lights, ImmutableArray<LightId> strobe, ImmutableArray<LightId> flash, ImmutableArray<LightId> effect, ImmutableArray<LightId> background)
    {
        Lights = lights;

        Strobe = strobe;
        Flash = flash;
        Effect = effect;
        Background = background;
    }

    public static NDiscoPlusArgsLights CreateSingleChannel(IEnumerable<NDPLight> lights)
        => CreateSingleChannel(lights.ToImmutableArray());

    public static NDiscoPlusArgsLights CreateSingleChannel(params NDPLight[] lights)
        => CreateSingleChannel(lights.ToImmutableArray());

    public static NDiscoPlusArgsLights CreateSingleChannel(ImmutableArray<NDPLight> lights)
    {
        FrozenDictionary<LightId, NDPLight> lightsDict = lights.ToFrozenDictionary(key => key.Id);
        ImmutableArray<LightId> lightIds = lightsDict.Keys;
        return new(lightsDict, lightIds, lightIds, lightIds, lightIds);
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
        // Analyze audio
        AudioAnalysis analysis = AudioAnalyzer.Analyze(args.Features, args.Analysis);

        // Initialize effect generation
        MusicEffectGenerator effectGen = MusicEffectGenerator.CreateRandom(random);

        NDPColorPalette effectPalette = ModifyPaletteForEffects(palette);

        EffectAPI api = new(
            args.Effects,
            [
                new StrobeEffectChannel(args.Lights.Strobe.Select(id => args.Lights.Lights[id])),
                new FlashEffectChannel(args.Lights.Flash.Select(id => args.Lights.Lights[id])),
                new DefaultEffectChannel(args.Lights.Effect.Select(id => args.Lights.Lights[id])),
                new BackgroundEffectChannel(args.Lights.Background.Select(id => args.Lights.Lights[id])),
            ],
            new BackgroundChannel(args.Lights.Background.Select(id => args.Lights.Lights[id]))
        );

        Models.Context context = new(
            random: random,
            palette: effectPalette,
            analysis: analysis
        );

        // Strobes
        foreach (NDPStrobe strobe in NDPStrobe.All)
            strobe.Generate(context, api);

        // TODO: Flahes

        // Effects
        foreach (EffectRecord eff in effectGen.Generate(analysis))
        {
            EffectContext effectContext = EffectContext.Extend(context, eff.Section);

            eff.Effect?.Generate(effectContext, api);
        }

        // Background
        new ColorCycleBackgroundEffect().Generate(context, api);

        float endOfFadeIn = args.Analysis.Track.EndOfFadeIn;
        float startOfFadeOut = args.Analysis.Track.StartOfFadeOut;

        return new NDPData(
            track: args.Track,
            referencePalette: palette,
            effectPalette: effectPalette,

            effectConfig: api.Config,
            effects: ChunkedEffectsCollection.Construct(api),

            lights: args.Lights.Lights
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

        TrackImage largestImage = track.Images[0];
        int targetHalfSize = largestImage.Size / 2;
        // Try find an image closest to 50 % of the largest image available (to reduce the power required for palette computation)
        TrackImage halfSizeImage = track.Images.MinBy(img => Math.Abs(targetHalfSize - img.Size));

        var result = await http.GetAsync(halfSizeImage.Url);
        if (!result.IsSuccessStatusCode)
            return GetRandomDefaultPalette();

        SKBitmap bitmap = SKBitmap.Decode(await result.Content.ReadAsStreamAsync());
        uint[] pixels = Array.ConvertAll(bitmap.Pixels, p => (uint)p);
        List<uint> rawColors = MaterialColorUtilities.Utils.ImageUtils.ColorsFromImage(pixels);

        return new NDPColorPalette(rawColors.Select(c => new SKColor(c)));
    }
}