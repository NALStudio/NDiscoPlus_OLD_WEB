using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Models;
internal readonly struct StateContext
{
    public StateContext(int lightCount)
    {
        LightCount = lightCount;
    }

    public int LightCount { get; }
}
