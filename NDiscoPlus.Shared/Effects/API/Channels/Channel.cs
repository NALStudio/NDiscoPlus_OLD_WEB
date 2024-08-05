using NDiscoPlus.Shared.Models;
using System.Collections.Frozen;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Effects.API.Channels;
public abstract class Channel
{
    public NDPLightCollection Lights { get; }

    protected Channel(IEnumerable<NDPLight> lights)
    {
        Lights = NDPLightCollection.Create(lights);
    }

    public NDPLight GetLight(LightId id) => Lights[id];
    public bool TryGetLight(LightId id, out NDPLight light) => Lights.TryGetValue(id, out light);
}
