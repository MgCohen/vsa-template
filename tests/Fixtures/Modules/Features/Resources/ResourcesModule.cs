namespace Resources;

using Microsoft.Extensions.DependencyInjection;
using Resources.Domain;
using Umpire.Kernel;

public sealed class ResourcesModule : Module {
    public override void Configure(IServiceCollection services) => services.AddSingleton<Wallet>();
}
