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
using NDiscoPlus.Shared.MemoryPack;
using NDiscoPlus.Shared.MemoryPack.Formatters;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.Color;
using NDiscoPlus.Shared.Music;
using NDiscoPlus.Spotify.Models;
using NDiscoPlus.Spotify.Serializable;
using SkiaSharp;
using SpotifyAPI.Web;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace NDiscoPlus.Shared;

[MemoryPackable]
public partial class NDiscoPlusArgs
{
    public NDiscoPlusArgs(SpotifyPlayerTrack track, NDPColorPalette? referencePalette, TrackAudioFeatures features, TrackAudioAnalysis analysis, EffectConfig effects, NDiscoPlusArgsLights lights)
    {
        Track = track;
        ReferencePalette = referencePalette;

        Features = features;
        Analysis = analysis;

        Effects = effects;
        Lights = lights;
    }

    public SpotifyPlayerTrack Track { get; }

    /// <summary>
    /// <para>If <see langword="null"/>, use a random default color palette.</para>
    /// </summary>
    public NDPColorPalette? ReferencePalette { get; }

    [TrackAudioFeaturesFormatter]
    public TrackAudioFeatures Features { get; }
    [TrackAudioAnalysisFormatter]
    public TrackAudioAnalysis Analysis { get; }

    public EffectConfig Effects { get; }
    public NDiscoPlusArgsLights Lights { get; }
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

    public NDPData ComputeData(NDiscoPlusArgs args)
    {
        // *** ANALYSIS ***
        // Analyze audio
        AudioAnalysis analysis = AudioAnalyzer.Analyze(args.Features, args.Analysis);

        // Generate effects on analysis
        MusicEffectGenerator effectGen = MusicEffectGenerator.CreateRandom(random);
        GeneratedEffects effects = effectGen.Generate(analysis);


        // *** GENERATION ***
        NDPColorPalette referencePalette = args.ReferencePalette ?? GetRandomDefaultPalette();
        NDPColorPalette effectPalette = ModifyPaletteForEffects(referencePalette);

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
        StrobeContext strobeContext = StrobeContext.Extend(context, effects);
        foreach (NDPStrobe strobe in NDPStrobe.All)
            strobe.Generate(strobeContext, api);

        // TODO: Flahes

        // Effects
        foreach (EffectRecord eff in effects.Effects)
        {
            EffectContext effectContext = EffectContext.Extend(context, eff.Section);
            eff.Effect?.Generate(effectContext, api);
        }

        // Background
        new ColorCycleBackgroundEffect().Generate(context, api);

        float endOfFadeIn = args.Analysis.Track.EndOfFadeIn;
        float startOfFadeOut = args.Analysis.Track.StartOfFadeOut;


        // *** OUTPUT ***
        return new NDPData(
            track: args.Track,
            referencePalette: referencePalette,
            effectPalette: effectPalette,

            effectConfig: api.Config,
            effects: ChunkedEffectsCollection.Construct(api),

            lights: args.Lights.Lights
        );
    }
    public SerializedValue ComputeDataBlazorWorker(SerializedValue args)
        => SerializedValue.Serialize(ComputeData(args.Deserialize<NDiscoPlusArgs>()));

    private NDPColorPalette GetRandomDefaultPalette()
    {
        return DefaultPalettes[random.Next(DefaultPalettes.Count)];
    }

    public async Task<NDPColorPalette?> FetchImagePalette(SpotifyPlayerTrack track)
    {
        http ??= new HttpClient();

        TrackImage largestImage = track.Images[0];
        int targetHalfSize = largestImage.Size / 2;
        // Try find an image closest to 50 % of the largest image available (to reduce the power required for palette computation)
        TrackImage halfSizeImage = track.Images.MinBy(img => Math.Abs(targetHalfSize - img.Size));

        var result = await http.GetAsync(halfSizeImage.Url);
        if (!result.IsSuccessStatusCode)
            return null;

        SKBitmap bitmap = SKBitmap.Decode(await result.Content.ReadAsStreamAsync());
        uint[] pixels = Array.ConvertAll(bitmap.Pixels, p => (uint)p);
        List<uint> rawColors = MaterialColorUtilities.Utils.ImageUtils.ColorsFromImage(pixels);

        return new NDPColorPalette(rawColors.Select(c => new SKColor(c)));
    }
    public async Task<SerializedValue?> FetchImagePaletteBlazorWorker(SerializedValue track)
    {
        NDPColorPalette? palette = await FetchImagePalette(track.Deserialize<SpotifyPlayerTrack>());
        if (palette.HasValue)
            return SerializedValue.Serialize(palette.Value);
        else
            return null;
    }
}