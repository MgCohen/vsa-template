namespace EngineCore.Tests;

using PackVariant.Packs.Ecs.Architecture;
using Umpire.Architecture;
using Umpire.Vsa.Definition;

public sealed class PackVariantTests {
    static readonly EcsArchitecture Definition = new();

    [Test]
    public void The_variant_keeps_each_decision_of_the_VSA_architecture() {
        var variant = VsaDecisionsTests.Decisions(Definition).ToHashSet();

        Assert.That(VsaDecisionsTests.Decisions(new VsaArchitecture()).Where(d => !variant.Contains(d)), Is.Empty);
    }

    [Test]
    public void The_new_module_kind_has_its_own_group_folder() {
        var paths = Definition.Parts().Where(InEcsFeature).Select(p => p.Path);

        Assert.That(paths, Is.SupersetOf(new[] { "EcsFeatures/{ecsfeature}/Components", "EcsFeatures/{ecsfeature}/Systems" }));
    }

    [TestCase("Systems", "Components", "a", Permission.Allow)]
    [TestCase("Systems", "Components", "b", Permission.Deny)]
    [TestCase("Components", "Systems", "a", Permission.Deny)]
    public void A_system_uses_the_components_of_its_own_module(string user, string target, string module, Permission expected) {
        var decision = MayUse.For(Definition, Part(user), Module("a"), Part(target), Module(module));

        Assert.That(decision.Permission, Is.EqualTo(expected));
    }

    [Test]
    public void A_system_uses_the_pack_of_the_app() {
        var pack = Definition.Parts().Single(p => p.Is(typeof(Pack)));

        var decision = MayUse.For(Definition, Part("Systems"), Module("a"), pack, new Dictionary<string, string> { ["Pack"] = "Ecs" });

        Assert.That(decision.Permission, Is.EqualTo(Permission.Allow));
    }

    static bool InEcsFeature(Part part) => part.SelfAndAncestors().Any(a => a.Is(typeof(EcsFeature)));

    static Part Part(string name) => Definition.Parts().Single(p => p.Name == name && InEcsFeature(p));

    static Dictionary<string, string> Module(string name) => new() { ["EcsFeature"] = name };
}
