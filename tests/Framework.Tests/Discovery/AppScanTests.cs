using Bare;
using Shelf;
using Shelf.Presenters;
using Toolbox;
using Twin;
using VSA.Module;

namespace Framework.Tests.Discovery;

public sealed class AppScanTests {
    [Test]
    public void PluginRootsAreFirstThenRootsAreInOrdinalOrderOfName() {
        // This test project references the module roots Answers, Bare, Echo, Shelf, Signals and Twin, which get the plugin root Toolbox,
        // and the module roots of the CommandApp fixture: Battle, Cards and Resources.
        var roots = AppScan.ModuleRoots();

        Assert.That(roots.Select(root => root.Name), Is.EqualTo(new[] { "Toolbox", "Answers", "Bare", "Battle", "Cards", "Echo", "Resources", "Shelf", "Signals", "Twin" }));
    }

    [Test]
    public void RootIsPluginIfItsRootClassInheritsPlugin() {
        Assert.That(ModuleRoot.Of(typeof(ToolboxPlugin).Assembly).IsPlugin, Is.True);
        Assert.That(ModuleRoot.Of(typeof(BareModule).Assembly).IsPlugin, Is.False);
    }

    [Test]
    public void RootWithNoLayerProjectsHasNoLayers() {
        var root = ModuleRoot.Of(typeof(BareModule).Assembly);

        Assert.That(root.Layers, Is.Empty);
    }

    [Test]
    public void RootFindsItsLayerProjectsByName() {
        var root = ModuleRoot.Of(typeof(ShelfModule).Assembly);

        Assert.That(root.Layers, Is.EqualTo(new[] { "Shelf.Presenters" }));
    }

    [Test]
    public void RootCreatesItsOneModuleClass() {
        var module = ModuleRoot.Of(typeof(ShelfModule).Assembly).CreateModule();

        Assert.That(module, Is.TypeOf<ShelfModule>());
    }

    [Test]
    public void RootWithNoModuleClassIsAnError() {
        // Shelf.Presenters has no Module class.
        var root = ModuleRoot.Of(typeof(ShelfPresenter).Assembly);

        var error = Assert.Throws<InvalidOperationException>(() => root.CreateModule());

        Assert.That(error.Message, Is.EqualTo(
            "Module Shelf.Presenters has no Module class. Add one class that inherits Module or Module<TSettings> to the Shelf.Presenters module root."));
    }

    [Test]
    public void RootWithTwoModuleClassesIsAnError() {
        var root = ModuleRoot.Of(typeof(TwinModule).Assembly);

        var error = Assert.Throws<InvalidOperationException>(() => root.CreateModule());

        Assert.That(error.Message, Does.StartWith("Module Twin has 2 Module classes: ")
            .And.Contain("Twin.TwinModule")
            .And.Contain("Twin.OtherTwinModule")
            .And.EndWith(". A module root must have one."));
    }

    [Test]
    public void AssemblyWithOnlyANestedModuleClassIsNotARoot() {
        Assert.That(ModuleRoot.IsRoot(typeof(AppScanTests).Assembly), Is.False);
    }
}
