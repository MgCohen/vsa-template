using Umpire.Architecture;
using Umpire.Vsa.Definition;

namespace EngineCore.Tests;

public sealed class VsaRulesTests {
    static readonly VsaArchitecture Definition = new();

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

        Assert.That(Decide(user, target).Permission, Is.EqualTo(expected));
    }

    [TestCase(typeof(Umpire.Vsa.Handlers.Commands.BeforeCommandHook<>), "Handlers.Commands")]
    [TestCase(typeof(Umpire.Vsa.Handlers.Commands.AfterCommandHook<,>), "Handlers.Commands")]
    [TestCase(typeof(Umpire.Vsa.Handlers.Events.BeforeEventHook<>), "Handlers.Events")]
    [TestCase(typeof(Umpire.Vsa.Handlers.Events.AfterEventHook<>), "Handlers.Events")]
    public void A_hook_binds_to_the_handlers_of_its_target_kind(Type hookBase, string part) {
        var owners = Definition.Parts().Where(p => p.Types.Contains(hookBase)).Select(p => p.Name).Distinct();

        Assert.That(owners, Is.EqualTo(new[] { part }));
    }

    [TestCase(typeof(Umpire.Vsa.Contracts.Requests.IRequest))]
    [TestCase(typeof(Umpire.Vsa.Contracts.Requests.IResponse))]
    public void The_request_bases_bind_to_the_request_contracts(Type requestBase) {
        var owners = Definition.Parts().Where(p => p.Types.Contains(requestBase)).Select(p => p.Name).Distinct();

        Assert.That(owners, Is.EqualTo(new[] { "Contracts.Requests" }));
    }

    [TestCase("Kernel", "Composition", Permission.Allow)]
    [TestCase("Kernel", "Host", Permission.Allow)]
    [TestCase("Kernel", "Domain", Permission.Deny)]
    [TestCase("Feature", "Module", Permission.Deny)]
    [TestCase("Plugin", "Module", Permission.Deny)]
    public void The_app_host_uses_only_the_framework_entry(string group, string to, Permission expected) {
        Assert.That(Decide(Place("App", "Host"), Place(group, to)).Permission, Is.EqualTo(expected));
    }

    [TestCase("Presenters", "Feature", "Domain.Shared", "A")]
    [TestCase("Presenters", "Feature", "Domain.Shared", "B")]
    [TestCase("Presenters", "Plugin", "Domain.Shared", "A")]
    [TestCase("Presenters", "Kernel", "Contracts.Events", "A")]
    [TestCase("Module", "Feature", "Domain.Shared", "B")]
    [TestCase("Module", "Plugin", "Infrastructure", "A")]
    public void A_use_only_target_is_allowed_with_internal_access(string from, string group, string to, string feature) {
        var (permission, pair) = Decide(Place("Feature", from), Place(group, to, feature));

        Assert.That((permission, pair?.Access), Is.EqualTo((Permission.Allow, (Access?)Access.Internal)));
    }

    [Test]
    public void A_module_root_uses_the_framework_with_public_access() {
        var (permission, pair) = Decide(Place("Feature", "Module"), Place("Kernel", "Domain"));

        Assert.That((permission, pair?.Access), Is.EqualTo((Permission.Allow, (Access?)Access.Public)));
    }

    static Decision Decide(PartPlace from, PartPlace to) =>
        Umpire.Architecture.Decide.For(Definition, from.Part, from.Values, to.Part, to.Values);

    static PartPlace Place(string group, string name, string feature = "A", string slice = "s1") {
        var part = Definition.Parts().Single(p => p.Name == name && p.SelfAndAncestors().Any(a => a.Name == group));
        var values = part.Groups.ToDictionary(i => i.Name, i => i.Name switch {
            "Feature" => feature,
            "Slice" => slice,
            _ => "P",
        });
        return new PartPlace(part, values);
    }

    sealed record PartPlace(Part Part, IReadOnlyDictionary<string, string> Values);
}
