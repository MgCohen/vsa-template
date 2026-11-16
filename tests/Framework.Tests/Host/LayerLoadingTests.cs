namespace Framework.Tests.Host;

using Shelf;
using Shelf.Presenters;

public sealed class LayerLoadingTests {
    [Test]
    public void HostLoadsOnlyTheLayersThatExist() {
        var registrations = WebApps.OnTestServer(new ShelfModule()).Registrations();

        Assert.That(registrations.Any(s => s.ServiceType == typeof(ShelfPresenter)), Is.True);
    }
}
