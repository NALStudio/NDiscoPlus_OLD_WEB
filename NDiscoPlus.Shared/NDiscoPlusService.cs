using NDiscoPlus.Shared.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared;
public class NDiscoPlusService
{
    HttpClient? http;
    Random? random;

    public static readonly ImmutableList<NDiscoPlusColorPalette> DefaultPalettes =
    [
        new NDiscoPlusColorPalette(new(255, 0, 0), new(0, 255, 255), new(255, 105, 180), new(102, 51, 153))
    ];

    public NDiscoPlusData ComputeData(SpotifyPlayerTrack track, NDiscoPlusColorPalette palette)
    {
        return new NDiscoPlusData(
            track,
            new NDiscoPlusContext(),
            palette
        );
    }

    public async Task<NDiscoPlusData> ComputeDataWithImageColors(SpotifyPlayerTrack track)
    {
        var palette = await FetchImagePalette(track);
        return ComputeData(track, palette);
    }

    /// <summary>
    /// Blazor workers have a hard time serializing objects.
    /// </summary>
    /// <param name="serializedTrack">A serialized <see cref="NDiscoPlusData"/> instance.</param>
    /// <returns></returns>
    public async Task<string> ComputeDataWithImageColorsFromSerialized(string serializedTrack)
    {
        SpotifyPlayerTrack track = SpotifyPlayerTrack.Deserialize(serializedTrack);
        NDiscoPlusData data = await ComputeDataWithImageColors(track);
        return NDiscoPlusData.Serialize(data);
    }

    public async Task<NDiscoPlusColorPalette> FetchImagePalette(SpotifyPlayerTrack track)
    {
        http ??= new HttpClient();
        var result = await http.GetAsync(track.ImageUrl);
        if (!result.IsSuccessStatusCode)
        {
            random ??= new Random();
            return DefaultPalettes[random.Next(DefaultPalettes.Count)];
        }

        SKBitmap bitmap = SKBitmap.Decode(await result.Content.ReadAsStreamAsync());
        uint[] pixels = bitmap.Pixels.Select(p => (uint)p).ToArray();
        List<uint> rawColors = MaterialColorUtilities.Utils.ImageUtils.ColorsFromImage(pixels);

        return new NDiscoPlusColorPalette(rawColors.Select(c => new SKColor(c)));
    }
}