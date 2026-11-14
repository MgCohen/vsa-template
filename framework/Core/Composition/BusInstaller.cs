using Microsoft.Extensions.DependencyInjection;
using VSA.Presenters;
using VSA.Kernel;

namespace VSA.Composition;

sealed class BusInstaller : IInstaller {
    public void Install(IServiceCollection services, AnchorTypes types) =>
        services.AddScoped<IBus>(provider => new InProcessBus(provider));
}
