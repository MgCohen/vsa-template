using Microsoft.AspNetCore.Builder;
using VSA.Composition;
using VSA.Host;

namespace VSA.App;

public static class VsaAppExtensions {
    public static WebApplicationBuilder AddVsa(this WebApplicationBuilder builder) =>
        builder.AddVsa(VsaServiceExtensions.DiscoveredModules());

    public static WebApplicationBuilder AddVsa(this WebApplicationBuilder builder, params VSA.Module.Module[] modules) {
        builder.Services
            .AddVsaCore()
            .AddVsaHost(modules)
            .AddVsaModules(builder.Configuration, modules);
        return builder;
    }
}
