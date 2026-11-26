namespace Greeter.Shared.Features.Counting;

using Microsoft.Extensions.DependencyInjection;
using Spike.Kernels;

public sealed class CountingModule : Module {
    protected override void Configure(IServiceCollection services) =>
        services.AddSingleton<Counter>();
}
