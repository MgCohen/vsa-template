using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using VSA.Handlers.Requests;

namespace VSA.Host;

public static class VsaHostExtensions {
    public static WebApplicationBuilder AddVsa(this WebApplicationBuilder builder) {
        WebComposer.Create(builder).Install();
        return builder;
    }

    public static WebApplicationBuilder AddVsa(this WebApplicationBuilder builder, params VSA.Module.Module[] modules) {
        WebComposer.Create(builder, modules).Install();
        return builder;
    }

    public static WebApplication MapVsaRequests(this WebApplication app) {
        if (app.Services.GetService<RequestEndpointInstaller>() is not null) {
            app.UseFastEndpoints();
        }
        return app;
    }
}
