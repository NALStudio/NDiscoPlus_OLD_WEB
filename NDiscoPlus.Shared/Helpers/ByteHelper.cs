using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Helpers;
internal static class ByteHelper
{
    public static string UnsafeCastToString(ReadOnlySpan<byte> bytes)
    {
        Span<char> chars = stackalloc char[bytes.Length];

        for (int i = 0; i < chars.Length; i++)
            chars[i] = (char)bytes[i];

        return new string(chars);
    }

    public static byte[] UnsafeCastFromString(string str)
    {
        ReadOnlySpan<char> src = str.AsSpan();

        byte[] output = new byte[src.Length];
        Span<byte> dest = output.AsSpan();

        for (int i = 0; i < src.Length; i++)
            dest[i] = (byte)src[i];

        return output;
    }
}
