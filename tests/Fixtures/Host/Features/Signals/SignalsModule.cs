using Microsoft.Extensions.DependencyInjection;
using Signals.Slices.Ring;
using Umpire.Kernel;

namespace Signals;

public sealed class SignalsModule : Module {
    public override void Configure(IServiceCollection services) => services.AddSingleton<RingLog>();
}
