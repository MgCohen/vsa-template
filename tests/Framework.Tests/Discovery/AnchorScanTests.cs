using Bare;
using Shelf;
using Toolbox;
using Twin;
using VSA.Kernel;

namespace Framework.Tests.Discovery;

public sealed class AnchorScanTests {
    [Test]
    public void RootsAreInOrdinalOrderOfName() {
        // This test project references the plugin root Toolbox, the module roots Answers, Bare, Echo, Shelf, Signals and Twin,
        // and the module roots of the CommandApp fixture: Battle, Cards and Resources.
        var roots = AnchorScan.Roots(typeof(VSA.Module.Module));

        Assert.That(roots.Select(root => root.Name), Is.EqualTo(new[] { "Answers", "Bare", "Battle", "Cards", "Echo", "Resources", "Shelf", "Signals", "Toolbox", "Twin" }));
    }

    [Test]
    public void PluginsAreBeforeModulesThenInOrdinalOrderOfName() {
        var anchors = AnchorScan.InOrder([new ShelfModule(), new BareModule(), new ToolboxPlugin()]);

        Assert.That(anchors.Select(anchor => anchor.Name), Is.EqualTo(new[] { "Toolbox", "Bare", "Shelf" }));
    }

    [Test]
    public void RootNameIsTheLastPartOfTheNamespaceOfItsClass() {
        Assert.That(AnchorRoot.NameOf(typeof(ShelfModule)), Is.EqualTo("Shelf"));
        Assert.That(new ShelfModule().Name, Is.EqualTo("Shelf"));
    }

    [Test]
    public void RootCreatesItsOneModuleClass() {
        var module = AnchorRoot.In(typeof(ShelfModule).Assembly, typeof(VSA.Module.Module)).Single().Create();

        Assert.That(module, Is.TypeOf<ShelfModule>());
    }

    [Test]
    public void RootWithTwoModuleClassesInOneNamespaceIsAnError() {
        var root = AnchorRoot.In(typeof(TwinModule).Assembly, typeof(VSA.Module.Module)).Single();

        var error = Assert.Throws<InvalidOperationException>(() => root.Create());

        Assert.That(error.Message, Does.StartWith("Module Twin has 2 Module classes: ")
            .And.Contain("Twin.TwinModule")
            .And.Contain("Twin.OtherTwinModule")
            .And.EndWith(". A module must have one."));
    }

    [Test]
    public void AssemblyWithOnlyANestedModuleClassHasNoRoot() {
        Assert.That(AnchorRoot.In(typeof(AnchorScanTests).Assembly, typeof(VSA.Module.Module)), Is.Empty);
    }

    [Test]
    public void TheInstallerScanFindsTheInstallersOfTheKernelAndOfEachLayer() {
        var installers = AnchorScan.Installers().Select(installer => installer.GetType().Name);

        Assert.That(installers, Is.SupersetOf(new[] {
            "AutoRegisterInstaller", "BusInstaller", "PipelineInstaller", "EventHandlerInstaller",
            "CommandHandlerInstaller", "RequestHandlerInstaller", "PresenterInstaller",
        }));
    }
}
