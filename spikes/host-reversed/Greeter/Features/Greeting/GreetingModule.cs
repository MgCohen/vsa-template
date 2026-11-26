namespace Greeter.Features.Greeting;

using Microsoft.Extensions.DependencyInjection;
using Spike.Kernels;

public sealed class GreetingModule : Module {
    protected override void Configure(IServiceCollection services) =>
        services.AddSingleton<IGreetingFormat, PoliteFormat>();
}
