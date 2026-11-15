using Bare;
using Shelf;
using Toolbox;
using Twin;
using Umpire.Kernel;

namespace Framework.Tests.Discovery;

public sealed class AppTypesTests {
    [Test]
    public void RootsAreInOrdinalOrderOfName() {
        var roots = AppTypes.Roots();

        Assert.That(roots.Select(root => root.Name), Is.EqualTo(new[] { "Answers", "Bare", "Battle", "Cards", "Echo", "Resources", "Shelf", "Signals", "Toolbox", "Twin" }));
    }

    [Test]
    public void TheScanSkipsTheModulesOfATestAssembly() {
        Assert.That(AppTypes.Roots().Select(root => root.Name), Has.None.EqualTo("TestOnly"));
        var testRoots = ModuleRoot.In(typeof(TestOnly.TestOnlyModule).Assembly).Select(root => root.Name);

        Assert.That(testRoots, Does.Contain("TestOnly"));
    }

    [Test]
    public void PluginsAreBeforeModulesThenInOrdinalOrderOfName() {
        var modules = AppTypes.InOrder([new ShelfModule(), new BareModule(), new ToolboxPlugin()]);

        Assert.That(modules.Select(module => module.Name), Is.EqualTo(new[] { "Toolbox", "Bare", "Shelf" }));
    }

    [Test]
    public void TheTypesKeepTheModulesInOrder() {
        var types = new AppTypes([new ShelfModule(), new ToolboxPlugin()]);

        Assert.That(types.Modules.Select(module => module.Name), Is.EqualTo(new[] { "Toolbox", "Shelf" }));
    }

    [Test]
    public void RootNameIsTheLastPartOfTheNamespaceOfItsClass() {
        Assert.That(AppTypes.NameOf(typeof(ShelfModule)), Is.EqualTo("Shelf"));
        Assert.That(new ShelfModule().Name, Is.EqualTo("Shelf"));
    }

    [Test]
    public void RootCreatesItsOneModuleClass() {
        var module = ModuleRoot.In(typeof(ShelfModule).Assembly).Single().Create();

        Assert.That(module, Is.TypeOf<ShelfModule>());
    }

    [Test]
    public void RootWithTwoModuleClassesInOneNamespaceIsAnError() {
        var root = ModuleRoot.In(typeof(TwinModule).Assembly).Single();

        var error = Assert.Throws<InvalidOperationException>(() => root.Create());

        Assert.That(error.Message, Does.StartWith("Module Twin has 2 Module classes: ")
            .And.Contain("Twin.TwinModule")
            .And.Contain("Twin.OtherTwinModule")
            .And.EndWith(". A module must have one."));
    }

    [Test]
    public void ANestedModuleClassIsNotARoot() {
        Assert.That(ModuleRoot.In(typeof(AppTypesTests).Assembly).Select(root => root.Name), Is.EqualTo(new[] { "TestOnly" }));
    }
}
