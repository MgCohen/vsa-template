using Microsoft.Extensions.DependencyInjection;
using Resources.Domain;
using Umpire.Kernel;

namespace Resources;

public sealed class ResourcesModule : Module {
    public override void Configure(IServiceCollection services) => services.AddSingleton<Wallet>();
}
