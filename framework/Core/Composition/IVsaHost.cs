using Microsoft.Extensions.Hosting;
using VSA.Module;

namespace VSA.Composition;

public interface IVsaHost {
    IHostApplicationBuilder CreateBuilder(string[] args);

    void Install(IHostApplicationBuilder builder, ModuleTypes types);

    IHost Build(IHostApplicationBuilder builder);
}
