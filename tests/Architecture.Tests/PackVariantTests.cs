namespace EngineCore.Tests;

using PackVariant.Packs.Ecs.Architecture;
using Umpire.Architecture;
using Umpire.Architecture.Blocks;
using Umpire.Kernels.Blocks;
using Umpire.Vsa.Definition;

public sealed class PackVariantTests {
    static readonly EcsArchitecture Definition = new();
    static readonly ResolvedArchitecture Resolved = new(Definition);

    [Test]
    public void The_variant_keeps_each_decision_of_the_VSA_architecture() {
        var variant = VsaDecisionsTests.Decisions(Resolved).ToHashSet();

        Assert.That(VsaDecisionsTests.Decisions(new ResolvedArchitecture(new VsaArchitecture())).Where(d => !variant.Contains(d)), Is.Empty);
    }

    [Test]
    public void The_new_module_kind_has_its_own_group_folder() {
        var paths = Resolved.Parts.Where(InEcsFeature).Select(p => p.Path);

        Assert.That(paths, Is.SupersetOf(new[] { "App/EcsFeatures/{ecsfeature}/Components", "App/EcsFeatures/{ecsfeature}/Systems" }));
    }

    [TestCase("Systems", "Components", "a", Permission.Allow)]
    [TestCase("Systems", "Components", "b", Permission.Deny)]
    [TestCase("Components", "Systems", "a", Permission.Deny)]
    public void A_system_uses_the_components_of_its_own_module(string user, string target, string module, Permission expected) {
        var decision = Resolved.DecisionFor(Part(user), Module("a"), Part(target), Module(module));

        Assert.That(decision.Permission, Is.EqualTo(expected));
    }

    [Test]
    public void A_system_uses_the_pack_of_the_app() {
        var pack = Resolved.Parts.Single(p => p.Is(typeof(Pack)));

        var decision = Resolved.DecisionFor(Part("Systems"), Module("a"), pack, new Dictionary<string, string> { ["Pack"] = "Ecs" });

        Assert.That(decision.Permission, Is.EqualTo(Permission.Allow));
    }

    static bool InEcsFeature(Part part) => part.SelfAndAncestors().Any(a => a.Is(typeof(EcsFeature)));

    static Part Part(string name) => Resolved.Parts.Single(p => p.Name == name && InEcsFeature(p));

    static Dictionary<string, string> Module(string name) => new() { ["EcsFeature"] = name };
}
