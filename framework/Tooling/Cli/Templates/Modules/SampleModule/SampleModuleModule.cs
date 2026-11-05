using VSA.Module;

namespace SampleModule;

// The root of the SampleModule module. The host finds it and calls Configure at start-up.
// When Domain has an aggregate, register its IRepository here, for example:
//   module.Services.AddScoped<IRepository<Order, OrderId>, OrderRepository>();
public sealed class SampleModuleModule : Module {
    public override void Configure(ModuleBuilder module) {
    }
}
