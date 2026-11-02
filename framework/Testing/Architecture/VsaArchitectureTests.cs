using ArchUnitNET.Loader;
using ArchUnitNET.NUnit;
using NUnit.Framework;
using ArchModel = ArchUnitNET.Domain.Architecture;

namespace VSA.Testing.Architecture;

// The VSA architecture rules as NUnit tests. A consumer test project needs one class:
//   public sealed class ArchitectureTests : VsaArchitectureTests<Program>;
// TEntry is a type in the host assembly. The modules come from the project libraries of the test run.
// To add an app rule, add a [Test] in the derived class and check an ArchUnitNET rule on Architecture.
public abstract class VsaArchitectureTests<TEntry> {
    static readonly Lazy<VsaApp> SharedApp = new(VsaApp.Of<TEntry>);
    static readonly Lazy<ArchModel> SharedArchitecture = new(() => Load(SharedApp.Value));

    protected static VsaApp App => SharedApp.Value;
    protected static ArchModel Architecture => SharedArchitecture.Value;

    // The module projects and the VSA bases. ArchUnitNET checks only the types that it loads.
    public static ArchModel Load(VsaApp app) => new ArchLoader().LoadAssemblies([.. app.Assemblies, .. LayerRules.Bases()]).Build();

    // With no modules, each other rule passes with nothing to check. That is a wrong pass.
    [Test]
    public void The_app_has_modules() {
        if (App.Modules.Count == 0) {
            Assert.Fail($"No module root found for {typeof(TEntry).Assembly.GetName().Name}. Add a project reference to each module, and build the modules with VSA.Sdk.");
        }
    }

    [Test]
    public void VSA2001_module_projects_follow_the_layer_map() {
        var unmapped = LayerRules.Unmapped(App).ToList();
        if (unmapped.Count > 0) {
            Assert.Fail(string.Join(Environment.NewLine, unmapped));
        }
        VsaRules.LayerMap(App)?.Check(Architecture);
    }

    [Test]
    public void VSA2002_Module_classes_are_public_and_sealed() => VsaRules.ModuleClasses.Check(Architecture);
}
