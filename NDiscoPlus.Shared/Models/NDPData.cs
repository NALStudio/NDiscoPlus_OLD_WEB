using MemoryPack;
using Microsoft.AspNetCore.WebUtilities;
using NDiscoPlus.Shared.Effects.API;
using NDiscoPlus.Shared.Effects.API.Channels.Background;
using NDiscoPlus.Shared.Effects.API.Channels.Effects;
using NDiscoPlus.Shared.Effects.API.Channels.Effects.Intrinsics;
using NDiscoPlus.Shared.Helpers;
using NDiscoPlus.Shared.MemoryPack.Formatters;
using NDiscoPlus.Spotify.Models;
using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace NDiscoPlus.Shared.Models;

[MemoryPackable]
public partial class NDPData
{
    [MemoryPackConstructor]
    internal NDPData(
        SpotifyPlayerTrack track,
        NDPColorPalette referencePalette, NDPColorPalette effectPalette,
        EffectConfig effectConfig, ChunkedEffectsCollection effects,
        FrozenDictionary<LightId, NDPLight> lights
    )
    {
        Track = track;
        ReferencePalette = referencePalette;
        EffectPalette = effectPalette;

        EffectConfig = effectConfig;
        Effects = effects;

        Lights = lights;
    }

    public SpotifyPlayerTrack Track { get; }

    public NDPColorPalette ReferencePalette { get; }
    public NDPColorPalette EffectPalette { get; }

    public EffectConfig EffectConfig { get; }
    public ChunkedEffectsCollection Effects { get; }

    [NDPLightFrozenDictionaryValueFormatter]
    public FrozenDictionary<LightId, NDPLight> Lights { get; }

    public static string Serialize(NDPData data)
    {
        byte[] bytes = MemoryPackSerializer.Serialize(data);
        return ByteHelper.UnsafeCastToStringUtf8(bytes);
    }

    public static NDPData Deserialize(string dataSerialized)
    {
        ReadOnlySpan<byte> bytes = ByteHelper.UnsafeCastFromStringUtf8(dataSerialized);
        NDPData? data = MemoryPackSerializer.Deserialize<NDPData>(bytes);
        return data ?? throw new ArgumentException("Cannot deserialize value.", nameof(dataSerialized));
    }
}


[MemoryPackable]
internal readonly partial struct EffectChunk
{
    [MemoryPackInclude]
    private readonly List<int> effectIndexes;

    [MemoryPackIgnore]
    public IEnumerable<int> EffectIndexes => effectIndexes.AsReadOnly();

    public void AddIndex(int index)
        => effectIndexes.Add(index);

    public EffectChunk()
    {
        effectIndexes = new();
    }

#pragma warning disable IDE0051 // Remove unused private members
    [MemoryPackConstructor]
    private EffectChunk(List<int> effectIndexes)
    {
        this.effectIndexes = effectIndexes;
    }
#pragma warning restore IDE0051 // Remove unused private members
}

[MemoryPackable]
public sealed partial class ChunkedEffectsCollection
{

    public const int CHUNK_SIZE_SECONDS = 1;

    public FrozenDictionary<LightId, ImmutableArray<BackgroundTransition>> BackgroundTransitions { get; }


    [MemoryPackInclude]
    private readonly ImmutableArray<EffectChunk> chunks;
    [MemoryPackInclude]
    private readonly ImmutableArray<Effect> effects;

    [MemoryPackIgnore]
    public int ChunkCount => chunks.Length;
    [MemoryPackIgnore]
    public static TimeSpan ChunkSize => TimeSpan.FromSeconds(CHUNK_SIZE_SECONDS);

    private ChunkedEffectsCollection(ImmutableArray<EffectChunk> chunks, ImmutableArray<Effect> effects, FrozenDictionary<LightId, ImmutableArray<BackgroundTransition>> backgroundTransitions)
    {
        this.chunks = chunks;
        this.effects = effects;
        BackgroundTransitions = backgroundTransitions;
    }

    private static int ToChunkIndex(TimeSpan time)
        => ToChunkIndex(time.TotalSeconds);

    private static int ToChunkIndex(double timeSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeSeconds, 0d, nameof(timeSeconds));
        return (int)timeSeconds / CHUNK_SIZE_SECONDS;
    }

    private bool TryGetChunk(TimeSpan time, [MaybeNullWhen(false)] out EffectChunk chunk)
    {
        int index = ToChunkIndex(time);
        if (index >= chunks.Length)
        {
            chunk = default;
            return false;
        }

        chunk = chunks[index];
        return true;
    }

    private IEnumerable<Effect> GetEffects(EffectChunk chunk)
    {
        foreach (int effectIndex in chunk.EffectIndexes)
            yield return effects[effectIndex];
    }

    public IEnumerable<Effect> GetEffectsOfChunk(int chunkIndex)
    {
        if (chunkIndex < 0 || chunkIndex >= chunks.Length)
            throw new ArgumentOutOfRangeException(nameof(chunkIndex));

        return GetEffects(chunks[chunkIndex]);
    }

    public IEnumerable<Effect> GetEffects(TimeSpan time)
    {
        if (TryGetChunk(time, out EffectChunk chunk))
            return GetEffects(chunk);
        else
            return Enumerable.Empty<Effect>();
    }

    private static void ConstructEffects(ref List<Effect> effects, ref List<EffectChunk> chunks, EffectChannel channel)
    {
        foreach (Effect e in channel.Effects)
        {
            int effectIndex = effects.Count;
            effects.Add(e);

            double startTotalSeconds = e.Start.TotalSeconds;
            int startChunk = startTotalSeconds >= 0d ? ToChunkIndex(startTotalSeconds) : 0;
            int endChunk = ToChunkIndex(e.End); // inclusive

            while (chunks.Count < (endChunk + 1))
                chunks.Add(new EffectChunk());

            for (int i = startChunk; i <= endChunk; i++)
                chunks[i].AddIndex(effectIndex);
        }
    }

    internal static ChunkedEffectsCollection Construct(EffectAPI effects)
    {
        List<Effect> effectList = new();
        List<EffectChunk> chunkList = new();
        // iterate in reverse so that later channels override the previous channels (strobes override flashes, flashes override default effects, ...)
        for (int i = (effects.Channels.Count - 1); i >= 0; i--)
            ConstructEffects(ref effectList, ref chunkList, effects.Channels[i]);

        return new ChunkedEffectsCollection(
            chunks: chunkList.ToImmutableArray(),
            effects: effectList.ToImmutableArray(),
            backgroundTransitions: effects.Background.Freeze()
        );
    }
}