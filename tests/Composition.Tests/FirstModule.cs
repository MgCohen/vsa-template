namespace Composition.Tests.First;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

public sealed class FirstModule : Module {
    protected internal override int Order => 1;

    public override void Configure(IServiceCollection services) =>
        services.AddSingleton(new ComposeOrderTests.Step("module:1"));
}
