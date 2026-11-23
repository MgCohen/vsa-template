namespace EngineCore.Tests;

using PackVariant.EcsFeatures.Combat.Systems;
using PackVariant.Packs.Ecs.Architecture;
using Umpire.Architecture;

public sealed class ArchitectureTestVariantTests : ArchitectureTest<EcsArchitecture> {
    [Test]
    public void A_system_binds_to_the_systems_of_its_module() {
        var place = At<DamageSystem>("App/EcsFeatures/Combat/Systems");

        Assert.That(place.Part, Is.EqualTo("Systems"));
        Assert.That(place.Values["EcsFeature"], Is.EqualTo("Combat"));
    }

    [Test]
    public void A_system_does_not_use_the_components_of_another_module() {
        var system = At<DamageSystem>("App/EcsFeatures/Combat/Systems");

        Assert.That(MayUse(system, At("App/EcsFeatures/Loot/Components")).Permission, Is.EqualTo(Permission.Deny));
    }
}
