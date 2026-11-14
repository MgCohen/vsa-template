using Bare;
using Shelf;
using Toolbox;
using Twin;
using VSA.Module;

namespace Framework.Tests.Discovery;

public sealed class AppScanTests {
    [Test]
    public void PluginRootsAreFirstThenRootsAreInOrdinalOrderOfName() {
        // This test project references the plugin root Toolbox, the module roots Answers, Bare, Echo, Shelf, Signals and Twin,
        // and the module roots of the CommandApp fixture: Battle, Cards and Resources.
        var roots = AppScan.ModuleRoots();

        Assert.That(roots.Select(root => root.Name), Is.EqualTo(new[] { "Toolbox", "Answers", "Bare", "Battle", "Cards", "Echo", "Resources", "Shelf", "Signals", "Twin" }));
    }

    [Test]
    public void RootIsPluginIfItsRootClassInheritsPlugin() {
        Assert.That(ModuleRoot.In(typeof(ToolboxPlugin).Assembly).Single().IsPlugin, Is.True);
        Assert.That(ModuleRoot.In(typeof(BareModule).Assembly).Single().IsPlugin, Is.False);
    }

    [Test]
    public void RootNameIsTheLastPartOfTheNamespaceOfItsClass() {
        Assert.That(ModuleRoot.NameOf(typeof(ShelfModule)), Is.EqualTo("Shelf"));
        Assert.That(new ShelfModule().Name, Is.EqualTo("Shelf"));
    }

    [Test]
    public void RootCreatesItsOneModuleClass() {
        var module = ModuleRoot.In(typeof(ShelfModule).Assembly).Single().CreateModule();

        Assert.That(module, Is.TypeOf<ShelfModule>());
    }

    [Test]
    public void RootWithTwoModuleClassesInOneNamespaceIsAnError() {
        var root = ModuleRoot.In(typeof(TwinModule).Assembly).Single();

        var error = Assert.Throws<InvalidOperationException>(() => root.CreateModule());

        Assert.That(error.Message, Does.StartWith("Module Twin has 2 Module classes: ")
            .And.Contain("Twin.TwinModule")
            .And.Contain("Twin.OtherTwinModule")
            .And.EndWith(". A module must have one."));
    }

    [Test]
    public void AssemblyWithOnlyANestedModuleClassIsNotARoot() {
        Assert.That(ModuleRoot.IsRoot(typeof(AppScanTests).Assembly), Is.False);
    }
}
