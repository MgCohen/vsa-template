using VSA.Testing.Architecture;
using VSA.Testing.Core;
using static Rules.Tests.Fixtures;

namespace Rules.Tests;

public sealed class LayerMapRuleTests {
    // The Good fixture is correct, so a bad case uses a smaller layer map that does not permit what Good uses.
    static IReadOnlyList<Layer> With(Layer handlers) => [Layers.Domain, Layers.ContractsRequests, Layers.Module, handlers];

    [Test]
    public void Good_fixture_passes() => Passes(VsaRules.LayerMap(App("Good"))!, App("Good"));

    [Test]
    public void Use_of_a_module_project_that_the_map_does_not_permit_fails() {
        var message = Fails(VsaRules.LayerMap(App("Good"), With(new Layer("Handlers.Requests").Reference(Layers.Domain)))!, App("Good"));
        Assert.That(message, Does.Contain("Good.Handlers.Requests"));
        Assert.That(message, Does.Contain("Good.Contracts.Requests"));
    }

    [Test]
    public void Use_of_a_base_that_the_map_does_not_permit_fails() {
        var message = Fails(VsaRules.LayerMap(App("Good"), With(new Layer("Handlers.Requests").Reference(Layers.ContractsRequests)))!, App("Good"));
        Assert.That(message, Does.Contain("Good.Handlers.Requests"));
        Assert.That(message, Does.Contain("VSA.Domain"));
    }

    [Test]
    public void Project_in_a_layer_that_is_not_in_the_map_fails() {
        var message = string.Join(Environment.NewLine, LayerRules.Unmapped(App("Good"), [Layers.Module, Layers.HandlersRequests]));
        Assert.That(message, Does.Contain("Good.Contracts.Requests is in layer Contracts.Requests, which is not in the layer map"));
    }
}

public sealed class ModuleClassRuleTests {
    [Test]
    public void Good_fixture_passes() => Passes(VsaRules.ModuleClasses, App("Good"));

    [Test]
    public void Module_class_that_is_not_sealed_fails() =>
        Assert.That(Fails(VsaRules.ModuleClasses, App("OpenModuleClass")), Does.Contain("OpenModuleClass.OpenModuleClassModule"));
}

public sealed class LayerMapTests {
    [Test]
    public void Each_layer_name_is_unique() =>
        Assert.That(Layers.All.Select(l => l.Name), Is.Unique);

    // A reference by name, for example "Infrastructure.EfCore", must name a layer of the map.
    [Test]
    public void Each_edge_names_a_layer_of_the_map() {
        var unknown = Layers.All.SelectMany(l => l.References.Concat(l.Exposed).Where(e => Layers.Find(e.Target) is null).Select(e => $"{l.Name} -> {e.Target}"));
        Assert.That(unknown, Is.Empty);
    }
}
