namespace SampleApp.Features.SampleModule;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernel;

// The root of the SampleModule module. The host finds it and calls Configure at start-up.
// When Domain has an aggregate, register its IRepository here, for example:
//   services.AddScoped<IRepository<Order, OrderId>, OrderRepository>();
public sealed class SampleModuleModule : Module {
    public override void Configure(IServiceCollection services) {
    }
}
