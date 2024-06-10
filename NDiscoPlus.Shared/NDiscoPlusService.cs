using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.Models;
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

public record NDiscoPlusArgs(SpotifyPlayerTrack Track, TrackAudioAnalysis Analysis)
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
    Random? random;

    public static readonly ImmutableList<NDPColorPalette> DefaultPalettes =
    [
        new NDPColorPalette(new(255, 0, 0), new(0, 255, 255), new(255, 105, 180), new(102, 51, 153)),
        new NDPColorPalette(new(15, 192, 252), new(123, 29, 175), new(255, 47, 185), new(212, 255, 71)),
        new NDPColorPalette(new(255, 0, 0), new(0, 255, 0), new(0, 0, 255), new(255, 255, 0)),
        new NDPColorPalette(new(164, 20, 217), new(255, 128, 43), new(249, 225, 5), new(52, 199, 165), new(93, 80, 206)),
    ];

    private NDPColorPalette GetRandomDefaultPalette()
    {
        random ??= new Random();
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

    /// <summary>
    /// Palette might get overridden if it's deemed to be insufficient.
    /// </summary>
    public NDPData ComputeData(NDiscoPlusArgs args, NDPColorPalette palette)
    {
        NDPColorPalette effectPalette = ModifyPaletteForEffects(palette);

        return new NDPData(
            args.Track,
            new NDPContext(),
            palette,
            effectPalette,
            NDPTimings.FromAnalysis(args.Analysis)
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