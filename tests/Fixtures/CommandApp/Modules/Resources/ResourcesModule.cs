using Resources.Domain;
using VSA.Module;

namespace Resources;

public sealed class ResourcesModule : Module {
    public override void Configure(ModuleBuilder module) => module.Services.AddSingleton<Wallet>();
}
