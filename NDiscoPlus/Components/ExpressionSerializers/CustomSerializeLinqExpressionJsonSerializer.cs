using BlazorWorker.WorkerBackgroundService;
using NDiscoPlus.Shared.MemoryPack;

namespace NDiscoPlus.Components.ExpressionSerializers;

public class CustomSerializeLinqExpressionJsonSerializer : SerializeLinqExpressionJsonSerializerBase
{
    public override Type[] GetKnownTypes() => [
        typeof(SerializedValue)
    ];
}
