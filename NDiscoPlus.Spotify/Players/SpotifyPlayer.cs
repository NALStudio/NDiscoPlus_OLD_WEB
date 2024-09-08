using NDiscoPlus.Spotify.Models;
using System.Runtime.CompilerServices;

namespace NDiscoPlus.Spotify.Players;

public delegate void TrackChanged(SpotifyPlayerTrack? oldTrack, SpotifyPlayerTrack? newTrack);

public abstract class SpotifyPlayer
{
    protected abstract ValueTask Init();
    protected abstract SpotifyPlayerContext? Update();

    public async IAsyncEnumerable<SpotifyPlayerContext?> ListenAsync(int frequency, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Init();

        double periodSeconds = 1d / frequency; // Hz = 1/s => s = 1/Hz

        PeriodicTimer timer = new(TimeSpan.FromSeconds(periodSeconds));

        SpotifyPlayerTrack? currentTrack = null;
        SpotifyPlayerTrack? nextTrack = null;

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
            SpotifyPlayerContext? ctx = Update();

            SpotifyPlayerTrack? newCurrentTrack = ctx?.Track;
            SpotifyPlayerTrack? newNextTrack = ctx?.NextTrack;

            if (newCurrentTrack?.Id != currentTrack?.Id)
            {
                CurrentTrackChanged?.Invoke(currentTrack, newCurrentTrack);
                currentTrack = newCurrentTrack;
            }
            if (newNextTrack?.Id != nextTrack?.Id)
            {
                NextTrackChanged?.Invoke(nextTrack, newNextTrack);
                nextTrack = newNextTrack;
            }

            yield return ctx;
        }
    }

    public event TrackChanged? CurrentTrackChanged;
    public event TrackChanged? NextTrackChanged;
}
