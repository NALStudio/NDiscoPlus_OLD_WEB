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
public class NDiscoPlusService
{
    HttpClient? http;
    Random? random;

    public static readonly ImmutableList<NDPColorPalette> DefaultPalettes =
    [
        new NDPColorPalette(new(255, 0, 0), new(0, 255, 255), new(255, 105, 180), new(102, 51, 153))
    ];

    public NDPData ComputeData(SpotifyPlayerTrack track, TrackAudioAnalysis analysis, NDPColorPalette palette)
    {
        return new NDPData(
            track,
            new NDPContext(),
            palette,
            analysis
        );
    }

    public async Task<NDPData> ComputeDataWithImageColors(SpotifyPlayerTrack track, TrackAudioAnalysis analysis)
    {
        var palette = await FetchImagePalette(track);
        return ComputeData(track, analysis, palette);
    }

    /// <summary>
    /// Blazor workers have a hard time serializing objects.
    /// </summary>
    /// <param name="serializedTrack">A serialized <see cref="NDPData"/> instance.</param>
    /// <returns></returns>
    public async Task<string> ComputeDataWithImageColorsFromSerialized(string serializedTrack, string serializedAnalysis)
    {
        SpotifyPlayerTrack track = SpotifyPlayerTrack.Deserialize(serializedTrack);

        TrackAudioAnalysis? analysis = JsonSerializer.Deserialize<TrackAudioAnalysis>(serializedAnalysis);
        if (analysis is null)
            throw new InvalidOperationException("Cannot deserialize analysis.");

        NDPData data = await ComputeDataWithImageColors(track, analysis);
        return NDPData.Serialize(data);
    }

    public async Task<NDPColorPalette> FetchImagePalette(SpotifyPlayerTrack track)
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

        return new NDPColorPalette(rawColors.Select(c => new SKColor(c)));
    }
}