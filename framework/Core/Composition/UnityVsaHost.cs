using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VSA.Module;

namespace VSA.Composition;

sealed class UnityVsaHost : IVsaHost {
    public IHostApplicationBuilder CreateBuilder(string[] args) {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings {
            DisableDefaults = true,
            ContentRootPath = Directory.GetCurrentDirectory(),
        });
        builder.Configuration.AddCommandLine(args);
        builder.Services.AddSingleton<IHostLifetime, UnityLifetime>();
        return builder;
    }

    public void Install(IHostApplicationBuilder builder, ModuleTypes types) {
    }

    public IHost Build(IHostApplicationBuilder builder) => ((HostApplicationBuilder)builder).Build();
}
