namespace PackVariant.EcsFeatures.Combat;

using Microsoft.Extensions.DependencyInjection;
using PackVariant.EcsFeatures.Combat.Systems;
using Umpire.Kernels;

public sealed class CombatModule : Module {
    public override void Configure(IServiceCollection services) {
        services.AddSingleton<DamageSystem>();
    }
}
