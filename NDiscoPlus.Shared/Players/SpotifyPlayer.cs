using NDiscoPlus.Shared.Models;
using System.Runtime.CompilerServices;

namespace NDiscoPlus.Shared.Players;

public abstract class SpotifyPlayer
{
    protected abstract Task Init();
    protected abstract SpotifyPlayerContext? Update();

    public async IAsyncEnumerable<SpotifyPlayerContext?> ListenAsync(int frequency, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Init();

        double periodSeconds = 1d / frequency; // Hz = 1/s => s = 1/Hz

        PeriodicTimer timer = new(TimeSpan.FromSeconds(periodSeconds));

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
            yield return Update();
        }
    }
}
