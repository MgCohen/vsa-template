using Microsoft.Extensions.DependencyInjection;
using VSA.Kernel;

namespace Twin;

// Two Module classes in one module namespace. AnchorRoot.CreateModule must give an error for it.
public sealed class TwinModule : Module {
    public override void Configure(IServiceCollection services) {
    }
}

public sealed class OtherTwinModule : Module {
    public override void Configure(IServiceCollection services) {
    }
}
