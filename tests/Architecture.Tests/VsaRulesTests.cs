namespace EngineCore.Tests;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;
using Umpire.Kernels.Blocks;
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

    [TestCase("Infrastructure", "Domain", Permission.Allow)]
    [TestCase("Handlers.Commands", "Contracts.Commands", Permission.Allow)]
    [TestCase("Handlers.Events", "Contracts.Events.Publishing", Permission.Allow)]
    [TestCase("Contracts.Events.Publishing", "Contracts.Events", Permission.Allow)]
    [TestCase("Presenters", "Contracts.Requests", Permission.Allow)]
    [TestCase("Presenters", "Handlers.Events", Permission.Allow)]
    [TestCase("Views", "Presenters", Permission.Allow)]
    [TestCase("Composition", "Handlers.Commands", Permission.Allow)]
    [TestCase("Kernel", "Handlers.Commands", Permission.Allow)]
    [TestCase("Domain", "Infrastructure", Permission.Deny)]
    [TestCase("Infrastructure", "Contracts.Requests", Permission.Deny)]
    [TestCase("Contracts.Requests", "Contracts.Commands", Permission.Deny)]
    [TestCase("Handlers.Commands", "Contracts.Events", Permission.Deny)]
    [TestCase("Handlers.Requests", "Domain", Permission.Deny)]
    [TestCase("Presenters", "Handlers.Commands", Permission.Deny)]
    [TestCase("Presenters", "Contracts.Events.Publishing", Permission.Deny)]
    [TestCase("Views", "Handlers.Commands", Permission.Deny)]
    [TestCase("Composition", "Domain", Permission.Deny)]
    public void A_kernel_layer_uses_only_the_layers_of_its_project_references(string from, string to, Permission expected) {
        Assert.That(MayUse(Place("Kernel", from), Place("Kernel", to)).Permission, Is.EqualTo(expected));
    }

    [TestCase("Kernel", "Composition", Permission.Allow)]
    [TestCase("Core", "Hosts", Permission.Allow)]
    [TestCase("Kernel", "Domain", Permission.Deny)]
    [TestCase("Feature", "Feature", Permission.Allow)]
    [TestCase("Plugin", "Plugin", Permission.Allow)]
    [TestCase("Feature", "Domain", Permission.Deny)]
    [TestCase("Feature", "Infrastructure", Permission.Deny)]
    [TestCase("Plugin", "Domain", Permission.Deny)]
    public void The_app_host_uses_the_framework_entry_and_only_the_root_class_of_a_module(string group, string to, Permission expected) {
        Assert.That(MayUse(Place("App", "Host"), Place(group, to)).Permission, Is.EqualTo(expected));
    }

    [TestCase("Presenters", "Feature", "Domain.Shared", "A")]
    [TestCase("Presenters", "Feature", "Domain.Shared", "B")]
    [TestCase("Presenters", "Plugin", "Domain.Shared", "A")]
    [TestCase("Presenters", "Kernel", "Contracts.Events", "A")]
    [TestCase("Feature", "Feature", "Domain.Shared", "B")]
    [TestCase("Feature", "Plugin", "Infrastructure", "A")]
    public void A_use_only_target_is_allowed_with_internal_access(string from, string group, string to, string feature) {
        var decision = MayUse(Place("Feature", from), Place(group, to, feature));

        Assert.That((decision.Permission, decision.Rule?.Access), Is.EqualTo((Permission.Allow, (Access?)Access.Internal)));
    }

    [Test]
    public void A_module_root_uses_the_framework_with_public_access() {
        var decision = MayUse(Place("Feature", "Feature"), Place("Kernel", "Domain"));

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
    public void The_app_host_uses_the_framework_kernel() {
        Assert.That(MayUse(Place("App", "Host"), Place("Core", "KernelParts.App")).Permission, Is.EqualTo(Permission.Allow));
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

    [TestCase("(root)", "Host", Permission.Allow)]
    [TestCase("Feature", "Domain", Permission.Allow)]
    [TestCase("Feature", "Handlers.Commands", Permission.Allow)]
    [TestCase("Plugin", "Infrastructure", Permission.Allow)]
    public void The_app_class_and_the_modules_use_the_packs_of_the_app(string group, string user, Permission expected) {
        Assert.That(MayUse(Place(group, user), Place("Pack", "Pack")).Permission, Is.EqualTo(expected));
    }

    [Test]
    public void A_folder_in_Packs_is_the_place_of_a_pack_block() {
        var type = new TypeFacts("App.Packs.Ecs.Kernel.EcsSystem", "EcsSystem", "App", new Source("C:/app/App/Packs/Ecs/Kernel/EcsSystem.cs", 1), [], [], [], []);

        var bindings = Resolved.Place([type], "C:/app", ["C:/app/App/Packs/Ecs"]);

        Assert.That(bindings.Unmapped, Is.Empty);
        Assert.That(bindings.All.Single().Part.Is(typeof(Pack)), Is.True);
    }

    [Test]
    public void A_type_at_the_block_root_of_a_VSA_app_has_no_part() {
        var type = new TypeFacts("App.Stray", "Stray", "App", new Source("C:/app/Stray.cs", 1), [], [], [], []);

        var bindings = Resolved.Place([type], "C:/app");

        Assert.That(bindings.Unmapped.Select(u => u.Type.Name), Is.EqualTo(new[] { "Stray" }));
    }

    [Test]
    public void A_hook_override_adds_a_part_to_each_feature() {
        var paths = new ResolvedArchitecture(new ExtraFeaturePart()).Parts.Select(p => p.Path);

        Assert.That(paths, Does.Contain("App/Features/{feature}/Extra"));
    }

    [TestCase(typeof(Umpire.Vsa.Definition.Feature))]
    [TestCase(typeof(Umpire.Vsa.Definition.Plugin))]
    public void A_module_kind_is_a_module_and_not_a_layer(Type kind) {
        Assert.That(kind.IsAssignableTo(typeof(Module)), Is.True);
        Assert.That(kind.IsAssignableTo(typeof(Layer)), Is.False);
    }

    [TestCase("/_/packs/Vsa/Kernel/Contracts/Requests/IRequestSender.cs", "Contracts.Requests", "Kernel")]
    [TestCase("/_/packs/Vsa/Kernel/Handlers/Commands/CommandHandler.cs", "Handlers.Commands", "Kernel")]
    [TestCase("/_/framework/Components/Messages/Contracts/Envelope.cs", "MessagesParts.Contracts", "MessagesComponent")]
    [TestCase("C:/repo/packs/Vsa/Kernel/Domain/Entity.cs", "Domain", "Kernel")]
    [TestCase("/_/framework/Kernel/Hosts/Web/WebHost.cs", "Hosts", "Core")]
    [TestCase("/_/framework/Kernel/App/App.cs", "KernelParts.App", "Core")]
    [TestCase("/_/framework/Kernel/Messages/Pipeline/Pipelines.cs", "KernelParts.Messages", "Core")]
    public void A_type_of_a_DLL_binds_to_the_body_part_of_its_source_path(string file, string part, string block) {
        var type = new TypeFacts("Umpire.Some.Type", "Type", "Umpire", new Source(file, 1), [], [], [], []);

        var bound = Resolved.Place([type], Path.GetFullPath("Demo")).Of(type.FullName);

        Assert.Multiple(() => {
            Assert.That(bound?.Part.Name, Is.EqualTo(part));
            Assert.That(bound?.Part.SelfAndAncestors().Select(p => p.Name), Does.Contain(block));
            Assert.That(bound?.External, Is.True);
        });
    }

    [Test]
    public void A_type_of_a_DLL_with_no_folder_of_a_placed_block_is_not_bound() {
        var type = new TypeFacts("Other.Type", "Type", "Other", new Source("/_/src/Other/Type.cs", 1), [], [], [], []);

        Assert.That(Resolved.Place([type], Path.GetFullPath("Demo")).Of(type.FullName), Is.Null);
    }

    [Test]
    public void A_domain_use_of_a_request_sender_in_the_VSA_runtime_DLL_is_denied() {
        var block = Path.GetFullPath("Demo");
        var sender = new TypeFacts("Umpire.Vsa.Contracts.Requests.IRequestSender", "IRequestSender", "Umpire.Vsa",
            new Source("/_/packs/Vsa/Kernel/Contracts/Requests/IRequestSender.cs", 1), [], [], [], []);
        var order = AppType(block, "Demo.Features.Shop.Domain.Order", "App/Features/Shop/Domain/Order.cs") with { Mentions = [sender.FullName] };

        var bindings = Resolved.Place([order, sender], block);

        Assert.That(bindings.Of(sender.FullName)?.Part.Name, Is.EqualTo("Contracts.Requests"));
        Assert.That(Resolved.DecisionFor(bindings.Of(order.FullName)!, bindings.Of(sender.FullName)!).Permission, Is.EqualTo(Permission.Deny));
    }

    [Test]
    public void An_app_type_with_an_Umpire_namespace_binds_to_its_folder_and_not_to_the_kernel() {
        var block = Path.GetFullPath("Demo");
        var order = AppType(block, "Umpire.Demo.Features.Shop.Domain.Order", "App/Features/Shop/Domain/Order.cs");

        var bound = Resolved.Place([order], block).Of(order.FullName);

        Assert.That(bound?.External, Is.False);
        Assert.That(bound?.Part.Name, Is.EqualTo("Domain"));
    }

    [Test]
    public void An_app_folder_named_Kernel_in_the_runtime_root_does_not_bind_to_the_kernel_root() {
        var block = Path.GetFullPath("Demo");
        var thing = AppType(block, "Demo.Features.X.Kernel.Thing", "App/Features/X/Kernel/Thing.cs");

        var bound = Resolved.Place([thing], block).Of(thing.FullName);

        Assert.That(bound?.External, Is.Not.True);
        Assert.That(bound?.Part.SelfAndAncestors().Select(p => p.Concept?.GetType()) ?? [],
            Has.None.EqualTo(typeof(Umpire.Architecture.Blocks.Kernel)));
    }

    static TypeFacts AppType(string block, string fullName, string file) =>
        new(fullName, fullName[(fullName.LastIndexOf('.') + 1)..], "Demo", new Source(Path.Combine(block, file), 1), [], [], [], []);

    static Decision MayUse(PartPlace from, PartPlace to) =>
        Resolved.DecisionFor(from.Part, from.Values, to.Part, to.Values);

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
        protected override void Define(Part root, Part kernel) {
            root.For<Umpire.Vsa.Definition.Feature>().Add<Extra>();
        }
    }

    sealed record Extra : Layer;
}
