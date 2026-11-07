using Microsoft.AspNetCore.Builder;
using VSA.Composition;
using VSA.Handlers.Requests;
using VSA.Module;

namespace VSA.Host;

public static class WebComposer {
    public static VsaComposer Create(WebApplicationBuilder builder) =>
        Create(builder, VsaComposer.DiscoveredModules());

    public static VsaComposer Create(WebApplicationBuilder builder, params VSA.Module.Module[] modules) =>
        new VsaComposer(new MicrosoftServices(builder.Services), builder.Configuration, modules)
            .AddBefore<ModuleInstaller>(new LoopbackBusInstaller(builder.Services))
            .AddBefore<ModuleInstaller>(new RequestEndpointInstaller(builder.Services));
}
