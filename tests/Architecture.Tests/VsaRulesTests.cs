namespace EngineCore.Tests;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;
using Umpire.Vsa.Definition;

public sealed class VsaRulesTests {
    static readonly ResolvedArchitecture Resolved = new(new VsaArchitecture());

    [TestCase("Handlers.Requests", "Contracts.Commands", Permission.Allow)]
    [TestCase("Handlers.Events", "Contracts.Commands", Permission.Allow)]
    [TestCase("Handlers.Commands", "Contracts.Commands", Permission.Allow)]
    [TestCase("Handlers.Commands", "Contracts.Events", Permission.Allow)]
    [TestCase("Handlers.Requests", "Contracts.Requests", Permission.Deny)]
    [TestCase("Handlers.Commands", "Handlers.Commands", Permission.Deny)]
    [TestCase("Contracts.Requests", "Contracts.Commands", Permission.Deny)]
    public void A_slice_uses_only_the_commands_and_events_of_another_slice(string from, string to, Permission expected) {
        var user = Place("Feature", from);
        var target = Place("Feature", to, slice: "other");

        Assert.That(MayUse(user, target).Permission, Is.EqualTo(expected));
    }

    [TestCase(typeof(Umpire.Vsa.Handlers.Commands.BeforeCommandHook<>), "Handlers.Commands.Hooks")]
    [TestCase(typeof(Umpire.Vsa.Handlers.Commands.AfterCommandHook<,>), "Handlers.Commands.Hooks")]
    [TestCase(typeof(Umpire.Vsa.Handlers.Events.BeforeEventHook<>), "Handlers.Events.Hooks")]
    [TestCase(typeof(Umpire.Vsa.Handlers.Events.AfterEventHook<>), "Handlers.Events.Hooks")]
    public void A_hook_binds_to_the_handlers_of_its_target_kind(Type hookBase, string part) {
        var owners = Resolved.Parts.Where(p => p.Types.Contains(hookBase)).Select(p => p.Name).Distinct();

        Assert.That(owners, Is.EqualTo(new[] { part }));
    }

    [TestCase(typeof(Umpire.Vsa.Contracts.Requests.IRequest))]
    [TestCase(typeof(Umpire.Vsa.Contracts.Requests.IResponse))]
    public void The_request_bases_bind_to_the_request_contracts(Type requestBase) {
        var owners = Resolved.Parts.Where(p => p.Types.Contains(requestBase)).Select(p => p.Name).Distinct();

        Assert.That(owners, Is.EqualTo(new[] { "Contracts.Requests" }));
    }

    [TestCase("Kernel", "Composition", Permission.Allow)]
    [TestCase("Kernel", "Host", Permission.Allow)]
    [TestCase("Kernel", "Domain", Permission.Deny)]
    [TestCase("Feature", "Module", Permission.Deny)]
    [TestCase("Plugin", "Module", Permission.Deny)]
    public void The_app_host_uses_only_the_framework_entry(string group, string to, Permission expected) {
        Assert.That(MayUse(Place("App", "Host"), Place(group, to)).Permission, Is.EqualTo(expected));
    }

    [TestCase("Presenters", "Feature", "Domain.Shared", "A")]
    [TestCase("Presenters", "Feature", "Domain.Shared", "B")]
    [TestCase("Presenters", "Plugin", "Domain.Shared", "A")]
    [TestCase("Presenters", "Kernel", "Contracts.Events", "A")]
    [TestCase("Module", "Feature", "Domain.Shared", "B")]
    [TestCase("Module", "Plugin", "Infrastructure", "A")]
    public void A_use_only_target_is_allowed_with_internal_access(string from, string group, string to, string feature) {
        var decision = MayUse(Place("Feature", from), Place(group, to, feature));

        Assert.That((decision.Permission, decision.Rule?.Access), Is.EqualTo((Permission.Allow, (Access?)Access.Internal)));
    }

    [Test]
    public void A_module_root_uses_the_framework_with_public_access() {
        var decision = MayUse(Place("Feature", "Module"), Place("Kernel", "Domain"));

        Assert.That((decision.Permission, decision.Rule?.Access), Is.EqualTo((Permission.Allow, (Access?)Access.Public)));
    }

    [TestCase("Contracts.Requests", "Handlers.Requests")]
    [TestCase("Contracts.Commands", "Handlers.Commands")]
    [TestCase("Contracts.Events", "Handlers.Events")]
    public void A_contract_does_not_use_a_handler_of_its_own_slice(string contract, string handler) {
        Assert.That(MayUse(Place("Feature", contract), Place("Feature", handler)).Permission, Is.EqualTo(Permission.Deny));
    }

    [TestCase("Infrastructure")]
    [TestCase("Presenters")]
    [TestCase("Contracts.Events")]
    [TestCase("Handlers.Commands")]
    public void A_plugin_layer_uses_the_same_layer_of_another_plugin(string layer) {
        var part = Resolved.Parts.Single(p => p.Name == layer && p.Groups.Any(g => g.Name == "Plugin"));

        var decision = Resolved.DecisionFor(
            part, new Dictionary<string, string> { ["Plugin"] = "A" }, part, new Dictionary<string, string> { ["Plugin"] = "B" });

        Assert.That(decision.Permission, Is.EqualTo(Permission.Allow));
    }

    [Test]
    public void The_pack_assembly_binds_to_the_kernel_and_the_app_host_uses_it() {
        var kernel = Resolved.ExternalPart("Umpire.Vsa");

        Assert.That(kernel?.Name, Is.EqualTo("Kernel"));
        Assert.That(MayUse(Place("App", "Host"), new PartPlace(kernel!, new Dictionary<string, string>())).Permission,
            Is.EqualTo(Permission.Allow));
    }

    [TestCase("Contracts.Requests")]
    [TestCase("Contracts.Events")]
    [TestCase("Handlers.Commands")]
    [TestCase("Handlers.Commands.Hooks")]
    public void A_slice_uses_the_Slices_Shared_folder_of_its_own_feature_only(string user) {
        Assert.Multiple(() => {
            Assert.That(MayUse(Place("Feature", user), Place("Feature", "Slice.Shared")).Permission, Is.EqualTo(Permission.Allow));
            Assert.That(MayUse(Place("Feature", user), Place("Feature", "Slice.Shared", feature: "B")).Permission, Is.EqualTo(Permission.Deny));
        });
    }

    [TestCase("App", "Host", Permission.Allow)]
    [TestCase("Feature", "Domain", Permission.Allow)]
    [TestCase("Feature", "Handlers.Commands", Permission.Allow)]
    [TestCase("Plugin", "Infrastructure", Permission.Allow)]
    public void The_app_class_and_the_modules_use_the_packs_of_the_app(string group, string user, Permission expected) {
        Assert.That(MayUse(Place(group, user), Place("Pack", "Pack")).Permission, Is.EqualTo(expected));
    }

    [Test]
    public void A_folder_in_Packs_is_the_place_of_a_pack_block() {
        var type = new TypeFacts("App.Packs.Ecs.Kernel.EcsSystem", "EcsSystem", "App", new Source("C:/app/Packs/Ecs/Kernel/EcsSystem.cs", 1), [], [], [], []);

        var bindings = Resolved.Place([type], "C:/app", ["C:/app/Packs/Ecs"]);

        Assert.That(bindings.Unmapped, Is.Empty);
        Assert.That(bindings.All.Single().Part.Is(typeof(Pack)), Is.True);
    }

    [Test]
    public void A_hook_override_adds_a_part_to_each_feature() {
        var paths = new ResolvedArchitecture(new ExtraFeaturePart()).Parts.Select(p => p.Path);

        Assert.That(paths, Does.Contain("Features/{feature}/Extra"));
    }

    [TestCase(typeof(Umpire.Vsa.Definition.Feature))]
    [TestCase(typeof(Umpire.Vsa.Definition.Plugin))]
    public void A_module_kind_is_a_module_and_not_a_layer(Type kind) {
        Assert.That(kind.IsAssignableTo(typeof(Module)), Is.True);
        Assert.That(kind.IsAssignableTo(typeof(Layer)), Is.False);
    }

    [Test]
    public void The_module_rules_select_only_the_parts_of_the_module_classes() {
        var modules = Resolved.Parts.Where(p => p.Is(typeof(Module))).ToList();

        Assert.That(modules.Any(p => p.IsGroup), Is.False);
        Assert.That(modules.SelectMany(p => p.Types),
            Is.EquivalentTo(new[] { typeof(Umpire.Kernel.Module), typeof(Umpire.Vsa.Composition.Plugin) }));
    }

    static Decision MayUse(PartPlace from, PartPlace to) =>
        Resolved.DecisionFor( from.Part, from.Values, to.Part, to.Values);

    static PartPlace Place(string group, string name, string feature = "A", string slice = "s1") {
        var part = Resolved.Parts.Single(p => p.Name == name && p.SelfAndAncestors().Any(a => a.Name == group));
        var values = part.Groups.ToDictionary(i => i.Name, i => i.Name switch {
            "Feature" => feature,
            "Slice" => slice,
            _ => "P",
        });
        return new PartPlace(part, values);
    }

    sealed record PartPlace(Part Part, IReadOnlyDictionary<string, string> Values);

    sealed class ExtraFeaturePart : VsaArchitecture {
        protected override void DefineFeature(PartGroup feature) {
            base.DefineFeature(feature);
            feature.Add<Extra>();
        }
    }

    sealed record Extra : Layer;
}
