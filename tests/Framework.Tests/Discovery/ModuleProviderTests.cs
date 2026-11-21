namespace Framework.Tests.Discovery;

using Framework.Tests.Host;
using Microsoft.Extensions.DependencyInjection;
using Shelf;
using Toolbox;
using Umpire.Kernels;
using Assembly = System.Reflection.Assembly;

public sealed class ModuleProviderTests {
    [Test]
    public void TheDefaultProviderOfAHostIsTheScan() {
        using var host = new AppHost();

        Assert.That(host.ModuleProvider, Is.TypeOf<ScannedModules>());
    }

    [Test]
    public void TheScanCreatesTheModuleOfEachScannedRootSoTheBadFixtureRootStopsIt() {
        var types = new AppTypes(new ScannedModules());

        var error = Assert.Throws<InvalidOperationException>(() => _ = types.Modules);

        Assert.That(error.Message, Does.StartWith("Module Twin has 2 Module classes"));
    }

    [Test]
    public void AFixedListGivesItsModulesInOrder() {
        var types = new AppTypes(new FixedModules(new ShelfModule(), new ToolboxPlugin()));

        Assert.That(types.Modules.Select(module => module.Name), Is.EqualTo(new[] { "Toolbox", "Shelf" }));
    }

    [Test]
    public void AnEmptyFixedListGivesNoModule() =>
        Assert.That(new AppTypes(new FixedModules()).Modules, Is.Empty);

    [Test]
    public void AFixedListRefusesTheSameModuleTypeTwoTimes() {
        var error = Assert.Throws<InvalidOperationException>(
            () => _ = new FixedModules(new ShelfModule(), new ShelfModule()));

        Assert.That(error.Message, Is.EqualTo(
            "FixedModules has more than one module with the type Shelf.ShelfModule. Give each module one time."));
    }

    [Test]
    public void ACustomProviderScopesTheModulesToItsAssemblies() {
        var types = new AppTypes(new OneAssembly(typeof(ShelfModule).Assembly));

        Assert.That(types.Modules.Select(module => module.Name), Is.EqualTo(new[] { "Shelf" }));
    }

    [Test]
    public void TheScopeOfTheModulesIsAlsoTheScopeOfTheTypeScanWithNoLoopBackToTheProvider() {
        var shelf = typeof(ShelfModule).Assembly;

        var declared = new AppTypes(new OneAssembly(shelf)).Declared();

        Assert.That(declared.Select(type => type.Assembly).Distinct(), Is.EqualTo(new[] { shelf }));
    }

    [Test]
    public void TheScanOfGivenAssembliesKeepsATestAssembly() {
        var testOnly = typeof(TestOnly.TestOnlyModule).Assembly;

        var modules = new AppTypes(new OneAssembly(testOnly)).Modules;

        Assert.That(modules.Select(module => module.Name), Does.Contain("TestOnly"));
    }

    [Test]
    public void BuildComposesTheModulesOfTheProviderOfTheHost() {
        using var host = new AppHost { ModuleProvider = new OneAssembly(typeof(ShelfModule).Assembly) };

        var services = host.Build([], new ProbeApp());

        Assert.That(services.GetRequiredService<AppTypes>().Modules.Select(module => module.Name),
            Is.EqualTo(new[] { "Shelf" }));
    }

    sealed class OneAssembly(Assembly assembly) : IModuleProvider {
        public IReadOnlyList<Module> Modules(ModuleScan scan) => scan.In([assembly]);
    }

    sealed class ProbeApp : App {
        public override Pack Pack { get; } = new TestPack(() => new AppHost());
    }
}
