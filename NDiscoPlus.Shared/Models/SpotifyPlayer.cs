namespace NDiscoPlus.Shared.Models;

public record SpotifyPlayerContext(
    TimeSpan Progress,
    bool IsPlaying,
    string TrackName,
    TimeSpan TrackLength,
    string ImageUrl,
    string[] Artists
);