using Bare;
using Shelf;
using Shelf.Presenters;
using Toolbox;
using Twin;
using VSA.Module;

namespace Framework.Tests.Discovery;

public sealed class ModuleDiscoveryTests {
    [Test]
    public void SharedRootsAreFirstThenRootsAreInOrdinalOrderOfName() {
        // This test project references the module roots Bare, Echo, Shelf and Twin, which get the Shared root Toolbox,
        // and the module roots of the CommandApp fixture: Battle, Cards and Resources.
        var roots = ModuleDiscovery.FindRoots();

        Assert.That(roots.Select(root => root.Name), Is.EqualTo(new[] { "Toolbox", "Bare", "Battle", "Cards", "Echo", "Resources", "Shelf", "Twin" }));
    }

    [Test]
    public void RootRecordsIfItIsShared() {
        Assert.That(ModuleRoot.Of(typeof(ToolboxModule).Assembly).IsShared, Is.True);
        Assert.That(ModuleRoot.Of(typeof(BareModule).Assembly).IsShared, Is.False);
    }

    [Test]
    public void RootWithNoLayerProjectsHasNoLayers() {
        var root = ModuleRoot.Of(typeof(BareModule).Assembly);

        Assert.That(root.Layers, Is.Empty);
    }

    [Test]
    public void RootRecordsItsLayerProjects() {
        var root = ModuleRoot.Of(typeof(ShelfModule).Assembly);

        Assert.That(root.Layers, Is.EqualTo(new[] { "Shelf.Presenters" }));
    }

    [Test]
    public void ModulesAreCreatedInTheOrderOfTheRoots() {
        var modules = ModuleDiscovery.CreateModules([ModuleRoot.Of(typeof(ShelfModule).Assembly)]);

        Assert.That(modules.Select(module => module.GetType()), Is.EqualTo(new[] { typeof(ShelfModule) }));
    }

    [Test]
    public void RootWithNoModuleClassIsAnError() {
        // Shelf.Presenters has no Module class.
        var root = ModuleRoot.Of(typeof(ShelfPresenter).Assembly);

        var error = Assert.Throws<InvalidOperationException>(() => ModuleDiscovery.CreateModules([root]));

        Assert.That(error.Message, Is.EqualTo(
            "Module Shelf.Presenters has no Module class. Add one class that inherits Module or Module<TSettings> to the Shelf.Presenters module root."));
    }

    [Test]
    public void RootWithTwoModuleClassesIsAnError() {
        var root = ModuleRoot.Of(typeof(TwinModule).Assembly);

        var error = Assert.Throws<InvalidOperationException>(() => ModuleDiscovery.CreateModules([root]));

        Assert.That(error.Message, Does.StartWith("Module Twin has 2 Module classes: ")
            .And.Contain("Twin.TwinModule")
            .And.Contain("Twin.OtherTwinModule")
            .And.EndWith(". A module root must have one."));
    }
}
