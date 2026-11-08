using Microsoft.Extensions.Hosting;
using VSA.Module;

namespace VSA.Composition;

sealed class GenericVsaHost : IVsaHost {
    public IHostApplicationBuilder CreateBuilder(string[] args) => Host.CreateApplicationBuilder(args);

    public void Install(IHostApplicationBuilder builder, ModuleTypes types) {
    }

    public IHost Build(IHostApplicationBuilder builder) => ((HostApplicationBuilder)builder).Build();
}
