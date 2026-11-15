using Microsoft.Extensions.DependencyInjection;
using Resources.Domain;
using VSA.Kernel;

namespace Resources;

public sealed class ResourcesModule : Module {
    public override void Configure(IServiceCollection services) => services.AddSingleton<Wallet>();
}
