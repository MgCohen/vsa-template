namespace Twin;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernel;

public sealed class TwinModule : Module {
    public override void Configure(IServiceCollection services) {
    }
}

public sealed class OtherTwinModule : Module {
    public override void Configure(IServiceCollection services) {
    }
}
