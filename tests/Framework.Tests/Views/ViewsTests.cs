namespace Framework.Tests.Views;

using Framework.Tests.Host;
using Shelf;
using Umpire.Kernel;
using Umpire.Vsa.Views;

public sealed class ViewsTests {
    [Test]
    public void AnAppWithNoViewsRootGetsNoViewServices() {
        var registrations = WebApps.OnTestServer(new ShelfModule()).Registrations();

        Assert.That(registrations.Any(s => s.ServiceType.Namespace?.StartsWith("Microsoft.AspNetCore.Components", StringComparison.Ordinal) == true), Is.False);
    }

    [Test]
    public void AViewsRootInAnAssemblyWithNoModuleIsNotTheRootOfTheApp() {
        var roots = new AppTypes([new ShelfModule()]).ConcreteTypesOf(typeof(ViewsRoot));

        Assert.That(roots, Does.Not.Contain(typeof(TestAssemblyRoot)).And.Empty);
    }

    [Test]
    public void NoViewsRootFindsNoRoot() =>
        Assert.That(ViewsServiceCollectionExtensions.FindRoot([]), Is.Null);

    [Test]
    public void TwoViewsRootsStopTheApp() {
        var error = Assert.Throws<InvalidOperationException>(() => ViewsServiceCollectionExtensions.FindRoot([typeof(FirstRoot), typeof(SecondRoot)]));

        Assert.That(error!.Message, Does.StartWith("The app has 2 views roots: "));
        Assert.That(error.Message, Does.EndWith("Keep one component that inherits ViewsRoot."));
    }

    abstract class FirstRoot : ViewsRoot;

    abstract class SecondRoot : ViewsRoot;

    sealed class TestAssemblyRoot : ViewsRoot;
}
