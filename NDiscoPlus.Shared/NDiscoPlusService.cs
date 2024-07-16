using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
using NDiscoPlus.Shared.Models.NDPColor;
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
using System.Threading.Tasks;

namespace NDiscoPlus.Shared;

public record NDiscoPlusArgs(SpotifyPlayerTrack Track, TrackAudioFeatures Features, TrackAudioAnalysis Analysis)
{
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

public class NDiscoPlusService
{
    HttpClient? http;
    readonly Random random = new();

    public static readonly ImmutableList<NDPColorPalette> DefaultPalettes =
    [
        // sRGB
        new NDPColorPalette(NDPColor.FromSRGB(255, 0, 0), NDPColor.FromSRGB(0, 255, 255), NDPColor.FromSRGB(255, 105, 180), NDPColor.FromSRGB(102, 51, 153)),
        new NDPColorPalette(NDPColor.FromSRGB(15, 192, 252), NDPColor.FromSRGB(123, 29, 175), NDPColor.FromSRGB(255, 47, 185), NDPColor.FromSRGB(212, 255, 71)),
        new NDPColorPalette(NDPColor.FromSRGB(255, 0, 0), NDPColor.FromSRGB(0, 255, 0), NDPColor.FromSRGB(0, 0, 255), NDPColor.FromSRGB(255, 255, 0)),
        new NDPColorPalette(NDPColor.FromSRGB(164, 20, 217), NDPColor.FromSRGB(255, 128, 43), NDPColor.FromSRGB(249, 225, 5), NDPColor.FromSRGB(52, 199, 165), NDPColor.FromSRGB(93, 80, 206)),

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

        if (palette.Count < MinPaletteCount)
            return GetRandomDefaultPalette();

        if (palette.Count > MaxPaletteCount)
            return new NDPColorPalette(palette.Take(MaxPaletteCount));

        return palette;
    }

    public NDPData ComputeData(NDiscoPlusArgs args, NDPColorPalette palette)
    {
        NDPColorPalette effectPalette = ModifyPaletteForEffects(palette);

        MusicEffectGenerator effectGen = MusicEffectGenerator.CreateRandom(random);
        ImmutableArray<EffectRecord> effects = effectGen.Generate(args).ToImmutableArray();

        return new NDPData(
            track: args.Track,
            context: new NDPContext(),
            referencePalette: palette,
            effectPalette: effectPalette,
            timings: NDPTimings.FromAnalysis(args.Analysis),
            effects: effects
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