using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Helpers;
public static class MemoryPackHelper
{
    public static string SerializeToBase64<T>(T? value, MemoryPackSerializerOptions? options = null)
    {
        byte[] bytes = MemoryPackSerializer.Serialize(value, options: options);
        return Convert.ToBase64String(bytes);
    }

    public static T? DeserializeFromBase64<T>(string value, MemoryPackSerializerOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);

        byte[] bytes = Convert.FromBase64String(value);
        return MemoryPackSerializer.Deserialize<T>(bytes, options: options);
    }

    // Seems to mostly roundtrip correctly, but did break when translating to a JSON key... So we'll use Base64 instead.
    // private static string UnsafeCastToStringUtf8(ReadOnlySpan<byte> bytes)
    // {
    //     Span<char> chars = stackalloc char[bytes.Length];
    // 
    //     for (int i = 0; i < chars.Length; i++)
    //         chars[i] = (char)bytes[i]; // Cannot do straight span cast as C# is utf-16 and BlazorWorker is utf-8
    // 
    //     return new string(chars);
    // }
    // private static byte[] UnsafeCastFromStringUtf8(string str)
    // {
    //     ReadOnlySpan<char> src = str.AsSpan();
    // 
    //     byte[] output = new byte[src.Length];
    //     Span<byte> dest = output.AsSpan();
    // 
    //     for (int i = 0; i < src.Length; i++)
    //         dest[i] = (byte)src[i];
    // 
    //     return output;
    // }

    // Doesn't seem to roundtrip correctly...
    // public static string UnsafeCastToStringUtf16(ReadOnlySpan<byte> bytes)
    // {
    //     ReadOnlySpan<char> chars = MemoryMarshal.Cast<byte, char>(bytes);
    //     return new string(chars);
    // }
    // public static ReadOnlySpan<byte> UnsafeCastFromStringUtf16(string str)
    // {
    //     ReadOnlySpan<char> chars = str.AsSpan();
    //     return MemoryMarshal.Cast<char, byte>(chars);
    // }
}
