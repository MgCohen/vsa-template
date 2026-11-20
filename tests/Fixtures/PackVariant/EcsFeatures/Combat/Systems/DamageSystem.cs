namespace PackVariant.EcsFeatures.Combat.Systems;

using PackVariant.EcsFeatures.Combat.Components;
using PackVariant.Packs.Ecs.Kernel.Domain;

public sealed class DamageSystem : EcsSystem {
    public Health Current { get; private set; } = new(100);

    public override void Update(float seconds) {
        Current = Current with { Points = Current.Points - (int)seconds };
    }
}
