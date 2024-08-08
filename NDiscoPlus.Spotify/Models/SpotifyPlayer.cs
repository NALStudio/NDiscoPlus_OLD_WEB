using MemoryPack;
using SpotifyAPI.Web;

namespace NDiscoPlus.Spotify.Models;

[MemoryPackable]
public partial class SpotifyPlayerTrack
{
    public SpotifyPlayerTrack(string id, string name, TimeSpan length, string imageUrl, string smallImageUrl, string[] artists)
    {
        Id = id;
        Name = name;
        Length = length;
        ImageUrl = imageUrl;
        SmallImageUrl = smallImageUrl;
        Artists = artists;
    }

    public string Id { get; init; }
    public string Name { get; init; }
    public TimeSpan Length { get; init; }
    public string ImageUrl { get; init; }
    public string SmallImageUrl { get; init; }
    public string[] Artists { get; init; }

    public static SpotifyPlayerTrack FromSpotifyTrack(FullTrack track)
    {
        Image[] images = track.Album.Images.Where(i => i.Width == i.Height).ToArray();
        if (images.Length < 1)
            throw new ArgumentException("No valid album images in track.");

        return new SpotifyPlayerTrack(
            track.Id,
            track.Name,
            TimeSpan.FromMilliseconds(track.DurationMs),
            track.Album.Images[0].Url,
            track.Album.Images[^1].Url,
            track.Artists.Select(a => a.Name).ToArray()
        );
    }

    public static SpotifyPlayerTrack? FromSpotifyTrackOrNull(FullTrack? track)
    {
        if (track is null)
            return null;
        return FromSpotifyTrack(track);
    }

    /// <summary>
    /// Serialize to pass object to workers.
    /// Serialized representation is currently JSON, but might change in the future.
    /// </summary>
    public static string Serialize(SpotifyPlayerTrack track)
    {
        byte[] bytes = MemoryPackSerializer.Serialize(track);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Deserialize to receive object from workers.
    /// Serialized representation is currently JSON, but might change in the future. Use the one provided by <see cref="Serialize(SpotifyPlayerTrack)"/>
    /// </summary>
    public static SpotifyPlayerTrack Deserialize(string track)
    {
        byte[] bytes = Convert.FromBase64String(track);
        SpotifyPlayerTrack? t = MemoryPackSerializer.Deserialize<SpotifyPlayerTrack>(bytes);
        return t ?? throw new InvalidOperationException("Cannot deserialize value.");
    }
}

public class SpotifyPlayerContext
{
    public SpotifyPlayerContext(TimeSpan progress, bool isPlaying, SpotifyPlayerTrack track, SpotifyPlayerTrack? nextTrack)
    {
        Progress = progress;
        IsPlaying = isPlaying;
        Track = track;
        NextTrack = nextTrack;
    }

    public TimeSpan Progress { get; }
    public bool IsPlaying { get; }
    public SpotifyPlayerTrack Track { get; }

    /// <summary>
    /// The next track information is supplied once we are almost certain what the next track is going to be.
    /// This depends on the implementation:
    ///     - Spotify web player gives this info in the last 20 seconds of the currently playing song and updates it every 5 seconds.
    /// </summary>
    public SpotifyPlayerTrack? NextTrack { get; }
}