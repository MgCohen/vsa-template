namespace Composition.Tests.Last;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

public sealed class LastModule : Module {
    protected internal override int Order => 2;

    protected override void Configure(IServiceCollection services) =>
        services.AddSingleton(new ComposeOrderTests.Step("module:2"));
}
