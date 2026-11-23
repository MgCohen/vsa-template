namespace Composition.Tests.First;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

public sealed class FirstModule : Module {
    protected internal override int Order => 1;

    protected override void Configure(IServiceCollection services) =>
        services.AddSingleton(new ComposeOrderTests.Step("module:1"));
}
