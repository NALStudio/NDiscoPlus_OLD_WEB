using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Helpers;
internal static class ByteHelper
{
    public static string CastToJsonSafeString(byte[] bytes)
        => Convert.ToBase64String(bytes);

    public static byte[] CastFromJsonSafeString(string str)
        => Convert.FromBase64String(str);
}
