using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NDiscoPlus;

public class NDiscoPlus : IAsyncDisposable
{
    [MemberNotNullWhen(true, nameof(playerThreadCancellation), nameof(playerThread))]
    public bool PlayerThreadRunning
    {
        get
        {
            Debug.Assert(playerThread != null);
            return playerThreadCancellation != null;
        }
    }
    CancellationTokenSource? playerThreadCancellation;
    Thread? playerThread;

    [MemberNotNullWhen(true, nameof(lightThreadCancellation), nameof(lightThread))]
    public bool LightThreadRunning
    {
        get
        {
            Debug.Assert(lightThread != null);
            return lightThreadCancellation != null;
        }
    }
    CancellationTokenSource? lightThreadCancellation;
    Thread? lightThread;

    private NDiscoPlus()
    {
    }

    /// <summary>
    /// Starts a new NDiscoPlus player
    /// </summary>
    public static NDiscoPlus StartNew()
    {
        NDiscoPlus ndp = new();

        ndp.StartPlayer();

        return ndp;
    }

    private void StartPlayer()
    {
        if (PlayerThreadRunning)
            throw new InvalidOperationException("Player running already");

        Debug.Assert(playerThreadCancellation == null);
        Debug.Assert(playerThread == null);

        playerThreadCancellation = new CancellationTokenSource();

        playerThread = new Thread(async () => await PlayerLoop(playerThreadCancellation.Token));
        playerThread.Start();
    }

    private async Task StopPlayer()
    {
        if (!PlayerThreadRunning)
            return;

        await playerThreadCancellation.CancelAsync();
        Debug.Assert(!playerThread.IsAlive);

        playerThreadCancellation = null;
        playerThread = null;
    }

    public void StartLights()
    {
        if (LightThreadRunning)
            throw new InvalidOperationException("Lights running already");

        Debug.Assert(lightThreadCancellation == null);
        Debug.Assert(lightThread == null);

        lightThreadCancellation = new CancellationTokenSource();

        lightThread = new Thread(async () => await LightLoop(lightThreadCancellation.Token));
        lightThread.Start();
    }

    public async Task StopLights()
    {
        if (!LightThreadRunning)
            return;

        await lightThreadCancellation.CancelAsync();
        Debug.Assert(!lightThread.IsAlive);

        lightThreadCancellation = null;
        lightThread = null;
    }


    private static async Task PlayerLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("ABC ABC ABC");
            await Task.Delay(1000, cancellationToken);
        }
    }

    private static async Task LightLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("CBA CBA CBA");
            await Task.Delay(1000, cancellationToken);
        }
    }

    /// <summary>
    /// Light loop must be stopped before disposing the NDiscoPlus instance.
    /// Dispose stops the player loop.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await StopPlayer();
    }
}
