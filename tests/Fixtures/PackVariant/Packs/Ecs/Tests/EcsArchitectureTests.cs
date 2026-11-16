namespace PackVariant.Packs.Ecs.Tests;

using NUnit.Framework;
using PackVariant.EcsFeatures.Combat;
using PackVariant.EcsFeatures.Combat.Systems;
using PackVariant.Packs.Ecs.Architecture;
using Umpire.Architecture;
using Umpire.Vsa.Testing.Unit;

public sealed class EcsArchitectureTests : VsaArchitectureTest<EcsArchitecture> {
    [Test]
    public void The_module_class_of_an_ecs_feature_binds_to_its_module_root() {
        Assert.That(At<CombatModule>("EcsFeatures/Combat").Part, Is.EqualTo("Module"));
    }

    [Test]
    public void A_system_binds_to_the_systems_of_its_module() {
        Assert.That(At<DamageSystem>("EcsFeatures/Combat/Systems").Part, Is.EqualTo("Systems"));
    }

    [Test]
    public void A_system_uses_the_components_of_its_own_module() {
        var system = At<DamageSystem>("EcsFeatures/Combat/Systems");

        Assert.That(MayUse(system, At("EcsFeatures/Combat/Components")).Permission, Is.EqualTo(Permission.Allow));
        Assert.That(MayUse(system, At("EcsFeatures/Loot/Components")).Permission, Is.EqualTo(Permission.Deny));
    }

    [Test]
    public void A_feature_module_keeps_the_slices_of_the_VSA_architecture() {
        Assert.That(At("Features/Inventory/Slices/AddItem").Part, Is.EqualTo("Slice"));
    }
}
