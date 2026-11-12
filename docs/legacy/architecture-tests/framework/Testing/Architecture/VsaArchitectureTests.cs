using ArchUnitNET.Loader;
using ArchUnitNET.NUnit;
using NUnit.Framework;
using VSA.Testing.Core;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchModel = ArchUnitNET.Domain.Architecture;

namespace VSA.Testing.Architecture;

// The VSA architecture rules as NUnit tests. Each rule is one [Test] here. A consumer test project needs one class:
//   public sealed class ArchitectureTests : VsaArchitectureTests;
// The modules come from the project libraries of the test run, so the test project must reference the host.
// To add an app rule, add a [Test] in the derived class and check an ArchUnitNET rule on Architecture.
public abstract class VsaArchitectureTests {
    VsaApp? app;
    ArchModel? architecture;

    // A fixture test overrides App or LayerMap, so the same tests run on other modules or on another layer map.
    protected virtual VsaApp App => app ??= VsaApp.Current();
    protected virtual IReadOnlyList<Layer> LayerMap => Layers.All;

    // The module projects and the VSA bases. ArchUnitNET checks only the types that it loads.
    protected virtual ArchModel Architecture => architecture ??= new ArchLoader()
        .LoadAssemblies([.. LayerRules.Projects(App).Select(p => p.Assembly), .. LayerRules.Bases(Layers.All)])
        .Build();

    // With no modules, each other rule passes with nothing to check. That is a wrong pass.
    [Test]
    public void The_app_has_modules() {
        if (App.Modules.Count == 0) {
            Assert.Fail($"No module root found for {GetType().Assembly.GetName().Name}. Add a project reference to the host, and build the modules with VSA.Sdk.");
        }
    }

    // Each module project uses only the VSA bases and module projects that the layer map permits.
    [Test]
    public void VSA2001_module_projects_follow_the_layer_map() {
        var unmapped = LayerRules.Unmapped(App, LayerMap).ToList();
        if (unmapped.Count > 0) {
            Assert.Fail(string.Join(Environment.NewLine, unmapped));
        }
        // One rule for each module project. The scope reports the failures of all projects, not only the first.
        using var scope = Assert.EnterMultipleScope();
        foreach (var (project, forbidden) in LayerRules.Forbidden(App, LayerMap)) {
            Types().That().ResideInAssembly(project.Assembly)
                .Should().NotDependOnAny(Types().That().ResideInAssembly(forbidden[0], [.. forbidden.Skip(1)]))
                .Because($"the layer map does not permit {project.Layer} to use them. Remove the use, or change the layer map")
                .WithoutRequiringPositiveResults()
                .Check(Architecture);
        }
    }

    // The host creates each Module class at start-up. Public sealed: no other class inherits it.
    [Test]
    public void VSA2002_Module_classes_are_public_and_sealed() =>
        Classes().That().AreAssignableTo(typeof(VSA.Module.Module)).And().AreNotAbstract()
            .Should().BePublic().AndShould().BeSealed()
            .Because("the host creates each Module class, and no other class may inherit it")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
}
