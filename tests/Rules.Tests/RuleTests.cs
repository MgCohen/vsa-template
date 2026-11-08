using VSA.Testing.Architecture;
using VSA.Testing.Core;
using static Rules.Tests.Fixtures;

namespace Rules.Tests;

// Each test runs a base test of VsaArchitectureTests on fixture modules. The rule is written only in the base.
public sealed class LayerMapRuleTests {
    // The Good fixture is correct, so a bad case uses a smaller layer map that does not permit what Good uses.
    static IReadOnlyList<Layer> With(Layer handlers) => [Layers.Domain, Layers.ContractsRequests, Layers.Module, handlers];

    [Test]
    public void Good_fixture_passes() => FixtureApp.Of("Good").VSA2001_module_projects_follow_the_layer_map();

    [Test]
    public void Use_of_a_module_project_that_the_map_does_not_permit_fails() {
        var message = Fails(FixtureApp.Of("Good", With(new Layer("Handlers.Requests").Reference(Layers.Domain))).VSA2001_module_projects_follow_the_layer_map);
        Assert.That(message, Does.Contain("Good.Handlers.Requests"));
        Assert.That(message, Does.Contain("Good.Contracts.Requests"));
    }

    [Test]
    public void Use_of_a_base_that_the_map_does_not_permit_fails() {
        var message = Fails(FixtureApp.Of("Good", With(new Layer("Handlers.Requests").Reference(Layers.ContractsRequests))).VSA2001_module_projects_follow_the_layer_map);
        Assert.That(message, Does.Contain("Good.Handlers.Requests"));
        Assert.That(message, Does.Contain("VSA.Domain"));
    }

    [Test]
    public void Project_in_a_layer_that_is_not_in_the_map_fails() {
        var message = Fails(FixtureApp.Of("Good", [Layers.Module, Layers.HandlersRequests]).VSA2001_module_projects_follow_the_layer_map);
        Assert.That(message, Does.Contain("Good.Contracts.Requests is in layer Contracts.Requests, which is not in the layer map"));
    }
}

// The UsesPlugins fixture module uses the plugin modules of tests/Fixtures/PluginApp. Pipeline uses RecordIds and Units.
public sealed class PluginModuleRuleTests {
    static VsaApp App(bool pipelineIsPlugin = true, bool recordIdsIsPlugin = true) => new([
        Root("UsesPlugins"),
        Root("Pipeline") with { IsPlugin = pipelineIsPlugin },
        Root("RecordIds") with { IsPlugin = recordIdsIsPlugin },
        Root("Units"),
    ]);

    [Test]
    public void The_Sdk_records_the_Plugins_folder() {
        Assert.That(Root("Pipeline").IsPlugin, Is.True);
        Assert.That(Root("UsesPlugins").IsPlugin, Is.False);
    }

    [Test]
    public void Use_of_the_same_layer_of_a_plugin_module_passes() =>
        FixtureApp.Of(App()).VSA2001_module_projects_follow_the_layer_map();

    [Test]
    public void Use_of_the_same_layer_of_a_module_that_is_not_a_plugin_fails() {
        var message = Fails(FixtureApp.Of(App(pipelineIsPlugin: false)).VSA2001_module_projects_follow_the_layer_map);
        // The layer project, and the types: the module request on the plugin base that is now a module.
        Assert.That(message, Does.Contain("UsesPlugins.Contracts.Requests"));
        Assert.That(message, Does.Contain("UsesPlugins.Slices.DealDamage.DealDamage does depend on \"Pipeline.Contracts.Requests.Message\""));
    }

    [Test]
    public void Use_of_a_module_by_a_plugin_module_fails() {
        var message = Fails(FixtureApp.Of(App(recordIdsIsPlugin: false)).VSA2001_module_projects_follow_the_layer_map);
        Assert.That(message, Does.Contain("Pipeline.Contracts.Requests"));
        Assert.That(message, Does.Contain("Pipeline.Contracts.Requests.MessageId does depend on \"RecordIds.Contracts.Requests.Id\""));
    }
}

public sealed class ModuleClassRuleTests {
    [Test]
    public void Good_fixture_passes() => FixtureApp.Of("Good").VSA2002_Module_classes_are_public_and_sealed();

    [Test]
    public void Module_class_that_is_not_sealed_fails() =>
        Assert.That(Fails(FixtureApp.Of("OpenModuleClass").VSA2002_Module_classes_are_public_and_sealed), Does.Contain("OpenModuleClass.OpenModuleClassModule"));
}

public sealed class AppHasModulesTests {
    [Test]
    public void App_with_no_modules_fails() =>
        Assert.That(Fails(new NoModules().The_app_has_modules), Does.Contain("No module root found"));

    [Explicit("The test calls The_app_has_modules.")]
    sealed class NoModules : VsaArchitectureTests {
        protected override VsaApp App { get; } = new([]);
    }
}

public sealed class LayerMapTests {
    [Test]
    public void Each_layer_name_is_unique() =>
        Assert.That(Layers.All.Select(l => l.Name), Is.Unique);

    // A reference by name, for example "Domain.Shared", must name a layer of the map.
    [Test]
    public void Each_edge_names_a_layer_of_the_map() {
        var unknown = Layers.All.SelectMany(l => l.References.Where(e => Layers.Find(e.Target) is null).Select(e => $"{l.Name} -> {e.Target}"));
        Assert.That(unknown, Is.Empty);
    }
}
