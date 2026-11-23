namespace Resources;

using Microsoft.Extensions.DependencyInjection;
using Resources.Domain;
using Umpire.Kernels;

public sealed class ResourcesModule : Module {
    protected override void Configure(IServiceCollection services) => services.AddSingleton<Wallet>();
}
