namespace Framework.Tests.Discovery;

using Shelf;
using Toolbox;
using Umpire.Kernels;

public sealed class ModuleProviderTests {
    [Test]
    public void TheDefaultProviderOfAHostIsTheScan() =>
        Assert.That(new AppHost().ModuleProvider, Is.TypeOf<ScannedModules>());

    [Test]
    public void TheScanCreatesTheModuleOfEachScannedRootSoTheBadFixtureRootStopsIt() {
        var types = AppTypes.Of(new ScannedModules());

        var error = Assert.Throws<InvalidOperationException>(() => _ = types.Modules);

        Assert.That(error.Message, Does.StartWith("Module Twin has 2 Module classes"));
    }

    [Test]
    public void AFixedListGivesItsModulesInOrder() {
        var types = AppTypes.Of(new FixedModules(new ShelfModule(), new ToolboxPlugin()));

        Assert.That(types.Modules.Select(module => module.Name), Is.EqualTo(new[] { "Toolbox", "Shelf" }));
    }

    [Test]
    public void AnEmptyFixedListGivesNoModule() =>
        Assert.That(AppTypes.Of(new FixedModules()).Modules, Is.Empty);

    [Test]
    public void ACustomProviderScopesTheModulesToItsAssemblies() {
        var types = AppTypes.Of(new OneAssembly(typeof(ShelfModule).Assembly));

        Assert.That(types.Modules.Select(module => module.Name), Is.EqualTo(new[] { "Shelf" }));
    }

    [Test]
    public void TheScopeOfTheModulesIsAlsoTheScopeOfTheTypeScan() {
        var shelf = typeof(ShelfModule).Assembly;

        var declared = AppTypes.Of(new OneAssembly(shelf)).Declared();

        Assert.That(declared.Select(type => type.Assembly).Distinct(), Is.EqualTo(new[] { shelf }));
    }

    sealed class OneAssembly(System.Reflection.Assembly assembly) : IModuleProvider {
        public IReadOnlyList<Module> Modules(AppTypes types) => types.ModulesIn([assembly]);
    }
}
