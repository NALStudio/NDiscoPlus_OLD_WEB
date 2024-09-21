using NDiscoPlus.Components.LightHandlerConfigEditor;
using NDiscoPlus.Shared.Models;

namespace NDiscoPlus.LightHandlers.Hue;

public class HueLightHandlerConfig : LightHandlerConfig
{
    public override LightHandler CreateLightHandler()
        => new HueLightHandler(this);

    public override Type GetEditorType()
        => typeof(HueLightHandlerConfigEditor);
}

public class HueLightHandler : LightHandler
{
    public HueLightHandler(LightHandlerConfig? config) : base(config)
    {
    }

    public override ValueTask<NDPLight[]> GetLights()
    {
        throw new NotImplementedException();
    }

    public override ValueTask<bool> Start(ErrorMessageCollector? errors, out NDPLight[] lights)
    {
        throw new NotImplementedException();
    }

    public override ValueTask Stop()
    {
        throw new NotImplementedException();
    }

    public override ValueTask Update(LightColorCollection lights)
    {
        throw new NotImplementedException();
    }

    public override ValueTask<bool> ValidateConfig(ErrorMessageCollector? errors)
    {
        throw new NotImplementedException();
    }

    protected override LightHandlerConfig CreateConfig()
        => new HueLightHandlerConfig();
}
