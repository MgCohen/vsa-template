using ArchUnitNET.NUnit;
using NUnit.Framework;
using VSA.Testing.Core;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchModel = ArchUnitNET.Domain.Architecture;

namespace VSA.Testing.Architecture;

// The VSA architecture rules as NUnit tests. Each rule is one [Test] here. A consumer test project needs one class:
//   public sealed class ArchitectureTests : VsaArchitectureTests<Program>;
// TEntry is a type in the host assembly. The modules come from the project libraries of the test run.
// To add an app rule, add a [Test] in the derived class and check an ArchUnitNET rule on Architecture.
public abstract class VsaArchitectureTests<TEntry> {
    static readonly Lazy<VsaApp> SharedApp = new(VsaApp.Of<TEntry>);
    static readonly Lazy<ArchModel> SharedArchitecture = new(() => VsaRules.Load(SharedApp.Value));

    // A fixture test overrides these, so the same tests run on other modules or on another layer map.
    protected virtual VsaApp App => SharedApp.Value;
    protected virtual ArchModel Architecture => SharedArchitecture.Value;
    protected virtual IReadOnlyList<Layer> LayerMap => Layers.All;

    // With no modules, each other rule passes with nothing to check. That is a wrong pass.
    [Test]
    public void The_app_has_modules() {
        if (App.Modules.Count == 0) {
            Assert.Fail($"No module root found for {typeof(TEntry).Assembly.GetName().Name}. Add a project reference to the host, and build the modules with VSA.Sdk.");
        }
    }

    // Each module project uses only the VSA bases and module projects that the layer map permits.
    [Test]
    public void VSA2001_module_projects_follow_the_layer_map() {
        var unmapped = LayerRules.Unmapped(App, LayerMap).ToList();
        if (unmapped.Count > 0) {
            Assert.Fail(string.Join(Environment.NewLine, unmapped));
        }
        VsaRules.All(LayerRules.For(App, LayerMap))?.Check(Architecture);
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
