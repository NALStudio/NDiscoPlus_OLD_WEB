using SpotifyAPI.Web;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Models;

public record SpotifyPlayerTrack
{
    public SpotifyPlayerTrack(string id, string name, TimeSpan length, string imageUrl, string[] artists)
    {
        Id = id;
        Name = name;
        Length = length;
        ImageUrl = imageUrl;
        Artists = artists;
    }

    [JsonRequired]
    public string Id { get; init; }

    [JsonRequired]
    public string Name { get; init; }

    [JsonRequired]
    public TimeSpan Length { get; init; }

    [JsonRequired]
    public string ImageUrl { get; init; }

    [JsonRequired]
    public string[] Artists { get; init; }


    public static SpotifyPlayerTrack FromSpotifyTrack(FullTrack track)
    {
        return new SpotifyPlayerTrack(
            track.Id,
            track.Name,
            TimeSpan.FromMilliseconds(track.DurationMs),
            track.Album.Images[0].Url,
            track.Artists.Select(a => a.Name).ToArray()
        );
    }

    /// <summary>
    /// Serialize to pass object to workers.
    /// Serialized representation is currently JSON, but might change in the future.
    /// </summary>
    public static string Serialize(SpotifyPlayerTrack track)
    {
        string output = JsonSerializer.Serialize(track);
        Debug.Assert(!string.IsNullOrEmpty(output));
        return output;
    }

    /// <summary>
    /// Deserialize to receive object from workers.
    /// Serialized representation is currently JSON, but might change in the future. Use the one provided by <see cref="Serialize(SpotifyPlayerTrack)"/>
    /// </summary>
    public static SpotifyPlayerTrack Deserialize(string track)
    {
        SpotifyPlayerTrack? t = JsonSerializer.Deserialize<SpotifyPlayerTrack>(track);
        return t ?? throw new InvalidOperationException("Cannot deserialize value.");
    }
}

public record SpotifyPlayerContext(
    TimeSpan Progress,
    bool IsPlaying,
    SpotifyPlayerTrack Track,

    /// Next Track might or might not be accurate depending on when the data was loaded by the player implementation.
    /// SpotifyWebPlayer loads the next track when the song is changed.
    SpotifyPlayerTrack? NextTrack
);