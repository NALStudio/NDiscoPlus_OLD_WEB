using NDiscoPlus.Shared.Models;
using System.Collections.Frozen;

namespace NDiscoPlus.Shared.Effects.API.Channels;
public abstract class Channel
{
    public IList<NDPLight> Lights => lights.Values;

    protected readonly FrozenDictionary<LightId, NDPLight> lights;

    protected Channel(params NDPLight[] lights)
    {
        this.lights = lights.ToFrozenDictionary(keySelector: l => l.Id);
    }

    public NDPLight GetLight(LightId id) => lights[id];
    public bool TryGetLight(LightId id, out NDPLight light) => lights.TryGetValue(id, out light);
}
