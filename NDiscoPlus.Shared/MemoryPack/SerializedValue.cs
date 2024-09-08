using MemoryPack;
using NDiscoPlus.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.MemoryPack;

[DataContract]
public sealed class SerializedValue
{
    [DataMember]
    [Newtonsoft.Json.JsonProperty(PropertyName = "@type")]
    private readonly string _typeName;

    [DataMember]
    [Newtonsoft.Json.JsonProperty(PropertyName = "@value")]
    private readonly string _value;

    [Newtonsoft.Json.JsonConstructor]
    private SerializedValue(string typeName, string value)
    {
        _typeName = typeName;
        _value = value;
    }

    private static string GetTypeName<T>()
        => typeof(T).Name;

    public static SerializedValue Serialize<T>(T value) where T : IMemoryPackable<T>
    {
        string typeName = GetTypeName<T>();

        byte[] bytes = MemoryPackSerializer.Serialize(value);
        string serialized = ByteHelper.UnsafeCastToStringUtf8(bytes);
        return new(typeName, serialized);
    }

    private static T Deserialize<T>(SerializedValue valueSerialized)
    {
        string typeName = GetTypeName<T>();
        if (valueSerialized._typeName != typeName)
            throw new ArgumentException($"Cannot deserialize value of type '{valueSerialized._typeName}' as type '{typeName}'.", nameof(valueSerialized));

        ReadOnlySpan<byte> bytes = ByteHelper.UnsafeCastFromStringUtf8(valueSerialized._value);
        T? deserialized = MemoryPackSerializer.Deserialize<T>(bytes);
        return deserialized ?? throw new ArgumentException("Cannot deserialize value.", nameof(valueSerialized));
    }
    public T Deserialize<T>() => Deserialize<T>(this);
}