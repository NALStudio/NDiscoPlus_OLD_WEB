using BlazorWorker.WorkerBackgroundService;
using NDiscoPlus.Shared.MemoryPack;

namespace NDiscoPlus.Components;

public class CustomSerializeLinqExpressionJsonSerializer : SerializeLinqExpressionJsonSerializerBase
{
    public override Type[] GetKnownTypes() => [
        typeof(SerializedValue)
    ];
}
