using NDiscoPlus.Shared.Models;
using System.Collections.Frozen;

namespace NDiscoPlus.Shared.Effects.API.Channels;
public abstract class Channel
{
    public NDPLightCollection Lights => lights;

    protected readonly NDPLightCollection lights;

    protected Channel(IList<NDPLight> lights)
    {
        this.lights = NDPLightCollection.Create(lights);
    }

    public NDPLight GetLight(LightId id) => lights[id];
    public bool TryGetLight(LightId id, out NDPLight light) => lights.TryGetValue(id, out light);
}
