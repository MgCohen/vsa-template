namespace Signals;

using Microsoft.Extensions.DependencyInjection;
using Signals.Slices.Ring;
using Umpire.Kernels;

public sealed class SignalsModule : Module {
    protected override void Configure(IServiceCollection services) => services.AddSingleton<RingLog>();
}
