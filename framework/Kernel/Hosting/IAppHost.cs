using Microsoft.Extensions.Hosting;
using VSA.Analyzers.Markers;

namespace VSA.Kernel;

[RequireImplementors]
public interface IAppHost {
    IHostApplicationBuilder CreateBuilder(string[] args);

    void Install(IHostApplicationBuilder builder, AnchorTypes types);

    IHost Build(IHostApplicationBuilder builder);

    int Run(IHost app);

    void Stop(IHost app);
}
