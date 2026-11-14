using Microsoft.Extensions.DependencyInjection;
using Resources.Domain;
using VSA.Module;
using VSA.Kernel;

namespace Resources;

public sealed class ResourcesModule : Module {
    public override void Configure(AnchorBuilder module) => module.Services.AddSingleton<Wallet>();
}
