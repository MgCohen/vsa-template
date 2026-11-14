using Microsoft.Extensions.Hosting;
using VSA.Analyzers.Markers;
using VSA.Module;

namespace VSA.Composition;

[RequireImplementors]
public interface IVsaHost {
    IHostApplicationBuilder CreateBuilder(string[] args);

    void Install(IHostApplicationBuilder builder, ModuleTypes types);

    IHost Build(IHostApplicationBuilder builder);

    int Run(IHost app);

    void Stop(IHost app);
}
