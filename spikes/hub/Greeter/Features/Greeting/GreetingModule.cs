namespace Greeter.Features.Greeting;

using Greeter.Features.Greeting.Slices.Greet;
using Microsoft.Extensions.DependencyInjection;
using Spike.Core;

public sealed class GreetingModule : Module {
    protected override void Configure(IServiceCollection services) =>
        services.AddValidator<GreetValidator>();
}
